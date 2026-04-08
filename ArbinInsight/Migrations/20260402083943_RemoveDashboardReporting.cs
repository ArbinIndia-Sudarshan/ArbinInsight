using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbinInsight.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDashboardReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardLimitRecords",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "DashboardSubTestRuns",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "DashboardTestRuns",
                schema: "reporting");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.CreateTable(
                name: "DashboardTestRuns",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BinNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChannelIndex = table.Column<int>(type: "int", nullable: true),
                    EndDateTimeRaw = table.Column<long>(type: "bigint", nullable: true),
                    EndDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublisherNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Retest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceConnectionName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceTestKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartDateTimeRaw = table.Column<long>(type: "bigint", nullable: true),
                    StartDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TestName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestProfileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestProjectName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardTestRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DashboardSubTestRuns",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DashboardTestRunId = table.Column<long>(type: "bigint", nullable: false),
                    Enable = table.Column<bool>(type: "bit", nullable: true),
                    EndDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParentSourceTestKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceConnectionName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceSubTestKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StopOnFail = table.Column<bool>(type: "bit", nullable: true),
                    TestName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestStatus = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardSubTestRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardSubTestRuns_DashboardTestRuns_DashboardTestRunId",
                        column: x => x.DashboardTestRunId,
                        principalSchema: "reporting",
                        principalTable: "DashboardTestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DashboardLimitRecords",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DashboardSubTestRunId = table.Column<long>(type: "bigint", nullable: true),
                    DashboardTestRunId = table.Column<long>(type: "bigint", nullable: true),
                    LastMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LimitName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxValue = table.Column<double>(type: "float", nullable: true),
                    MeasuredValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinValue = table.Column<double>(type: "float", nullable: true),
                    ParentSourceSubTestKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentSourceTestKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceConnectionName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceLimitKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Tolerance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardLimitRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardLimitRecords_DashboardSubTestRuns_DashboardSubTestRunId",
                        column: x => x.DashboardSubTestRunId,
                        principalSchema: "reporting",
                        principalTable: "DashboardSubTestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DashboardLimitRecords_DashboardTestRuns_DashboardTestRunId",
                        column: x => x.DashboardTestRunId,
                        principalSchema: "reporting",
                        principalTable: "DashboardTestRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardLimitRecords_DashboardSubTestRunId_LastSyncedAtUtc",
                schema: "reporting",
                table: "DashboardLimitRecords",
                columns: new[] { "DashboardSubTestRunId", "LastSyncedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardLimitRecords_DashboardTestRunId",
                schema: "reporting",
                table: "DashboardLimitRecords",
                column: "DashboardTestRunId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardLimitRecords_SourceConnectionName_SourceLimitKey",
                schema: "reporting",
                table: "DashboardLimitRecords",
                columns: new[] { "SourceConnectionName", "SourceLimitKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardSubTestRuns_DashboardTestRunId_LastSyncedAtUtc",
                schema: "reporting",
                table: "DashboardSubTestRuns",
                columns: new[] { "DashboardTestRunId", "LastSyncedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardSubTestRuns_SourceConnectionName_SourceSubTestKey",
                schema: "reporting",
                table: "DashboardSubTestRuns",
                columns: new[] { "SourceConnectionName", "SourceSubTestKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardTestRuns_PublisherNodeId_LastSyncedAtUtc",
                schema: "reporting",
                table: "DashboardTestRuns",
                columns: new[] { "PublisherNodeId", "LastSyncedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardTestRuns_SourceConnectionName_SourceTestKey",
                schema: "reporting",
                table: "DashboardTestRuns",
                columns: new[] { "SourceConnectionName", "SourceTestKey" },
                unique: true);
        }
    }
}
