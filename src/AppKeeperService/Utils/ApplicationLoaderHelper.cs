using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace AppKeeperService.Utils;

    /// <summary>
    /// Class that allows running applications with full admin rights. 
    /// In addition the application launched will bypass the Vista UAC prompt.
    /// </summary>

// Source: https://www.codeproject.com/Articles/35773/Subverting-Vista-UAC-in-Both-32-and-64-bit-Archite

    public class ApplicationLoaderHelper
{

    #region Structures

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public int Length;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STARTUPINFO
    {
        public int cb;
        public String lpReserved;
        public String lpDesktop;
        public String lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    #endregion

    #region Enumerations

    enum TOKEN_TYPE : int
    {
        TokenPrimary = 1,
        TokenImpersonation = 2
    }

    enum SECURITY_IMPERSONATION_LEVEL : int
    {
        SecurityAnonymous = 0,
        SecurityIdentification = 1,
        SecurityImpersonation = 2,
        SecurityDelegation = 3,
    }

    #endregion

    #region Constants

    public const int TOKEN_DUPLICATE = 0x0002;
    public const uint MAXIMUM_ALLOWED = 0x2000000;
    public const int CREATE_NEW_CONSOLE = 0x00000010;

    public const int IDLE_PRIORITY_CLASS = 0x40;
    public const int NORMAL_PRIORITY_CLASS = 0x20;
    public const int HIGH_PRIORITY_CLASS = 0x80;
    public const int REALTIME_PRIORITY_CLASS = 0x100;
    public const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    #endregion

    #region Win32 API Imports

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hSnapshot);

    [DllImport("kernel32.dll")]
    static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public extern static bool CreateProcessAsUser(IntPtr hToken, String lpApplicationName, String lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
        ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandle, int dwCreationFlags, IntPtr lpEnvironment,
        String lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll")]
    static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);

