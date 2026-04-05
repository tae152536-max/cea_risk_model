-- =============================================
-- Seed reference data — run once after deploy
-- =============================================

-- Areas
INSERT INTO [dbo].[Areas] ([AreaName], [Region]) VALUES
    ('Cairo North',    'Cairo'),
    ('Cairo South',    'Cairo'),
    ('Alexandria',     'Alexandria'),
    ('Giza',           'Giza'),
    ('Delta',          'Delta'),
    ('Upper Egypt',    'Upper Egypt'),
    ('Canal Zone',     'Canal');

-- Products
INSERT INTO [dbo].[Products] ([ProductName], [Class]) VALUES
    ('Xaralto',  'Anticoagulant'),
    ('Vissane',  'Contraceptive'),
    ('Arcoxia',  'NSAID');
