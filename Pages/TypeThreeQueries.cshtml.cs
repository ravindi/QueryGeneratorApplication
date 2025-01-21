using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WebApplication1.Pages

{
    public class TypeThreeQueriesModel : PageModel
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

        public async Task<IActionResult> OnGet()
        {

            List<(string Entity, string State, List<string> Relations, String Obj)> ExtractedEntityAndStates = new List<(string, string, List<string>, String)>
                {

                    ("Bicycle", "Riding",new List<string>{"leftOf" }, "Bicycle"),
                      ("Bicycle", "Riding",new List<string>{"rightOf" }, "Bicycle"),

            };

            //
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
                    string queries = GenerateQueries(entity, state, obj);
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

        private List<string> GenerateQueriesForRelations(string entity, string relation, string obj)
        {
            List<string> queries = new List<string>();

            if (entity == "Car" && (relation.Contains("ahead") || relation.Contains("behind") || relation.Contains("rightOf")
                || relation.Contains("leftOf") || relation.Contains("infrontOf")) && obj == "Bicycle")
            {
                string[] operand = new string[] { "=", ">=", "<=" };
                foreach (string operandValue in operand)
                {
                    string query1 = @$"
                    prefix mv:http://mobivoc.org
                    pull (car.*)
                    define
                    entity car is FROM mv:Car where car.proximityToBicycle {operandValue} {GenerateRandomNumericValue(20, 40)} and car.speed = 0 ,
                    entity bicycle is FROM mv:Bicycle where bicycle.speed {operandValue} {GenerateRandomNumericValue(1, 7)} and bicycle.clusterId = car.clusterId;";                    queries.Add(query1);

                    string query2 = @$"
                    prefix mv:http://mobivoc.org
                    pull (car.vin, car.location)
                    define
                    entity car is FROM mv:Car where car.proximityToBicycle {operandValue} {GenerateRandomNumericValue(20, 40)} and car.speed = 0 ,
                    entity bicycle is FROM mv:Bicycle where bicycle.speed {operandValue} {GenerateRandomNumericValue(1, 7)} and bicycle.clusterId = car.clusterId;";
                    queries.Add(query2);
                }
            }

            if (entity == "Car" && relation.Contains("overlap"))
            {
                string[] operand = new string[] { "=", ">=", "<=" };
                foreach (string operandValue in operand)
                {
                    string query1 = @$"
                    prefix mv:http://mobivoc.org
                    pull (car.*)
                    define
                    entity car is FROM mv:Car where car.speed = 0, 
                    entity bicycle is FROM mv:Bicycle where bicycle.speed{operandValue} {GenerateRandomNumericValue(1, 6)} and bicycle.clusterId = car.clusterId;";
                    queries.Add(query1);

                    string query2 = @$"
                    prefix mv:http://mobivoc.org
                    pull (car.vin, car.location)
                    define
                    entity car is FROM mv:Car where car.speed = 0, 
                    entity bicycle is FROM mv:Bicycle where bicycle.speed{operandValue} {GenerateRandomNumericValue(1, 6)} and bicycle.clusterId = car.clusterId;";
                    queries.Add(query2);
                }
            }

            if (entity == "Car" && relation.Contains("open"))
            {
                string query2 = @$"
                prefix mv:http://mobivoc.org
                pull (car.vin)
                define
                entity car is FROM mv:Car where car.vehicleDoorStatus = ""unlock"" and car.speed = 0, 
                entity bicycle is FROM mv:Bicycle where bicycle.speed > 0 and bicycle.clusterId = car.clusterId;";
                queries.Add(query2);

                string query3 = @$"
                prefix mv:http://mobivoc.org
                pull (car.vin, bicycle.id)
                define
                entity car is FROM mv:Car where car.vehicleDoorStatus = ""unlock"" and car.speed = 0, 
                entity bicycle is FROM mv:Bicycle where bicycle.speed >= 3 and bicycle.clusterId = car.clusterId;";
                queries.Add(query3);

                string query4 = @$"
                prefix mv:http://mobivoc.org
                pull (car.vin, bicycle.id)
                define
                entity car is FROM mv:Car where car.vehicleDoorStatus = ""unlock"" and car.speed = 0, 
                entity bicycle is FROM mv:Bicycle where bicycle.speed <= 5 and bicycle.clusterId = car.clusterId;";
                queries.Add(query4);
            }

            if (entity == "Bicycle" && obj == "Car")
            {
            //    if (relation.Contains("ahead") || relation.Contains("behind") || relation.Contains("rightOf")
            // || relation.Contains("leftOf") || relation.Contains("infrontOf"))
            //{
            //        string[] operand = new string[] { "=", ">=", "<=" };
            //        foreach (string operandValue in operand)
            //        {
            //            string query1 = @$"
            //            prefix mv:http://mobivoc.org
            //            pull (bicycle.*)
            //            define
            //            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle{operandValue} {GenerateRandomNumericValue(1, 40)} and bicycle.speed {operandValue} {GenerateRandomNumericValue(1, 6)},
            //            entity car is FROM mv:Car where car.presenceOfPersonInside = ""true"" and car.clusterId = bicycle.clusterId";
            //            queries.Add(query1);

            //            string query2 = @$"
            //            prefix mv:http://mobivoc.org
            //            pull (distinct(bicycle.vin, bicycle.location))
            //            define
            //            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle{operandValue} {GenerateRandomNumericValue(1, 40)} and bicycle.speed {operandValue} {GenerateRandomNumericValue(1, 6)},
            //            entity car is FROM mv:Car where car.presenceOfPersonInside = ""true"" and car.clusterId = bicycle.clusterId";
            //            queries.Add(query2);
            //        }
            //    }
                if (entity == "Bicycle" && relation.Contains("overlap"))
                {
                    string query1 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.*)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.speed <= {GenerateRandomNumericValue(1, 6)},
                    entity car is FROM mv:Car where car.presenceOfPersonInside = ""true"" and car.clusterId = bicycle.clusterId;";
                    queries.Add(query1);

                }
            }

            if (entity == "Bicycle" && obj == "Bicycle")
            {
                if (relation.Contains("ahead") || relation.Contains("behind") || relation.Contains("rightOf")
             || relation.Contains("leftOf") || relation.Contains("infrontOf"))
                {
                    string[] operand = new string[] { "=", ">=", "<=" };
                    foreach (string operandValue in operand)
                    {
                        string query1 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.*)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle{operandValue} {GenerateRandomNumericValue(1, 40)} and bicycle.speed {operandValue} {GenerateRandomNumericValue(1, 6)},
                        entity person is FROM mv:Person where person.age <= 35 and person.id = bicycle.personId;";
                        queries.Add(query1);

                        string query2 = @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(bicycle.vin, bicycle.location))
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle{operandValue} {GenerateRandomNumericValue(1, 40)} and bicycle.speed {operandValue} {GenerateRandomNumericValue(1, 6)},
                        entity person is FROM mv:Person where person.age <= 18 and person.id = bicycle.personId;";
                        queries.Add(query2);
                    }
                }
            }

            if (entity == "Bicycle" && relation.Contains("Rain"))
            {
                string query3 = @$"
                prefix mv:http://mobivoc.org
                pull (bicycle.id,bicycle.speed)
                define
                entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.speed >= 5,
                entity person is FROM mv:Person where person.age <= 18 and person.id = bicycle.personId;";
                queries.Add(query3);

                string query4 = @$"
                prefix mv:http://mobivoc.org
                pull (bicycle.id,bicycle.conditionOfBrakes)
                define
                entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.conditionOfBrakes = ""Poor"",
                entity person is FROM mv:Person where person.age >= 25 and person.id = bicycle.personId;";
                queries.Add(query4);

                string query5 = @$"
                prefix mv:http://mobivoc.org
                pull (bicycle.id,bicycle.wearingOfTyres)
                define
                entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.wearingOfTyres = ""Poor"",
                entity person is FROM mv:Person where person.age <= 18 and person.id = bicycle.personId;";
                queries.Add(query5);

                string query6 = @$"
                prefix mv:http://mobivoc.org
                pull (bicycle.id, bicycle.trafficCondition)
                define
                entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.trafficCondition = ""High"",
                entity person is FROM mv:Person where person.age >= 30 and person.id = bicycle.personId; ";
                queries.Add(query6);

                string query7 = @$"
                prefix mv:http://mobivoc.org
                pull (bicycle.id, bicycle.roadCondition)
                define
                entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.roadCondition = ""Wet"",
                entity person is FROM mv:Person where person.heartRate >= 95 and person.id = bicycle.personId;";
                queries.Add(query7);

                string query8 = @$"
                prefix mv:http://mobivoc.org
                pull (bicycle.id, bicycle.location)
                define
                entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.roadCondition = ""Wet"",
                entity person is FROM mv:Person where person.heartRate >= 95 and person.id = bicycle.personId;";
                queries.Add(query8);
            }


            if (entity == "Bicycle" && relation.Contains("dooring") || relation.Contains("accident") ||
                relation.Contains("falling"))
            {
                string query2 = @$"
                prefix mv:http://mobivoc.org
                pull (distinct(person.id))
                define
                entity bicycle is FROM mv:Bicycle
                entity person is FROM mv:Person where person.heartRate > 80 and person.collisionState = ""true"" and person.id = bicycle.personId;";
                queries.Add(query2);

                string query9 = @$"
                prefix mv:http://mobivoc.org
                pull (bicycle.location, person.id)
                define
                entity bicycle is FROM mv:Bicycle
                entity person is FROM mv:Person where person.heartRate > 80 and person.injuryLevel = {GenerateRandomNumericValue(1, 5)} and person.id = bicycle.personId;";
                queries.Add(query9);

                string query8 = @$"
                prefix mv:http://mobivoc.org
                pull (distinct(person.id ))
                define
                entity bicycle is FROM mv:Bicycle
                entity person is FROM mv:Person where person.heartRate > 95 and person.injuryLevel = {GenerateRandomNumericValue(1, 5)} and person.id = bicycle.personId;";
                queries.Add(query8);

                string query3 = @$"
                prefix mv:http://mobivoc.org
                pull (distinct(person.id))
                define
                entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Average""
                entity person is FROM mv:Person where person.injuryLevel = {GenerateRandomNumericValue(1, 4)} and person.collisionState = ""true"" and person.id = bicycle.personId;";
                queries.Add(query3);


                string query6 = @$"
                prefix mv:http://mobivoc.org
                pull (distinct(person.id))
                define
                entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Average""
                entity person is FROM mv:Person where person.injuryLevel <= {GenerateRandomNumericValue(1, 4)} and person.collisionState = ""true"" and person.id = bicycle.personId;";
                queries.Add(query6);

                string query4 = @$"
                prefix mv:http://mobivoc.org
                pull (distinct(person.id))
                define
                entity bicycle is FROM mv:Bicycle where bicycle.wearingOfTyres == ""Poor""
                entity person is FROM mv:Person where person.injuryLevel = {GenerateRandomNumericValue(1, 4)} and person.heartRate > {GenerateRandomNumericValue(75, 90)} and person.id = bicycle.personId ;";
                queries.Add(query4);
            }

            if (entity == "Car" && relation.Contains("dooring") || relation.Contains("accident") ||
                relation.Contains("falling"))
            {
                string query2 = @$"
                prefix mv:http://mobivoc.org
                pull (distinct(biccyle.id))
                define
                entity car is FROM mv:Car
                entity bicycle is FROM mv:Bicycle where car.collissionState = ""true"" and bicycle.clusterId = car.clusterId";
                queries.Add(query2);

                string query3 = @$"
                prefix mv:http://mobivoc.org
                pull (distinct(bicycle.id, biycle.location))
                define
                entity car is FROM mv:Car
                entity bicycle is FROM mv:Bicycle where car.collissionState = ""true"" and bicycle.clusterId = car.clusterId";
                queries.Add(query3);

            }

                if (entity == "Bicycle" && relation.Contains("approach"))
            {
                if (!string.IsNullOrEmpty(obj))
                {
                    if (obj.Contains("Car"))
                    {
                        string query2 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id)
                        define
                        entity car is FROM mv:car where car.speed >= {GenerateRandomNumericValue(30,60)}
                        entity bicycle is FROM mv:Bicycle where bicycle.speed >= 3 and bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)} and bicycle.clusterId = car.clusterId";
                        queries.Add(query2);
                    }
                }
                if (!string.IsNullOrEmpty(obj))
                {
                    if (obj.Contains("puddle") || obj.Contains("pothole")
                      || obj.Contains("fallenBranch" ) || obj.Contains("pedestrian"))
                    {

                        string[] operand = new string[] { "=", ">=", "<=" };
                        foreach (string operandValue in operand)
                        {
                            string query9 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.speed {operandValue} 3 and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}
                            entity person is FROM mv:Person where person.physicalAge < 30 and person.id = bicycle.personId ;";
                            queries.Add(query9);

                            string query10 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Poor"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}
                            entity person is FROM mv:Person where person.physicalAge {operandValue} 30 and person.id = bicycle.personId ;";
                            queries.Add(query10);
                        }
                        
                        string query11 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.wearingOfTyres = ""Poor"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}
                        entity person is FROM mv:Person where person.physicalAge >= 28 and person.id = bicycle.personId ;";
                        queries.Add(query11);

                        string query7 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id,bicycle.speed)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Poor"" and bicycle.proximityToObstacle <= {GenerateRandomNumericValue(15, 50)}
                        entity person is FROM mv:Person where person.physicalAge < 30 and person.id = bicycle.personId ;";
                        queries.Add(query7);

                        string query8 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id,bicycle.speed)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.roadCondition = ""Wet"" and bicycle.proximityToObstacle <= {GenerateRandomNumericValue(15, 50)}
                        entity person is FROM mv:Person where person.physicalAge > 33 and person.id = bicycle.personId ;";
                        queries.Add(query8);
                        
                    }
                    

                    if (obj.Contains("intersection") || obj.Contains("crossing"))
                    {
                        string[] operand = new string[] { "=", ">=", "<=" };
                        foreach (string operandValue in operand)
                        {
                            string query4 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.speed >= {GenerateRandomNumericValue(2, 5)} and bicycle.proximityToRoadJunction < {GenerateRandomNumericValue(15, 50)}
                            entity person is FROM mv:Person where person.physicalAge {operandValue} 30 and person.id = bicycle.personId ;";
                            queries.Add(query4);

                            string query7 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Poor"" and bicycle.proximityToRoadJunction < {GenerateRandomNumericValue(15, 50)}
                            entity person is FROM mv:Person where person.physicalAge {operandValue} {GenerateRandomNumericValue(25, 35)} and person.id = bicycle.personId ;";
                            queries.Add(query7);


                            string query8 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.trafficCondition = ""High"" and bicycle.proximityToRoadJunction < {GenerateRandomNumericValue(15, 50)}
                            entity person is FROM mv:Person where person.physicalAge {operandValue} {GenerateRandomNumericValue(15, 50)} and person.id = bicycle.personId; ";
                            queries.Add(query8);
                        }
                    }
                }
            }

            return queries;
        }

        public string GenerateQueries(string entity, string state, string obj)
        {

            var personId = GenerateRandomId("Person");

            string[] bicycleRelatedAttributes = { "speed", "conditionOfBrakes", "wearingOfTyres",
            "roadCondition", "trafficCondition", "proximityToVehicle" };
            Dictionary<string, object> bicycleRelatedAttributeValues = new Dictionary<string, object>
        {
            { "conditionOfBrakes", GenerateRandomBrakesEnum() },
            { "wearingOfTyres", GenerateRandomConditionEnum() },
            { "roadCondition", GenerateRandomRoadConditionEnum() },
            { "trafficCondition", GenerateRandomConditionEnum() },
            { "proximityToVehicle", GenerateRandomNumericValue(0, 150)}
            };

            if (state == "Riding")
            {
                bicycleRelatedAttributeValues["speed"] = GenerateRandomNumericValue(0, 10);
            }
            else if (state == "Stopping")

            {
                bicycleRelatedAttributeValues["speed"] = 0;
            }

            string[] carRelatedAttributes = { "speed", "acceleration", "engineStatus", "presenceOfPersonInside", "vehicleDoorStatus" };
            Dictionary<string, object> carRelatedAttributeValues = new Dictionary<string, object>

        {
            {"vehicleDoorStatus",GenerateRandomBoolean()  }
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
                carRelatedAttributeValues["presenceOfPersonInside"] = GenerateRandomBoolean();
                carRelatedAttributeValues["engineStatus"] = GenerateRandomBoolean();
            }

            string[] personRelatedAttributes = { "physicalAge", "heartRate", "respiratoryRate"};
            Dictionary<string, object> personRelatedAttributeValues = new Dictionary<string, object>

        {
            { "physicalAge", GenerateRandomNumericValue(15, 60) },
            { "heartRate", GenerateRandomNumericValue(70, 110) },
            { "respiratoryRate", GenerateRandomNumericValue(20, 30) }
            };

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

            StringBuilder queriesBuilder = new StringBuilder();

            if (entity == "Bicycle" && obj == "Car" && state == "Riding")
            {

                for (int j = 0; j < bicycleRelatedAttributes.Length; j++)
                {
                    string attributeName = bicycleRelatedAttributes[j];
                    object attributeValue = bicycleRelatedAttributeValues[attributeName];

                    List<string> bicycleQueries = GenerateQueriesForTwoEntities(entity, attributeName, attributeValue);
                    foreach (string bicycleQuery in bicycleQueries)
                    {
                        queriesBuilder.AppendLine(bicycleQuery);
                    }
                    //List<string> timestampBasedQueries = GenerateTimestampQuery(entity, bicycleRelatedAttributes[j], tempTimestamp, num, attributeValue);
                    //foreach (string timestampBasedQuery in timestampBasedQueries)
                    //{
                    //    queriesBuilder.AppendLine(timestampBasedQuery);
                    //}
                }
            }

            //if (entity == "Car" && obj == "Bicycle")
            //{
            //    for (int j = 0; j < carRelatedAttributes.Length; j++)
            //    {
            //        string attributeName = carRelatedAttributes[j];
            //        object attributeValue = carRelatedAttributeValues[attributeName];

            //        List<string> carQueries = GenerateQueriesForTwoEntities(entity, attributeName, attributeValue);
            //        foreach (string carQuery in carQueries)
            //        {
            //            queriesBuilder.AppendLine(carQuery);
            //        }
            //        List<string> timestampBasedQueries = GenerateTimestampQuery(entity, carRelatedAttributes[j], tempTimestamp, num, attributeValue);
            //        foreach (string timestampBasedQuery in timestampBasedQueries)
            //        {
            //            queriesBuilder.AppendLine(timestampBasedQuery);
            //        }
            //    }

            //}

            if (entity == "Bicycle" && (state == "Riding" || state == "Stopping"))
            {
                for (int j = 0; j < personRelatedAttributes.Length; j++)
                {
                    string attributeName = personRelatedAttributes[j];
                    object attributeValue = personRelatedAttributeValues[attributeName];

                    List<string> personQueries = GenerateQueriesForTwoEntities("person", attributeName, attributeValue);
                    foreach (string personQuery in personQueries)
                    {
                        queriesBuilder.AppendLine(personQuery);
                    }

                    List<string> timestampBasedQueries = GenerateTimestampQuery("person", personRelatedAttributes[j], tempTimestamp, num, attributeValue);
                    foreach (string timestampBasedQuery in timestampBasedQueries)
                    {
                        queriesBuilder.AppendLine(timestampBasedQuery);
                    }
                }
            }

            string allQueries = queriesBuilder.ToString();
            Console.WriteLine(allQueries);

            return allQueries;
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
            int randomNumber = random.Next(start, end); // Generates a random number between 0 (inclusive) AND 11 (exclusive)
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

        private static bool GenerateRandomBoolean()
        {
            Random random = new Random();
            return random.Next(0, 2) == 1;
        }


        private List<string> GenerateQueriesForTwoEntities(string entity, string field, object value)
        {
            string formattedValue;
            List<string> queries = new List<string>();

            void AddQuery(string query)
            {
                queries.Add(query);
            }

            // Helper function to format values
            string FormatValue(object val)
            {
                return (val is int || val is double || val is float) ? val.ToString() : $"\"{val}\"";
            }

            switch (entity)
            {
                case "Bicycle":
                    formattedValue = FormatValue(value);
                    
                        string[] operand;
                        if (value is int || value is double || value is float)
                        {
                            operand = (Convert.ToDouble(value) == 0) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                        }
                        else
                        {
                            operand = (value.Equals(0)) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                        }
                        string[] operators = new string[] { "AND", "OR" };

                        for (int i = 0; i < operators.Length; i++)
                        {
                            string operatorType = operators[i];
                                // Queries for Bicycle
                                string query1 =
                                @$"
                                prefix mv:http://mobivoc.org
                                pull (distinct(bicycle.id, car.vin))
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.{field} = {formattedValue} ,
                                entity car is FROM mv:Car where car.presenceOfPersonInside = ""true"" {operatorType} car.speed = 0 and where car.clusterId = bicycle.clusterId;";
                                AddQuery(query1);

                                string query2 =
                                @$"
                                prefix mv:http://mobivoc.org
                                pull ((bicycle.id, bicycle.location, car.vin, car.location))
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.{field} = {formattedValue} ,
                                entity car is FROM mv:Car where car.presenceOfPersonInside = ""true"" {operatorType} car.speed = 0 and where car.clusterId = bicycle.clusterId;";
                                AddQuery(query2);                           

                        }
                    
        

                        // join condition
                        if (field != "personId")
                        {                       

                            string query3 =
                            @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.*)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.{field} = {formattedValue},
                            entity person is FROM mv:Person where person.id = bicycle.personId ;";
                            AddQuery(query3);

                            string query4 =
                            @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.*, person.*)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.{field} = {formattedValue},
                            entity person is FROM mv:Person where person.physicalAge < 30 and person.id = bicycle.personId ;";
                            AddQuery(query4);                    
                                
                        }     
                        

                           
                        if (value is int || value is double || value is float)
                        {
                            operand = (Convert.ToDouble(value) == 0) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                        }
                        else
                        {
                            operand = (value.Equals(0)) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                        }
                        
                    for (int i = 0; i < operators.Length; i++)
                        {
                        string operatorType = operators[i];

                                if (field != "wearingOfTyres")
                                    {
                                        string query3 =
                                        @$"
                                        prefix mv:http://mobivoc.org
                                        pull (distinct(bicycle.id, car.vin))
                                        define
                                        entity bicycle is FROM mv:Bicycle where bicycle.{field} >= {formattedValue} {operatorType} bicycle.wearingOfTyres = ""Moderate"",
                                        entity car is FROM mv:Car where and car.speed = 0 and car.clusterId = bicycle.clusterId;";
                                        AddQuery(query3);
                                
                                        string query5 =
                                        @$"
                                        prefix mv:http://mobivoc.org
                                        pull (distinct(bicycle.id, car.vin))
                                        define
                                        entity bicycle is FROM mv:Bicycle where bicycle.{field} <= {formattedValue} {operatorType} bicycle.wearingOfTyres = ""Moderate"",
                                        entity car is FROM mv:Car where car.speed = 0 and car.presenceOfPersonInside = ""true"" and car.clusterId = bicycle.clusterId;";
                                        AddQuery(query5);
                        }
                            }
                    
                    break;

                case "Car":
                    if (field != "trafficCondition")
                    {
                        formattedValue = FormatValue(value);                                        

                            if (value is int || value is double || value is float)
                            {
                                operand = (Convert.ToDouble(value) == 0) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                            }
                            else
                            {
                                operand = (value.Equals(0)) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                            }

                            foreach (string operandValue in operand)
                                {
                                    string query2 =
                                    @$"
                                    prefix mv:http://mobivoc.org
                                    pull (distinct(bicycle.id, car.vin))
                                    define
                                    entity bicycle is FROM mv:Bicycle where bicycle.trafficCondition = ""High"" and bicycle.speed > 0,
                                    entity car is FROM mv:Car where car.{field} {operandValue} {formattedValue} and car.clusterId = bicycle.clusterId;";
                                    AddQuery(query2);

                                    string query3 =
                                    @$"
                                    prefix mv:http://mobivoc.org
                                    pull (distinct(bicycle.id, car.vin))
                                    define
                                    entity bicycle is FROM mv:Bicycle where bicycle.trafficCondition = ""High"" and bicycle.proximityToVehicle > {GenerateRandomNumericValue (0,70)},
                                    entity car is FROM mv:Car where car.{field} {operandValue} {formattedValue} and car.clusterId = bicycle.clusterId;";
                                    AddQuery(query3);
                        }                                  
                        
                    }
                    break;

                case "person":
                    formattedValue = FormatValue(value);                
                        
                        if (value is int || value is double || value is float)
                        {
                            operand = (Convert.ToDouble(value) == 0) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                        }
                        else
                        {
                            operand = (value.Equals(0)) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                        }

                    string[] operators1 = new string[] { "AND", "OR" };

                    for (int i = 0; i < operators1.Length; i++)
                        {
                            string operatorType = operators1[i];
                            foreach (string operandValue in operand)
                            {
                                string query2 =
                                @$"
                                prefix mv:http://mobivoc.org
                                pull (distinct(person.id))
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.speed {operandValue} 0 {operatorType} bicycle.trafficCondition = ""High"",
                                entity person is FROM mv:Person where person.{field} {operandValue} {formattedValue} and person.id = bicycle.personId;";
                                AddQuery(query2);
                            }
                        }
                    
                    break;
            }

            return queries;
        }

        static List<string> GenerateTimestampQuery(string entity, string field, DateTime timestamp, int num, object value)
        {
            List<string> queries = new List<string>();

            DateTime targetTimestamp = timestamp.AddMinutes(num);
            string formattedTime = targetTimestamp.ToString("dd/MM/yyyy  h:mm:00 tt");

            if (entity == "Bicycle")
            {

                string formattedValue;
                
                    if (value is bool || value is string || value is Enum)
                    {
                        formattedValue = $"\"{value}\"";

                        string query1 =
                        $@"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.{field})
                        define
                        entity bicycle is FROM mv:Bicycle where bicyle.{field} = {formattedValue} AND bicycle.timestamp = '{formattedTime}',
                        entity car is FROM mv:Car where car.speed = 0 and car.presenceOfPersonInside = ""true"" and car.clusterId = bicycle.clusterId;"; 
                        queries.Add(query1);

                        string query2 =
                        $@"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.{field}, car.vin)
                        define
                        entity bicycle is FROM mv:Bicycle where bicyle.{field} = {formattedValue} AND bicycle.timestamp = '{formattedTime}',
                        entity car is FROM mv:Car where car.speed = 0 and car.clusterId = bicycle.clusterId;";
                        queries.Add(query2);

                    }
                    else if (value is int || value is double || value is float)
                    {
                        formattedValue = value.ToString();
                        string[] operand;
                        if (value is int || value is double || value is float)
                        {
                            operand = (Convert.ToDouble(value) == 0) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                        }
                        else
                        {
                            operand = (value.Equals(0)) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                        }

                        string query1 =
                        $@"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.{field})
                        define
                        entity bicycle is FROM mv:Bicycle where bicyle.{field} {operand} {formattedValue} and bicycle.timestamp = '{formattedTime}',
                        entity car is FROM mv:Car where car.speed = 0 and car.presenceOfPersonInside = ""true"" and car.clusterId = bicycle.clusterId;";
                        queries.Add(query1);

                        string query2 =
                        $@"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.{field}, car.vin)
                        define
                        entity bicycle is FROM mv:Bicycle where bicyle.{field} {operand} {formattedValue} and bicycle.timestamp = '{formattedTime}',
                        entity car is FROM mv:Car where car.speed = 0 or car.acceleration = 0 and car.clusterId = bicycle.clusterId;";
                        queries.Add(query2);
                }
                    
                            
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
                string formattedValue;

                    if (value is bool || value is string || value is Enum)
                    {
                        formattedValue = $"\"{value}\"";

                    string query1 =
                    $@"
                    prefix mv:http://mobivoc.org
                    pull (car.{field})
                    define
                    entity car is FROM mv:Car where car.{field} = {formattedValue} AND car.timestamp = '{timeVal}',
                    entity bicycle is FROM mv:Bicycle where bicycle.speed> 0 and bicycle.proximityToVehicle = {GenerateRandomNumericValue (0,50)}
                    and bicycle.clusterId = car.clusterId;";
                    queries.Add(query1);

                    string query2 =
                    $@"
                    prefix mv:http://mobivoc.org
                    pull (car.{field}, bicycle.id)
                    define
                    entity car is FROM mv:Car where car.{field} = {formattedValue} AND car.timestamp = '{timeVal}',
                    entity bicycle is FROM mv:Bicycle where bicycle.speed > 5 and bicycle.proximityToVehicle = {GenerateRandomNumericValue(0, 50)}
                    and bicycle.clusterId = car.clusterId;";
                    queries.Add(query2);

            }
            
            else if (value is int || value is double || value is float)
            {
         
                formattedValue = value.ToString();

                    string[] operand;
                    if (value is int || value is double || value is float)
                    {
                        operand = (Convert.ToDouble(value) == 0) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                    }
                    else
                    {
                        operand = (value.Equals(0)) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                    }

                    foreach (string operandValue in operand)
                    {
                        string query1 =
                        $@"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.{field})
                        define
                        entity car is FROM mv:Car where car.{field} {operandValue} {formattedValue} AND car.timestamp = '{timeVal}',
                        entity bicycle is FROM mv:Bicycle where bicycle.speed> 0 and bicycle.proximityToVehicle = {GenerateRandomNumericValue(0, 50)}
                        and bicycle.clusterId = car.clusterId;";
                        queries.Add(query1);

                        string query2 =
                        $@"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.{field})
                        define
                        entity car is FROM mv:Car where car.{field} {operandValue} {formattedValue} AND car.timestamp = '{timeVal}',
                        entity bicycle is FROM mv:Bicycle where bicycle.speed> 0 and bicycle.proximityToVehicle = {GenerateRandomNumericValue(0, 50)}
                        and bicycle.clusterId = car.clusterId;" ;
                        queries.Add(query2);
                    }

                }
            }

            if (entity == "Person")
            {
                string formattedValue;

                if (value is int || value is double || value is float)
                {

                    formattedValue = value.ToString();

                    string[] operand;
                    if (value is int || value is double || value is float)
                    {
                        operand = (Convert.ToDouble(value) == 0) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                    }
                    else
                    {
                        operand = (value.Equals(0)) ? new string[] { "=", ">=" } : new string[] { "=", ">=", "<=" };
                    }

                    foreach (string operandValue in operand)
                    {
                        string query0 =
                        $@"
                        prefix mv:http://mobivoc.org
                        pull (person.{field}, bicycle.id)
                        define
                        entity person is FROM mv:Person where person.{field} {operandValue} {formattedValue} AND person.timestamp = '{formattedTime}',
                        entity bicycle is FROM mv:Bicycle where bicycle.speed> 0 ";
                        queries.Add(query0);
                    }              

                }
            }

            return queries;
            
        }
    }
}
