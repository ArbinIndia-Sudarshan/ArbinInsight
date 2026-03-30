using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbinInsight.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncAndSourceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channels_TestProfiles_testProfileId",
                table: "Channels");

            migrationBuilder.DropIndex(
                name: "IX_TestList_Table_MachineDataId",
                table: "TestList_Table");

            migrationBuilder.DropIndex(
                name: "IX_Channels_MachineDataId",
                table: "Channels");

            migrationBuilder.EnsureSchema(
                name: "sync");

            migrationBuilder.RenameColumn(
                name: "testProfileId",
                table: "Channels",
                newName: "TestProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Channels_testProfileId",
                table: "Channels",
                newName: "IX_Channels_TestProfileId");

            migrationBuilder.AddColumn<DateTime>(
                name: "BrokerReceivedAtUtc",
                table: "TestProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastMessageId",
                table: "TestProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublisherNodeId",
                table: "TestProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceLocalId",
                table: "TestProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BrokerReceivedAtUtc",
                table: "TestList_Table",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChannelId",
                table: "TestList_Table",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastMessageId",
                table: "TestList_Table",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublisherNodeId",
                table: "TestList_Table",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceLocalId",
                table: "TestList_Table",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestProfileId",
                table: "TestList_Table",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BrokerReceivedAtUtc",
                table: "Test",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChannelId",
                table: "Test",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastMessageId",
                table: "Test",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublisherNodeId",
                table: "Test",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceLocalId",
                table: "Test",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BrokerReceivedAtUtc",
                table: "SMBMessagePair",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastMessageId",
                table: "SMBMessagePair",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublisherNodeId",
                table: "SMBMessagePair",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceLocalId",
                table: "SMBMessagePair",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BrokerReceivedAtUtc",
                table: "MachineDatas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastMessageId",
                table: "MachineDatas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublisherNodeId",
                table: "MachineDatas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceLocalId",
                table: "MachineDatas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BrokerReceivedAtUtc",
                table: "Limit",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastMessageId",
                table: "Limit",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublisherNodeId",
                table: "Limit",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceLocalId",
                table: "Limit",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BrokerReceivedAtUtc",
                table: "Channels",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastMessageId",
                table: "Channels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublisherNodeId",
                table: "Channels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceLocalId",
                table: "Channels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BrokerReceivedAtUtc",
                table: "CANMessagePairs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastMessageId",
                table: "CANMessagePairs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublisherNodeId",
                table: "CANMessagePairs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceLocalId",
                table: "CANMessagePairs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChannelStatusHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    PublisherNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelIndex = table.Column<long>(type: "bigint", nullable: false),
                    AmbientTemperature = table.Column<float>(type: "real", nullable: true),
                    ChannelStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRunning = table.Column<bool>(type: "bit", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BarCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BINNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SnapshotAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "DeadLetterMessages",
                schema: "sync",
                columns: table => new
                {
                    DeadLetterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublisherNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoutingKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ErrorText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadLetterMessages", x => x.DeadLetterId);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "sync",
                columns: table => new
                {
                    InboxId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublisherNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoutingKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    ErrorText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.InboxId);
                });

            migrationBuilder.CreateTable(
                name: "MachineHeartbeats",
                schema: "sync",
                columns: table => new
                {
                    HeartbeatId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublisherNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MachineStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QueueDepth = table.Column<int>(type: "int", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
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
                    PublisherNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MachineStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<double>(type: "float", nullable: true),
                    SnapshotAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublisherNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoutingKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AggregateKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NextRetryAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "PublisherNodes",
                schema: "sync",
                columns: table => new
                {
                    PublisherNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HostName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastHeartbeatUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublisherNodes", x => x.PublisherNodeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestProfiles_PublisherNodeId_SourceLocalId",
                table: "TestProfiles",
                columns: new[] { "PublisherNodeId", "SourceLocalId" },
                unique: true,
                filter: "[PublisherNodeId] IS NOT NULL AND [SourceLocalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TestList_Table_ChannelId",
                table: "TestList_Table",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_TestList_Table_MachineDataId_ChannelId_Start_Date_Time",
                table: "TestList_Table",
                columns: new[] { "MachineDataId", "ChannelId", "Start_Date_Time" });

            migrationBuilder.CreateIndex(
                name: "IX_TestList_Table_PublisherNodeId_SourceLocalId",
                table: "TestList_Table",
                columns: new[] { "PublisherNodeId", "SourceLocalId" },
                unique: true,
                filter: "[PublisherNodeId] IS NOT NULL AND [SourceLocalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TestList_Table_TestProfileId",
                table: "TestList_Table",
                column: "TestProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Test_ChannelId",
                table: "Test",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Test_PublisherNodeId_SourceLocalId",
                table: "Test",
                columns: new[] { "PublisherNodeId", "SourceLocalId" },
                unique: true,
                filter: "[PublisherNodeId] IS NOT NULL AND [SourceLocalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MachineDatas_PublisherNodeId_MachineId",
                table: "MachineDatas",
                columns: new[] { "PublisherNodeId", "MachineId" },
                unique: true,
                filter: "[PublisherNodeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Limit_PublisherNodeId_SourceLocalId",
                table: "Limit",
                columns: new[] { "PublisherNodeId", "SourceLocalId" },
                unique: true,
                filter: "[PublisherNodeId] IS NOT NULL AND [SourceLocalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_MachineDataId_ChannelIndex",
                table: "Channels",
                columns: new[] { "MachineDataId", "ChannelIndex" },
                unique: true,
                filter: "[MachineDataId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelStatusHistory_ChannelId_SnapshotAtUtc",
                table: "ChannelStatusHistory",
                columns: new[] { "ChannelId", "SnapshotAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelStatusHistory_PublisherNodeId_ChannelIndex_SnapshotAtUtc",
                table: "ChannelStatusHistory",
                columns: new[] { "PublisherNodeId", "ChannelIndex", "SnapshotAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_MessageId",
                schema: "sync",
                table: "InboxMessages",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_Status_ReceivedAtUtc",
                schema: "sync",
                table: "InboxMessages",
                columns: new[] { "Status", "ReceivedAtUtc" });

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

            migrationBuilder.CreateIndex(
                name: "IX_PublisherNodes_NodeCode",
                schema: "sync",
                table: "PublisherNodes",
                column: "NodeCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Channels_TestProfiles_TestProfileId",
                table: "Channels",
                column: "TestProfileId",
                principalTable: "TestProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Test_Channels_ChannelId",
                table: "Test",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestList_Table_Channels_ChannelId",
                table: "TestList_Table",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestList_Table_TestProfiles_TestProfileId",
                table: "TestList_Table",
                column: "TestProfileId",
                principalTable: "TestProfiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channels_TestProfiles_TestProfileId",
                table: "Channels");

            migrationBuilder.DropForeignKey(
                name: "FK_Test_Channels_ChannelId",
                table: "Test");

            migrationBuilder.DropForeignKey(
                name: "FK_TestList_Table_Channels_ChannelId",
                table: "TestList_Table");

            migrationBuilder.DropForeignKey(
                name: "FK_TestList_Table_TestProfiles_TestProfileId",
                table: "TestList_Table");

            migrationBuilder.DropTable(
                name: "ChannelStatusHistory");

            migrationBuilder.DropTable(
                name: "DeadLetterMessages",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "MachineHeartbeats",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "MachineStatusHistory");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "PublisherNodes",
                schema: "sync");

            migrationBuilder.DropIndex(
                name: "IX_TestProfiles_PublisherNodeId_SourceLocalId",
                table: "TestProfiles");

            migrationBuilder.DropIndex(
                name: "IX_TestList_Table_ChannelId",
                table: "TestList_Table");

            migrationBuilder.DropIndex(
                name: "IX_TestList_Table_MachineDataId_ChannelId_Start_Date_Time",
                table: "TestList_Table");

            migrationBuilder.DropIndex(
                name: "IX_TestList_Table_PublisherNodeId_SourceLocalId",
                table: "TestList_Table");

            migrationBuilder.DropIndex(
                name: "IX_TestList_Table_TestProfileId",
                table: "TestList_Table");

            migrationBuilder.DropIndex(
                name: "IX_Test_ChannelId",
                table: "Test");

            migrationBuilder.DropIndex(
                name: "IX_Test_PublisherNodeId_SourceLocalId",
                table: "Test");

            migrationBuilder.DropIndex(
                name: "IX_MachineDatas_PublisherNodeId_MachineId",
                table: "MachineDatas");

            migrationBuilder.DropIndex(
                name: "IX_Limit_PublisherNodeId_SourceLocalId",
                table: "Limit");

            migrationBuilder.DropIndex(
                name: "IX_Channels_MachineDataId_ChannelIndex",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "BrokerReceivedAtUtc",
                table: "TestProfiles");

            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "TestProfiles");

            migrationBuilder.DropColumn(
                name: "PublisherNodeId",
                table: "TestProfiles");

            migrationBuilder.DropColumn(
                name: "SourceLocalId",
                table: "TestProfiles");

            migrationBuilder.DropColumn(
                name: "BrokerReceivedAtUtc",
                table: "TestList_Table");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "TestList_Table");

            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "TestList_Table");

            migrationBuilder.DropColumn(
                name: "PublisherNodeId",
                table: "TestList_Table");

            migrationBuilder.DropColumn(
                name: "SourceLocalId",
                table: "TestList_Table");

            migrationBuilder.DropColumn(
                name: "TestProfileId",
                table: "TestList_Table");

            migrationBuilder.DropColumn(
                name: "BrokerReceivedAtUtc",
                table: "Test");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "Test");

            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "Test");

            migrationBuilder.DropColumn(
                name: "PublisherNodeId",
                table: "Test");

            migrationBuilder.DropColumn(
                name: "SourceLocalId",
                table: "Test");

            migrationBuilder.DropColumn(
                name: "BrokerReceivedAtUtc",
                table: "SMBMessagePair");

            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "SMBMessagePair");

            migrationBuilder.DropColumn(
                name: "PublisherNodeId",
                table: "SMBMessagePair");

            migrationBuilder.DropColumn(
                name: "SourceLocalId",
                table: "SMBMessagePair");

            migrationBuilder.DropColumn(
                name: "BrokerReceivedAtUtc",
                table: "MachineDatas");

            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "MachineDatas");

            migrationBuilder.DropColumn(
                name: "PublisherNodeId",
                table: "MachineDatas");

            migrationBuilder.DropColumn(
                name: "SourceLocalId",
                table: "MachineDatas");

            migrationBuilder.DropColumn(
                name: "BrokerReceivedAtUtc",
                table: "Limit");

            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "Limit");

            migrationBuilder.DropColumn(
                name: "PublisherNodeId",
                table: "Limit");

            migrationBuilder.DropColumn(
                name: "SourceLocalId",
                table: "Limit");

            migrationBuilder.DropColumn(
                name: "BrokerReceivedAtUtc",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "PublisherNodeId",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "SourceLocalId",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "BrokerReceivedAtUtc",
                table: "CANMessagePairs");

            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "CANMessagePairs");

            migrationBuilder.DropColumn(
                name: "PublisherNodeId",
                table: "CANMessagePairs");

            migrationBuilder.DropColumn(
                name: "SourceLocalId",
                table: "CANMessagePairs");

            migrationBuilder.RenameColumn(
                name: "TestProfileId",
                table: "Channels",
                newName: "testProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Channels_TestProfileId",
                table: "Channels",
                newName: "IX_Channels_testProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TestList_Table_MachineDataId",
                table: "TestList_Table",
                column: "MachineDataId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_MachineDataId",
                table: "Channels",
                column: "MachineDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Channels_TestProfiles_testProfileId",
                table: "Channels",
                column: "testProfileId",
                principalTable: "TestProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
