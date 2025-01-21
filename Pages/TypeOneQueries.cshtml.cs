using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Web;

namespace WebApplication1.Pages
{
    public class TypeOneQueriesModel : PageModel
    {
        public enum BrakesEnum
        {
            Good,
            Average,
            Poor
        }
        public enum DifferentConditionEnum
        {
            Good,
            Wearing,
            Poor
        }
        public enum RoadConditionEnum
        {
            Dry,
            Wet,
            Icy,
            Muddy
        }

        public enum WeatherEnum
        {
            Rainy,
            Sunny,
            Gloomy,
            Cloudy
        }
        public enum RoadSurfaceEnum
        {
            Smooth,
            Coarse,
            Concrete
        }
        public string GeneratedQueries { get; private set; }

        [BindProperty(SupportsGet = true)]
        public string Data { get; set; }

        public List<(string Entity, string State, List<string> Relation, string Obj, string Perception)> DeserializedData { get; set; }
        public List<string> EntityStates { get; set; }
        //public void OnGet()
        //{
        //    if (!string.IsNullOrEmpty(Data))
        //    {
        //        string decodedData = System.Web.HttpUtility.UrlDecode(Data);
        //        DeserializedData = JsonConvert.DeserializeObject<List<(string Entity, string State, List<string> Relation, string Obj, string Perception)>>(decodedData);

        //        // Initialize the list to store entity states
        //        EntityStates = new List<string>();

        //        // Initialize the StringBuilder for storing generated queries
        //        StringBuilder queriesBuilder = new StringBuilder();

        //        // Parallel options
        //        var options = new ParallelOptions { MaxDegreeOfParallelism = System.Environment.ProcessorCount };

        //        // Iterate through each element and extract entity states
        //        Parallel.ForEach(DeserializedData, options, item =>
        //        {
        //            string entity = item.Entity;
        //            string state = item.State;
        //            string obj = item.Obj;
        //            string perception = item.Perception;
        //            List<string> relations = item.Relation;

        //            // Generate queries for the entity state
        //            string queries = GenerateQueries(entity, state, perception, obj);
        //            lock (queriesBuilder) // Ensure thread-safe access to the StringBuilder
        //            {
        //                queriesBuilder.AppendLine(queries);
        //            }

