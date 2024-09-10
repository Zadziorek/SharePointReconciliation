<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>File Reader</title>
</head>
<body>

    <h2>Select Files for Comparison</h2>
    <form id="fileForm">
        <input type="file" id="fileInput" multiple />
        <br/><br/>
        <button type="button" onclick="processFiles()">Read Files</button>
    </form>

    <div id="fileContents">
        <!-- File content will be displayed here -->
    </div>

    <script>
        function processFiles() {
            const fileInput = document.getElementById('fileInput');
            const fileContentsDiv = document.getElementById('fileContents');
            fileContentsDiv.innerHTML = '';  // Clear previous contents

            const files = fileInput.files;  // Get the selected files

            if (files.length > 0) {
                for (let i = 0; i < files.length; i++) {
                    const file = files[i];
                    const reader = new FileReader();

                    reader.onload = function (e) {
                        const fileContent = e.target.result;
                        fileContentsDiv.innerHTML += `<h3>${file.name}</h3><pre>${fileContent}</pre><hr>`;
                    };

                    // Read the file as text (you can also read it as DataURL, binary, etc.)
                    reader.readAsText(file);
                }
            } else {
                alert("No files selected!");
            }
        }
    </script>

</body>
</html>
