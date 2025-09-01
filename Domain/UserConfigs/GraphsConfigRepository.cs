using System.Collections.Generic;

using Dapper;
using Microsoft.Data.SqlClient;

namespace ConfigImport.Domain.UserConfigs
{
    public class GraphsConfigRepository
    {
        private readonly string _connectionString;

        public GraphsConfigRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Dictionary<string, List<short>> Load(string user)
        {
            const string sql = "SELECT GraphUid, VariableId FROM dbo.GraphsConfig WHERE UserName = @UserName";
            using var connection = new SqlConnection(_connectionString);
            var rows = connection.Query(sql, new { UserName = user });

            var result = new Dictionary<string, List<short>>();
            foreach (var row in rows)
            {
                string uid = row.GraphUid;
                short varId = row.VariableId;
                if (!result.TryGetValue(uid, out var list))
                {
                    list = new List<short>();
                    result[uid] = list;
                }
                list.Add(varId);
            }

            return result;

        }

        public void Save(string user, Dictionary<string, List<short>> data)
        {

            const string deleteSql = "DELETE FROM dbo.GraphsConfig WHERE UserName = @UserName";
            const string insertSql = "INSERT INTO dbo.GraphsConfig (UserName, GraphUid, VariableId) VALUES (@UserName, @GraphUid, @VariableId)";

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            connection.Execute(deleteSql, new { UserName = user }, tx);

            foreach (var pair in data)
            {
                foreach (var varId in pair.Value)
                {
                    connection.Execute(insertSql, new
                    {
                        UserName = user,
                        GraphUid = pair.Key,
                        VariableId = varId
                    }, tx);
                }
            }

            tx.Commit();

        }
    }
}
