using Indx.Api;
using Indx.Json;
using Indx.Json.Api;
namespace IndxConsoleAppJson
{
    internal class Program
    {
        private static void Main()
        {
            //
            // CREATE INSTANCE
            //

            SearchEngineJson engine = new SearchEngineJson();
            // Load a license like this: new SearchEngineJson("file.license");
            // Get a developer license on https://indx.co

            //
            // STREAM DATA FROM FILE AND ANALYZE JSON
            //
            
            string file = "data/imdb_top10k.json";
            FileStream fstream = File.Open(file, FileMode.Open, FileAccess.Read);
            engine.Init(fstream, out string _errorMessage);
            fstream.Close();

            //
            // SETUP FIELDS
            //

            engine.GetField("Movie_Name")!.Indexable = true;
            engine.GetField("Movie_Name")!.Weight = Weight.High;
            engine.GetField("Stars")!.Indexable = true;
            engine.GetField("Stars")!.Facetable = true;
            engine.GetField("Stars")!.Weight = Weight.Low;
            engine.GetField("Description")!.Indexable = true;
            engine.GetField("Description")!.Weight = Weight.Med;
            engine.GetField("Year_of_Release")!.Facetable = true;

            //
            // LOAD DATA
            //

            fstream = File.Open(file, FileMode.Open, FileAccess.Read);
            engine.LoadJson(fstream, out _);
            fstream.Close();

            //
            // RUN INDEXING
            //

            engine.Index();

            // Check progress
            while (engine.Status.SystemState != SystemState.Ready) // Print indexing progress
            {
                int progressPercent = engine.Status.IndexProgressPercent;
                Console.Write($"\rIndexing {progressPercent}%");
                Thread.Sleep(50); // check every 50ms
            }

            Console.Clear();

            bool continueSearch = true;
            while (continueSearch)
            {
                //
                // SET UP SEARCH QUERY
                //

                Console.Write("🔍 Search: ");
                var text = Console.ReadLine() ?? ""; // pattern to be searched for

                int num = 10;
                JsonQuery query = new JsonQuery(text, num);

                //
                // SEARCH
                //

                var result = engine.Search(query);
                if (result != null)
                {
                    foreach(var rec in result.Records)
                    {
                        long key = rec.DocumentKey;
                        string json = engine.GetJsonDataOfKey(key); // Get JSON object

                        Console.WriteLine(json);
                    }
                }

                //
                // PRINT FACETS
                //

                // Enable facets in the query. This is by default set to false.
                query.EnableFacets = true;
                // Search again with facets enabled
                result = engine.Search(query);

                // Set up KeyValuePair for facets
                Dictionary<string, KeyValuePair<string, int>[]>? facets = result.Facets;

                // Print facets
                foreach (var field in engine.DocumentFields.GetFacetableFieldList())
                {
                    var fName = field.Name;
                    if (facets != null)
                    {
                        if (facets.TryGetValue(fName, out KeyValuePair<string, int>[]? histogram))
                        {
                            if (histogram != null)
                            {
                                Console.WriteLine("");
                                Console.WriteLine(field.Name);
                                foreach (var item in histogram)
                                {
                                    Console.WriteLine($"{item.Key} ({item.Value})");
                                };
                            }
                        }
                    }
                }
                Console.WriteLine("");

                //
                // META INFORMATION
                //

                Console.WriteLine($"\nExact hits found: {result.TruncationIndex + 1}"); // this will be 0 if query has large typos
                Console.WriteLine("Version: " + engine.Status.Version);

                // Continue prompt
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write("\nPress ESC to quit or any other key to continue.");
                Console.ResetColor();
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Escape) continueSearch = false;
                else Console.Clear(); 
            }               
        }
    }
}