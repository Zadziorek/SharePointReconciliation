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
$appErrorIndex = -1
$dataTransferredIndex = -1

# Step 1: Find "Application error"
for ($i = $lastLines.Count - 1; $i -ge 0; $i--) {
    if ($lastLines[$i] -match "Application error") {
        $appErrorIndex = $i
        break
    }
}

if ($appErrorIndex -ge 0) {
    # Step 2: Find "Data Transferred" before "Application error"
    for ($i = $appErrorIndex - 1; $i -ge 0; $i--) {
        if ($lastLines[$i] -match "Data Transferred") {
            $dataTransferredIndex = $i
            break
        }
    }

    if ($dataTransferredIndex -ge 0) {
        # Step 3: From "Data Transferred" to "Application error", find "RAVN URL"
        $ravnUrlIndex = -1
        for ($i = $dataTransferredIndex + 1; $i -lt $appErrorIndex; $i++) {
            if ($lastLines[$i] -match "RAVN URL") {
                $ravnUrlIndex = $i
                break
            }
        }

        if ($ravnUrlIndex -gt 0) {
            # Step 4: Get the line before "RAVN URL"
            $lineOfInterest = $lastLines[$ravnUrlIndex - 1]
            $desiredDataField = Extract-DataField $lineOfInterest
            if ($desiredDataField) {
                $message = "$(Get-Date) - Extracted data field: $desiredDataField"

                # Process the documents file as before
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
                $message = "$(Get-Date) - Failed to extract the data field from the line."
            }
        } else {
            $message = "$(Get-Date) - 'RAVN URL' not found between 'Data Transferred' and 'Application error'."
        }
    } else {
        $message = "$(Get-Date) - 'Data Transferred' not found before 'Application error'."
    }
} else {
    $message = "$(Get-Date) - 'Application error' not found in the last $lineCount lines."
}

# Output the message
Write-Host $message

# Optionally, log the message to a file
# $logOutputPath = "C:\path\to\your\output.log"
# Add-Content -Path $logOutputPath -Value $message
