
using System;
using System.Data;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace Util
{
    public class HelperDAO<T> where T : new()
    {
        //Delegate types
        public delegate string SqlBuilder(HelperDAO<T> helper);
        public delegate void SqlReader(HelperDAO<T> helper);

        //Fields
        private MySqlDataReader _reader;
        private T _data;
        private bool hasTransaction;
        private MySqlConnection transactionalConnection;
        private MySqlCommand transactionalCommand;
        
        //Properties
        public T data { get => _data; set => _data = value; }
        public MySqlDataReader reader { get => _reader; private set => _reader = value; }

        //Constructor
        public HelperDAO(T data)
        {
            this.data = data;
        }

        //Finalizer
        ~HelperDAO()
        {
            if (transactionalCommand != null)
            {
                transactionalCommand.Dispose();
            }
            
            if (transactionalConnection != null && transactionalConnection.State != ConnectionState.Closed)
            {
                transactionalConnection.Close();
            }
        }


        ////// Public Methods  //////

        public bool Find(SqlBuilder sqlBuilder, SqlReader sqlReader)
        {
            Func<MySqlCommand, bool> application = delegate (MySqlCommand command)
            {
                try
                {
                    command.CommandText = sqlBuilder(this);
                    this.reader = command.ExecuteReader();

                    if (this.reader.Read())
                    {
                        sqlReader(this);
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            };

            return UseCommand(application);
        }

        public List<T> FindAll(SqlBuilder sqlBuilder, SqlReader sqlReader)
        {
            Func<MySqlCommand, List<T>> application = delegate (MySqlCommand command)
            {
                //Backup original data state.
                T backup = this.data;

                try
                {
                    command.CommandText = sqlBuilder(this);
                    this.reader = command.ExecuteReader();

                    List<T> list = new List<T>();

                    while (this.reader.Read())
                    {
                        this.data = new T();
                        sqlReader(this);
                        list.Add(this.data);
                    }

                    return list;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    //Restore original data state.
                    this.data = backup;
                }
            };

            return UseCommand(application);
        }

        public int Insert(SqlBuilder sqlBuilder)
        {
            Func<MySqlCommand, int> application = GetSingleApplication(sqlBuilder);
            return hasTransaction ? UseTransactionalCommand(application) : UseCommand(application);
        }

        public int Insert(SqlBuilder sqlBuilder, List<T> dataList)
        {
            Func<MySqlCommand, int> application = GetIterativeApplication(sqlBuilder, dataList);
            return hasTransaction ? UseTransactionalCommand(application) : UseCommand(application, transactional: true);
        }

        public int Update(SqlBuilder sqlBuilder, bool secureMode = true)
        {
            Func<MySqlCommand, int> application = GetSingleApplication(sqlBuilder, secureMode);
            return hasTransaction ? UseTransactionalCommand(application) : UseCommand(application);
        }

        public int Update(SqlBuilder sqlBuilder, List<T> dataList, bool secureMode = true)
        {
            Func<MySqlCommand, int> application = GetIterativeApplication(sqlBuilder, dataList, secureMode);
            return hasTransaction ? UseTransactionalCommand(application) : UseCommand(application, transactional: true);
        }

        public int Delete(SqlBuilder sqlBuilder, bool secureMode = true)
        {
            Func<MySqlCommand, int> application = GetSingleApplication(sqlBuilder, secureMode);
            return hasTransaction ? UseTransactionalCommand(application) : UseCommand(application);
        }

        public int Delete(SqlBuilder sqlBuilder, List<T> dataList, bool secureMode = true)
        {
            Func<MySqlCommand, int> application = GetIterativeApplication(sqlBuilder, dataList, secureMode);
            return hasTransaction ? UseTransactionalCommand(application) : UseCommand(application, transactional: true);
        }

        public U TryGetField<U>(Func<int, U> fieldReader, string columnName)
        {
            try
            {
                if (!this.reader.IsDBNull(this.reader.GetOrdinal(columnName)))
                {
                    return fieldReader(this.reader.GetOrdinal(columnName));
                }
                else
                {
                    throw new Exception("Column '" + columnName + "' field is null.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void BeginTransaction()
        {
            if (!hasTransaction)
            {
                hasTransaction = true;
                transactionalConnection = new DBConnect().Connection;
                transactionalConnection.Open();
                transactionalCommand = new MySqlCommand(null, transactionalConnection, transactionalConnection.BeginTransaction());
            }
        }

        public void Commit()
        {
            if (!hasTransaction)
            {
                throw new Exception("Transaction has not started.");
            }

            try
            {
                transactionalCommand.Transaction.Commit();
            }
            catch (Exception ex)
            {
                try
                {
                    transactionalCommand.Transaction.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (Exception ex2)
                {
                    throw new Exception(ex2.Message);
                }
            }
            finally
            {
                hasTransaction = false;

                if (transactionalCommand != null)
                {
                    transactionalCommand.Dispose();
                }
                
                if (transactionalConnection != null && transactionalConnection.State != ConnectionState.Closed)
                {
                    transactionalConnection.Close();
                }
            }
        }

        public void Rollback()
        {
            if (!hasTransaction)
            {
                throw new Exception("Transaction has not started.");
            }

            try
            {
                transactionalCommand.Transaction.Rollback();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                hasTransaction = false;

                if (transactionalCommand != null)
                {
                    transactionalCommand.Dispose();
                }
                
                if (transactionalConnection != null && transactionalConnection.State != ConnectionState.Closed)
                {
                    transactionalConnection.Close();
                }
            }
        }

        
        ////// Private Methods  //////

        private U UseCommand<U>(Func<MySqlCommand, U> application, bool transactional = false)
        {
            using (MySqlConnection connection = new DBConnect().Connection)
            using (MySqlCommand command = new MySqlCommand(null, connection))
            {
                connection.Open();
                
                if (transactional)
                {
                    try
                    {
                        command.Transaction = connection.BeginTransaction();
                        U applicationReturn = application(command);
                        command.Transaction.Commit();

                        return applicationReturn;
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            command.Transaction.Rollback();
                            throw new Exception(ex.Message);
                        }
                        catch (Exception ex2)
                        {
                            throw new Exception(ex2.Message);
                        }
                    }

                }

                return application(command);
            }
        }

        private int UseTransactionalCommand(Func<MySqlCommand, int> application)
        {
            return application(transactionalCommand);
        }

        private Func<MySqlCommand, int> GetSingleApplication(SqlBuilder sqlBuilder, bool secureMode = false)
        {
            return delegate (MySqlCommand command)
            {
                try
                {
                    command.CommandText = sqlBuilder(this);

                    if (secureMode)
                    {
                        ValidateSensitiveSql(command.CommandText);
                    }

                    return command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            };
        }

        private Func<MySqlCommand, int> GetIterativeApplication(SqlBuilder sqlBuilder, List<T> dataList, bool secureMode = false)
        {
            return delegate (MySqlCommand command)
            {
                //Backup original data state.
                T backup = this.data;

                try
                {
                    int affectRows = 0;

                    foreach (T data in dataList)
                    {
                        this.data = data;
                        command.CommandText = sqlBuilder(this);

                        if (secureMode)
                        {
                            ValidateSensitiveSql(command.CommandText);
                        }

                        affectRows += command.ExecuteNonQuery();
                    }

                    return affectRows;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    //Restore original data state.
                    this.data = backup;
                }
            };
        }

        private void ValidateSensitiveSql(string sql)
        {
            string pattern = @"[ ]*" +
                             @"([uU][pP][dD][aA][tT][eE]\s+.+\s+[sS][eE][tT]|" +
                             @"[dD][eE][lL][eE][tT][eE]\s+[fF][rR][oO][mM])" +
                             @"\s+" +
                             @".+" +
                             @"\s+" +
                             @"[wW][hH][eE][rR][eE]" +
                             @"\s+" +
                             @".+" +
                             @"[^']" +
                             @"(=|<>|!=|>|<|>=|<=|" +
                             @"\s+([ ]*|[nN][oO][tT]\s+)[bB][eE][tT][wW][eE][eE][nN]\s+|" +
                             @"\s+([ ]*|[nN][oO][tT]\s+)[lL][iI][kK][eE]\s+|" +
                             @"\s+([ ]*|[nN][oO][tT]\s+)[iI][nN][ ]*[\(])";
            
            MatchCollection matches = Regex.Matches(sql.Replace('\n', ' ').Replace('\r', ' '), pattern);

            if (matches.Count == 0)
            {
                throw new Exception("Non secure SQL: [" + sql + "]");
            }
        }
    }
}
