# AppKeeper

### Tiny utillity-service to keep an application running.
Intended to be installed as a service on target-host.

Will look for current processes, and if target-application is not found, will attempt to restart it, while impersonating current user. <br>

The usecase is *only* intended for applications that interact directly with a user-session, ie. has a GUI.

### appSettings.json
One or more applications can be designated for monitoring. <br>
Remember to escape backslashes in `PathToExe` !

Section `CoreSettings` example:
```json
"CoreSettings": {
    "ApplicationList": [
      {
        "DisplayName": "DR BDF Crunch",
        "PathToExe": "D:\\OL2024\\CRUNCH\\App\\DR BDF Crunch.exe"
      }
    ],
    "RecheckStatusIntervalInSecs": 5,
    "TimeoutAfterKillInSecs": 5
  }
```


### Install Scripts
In folder **ServiceInstallScripts** are  powershell install/uninstall scripts for installing as service on host.

Notice: Use a plain PS window for executing the scripts. <br>
Do no execute scripts from PS ISE.<br>
ISE blindly assumes that target-files are located in `C:/Windows/System32`, as this is the home-directory of ISE.