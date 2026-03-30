using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbinInsight.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedSyncAndHistoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelStatusHistory");

            migrationBuilder.DropTable(
                name: "MachineHeartbeats",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "MachineStatusHistory");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "sync");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelStatusHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    AmbientTemperature = table.Column<float>(type: "real", nullable: true),
                    BINNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BarCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChannelIndex = table.Column<long>(type: "bigint", nullable: false),
                    ChannelStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRunning = table.Column<bool>(type: "bit", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublisherNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SnapshotAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelStatusHistory_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MachineHeartbeats",
                schema: "sync",
                columns: table => new
                {
                    HeartbeatId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublisherNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueDepth = table.Column<int>(type: "int", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineHeartbeats", x => x.HeartbeatId);
                });

            migrationBuilder.CreateTable(
                name: "MachineStatusHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineDataId = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<double>(type: "float", nullable: true),
                    MachineStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublisherNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineStatusHistory_MachineDatas_MachineDataId",
                        column: x => x.MachineDataId,
                        principalTable: "MachineDatas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "sync",
                columns: table => new
                {
                    OutboxId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AggregateKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AggregateType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NextRetryAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublisherNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RoutingKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.OutboxId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelStatusHistory_ChannelId_SnapshotAtUtc",
                table: "ChannelStatusHistory",
                columns: new[] { "ChannelId", "SnapshotAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelStatusHistory_PublisherNodeId_ChannelIndex_SnapshotAtUtc",
                table: "ChannelStatusHistory",
                columns: new[] { "PublisherNodeId", "ChannelIndex", "SnapshotAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MachineStatusHistory_MachineDataId_SnapshotAtUtc",
                table: "MachineStatusHistory",
                columns: new[] { "MachineDataId", "SnapshotAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_MessageId",
                schema: "sync",
                table: "OutboxMessages",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status_NextRetryAtUtc",
                schema: "sync",
                table: "OutboxMessages",
                columns: new[] { "Status", "NextRetryAtUtc" });
        }
    }
}
