using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookify.Data.Migrations
{
    public partial class AddContactReply : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tạo bảng nếu chưa có (idempotent)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ContactMessages' AND xtype='U')
                BEGIN
                    CREATE TABLE [ContactMessages] (
                        [Id] int NOT NULL IDENTITY,
                        [FullName] nvarchar(100) NOT NULL,
                        [Email] nvarchar(150) NOT NULL,
                        [Message] nvarchar(1000) NOT NULL,
                        [UserId] nvarchar(max) NULL,
                        [SentAt] datetime2 NOT NULL,
                        [IsRead] bit NOT NULL,
                        [AdminReply] nvarchar(max) NULL,
                        [RepliedAt] datetime2 NULL,
                        CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
                    );
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id=OBJECT_ID('ContactMessages') AND name='AdminReply')
                        ALTER TABLE [ContactMessages] ADD [AdminReply] nvarchar(max) NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id=OBJECT_ID('ContactMessages') AND name='RepliedAt')
                        ALTER TABLE [ContactMessages] ADD [RepliedAt] datetime2 NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ContactMessages");
        }
    }
}
