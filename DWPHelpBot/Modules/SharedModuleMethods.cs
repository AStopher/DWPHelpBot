using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWPHelpBot.Utilities
{
    public class SharedModuleMethods
    {
        protected DWPHelpBotBootstrapModule dwpHelpBotBootstrapModule = null;
        protected Random random = null;
        protected string logLabel = "Unknown";

        public virtual void SetupClass(DWPHelpBotBootstrapModule _dwpHelpBotBootstrapModule)
        {
            dwpHelpBotBootstrapModule = _dwpHelpBotBootstrapModule;
            random = new Random();

            dwpHelpBotBootstrapModule.GetDatabaseUtilityModule().LogToDatabase("ModuleInitialisation", string.Format("Module \"{0}\" was initialised.", logLabel));
            Log("Initialised.");
        }

        public virtual void SetupClassSpecialised(DWPHelpBotBootstrapModule _dwpHelpBotBootstrapModule, string[] args)
        {
            dwpHelpBotBootstrapModule = _dwpHelpBotBootstrapModule;

            dwpHelpBotBootstrapModule.GetDatabaseUtilityModule().LogToDatabase("ModuleInitialisation", string.Format("Module \"{0}\" was initialised.", logLabel));
            Log("Initialised.");
        }

        public virtual void Update() { }

        public string GenerateRandomRequestId()
        {
            return random.Next(0, 100000000).ToString();
        }

        protected void Log(string message)
        {
            dwpHelpBotBootstrapModule.LogToConsole(string.Format("[ {0} ] {1}", logLabel, message));
        }

        protected void LogError(string message)
        {
            dwpHelpBotBootstrapModule.LogErrorToConsole(string.Format("[ {0} ] {1}", logLabel, message));
        }
    }
}