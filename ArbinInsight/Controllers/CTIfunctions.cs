using ArbinCTI.Core;
using ArbinCTI.Core.Control;
using ArbinCTI.Core.Inteface;
using log4net;
using System.Diagnostics;
using System.Net.Sockets;

namespace ArbinInsight.Controllers
{
    public class CTIfunctions
    {
        MyArbinControl m_Ctrl;
        ArbinClient m_Client;
        public ArbinCommandFeed arbinCommandFeed;
        string m_strUser = string.Empty;
        string m_strPassword = string.Empty;
        string m_strIP = "127.0.0.1";
        const int CTI_PORT = 9031;
        public bool connected;
        private static readonly ILog log = LogManager.GetLogger("NormalLogger");

        public CTIfunctions(string userName, string Password)
        {
            log.Info("Initializing CTIfunctions instance...");
            m_strUser = userName;
            m_strPassword = Password;
            arbinCommandFeed = new ArbinCommandFeed();
            m_Ctrl = new MyArbinControl(arbinCommandFeed);
            log.Info("CTIfunctions instance created.");
        }

        /// <summary>
        /// Connects to the CTI server and performs login operations.
        /// </summary>
        /// <returns> ArbinCommandLoginFeed </returns>
        internal ArbinCommandLoginFeed ConnectLogin()
        {
            log.Info("Attempting CTI login...");
            if (connected == true)
            {
                log.Info("Connection successful. Posting login request...");
                m_Ctrl.PostLogicConnect(m_Client, false);
                m_Ctrl.PostUserLogin(m_Client, m_strUser, m_strPassword);
                if (m_Ctrl.WaitAutoResetEvent.WaitOne(3000))
                {

                }
                if (arbinCommandFeed.LoginFeed != null && (arbinCommandFeed.LoginFeed.Result == ArbinCommandLoginFeed.LOGIN_RESULT.CTI_LOGIN_SUCCESS ||
                    arbinCommandFeed.LoginFeed.Result == ArbinCommandLoginFeed.LOGIN_RESULT.CTI_LOGIN_BEFORE_SUCCESS))
                {
                    log.Info($"Login Successfull: {arbinCommandFeed.LoginFeed.Result}");
                    return arbinCommandFeed.LoginFeed;
                }
                else
                {
                    return arbinCommandFeed.LoginFeed;
                }
            }
            else
            {
                if (m_Client != null)
                {
                    m_Client.ShutDown();
                }
                try
                {
                    m_Ctrl.Start();
                    m_Client = new ArbinClient();
                    m_Client.OnConnectionChanged += (Socket, e) =>
                    {
                        connected = e.Connected;
                    };
                    m_Client.OnSocketInfoLogCall += M_Client_OnSocketInfoLogCall;
                    m_Ctrl.ListenSocketRecv(m_Client);

                    int err;
                    int Result = 0;
                    log.Debug($"Connecting to {m_strIP}:{CTI_PORT}");
                    Result = m_Client.ConnectAsync(m_strIP, CTI_PORT, 0, out err);
                    if (Result != 0)
                    {
                        if (Result == -2)
                        {
                            SocketError socketError = (SocketError)err;
                            log.Error($"Socket Error While CTI Connection: {socketError}");
                            return arbinCommandFeed.LoginFeed;
                        }

                        log.Error($"Error While CTI Connection: {Result}");
                        return arbinCommandFeed.LoginFeed;
                    }
                    else
                    {
                        int j = 0;
                        while (j < 5)
                        {
                            if (connected == true)
                            {
                                log.Info("Connection successful. Posting login request...");
                                m_Ctrl.PostLogicConnect(m_Client, false);
                                m_Ctrl.PostUserLogin(m_Client, m_strUser, m_strPassword);
                                if (m_Ctrl.WaitAutoResetEvent.WaitOne(3000))
                                {

                                }
                                if (arbinCommandFeed.LoginFeed != null && (arbinCommandFeed.LoginFeed.Result == ArbinCommandLoginFeed.LOGIN_RESULT.CTI_LOGIN_SUCCESS ||
                                    arbinCommandFeed.LoginFeed.Result == ArbinCommandLoginFeed.LOGIN_RESULT.CTI_LOGIN_BEFORE_SUCCESS))
                                {
                                    log.Info($"Login Successfull: {arbinCommandFeed.LoginFeed.Result}");
                                    return arbinCommandFeed.LoginFeed;
                                }
                                else
                                {
                                    throw new Exception("Login Feed is Null");
                                }
                            }
                            j++;
                            Thread.Sleep(1000);
                        }
                        return arbinCommandFeed.LoginFeed;
                    }
                }
                catch (Exception e)
                {
                    log.Error("ConnectLogin Exception:" + e.Message);
                    return arbinCommandFeed.LoginFeed;
                }
            }            
        }

