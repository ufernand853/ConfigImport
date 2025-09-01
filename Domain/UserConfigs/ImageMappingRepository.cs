using System.Collections.Generic;


using Dapper;
using Microsoft.Data.SqlClient;

namespace ConfigImport.Domain.UserConfigs
{
    public class ImageMappingRepository
    {
        private readonly string _connectionString;

        public ImageMappingRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Dictionary<string, Dictionary<string, string>> Load(string user)
        {

            const string sql = "SELECT FormName, ElementId, ImagePath FROM dbo.ImageMappings WHERE UserName = @UserName";
            using var connection = new SqlConnection(_connectionString);
            var rows = connection.Query(sql, new { UserName = user });

            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach (var row in rows)
            {
                string form = row.FormName;
                string element = row.ElementId;
                string path = row.ImagePath;
                if (!result.TryGetValue(form, out var formMap))
                {
                    formMap = new Dictionary<string, string>();
                    result[form] = formMap;
                }
                formMap[element] = path;
            }

            return result;

        }

        public void Save(string user, Dictionary<string, Dictionary<string, string>> data)
        {

            const string deleteSql = "DELETE FROM dbo.ImageMappings WHERE UserName = @UserName";
            const string insertSql = "INSERT INTO dbo.ImageMappings (UserName, FormName, ElementId, ImagePath) VALUES (@UserName, @FormName, @ElementId, @ImagePath)";

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            connection.Execute(deleteSql, new { UserName = user }, tx);

            foreach (var form in data)
            {
                foreach (var kv in form.Value)
                {
                    connection.Execute(insertSql, new
                    {
                        UserName = user,
                        FormName = form.Key,
                        ElementId = kv.Key,
                        ImagePath = kv.Value
                    }, tx);
                }
            }

            tx.Commit();

        }
    }
}
