using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Text;

namespace WebApplication1.Pages
{

    public class TypeFiveQueriesModel : PageModel
    {
        public enum BrakesEnum
        {
            Good,
            Average,
            Poor
        }

        public enum DifferentConditionEnum
        {
            Low,
            Average,
            High
        }

        public enum RoadConditionEnum
        {
            Dry,
            Wet,
            Icy,
            Muddy
        }

        public enum RoadSurfaceEnum
        {
            Smooth,
            Coarse,
            Concrete
        }
        public List<(string Entity, string State)> ExtractedEntityAndStates { get; private set; }

        public string GeneratedQueries { get; private set; }

        public IActionResult OnGet()
        {
            List<(string Entity, string State, List<string> Relations, String Obj)> ExtractedEntityAndStates = new List<(string, string, List<string>, String)>
                {

                    ("Bicycle", "Riding",new List<string>{"leftOf" }, "Car"),
                    ("Bicycle", "Riding",new List<string>{"RightOf" }, "Bicycle"),
                     ("Car", "Driving",new List<string>{"ahead" }, "Bicycle"),
                     ("Car", "Driving",new List<string>{"behind" }, "Bicycle"),
            };

            // Serialize the list to JSON
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(ExtractedEntityAndStates);

            // Assign the JSON string to TempData
            TempData["ExtractedEntityAndStates"] = json;

            // Then, retrieve it from TempData
            var jsonFromTempData = TempData["ExtractedEntityAndStates"] as string;
            if (!string.IsNullOrEmpty(jsonFromTempData))
            {
                // Deserialize JSON back to list of tuples
                ExtractedEntityAndStates = Newtonsoft.Json.JsonConvert.DeserializeObject<List<(string, string, List<string>, string)>>(jsonFromTempData);
                StringBuilder queriesBuilder = new StringBuilder();

                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount // Use all available CPU cores
                };

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Debug.WriteLine("Execution Time ######################");
                // Use Parallel.ForEach for parallelism
                Parallel.ForEach(ExtractedEntityAndStates, options, kvp =>
                {
                    string entity = kvp.Entity;
                    string state = kvp.State;
                    string obj = kvp.Obj;
                    List<string> queries = GenerateQueries(entity, state, obj);
                    foreach (string query in queries)
                    {
                        queriesBuilder.AppendLine(query);
                    }


                    Random random = new Random();
                    DateTime startTime = new DateTime(2023, 5, 13, 10, 30, 0); // 13/05/2023 10:30:00 AM
                    DateTime endTime = new DateTime(2023, 5, 13, 17, 30, 0); // 13/05/2023 5:30:00 PM

                    // Set seconds component to 0
                    startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, 0);
                    endTime = new DateTime(endTime.Year, endTime.Month, endTime.Day, endTime.Hour, endTime.Minute, 0);

                    // Calculate the difference in seconds
                    int differenceInSeconds = (int)(endTime - startTime).TotalSeconds;

                    // Generate a random number of seconds to add to the start time
                    int randomSeconds = random.Next(0, differenceInSeconds);

                    // Create the random DateTime in the specified range
                    DateTime tempTimestamp = startTime.AddSeconds(randomSeconds);

                    int num = 10;
                    
                    List<string> timestampQueries = GenerateTimestampQuery(entity, tempTimestamp, num);

                    foreach (string query in timestampQueries)
                    {
                        queriesBuilder.AppendLine(query);
                    }
                });

                GeneratedQueries = queriesBuilder.ToString();

                // Generate queries for spatial relations outside the Parallel.ForEach loop
                foreach (var kvp in ExtractedEntityAndStates)
                {
                    string entity = kvp.Entity;
                    string obj = kvp.Obj;
                    List<string> Relations = kvp.Relations;

                    if (Relations != null)
                    {
                        foreach (string relation in Relations)
                        {
                            if (!string.IsNullOrEmpty(relation))
                            {
                                List<string> relationQueries = GenerateQueriesForRelations(entity, relation, obj);
                                foreach (string relationQuery in relationQueries)
                                {
                                    queriesBuilder.AppendLine(relationQuery);
                                }
                            }
                        }
                    }
                }
                GeneratedQueries = queriesBuilder.ToString();

