DROP SCHEMA IF EXISTS drones CASCADE;

-- Création du schéma dédié au module Drones (isolation modulaire)
CREATE SCHEMA IF NOT EXISTS drones;

-- Table des drones (§8.1)
CREATE TABLE IF NOT EXISTS drones.drone (
    id UUID PRIMARY KEY,
    client_id VARCHAR(50) NOT NULL,
    imsi VARCHAR(15) NOT NULL UNIQUE,
    type_carte VARCHAR(10) NOT NULL,
    modele VARCHAR(100) NOT NULL,
    statut VARCHAR(30) NOT NULL,
    date_enregistrement TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_drone_imsi CHECK (imsi ~ '^[0-9]{14,15}$'),
    CONSTRAINT chk_drone_type_carte CHECK (type_carte IN ('SIM', 'eSIM', 'Sim', 'ESim')),
    CONSTRAINT chk_drone_statut CHECK (statut IN ('Enregistre', 'Actif', 'Desactive'))
);

-- Index pour optimiser les recherches par IMSI et par client
CREATE INDEX IF NOT EXISTS idx_drone_imsi ON drones.drone(imsi);
CREATE INDEX IF NOT EXISTS idx_drone_client_id ON drones.drone(client_id);

-- Vue de compatibilité (si d'anciens scripts référencent drones.drones)
CREATE OR REPLACE VIEW drones.drones AS 
SELECT id, client_id, imsi, type_carte, modele, statut, date_enregistrement 
FROM drones.drone;