        internal ArbinCommandLoginFeed CTILoginOnly()
        {
            if (connected == true)
            {
                log.Info("Connection successful. Posting login request...");
                m_Ctrl.PostLogicConnect(m_Client, false);
                m_Ctrl.PostUserLogin(m_Client, m_strUser, m_strPassword);
                if (m_Ctrl.WaitAutoResetEvent.WaitOne(3000))
                {

                }
                if (arbinCommandFeed.LoginFeed != null && (arbinCommandFeed.LoginFeed.Result == ArbinCommandLoginFeed.LOGIN_RESULT.CTI_LOGIN_SUCCESS ||
                    arbinCommandFeed.LoginFeed.Result == ArbinCommandLoginFeed.LOGIN_RESULT.CTI_LOGIN_BEFORE_SUCCESS))
                {
                    log.Info($"Login Successfull: {arbinCommandFeed.LoginFeed.Result}");
                    return arbinCommandFeed.LoginFeed;
                }
                else
                {
                    return arbinCommandFeed.LoginFeed;
                }
            }
            else
            {
                return ConnectLogin();
            }
        }

        private void M_Client_OnSocketInfoLogCall(IArbinSocket Socket, ArbinSocketEventArgSet.SocketErrorEventArgs e)
        {
            if (!(e.Msg.Contains("PostGetChannelsData")))
            {
                log.Info($"Socket Log: {e.Msg}, {e.Exp}");
            }
        }