        //            // Generate queries for each relation
        //            if (relations != null)
        //            {
        //                foreach (string relation in relations)
        //                {
        //                    if (!string.IsNullOrEmpty(relation))
        //                    {
        //                        List<string> relationQueries = GenerateQueriesForRelations(entity, relation, obj, perception);
        //                        lock (queriesBuilder) // Ensure thread-safe access to the StringBuilder
        //                        {
        //                            foreach (string relationQuery in relationQueries)
        //                            {
        //                                queriesBuilder.AppendLine(relationQuery);
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        });

        //        // Set the generated queries to the property
        //        GeneratedQueries = queriesBuilder.ToString();
        //    }
        //}


        public async Task<IActionResult> OnGet()
        {

            List<(string Entity, string State, List<string> Relations, string Obj, string Perception)> ExtractedEntityAndStates = new List<(string, string, List<string>, string, string)>
                {
                      ("Bicycle","Accident",new List<string>{"collision"}, "", ""),

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
                ExtractedEntityAndStates = Newtonsoft.Json.JsonConvert.DeserializeObject<List<(string, string, List<string>, string, string)>>(jsonFromTempData);
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
                    string perception = kvp.Perception;
                    string queries = GenerateQueries(entity, state, perception, obj);
                    lock (queriesBuilder) // Ensure thread-safe access to the StringBuilder
                    {
                        queriesBuilder.AppendLine(queries);
                    }
                });

                GeneratedQueries = queriesBuilder.ToString();

                // Generate queries for spatial relations outside the Parallel.ForEach loop
                foreach (var kvp in ExtractedEntityAndStates)
                {
                    string entity = kvp.Entity;
                    string state = kvp.State;
                    string obj = kvp.Obj;
                    string perception = kvp.Perception;
                    List<string> Relations = kvp.Relations;
                    if (Relations != null)
                    {
                        foreach (string relation in Relations)
                        {
                            if (!string.IsNullOrEmpty(relation))
                            {
                                List<string> relationQueries = GenerateQueriesForRelations(entity, relation, obj, perception);
                                foreach (string relationQuery in relationQueries)
                                {
                                    queriesBuilder.AppendLine(relationQuery);
                                }
                            }

                        }
                    }

                }
                GeneratedQueries = queriesBuilder.ToString();
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

        private List<string> GenerateQueriesForRelations(string entity, string relation, string obj, string perception)
        {
            List<string> queries = new List<string>();

            if (entity == "Car" && (obj == "Bicycle" || obj == "Car"))
            {
                if (!string.IsNullOrEmpty(relation))
                {
                    if ((relation.Contains("ahead") || relation.Contains("behind") || relation.Contains("rightOf")
                   || relation.Contains("leftOf") || relation.Contains("infrontOf")))
                    {
                        string[] operand = new string[] { "=", ">=", "<=" };

                        if (perception == "")
                        {
                            foreach (string operandValue in operand)
                            {
                                string query1 = @$"
                                prefix mv:http://mobivoc.org
                                pull (car.*)
                                define
                                entity car is FROM mv:Car where car.proximityToBicycle {operandValue} {GenerateRandomNumericValue(20, 40)};";
                                queries.Add(query1);

                                string query2 = @$"
                                prefix mv:http://mobivoc.org
                                pull (car.vin, car.location)
                                define
                                entity car is FROM mv:Car where car.proximityToBicycle {operandValue} {GenerateRandomNumericValue(20, 40)};";
                                queries.Add(query2);

                            }
                        }


                        if (perception != "")
                        {
                            foreach (string operandValue in operand)
                            {
                                string query1 = @$"
                                prefix mv:http://mobivoc.org
                                pull (car.*)
                                define
                                entity car is FROM mv:Car where car.proximityToBicycle {operandValue} {GenerateRandomNumericValue(20, 40)};";
                                queries.Add(query1);

                                string query2 = @$"
                                prefix mv:http://mobivoc.org
                                pull (car.vin, car.location)
                                define
                                entity car is FROM mv:Car where car.proximityToBicycle {operandValue} {GenerateRandomNumericValue(20, 40)};";
                                queries.Add(query2);
                            }
                        }

                    }

                    if (entity == "Car" && relation.Contains("open") && perception == "")
                    {
                        string query2 = @$"
                        prefix mv:http://mobivoc.org
                        pull (car.vin)
                        define
                        entity car is FROM mv:Car where car.vehicleDoorStatus = ""unlock""";
                        queries.Add(query2);

                        string query3 = @$"
                        prefix mv:http://mobivoc.org
                        pull (car.location)
                        define
                        entity car is FROM mv:Car where car.doorOpenStatus >= 3";
                        queries.Add(query3);

                        string query4 = @$"
                        prefix mv:http://mobivoc.org
                        pull (car.vin)
                        define
                        entity car is FROM mv:Car where car.doorOpenStatus = 1";
                        queries.Add(query4);

                        string query5 = @$"
                        prefix mv:http://mobivoc.org
                        pull (car.vin)
                        define
                        entity car is FROM mv:Car where car.doorOpenStatus >= 5";
                        queries.Add(query5);

                        string query6 = @$"
                        prefix mv:http://mobivoc.org
                        pull (car.presenceOfPersonInside)
                        define
                        entity car is FROM mv:Car where car.doorOpenStatus >= 5";
                        queries.Add(query6);
                    }

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
                        if (perception == "")
                        {
                            foreach (string operandValue in operand)
                            {
                                string query1 = @$"
                                prefix mv:http://mobivoc.org
                                pull (bicycle.*)
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle {operandValue} {GenerateRandomNumericValue(10, 25)};";
                                queries.Add(query1);

                                string query2 = @$"
                                prefix mv:http://mobivoc.org
                                pull (bicycle.id, bicycle.location)
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle {operandValue} {GenerateRandomNumericValue(10, 25)};";
                                queries.Add(query2);
                            }

                            string query3 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query3);

                            string query8 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.speed, bicycle.location)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle <= {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query8);

                            string query4 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.conditionOfBrakes)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query4);

                            string query5 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.wearingOfTyres)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle <= {GenerateRandomNumericValue(15, 50)}";
                                        queries.Add(query5);

                            string query6 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.trafficCondition, bicycle.location)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle <= {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query6);
                        }

                        if (perception != "")
                        {
                            
                                string query1 = @$"
                                prefix mv:http://mobivoc.org
                                pull (bicycle.proximityToVehicle)
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                                queries.Add(query1);

                                string query2 = @$"
                                prefix mv:http://mobivoc.org
                                pull (bicycle.proximityToVehicle, bicycle.location)
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                                queries.Add(query2);
                            

                            string query3 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.speed,bicycle.proximityToVehicle)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                            queries.Add(query3);

                            string query8 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.speed, bicycle.location,bicycle.proximityToVehicle)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                            queries.Add(query8);

                            string query4 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.conditionOfBrakes)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                            queries.Add(query4);

                            string query5 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.wearingOfTyres)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                            queries.Add(query5);

                            string query6 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.trafficCondition, bicycle.location)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                            queries.Add(query6);
                        }

                    }
                }
            }
            if (entity == "Bicycle" && relation.Contains("approach") && (obj == "pothole" || obj == "puddle" || obj == "intersection"
                || obj == "crossing" || obj == "pedestrian"))
            {
                if (perception == "")
                {
                        string query2 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.proximityToObstacle >= {GenerateRandomNumericValue(15, 50)}";
                        queries.Add(query2);

                        string query23 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id, bicycle.location)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.proximityToObstacle <= {GenerateRandomNumericValue(15, 50)}";
                        queries.Add(query23);

                    string query3 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.speed)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.proximityToObstacle <= {GenerateRandomNumericValue(15, 50)}";
                    queries.Add(query3);

                    string query4 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.conditionOfBrakes)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.proximityToObstacle <= {GenerateRandomNumericValue(15, 50)}";
                    queries.Add(query4);

                    string query5 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.wearingOfTyres)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                    queries.Add(query5);

                    string query6 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.trafficCondition)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.proximityToObstacle <= {GenerateRandomNumericValue(15, 50)}";
                    queries.Add(query6);
                }
            }

                if (entity == "Bicycle" && relation.Contains("Rain"))
                {
                    string query3 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.speed)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining""";
                    queries.Add(query3);

                    string query4 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.conditionOfBrakes)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining""";
                    queries.Add(query4);

                    string query5 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.wearingOfTyres)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining""";
                    queries.Add(query5);

                    string query6 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id, bicycle.trafficCondition)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining""";
                    queries.Add(query6);
                }

                if (entity == "Bicycle" && relation.Contains("dooring") || relation.Contains("collision") ||
                   relation.Contains("falling"))
                {
                    string query4 = @$"
                    prefix mv:http://mobivoc.org
                    pull (distinct(bicycle.id, bicycle.location))
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true""";
                    queries.Add(query4);

                    string query5 = @$"
                    prefix mv:http://mobivoc.org
                    pull (distinct(person.id, person.injuryLevel))
                    define
                    entity person is FROM mv:Person where person.injuryLevel = {GenerateRandomNumericValue(1, 5)}";
                    queries.Add(query5);

                    string query6 = @$"
                    prefix mv:http://mobivoc.org
                    pull (distinct(bicycle.location))
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true""";
                    queries.Add(query6);
                }

                if (perception != "")
                {
                  
                        string query2 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.proximityToObstacle)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                        queries.Add(query2);

                        string query23 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.proximityToObstacle, bicycle.location)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                        queries.Add(query23);


                    string query3 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicyle.proximityToObstacle,bicycle.speed)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query3);

                    string query4 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicyle.proximityToObstacle,bicycle.conditionOfBrakes)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                    queries.Add(query4);

                    string query5 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicyle.proximityToObstacle,bicycle.wearingOfTyres)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query5);

                    string query6 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicyle.proximityToObstacle,bicycle.trafficCondition)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query6);
                }


                if (entity == "Bicycle" && relation.Contains("Rain"))
                {
                    string query3 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicyle.weatherCondition,bicycle.speed)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"""; ;
                    queries.Add(query3);

                    string query4 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicyle.weatherCondition,bicycle.conditionOfBrakes)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query4);

                    string query5 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicyle.weatherCondition,bicycle.wearingOfTyres)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query5);

                    string query6 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicyle.weatherCondition, bicycle.trafficCondition)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query6);
                }

                if (entity == "Bicycle" && relation.Contains("dooring") || relation.Contains("collision") ||
                   relation.Contains("falling"))
                {
                    string query4 = @$"
                    prefix mv:http://mobivoc.org
                    pull ( bicycle.collisionState, bicycle.location)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query4);

                    string query5 = @$"
                    prefix mv:http://mobivoc.org
                    pull (person.injuryLevel)
                    define
                    entity person is FROM mv:Person where person.id = ""{GenerateRandomId("Person")}"";";
                    queries.Add(query5);

                    string query6 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.location,bicycle.collisionState )
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query6);
                }

            

            return queries;
        }

        public string GenerateQueries(string entity, string state, string perception, string obj)
        {

            var personId = GenerateRandomId("Person");

            string[] bicycleRelatedAttributes = { "speed", "conditionOfBrakes", "wearingOfTyres",
            "roadCondition","weatherCondition", "trafficCondition", "proximityToVehicle"};
            Dictionary<string, object> bicycleRelatedAttributeValues = new Dictionary<string, object>
        {
            { "conditionOfBrakes", GenerateRandomBrakesEnum() },
            { "wearingOfTyres", GenerateRandomConditionEnum() },
            { "roadCondition", GenerateRandomRoadConditionEnum() },
            { "weatherCondition", GenerateRandomWeatherEnum() },
            { "trafficCondition", GenerateRandomConditionEnum() },
            { "proximityToVehicle", GenerateRandomNumericValue(1, 80)}
            };

            if (state == "Riding")
            {
                bicycleRelatedAttributeValues["speed"] = GenerateRandomNumericValue(0, 7);
            }
            else if (state == "Stopping")

            {
                bicycleRelatedAttributeValues["speed"] = 0;
            }

            string[] carRelatedAttributes = { "speed", "acceleration", "engineStatus", "presenceOfPersonInside",
                "vehicleDoorStatus", "proximityToBicycle" };
            Dictionary<string, object> carRelatedAttributeValues = new Dictionary<string, object>

        {
            {"vehicleDoorStatus",GenerateRandomLockStatus()  },
            { "proximityToBicycle", GenerateRandomNumericValue(1, 80)}
        };
            if (state == "Driving")
            {
                carRelatedAttributeValues["speed"] = GenerateRandomNumericValue(1, 10);
                carRelatedAttributeValues["acceleration"] = GenerateRandomNumericValue(1, 10);
                carRelatedAttributeValues["presenceOfPersonInside"] = 1;
                carRelatedAttributeValues["engineStatus"] = 1;
            }
            else if (state == "Parking")
            {
                carRelatedAttributeValues["speed"] = 0;
                carRelatedAttributeValues["acceleration"] = 0;
                carRelatedAttributeValues["presenceOfPersonInside"] = GenerateRandomBooleanString();
                carRelatedAttributeValues["engineStatus"] = GenerateRandomBooleanString();
            }

            string[] personRelatedAttributes = { "physicalAge", "heartRate", "respiratoryRate"};
            Dictionary<string, object> personRelatedAttributeValues = new Dictionary<string, object>

        {
            { "physicalAge", GenerateRandomNumericValue(23, 36) },
            { "heartRate", GenerateRandomNumericValue(84, 101) },
            { "respiratoryRate", GenerateRandomNumericValue(24, 33) }
            };

            Random random = new Random();
            DateTime startTime = new DateTime(2023, 5, 13, 10, 30, 0); // 13/05/2023 10:30:00 PM
            DateTime endTime = new DateTime(2023, 5, 13, 17, 30, 0); // 13/05/2023 5:30:00 PM

            // Calculate the difference in seconds
            int differenceInSeconds = (int)(endTime - startTime).TotalSeconds;

            // Generate a random number of seconds to add to the start time
            int randomSeconds = random.Next(0, differenceInSeconds);

            // Create the random DateTime in the specified range
            DateTime tempTimestamp = startTime.AddSeconds(randomSeconds);


            int num = 10; // Replace with your num logic
            StringBuilder queriesBuilder = new StringBuilder();

            if (entity == "Bicycle" && (state == "Riding" || state == "Stopping"))
            {
                for (int j = 0; j < bicycleRelatedAttributes.Length; j++)
                {
                    string attributeName = bicycleRelatedAttributes[j];
                    object attributeValue = bicycleRelatedAttributeValues[attributeName];

                    List<string> bicycleQueries = GenerateQueryForbicycle(attributeName, attributeValue,perception);
                   // List<string> bicycleNGSIQueries = ConvertToNGSIQueries(attributeName, attributeValue);
                    

                    foreach (string bicycleQuery in bicycleQueries)
                    {
                        queriesBuilder.AppendLine(bicycleQuery);
                    }

                    //foreach (string bicycleNSGSIQuery in bicycleNGSIQueries)
                    //{
                    //    queriesBuilder.AppendLine(bicycleNSGSIQuery);
                    //}


                    string query2 = GenerateTimestampQuery(entity, bicycleRelatedAttributes[j], tempTimestamp, num);
                    queriesBuilder.AppendLine(query2);
                }
            }



            if (entity == "Car" && state =="Driving" || state == "Parking")
            {
                for (int j = 0; j < carRelatedAttributes.Length; j++)
                {
                    string attributeName = carRelatedAttributes[j];
                    object attributeValue = carRelatedAttributeValues[attributeName];

                    List<string> carQueries = GenerateQueriesForCar(attributeName, attributeValue,perception);
                    foreach (string carQuery in carQueries)
                    {
                        queriesBuilder.AppendLine(carQuery);
                    }
                    string query2 = GenerateTimestampQuery(entity, carRelatedAttributes[j], tempTimestamp, num);
                    queriesBuilder.AppendLine(query2);
                }
            }

            if (entity == "Bicycle" && (state == "Riding" || state == "Stopping"))
            {
                for (int j = 0; j < personRelatedAttributes.Length; j++)
                {
                    string attributeName = personRelatedAttributes[j];
                    object attributeValue = personRelatedAttributeValues[attributeName];

                    List<string> personQueries = GenerateQueriesForPerson(attributeName, attributeValue,perception);
                    foreach (string personQuery in personQueries)
                    {
                        queriesBuilder.AppendLine(personQuery);
                    }
                }
            }

            string allQueries = queriesBuilder.ToString();
            Console.WriteLine(allQueries);

            return allQueries;
        }

        private List<string> GenerateQueriesForCar(string field, object value,string perception)
        {
            string formattedValue;
            List<string> queries = new List<string>();

            if (value is string || value is Enum || value is bool)
            {
                formattedValue = $"\"{value}\"";

                string query2 = @$"
                prefix mv:http://mobivoc.org
                pull (car.*)
                define
                entity car is FROM mv:Car where car.{field} = {formattedValue};";
                queries.Add(query2);

            }

            else if (value is int || value is double || value is float)
            {
                formattedValue = value.ToString();

                string[] operand;
                if (value is int || value is double || value is float)
                {
                    operand = (Convert.ToDouble(value) == 0) ? new string[] { "=", ">=" }
                    : new string[] { "=", ">=", "<=" };
                }
                else
                {
                    operand = (value.Equals(0)) ? new string[] { "=", ">=" }
                    : new string[] { "=", ">=", "<=" };
                }

                if (perception == "")
                {
                    foreach (string operandValue in operand)
                    {
                        // Query with equality condition
                        string query3 = @$"
                        prefix mv:http://mobivoc.org
                        pull (car.*)
                        define
                        entity car is FROM mv:Car where car.{field} {operandValue} {formattedValue};";
                        queries.Add(query3);

                        string query4 = @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(car.vin, car.location))
                        define
                        entity car is FROM mv:Car where car.{field} {operandValue} {formattedValue};";
                        queries.Add(query4);
                    }
                }

                if (perception != "")
                {
                    foreach (string operandValue in operand)
                    {
                        // Query with equality condition
                        string query3 = @$"
                        prefix mv:http://mobivoc.org
                        pull (car.*)
                        define
                        entity car is FROM mv:Car where car.vin = ""{GenerateRandomId("Car")}"";";
                        queries.Add(query3);

                        string query4 = @$"
                        prefix mv:http://mobivoc.org
                        pull (car.vin, car.location)
                        define
                        entity car is FROM mv:Car where car.vin = ""{GenerateRandomId("Car")}"";";
                        queries.Add(query4);
                    }
                }

            }
            return queries;
        }


        private List<string> GenerateQueriesForPerson(string field, object value, string perception)
        {
            string formattedValue;
            List<string> queries = new List<string>();

            if (value is int || value is double || value is float)
            {
                formattedValue = value.ToString();

                if (perception == "")
                {
                        string query2 = @$"
                        prefix mv:http://mobivoc.org
                        pull (person.*)
                        define
                        entity person is FROM mv:Person where person.{field} <= {formattedValue};";
                        queries.Add(query2);

                        string query3 = @$"
                        prefix mv:http://mobivoc.org
                        pull (person.id,person {field})
                        define
                        entity person is FROM mv:Person where person.{field} >= {formattedValue};";
                        queries.Add(query3);

                }

                if (perception != "")
                {

                        string query2 = @$"
                        prefix mv:http://mobivoc.org
                        pull (person.*)
                        define
                        entity person is FROM mv:Person where person.id = ""{GenerateRandomId ("Person")}"";";
                        queries.Add(query2);

                        string query3 = @$"
                        prefix mv:http://mobivoc.org
                        pull (person.physicalAge)
                        define
                        entity person is FROM mv:Person where person.id = ""{GenerateRandomId("Person")}"";";
                        queries.Add(query3);

                        string query4 = @$"
                        prefix mv:http://mobivoc.org
                        pull (person.heartRate)
                        define
                        entity person is FROM mv:Person where person.id = ""{GenerateRandomId("Person")}"";";
                        queries.Add(query4);                    

                }

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

        private static WeatherEnum GenerateRandomWeatherEnum()
        {
            var values = Enum.GetValues(typeof(WeatherEnum));
            var random = new Random();
            return (WeatherEnum)values.GetValue(random.Next(values.Length));
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

        private static string GenerateRandomId(string entity)
        {
            string randomId = null;

            if (entity == "Bicycle")
            {
                Random rand = new Random();
                int randomNumber = rand.Next(0, 40); // Generate a random number between 0 and 36
                randomId = $"BIC{randomNumber}";
            }
            else if (entity == "Car")
            {
                Random rand = new Random();
                int randomNumber = rand.Next(0, 4950);
                randomId = $"CAR{randomNumber}";
            }

            else if (entity == "Person")
            {
                Random rand = new Random();
                int randomNumber = rand.Next(0, 40);
                randomId = $"PERS{randomNumber}";
            }
            return randomId;
        }

        private static Random random = new Random();

        private static string GenerateRandomLockStatus()
        {
            return random.Next(0, 2) == 1 ? "unlock" : "lock";
        }

        private static string GenerateRandomBooleanString()
        {
            return (random.Next(0, 2) == 1).ToString().ToLower();
        }

        static List<string> GenerateQueryForbicycle(string field, object value, string perception)
        {
            string formattedValue;
            List<string> queries = new List<string>();

            if (value is string || value is Enum || value is bool)
            {
                formattedValue = $"\"{value}\"";

                if(perception == "")
                {
                    // Query with equality condition
                    string query1 =
                    @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.*)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.{field} = {formattedValue};";
                    queries.Add(query1);

                    // Query with equality condition
                    string query0 =
                    @$"
                    prefix mv:http://mobivoc.org
                    pull (distinct(bicycle.id))
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.{field} = {formattedValue};";
                    queries.Add(query0);

                    // Query with equality condition
                    string query2 =
                    @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.{field})
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query2);

                    string query6 =
                    @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id, bicycle.location)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.{field} = {formattedValue};";
                    queries.Add(query6);


                    // Query with equality condition
                    string query3 =
                    @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.*)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.{field} = {formattedValue};";
                    queries.Add(query3);

                    string query4 =
                    @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.{field})
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.{field} = {formattedValue};";
                    queries.Add(query4);
                }

                if (perception != "")
                {
                    // Query with equality condition
                    string query1 =
                    @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.*)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query1);

                    // Query with equality condition
                    string query0 =
                    @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.location)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query0);

                    // Query with equality condition
                    string query2 =
                    @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.{field})
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query2);

                    string query4 =
                    @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.{field})
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                    queries.Add(query4);
                }

            }

            else if (value is int || value is double || value is float)
            {
                formattedValue = value.ToString();


                    string query3 =
                    @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.*)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.{field} >= {formattedValue};";
                    queries.Add(query3);

                string query6 =
                @$"
                prefix mv:http://mobivoc.org
                pull (bicycle.id, bicycle.{field})
                define
                entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                queries.Add(query6);
            }

            return queries;
        }

            //public static List<string> ConvertToNGSIQueries(string field, object value)
        //{
        //    string formattedValue;
        //    List<string> queries = new List<string>();

        //    if (value is string || value is Enum || value is bool)
        //    {
        //        formattedValue = value.ToString();
        //        string query1 =
        //               @$"
        //                {{
        //                    ""entities"": [
        //                        {{
        //                            ""type"": ""Bicycle"",
        //                            ""isPattern"": ""true"",
        //                            ""q"": ""{field}=='{formattedValue}'""
        //                        }}
        //                    ]
        //                }}
        //                ";
        //        queries.Add(query1);

        //        string query0 =
        //            @$"
        //    {{
        //        ""entities"": [
        //            {{
        //                ""type"": ""Bicycle"",
        //                ""isPattern"": ""true"",
        //                ""options"": ""keyValues"",
        //                ""attrs"": [""id""],
        //                ""q"": ""{field}=='{formattedValue}'""
        //            }}
        //        ]
        //    }}
        //    ";
        //        queries.Add(query0);

        //        // Query with random id
        //        string query2 =
        //        @$"
        //    {{
        //        ""entities"": [
        //            {{
        //                ""type"": ""Bicycle"",
        //                ""isPattern"": ""true"",
        //                ""id"": ""urn:ngsi-ld:Bicycle:{GenerateRandomId("Bicycle")}""
        //            }}
        //        ]
        //    }}
        //    ";
        //        queries.Add(query2);

        //        // Query with wildcard
        //        string query3 =
        //        @$"
        //    {{
        //        ""entities"": [
        //            {{
        //                ""type"": ""Bicycle"",
        //                ""isPattern"": ""true"",
        //                ""q"": ""{field}=='{formattedValue}'""
        //            }}
        //        ]
        //    }}
        //    ";
        //        queries.Add(query3);

        //        // Query with id and field
        //        string query4 =
        //        @$"
        //    {{
        //        ""entities"": [
        //            {{
        //                ""type"": ""Bicycle"",
        //                ""isPattern"": ""true"",
        //                ""attrs"": [""id"", ""{field}""],
        //                ""q"": ""{field}=='{formattedValue}'""
        //            }}
        //        ]
        //    }}
        //    ";
        //        queries.Add(query4);
        //    }

        //    else if (value is int || value is double || value is float)
        //    {
        //        formattedValue = value.ToString();

        //        // Query with comparison operand
        //        string[] operands = new string[] { "=", ">=", "<=" };
        //      foreach (string operandValue in operands)
        //       {
        //        string query =
        //                @$"
        //        {{
        //            ""entities"": [
        //                {{
        //                    ""type"": ""Bicycle"",
        //                    ""isPattern"": ""true"",
        //                    ""q"": ""{field}{operandValue}{formattedValue}""
        //                }}
        //            ]
        //        }}
        //        ";
        //      queries.Add(query);
        //          }

        //                        // Query with random id
        //                        string query6 =
        //                @$"
        //        {{
        //            ""entities"": [
        //                {{
        //                    ""type"": ""Bicycle"",
        //                    ""isPattern"": ""true"",
        //                    ""id"": ""urn:ngsi-ld:Bicycle:{GenerateRandomId("Bicycle")}""
        //                }}
        //            ]
        //        }}
        //        ";
        //     queries.Add(query6);
        //                    }

        //        return queries;
        //    }


            static string GenerateTimestampQuery(string entity, string field, DateTime timestamp, int num)
            {
                DateTime targetTimestamp = timestamp.AddMinutes(num);
                string formattedTime = targetTimestamp.ToString("dd/MM/yyyy  h:mm:00 tt");

                if (entity == "Bicycle")
                {
                    string query1 = 
                    $@"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id, bicycle.{field})
                    define
                    entity bicycle is FROM mv:Bicycle
                    WHERE bicycle.id = ""{GenerateRandomId("Bicycle")}"" and bicycle.timestamp = '{formattedTime}';";
                    return query1;
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
                    string query1 = $@"
                        prefix mv:http://mobivoc.org
                        pull ({entity}.{field})
                        define
                        entity car is FROM mv:Car
                        WHERE car.vin = ""{GenerateRandomId("Car")}"" and car.timestamp = '{timeVal}';";
                        return query1;
                }
                if (entity == "Person")
                {
                    string query1 = $@"
                    prefix mv:http://mobivoc.org
                    pull ({entity}.{field})
                    define
                    entity person is FROM mv:Person
                    WHERE person.id = ""{GenerateRandomId("Person")}"" and person.timestamp = '{formattedTime}';";
                    return query1;
            }
            return string.Empty; // Return an empty string if the entity is not recognized
        }
    }
}

           
