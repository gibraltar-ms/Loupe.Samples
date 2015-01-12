﻿--This is a SQL script generated by VistaDB's DataBuilder tool
--Any alterations to this script can cause errors durring execution
--Script Generated on: 1/11/2015 1:32:16 PM

CREATE TABLE [Session] ([Id] Int IDENTITY(96,1) NOT NULL,[SessionId] UniqueIdentifier NOT NULL,[SessionDate] Datetime NOT NULL);
CREATE TABLE [Username] ([Id] Int IDENTITY(68,1) NOT NULL,[Username] VarChar(100) NOT NULL);
CREATE TABLE [UserSession] ([Session_FK] Int NOT NULL,[User_FK] Int NOT NULL);
ALTER TABLE [Session] ADD CONSTRAINT [PK_Session] PRIMARY KEY ([Id]);
ALTER TABLE [Username] ADD CONSTRAINT [PK_User] PRIMARY KEY ([Id]);
ALTER TABLE [UserSession] ADD CONSTRAINT [PK_UserSession] PRIMARY KEY ([Session_FK],[User_FK]);
ALTER TABLE [UserSession] ADD CONSTRAINT [FK_UserSession_Session] FOREIGN KEY ([Session_FK]) REFERENCES [Session]([Id]) ON DELETE CASCADE;
ALTER TABLE [UserSession] ADD CONSTRAINT [FK_UserSession_User] FOREIGN KEY ([User_FK]) REFERENCES [Username]([Id]) ON DELETE CASCADE;
CREATE UNIQUE INDEX [IX_Session_SessionDate_SessionId] ON [Session]([SessionDate],[SessionId]);
CREATE UNIQUE INDEX [UK_User_Username] ON [Username]([Username]);

