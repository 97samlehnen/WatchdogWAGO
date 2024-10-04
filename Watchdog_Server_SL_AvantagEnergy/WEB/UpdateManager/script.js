document.getElementById('generateBtn').addEventListener('click', function() {
    fetch('generate_server_ip.php')
        .then(response => response.text())
        .then(data => {
            document.getElementById('statusMessage').innerText = data;
        })
        .catch(error => {
            document.getElementById('statusMessage').innerText = 'Fehler: ' + error;
        });
});
