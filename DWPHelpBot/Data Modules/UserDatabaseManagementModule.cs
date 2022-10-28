//using MySql.Data.MySqlClient;
//using System;
//using System.Collections.Generic;
//using Wildfire.Utilities;

//namespace Wildfire.DatabaseUtils
//{
//    class UserDatabaseManagementModule : SharedModuleMethods
//    {
//        private DatabaseUtilities databaseUtils                                     = null;
//        private Dictionary<string, Action<string, bool>> newUserRequests                    = null;
//        private Dictionary<string, Action<string, PlayerData>> playerDataRequests           = null;
//        private Dictionary<string, Action<string, string>> emailRequests                    = null;
//        private Dictionary<string, Action<string, string>> usernameRequests                 = null;
//        private Dictionary<string, Action<string, int>> idRequests                          = null;
//        private Dictionary<string, Action<string, bool>> isBannedRequests                   = null;
//        private Dictionary<string, Action<string, bool>> usernameTakenRequests              = null;
//        private Dictionary<string, Action<string, bool>> passwordMatchRequests              = null;

//        private Dictionary<string, string> ipAddresses                              = null;

//        public UserDatabaseManagementModule(WildfireBootstrapModule _wildfireBootstrapModule, DatabaseUtilities _databaseUtils)
//        {
//            wildfireBootstrapModule = _wildfireBootstrapModule;
//            databaseUtils = _databaseUtils;
//            logLabel = "User Database Management Module";

//            newUserRequests = new Dictionary<string, Action<string, bool>>();
//            playerDataRequests = new Dictionary<string, Action<string, PlayerData>>();
//            emailRequests = new Dictionary<string, Action<string, string>>();
//            usernameRequests = new Dictionary<string, Action<string, string>>();
//            idRequests = new Dictionary<string, Action<string, int>>();
//            isBannedRequests = new Dictionary<string, Action<string, bool>>();
//            usernameTakenRequests = new Dictionary<string, Action<string, bool>>();
//            passwordMatchRequests = new Dictionary<string, Action<string, bool>>();

//            databaseUtils.LogToDatabase("ModuleInitialisation", string.Format("Module \"{0}\" was initialised.", logLabel));
//            Log("Initialised.");
//        }

//        public void GetPlayerData(string username, string ipAddress, Action<string, PlayerData> callback)
//        {
//            string requestId = GenerateRandomRequestId();

//            playerDataRequests.Add(requestId, callback);
//            ipAddresses.Add(requestId, ipAddress);

//            string preparedStatement = 
//                databaseUtils.CreateSelectStatement(
//                    "users", 
//                    new List<string> { 
//                        "id",
//                        "name",
//                        "password",
//                        "email",
//                        "country",
//                        "banned"
//                    }, 
//                    new Dictionary<string, string> { 
//                        { "name", username }
//                    });

//            databaseUtils.ExecuteQuery(preparedStatement, requestId, OnPlayerDataRetrieved);
//        }

//        public void GetPlayerData(string username, string password, string ipAddress, Action<string, PlayerData> callback)
//        {
//            string requestId = GenerateRandomRequestId();

//            playerDataRequests.Add(requestId, callback);
//            ipAddresses.Add(requestId, ipAddress);

//            string preparedStatement =
//                databaseUtils.CreateSelectStatement(
//                    "users",
//                    new List<string> {
//                        "id",
//                        "name",
//                        "password",
//                        "email",
//                        "country",
//                        "banned"
//                    },
//                    new Dictionary<string, string> {
//                        { "name", username },
//                        { "password", password }
//                    });

//             databaseUtils.ExecuteQuery(preparedStatement, requestId, OnPlayerDataRetrieved);
//        }

//        private void OnPlayerDataRetrieved(string requestId, MySqlDataReader reader)
//        {
//            while (reader.Read())
//            {
//                playerDataRequests[requestId].Invoke(requestId, databaseUtils.GetUserManagementModule().AddPlayerToCache(
//                    new PlayerData(
//                        reader.GetInt32(0),
//                        reader.GetInt32(0) * databaseUtils.GetUserManagementModule().GetProfileIdOffset(),
//                        reader.GetString(1),
//                        reader.GetString(2),
//                        reader.GetString(3),
//                        reader.GetString(4),
//                        ipAddresses[requestId],
//                        reader.GetInt32(5) != 0
//                    )
//                ));
//            }

//            playerDataRequests.Remove(requestId);
//            ipAddresses.Remove(requestId);
//        }

//        public void CreateNewUser(PlayerData playerData, Action<string, bool> callback)
//        {
//            string requestId = GenerateRandomRequestId();
//            newUserRequests.Add(requestId, callback);

//            databaseUtils.ExecuteStatement(
//                databaseUtils.CreateInsertStatement(
//                    "users",
//                    new List<string> {
//                        "name",
//                        "email",
//                        "password",
//                        "lastip",
//                        "lastseen",
//                    },
//                    new List<string> {
//                        MySqlHelper.EscapeString(playerData.name),
//                        MySqlHelper.EscapeString(playerData.email),
//                        MySqlHelper.EscapeString(playerData.passwordEncrypted),
//                        playerData.ipAddress,
//                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
//                    }
//                ),
//                requestId,
//                CreateUserResult
//            );
//        }

//        private void CreateUserResult(string requestId, int result)
//        {
//            newUserRequests[requestId]?.Invoke(requestId, result == 1 ? true : false);
//            newUserRequests.Remove(requestId);
//        }

