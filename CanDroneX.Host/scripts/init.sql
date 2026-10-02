-- ============================================================================
-- Module Drones (§8.1)
-- ============================================================================
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

-- ============================================================================
-- Module Catalogue (§8.1) - Données de démonstration Phase 1
-- ============================================================================
CREATE SCHEMA IF NOT EXISTS catalogue;

CREATE TABLE IF NOT EXISTS catalogue.offre_service (
    type_service VARCHAR(50) PRIMARY KEY,
    libelle VARCHAR(100) NOT NULL,
    profil_reseau VARCHAR(100) NOT NULL
);

-- Insertion des offres de démonstration du cahier des charges (§3.1, §7.2)
INSERT INTO catalogue.offre_service (type_service, libelle, profil_reseau)
VALUES 
    ('C2_URLLC', 'Commande et Contrôle (C2)', 'URLLC - Faible latence, haute fiabilité, priorité élevée'),
    ('IMAGERIE_EMBB', 'Flux vidéo et imagerie mission', 'eMBB - Haut débit montant, latence moins critique')
ON CONFLICT (type_service) DO UPDATE 
SET libelle = EXCLUDED.libelle, profil_reseau = EXCLUDED.profil_reseau;