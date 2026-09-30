-- Création du schéma dédié au module Drones (isolation modulaire)
CREATE SCHEMA IF NOT EXISTS drones;

-- Table des drones
CREATE TABLE IF NOT EXISTS drones.drones (
    id UUID PRIMARY KEY,
    client_id VARCHAR(50) NOT NULL DEFAULT 'client-demo',
    imsi VARCHAR(15) NOT NULL UNIQUE,
    modele VARCHAR(100) NOT NULL,
    statut VARCHAR(30) NOT NULL,
    date_enregistrement TIMESTAMPTZ NOT NULL
);

-- Index pour optimiser la recherche par IMSI (déjà garanti unique)
CREATE INDEX IF NOT EXISTS idx_drones_imsi ON drones.drones(imsi);