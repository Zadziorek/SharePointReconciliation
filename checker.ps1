# Path to the log file
$logFilePath = "C:\path\to\your\logfile.log"

# Path to the documents list file (PRIO0.txt)
$documentsFilePath = "C:\path\to\your\PRIO0.txt"

# Number of lines to consider from the log file
$lineCount = 1000  # Adjust as necessary

# Function to extract the data field from the log line
function Extract-DataField {
    param($line)
    # Split the line by spaces
    $fields = $line -split '\s+'
    if ($fields.Count -ge 3) {
        # The third field contains the data field
        $dataField = $fields[2]
        return $dataField
    } else {
        return $null
    }
}

# Read the last $lineCount lines from the log file
$lastLines = Get-Content -Path $logFilePath -Tail $lineCount

# Initialize variables
$desiredDataField = $null
$dataTransferredFound = $false

# Process the log file to find "Data Transferred" and then "RAVN URL" after it
for ($i = 0; $i -lt $lastLines.Count; $i++) {
    $line = $lastLines[$i]

    if (-not $dataTransferredFound -and $line -match "Data Transferred") {
        # Found "Data Transferred"
        $dataTransferredIndex = $i
        $dataTransferredFound = $true
        continue
    }

    if ($dataTransferredFound -and $line -match "RAVN URL") {
        # Found "RAVN URL" after "Data Transferred"
        if ($i -gt 0) {
            # Get the line before "RAVN URL"
            $lineOfInterest = $lastLines[$i - 1]
            $desiredDataField = Extract-DataField $lineOfInterest
            break
        }
    }
}

if ($desiredDataField) {
    $message = "$(Get-Date) - Extracted data field: $desiredDataField"

    # Backup the documents file
    Copy-Item -Path $documentsFilePath -Destination "${documentsFilePath}.bak" -Force

    # Open the documents file for reading and writing line by line
    $found = $false
    $tempFilePath = "${documentsFilePath}.tmp"

    # Use FileStreams for efficient file I/O
    $docFileReader = [System.IO.StreamReader]::new($documentsFilePath)
    $docFileWriter = [System.IO.StreamWriter]::new($tempFilePath, $false)

    try {
        while (($docLine = $docFileReader.ReadLine()) -ne $null) {
            if (-not $found) {
                # Compare the entire line directly to the desired data field
                if ($docLine -eq $desiredDataField) {
                    $found = $true
                    # Write the matched line to the temp file
                    $docFileWriter.WriteLine($docLine)
                    continue
                }
                # Do not write lines before the match
            } else {
                # After the match has been found, write the remaining lines
                $docFileWriter.WriteLine($docLine)
            }
        }
    } finally {
        $docFileReader.Close()
        $docFileWriter.Close()
    }

    if ($found) {
        # Replace the original documents file with the temp file
        Move-Item -Path $tempFilePath -Destination $documentsFilePath -Force
        $message += "`n$(Get-Date) - Updated documents file. Lines before the matched data field have been removed."
    } else {
        # If the data field was not found, delete the temp file
        Remove-Item -Path $tempFilePath -Force
        $message += "`n$(Get-Date) - The extracted data field was not found in the documents file."
    }
} else {
    $message = "$(Get-Date) - Failed to extract the data field from the log file."
}

# Output the message
Write-Host $message

# Optionally, log the message to a file
# $logOutputPath = "C:\path\to\your\output.log"
# Add-Content -Path $logOutputPath -Value $message
