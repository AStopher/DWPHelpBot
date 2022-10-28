using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWPHelpBot.Utilities
{
    class StatisticsModule : SharedModuleMethods
    {
        private int dailyActiveUsers                                = 0;

        private TimedEvent dailyStatsEvent                          = null;

        public override void SetupClass(DWPHelpBotBootstrapModule _dwpHelpBotBootstrapModule)
        {
            logLabel = "Statistics Module";
            base.SetupClass(_dwpHelpBotBootstrapModule);

            dailyStatsEvent = dwpHelpBotBootstrapModule.GetTimedEventModule().AddNewTimedEvent(
                new TimedEvent("ProcessDailyStatistics", GetTimeUntilMidnight(), new List<Action> { ProcessDailyStatistics }));

            Log(string.Format(
                "Initialised, {0}h:{1}m:{2}s until next run of daily stats events.", GetTimeUntilMidnight().Hours, GetTimeUntilMidnight().Minutes, GetTimeUntilMidnight().Seconds));
        }

        public void NoteActiveUser()
        {
            dailyActiveUsers++;
        }

        private void ProcessDailyStatistics()
        {
            //wildfireBootstrapModule.GetDatabaseUtilityModule().ExecuteStatement(
            //    wildfireBootstrapModule.GetDatabaseUtilityModule().CreateInsertStatement(
            //        "dau",
            //        new List<string> {
            //            "value",
            //            "date",
            //            "assigned"
            //        },
            //        new List<string> {
            //            dailyActiveUsers.ToString(),
            //            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            //            "Wildfire Core User Management Module"
            //        }
            //    ),
            //    "0"
            //);

            dailyStatsEvent.ChangeTriggerFrequency(GetTimeUntilMidnight());

            ResetDailyStatCounters();
        }

        private void ResetDailyStatCounters()
        {
            dailyActiveUsers = 0;
        }

        private TimeSpan GetTimeUntilMidnight()
        {
            DateTime timeNow = DateTime.Now;
            DateTime midnight = timeNow.AddDays(1).Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            return midnight - timeNow;
        }
    }
}