    [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
    public extern static bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess,
        ref SECURITY_ATTRIBUTES lpThreadAttributes, int TokenType,
        int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("advapi32", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
    static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

    [DllImport("userenv.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

    //[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    //static extern int GetLastError();

    [DllImport("userenv.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

    #endregion

    /// <summary>
    /// Launches the given application with full admin rights, and in addition bypasses the Vista UAC prompt
    /// </summary>
    /// <param name="applicationName">The name of the application to launch</param>
    /// <param name="procInfo">Process information regarding the launched application that gets returned to the caller</param>
    /// <returns></returns>
    public static (bool, int) StartProcessAndBypassUAC(String applicationName, ILogger logger)
    {
        uint _winlogonPid = 0;
        IntPtr _hUserTokenDup = IntPtr.Zero, _hPToken = IntPtr.Zero, _hProcess = IntPtr.Zero, _lpEnvironment = IntPtr.Zero;
        PROCESS_INFORMATION _procInfo = new PROCESS_INFORMATION();

        // obtain the currently active session id; every logged on user in the system has a unique session id
        uint _dwSessionId = WTSGetActiveConsoleSessionId();
        logger.LogInformation("Current console SessionId: {sessionId}", _dwSessionId);

        // obtain the process id of the winlogon process that is running within the currently active session
        string _processToClone = "explorer";
        Process[] _processes = Process.GetProcessesByName(_processToClone); //"winlogon" 
        foreach (Process _p in _processes)
        {
            logger.LogInformation("Evaluating process: {process}, Id: {id}, SessionId: {sId}", _p.ProcessName, _p.Id, _p.SessionId);
            if (_p.ProcessName == _processToClone) // v1.1.0.5: disregard console session in favor of process-name. (uint)_p.SessionId == _dwSessionId
            {
                logger.LogInformation("Match - setting Id to: {id}", _p.Id);
                _winlogonPid = (uint)_p.Id;
            }
        }
        logger.LogInformation("{processToClone} is running in session-id: {_winlogonPid}", _processToClone, _winlogonPid);


        // obtain a handle to the winlogon process
        _hProcess = OpenProcess(MAXIMUM_ALLOWED, false, _winlogonPid);

        logger.LogInformation("Preparing to evaluate available processes.");
        // obtain a handle to the access token of the winlogon process
        if (!OpenProcessToken(_hProcess, TOKEN_DUPLICATE, ref _hPToken))
        {
            CloseHandle(_hProcess);
            logger.LogWarning("Failed to obtain handle to access token");
            return (false, 0);
        }
        logger.LogInformation("Obtained access token OK");


        // Security attibute structure used in DuplicateTokenEx and CreateProcessAsUser
        // I would prefer to not have to use a security attribute variable and to just 
        // simply pass null and inherit (by default) the security attributes
        // of the existing token. However, in C# structures are value types and therefore
        // cannot be assigned the null value.
        SECURITY_ATTRIBUTES _sa = new SECURITY_ATTRIBUTES();
        _sa.Length = Marshal.SizeOf(_sa);

        logger.LogInformation("Preparing to copy access token.");
        // copy the access token of the winlogon process; the newly created token will be a primary token
        if (!DuplicateTokenEx(_hPToken, MAXIMUM_ALLOWED, ref _sa, (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int)TOKEN_TYPE.TokenPrimary, ref _hUserTokenDup))
        {
            CloseHandle(_hProcess);
            CloseHandle(_hPToken);
            logger.LogWarning("Failed to copy access token");
            return (false, 0);
        }
        logger.LogInformation("Copied access token OK");

        // By default CreateProcessAsUser creates a process on a non-interactive window station, meaning
        // the window station has a desktop that is invisible and the process is incapable of receiving
        // user input. To remedy this we set the lpDesktop parameter to indicate we want to enable user 
        // interaction with the new process.
        STARTUPINFO _si = new STARTUPINFO();
        _si.cb = (int)Marshal.SizeOf(_si);
        _si.lpDesktop = @"winsta0\default"; // interactive window station parameter; basically this indicates that the process created can display a GUI on the desktop

        // flags that specify the priority and creation method of the process
        int _dwCreationFlags = CREATE_UNICODE_ENVIRONMENT; //NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;

        // create a discreet environment-block the new process
        bool _resultEnv = CreateEnvironmentBlock(out _lpEnvironment, _hUserTokenDup, false);
        logger.LogInformation("@ApplicationLoaderHelper, got environment as current user: {resultEnv}", _resultEnv);
        ReadEnvironmentBlock(_lpEnvironment, logger);

        // create a new process in the current user's logon session
        bool _processWasStartedSuccessfully = CreateProcessAsUser(_hUserTokenDup,        // client's access token
                                        null,                   // file to execute #null
                                        applicationName,        // command line #applicationName
                                        ref _sa,                 // pointer to process SECURITY_ATTRIBUTES
                                        ref _sa,                 // pointer to thread SECURITY_ATTRIBUTES
                                        false,                  // handles are not inheritable
                                        _dwCreationFlags,        // creation flags
                                        _lpEnvironment,          // pointer to new environment block  // IntPtr.Zero
                                        null,                   // name of current directory 
                                        ref _si,                 // pointer to STARTUPINFO structure
                                        out _procInfo            // receives information about new process
                                        );

        if (_processWasStartedSuccessfully == false)
        {
            int _error = Marshal.GetLastWin32Error();
            logger.LogError("@ApplicationLoaderHelper, could not start process {processName} as user. Errorcode: {error}", applicationName, _error);
        }

        // invalidate the handles
        CloseHandle(_hProcess);
        CloseHandle(_hPToken);
        CloseHandle(_hUserTokenDup);

        logger.LogInformation("@ApplicationLoaderHelper, started process {processName}, PID: {dwProcessId}", applicationName, _procInfo.dwProcessId);
        return (_processWasStartedSuccessfully, (int)_procInfo.dwProcessId);
    }


    /// <summary>
    /// This method is only intended to dump the contents of the EnvVar block to the log-file.
    /// </summary>
    private static void ReadEnvironmentBlock(IntPtr lpEnvironment, ILogger logger)
    {
        // not used - but left in if we later want to return the result
        var _envVars = new Dictionary<string, string> { };

        IntPtr _next = lpEnvironment;
        while (Marshal.ReadByte(_next) != 0)
        {
            var str = Marshal.PtrToStringUni(_next);
            // skip first character because windows allows env vars to begin with equal sign
            var _splitPoint = str.IndexOf('=', 1);
            var _envVarName = str.Substring(0, _splitPoint);
            var _envVarVal = str.Substring(_splitPoint + 1);
            _envVars.Add(_envVarName, _envVarVal);
            _next = (IntPtr)((Int64)_next + (str.Length * 2) + 2);

            logger.LogInformation("@ApplicationLoaderHelper, environment var: {key}: {value}", _envVarName, _envVarVal);
        }
    }
}
