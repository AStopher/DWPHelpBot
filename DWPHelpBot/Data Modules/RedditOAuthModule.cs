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
    public class RedditOAuthModule : SharedModuleMethods
    {
        private readonly string redirectEndpoint = "/auth";
        private readonly int localServerPort = 7708;
        private DatabaseUtilities databaseUtils;

        public RedditOAuthModule(DWPHelpBotBootstrapModule _dwpHelpBotBootstrapModule, DatabaseUtilities _databaseUtils)
        {
            dwpHelpBotBootstrapModule = _dwpHelpBotBootstrapModule;
            databaseUtils = _databaseUtils;

            logLabel = "Reddit Open Authentication Management Module";
            databaseUtils.LogToDatabase("ModuleInitialisation", string.Format("Module \"{0}\" was initialised.", logLabel));

            Log("Initialised.");
        }

        public void StartServer()
        {
            
        }

        public void AttemptAuthorisation()
        {

        }
    }
}