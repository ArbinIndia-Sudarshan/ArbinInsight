using ArbinInsight.Data;
using ArbinInsight.Models;
using ArbinInsight.Models.Sync;
using Microsoft.EntityFrameworkCore;

namespace ArbinInsight.Services
{
    public class MachineDemoRefreshService : BackgroundService
    {
        private static readonly string[] MachineNames = ["BTM-01", "BTM-02", "BTM-03"];
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MachineDemoRefreshService> _logger;

        public MachineDemoRefreshService(IServiceScopeFactory scopeFactory, ILogger<MachineDemoRefreshService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunRefreshCycleAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunRefreshCycleAsync(stoppingToken);
            }
        }

        private async Task RunRefreshCycleAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await dbContext.Database.MigrateAsync(cancellationToken);
                await SeedAsync(dbContext, cancellationToken);
                await RefreshAsync(dbContext, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Machine demo refresh cycle failed.");
            }
        }

        private static async Task SeedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
        {
            var existingMachineNames = await dbContext.MachineDatas
                .Where(x => MachineNames.Contains(x.MachineName))
                .Select(x => x.MachineName)
                .ToListAsync(cancellationToken);

            if (existingMachineNames.Count == MachineNames.Length)
            {
                return;
            }

            var nowUtc = DateTime.UtcNow;
            for (var machineNumber = 1; machineNumber <= MachineNames.Length; machineNumber++)
            {
                var machineName = MachineNames[machineNumber - 1];
                if (existingMachineNames.Contains(machineName))
                {
                    continue;
                }

                var publisherId = Guid.NewGuid();
                var publisher = new PublisherNode
                {
                    PublisherNodeId = publisherId,
                    NodeCode = $"NODE-{machineName}",
                    MachineId = 100 + machineNumber,
                    MachineName = machineName,
                    HostName = $"{machineName.ToLowerInvariant()}.lab.local",
                    IpAddress = $"192.168.1.{10 + machineNumber}",
                    IsOnline = true,
                    LastHeartbeatUtc = nowUtc,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                };

                var machine = new MachineData
                {
                    PublisherNodeId = publisherId,
                    MachineId = publisher.MachineId,
                    MachineName = machineName,
                    Capacity = 8,
                    Status = machineNumber == 2 ? "Idle" : "Running",
                    BrokerReceivedAtUtc = nowUtc,
                    LastUpdated = nowUtc,
                    Channels = BuildChannels(machineName, machineNumber, nowUtc)
                };

                machine.TestLists = BuildInitialTestLists(machine, nowUtc);
                dbContext.PublisherNodes.Add(publisher);
                dbContext.MachineDatas.Add(machine);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static List<Channel> BuildChannels(string machineName, int machineNumber, DateTime nowUtc)
        {
            var channels = new List<Channel>();
            for (var slot = 1; slot <= 8; slot++)
            {
                var isRunning = slot <= 3 + machineNumber;
                var startedAt = nowUtc.AddMinutes(-(slot * 14 + machineNumber * 6));
                var testProfile = new TestProfile
                {
                    TestProfileName = $"Profile-{machineName}-{slot}",
                    TestObjectName = $"Battery Pack {slot}",
                    CANBMSFileName = $"pack-{slot}.dbc",
                    SMBFileName = $"pack-{slot}.smb",
                    Tests = new List<Test>(),
                    UDSRequestID = $"REQ-{machineNumber}-{slot}",
                    UDSResponseID = $"RES-{machineNumber}-{slot}",
                    CreatedDateTime = nowUtc.AddDays(-2),
                    ModifiedDateTime = nowUtc,
                    Creator = "demo-seed",
                    Modifier = "demo-seed",
                    ExecuteStopTests = false
                };

                channels.Add(new Channel
                {
                    ChannelIndex = (uint)slot,
                    TestID = machineNumber * 1000 + slot,
                    BarCode = $"BAT-{machineName}-{slot:00}",
                    TestName = isRunning ? "Charge-Discharge Cycle" : "Resting Validation",
                    ChannelStatus = isRunning ? "Running" : "Idle",
                    IsRunning = isRunning,
                    Result = isRunning ? "In Progress" : (slot % 4 == 0 ? "Failed" : "Passed"),
                    Retest = false,
                    RetestNumber = "0",
                    UserName = machineNumber == 1 ? "admin" : machineNumber == 2 ? "operator" : "qa.user",
                    StartDateTime = startedAt,
                    EndDateTime = isRunning ? default : startedAt.AddMinutes(70 + slot * 5),
                    ambientTemperature = 25f + slot,
                    testProfile = testProfile,
                    cANMessagePairList = new List<CANMessagePair>(),
                    sMBMessagePairList = new List<SMBMessagePair>(),
                    ManuallyStopFlag = false,
                    StopTestsExecuted = false,
                    BINNumber = $"{machineNumber}{slot:00}"
                });
            }

            return channels;
        }

        private static List<TestList> BuildInitialTestLists(MachineData machine, DateTime nowUtc)
        {
            var rows = new List<TestList>();
            foreach (var channel in machine.Channels)
            {
                for (var offset = 0; offset < 4; offset++)
                {
                    var startedAt = nowUtc.AddHours(-(offset + 1) * 5).AddMinutes(-Convert.ToInt32(channel.ChannelIndex) * 6);
                    var endedAt = startedAt.AddMinutes(80 + Convert.ToInt32(channel.ChannelIndex) * 3);
                    rows.Add(new TestList
                    {
                        PublisherNodeId = machine.PublisherNodeId,
                        SourceLocalId = machine.MachineId * 1000 + Convert.ToInt32(channel.ChannelIndex) * 10 + offset,
                        Test_Name = channel.TestName,
                        Barcode = channel.BarCode,
                        Result = offset % 5 == 0 ? "Failed" : "Passed",
                        Retest = "False",
                        Start_Date_Time = new DateTimeOffset(startedAt).ToUnixTimeMilliseconds(),
                        End_Date_Time = new DateTimeOffset(endedAt).ToUnixTimeMilliseconds(),
                        User_Name = channel.UserName,
                        Channel_Index = Convert.ToInt32(channel.ChannelIndex),
                        BIN_Number = channel.BINNumber,
                        TestProjectName = channel.testProfile?.TestObjectName,
                        TestProfile_Name = channel.testProfile?.TestProfileName
                    });
                }
            }

            return rows;
        }

        private static async Task RefreshAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
        {
            var nowUtc = DateTime.UtcNow;
            var machines = await dbContext.MachineDatas
                .Include(x => x.Channels)
                    .ThenInclude(x => x.testProfile)
                .Include(x => x.TestLists)
                .Where(x => MachineNames.Contains(x.MachineName))
                .OrderBy(x => x.MachineName)
                .ToListAsync(cancellationToken);

            var publishers = await dbContext.PublisherNodes
                .Where(x => MachineNames.Contains(x.MachineName))
                .OrderBy(x => x.MachineName)
                .ToListAsync(cancellationToken);

            foreach (var machine in machines)
            {
                var publisher = publishers.FirstOrDefault(x => x.MachineId == machine.MachineId);
                var activeTarget = 2 + (nowUtc.Second / 5 + machine.MachineId) % 6;
                activeTarget = Math.Min(activeTarget, machine.Channels.Count);

                machine.Status = activeTarget > 0 ? "Running" : "Idle";
                machine.LastUpdated = nowUtc;
                machine.BrokerReceivedAtUtc = nowUtc;

                if (publisher != null)
                {
                    publisher.IsOnline = true;
                    publisher.LastHeartbeatUtc = nowUtc;
                    publisher.UpdatedAtUtc = nowUtc;
                }

                foreach (var channel in machine.Channels.OrderBy(x => x.ChannelIndex))
                {
                    var isRunning = channel.ChannelIndex <= activeTarget;
                    channel.IsRunning = isRunning;
                    channel.ChannelStatus = isRunning ? "Running" : "Idle";
                    channel.Result = isRunning ? "In Progress" : (channel.ChannelIndex % 4 == (uint)((nowUtc.Second / 5) % 4) ? "Failed" : "Passed");
                    channel.ambientTemperature = 24f + (float)(channel.ChannelIndex * 0.8) + (float)((nowUtc.Second / 5) % 3);
                    channel.TestName = isRunning ? "Charge-Discharge Cycle" : "Post-Run Validation";
                    channel.StartDateTime = isRunning ? nowUtc.AddMinutes(-(20 + Convert.ToInt32(channel.ChannelIndex) * 8)) : nowUtc.AddMinutes(-(95 + Convert.ToInt32(channel.ChannelIndex) * 6));
                    channel.EndDateTime = isRunning ? default : nowUtc.AddMinutes(-(5 + Convert.ToInt32(channel.ChannelIndex)));
                    if (channel.testProfile != null)
                    {
                        channel.testProfile.ModifiedDateTime = nowUtc;
                        channel.testProfile.Modifier = "demo-refresh";
                    }
                }

                var latestChannel = machine.Channels.OrderBy(x => x.ChannelIndex).First();
                machine.TestLists.Add(new TestList
                {
                    PublisherNodeId = machine.PublisherNodeId,
                    SourceLocalId = unchecked((int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % int.MaxValue)) + machine.MachineId * 1000000,
                    Test_Name = latestChannel.TestName,
                    Barcode = latestChannel.BarCode,
                    Result = nowUtc.Second % 3 == 0 ? "Failed" : "Passed",
                    Retest = "False",
                    Start_Date_Time = new DateTimeOffset(nowUtc.AddMinutes(-45)).ToUnixTimeMilliseconds(),
                    End_Date_Time = new DateTimeOffset(nowUtc.AddMinutes(-2)).ToUnixTimeMilliseconds(),
                    User_Name = latestChannel.UserName,
                    Channel_Index = Convert.ToInt32(latestChannel.ChannelIndex),
                    BIN_Number = latestChannel.BINNumber,
                    TestProjectName = latestChannel.testProfile?.TestObjectName,
                    TestProfile_Name = latestChannel.testProfile?.TestProfileName
                });

                if (machine.TestLists.Count > 40)
                {
                    var oldestRows = machine.TestLists
                        .OrderBy(x => x.End_Date_Time ?? x.Start_Date_Time ?? long.MaxValue)
                        .Take(machine.TestLists.Count - 40)
                        .ToList();
                    dbContext.TestList_Table.RemoveRange(oldestRows);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
