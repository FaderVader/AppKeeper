#Requires -RunAsAdministrator

param(
        [Parameter(Mandatory=$false)][string]$serviceName = "OBS.BDF.Manager.Subscriber"
    )

$deleteConfirmation = Read-Host "Are you sure you want to delete service $($serviceName)? [y/n]"
if ($deleteConfirmation -eq 'y') {
    $filter = "Name='" + $serviceName + "'"
    $service = Get-WmiObject -Class Win32_Service -Filter $filter
    $service.delete()
    
    "uninstallation completed"
}

# Remove-Service -Name $serviceName -Confirm