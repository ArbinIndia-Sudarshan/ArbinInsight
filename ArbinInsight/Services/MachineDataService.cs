using ArbinInsight.Data;
using ArbinInsight.Models;
using Microsoft.EntityFrameworkCore;

namespace ArbinInsight.Services
{
    public class MachineDataService : IMachineDataService
    {
        private readonly ApplicationDbContext _dbContext;

        public MachineDataService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<MachineData>> GetAllAsync()
        {
            return await CreateMachineDataQuery()
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<MachineData?> GetByIdAsync(int id)
        {
            return await CreateMachineDataQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<MachineData> CreateAsync(MachineData machineData)
        {
            var entity = MapMachineDataForCreate(machineData);

            _dbContext.MachineDatas.Add(entity);
            await _dbContext.SaveChangesAsync();

            var testLists = BuildTestLists(entity);
            if (testLists.Count > 0)
            {
                _dbContext.TestList_Table.AddRange(testLists);
                await _dbContext.SaveChangesAsync();
            }

            return await GetByIdAsync(entity.Id) ?? entity;
        }

        public async Task<bool> UpdateAsync(int id, MachineData machineData)
        {
            var existing = await _dbContext.MachineDatas.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null)
            {
                return false;
            }

            existing.MachineId = machineData.MachineId;
            existing.MachineName = machineData.MachineName;
            existing.Capacity = machineData.Capacity;
            existing.Status = machineData.Status;
            existing.LastUpdated = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await CreateMachineDataQuery()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null)
            {
                return false;
            }

            var testLists = await _dbContext.TestList_Table
                .Where(x => x.MachineData != null && x.MachineData.Id == id)
                .ToListAsync();
            var channels = existing.Channels ?? new List<Channel>();
            var canMessagePairs = channels.SelectMany(x => x.cANMessagePairList ?? new List<CANMessagePair>()).ToList();
            var smbMessagePairs = channels.SelectMany(x => x.sMBMessagePairList ?? new List<SMBMessagePair>()).ToList();
            var tests = channels
                .Where(x => x.testProfile != null)
                .SelectMany(x => x.testProfile.Tests ?? new List<Test>())
                .ToList();
            var limits = tests.SelectMany(x => x.Limits ?? new List<Limit>()).ToList();
            var testProfiles = channels
                .Where(x => x.testProfile != null)
                .Select(x => x.testProfile)
                .DistinctBy(x => x.Id)
                .ToList();

            if (testLists.Count > 0)
            {
                _dbContext.TestList_Table.RemoveRange(testLists);
            }
            if (canMessagePairs.Count > 0)
            {
                _dbContext.CANMessagePairs.RemoveRange(canMessagePairs);
            }
            if (smbMessagePairs.Count > 0)
            {
                _dbContext.SMBMessagePair.RemoveRange(smbMessagePairs);
            }
            if (limits.Count > 0)
            {
                _dbContext.Limits.RemoveRange(limits);
            }
            if (tests.Count > 0)
            {
                _dbContext.Tests.RemoveRange(tests);
            }
            if (channels.Count > 0)
            {
                _dbContext.Channels.RemoveRange(channels);
            }
            if (testProfiles.Count > 0)
            {
                _dbContext.TestProfiles.RemoveRange(testProfiles);
            }

            _dbContext.MachineDatas.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private IQueryable<MachineData> CreateMachineDataQuery()
        {
            return _dbContext.MachineDatas
                .Include(x => x.Channels)
                    .ThenInclude(x => x.testProfile)
                        .ThenInclude(x => x.Tests)
                            .ThenInclude(x => x.Limits)
                .Include(x => x.Channels)
                    .ThenInclude(x => x.cANMessagePairList)
                .Include(x => x.Channels)
                    .ThenInclude(x => x.sMBMessagePairList);
        }

        private static MachineData MapMachineDataForCreate(MachineData machineData)
        {
            return new MachineData
            {
                MachineId = machineData.MachineId,
                MachineName = machineData.MachineName,
                Capacity = machineData.Capacity,
                Status = machineData.Status,
                LastUpdated = DateTime.UtcNow,
                Channels = machineData.Channels?.Select(MapChannelForCreate).ToList() ?? new List<Channel>()
            };
        }

        private static Channel MapChannelForCreate(Channel channel)
        {
            return new Channel
            {
                ambientTemperature = channel.ambientTemperature,
                ChannelIndex = channel.ChannelIndex,
                TestID = channel.TestID,
                BarCode = channel.BarCode,
                TestName = channel.TestName,
                ChannelStatus = channel.ChannelStatus,
                IsRunning = channel.IsRunning,
                Result = channel.Result,
                Retest = channel.Retest,
                RetestNumber = channel.RetestNumber,
                UserName = channel.UserName,
                StartDateTime = channel.StartDateTime,
                EndDateTime = channel.EndDateTime,
                testProfile = MapTestProfileForCreate(channel.testProfile),
                cANMessagePairList = channel.cANMessagePairList?.Select(MapCanMessagePairForCreate).ToList() ?? new List<CANMessagePair>(),
                sMBMessagePairList = channel.sMBMessagePairList?.Select(MapSmbMessagePairForCreate).ToList() ?? new List<SMBMessagePair>(),
                ManuallyStopFlag = channel.ManuallyStopFlag,
                StopTestsExecuted = channel.StopTestsExecuted,
                BINNumber = channel.BINNumber
            };
        }

        private static TestProfile MapTestProfileForCreate(TestProfile? testProfile)
        {
            if (testProfile == null)
            {
                return null!;
            }

            return new TestProfile
            {
                TestProfileName = testProfile.TestProfileName,
                TestObjectName = testProfile.TestObjectName,
                CANBMSFileName = testProfile.CANBMSFileName,
                SMBFileName = testProfile.SMBFileName,
                Tests = testProfile.Tests?.Select(MapTestForCreate).ToList() ?? new List<Test>(),
                UDSRequestID = testProfile.UDSRequestID,
                UDSResponseID = testProfile.UDSResponseID,
                CreatedDateTime = testProfile.CreatedDateTime,
                ModifiedDateTime = testProfile.ModifiedDateTime,
                Creator = testProfile.Creator,
                Modifier = testProfile.Modifier,
                ExecuteStopTests = testProfile.ExecuteStopTests
            };
        }

        private static Test MapTestForCreate(Test test)
        {
            return new Test
            {
                Enable = test.Enable,
                StopOnFail = test.StopOnFail,
                TestName = test.TestName,
                ScheduleName = test.ScheduleName,
                TestStatus = test.TestStatus,
                Result = test.Result,
                StartDateTime = test.StartDateTime,
                EndDateTime = test.EndDateTime,
                Limits = test.Limits?.Select(MapLimitForCreate).ToList() ?? new List<Limit>()
            };
        }

        private static Limit MapLimitForCreate(Limit limit)
        {
            return new Limit
            {
                LimitName = limit.LimitName,
                MinValue = limit.MinValue,
                MaxValue = limit.MaxValue,
                MeasuredValue = limit.MeasuredValue,
                Unit = limit.Unit,
                Tolerance = limit.Tolerance,
                Result = limit.Result
            };
        }

        private static CANMessagePair MapCanMessagePairForCreate(CANMessagePair pair)
        {
            return new CANMessagePair
            {
                VariableName = pair.VariableName,
                Nickname = pair.Nickname
            };
        }

        private static SMBMessagePair MapSmbMessagePairForCreate(SMBMessagePair pair)
        {
            return new SMBMessagePair
            {
                VariableName = pair.VariableName,
                Nickname = pair.Nickname
            };
        }

        private static List<TestList> BuildTestLists(MachineData machineData)
        {
            if (machineData.Channels == null || machineData.Channels.Count == 0)
            {
                return new List<TestList>();
            }

            return machineData.Channels.Select(channel => new TestList
            {
                Test_Name = channel.TestName,
                Barcode = channel.BarCode,
                Result = channel.Result,
                Retest = channel.Retest.ToString(),
                Start_Date_Time = ToUnixMilliseconds(channel.StartDateTime),
                End_Date_Time = ToUnixMilliseconds(channel.EndDateTime),
                User_Name = channel.UserName,
                Channel_Index = (int)channel.ChannelIndex,
                BIN_Number = channel.BINNumber,
                TestProjectName = channel.testProfile?.TestObjectName ?? channel.TestName,
                TestProfile_Name = channel.testProfile?.TestProfileName,
                MachineData = machineData
            }).ToList();
        }

        private static long? ToUnixMilliseconds(DateTime dateTime)
        {
            if (dateTime == default)
            {
                return null;
            }

            return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }
    }
}
