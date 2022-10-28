using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWPHelpBot.Utilities
{
    public class TimedEventModule : SharedModuleMethods
    {
        private List<TimedEvent> timedEvents = new List<TimedEvent>();

        public override void SetupClass(DWPHelpBotBootstrapModule _dwpHelpBotBootstrapModule)
        {
            logLabel = "Timed Event Module";
            base.SetupClass(_dwpHelpBotBootstrapModule);
        }

        public TimedEvent AddNewTimedEvent(TimedEvent timedEvent)
        {
            timedEvents.Add(timedEvent);
            
            if (!timedEvent.GetConsoleMessagesSupressed())
            {
                dwpHelpBotBootstrapModule.GetDatabaseUtilityModule().LogToDatabase(
                        "TimedEventAdded", string.Format("Timed event \"{0}\" was added.", timedEvent.GetName()));
                Log(string.Format("Timed event \"{0}\" was added.", timedEvent.GetName()));
            }

            return timedEvent;
        }

        public void RemoveTimedEvent(TimedEvent timedEvent)
        {
            timedEvents.Remove(timedEvent);
            if (!timedEvent.GetConsoleMessagesSupressed())
            {
                dwpHelpBotBootstrapModule.GetDatabaseUtilityModule().LogToDatabase(
                        "TimedEventRemoved", string.Format("Timed event \"{0}\" was removed.", timedEvent.GetName()));
                Log(string.Format("Event \"{0}\" was successfully removed.", timedEvent.GetName()));
            }  
        }

        public void RemoveTimedEvent(string _eventName)
        {
            TimedEvent foundEvent = timedEvents.Find( timedEvent => timedEvent.GetName() == _eventName );

            if (foundEvent != null)
            {
                timedEvents.Remove(foundEvent);
                if (!foundEvent.GetConsoleMessagesSupressed())
                {
                    dwpHelpBotBootstrapModule.GetDatabaseUtilityModule().LogToDatabase(
                        "TimedEventRemoved", string.Format("Timed event \"{0}\" was removed.", foundEvent.GetName()));
                    Log(string.Format("Event \"{0}\" was successfully removed.", _eventName));
                }
            }
            else
            {
                if (!foundEvent.GetConsoleMessagesSupressed())
                {
                    LogError(string.Format("A call was made to remove an event named \"{0}\", but no such event was found!", _eventName));
                    dwpHelpBotBootstrapModule.GetDatabaseUtilityModule().LogToDatabase(
                            "TimedEventError", string.Format("A call was made to remove an event named \"{0}\", but no such event exists.", _eventName));
                }
            }
        }

        public override void Update()
        {
            timedEvents.ForEach(timedEvent =>
            {
                if (timedEvent.GetDateTimeOfNextRun() <= DateTime.Now)
                {
                    if (!timedEvent.GetConsoleMessagesSupressed())
                        Log(string.Format("Executing timed event \"{0}\".", timedEvent.GetName()));

                    timedEvent.Run();
                    if (!timedEvent.GetConsoleMessagesSupressed())
                        dwpHelpBotBootstrapModule.GetDatabaseUtilityModule().LogToDatabase(
                            "TimedEventExecution", string.Format("Event {0} was executed successfully.", timedEvent.GetName()));
                }
            });

            base.Update();
        }
    }
}
