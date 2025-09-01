using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using ConfigImport.Domain.UserConfigs;
using Microsoft.Extensions.Configuration;

namespace ConfigImport
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            Trace.AutoFlush = true;

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                var connectionString = config.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("Connection string not found in configuration.");
                    return;
                }

                if (args.Length < 1)
                {
                    Console.WriteLine("Usage: dotnet run --project ConfigImport -- <user> [configFolder]");
                    return;
                }

                var user = args[0];
                var folder = args.Length > 1
                    ? args[1]
                    : config["JsonFolder"] ?? Directory.GetCurrentDirectory();

                Trace.WriteLine($"Starting import for user {user} from folder {folder}");
                Import(connectionString, user, folder);
                Trace.WriteLine("Import completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Trace.WriteLine($"Error: {ex}");
            }

        }

        private static void Import(string connectionString, string user, string folder)
        {
            var imageRepo = new ImageMappingRepository(connectionString);
            var tagRepo = new TagMappingRepository(connectionString);
            var graphsRepo = new GraphsConfigRepository(connectionString);

            var imageFile = Path.Combine(folder, "ImageMappings.json");
            if (File.Exists(imageFile))
            {
                Trace.WriteLine("Importing image mappings");
                var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(imageFile)) ?? new();
                imageRepo.Save(user, data);
            }
            else
            {
                Trace.WriteLine("Image mappings file not found");
            }

            var tagFile = Path.Combine(folder, "Mapeos.json");
            if (File.Exists(tagFile))
            {
                Trace.WriteLine("Importing tag mappings");

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
            else
            {
                Trace.WriteLine("Tag mappings file not found");
            }

            var graphsFile = Path.Combine(folder, "GraficasMultiples.json");
            if (File.Exists(graphsFile))
            {
                Trace.WriteLine("Importing graphs configuration");
                var data = JsonSerializer.Deserialize<Dictionary<string, List<short>>>(File.ReadAllText(graphsFile)) ?? new();
                graphsRepo.Save(user, data);
            }
            else
            {
                Trace.WriteLine("Graphs configuration file not found");
            }
        }
    }
}