        /// <summary>
        /// Assigns a schedule to a specific channel.
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="chnl"></param>
        /// <returns></returns>
        internal bool AssignSchedule(string schedule, int chnl)
        {
            try
            {
                arbinCommandFeed.ScheduleFeed = null;
                string Schedule = schedule.Split('\\').Last();
                log.Info($"Assigning Schedule :{schedule}, on Channel index: {chnl}");

                if (m_Client != null && m_Client.IsConnected())
                {
                    AutoResetEvent autoResetEvent = new AutoResetEvent(false);
                    autoResetEvent.WaitOne(4000);
                    m_Ctrl.PostAssignSchedule(m_Client, Schedule, "", 0, 0, 0, 0, 0, false, chnl);
                    while (true)
                    {
                        if (arbinCommandFeed.ScheduleFeed != null)
                        {
                            if (arbinCommandFeed.ScheduleFeed.Result == ArbinCommandAssignScheduleFeed.ASSIGN_TOKEN.CTI_ASSIGN_SUCCESS)
                            {
                                log.Info($"Assign Successfull: {arbinCommandFeed.ScheduleFeed.Result}");
                                return true;
                            }
                            else
                            {
                                log.Error($"Error in Assigning Schedule: {schedule}, on Channel index: {chnl} : " + arbinCommandFeed.ScheduleFeed.Result.ToString());
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("Error in Assigning Schedule - CTI Connection Error");
                }
            }
            catch (Exception e)
            {
                log.Error($"Exception in Assigning Schedule: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Assigns a barcode to a specific channel index.
        /// </summary>
        /// <param name="channelIndex"></param>
        /// <param name="Barcode"></param>
        /// <returns></returns>
        internal bool AssignBarCode(int channelIndex, string Barcode)
        {
            try
            {
                log.Info($"Assigning Barcode :{Barcode}, on Channel index: {channelIndex}");
                AutoResetEvent autoResetEvent = new AutoResetEvent(false);
                arbinCommandFeed.AssignBarcodeInfoFeed = null;
                List<ArbinCommandAssignBarcodeInfoFeed.ChannelBarcodeInfo> channelBarcodeInfos = new List<ArbinCommandAssignBarcodeInfoFeed.ChannelBarcodeInfo>();
                ArbinCommandAssignBarcodeInfoFeed.ChannelBarcodeInfo channelBarcodeInfo = new ArbinCommandAssignBarcodeInfoFeed.ChannelBarcodeInfo();
                channelBarcodeInfo.GlobalIndex = (ushort)channelIndex;
                channelBarcodeInfo.Barcode = Barcode;
                channelBarcodeInfos.Add(channelBarcodeInfo);

                if (m_Client != null && m_Client.IsConnected())
                {
                    m_Ctrl.PostAssignBarcodeInfo(m_Client, ArbinCommandAssignBarcodeInfoFeed.EChannelType.IV, channelBarcodeInfos);
                    autoResetEvent.WaitOne(3000);
                    while (true)
                    {
                        if (arbinCommandFeed.AssignBarcodeInfoFeed != null)
                        {
                            if (arbinCommandFeed.AssignBarcodeInfoFeed.BarcodeInfos[0].Error == ArbinCommandAssignBarcodeInfoFeed.ASSIGN_BARCODE_RESULT.CTI_ASSIGN_BARCODE_SUCCESS)
                            {
                                log.Info($"Assign Barcode Successfull: {arbinCommandFeed.AssignBarcodeInfoFeed.BarcodeInfos[0].Error}");
                                return true;
                            }
                            else
                            {
                                log.Error($"Assigning Barcode Failed: {Barcode}, on Channel index: {channelIndex} " + arbinCommandFeed.AssignBarcodeInfoFeed.BarcodeInfos[0].Error.ToString());
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    log.Error("Error in Assigning Barcode - CTI Connection Error");
                    return false;
                }
            }
            catch (Exception e)
            {
                log.Error($"Exception in Assigning Barcode: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Assigns a file to a specific channel index based on the file type.
        /// </summary>
        /// <param name="channelIndex"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        internal bool AssignFile(int channelIndex, string file)
        {
            try
            {
                arbinCommandFeed.AssignFileFeed = null;
                List<ushort> Channels = new List<ushort>() { (ushort)channelIndex };
                ArbinCommandAssignFileFeed.EFileKind eFileKind = ArbinCommandAssignFileFeed.EFileKind.None;
                string fileExtension = Path.GetExtension(file);
                string fileName = file;
                switch (fileExtension)
                {
                    case ".sdx":
                        eFileKind = ArbinCommandAssignFileFeed.EFileKind.Schedule;
                        break;
                    case ".to":
                        eFileKind = ArbinCommandAssignFileFeed.EFileKind.TestObject;
                        break;
                    case ".can":
                    case ".CAN":
                        eFileKind = ArbinCommandAssignFileFeed.EFileKind.CANBMS;
                        break;
                    case ".smb":
                    case ".SMB":
                        eFileKind = ArbinCommandAssignFileFeed.EFileKind.SMB;
                        break;
                }

                log.Info($"Assigning File :{fileName}, Type :{eFileKind}, on Channel index: {channelIndex}");

                if (m_Client != null && m_Client.IsConnected())
                {
                    m_Ctrl.PostAssignFile(m_Client, fileName, false, eFileKind, Channels);
                    while (true)
                    {
                        if (arbinCommandFeed.AssignFileFeed != null)
                        {
                            if (arbinCommandFeed.AssignFileFeed.Result == ArbinCommandAssignFileFeed.ASSIGN_TOKEN.CTI_ASSIGN_SUCCESS)
                            {
                                log.Info($"Assign File Successfull: {arbinCommandFeed.AssignFileFeed.Result}");
                                return true;
                            }
                            else
                            {
                                log.Error($"Error in Assigning File: {fileName}, Type :{eFileKind}, on Channel index: {channelIndex} : " + arbinCommandFeed.AssignFileFeed.Result.ToString());
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("Error in Assigning File - CTI Connection Error");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in Assigning File:{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Starts a test on a specific channel with the given schedule and metavariables.
        /// </summary>
        /// <param name="testname"></param>
        /// <param name="Chnl"></param>
        /// <param name="Schedule"></param>
        /// <param name="metavariables"></param>
        /// <returns></returns>
        internal bool StartTestCTI(string testname, uint Chnl, string Schedule, List<KeyValuePair<string, float>> metavariables, int EOLTestID)
        {
            log.Info($"Starting Test on Channel index: {Chnl}, Schedule name: {Schedule}");
            arbinCommandFeed.GetChannelDataFeed = null;
            arbinCommandFeed.StartChannelFeed = null;
            int channelindex = (int)Chnl;
            try
            {
                arbinCommandFeed.StartChannelFeed = null;
                StartResumeEx startResumeEx = new StartResumeEx();
                startResumeEx.channelIndex = Chnl;
                startResumeEx.TestNames = testname;
                startResumeEx.Schedules = Schedule;
                startResumeEx.MVUD1 = metavariables[0].Value;
                startResumeEx.MVUD2 = metavariables[1].Value;
                startResumeEx.MVUD3 = metavariables[2].Value;
                startResumeEx.MVUD4 = metavariables[3].Value;
                startResumeEx.MVUD5 = metavariables[4].Value;
                startResumeEx.MVUD6 = metavariables[5].Value;
                startResumeEx.MVUD7 = metavariables[6].Value;
                startResumeEx.MVUD8 = metavariables[7].Value;
                startResumeEx.MVUD9 = metavariables[8].Value;
                startResumeEx.MVUD10 = metavariables[9].Value;
                startResumeEx.MVUD11 = metavariables[10].Value;
                startResumeEx.MVUD12 = metavariables[11].Value;
                startResumeEx.MVUD13 = metavariables[12].Value;
                startResumeEx.MVUD14 = metavariables[13].Value;
                startResumeEx.MVUD15 = metavariables[14].Value;
                startResumeEx.MVUD16 = metavariables[15].Value;
                List<StartResumeEx> StartEx = new List<StartResumeEx>() { startResumeEx };

                if (m_Client != null && m_Client.IsConnected())
                {
                    m_Ctrl.PostStartChannelEx(m_Client, StartEx, "ArbinEOL", "EOLTestID:" + EOLTestID.ToString());
                    while (true)
                    {
                        if (arbinCommandFeed.StartChannelFeed != null)
                        {
                            string startresult = arbinCommandFeed.StartChannelFeed.Result.ToString();
                            if (arbinCommandFeed.StartChannelFeed.Result == ArbinCommandStartChannelFeed.START_TOKEN.CTI_START_SUCCESS)
                            {
                                log.Info($"Start Channel Success: {startresult} on Channel index: {Chnl}, Schedule name: {Schedule}");
                                Thread.Sleep(3000);
                                m_Ctrl.PostGetChannelsData(m_Client);
                                while (true)
                                {
                                    if (arbinCommandFeed.GetChannelDataFeed != null)
                                    {
                                        if (arbinCommandFeed.GetChannelDataFeed.m_Channels[channelindex].TestTime > 0)
                                        {
                                            log.Info($"Get Channels Data after start test");
                                            return true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                log.Error($"Error in Starting Test: {startresult} on Channel index: {Chnl}, Schedule name: {Schedule}");
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("Error in Starting Test - CTI Connection Error");
                }
            }
            catch (Exception e)
            {
                log.Error($"Exception in Start Test CTI:{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Fetches the auxiliary temperature for a specific Aux Temp channel.
        /// </summary>
        /// <param name="Chnl"></param>
        /// <returns></returns>
        internal float GetAuxTemp(int Chnl)
        {
            try
            {
                log.Info($"Fetching Aux Temperature for Channel index: {Chnl}");
                ArbinCommandGetMetaVariablesFeed.MetaVariableInfo MV_Info = new ArbinCommandGetMetaVariablesFeed.MetaVariableInfo();
                MV_Info.m_Channel = (ushort)Chnl;
                MV_Info.m_MV_DataType = TE_DATA_TYPE.MP_DATA_TYPE_AuxTemperature;
                MV_Info.m_MV_MetaCode = 0;

                m_Ctrl.PostGetMetaVariables(m_Client, new List<ArbinCommandGetMetaVariablesFeed.MetaVariableInfo> { MV_Info }, out int err);
                Stopwatch sw = Stopwatch.StartNew();
                while (true)
                {
                    if (arbinCommandFeed.GetMetaVariablesFeed != null)
                    {
                        log.Info($"Aux Temperature fetched successfully for Channel index: {Chnl} : Temp Value {arbinCommandFeed.GetMetaVariablesFeed.MetaVariableInfos[0].m_Value}");
                        return arbinCommandFeed.GetMetaVariablesFeed.MetaVariableInfos[0].m_Value;
                    }
                    if (sw.ElapsedMilliseconds > 10000)
                    {
                        throw new Exception("Fetching Aux TimedOut.");
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Exception while fetching the Aux Temperature" + e.Message);
                return 0;
            }
        }

        /// <summary>
        /// Fetches the details of all channels connected to the CTI.
        /// </summary>
        /// <returns></returns>
        internal List<ArbinCommandGetChannelDataFeed.ChannelInfo> GetChnlDetailsCTI()
        {
            try
            {
                arbinCommandFeed.GetChannelDataFeed = null;
                if (m_Client != null && m_Client.IsConnected())
                {
                    m_Ctrl.PostGetChannelsData(m_Client);
                    while (true)
                    {
                        if (arbinCommandFeed.GetChannelDataFeed != null && arbinCommandFeed.GetChannelDataFeed.m_Channels.Count != 0)
                        {
                            return arbinCommandFeed.GetChannelDataFeed.m_Channels;
                        }
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                log.Error("Error while fetching Channel Details\n" + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Stops a specific channel in MITS.
        /// </summary>
        /// <param name="Chnl"></param>
        /// <returns></returns>
        internal bool ChannelStopCTI(int Chnl)
        {
            try
            {
                if (m_Client != null && m_Client.IsConnected())
                {
                    m_Ctrl.PostStopChannel(m_Client, Chnl, false);
                    while (true)
                    {
                        if (arbinCommandFeed.StopChannelFeed != null)
                        {
                            if (arbinCommandFeed.StopChannelFeed.Result == ArbinCommandStopChannelFeed.STOP_TOKEN.SUCCESS | arbinCommandFeed.StopChannelFeed.Result == ArbinCommandStopChannelFeed.STOP_TOKEN.STOP_NOT_RUNNING)
                            {
                                log.Info($"Channel Stop Successfull on Channel index: {Chnl}, Result: {arbinCommandFeed.StopChannelFeed.Result}");
                                Thread.Sleep(100);
                                return true;
                            }
                            else
                            {
                                log.Error($"Error in Stopping Channel: {Chnl}, Result: {arbinCommandFeed.StopChannelFeed.Result}");
                                return false;
                            }
                        }                        
                    }
                }
                else
                {
                    throw new Exception("Error in Stopping Channel - CTI Connection Error");
                }
            }
            catch (Exception e)
            {
                log.Error("Exception in Stop Channel CTI:" + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Sets the interval time for logging data from the MCU. Only applicable in case of MITS 8.
        /// </summary>
        /// <returns></returns>
        internal bool SetMcuLogInterval()
        {
            int i = 0;
            m_Ctrl.PostSetIntervalTimeLogData(m_Client, 0.1f);
            while (true)
            {
                if (arbinCommandFeed.SetIntervalTimeLogDataFeed != null)
                {
                    if (arbinCommandFeed.SetIntervalTimeLogDataFeed.Result == ArbinCommandSetIntervalTimeLogDataFeed.SET_INTERVAL_TIME_LOG_DATA_RESULT.SET_INTERVALTIME_LOGDATA_SUCCESS)
                    {
                        log.Info("Set Interval Time Log Data Successfull!");
                        return true;
                    }
                    else
                    {
                        log.Error("Error in Set Interval Time Log Data: " + arbinCommandFeed.SetIntervalTimeLogDataFeed.Result.ToString());
                        return false;
                    }
                }                
            }
        }

        /// <summary>
        /// Checks if the client is connected to the server.
        /// </summary>
        /// <returns></returns>
        internal bool isConnected()
        {
            if (m_Client == null)
            {
                return false;
            }
            return m_Client.IsConnected();
        }

        /// <summary>
        /// Converts a specific channel's test object to an anonymous or named test object. Applicable only in case of MITS 10.
        /// </summary>
        /// <param name="ChannelIndex"></param>
        /// <returns></returns>
        internal bool ConvertTestObject(int ChannelIndex)
        {
            try
            {
                List<ushort> Chnl = new List<ushort> { (ushort)ChannelIndex };
                if (m_Client != null && m_Client.IsConnected())
                {
                    m_Ctrl.PostConvertToAnonymousOrNamedTO(m_Client, false, false, Chnl);
                    while (true)
                    {
                        if (arbinCommandFeed.ConvertTestObjest != null)
                        {
                            if (arbinCommandFeed.ConvertTestObjest.Result == ArbinCommandConvertToAnonymousOrNamedTOFeed.CONVERTTESTOBJECT_RESULT.CTI_CONVERT_SUCCESS)
                            {
                                log.Info($"Convert TestObject Successfull on Channel index: {ChannelIndex}");
                                return true;
                            }
                            else
                            {
                                log.Error($"Error in Convert TestObject on Channel index: {ChannelIndex} : " + arbinCommandFeed.ConvertTestObjest.Result.ToString());
                                return false;
                            }                            
                        }
                    }                    
                }
                else
                {
                    throw new Exception("Error in Convert TestObject - CTI Connection Error");
                }
            }
            catch (Exception e)
            {
                log.Error("Exception while coverting TestObject" + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Disconnects the client and cleans up resources.
        /// </summary>
        internal void DisconnectCTI()
        {
            if (m_Client != null)
            {
                m_Client.ShutDown();
                m_Ctrl.Exit();
                m_Client = null;
                m_Ctrl = null;
            }
        }

        /// <summary>
        /// This class is used to hold all the command feeds for Arbin CTI operations.
        /// </summary>
        public class ArbinCommandFeed
        {
            public ArbinCommandLoginFeed LoginFeed { get; set; }
            public ArbinCommandAssignScheduleFeed ScheduleFeed { get; set; }
            public ArbinCommandLogicConnectFeed LogicConnectFeed { get; set; }
            public ArbinCommandStartChannelFeed StartChannelFeed { get; set; }
            public ArbinCommandBrowseDirectoryFeed BrowseDirectoryFeed { get; set; }
            public ArbinCommandStopChannelFeed StopChannelFeed { get; set; }
            public ArbinCommandGetSerialNumberFeed GetSerialNumberFeed { get; set; }
            public ArbinCommandResumChanneleFeed resumChanneleFeed { get; set; }
            public ArbinCommandNewOrDeleteFeed NewOrDeleteFeed { get; set; }
            public ArbinCommandJumpChannelFeed JumpChannelFeed { get; set; }
            public ArbinCommandGetStartDataFeed GetStartDataFeed { get; set; }
            public ArbinCommandGetChannelDataSimpleModeFeed GetChannelDataSimpleModeFeed { get; set; }
            public ArbinCommandGetChannelDataMinimalistModeFeed GetChannelDataMinimalistModeFeed { get; set; }
            public ArbinCommandGetMetaVariablesFeed GetMetaVariablesFeed { get; set; }
            public ArbinCommandSetIntervalTimeLogDataFeed SetIntervalTimeLogDataFeed { get; set; }
            public ArbinCommandCheckFileExFeed checkFileExFeed { get; set; }
            public ArbinCommandAssignFileFeed AssignFileFeed { get; set; }
            public ArbinCommandGetChannelDataFeed GetChannelDataFeed { get; set; }
            public ArbinCommandAssignBarcodeInfoFeed AssignBarcodeInfoFeed { get; set; }
            public ArbinCommandConvertToAnonymousOrNamedTOFeed ConvertTestObjest { get; set; }
        }

        /// <summary>
        /// This class extends the ArbinControl class to handle specific CTI commands and feeds.
        /// </summary>
        public class MyArbinControl : ArbinControl
        {
            public ArbinCommandFeed CommandFeed { get; }

            public AutoResetEvent WaitAutoResetEvent = new AutoResetEvent(false);

            /// <summary>
            /// Constructor for MyArbinControl which initializes the command feed.
            /// </summary>
            /// <param name="commandFeed"></param>
            public MyArbinControl(ArbinCommandFeed commandFeed)
            {
                CommandFeed = commandFeed;
            }

            public override void ExecuteProtocolCmd(IArbinSocket Socket, List<ArbinCommand> Cmds)
            {
                base.ExecuteProtocolCmd(Socket, Cmds);
            }

            public override void OnApplyForUDPCommunicationFeedBack(ArbinCommandApplyForUDPCommunicationFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnAssignFileFeedBack(ArbinCommandAssignFileFeed cmd)
            {
                CommandFeed.AssignFileFeed = cmd;
            }

            public override void OnAssignScheduleFeedBack(ArbinCommandAssignScheduleFeed cmd)
            {
                CommandFeed.ScheduleFeed = cmd;
            }

            public override void OnBrowseDirectoryBack(ArbinCommandBrowseDirectoryFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnCheckFileBack(ArbinCommandCheckFileFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnCheckFileExBack(ArbinCommandCheckFileExFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnContinueChannelFeedBack(ArbinCommandContinueChannelFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnConvertToAnonymousOrNamedTOBack(ArbinCommandConvertToAnonymousOrNamedTOFeed cmd)
            {
                CommandFeed.ConvertTestObjest = cmd;
            }

            public override void OnDeleteFileBack(ArbinCommandDeleteFileFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnDownLoadFileBack(ArbinCommandDownLoadFileFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetChannelsDataFeedBack(ArbinCommandGetChannelDataFeed cmd)
            {
                CommandFeed.GetChannelDataFeed = cmd;
            }

            public override void OnGetChannelsDataMinimalistModeFeedBack(ArbinCommandGetChannelDataMinimalistModeFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetChannelsDataSimpleModeFeedBack(ArbinCommandGetChannelDataSimpleModeFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetMetaVariablesFeedBack(ArbinCommandGetMetaVariablesFeed cmd)
            {
                CommandFeed.GetMetaVariablesFeed = cmd;
            }

            public override void OnGetResumeDataBack(ArbinCommandGetResumeDataFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetSerialNumberFeedBack(ArbinCommandGetSerialNumberFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetStartDataBack(ArbinCommandGetStartDataFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetStatusExtendInformationBack(ArbinCommandGetChannelInfoExFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetTChamberMappingInfoFeedBack(ArbinCommandGetTChamberMappingInfoFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnJumpChannelFeedBack(ArbinCommandJumpChannelFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnLogicConnectFeedBack(ArbinCommandLogicConnectFeed cmd)
            {
                log.Info("OnLogicConnectFeedBack:" + cmd.dwConnectResult);
            }

            public override void OnNewFolderBack(ArbinCommandNewFolderFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnNewOrDeleteBack(ArbinCommandNewOrDeleteFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnResumeChannelFeedBack(ArbinCommandResumChanneleFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnSendMsgToCTIBack(ArbinCommandSendMsgToCTIFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnSetIntervalTimeLogDataFeedBack(ArbinCommandSetIntervalTimeLogDataFeed cmd)
            {
                CommandFeed.SetIntervalTimeLogDataFeed = cmd;
                log.Info("OnSetIntervalTimeLogDataFeedBack:" + cmd.Result.ToString());
            }

            public override void OnSetMetaVariableFeedBack(ArbinCommandSetMetaVariableFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnStartAutomaticCalibrationBack(ArbinCommandStartAutomaticCalibrationFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnStartChannelFeedBack(ArbinCommandStartChannelFeed cmd)
            {
                CommandFeed.StartChannelFeed = cmd;
            }

            public override void OnStopChannelFeedBack(ArbinCommandStopChannelFeed cmd)
            {
                CommandFeed.StopChannelFeed = cmd;
            }

            public override void OnUnknownCommandFeedBack(ArbinCommandUnknownCommandFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnUpdateMetaVariableAdvancedExFeedBack(ArbinCommandUpdateMetaVariableAdvancedExFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnUpdateMetaVariableAdvancedFeedBack(ArbinCommandUpdateMetaVariableAdvancedFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnUpdateParametersBack(ArbinCommandUpdateParameterFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnUpLoadFileBack(ArbinCommandUpLoadFileFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnUserLoginFeedBack(ArbinCommandLoginFeed cmd)
            {
                CommandFeed.LoginFeed = cmd;
            }

            public override void OnGetChannelsDataSPTTFeedBack(ArbinCommandGetChannelDataSPTTFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnAssignBarcodeInfoFeedBack(ArbinCommandAssignBarcodeInfoFeed cmd)
            {
                CommandFeed.AssignBarcodeInfoFeed = cmd;
            }

            public override void OnGetBarcodeInfoFeedBack(ArbinCommandGetBarcodeInfoFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetMachineTypeFeedBack(ArbinCommandGetMachineTypeFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetTrayStatusFeedBack(ArbinCommandGetTrayStatusFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnEngageTrayFeedBack(ArbinCommandEngageTrayFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetStringLimitLengthFeedBack(ArbinCommandGetStringLimitLengthFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetServerSoftwareVersionNumberFeedBack(ArbinCommandGetServerSoftwareVersionNumberFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnTimeSensitiveSetMVFeedBack(ArbinCommandTimeSensitiveSetMVFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnStartChannelAdvancedFeedBack(ArbinCommandStartChannelAdvancedFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetMappingAuxFeedBack(ArbinCommandGetMappingAuxFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnModifyScheduleFeedBack(ArbinCommandModifyScheduleFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGetDeviceUsageInfoFeedBack(ArbinCommandGetDeviceUsageInfoFeed cmd)
            {
                throw new NotImplementedException();
            }

            public override void OnGenerateScheduleFeedBack(ArbinCommandGenerateScheduleFeed cmd)
            {
                throw new NotImplementedException();
            }
        }
    }
}