//        public void SetOnline(int accountId, bool online)
//        {
//            databaseUtils.ExecuteStatement(
//                databaseUtils.CreateUpdateStatement(
//                    "users",
//                    "id",
//                    accountId.ToString(),
//                    new List<string> {
//                        "online"
//                    },
//                    new List<string> {
//                        online ? "1" : "0"
//                    }
//                ),
//                "0"
//            );
//        }

//        public void GetEmail(int accountId, Action<string, string> callback)
//        {
//            string requestId = GenerateRandomRequestId();
//            emailRequests.Add(requestId, callback);

//            if (databaseUtils.GetUserManagementModule().GetPlayerDataFromCache(accountId) != null)
//                callback(requestId, databaseUtils.GetUserManagementModule().GetPlayerDataFromCache(accountId).email);

//            databaseUtils.ExecuteQuery(
//                databaseUtils.CreateSelectStatement("users", new List<string> { "email" }, new Dictionary<string, string> { { "id", accountId.ToString() } }),
//                requestId, OnGetEmailResponse
//            );
//        }

//        private void OnGetEmailResponse(string requestId, MySqlDataReader reader)
//        {
//            while (reader.Read())
//                emailRequests[requestId].Invoke(requestId, reader.GetString(0));

//            emailRequests.Remove(requestId);
//        }

//        public void GetUsername(int accountId, Action<string, string> callback)
//        {
//            string requestId = GenerateRandomRequestId();
//            usernameRequests.Add(requestId, callback);

//            if (databaseUtils.GetUserManagementModule().GetPlayerDataFromCache(accountId) != null)
//                callback(requestId, databaseUtils.GetUserManagementModule().GetPlayerDataFromCache(accountId).name);

//            databaseUtils.ExecuteQuery(
//                databaseUtils.CreateSelectStatement("users", new List<string> { "name" }, new Dictionary<string, string> { { "id", accountId.ToString() } }),
//                requestId, OnGetUsernameResponse
//            );
//        }

//        private void OnGetUsernameResponse(string requestId, MySqlDataReader reader)
//        {
//            while (reader.Read())
//                usernameRequests[requestId].Invoke(requestId, reader.GetString(0));

//            usernameRequests.Remove(requestId);
//        }

//        public void GetId(string email, Action<string, int> callback)
//        {
//            string requestId = GenerateRandomRequestId();
//            idRequests.Add(requestId, callback);

//            if (databaseUtils.GetUserManagementModule().GetPlayerDataFromCache(email) != null)
//                callback(requestId, databaseUtils.GetUserManagementModule().GetPlayerDataFromCache(email).id);

//            databaseUtils.ExecuteQuery(databaseUtils.CreateSelectStatement("users", new List<string> { "id" }, new Dictionary<string, string> { { "email", email } }),
//                requestId, OnGetIdResponse);
//        }

//        private void OnGetIdResponse(string requestId, MySqlDataReader reader)
//        {
//            while (reader.Read())
//                idRequests[requestId].Invoke(requestId, reader.GetInt32(0));

//            idRequests.Remove(requestId);
//        }

//        public void IsBanned(int accountId, Action<string, bool> callback, string requestId)
//        {
//            isBannedRequests.Add(requestId, callback);

//            databaseUtils.ExecuteQuery(
//                databaseUtils.CreateSelectStatement(
//                    "users", new List<string> { "banned" }, new Dictionary<string, string> { { "id", accountId.ToString() } }),
//                requestId, OnIsBannedResponse);
//        }

//        public void IsBanned(string email, Action<string, bool> callback)
//        {
//            string requestId = GenerateRandomRequestId();
//            isBannedRequests.Add(requestId, callback);

//            databaseUtils.ExecuteQuery(
//                databaseUtils.CreateSelectStatement(
//                    "users", new List<string> { "banned" }, new Dictionary<string, string> { { "email", email } }),
//                requestId, OnIsBannedResponse);
//        }

//        private void OnIsBannedResponse(string requestId, MySqlDataReader reader)
//        {
//           isBannedRequests[requestId].Invoke(requestId, reader.HasRows);
//           isBannedRequests.Remove(requestId);
//        }

//        public void UsernameTaken(string username, Action<string, bool> callback)
//        {
//            string requestId = GenerateRandomRequestId();
//            usernameTakenRequests.Add(requestId, callback);

//            databaseUtils.ExecuteQuery(
//                databaseUtils.CreateSelectStatement(
//                    "users", new List<string> { "username" }, new Dictionary<string, string> { { "username", username } }),
//                requestId, OnUsernameTakenResponse);
//        }

//        private void OnUsernameTakenResponse(string requestId, MySqlDataReader reader)
//        {
//            usernameTakenRequests[requestId].Invoke(requestId, reader.HasRows);
//            usernameTakenRequests.Remove(requestId);
//        }

//        public void CheckForPasswordMatch(int id, string passwordEncrypted, Action<string, bool> callback, string requestId)
//        {
//            passwordMatchRequests.Add(requestId, callback);

//            databaseUtils.ExecuteQuery(
//                databaseUtils.CreateSelectStatement(
//                    "users", new List<string> { "id" }, new Dictionary<string, string> { { "password", passwordEncrypted }, { "id", id.ToString() } }),
//                requestId, OnPasswordMatchResponse);
//        }

//        private void OnPasswordMatchResponse(string requestId, MySqlDataReader reader)
//        {
//            passwordMatchRequests[requestId].Invoke(requestId, reader.HasRows);
//            passwordMatchRequests.Remove(requestId);
//        }

//        // TODO
//        public void AddServerToUserHistory()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}