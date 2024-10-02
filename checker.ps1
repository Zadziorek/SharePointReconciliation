# Path to the log file
$logFilePath = "C:\path\to\your\logfile.log"

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

# Monitor the log file for changes
Get-Content -Path $logFilePath -Wait | ForEach-Object {
    $line = $_

    # Check for "Application error"
    if ($line -match "Application error") {
        # Read all lines up to the current point
        $allLines = Get-Content -Path $logFilePath

        # Find the index of the last occurrence of "RAVN URL"
        $lastRavnIndex = -1
        for ($i = $allLines.Count - 1; $i -ge 0; $i--) {
            if ($allLines[$i] -match "RAVN URL") {
                $lastRavnIndex = $i
                break
            }
        }

        if ($lastRavnIndex -gt 0) {
            # Get the line before the "RAVN URL" line
            $lineOfInterest = $allLines[$lastRavnIndex - 1]
            # Extract the number
            $desiredNumber = Extract-Number $lineOfInterest
            if ($desiredNumber) {
                Write-Host "Extracted number: $desiredNumber"
            } else {
                Write-Host "Failed to extract the number from the line."
            }
        } else {
            Write-Host "No 'RAVN URL' line found before 'Application error'."
        }
    }
}
