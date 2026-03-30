using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbinInsight.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MachineDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineDatas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestProfileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TestObjectName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CANBMSFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SMBFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UDSRequestID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UDSResponseID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Creator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Modifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecuteStopTests = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestList_Table",
                columns: table => new
                {
                    Test_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Test_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Retest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Start_Date_Time = table.Column<long>(type: "bigint", nullable: true),
                    End_Date_Time = table.Column<long>(type: "bigint", nullable: true),
                    User_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Channel_Index = table.Column<int>(type: "int", nullable: true),
                    BIN_Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestProjectName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestProfile_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineDataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestList_Table", x => x.Test_ID);
                    table.ForeignKey(
                        name: "FK_TestList_Table_MachineDatas_MachineDataId",
                        column: x => x.MachineDataId,
                        principalTable: "MachineDatas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ambientTemperature = table.Column<float>(type: "real", nullable: false),
                    ChannelIndex = table.Column<long>(type: "bigint", nullable: false),
                    TestID = table.Column<int>(type: "int", nullable: false),
                    BarCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChannelStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRunning = table.Column<bool>(type: "bit", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Retest = table.Column<bool>(type: "bit", nullable: false),
                    RetestNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    testProfileId = table.Column<int>(type: "int", nullable: false),
                    ManuallyStopFlag = table.Column<bool>(type: "bit", nullable: false),
                    StopTestsExecuted = table.Column<bool>(type: "bit", nullable: false),
                    BINNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MachineDataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Channels_MachineDatas_MachineDataId",
                        column: x => x.MachineDataId,
                        principalTable: "MachineDatas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Channels_TestProfiles_testProfileId",
                        column: x => x.testProfileId,
                        principalTable: "TestProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Test",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestID = table.Column<int>(type: "int", nullable: false),
                    Enable = table.Column<bool>(type: "bit", nullable: false),
                    StopOnFail = table.Column<bool>(type: "bit", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TestStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TestProfileId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Test", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Test_TestProfiles_TestProfileId",
                        column: x => x.TestProfileId,
                        principalTable: "TestProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CANMessagePairs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VariableName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nickname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChannelId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CANMessagePairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CANMessagePairs_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SMBMessagePair",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VariableName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nickname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChannelId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMBMessagePair", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SMBMessagePair_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Limit",
                columns: table => new
                {
                    LimitID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LimitName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinValue = table.Column<double>(type: "float", nullable: false),
                    MaxValue = table.Column<double>(type: "float", nullable: false),
                    MeasuredValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tolerance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TestId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Limit", x => x.LimitID);
                    table.ForeignKey(
                        name: "FK_Limit_Test_TestId",
                        column: x => x.TestId,
                        principalTable: "Test",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CANMessagePairs_ChannelId",
                table: "CANMessagePairs",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_MachineDataId",
                table: "Channels",
                column: "MachineDataId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_testProfileId",
                table: "Channels",
                column: "testProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Limit_TestId",
                table: "Limit",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_SMBMessagePair_ChannelId",
                table: "SMBMessagePair",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Test_TestProfileId",
                table: "Test",
                column: "TestProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TestList_Table_MachineDataId",
                table: "TestList_Table",
                column: "MachineDataId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CANMessagePairs");

            migrationBuilder.DropTable(
                name: "Limit");

            migrationBuilder.DropTable(
                name: "SMBMessagePair");

            migrationBuilder.DropTable(
                name: "TestList_Table");

            migrationBuilder.DropTable(
                name: "Test");

            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "MachineDatas");

            migrationBuilder.DropTable(
                name: "TestProfiles");
        }
    }
}
