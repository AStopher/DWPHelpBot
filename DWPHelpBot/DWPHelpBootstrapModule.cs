using System;
using System.Globalization;
using System.Threading;
using DWPHelpBot.Utilities;
using DWPHelpBot.DatabaseUtils;
using System.Collections.Generic;

namespace DWPHelpBot
{
	public class DWPHelpBotBootstrapModule : SharedModuleMethods
    {
        private const string ver                                    = "0.0.1";
        private const float tickRate                                = 2f;       // Every 30s.
        private const string mandatoryBotMessage                    = "*I am a bot, and this action was performed automatically. Please contact the moderators of r/DWPHelp if you have any questions. Beep-boop.*";

        static DWPHelpBotBootstrapModule instance                   = null;

        private bool systemBooted                                   = false;
        private DateTime nextUpdateInterval;


        DatabaseUtilities databaseUtilityModule                     = null;
        TimedEventModule timedEventModule                           = null;
        StatisticsModule statisticsModule                           = null;

        Dictionary<string, Thread> threads                          = null;
        Dictionary<string, object> modules                          = null;
        string[] bootArgs                                           = null;

        public static void Main(string[] args)
        {
            instance = new DWPHelpBotBootstrapModule();
            instance.EntryPoint(args);
        }

        private void EntryPoint(string[] args)
		{
            logLabel = "DWPHelpBot Bootstrap Module";

            dwpHelpBotBootstrapModule = this;
            nextUpdateInterval = DateTime.Now;
            bootArgs = args;

            // NEW: tick rate is in minutes, NOT seconds. Changed from Wildfire Master code.
            Console.Title = "DWPHelpBot Services Master v" + ver + ", System Refresh: " + tickRate + " ticks per minute.";

            if (args.Length < 2)
            {
                Log("Not enough boot arguments were specified, need [database server address] [database username] [database password]. Cannot continue.");
                return;
            }
            else
            {
                Log("System is initialising.");

                Log(string.Format("MySQL database is {0} with {1}:******.", args[0], args[1]));

                Log(string.Format("Detected {0} available logical processors.", Environment.ProcessorCount));

                modules = new Dictionary<string, object>
                {
                    {"DatabaseUtilityModule", databaseUtilityModule},
                    {"TimedEventModule", timedEventModule},
                    {"StatisticsModule", statisticsModule},
                };

                threads = threads = new Dictionary<string, Thread>();

                threads.Add("DatabaseUtilityModule", new Thread(() => CreateThreadReference<DatabaseUtilities>(bootArgs)));
                threads["DatabaseUtilityModule"].Start();

                while (true)
                {
                    if (systemBooted)
                        ProcessUpdateThread();
                }
            }
        }

        // Executed by the DatabaseUtilityModule.
        public void ContinueSystemBoot()
        {
            foreach (KeyValuePair<string, object> module in modules)
            {
                if(module.Key != "DatabaseUtilityModule")
                {
                    if (module.Key == "TimedEventModule")
                        threads.Add(module.Key, new Thread(() => CreateThreadReference<TimedEventModule>(bootArgs)));
                    else if (module.Key == "StatisticsModule")
                        threads.Add(module.Key, new Thread(() => CreateThreadReference<StatisticsModule>(bootArgs)));

                    threads[module.Key].Start();
                }
            }

            timedEventModule.AddNewTimedEvent(
                new TimedEvent("Heartbeat", new TimeSpan(0, 0, 0, 30, 0), new List<Action> { databaseUtilityModule.SendKeepAlive }, true)
            );

            GetDatabaseUtilityModule().LogToDatabase(
                    "ModuleInitialisation", string.Format("Module \"{0}\" was initialised.", logLabel));

            systemBooted = true;
        }

        private void CreateThreadReference<T>(string[] args = null) where T : SharedModuleMethods, new()
        {
            object targetVar = new T();

            if (targetVar is DatabaseUtilities)
                databaseUtilityModule = (DatabaseUtilities)targetVar;
            else if (targetVar is TimedEventModule)
                timedEventModule = (TimedEventModule)targetVar;
            else if (targetVar is StatisticsModule)
                statisticsModule = (StatisticsModule)targetVar;

            if (targetVar is DatabaseUtilities)
            {
                if (args != null)
                    ((DatabaseUtilities)targetVar).SetupClassSpecialised(this, args);
            }
            else
                ((T)targetVar).SetupClass(this);
        }

        private void ProcessUpdateThread()
        {
            if (nextUpdateInterval <= DateTime.Now)
            {
                nextUpdateInterval = DateTime.Now.AddMilliseconds(1000.0f / tickRate);

                databaseUtilityModule.Update();
                timedEventModule.Update();

                base.Update();
            }
        }


		public void LogToConsole(string message)
		{
            Console.WriteLine(string.Format("DWPHelpBot v" + ver + " | [{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture), message));
            //Replication.ExecuteSQL(@"INSERT INTO `wildfire-core`.`log` (`what`) VALUES ('" + message + "');");
        }

        public void LogErrorToConsole(string message)
		{
			ConsoleColor c = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Error.WriteLine(string.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture), message));
			Console.ForegroundColor = c;
		}

        public DatabaseUtilities GetDatabaseUtilityModule()
        {
            return databaseUtilityModule;
        }

        public TimedEventModule GetTimedEventModule()
        {
            return timedEventModule;
        }
        
        public string GetVersion()
        {
            return ver;
        }
    }
}
