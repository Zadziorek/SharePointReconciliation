# Import the PnP module
Import-Module SharePointPnPPowerShellOnline

# User inputs
$sharePointPath = Read-Host "Enter the SharePoint document library URL"
$localPath = Read-Host "Enter the local folder path"

# Connect to SharePoint
Connect-PnPOnline -Url $sharePointPath -UseWebLogin

# Get files from SharePoint
$sharePointFiles = Get-PnPFolderItem -FolderSiteRelativeUrl $sharePointPath -ItemType File

# Get files from the local drive
$localFiles = Get-ChildItem -Path $localPath -Recurse

# Compare files (by name, size, and last modified time)
foreach ($localFile in $localFiles) {
    $fileName = $localFile.Name
    $sharePointFile = $sharePointFiles | Where-Object { $_.Name -eq $fileName }

    if ($sharePointFile) {
        # Compare file size
        if ($sharePointFile.Length -ne $localFile.Length) {
            Write-Output "File size mismatch for: $fileName"
        }
        # Compare last modified date
        if ($sharePointFile.TimeLastModified -ne $localFile.LastWriteTime) {
            Write-Output "Last modified date mismatch for: $fileName"
        }
    } else {
        Write-Output "File missing in SharePoint: $fileName"
    }
}

# Log discrepancies
Write-Output "Reconciliation complete!"
