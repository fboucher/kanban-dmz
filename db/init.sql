CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS board (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    Name TEXT NOT NULL,
    IsPublic BOOLEAN NOT NULL DEFAULT true,
    BackgroundColor TEXT NULL
);

CREATE TABLE IF NOT EXISTS kanban_column (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    BoardId UUID NOT NULL REFERENCES board(Id) ON DELETE CASCADE,
    Name TEXT NOT NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    Color TEXT NULL
);

CREATE TABLE IF NOT EXISTS category (
    Id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE,
    Color TEXT NULL
);

CREATE TABLE IF NOT EXISTS card (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    BoardId UUID NOT NULL REFERENCES board(Id) ON DELETE CASCADE,
    ColumnId UUID NOT NULL REFERENCES kanban_column(Id) ON DELETE RESTRICT,
    Title TEXT NOT NULL,
    PublicDescription TEXT NOT NULL DEFAULT '',
    PrivateDescription TEXT NOT NULL DEFAULT '',
    CategoryId INT REFERENCES category(Id) ON DELETE RESTRICT,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CreatedBy TEXT NOT NULL DEFAULT '',
    AssignedTo TEXT NOT NULL DEFAULT '',
    IsPublic BOOLEAN NOT NULL DEFAULT true,
    ImageUrl TEXT NULL,
    Color TEXT NULL
);

CREATE TABLE IF NOT EXISTS tag (
    Name TEXT PRIMARY KEY,
    Color TEXT NOT NULL DEFAULT '#E0E0E0'
);

CREATE TABLE IF NOT EXISTS card_tag (
    CardId UUID NOT NULL REFERENCES card(Id) ON DELETE CASCADE,
    Tag TEXT NOT NULL REFERENCES tag(Name) ON DELETE CASCADE,
    PRIMARY KEY (CardId, Tag)
);

CREATE TABLE IF NOT EXISTS card_comment (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    CardId UUID NOT NULL REFERENCES card(Id) ON DELETE CASCADE,
    Content TEXT NOT NULL,
    CreatedBy TEXT NOT NULL,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);


-- Seed default board
INSERT INTO board (Id, Name, IsPublic)
VALUES ('00000000-0000-0000-0000-000000000001', 'Default', true)
ON CONFLICT (Id) DO NOTHING;

-- Seed default columns
INSERT INTO kanban_column (Id, BoardId, Name, SortOrder)
VALUES
    ('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'Backlog', 0),
    ('10000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'To Do', 1),
    ('10000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'In Progress', 2),
    ('10000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000001', 'Pending', 3),
    ('10000000-0000-0000-0000-000000000005', '00000000-0000-0000-0000-000000000001', 'Done', 4)
ON CONFLICT (Id) DO NOTHING;

-- Seed default categories
INSERT INTO category (Name, Color)
VALUES 
    ('Bug', '#e3008c'), 
    ('Feature', '#0078d4'), 
    ('Chore', '#008272')
ON CONFLICT (Name) DO NOTHING;

-- Seed default tags
INSERT INTO tag (Name, Color)
VALUES
    ('security', '#f8d7da'),
    ('refactor', '#d1ecf1'),
    ('frontend', '#d4edda'),
    ('ui', '#fff3cd'),
    ('backend', '#d1ecf1'),
    ('db', '#e2e3e5'),
    ('setup', '#cce5ff')
ON CONFLICT (Name) DO NOTHING;

-- Seed default cards
INSERT INTO card (Id, BoardId, ColumnId, Title, PublicDescription, PrivateDescription, CategoryId, CreatedBy, AssignedTo, IsPublic)
VALUES
    ('20000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'Refactor Authentication Flow', 'Migrate Keycloak authentication settings to use Aspire configuration instead of hardcoded values.', 'Requires access to keycloak settings in dev.', 3, 'Frank', 'Frank', true),
    ('20000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002', 'Implement Dashboard Widgets', '', 'Private details about metrics layout.', 2, 'Frank', '', true),
    ('20000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000003', 'Fix DAB Policy Error', 'Investigate and resolve the policy initialization crash in Data API Builder.', 'Ensure that IsPublic column is properly handled.', 1, 'Frank', 'Alice', true),
    ('20000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000005', 'Initial Solution Setup', 'Create ASP.NET Core Blazor application, domain libraries, and wire up PostgreSQL database container.', 'Done in slice 1.', 2, 'Frank', 'Bob', true)
ON CONFLICT (Id) DO NOTHING;

-- Seed default card tags
INSERT INTO card_tag (CardId, Tag)
VALUES
    ('20000000-0000-0000-0000-000000000001', 'security'),
    ('20000000-0000-0000-0000-000000000001', 'refactor'),
    ('20000000-0000-0000-0000-000000000002', 'frontend'),
    ('20000000-0000-0000-0000-000000000002', 'ui'),
    ('20000000-0000-0000-0000-000000000003', 'backend'),
    ('20000000-0000-0000-0000-000000000003', 'db'),
    ('20000000-0000-0000-0000-000000000004', 'setup')
ON CONFLICT (CardId, Tag) DO NOTHING;

