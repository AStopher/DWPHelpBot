using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWPHelpBot.Utilities
{
    public class TimedEvent
    {
        private readonly string eventName                       = null;
        private readonly List<Action> triggers                  = null;
        private readonly bool supressConsoleMessages            = false;

        private TimeSpan triggerFrequency;
        private DateTime lastRun;
        private DateTime nextRun;


        public TimedEvent(string _eventName, TimeSpan _frequency, List<Action> _triggers, bool _supressConsoleMessages = false)
        {
            eventName = _eventName;
            triggerFrequency = _frequency;
            triggers = _triggers;
            lastRun = DateTime.MinValue;
            supressConsoleMessages = _supressConsoleMessages;

            nextRun = DateTime.Now.Add(triggerFrequency);
        }

        public void Run()
        {
            lastRun = DateTime.Now;
            nextRun = DateTime.Now.Add(triggerFrequency);

            triggers.ForEach(trigger =>
            {
                trigger?.Invoke();
            });
        }

        public DateTime GetDateTimeOfNextRun()
        {
            return nextRun;
        }

        public DateTime GetDateTimeOfLastRun()
        {
            return lastRun;
        }

        public void ChangeTriggerFrequency(TimeSpan newFrequency)
        {
            triggerFrequency = newFrequency;
        }

        public string GetName()
        {
            return eventName;
        }

        public TimeSpan GetTriggerFrequency()
        {
            return triggerFrequency;
        }

        public bool GetConsoleMessagesSupressed()
        {
            return supressConsoleMessages;
        }
    }
}
