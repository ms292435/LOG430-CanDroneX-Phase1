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

-- Drone de démonstration du cahier des charges DRN-0231 (§1.4, §7.2)
INSERT INTO drones.drone (id, client_id, imsi, type_carte, modele, statut, date_enregistrement)
VALUES (
    '00000000-0000-0000-0000-000000000231',
    'client-demo',
    '123456789012345',
    'SIM',
    'DJI Matrice 300 (DRN-0231)',
    'Enregistre',
    NOW()
)
ON CONFLICT (id) DO NOTHING;

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

-- ============================================================================
-- Module Commandes (§8.1)
-- ============================================================================
CREATE SCHEMA IF NOT EXISTS commandes;

-- Table des commandes
CREATE TABLE IF NOT EXISTS commandes.commande (
    id UUID PRIMARY KEY,
    client_id VARCHAR(50) NOT NULL,
    drone_id UUID NOT NULL,
    cle_idempotence VARCHAR(100) NOT NULL,
    empreinte_requete VARCHAR(64) NOT NULL,
    date_creation TIMESTAMPTZ NOT NULL,
    CONSTRAINT uq_commande_idempotence UNIQUE (client_id, cle_idempotence)
);

-- Table des éléments de commande
CREATE TABLE IF NOT EXISTS commandes.element_commande (
    id UUID PRIMARY KEY,
    commande_id UUID NOT NULL REFERENCES commandes.commande(id) ON DELETE CASCADE,
    type_service VARCHAR(50) NOT NULL,
    etat VARCHAR(30) NOT NULL,
    cause_echec TEXT NULL,
    CONSTRAINT uq_commande_service UNIQUE (commande_id, type_service),
    CONSTRAINT chk_element_etat CHECK (etat IN ('EN_ATTENTE', 'EN_ACTIVATION', 'ACTIF', 'EN_ECHEC', 'ANNULE', 'COMPENSE'))
);

CREATE INDEX IF NOT EXISTS idx_commande_client_id ON commandes.commande(client_id);
CREATE INDEX IF NOT EXISTS idx_commande_drone_id ON commandes.commande(drone_id);
CREATE INDEX IF NOT EXISTS idx_element_commande_id ON commandes.element_commande(commande_id);