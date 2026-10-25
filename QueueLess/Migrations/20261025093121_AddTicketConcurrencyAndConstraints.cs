using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueLess.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketConcurrencyAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Queues_QueueId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_QueueId",
                table: "Tickets");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Tickets",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_QueueId",
                table: "Tickets",
                column: "QueueId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_QueueId_TicketNumber",
                table: "Tickets",
                columns: new[] { "QueueId", "TicketNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Queues_QueueId",
                table: "Tickets",
                column: "QueueId",
                principalTable: "Queues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Queues_QueueId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_QueueId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_QueueId_TicketNumber",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Tickets");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_QueueId",
                table: "Tickets",
                column: "QueueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Queues_QueueId",
                table: "Tickets",
                column: "QueueId",
                principalTable: "Queues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
