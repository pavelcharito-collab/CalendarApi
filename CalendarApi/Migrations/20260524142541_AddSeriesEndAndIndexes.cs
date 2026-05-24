using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalendarApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSeriesEndAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SeriesEnd",
                table: "CalendarEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.Sql(
                """
                UPDATE CalendarEvents SET SeriesEnd = "End" WHERE Recurrence_Frequency IS NULL;
                UPDATE CalendarEvents SET SeriesEnd = Recurrence_Until WHERE Recurrence_Until IS NOT NULL;
                UPDATE CalendarEvents SET SeriesEnd = datetime(Start, '+2 years')
                WHERE Recurrence_Frequency IS NOT NULL AND Recurrence_Until IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_OwnerId",
                table: "CalendarEvents",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_SeriesEnd",
                table: "CalendarEvents",
                column: "SeriesEnd");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_Start",
                table: "CalendarEvents",
                column: "Start");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvents_Users_OwnerId",
                table: "CalendarEvents",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEvents_Users_OwnerId",
                table: "CalendarEvents");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_OwnerId",
                table: "CalendarEvents");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_SeriesEnd",
                table: "CalendarEvents");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_Start",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "SeriesEnd",
                table: "CalendarEvents");
        }
    }
}
