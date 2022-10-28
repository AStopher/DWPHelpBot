using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using DWPHelpBot.Utilities;

namespace DWPHelpBot.DatabaseUtils
{
    public class DatabaseUtilities : SharedModuleMethods
    {
        #region Singleton

        private static DatabaseUtilities _instance                          = null;

        #endregion

        #region Variables

#if IS_DEBUG
        private readonly string databasePort = "3306";
#else
        private readonly string databasePort = "5509";
#endif

        private List<MySQLQueueItem> queue                                      = null;
        private List<Thread> mySQLConnectionThreads                             = null;
        internal List<MySQLQueue> mySQLThreadQueue                              = null;

        #endregion

        #region MySQL Queue

        internal class MySQLQueueItem
        {
            public readonly bool isQuery                                                = false;
            public readonly string preparedStatement                                    = null;
            public readonly string requestId                                            = null;
            public readonly Action<string, int> statementExecutionResultCallback        = null;
            public readonly Action<string, MySqlDataReader> queryExecutionResultCallback= null;

            public MySQLQueueItem(bool _isQuery, string _preparedStatement, string _requestId,
                Action<string, int> _statementExecutionResultCallback = null, Action<string, MySqlDataReader> _queryExecutionResultCallback = null)
            {
                isQuery = _isQuery;
                preparedStatement = _preparedStatement;
                statementExecutionResultCallback = _statementExecutionResultCallback;
                queryExecutionResultCallback = _queryExecutionResultCallback;
                requestId = _requestId;
            }
        }

        internal class MySQLQueue
        {
            private readonly DatabaseUtilities databaseUtilities                = null;
            private readonly int workerId                                       = -1;

            public readonly MySqlConnection mySQLConnection                     = null;
            public bool isAvailable                                             { get; private set; }
            public bool workerExecuting                                         = false; // Same purpose as lock/unlock guards in C++.

            public MySQLQueue(DatabaseUtilities _databaseUtilities, string _databaseServer, string _databasePort, string _databaseUser, string _databasePassword, int _workerId)
            {
                // Ensure thread safety by making a copy in this thread.
                string databaseServer = _databaseServer;
                string databasePort = _databasePort;
                string databaseUser = _databaseUser;
                string databasePassword = _databasePassword;

                databaseUtilities = _databaseUtilities;
                workerId = _workerId;
                databaseUtilities.mySQLThreadQueue.Add(this);

                databaseUtilities.Log(string.Format("Worker {0} initialising.", workerId));

                try
                {
                    mySQLConnection =
                        new MySqlConnection(
                            string.Format("Server={0}; Port={1}; Database=wildfire-core; Uid={2}; Pwd={3};", databaseServer, databasePort, databaseUser, databasePassword));

                    mySQLConnection.StateChange += OnStateChanged;
                    mySQLConnection.Open();
                    databaseUtilities.Log(string.Format("Worker {0} ready.", workerId));
                }
                catch (MySqlException ex)
                {
                    databaseUtilities.LogError("Failed to connect to MySQL server: " + ex.Message);
                    databaseUtilities.Log(string.Format("Worker {0} failed to initialise and will not be available for use.", workerId));
                }
            }
            
            ~MySQLQueue()
            {
                databaseUtilities.Log(string.Format("Worker {0} shutting down.", workerId));

                isAvailable = false;
                mySQLConnection.StateChange -= OnStateChanged;
                mySQLConnection.Close();

                /* Stopping the thread is what should be calling this destructor, so no need to Stop()
                 *  the thread here (we just need to remove it from the worker pool).
                 */ 

                databaseUtilities.mySQLConnectionThreads.Remove(databaseUtilities.mySQLConnectionThreads[workerId]);
            }

            private void OnStateChanged(object sender, StateChangeEventArgs stateChangedArgs)
            {
                if ((stateChangedArgs.CurrentState == ConnectionState.Open) && !isAvailable)
                    isAvailable = true;
            }

            public void ExecuteStatement(string preparedStatement, string requestId, Action<string, int> callback = null)
            {
                if (workerExecuting)
                {
                    databaseUtilities.LogError(
                        string.Format("WARNING - worker {0} tried to execute a cross-thread query when the worker was already executing! This was prevented.", workerId));
                    return;
                }

                workerExecuting = true;

                if (callback != null)
                {
                    callback?.Invoke(requestId, new MySqlCommand(preparedStatement, mySQLConnection).ExecuteNonQuery());
                    workerExecuting = false;
                }
                else
                {
                    new MySqlCommand(preparedStatement, mySQLConnection).ExecuteNonQuery();
                    workerExecuting = false;
                }
            }

            public void ExecuteQuery(string preparedStatement, string requestId, Action<string, MySqlDataReader> callback = null)
            {
                if (workerExecuting)
                {
                    databaseUtilities.LogError(
                        string.Format("WARNING - worker {0} tried to execute a cross-thread query when the worker was already executing! This was prevented.", workerId));
                    return;
                }

                workerExecuting = true;

                MySqlDataReader reader = new MySqlCommand(preparedStatement, mySQLConnection).ExecuteReader();

                callback?.Invoke(requestId, reader);
                reader.Close();

                workerExecuting = false;
            }
        }

