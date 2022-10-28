using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DWPHelpBot.Utilities;
using DWPHelpBot.DatabaseUtils;

namespace DWPHelpBot.Utilities
{
    public class RedditDataModule : SharedModuleMethods
    {
        public const string clientId = "6NzXSdI9dA-9PI5y4dpPxw";
        private const string apiEndpoint = "https://www.reddit.com/api/v1/";
        public readonly string userAgent;
        private DatabaseUtilities databaseUtils;

        public RedditDataModule(DWPHelpBotBootstrapModule _dwpHelpBotBootstrapModule, DatabaseUtilities _databaseUtils)
        {
            dwpHelpBotBootstrapModule = _dwpHelpBotBootstrapModule;
            databaseUtils = _databaseUtils;

            userAgent = string.Format("User-Agent: dotnet:com.alexanderstopher.dwphelpbot:v{0} (by /u/MGNConflict, for r/DWPHelp)", dwpHelpBotBootstrapModule.GetVersion());

            logLabel = "Reddit Data Management Module";
            databaseUtils.LogToDatabase("ModuleInitialisation", string.Format("Module \"{0}\" was initialised.", logLabel));

            Log("Initialised.");
        }

        public string GetApiEndpoint()
        {
            return apiEndpoint;
        }
    }
}