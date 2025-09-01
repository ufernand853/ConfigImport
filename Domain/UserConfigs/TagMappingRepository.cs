using System;
using System.Collections.Generic;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ConfigImport.Domain.UserConfigs
{
    public class TagMappingRepository
    {
        private readonly string _connectionString;

        public TagMappingRepository(string connectionString)
        {
            _connectionString = connectionString;
        }


        public Dictionary<string, Dictionary<string, TagMappingEntry>> Load(string user)
        {
            const string sql = "SELECT FormName, ElementId, TagValue, TagEstado FROM dbo.TagMappings WHERE UserName = @UserName";
            using var connection = new SqlConnection(_connectionString);
            var rows = connection.Query(sql, new { UserName = user });

            var result = new Dictionary<string, Dictionary<string, TagMappingEntry>>();
            foreach (var row in rows)
            {
                string form = row.FormName;
                string element = row.ElementId;

                string tagStr = row.TagValue;
                string? estadoStr = row.TagEstado;

                if (!result.TryGetValue(form, out var formMap))
                {
                    formMap = new Dictionary<string, TagMappingEntry>();
                    result[form] = formMap;
                }
                short tag = short.TryParse(tagStr, out var t) ? t : (short)0;
                short? estado = short.TryParse(estadoStr, out var e) ? e : (short?)null;
                formMap[element] = new TagMappingEntry { TagValue = tag, TagEstado = estado };
            }

            return result;
        }

        public void Save(string user, Dictionary<string, Dictionary<string, TagMappingEntry>> data)
        {
            const string deleteSql = "DELETE FROM dbo.TagMappings WHERE UserName = @UserName";
            const string insertSql = "INSERT INTO dbo.TagMappings (UserName, FormName, ElementId, TagValue, TagEstado) VALUES (@UserName, @FormName, @ElementId, @TagValue, @TagEstado)";

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

                        TagValue = kv.Value.TagValue.ToString(),
                        TagEstado = kv.Value.TagEstado.HasValue ? kv.Value.TagEstado.Value.ToString() : null

                    }, tx);
                }
            }

            tx.Commit();

        }
    }
}