        #endregion 
        
        public DatabaseUtilities()
        {
            _instance = this;
        }

        ~DatabaseUtilities()
        {
            mySQLConnectionThreads.ForEach(mySQLThread => { mySQLThread.Abort(0); });
        }

        public override void SetupClassSpecialised(DWPHelpBotBootstrapModule _dwpHelpBotBootstrapModule, string[] args)
        {
            dwpHelpBotBootstrapModule = _dwpHelpBotBootstrapModule;
            logLabel = "Database Utility Module";

            mySQLConnectionThreads = new List<Thread>();
            mySQLThreadQueue = new List<MySQLQueue>();
            queue = new List<MySQLQueueItem>();

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                int tmp = i;
                mySQLConnectionThreads.Add(new Thread(() => new MySQLQueue(this, args[1], databasePort, args[2], args[3], tmp)));
                mySQLConnectionThreads[i].Start();
            }

            dwpHelpBotBootstrapModule.ContinueSystemBoot();
        }

        public void ExecuteStatement(string preparedStatement, string requestId, Action<string, int> callback = null)
        {
            queue.Add(new MySQLQueueItem(false, preparedStatement, requestId, callback));
        }

        public void ExecuteQuery(string preparedStatement, string requestId, Action<string, MySqlDataReader> callback = null)
        {
            queue.Add(new MySQLQueueItem(true, preparedStatement, requestId, null, callback));
        }

        public string CreateUpdateStatement(string tableName, string filterColumnName, string filterColumnValue, List<string> fieldNames, List<string> fieldValues)
        {
            int n = 0;
            fieldValues.ToList().ForEach(value =>
            {
                fieldValues[n] = "\"" + MySqlHelper.EscapeString(fieldValues[n]) + "\"";
                n++;
            });

            return string.Format("UPDATE `dwphelpbot`.`{0}` WHERE `{1}`='{2}' SET ({3}) VALUES ({4})", 
                                    tableName, filterColumnName, filterColumnValue, string.Join(",", fieldNames), string.Join(",", fieldValues));
        }

        public string CreateInsertStatement(string tableName, List<string> fieldNames, List<string> fieldValues)
        {
            int n = 0;
            fieldValues.ToList().ForEach(value =>
            {
                fieldValues[n] = "\"" + MySqlHelper.EscapeString(fieldValues[n]) + "\"";
                n++;
            });

            return string.Format("INSERT INTO `dwphelpbot`.`{0}` ({1}) VALUES ({2})",
                                    tableName, string.Join(",", fieldNames), string.Join(",", fieldValues));
        }

        public string CreateSelectStatement(string tableName, List<string> values, Dictionary<string, string> filters)
        {
            string whereClause = "";

            filters.ToList().ForEach( item => 
            {
                whereClause += string.Format("`{0}`='{1}'", item.Key, MySqlHelper.EscapeString(item.Value));
            });

            return string.Format("SELECT {0} FROM `dwphelpbot`.`{1}` WHERE {2};", string.Join(",", values), tableName, whereClause);
        }

        public void LogToDatabase(string eventName, string message)
        {
            ExecuteStatement(
                CreateInsertStatement(
                    "log",
                    new List<string> {
                        "event",
                        "product",
                        "notes",
                        "occurance",
                        "assigned"
                    },
                    new List<string> {
                        MySqlHelper.EscapeString(eventName),
                        "WildfireCore",
                        MySqlHelper.EscapeString(message),
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        "DWPHelpBot System Service"
                    }
                ),
                "0"
            );
        }

        public void SendKeepAlive()
        {
            ExecuteStatement(
                CreateInsertStatement(
                    "log",
                    new List<string> {
                        "event",
                        "product",
                        "notes",
                        "occurance",
                        "assigned"
                    },
                    new List<string> {
                        "Heartbeat",
                        "DWPHelpBot",
                        "Scheduled check-in.",
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        "DWPHelpBot System Service"
                    }
                ),
                "0"
            );
        }

        public override void Update()
        {
            int n = 0;

            mySQLThreadQueue.ForEach(queueItem =>
            {
                if (queue.Count > 0 && queueItem.isAvailable)
                {
                    if (!queueItem.workerExecuting && queueItem.mySQLConnection.State == System.Data.ConnectionState.Open &&
                            mySQLConnectionThreads[n].ThreadState == ThreadState.Running)
                    {
                        /*
                         * Need to make copies because we're accessing another thread below, and as C# passes by reference by default we can't guarantee
                         *      the variables will still exist in the same state.
                         */

                        string requestId = queue[0].requestId;
                        string preparedStatement = queue[0].preparedStatement;
                        Action<string, MySqlDataReader> queryExecutionResultCallback = queue[0].queryExecutionResultCallback;
                        Action<string, int> statementExecutionResultCallback = queue[0].statementExecutionResultCallback;

                        if (queue[0].isQuery)
                            queueItem.ExecuteQuery(preparedStatement, requestId, queryExecutionResultCallback);
                        else
                            queueItem.ExecuteStatement(preparedStatement, requestId, statementExecutionResultCallback);

                        queue.Remove(queue[0]);
                    }
                }

                n++;
            });

            base.Update();
        }
    }
}