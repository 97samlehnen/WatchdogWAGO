-- Erstellen der Datenbank
CREATE DATABASE IF NOT EXISTS server_db;

-- Verwenden der erstellten Datenbank
USE server_db;

-- Erstellen der Tabelle server_info
CREATE TABLE IF NOT EXISTS server_info (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ServerIP VARCHAR(15) NOT NULL
);

-- Einfügen des Eintrags ServerIP
INSERT INTO server_info (ServerIP) VALUES ('127.0.0.1');
