//using MySql.Data.MySqlClient;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Wildfire.Utilities;

//namespace Wildfire.DatabaseUtils
//{
//    public class UserManagementModule : SharedModuleMethods
//    {
//        private readonly int profileIdOffset                                            = 100000000;
//        private DatabaseUtilities databaseUtils                                         = null;
//        private UserDatabaseManagementModule userDatabaseInterface                      = null;
//        private GeographicalDataModule geographicalDataModule                           = null;
//        private List<PlayerData> cachedPlayerData                                       = new List<PlayerData>();

//        private Dictionary<string, PlayerData> playerDataPasswordCheckStorage           = null;
//        private Dictionary<string, PlayerData> playerDataBannedCheckStorage             = null;

//        public UserManagementModule(WildfireBootstrapModule _wildfireBootstrapModule, DatabaseUtilities _databaseUtils)
//        {
//            wildfireBootstrapModule = _wildfireBootstrapModule;
//            databaseUtils = _databaseUtils;
//            userDatabaseInterface = new UserDatabaseManagementModule(_wildfireBootstrapModule, _databaseUtils);
//            geographicalDataModule = _wildfireBootstrapModule.GetGeographicalDataModule();

//            logLabel = "User Management Module";
//            databaseUtils.LogToDatabase("ModuleInitialisation", string.Format("Module \"{0}\" was initialised.", logLabel));

//            Log("Initialised.");
//        }

//        public void MaintainPlayerDataCache()
//        {
//            int cacheExpiryHours = 24;
//            int onlineExpiryMinutes = 5;
//            string requestId;

//            cachedPlayerData.ToList().ForEach(player =>
//            {
//                requestId = GenerateRandomRequestId();

//                if ((player.lastSeen - DateTime.Now).TotalHours >= cacheExpiryHours)
//                    cachedPlayerData.Remove(player);

//                if ((player.lastSeen - DateTime.Now).TotalMinutes >= onlineExpiryMinutes)
//                    userDatabaseInterface.SetOnline(player.id, false);

//                userDatabaseInterface.CheckForPasswordMatch(player.id, player.passwordEncrypted, CacheCheckPasswordMatch, requestId);
//                playerDataPasswordCheckStorage.Add(requestId, player);

//                userDatabaseInterface.IsBanned(player.id, CacheCheckIsBanned, requestId);
//            });
//        }

//        public void CacheCheckPasswordMatch(string requestId, bool result)
//        {
//            if (!result)
//                cachedPlayerData.Remove(playerDataPasswordCheckStorage[requestId]);

//            playerDataPasswordCheckStorage.Remove(requestId);
//        }

//        public void CacheCheckIsBanned(string requestId, bool result)
//        {
//            if (result != playerDataBannedCheckStorage[requestId].isBanned)
//                cachedPlayerData[cachedPlayerData.IndexOf(playerDataBannedCheckStorage[requestId])].isBanned = result;

//            playerDataBannedCheckStorage.Remove(requestId);
//        }

//        public PlayerData AddPlayerToCache(PlayerData player)
//        {
//            if (GetPlayerDataFromCache(player.id) != null)
//                return GetPlayerDataFromCache(player.id);

//            cachedPlayerData.Add(player);

//            player.lastSeen = DateTime.Now;
//            userDatabaseInterface.SetOnline(player.id, true);

//            return player;
//        }

//        public PlayerData GetPlayerDataFromCache(int _id)
//        {
//            return cachedPlayerData.Find(player => player.id == _id);
//        }

//        public PlayerData GetPlayerDataFromCache(string _email)
//        {
//            return cachedPlayerData.Find(player => player.email == _email);
//        }

//        public int GetProfileIdOffset()
//        {
//            return profileIdOffset;
//        }
//    }
//}