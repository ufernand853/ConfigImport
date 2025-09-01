using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ConfigImport.Domain.UserConfigs;

namespace ConfigImport
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: dotnet run --project ConfigImport -- <connectionString> <user> [configFolder]");
                return;
            }

            var connectionString = args[0];
            var user = args[1];
            var folder = args.Length > 2 ? args[2] : Directory.GetCurrentDirectory();

            Import(connectionString, user, folder);
            Console.WriteLine("Import completed.");
        }

        private static void Import(string connectionString, string user, string folder)
        {
            var imageRepo = new ImageMappingRepository(connectionString);
            var tagRepo = new TagMappingRepository(connectionString);
            var graphsRepo = new GraphsConfigRepository(connectionString);

            var imageFile = Path.Combine(folder, "ImageMappings.json");
            if (File.Exists(imageFile))
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(imageFile)) ?? new();
                imageRepo.Save(user, data);
            }

            var tagFile = Path.Combine(folder, "Mapeos.json");
            if (File.Exists(tagFile))
            {

                var raw = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(File.ReadAllText(tagFile)) ?? new();
                short ReadShort(JsonElement el) => el.ValueKind == JsonValueKind.Number
                    ? el.GetInt16()
                    : short.TryParse(el.GetString(), out var v) ? v : (short)0;
                var data = new Dictionary<string, Dictionary<string, TagMappingEntry>>();
                foreach (var form in raw)
                {
                    var formDict = new Dictionary<string, TagMappingEntry>();
                    foreach (var kv in form.Value)
                    {
                        if (kv.Value.ValueKind == JsonValueKind.Object)
                        {

                            short tagVal = ReadShort(kv.Value.GetProperty("TagValue"));
                            short? tagEst = kv.Value.TryGetProperty("TagEstado", out var te) ? ReadShort(te) : (short?)null;

                            formDict[kv.Key] = new TagMappingEntry { TagValue = tagVal, TagEstado = tagEst };
                        }
                        else
                        {

                            short tagVal = ReadShort(kv.Value);
                            formDict[kv.Key] = new TagMappingEntry { TagValue = tagVal };
                        }
                    }
                    data[form.Key] = formDict;
                }

                tagRepo.Save(user, data);
            }

            var graphsFile = Path.Combine(folder, "GraficasMultiples.json");
            if (File.Exists(graphsFile))
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, List<short>>>(File.ReadAllText(graphsFile)) ?? new();
                graphsRepo.Save(user, data);
            }
        }
    }
}
