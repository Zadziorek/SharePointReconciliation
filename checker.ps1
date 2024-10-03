# Path to the log file
$logFilePath = "C:\path\to\your\logfile.log"

# Path to the documents list file
$documentsFilePath = "C:\path\to\your\documents.txt"

# Number of lines to consider
$lineCount = 1000

# Read the last 1000 lines from the log file
$lastLines = Get-Content -Path $logFilePath -Tail $lineCount

# Function to extract the data field
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

# Find the index of the last occurrence of "Application error"
$appErrorIndex = -1
for ($i = $lastLines.Count - 1; $i -ge 0; $i--) {
    if ($lastLines[$i] -match "Application error") {
        $appErrorIndex = $i
        break
    }
}

if ($appErrorIndex -ge 0) {
    # "Application error" found
    # Now search backwards from $appErrorIndex - 1 for "RAVN URL"
    $lastRavnIndex = -1
    for ($i = $appErrorIndex - 1; $i -ge 0; $i--) {
        if ($lastLines[$i] -match "RAVN URL") {
            $lastRavnIndex = $i
            break
        }
    }

    if ($lastRavnIndex -gt 0) {
        # Get the line before the "RAVN URL" line
        $lineOfInterest = $lastLines[$lastRavnIndex - 1]
        # Extract the data field
        $desiredDataField = Extract-DataField $lineOfInterest
        if ($desiredDataField) {
            $message = "$(Get-Date) - Extracted data field: $desiredDataField"

            # Backup the documents file
            Copy-Item -Path $documentsFilePath -Destination "${documentsFilePath}.bak" -Force

            # Read all lines from the documents file
            $allDocLines = Get-Content -Path $documentsFilePath

            # Initialize index variable
            $index = -1

            # Iterate through all lines to find the first occurrence of the desired data field
            for ($i = 0; $i -lt $allDocLines.Count; $i++) {
                $docLine = $allDocLines[$i]
                # Extract the data field from the documents line
                $docFields = $docLine -split '\s+'
                if ($docFields.Count -ge 3) {
                    $docDataField = $docFields[2]
                    if ($docDataField -eq $desiredDataField) {
                        $index = $i
                        break
                    }
                }
            }

            if ($index -ge 0) {
                # Get all lines from the index (including the matched line) to the end
                $remainingLines = $allDocLines[$index..($allDocLines.Count - 1)]
                # Write the remaining lines back to the file
                Set-Content -Path $documentsFilePath -Value $remainingLines

                $message += "`n$(Get-Date) - Updated documents file. Lines before the matched data field have been removed."
            } else {
                $message += "`n$(Get-Date) - The extracted data field was not found in the documents file."
            }
        } else {
            $message = "$(Get-Date) - Failed to extract the data field from the line."
        }
    } else {
        $message = "$(Get-Date) - No 'RAVN URL' line found before 'Application error' in the last $lineCount lines."
    }
} else {
    # "Application error" not found
    $message = "$(Get-Date) - Everything is fine."
}

# Output the message
Write-Host $message

# Optionally, log the message to a file
# $logOutputPath = "C:\path\to\your\output.log"
# Add-Content -Path $logOutputPath -Value $message
