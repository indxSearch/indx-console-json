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

            SearchEngineJson SearchEngine = new SearchEngineJson();
            // Load a license like this: new SearchEngineJson("file.license");
            // Get a developer license on https://indx.co

            //
            // STREAM DATA FROM FILE AND ANALYZE JSON
            //
            
            string file = "data/imdb_top10k.json";
            FileStream fstream = File.Open(file, FileMode.Open, FileAccess.Read);
            SearchEngine.Init(fstream, out string _errorMessage);
            fstream.Close();

            //
            // SETUP FIELDS
            //

            SearchEngine.GetField("Movie_Name")!.Indexable = true;
            SearchEngine.GetField("Movie_Name")!.Weight = Weight.High;
            SearchEngine.GetField("Stars")!.Indexable = true;
            SearchEngine.GetField("Stars")!.Facetable = true;
            SearchEngine.GetField("Stars")!.Weight = Weight.Low;
            SearchEngine.GetField("Description")!.Indexable = true;
            SearchEngine.GetField("Description")!.Weight = Weight.Med;
            SearchEngine.GetField("Year_of_Release")!.Facetable = true;

            //
            // LOAD DATA
            //

            fstream = File.Open(file, FileMode.Open, FileAccess.Read);
            SearchEngine.LoadJson(fstream, out _);
            fstream.Close();

            //
            // RUN INDEXING
            //

            SearchEngine.Index();

            // Check progress
            while (SearchEngine.Status.SystemState != SystemState.Ready) // Print indexing progress
            {
                int progressPercent = SearchEngine.Status.IndexProgressPercent;
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

                var result = SearchEngine.Search(query);
                if (result != null)
                {
                    foreach(var rec in result.Records)
                    {
                        long key = rec.DocumentKey;
                        string json = SearchEngine.GetJsonDataOfKey(key); // Get JSON object

                        Console.WriteLine(json);
                    }
                }

                //
                // PRINT FACETS
                //

                // Enable facets in the query. This is by default set to false.
                query.EnableFacets = true;
                // Search again with facets enabled
                result = SearchEngine.Search(query);

                // Set up KeyValuePair for facets
                Dictionary<string, KeyValuePair<string, int>[]>? facets = result.Facets;

                // Print facets
                foreach (var field in SearchEngine.DocumentFields.GetFacetableFieldList())
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
                Console.WriteLine("Version: " + SearchEngine.Status.Version);

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