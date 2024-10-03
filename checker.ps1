# Path to the log file
$logFilePath = "C:\path\to\your\logfile.log"

# Path to the documents list file
$documentsFilePath = "C:\path\to\your\documents.txt"

# Number of lines to consider
$lineCount = 1000

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

# Read the last $lineCount lines from the log file efficiently
$lastLines = Get-Content -Path $logFilePath -Tail $lineCount

# Initialize variables
$desiredDataField = $null
$appErrorFound = $false

# Process the log file to extract the desired data field
for ($i = $lastLines.Count - 1; $i -ge 0; $i--) {
    $line = $lastLines[$i]

    if (-not $appErrorFound -and $line -match "Application error") {
        # Found "Application error"
        $appErrorIndex = $i
        $appErrorFound = $true
        continue
    }

    if ($appErrorFound -and $line -match "RAVN URL") {
        # Found "RAVN URL" before "Application error"
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
                # Extract the data field from the documents line
                $docFields = $docLine -split '\s+'
                if ($docFields.Count -ge 3) {
                    $docDataField = $docFields[2]
                    if ($docDataField -eq $desiredDataField) {
                        $found = $true
                        # Write the matched line to the temp file
                        $docFileWriter.WriteLine($docLine)
                        continue
                    }
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


# Path to the large input file
$inputFilePath = "C:\path\to\your\PRIO0.txt"

# Number of parts to split into
$parts = 10

# Output file name prefix
$outputFilePrefix = "PRIO0_Part"

# Output directory (ensure this directory exists)
$outputDirectory = "C:\path\to\your\output\directory"

# Read the total number of lines in the input file efficiently
$lineCount = 0
$reader = [System.IO.StreamReader]::new($inputFilePath)
try {
    while ($null -ne $reader.ReadLine()) {
        $lineCount++
    }
} finally {
    $reader.Close()
}

# Calculate the number of lines per part
$linesPerPart = [Math]::Ceiling($lineCount / $parts)

# Initialize variables
$currentPart = 1
$currentLine = 0
$reader = [System.IO.StreamReader]::new($inputFilePath)

try {
    while (-not $reader.EndOfStream -and $currentPart -le $parts) {
        # Create a new output file for each part
        $outputFilePath = Join-Path -Path $outputDirectory -ChildPath "$outputFilePrefix$currentPart.txt"
        $writer = [System.IO.StreamWriter]::new($outputFilePath, $false)
        try {
            # Write lines to the output file
            for ($i = 1; $i -le $linesPerPart; $i++) {
                if (-not $reader.EndOfStream) {
                    $line = $reader.ReadLine()
                    $writer.WriteLine($line)
                    $currentLine++
                } else {
                    break
                }
            }
        } finally {
            $writer.Close()
        }
        Write-Host "Created file: $outputFilePath with $i lines"
        $currentPart++
    }
} finally {
    $reader.Close()
}

Write-Host "Splitting complete. Total lines processed: $currentLine"

