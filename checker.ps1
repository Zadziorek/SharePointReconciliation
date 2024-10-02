# Path to the log file
$logFilePath = "C:\path\to\your\logfile.log"

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

# Check if "Application error" is present in the last 1000 lines
$applicationErrorLines = $lastLines | Select-String -Pattern "Application error"

if ($applicationErrorLines) {
    # "Application error" found
    # Find the index of the last occurrence of "RAVN URL" in the last 1000 lines
    $lastRavnIndex = -1
    for ($i = $lastLines.Count - 1; $i -ge 0; $i--) {
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
        } else {
            $message = "$(Get-Date) - Failed to extract the number from the line."
        }
    } else {
        $message = "$(Get-Date) - No 'RAVN URL' line found in the last $lineCount lines."
    }
} else {
    # "Application error" not found
    $message = "$(Get-Date) - Everything is fine."
}

# Output the message
Write-Host $message

# Optionally, log the message to a file
# Uncomment the following line to enable logging
# Add-Content -Path $logOutputPath -Value $message
