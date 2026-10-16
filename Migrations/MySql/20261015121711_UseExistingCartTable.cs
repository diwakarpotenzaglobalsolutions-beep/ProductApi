using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMvcApp.Migrations.MySql
{
    /// <inheritdoc />
    public partial class UseExistingCartTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "cartDBs",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "cartDBs",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_cartDBs_ProductId",
                table: "cartDBs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_cartDBs_UserId_ProductId",
                table: "cartDBs",
                columns: new[] { "UserId", "ProductId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_cartDBs_products_ProductId",
                table: "cartDBs",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            // Transfer every new-cart row before removing the extra table. Old cartDBs
            // rows keep their IDs and saved details; an ID collision gets a new cart ID.
            migrationBuilder.Sql("""
                INSERT INTO `cartDBs`
                    (`ID`, `UserId`, `ProductId`, `Quantity`, `ProductName`, `Price`, `Desc`, `CreatedAt`, `UpdatedAt`)
                SELECT CASE WHEN existing.`ID` IS NULL THEN source.`ID` ELSE UUID() END,
                    source.`UserId`, source.`ProductId`, source.`Quantity`,
                    product.`ProductName`, product.`Price`, product.`Desc`, source.`CreatedAt`, source.`UpdatedAt`
                FROM `cartItems` AS source
                INNER JOIN `products` AS product ON product.`ID` = source.`ProductId`
                LEFT JOIN `cartDBs` AS existing ON existing.`ID` = source.`ID`;
                """);

            migrationBuilder.DropTable(name: "cartItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cartItems",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProductId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cartItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_cartItems_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cartItems_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_cartItems_ProductId",
                table: "cartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_cartItems_UserId_ProductId",
                table: "cartItems",
                columns: new[] { "UserId", "ProductId" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO `cartItems` (`ID`, `UserId`, `ProductId`, `Quantity`, `CreatedAt`, `UpdatedAt`)
                SELECT `ID`, `UserId`, `ProductId`, `Quantity`, `CreatedAt`, `UpdatedAt`
                FROM `cartDBs` WHERE `ProductId` IS NOT NULL;
                """);
            // Linked rows now live in cartItems again; retain all original unlinked rows.
            migrationBuilder.Sql("DELETE FROM `cartDBs` WHERE `ProductId` IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_cartDBs_products_ProductId",
                table: "cartDBs");

            migrationBuilder.DropIndex(
                name: "IX_cartDBs_ProductId",
                table: "cartDBs");

            migrationBuilder.DropIndex(
                name: "IX_cartDBs_UserId_ProductId",
                table: "cartDBs");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "cartDBs");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "cartDBs");

        }
    }
}
