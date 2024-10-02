# Path to the log file
$logFilePath = "C:\path\to\your\logfile.log"

# Path to the documents list file
$documentsFilePath = "C:\path\to\your\documents.txt"

# Path to the output log file (optional)
$logOutputPath = "C:\path\to\your\output.log"

# Number of lines to consider
$lineCount = 1000

# Read the last 1000 lines from the log file
$lastLines = Get-Content -Path $logFilePath -Tail $lineCount

# Function to extract the desired number
function Extract-Number {
    param($line)
    # Split the line by spaces
    $fields = $line -split '\s+'
    if ($fields.Count -ge 3) {
        # The third field contains the number and other data
        $numberField = $fields[2]
        # Split by semicolons
        $subFields = $numberField -split ';'
        # The first element is the desired number
        return $subFields[0]
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
        # Extract the number
        $desiredNumber = Extract-Number $lineOfInterest
        if ($desiredNumber) {
            $message = "$(Get-Date) - Extracted number: $desiredNumber"

            # Backup the documents file
            Copy-Item -Path $documentsFilePath -Destination "${documentsFilePath}.bak" -Force

            # Read all lines from the documents file
            $allLines = Get-Content -Path $documentsFilePath

            # Find the index of the line that contains the extracted number
            $index = $allLines.IndexOf($desiredNumber)

            if ($index -ge 0) {
                # Get all lines from the index (including the line with the extracted number) to the end
                $remainingLines = $allLines[$index..($allLines.Count - 1)]

                # Write the remaining lines back to the file
                Set-Content -Path $documentsFilePath -Value $remainingLines

                $message += "`n$(Get-Date) - Updated documents file. Lines before '$desiredNumber' have been removed."

                # Optionally, log the changes
                $logMessage = "$(Get-Date) - Removed lines before '$desiredNumber' from documents.txt"
                Add-Content -Path $logOutputPath -Value $logMessage
            } else {
                $message += "`n$(Get-Date) - The extracted number '$desiredNumber' was not found in the documents file."
            }
        } else {
            $message = "$(Get-Date) - Failed to extract the number from the line."
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
Add-Content -Path $logOutputPath -Value $message
