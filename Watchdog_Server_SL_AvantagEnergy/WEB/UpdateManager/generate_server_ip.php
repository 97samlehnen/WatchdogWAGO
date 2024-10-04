<?php
$servername = "localhost";
$username = "root"; // Dein MySQL-Benutzername
$password = ""; // Dein MySQL-Passwort
$dbname = "server_db";

// Verbindung zur Datenbank herstellen
$conn = new mysqli($servername, $username, $password, $dbname);

// Verbindung überprüfen
if ($conn->connect_error) {
    die("Verbindung fehlgeschlagen: " . $conn->connect_error);
}

// SQL-Abfrage zur Auswahl der ServerIP
$sql = "SELECT ServerIP FROM server_info LIMIT 1";
$result = $conn->query($sql);

if ($result->num_rows > 0) {
    // Daten aus der Abfrage holen
    $row = $result->fetch_assoc();
    $serverIP = $row["ServerIP"];

    // server_ip.txt Datei erstellen und ServerIP schreiben
    $file = fopen("server_ip.txt", "w");
    fwrite($file, $serverIP);
    fclose($file);

    echo "server_ip.txt erfolgreich erstellt mit IP: " . $serverIP;
} else {
    echo "Keine ServerIP in der Datenbank gefunden.";
}

$conn->close();
?>
