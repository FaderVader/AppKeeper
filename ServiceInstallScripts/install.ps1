#Requires -RunAsAdministrator

param(
        [Parameter(Mandatory=$false)][string]$serviceName = "AppKeeper",
        [Parameter(Mandatory=$false)][string]$serviceDisplayName = "AppKeeper",
        [Parameter(Mandatory=$false)][string]$serviceDescription = "Restart targetapplication in current users session",
        [Parameter(Mandatory=$false)][string]$binaryPath
    )

if(!$binaryPath){
    $location = Get-Location
    $binaryPath = Join-Path -Path $location -ChildPath "AppKeeperService.exe"
}

$installConfirmation = Read-Host "Are you sure you want to install service $($serviceName) from binary $($binaryPath)? [y/n]"
if ($installConfirmation -eq 'y') {
    "installing service " + $serviceName + " from binary " + $binaryPath

    New-Service -name $serviceName -binaryPathName $binaryPath -displayName $serviceDisplayName -Description $serviceDescription -startupType Automatic

    "installation completed"
}

$startConfirmation = Read-Host "Are you sure you want to start service $($serviceName)? [y/n]"
if ($startConfirmation -eq 'y') {
    Start-Service -Name $serviceName

    Get-Service -Name $serviceName
}