                // Stop the stopwatch
                stopwatch.Stop();
                Debug.WriteLine("Execution Time: {0}ms", stopwatch.Elapsed.TotalMilliseconds);
                
            }
            // Check if the data was found in TempData
            if (ExtractedEntityAndStates == null)
            {
                // Handle the case where data is not found
                return RedirectToPage("/ErrorPage");
            }
            return Page();
        }

        public List<string> GenerateQueries(string entity, string state, string obj)
        {
            List<string> queries = new List<string>();
            if (entity == "Bicycle" && state == "Riding")
            {
                string query1 = $@"
                prefix mv:http://mobivoc.org,
                pull MIN(speed) as min_bicycle_speed
                define
                entity bicycle is FROM mv:Bicycle where bicycle.trafficCondition = ""High""";
                queries.Add(query1);

                string query10 = $@"
                prefix mv:http://mobivoc.org,
                pull MIN(speed) as min_bicycle_speed
                define
                entity bicycle is FROM mv:Bicycle where bicycle.trafficCondition = ""Low""";
                queries.Add(query10);

                string query2 = $@"
                prefix mv:http://mobivoc.org,
                pull MAX(speed) as max_bicycle_speed
                define
                entity bicycle is FROM mv:Bicycle where bicycle.trafficCondition = ""High""";
                queries.Add(query2);

                string query11 = $@"
                prefix mv:http://mobivoc.org,
                pull MAX(speed) as max_bicycle_speed
                define
                entity bicycle is FROM mv:Bicycle where bicycle.trafficCondition = ""Low""";
                queries.Add(query11);

                string query3 = $@"
                prefix mv:http://mobivoc.org,
                pull AVG(speed) as avg_bicycle_speed
                define
                entity bicycle is FROM mv:Bicycle where bicycle.trafficCondition = ""Low""";
                queries.Add(query3);

                string query4 = $@"
                prefix mv:http://mobivoc.org,
                pull COUNT(*) as bicycle_with_high_speed
                define
                entity bicycle is FROM mv:Bicycle where bicycle.speed > {GenerateRandomNumericValue(4, 7)};";
                queries.Add(query4);

                string query5 = $@"
                prefix mv:http://mobivoc.org,
                pull COUNT(*) as bicycle_with_low_speed
                define
                entity bicycle is FROM mv:Bicycle where bicycle.speed < {GenerateRandomNumericValue(1, 4)};";
                queries.Add(query5);


                if (entity == "Bicycle" && state == "Riding" && obj == "Car")
                {
                    string query6 = $@"
                    prefix mv:http://mobivoc.org,
                    pull COUNT(*) as _bicycles_close_to_vehicle
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(10, 50)};";
                    queries.Add(query6);

                    string query7 = $@"
                    prefix mv:http://mobivoc.org,
                    pull COUNT(*) as _bicycles_close_to_vehicle
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(10, 50)} ;";
                    queries.Add(query7);

                    string query8 = $@"
                    prefix mv:http://mobivoc.org,
                    pull COUNT(*) as _bicycle_with_wearing
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.wearingOfTyres = ""{GenerateRandomConditionEnum()}"" and bicycle.proximityToVehicle < {GenerateRandomNumericValue(10, 50)} ;";
                    queries.Add(query8);

                    string query9 = $@"
                    prefix mv:http://mobivoc.org,
                    pull COUNT(*) as bicycle_with_high_speed
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.speed > {GenerateRandomNumericValue(4, 7)} and bicycle.distanceToPedestrian < {GenerateRandomNumericValue(10, 50)} and bicycle.proximityToVehicle < {GenerateRandomNumericValue(10, 50)} ;";
                    queries.Add(query9);
                }
            }

            if (entity == "Car" && state == "Parking")
            {

                string query8 = $@"
                prefix mv:http://mobivoc.org,
                pull COUNT(*) as cars_with_preson
                define
                entity car is FROM mv:Car where car.speed = 0;";
                queries.Add(query8);

                string query4 = $@"
                prefix mv:http://mobivoc.org,
                pull COUNT(*) as cars_with_preson
                define
                entity car is FROM mv:Car where car.personInside = true;";
                queries.Add(query4);

                string query5 = $@"
                prefix mv:http://mobivoc.org,
                pull COUNT(*) as cars_with_person
                define
                entity car is FROM mv:Car where car.personInside = true AND car.speed = 0;";
                queries.Add(query5);

                string query6 = $@"
                prefix mv:http://mobivoc.org,
                pull COUNT(*) as parked_cars_with_person
                define
                entity car is FROM mv:Car where car.personInside = true AND car.speed = 0 and vehicleDoorStatus = 0;";
                queries.Add(query6);

            }

            if (entity == "Bicycle" && state == "Riding" || state == "Stopping")
            {
                string query1 = $@"
                prefix mv:http://mobivoc.org,
                pull MIN(heartRate) as min_heart_rate
                define
                entity person is FROM mv:Person";
                queries.Add(query1);

                string query3 = $@"
                prefix mv:http://mobivoc.org,
                pull MAX(heartRate) as max_heart_rate
                define
                entity person is FROM mv:Person";
                queries.Add(query3);

                string query2 = $@"
                prefix mv:http://mobivoc.org,
                pull MAX(respiratoryRate) as max_resp_rate
                define
                entity person is FROM mv:Person ";
                queries.Add(query2);

                string query4 = $@"
                prefix mv:http://mobivoc.org,
                pull MIN(respiratoryRate) as min_resp_rate
                define
                entity person is FROM mv:Person ";
                queries.Add(query4);


                string query5 = $@"
                prefix mv:http://mobivoc.org,
                pull AVG(respiratoryRate) as avg_resp_rate
                define
                entity person is FROM mv:Person ";
                queries.Add(query5);

            }
            return queries;
        }


        private List<string> GenerateQueriesForRelations(string entity, string relation, string obj)
        {
            List<string> queries = new List<string>();

            if (entity == "Car" && (relation.Contains("ahead") || relation.Contains("behind") || relation.Contains("rightOf")
                || relation.Contains("leftOf") || relation.Contains("infrontOf")))
            {
                string[] operand = new string[] { "=", ">=", "<=" };
                foreach (string operandValue in operand)
                {
                    string query1 = @$"
                    prefix mv:http://mobivoc.org
                    pull MIN(speed) as bicycle_speed    
                    define
                    entity car is FROM mv:Car where car.proximityToBicycle{operandValue} {GenerateRandomNumericValue(10, 30)} and car.speed = 0;";
                    queries.Add(query1);
                }
            }

            if (entity == "Bicycle" && (obj == "Bicycle" || obj == "Car"))
            {
                if (!string.IsNullOrEmpty(relation))
                {
                    if ((relation.Contains("ahead") || relation.Contains("behind") || relation.Contains("rightOf")
               || relation.Contains("leftOf") || relation.Contains("infrontOf")))
                    {
                        string[] operand = new string[] { "=", ">=", "<=" };
                        foreach (string operandValue in operand)
                        {
                            string query1 = @$"
                            prefix mv:http://mobivoc.org
                            pull MAX(speed) as bicycle_speed    
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle{operandValue} {GenerateRandomNumericValue(15, 40)} and bicycle.{operandValue} {GenerateRandomNumericValue(1, 8)}";
                            queries.Add(query1);
                        }

                        string query3 = @$"
                        prefix mv:http://mobivoc.org
                        pull MAX(speed) as bicycle_speed    
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)} and bicycle.speed > 5";
                        queries.Add(query3);

                        string query4 = @$"
                        prefix mv:http://mobivoc.org
                        pull MAX(speed) as bicycle_speed    
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)} and bicycle.conditionOfBrakes = ""Poor""";
                        queries.Add(query4);

                        string query5 = @$"
                        prefix mv:http://mobivoc.org
                        pull MAX(speed) as bicycle_speed    
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)} and bicycle.wearingOfTyres = ""Poor""";
                        queries.Add(query5);

                        string query6 = @$"
                        prefix mv:http://mobivoc.org
                        pull MAX(speed) as bicycle_speed    
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)} and bicycle.trafficCondition. = ""High""";
                        queries.Add(query6);
                    }
                }
            }

            if (!string.IsNullOrEmpty(obj))
            {
                if (obj.Contains("puddle") || obj.Contains("pothole")
                  || obj.Contains("fallenBranch") || obj.Contains("pedestrian"))
                {
                    string query9 = @$"
                    prefix mv:http://mobivoc.org
                    pull MAX(speed) as bicycle_speed
                    define
                    entity bicycle is FROM mv:Bicycle where  bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                    queries.Add(query9);

                    string query10 = @$"
                    prefix mv:http://mobivoc.org
                    pull AVG (speed) as bicycle_speed 
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Poor"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                    queries.Add(query10);

                    string query11 = @$"
                    prefix mv:http://mobivoc.org
                    pull MAX(speed) as bicycle_speed 
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.wearingOfTyres = ""Poor"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                    queries.Add(query11);

                    string query8 = @$"
                    prefix mv:http://mobivoc.org
                    pull AVG (speed) as bicycle_speed 
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.roadCondition = ""Wet"" and bicycle.proximityToObstacle <= {GenerateRandomNumericValue(15, 50)}";
                    queries.Add(query8);
                }

                if (obj.Contains("intersection") || obj.Contains("crossing"))
                {
                    string query4 = @$"
                    prefix mv:http://mobivoc.org
                    pull AVG (speed) as bicycle_speed 
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.speed >= {GenerateRandomNumericValue(2, 5)} and bicycle.proximityToRoadJunction < {GenerateRandomNumericValue(15, 50)}";
                    queries.Add(query4);

                    string query7 = @$"
                    prefix mv:http://mobivoc.org
                    pull AVG (speed) as bicycle_speed 
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Poor"" and bicycle.proximityToRoadJunction <= {GenerateRandomNumericValue(15, 50)}";
                    queries.Add(query7);


                    string query8 = @$"
                    prefix mv:http://mobivoc.org
                    pull MAX (speed) as bicycle_speed 
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.trafficCondition = ""High"" and bicycle.proximityToRoadJunction < {GenerateRandomNumericValue(15, 50)}";
                    queries.Add(query8);
                }
            }



            if (entity == "Bicycle" && relation.Contains("Rain"))
            {
                string query3 = @$"
                prefix mv:http://mobivoc.org
                pull MAX(speed) as bicycle_speed    
                define
                entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.speed >= 5";
                queries.Add(query3);

                string query4 = @$"
                prefix mv:http://mobivoc.org
                pull MAX(speed) as bicycle_speed    
                define
                entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.conditionOfBrakes = ""Poor""";
                queries.Add(query4);

                string query5 = @$"
                prefix mv:http://mobivoc.org
                pull MAX(speed) as bicycle_speed    
                define
                entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.wearingOfTyres = ""Poor""";
                queries.Add(query5);

                string query6 = @$"
                prefix mv:http://mobivoc.org
                pull AVG(speed) as bicycle_speed    
                define
                entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.trafficCondition = ""High"" ";
                queries.Add(query6);

                string query7 = @$"
                prefix mv:http://mobivoc.org
                pull AVG(speed) as bicycle_speed    
                define
                entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.roadCondition = ""Wet"" ";
                queries.Add(query7);
            }

            if (entity == "Bicycle" && relation.Contains("dooring") || relation.Contains("accident") ||
                relation.Contains("falling"))
            {

                string query4 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*)
                define
                entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true"" and bicycle.conditionOfBrakes = ""Poor"" ";
                queries.Add(query4);

                string query8 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*)
                define
                entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true"" and bicycle.trafficCondition = ""High"" ";
                queries.Add(query8);

                string query6 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*)
                define
                entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true"" and bicycle.wearingOfTyres = ""Poor""";
                queries.Add(query6);

                string query2 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*)
                define
                entity person is FROM mv:Person where person.heartRate > 80 and person.collisionState = ""true"";
                ";
                queries.Add(query2);

                string query3 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*)
                define
                entity person is FROM mv:Person where person.injuryLevel = {GenerateRandomNumericValue(1, 4)} 
                and person.collisionState = ""true"";
                ";
                queries.Add(query3);

                string query7 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*)
                define
                entity person is FROM mv:Person where person.injuryLevel >= {GenerateRandomNumericValue(1, 4)} 
                and person.collisionState = ""true"";
                ";
                queries.Add(query7);

                string query17 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*)
                define
                entity person is FROM mv:Person where person.injuryLevel >= {GenerateRandomNumericValue(1, 4)} 
                and person.collisionState = ""true"" and person.age < 30;
                ";
                queries.Add(query17);

                string query18 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*)
                define
                entity person is FROM mv:Person where person.injuryLevel >= {GenerateRandomNumericValue(1, 4)} 
                and person.collisionState = ""true""and person.age >30;
                ";
                queries.Add(query18);
            }

            if (entity == "Bicycle" && relation.Contains("approach"))
            {
                if (!string.IsNullOrEmpty(obj))
                {
                    if (obj.Contains("Car"))
                    {
                        string query2 = @$"
                        prefix mv:http://mobivoc.org
                        pull COUNT(*)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.speed >= 3 and bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)}";
                        queries.Add(query2);
                    }
                }
                if (!string.IsNullOrEmpty(obj))
                {
                    if (obj.Contains("puddle") || obj.Contains("pothole")
                      || obj.Contains("fallenBranch") || obj.Contains("pedestrian"))
                    {
                        string query9 = @$"
                        prefix mv:http://mobivoc.org
                        pull MAX(speed) as bicycle_speed 
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.speed >= 3 and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                        queries.Add(query9);

                        string query10 = @$"
                        prefix mv:http://mobivoc.org
                        pull AVG(speed) as bicycle_speed 
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Poor"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                        queries.Add(query10);

                        string query11 = @$"
                        prefix mv:http://mobivoc.org
                        pull AVG(speed) as bicycle_speed 
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.wearingOfTyres = ""Poor"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                        queries.Add(query11);

                        string query8 = @$"
                        prefix mv:http://mobivoc.org
                        pull MAX(speed) as bicycle_speed 
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.roadCondition = ""Wet"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                        queries.Add(query8);
                    }

                    if (obj.Contains("intersection") || obj.Contains("crossing"))
                    {
                        string query4 = @$"
                        prefix mv:http://mobivoc.org
                        pull AVG(speed) as bicycle_speed 
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.speed >= {GenerateRandomNumericValue(2, 5)} and bicycle.proximityToRoadJunction < {GenerateRandomNumericValue(15, 50)}";
                        queries.Add(query4);

                        string query7 = @$"
                        prefix mv:http://mobivoc.org
                        pull AVG(speed) as bicycle_speed 
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Poor"" and bicycle.proximityToRoadJunction < {GenerateRandomNumericValue(15, 50)}";
                        queries.Add(query7);


                        string query8 = @$"
                        prefix mv:http://mobivoc.org
                        pull MAX(speed) as bicycle_speed 
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.trafficCondition = ""High"" and bicycle.proximityToRoadJunction < {GenerateRandomNumericValue(15, 50)}";
                        queries.Add(query8);
                    }
                }
            }

            if (entity == "Car" && relation.Contains("open") && obj =="Bicycle")
            {
                string query6 = $@"
                prefix mv:http://mobivoc.org,
                pull COUNT(*) as parked_cars_with_person
                define
                entity car is FROM mv:Car where car.personInside = true AND car.speed = 0 and vehicleDoorStatus = 0;";
                queries.Add(query6);


                string[] operand = new string[] { "=", ">=", "<=" };
                foreach (string operandValue in operand)
                {
                    string query1 = @$"
                    prefix mv:http://mobivoc.org
                    pull COUNT(*) as parked_cars_with_person
                    define
                    entity car is FROM mv:Car where car.proximityToVehicle{operandValue} {GenerateRandomNumericValue(1, 30)} and car.speed = 0,
                    entity bicycle is FROM mv:Bicycle where bicycle.speed{operandValue} {GenerateRandomNumericValue(1, 7)} and bicycle.clusterId = car.clusterId,
                    entity person is FROM mv:Person where person.age >= 25 and person.personId = bicycle.personId;";
                    queries.Add(query1);
                }
            }

            if (entity == "Bicycle" && relation.Contains("dooring") || relation.Contains("accident") ||
                relation.Contains("falling"))
            {

                string query4 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*) as accident
                define
                entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true"" and bicycle.conditionOfBrakes = ""Poor"" ";
                queries.Add(query4);

                string query8 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*) as accident
                define
                entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true"" and bicycle.conditionOfBrakes = ""Poor"" ";
                queries.Add(query8);

                string query6 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*) as accident
                define
                entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true"" and bicycle.wearingOfTyres = ""Poor""";
                queries.Add(query6);

                string query2 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*) as accident
                define
                entity person is FROM mv:Person where person.heartRate > 80 and person.collisionState = ""true"";
                ";
                queries.Add(query2);

                string query3 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*) as accident
                define
                entity person is FROM mv:Person where person.injuryLevel = {GenerateRandomNumericValue(1, 4)} 
                and person.collisionState = ""true"";
                ";
                queries.Add(query3);

                string query7 = @$"
                prefix mv:http://mobivoc.org
                pull COUNT(*) as accident
                define
                entity person is FROM mv:Person where person.injuryLevel >= {GenerateRandomNumericValue(1, 4)} 
                and person.collisionState = ""true"";
                ";
                queries.Add(query7);
            }

            return queries;
        }
        private static BrakesEnum GenerateRandomBrakesEnum()
        {
            var values = Enum.GetValues(typeof(BrakesEnum));
            var random = new Random();
            return (BrakesEnum)values.GetValue(random.Next(values.Length));
        }

        private static DifferentConditionEnum GenerateRandomConditionEnum()
        {
            var values = Enum.GetValues(typeof(DifferentConditionEnum));
            var random = new Random();
            return (DifferentConditionEnum)values.GetValue(random.Next(values.Length));
        }

        private static RoadConditionEnum GenerateRandomRoadConditionEnum()
        {
            var values = Enum.GetValues(typeof(RoadConditionEnum));
            var random = new Random();
            return (RoadConditionEnum)values.GetValue(random.Next(values.Length));
        }

        private static RoadSurfaceEnum GenerateRandomRoadSurfaceEnum()
        {
            var values = Enum.GetValues(typeof(RoadSurfaceEnum));
            var random = new Random();
            return (RoadSurfaceEnum)values.GetValue(random.Next(values.Length));
        }

        private static int GenerateRandomNumericValue(int start, int end)
        {
            Random random = new Random();
            int randomNumber = random.Next(start, end); // Generates a random number between 0 (inclusive) and 11 (exclusive)
            return randomNumber;
        }

        private static string GenerateRandomVIN(string entity)
        {
            Random rand = new Random();
            int randomNumber = rand.Next(0, 100); // Generate a random number between 0 and 100
            string randomVin = null;

            if (entity == "Bicycle")
            {
                randomVin = $"BIC{randomNumber:D2}"; // Format the number with leading zeros if necessary
            }
            else if (entity == "Car")
            {
                randomVin = $"CAR{randomNumber:D2}"; // Format the number with leading zeros if necessary

            }
            return randomVin;
        }

        private static bool GenerateRandomBoolean()
        {
            Random random = new Random();
            return random.Next(0, 2) == 1;
        }


        static List<string> GenerateTimestampQuery(string entity, DateTime timestamp, int num)
        {
            DateTime targetTimestamp = timestamp.AddMinutes(num);
            string formattedTime = targetTimestamp.ToString("dd/MM/yyyy  h:mm:00 tt");

            List<string> queries = new List<string>();

            if (entity == "Bicycle")
            {
                string query1 =
                $@"
                prefix mv:http://mobivoc.org,
                pull AVG(speed) as avg_bicycle_speed
                define
                entity bicycle is FROM mv:Bicycle
                WHERE bicycle.timestamp = '{formattedTime}' and bicycle.trafficCondition = ""High""";

                queries.Add(query1);

                string query2 =
                $@"
                prefix mv:http://mobivoc.org,
                pull COUNT(*) as bicycle_count
                define
                entity bicycle is FROM mv:Bicycle
                WHERE bicycle.timestamp = '{formattedTime}' and bicycle.trafficCondition = ""High"""; 
                queries.Add(query2);

            }


            if (entity == "Car")
            {
                Random random = new Random();
                DateTime startTime = new DateTime(2023, 5, 13, 17, 11, 0); // 13/05/2023 5:11:00 PM
                DateTime endTime = new DateTime(2023, 5, 13, 17, 28, 0);   // 13/05/2023 5:28:00 PM

                // Calculate the difference in seconds
                int differenceInSeconds = (int)(endTime - startTime).TotalSeconds;

                // Generate a random number of seconds to add to the start time
                int randomSeconds = random.Next(0, differenceInSeconds);

                // Create the random DateTime in the specified range
                DateTime randomTimestamp = startTime.AddSeconds(randomSeconds);

                // Format the time correctly with the AM/PM suffix
                string timeVal = randomTimestamp.ToString("dd/MM/yyyy  h:mm:00 tt");
                string query1 =
                $@"
                prefix mv:http://mobivoc.org,
                pull AVG(speed) as avg_car_speed
                define
                entity car is FROM mv:Car
                WHERE car.timestamp = '{timeVal}';";
                queries.Add(query1);

                string query2 =
                $@"
                prefix mv:http://mobivoc.org,
                pull MAX(speed) as max__speed
                define
                entity car is FROM mv:Car
                WHERE car.timestamp = '{timeVal}';";
                queries.Add(query2);

                string query3 =
                $@"
                prefix mv:http://mobivoc.org,
                pull MIN(speed) as min_car_speed
                define
                entity car is FROM mv:Car
                WHERE car.timestamp = '{timeVal}';";
                queries.Add(query3);
            }
            return queries; // Return an empty string if the entity is not recognized
        }
    }
}

