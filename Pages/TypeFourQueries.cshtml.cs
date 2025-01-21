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
    public class TypeFourQueriesModel : PageModel
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

            List<(string Entity, string State, List<string> Relations, string Obj)> ExtractedEntityAndStates = new List<(string, string, List<string>, string)>
                {
                     ("Bicycle", "Accident",new List<string>{"accident"}, "Car"),
            };

            //("Bicycle", "Riding", new List<string> { "ahead" }, "Car"),
            //         
            //            ("Car", "Driving", new List<string> { "behind" }, "Bicycle"),
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
                || relation.Contains("leftOf") || relation.Contains("infrontOf")))
            {
                string[] operand = new string[] { "=", ">=", "<=" };
                foreach (string operandValue in operand)
                {
                    string query1 = @$"
                    prefix mv:http://mobivoc.org
                    pull (car.vin, bicycle.id, bicycle.location)
                    define
                    entity car is FROM mv:Car where car.proximityToBicycle {operandValue} {GenerateRandomNumericValue(1, 30)} and car.speed = 0,
                    entity bicycle is FROM mv:Bicycle where bicycle.speed {operandValue} {GenerateRandomNumericValue(1, 7)} and bicycle.clusterId = car.clusterId,
                    entity person is FROM mv:Person where person.age >= 25 and person.personId = bicycle.personId;";
                    queries.Add(query1);
                }
            }

            if (entity == "Car" && relation.Contains("overlap"))
            {
                string[] operand = new string[] { "=", ">=", "<=" };
                foreach (string operandValue in operand)
                {
                    string query1 = @$"
                    prefix mv:http://mobivoc.org
                    pull (distinct(car.vin, bicycle.id, person.id))
                    define
                    entity car is FROM mv:Car where car.presenceOfPersonInside = ""true"" and car.speed = 0, 
                    entity bicycle is FROM mv:Bicycle where bicycle.speed {operandValue} {GenerateRandomNumericValue(1, 6)} and bicycle.clusterId = car.clusterId,
                    entity person is FROM mv:Person where person.age >= 30 and person.personId = bicycle.personId;";
                    queries.Add(query1);
                }
            }

            if (entity == "Car" && relation.Contains("open"))
            {
                string query2 = @$"
                prefix mv:http://mobivoc.org
                pull (car.VIN, car.location)
                define
                entity car is FROM mv:Car where car.DoorStatus = 'Open' and car.speed = 0, 
                entity bicycle is FROM mv:Bicycle where bicycle.speed > 0 and bicycle.clusterId = car.clusterId,
                entity person is FROM mv:Person where person.age >= 28 and person.personId = bicycle.personId;";
                queries.Add(query2);
            }

            if (entity == "Bicycle") 
                {
                if (!string.IsNullOrEmpty(relation))
                {

                    if (relation.Contains("ahead") || relation.Contains("behind") || relation.Contains("rightOf")
             || relation.Contains("leftOf") || relation.Contains("infrontOf") & obj == "Car")
                    {
                        string[] operand = new string[] { "=", ">=", "<=" };
                        foreach (string operandValue in operand)
                        {
                            string query1 = @$"
                            prefix mv:http://mobivoc.org
                            pull (car.*, bicyce.*, person.*)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.ProximityToVehicle {operandValue} {GenerateRandomNumericValue(1, 100)} and bicycle.{operandValue} {GenerateRandomNumericValue(1, 10)},
                            entity car is FROM mv:Car where car.presenceOfPersonInside = ""true"" and car.clusterId = bicycle.clusterId,
                            entity person is FROM mv:Person where person.age >= 20 and person.personId = bicycle.personId;";
                            queries.Add(query1);
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
                        entity car is FROM mv:Car where car.proximityToBicycle < 60 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.age <= 18 and person.id = bicycle.personId;";
                        queries.Add(query3);

                        string query4 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id,bicycle.conditionOfBrakes)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.conditionOfBrakes = ""Poor"",
                        entity car is FROM mv:Car where car.proximityToBicycle <= 50 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.age >= 25 and person.id = bicycle.personId;";
                        queries.Add(query4);

                        string query5 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id,bicycle.wearingOfTyres, car.location)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.wearingOfTyres = ""Poor"",
                        entity car is FROM mv:Car where car.proximityToBicycle <= 60 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.age <= 18 and person.id = bicycle.personId;";
                        queries.Add(query5);

                        string query6 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id, bicycle.trafficCondition)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.trafficCondition = ""High"",
                        entity car is FROM mv:Car where car.proximityToBicycle < 60 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.age >= 30 and person.id = bicycle.personId; ";
                        queries.Add(query6);

                        string query7 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id, bicycle.roadCondition)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.roadCondition = ""Wet"",
                        entity car is FROM mv:Car where car.proximityToBicycle <= 60 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.heartRate >= 95 and person.id = bicycle.personId;";
                        queries.Add(query7);

                        string query8 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id, bicycle.location, car.location)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.roadCondition = ""Wet"",
                        entity car is FROM mv:Car where car.proximityToBicycle < 60 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.heartRate >= 95 and person.id = bicycle.personId;";
                        queries.Add(query8);
                    }

                    if (entity == "Bicycle" && relation.Contains("dooring") || relation.Contains("accident") ||
               relation.Contains("falling"))
                    {
                        string query2 = @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(person.id, bicycle.location, car.vin))
                        define
                        entity bicycle is FROM mv:Bicycle
                        entity car is FROM mv:Car where car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.heartRate > 80 and person.collisionState = ""true"" and person.id = bicycle.personId;";
                        queries.Add(query2);

                        string query9 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.location, person.id)
                        define
                        entity bicycle is FROM mv:Bicycle
                        entity car is FROM mv:Car where car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.heartRate > 80 and person.injuryLevel = {GenerateRandomNumericValue(1, 5)} and person.id = bicycle.personId;";
                        queries.Add(query9);

                        string query8 = @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(person.id,person.collisionType ))
                        define
                        entity bicycle is FROM mv:Bicycle
                        entity car is FROM mv:Car where car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.heartRate > 95 and person.njuryLevel = {GenerateRandomNumericValue(1, 5)} and person.id = bicycle.personId;";
                        queries.Add(query8);

                        string query3 = @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(person.id))
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Average""
                        entity car is FROM mv:Car where car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.injuryLevel = {GenerateRandomNumericValue(1, 4)} and person.collisionState = ""true"" and person.id = bicycle.personId;";
                        queries.Add(query3);


                        string query6 = @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(person.id))
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Average""
                        entity car is FROM mv:Car where car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.injuryLevel <= {GenerateRandomNumericValue(1, 4)} and person.collisionState = ""true"" and person.id = bicycle.personId;";
                        queries.Add(query6);

                        string query4 = @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(person.id))
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.wearingOfTyres == ""Poor""
                        entity car is FROM mv:Car where car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.injuryLevel = {GenerateRandomNumericValue(1, 4)} and person.heartRate > {GenerateRandomNumericValue(75, 90)} and person.id = bicycle.personId ;";
                        queries.Add(query4);
                    }
                
            }
            if (entity == "Bicycle" && relation.Contains("overlap"))
            {
                string query1 = @$"
                prefix mv:http://mobivoc.org
                pull (bicycle.id,car.vin,person.id)
                define
                entity bicycle is FROM mv:Bicycle where bicycle.hasObstacle = ""true"" and bicycle.speed >=5,
                entity car is FROM mv:Car where car.presenceOfPersonInside = ""true"" and car.clusterId = bicycle.clusterId,
                entity person is FROM mv:Person where person.age >= 35 and person.personId = bicycle.personId;";
                queries.Add(query1);

            }

            
                return queries;
        }

        public string GenerateQueries(string entity, string state, string obj)
        {

            var personId = GenerateRandomId("Person");
            string[] bicycleRelatedAttributes = {"speed", "conditionOfBrakes", "wearingOfTyres",
            "roadCondition", "trafficCondition", "proximityToVehicle"};
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

            string[] personRelatedAttributes = { "physicalAge", "heartRate"};
            Dictionary<string, object> personRelatedAttributeValues = new Dictionary<string, object>

        {
            { "physicalAge", GenerateRandomNumericValue(15, 60) },
            { "heartRate", GenerateRandomNumericValue(70, 110) }
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

            if (entity == "Bicycle" && state == "Riding" && (obj == "Bicycle"))
            {

                for (int j = 0; j < bicycleRelatedAttributes.Length; j++)
                {
                    string bicycleAttribute = bicycleRelatedAttributes[j];
                    object bicycleValue = bicycleRelatedAttributeValues[bicycleAttribute];

                    foreach (string personAttribute in personRelatedAttributes)
                    {
                            // Retrieve values for both bicycle and person attributes
                            object personValue = personRelatedAttributeValues[personAttribute];
                            List<string> bicycleQueries = GenerateQueriesForThreeEntities(entity, bicycleAttribute, personAttribute, bicycleValue, personValue);
                            foreach (string bicycleQuery in bicycleQueries)
                            {
                                queriesBuilder.AppendLine(bicycleQuery);
                            }

                            List<string> timestampBasedQueries = GenerateTimestampQuery(entity, bicycleAttribute, personAttribute, bicycleValue, personValue, tempTimestamp, num);
                            foreach (string timestampBasedQuery in timestampBasedQueries)
                            {
                                queriesBuilder.AppendLine(timestampBasedQuery);
                            }
                    }
                }

            }

            if (entity == "Car" && state == "Parking" && (obj == "Bicycle" || obj == "Car"))
            {
                for (int j = 0; j < carRelatedAttributes.Length; j++)
                {

                    string carAttribute = carRelatedAttributes[j];
                    object carValue = carRelatedAttributeValues[carAttribute];

                    foreach (string personAttribute in personRelatedAttributes)
                    {
                        // Retrieve values for both bicycle and person attributes
                        object personValue = personRelatedAttributeValues[personAttribute];
                        List<string> carQueries = GenerateQueriesForThreeEntities(entity, carAttribute, personAttribute, carValue, personValue);
                        foreach (string carQuery in carQueries)
                        {
                            queriesBuilder.AppendLine(carQuery);
                        }
                    }

                }
            }
                List<string> queries = new List<string>();
                string[] operand;
                operand = new string[] { "=", ">=", "<=" };

                if (entity == "Car" && state == "Driving" && (obj == "Bicycle" ))
                {

                    foreach (string operandValue in operand)
                    {
                        string query1 =
                            @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(bicycle.id, car.vin, bicycle.location))
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.speed {operandValue} 5 and bicycle.trafficCondition = {GenerateRandomConditionEnum()},
                        entity car is FROM mv:Car where car.speed <= 60  and car.proximityToBicycle <=50 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.age <=30 and person.personId = bicycle.personId;";
                        queriesBuilder.AppendLine(query1);

                    string query2 =
                            @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id, car.vin, bicycle.location)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.speed {operandValue} 5 and bicycle.trafficCondition = {GenerateRandomConditionEnum()},
                        entity car is FROM mv:Car where car.speed <= 30  and car.proximityToBicycle {operandValue} 50 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.age >= 30 and person.personId = bicycle.personId;";
                        queriesBuilder.AppendLine(query2);

                    string query3 =
                            @$"
                        prefix mv:http://mobivoc.org
                        pull (car.vin)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.speed {operandValue} 6 and bicycle.wearingOfTyres = ""Poor"" ,
                        entity car is FROM mv:Car where car.speed >=0  and car.proximityToBicycle {operandValue} 80 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.age >= 30 and person.personId = bicycle.personId;";
                        queriesBuilder.AppendLine(query3);


                    string query4 =
                            @$"
                        prefix mv:http://mobivoc.org
                        pull (car.vin)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.speed {operandValue} 4 and bicycle.weatherCondition = ""Sunny"" ,
                        entity car is FROM mv:Car where car.speed >= 50  and car.proximityToBicycle {operandValue} 80 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.age >= 30 and person.personId = bicycle.personId;";
                        queriesBuilder.AppendLine(query4);


                    string query5 =
                            @$"
                        prefix mv:http://mobivoc.org
                        pull (car.vin)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.speed {operandValue} 5 and bicycle.wearingOfTyres = ""Poor"" ,
                        entity car is FROM mv:Car where car.speed >= 40  and car.proximityToBicycle {operandValue} 40 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.age >= 30 and person.personId = bicycle.personId;";
                        queriesBuilder.AppendLine(query5);
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


        private List<string> GenerateQueriesForThreeEntities(string entity, string field1, string field2, object value1, object value2)
        {
            string formattedValue1;
            string formattedValue2 = null;
            List<string> queries = new List<string>();

            if (value2 is string || value2 is Enum || value2 is bool)
            {
                formattedValue2 = $"\"{value2}\"";

            }

            if (value2 is int || value2 is double || value2 is float)
            {
                formattedValue2 = value2.ToString();
            }

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
                    
                        if (field1 != "trafficCondition") 
                    {
                        formattedValue1 = FormatValue(value1);         

                        // Queries for Bicycle
                        string query1 =
                        @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(bicycle.id, car.vin, bicycle.location))
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.{field1} = {formattedValue1} and bicycle.trafficCondition = {GenerateRandomConditionEnum()},
                        entity car is FROM mv:Car where car.vehicleDoorStatus = {GenerateRandomBoolean()} and car.speed = 0 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.{field2} = {formattedValue2} and person.personId = bicycle.personId;";
                        AddQuery(query1);

                        string query0 =
                        @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(bicycle.id, car.vin))
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.{field1} = {formattedValue1} and bicycle.trafficCondition = {GenerateRandomConditionEnum()},
                        entity car is FROM mv:Car where car.presenceOfPersonInside = ""true"" and car.speed = 0 or car.acceleration = 0 and and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.{field2} = {formattedValue2} and person.personId = bicycle.personId;";
                        AddQuery(query0);

                        if ((value1 is int || value1 is double || value1 is float) &&
                               (value2 is int || value2 is double || value2 is float))
                        {
                            string[] operand;
                            if ((Convert.ToDouble(value1) == 0) && (Convert.ToDouble(value2) == 0))
                            {
                                operand = new string[] { "=", ">=" };
                            }

                            else if ((Convert.ToDouble(value1) >= 0) && (Convert.ToDouble(value2) >= 0))
                            {
                                operand = new string[] { "=", ">=", "<=" };
                            }

                            else
                            {
                                operand = new string[] { "=", ">=" };
                            }

                            
                                string query3 =
                                @$"
                                prefix mv:http://mobivoc.org
                                pull (distinct(bicycle.id, car.id))
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.{field1} <= {formattedValue1} and bicycle.trafficCondition = {GenerateRandomConditionEnum()},
                                entity car is FROM mv:Car where car.speed = 0 or car.acceleration = 0 and car.clusterId = bicycle.clusterId,
                                entity person is FROM mv:Person where person.{field2} <= {formattedValue2} and person.personId = bicycle.personId;";
                                AddQuery(query3);

                                string query7 =
                                @$"
                                prefix mv:http://mobivoc.org
                                pull (bicycle.id, bicycle.{field1}, person.{field2})
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.{field1} >= {formattedValue1} and bicycle.trafficCondition = {GenerateRandomConditionEnum()},
                                entity car is FROM mv:Car where car.vehicleDoorStatus = {GenerateRandomBoolean()} and car.speed = 0 or car.acceleration = 0 and car.clusterId = bicycle.clusterId,
                                entity person is FROM mv:Person where person.{field2} >= {formattedValue2} and person.personId = bicycle.personId;";
                                AddQuery(query7);

                                string query5 =
                                @$"
                                prefix mv:http://mobivoc.org
                                pull (distinct(bicycle.id, bicycle.location, car.id))
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.{field1} <= {formattedValue1} and bicycle.trafficCondition = {GenerateRandomConditionEnum()},
                                entity car is FROM mv:Car where car.vehicleDoorStatus = {GenerateRandomBoolean()} and car.speed = 0 and car.presenceOfPersonInside = ""true"" and car.clusterId = bicycle.clusterId,
                                entity person is FROM mv:Person where person.{field2} <= {formattedValue2} and person.personId = bicycle.personId;";
                                AddQuery(query5);
                                
                                string query6 =
                                @$"
                                prefix mv:http://mobivoc.org
                                pull (distinct(bicycle.id, car.id))
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.{field1} >= {formattedValue1} and bicycle.trafficCondition = {GenerateRandomConditionEnum()},
                                entity car is FROM mv:Car where car.vehicleDoorStatus = {GenerateRandomBoolean()} and car.speed = 0 and car.presenceOfPersonInside = ""true"" and car.clusterId = bicycle.clusterId,
                                entity person is FROM mv:Person where person.{field2} >= {formattedValue2} and person.personId = bicycle.personId;";
                                AddQuery(query6);
                                                           

                        }


                    }
                    break;

                case "Car":
                    if (field1 != "vehicleDoorStatus")
                    {
                        formattedValue1 = FormatValue(value1);

                        // Queries for Car
                        string query1 =
                        @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(bicycle.id, car.vin))
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle = {GenerateRandomNumericValue(0,100)} and bicycle.speed > 0,
                        entity car is FROM mv:Car where car.{field1} = {formattedValue1}and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.{field2} = {formattedValue2} and person.personId = bicycle.personId;";
                        AddQuery(query1);

                        if ((value1 is int || value1 is double || value1 is float) &&
                                (value2 is int || value2 is double || value2 is float))
                        {

                            string[] operand;
                            if ((Convert.ToDouble(value1) == 0) && (Convert.ToDouble(value2) == 0))
                            {
                                operand = new string[] { "=", ">=" };
                            }

                            else if ((Convert.ToDouble(value1) >= 0) && (Convert.ToDouble(value2) >= 0))
                            {
                                operand = new string[] { "=", ">=", "<=" };
                            }

                            else
                            {
                                operand = new string[] { "=", ">=" };
                            }

                                    // Queries for numerical values
                                    string query2 =
                                    @$"
                                    prefix mv:http://mobivoc.org
                                    pull (distinct(bicycle.id, car.vin, bicycle.location))
                                    define
                                    entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle = {GenerateRandomNumericValue(0, 100)} and bicycle.speed > 0,
                                    entity car is FROM mv:Car where car.{field1} >= {formattedValue1} and car.clusterId = bicycle.clusterId,
                                    entity person is FROM mv:Person where person.{field2} <= {formattedValue2} and person.personId = bicycle.personId;";
                                    AddQuery(query2);

                                    string query3 =
                                    @$"
                                    prefix mv:http://mobivoc.org
                                    pull (bicycle.*, car.*, person.*)
                                    define
                                    entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle = {GenerateRandomNumericValue(0, 100)} and bicycle.speed > 0,
                                    entity car is FROM mv:Car where car.{field1} >= {formattedValue1} and car.clusterId = bicycle.clusterId,
                                    entity person is FROM mv:Person where person.{field2} <= {formattedValue2} and person.personId = bicycle.personId;";
                                    AddQuery(query3);
                            
                                   
                        }
                    }
                    break;               
            }

            return queries;
        }

        static List<string> GenerateTimestampQuery(string entity, string field1, string field2, object value1, object value2, DateTime timestamp, int num)
        {
            List<string> queries = new List<string>();
            string formattedValue2 = null ;

            DateTime targetTimestamp = timestamp.AddMinutes(num);
            string formattedTime = targetTimestamp.ToString("dd/MM/yyyy  h:mm:00 tt");

            if (value2 is string || value2 is Enum || value2 is bool)
            {
                formattedValue2 = $"\"{value2}\"";

            }

            if (value2 is int || value2 is double || value2 is float)
            {
                formattedValue2 = value2.ToString();
            }

            if (entity == "Bicycle")
            {
                string formattedValue1;
                if (field1 != "trafficCondition")
                {
                    if (value1 is bool || value1 is string || value1 is Enum)
                    {
                        formattedValue1 = $"\"{value1}\"";

                        string query1 =
                        $@"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.{field1})
                        define
                        entity bicycle is FROM mv:Bicycle where bicyle.{field1} = {formattedValue1} AND bicycle.timestamp = '{formattedTime}',
                        entity car is FROM mv:Car where car.speed = 0 and car.presenceOfPersonInside = ""true"" and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.{field2} = {formattedValue2} and person.personId = bicycle.personId;";
                        queries.Add(query1);

                        string query2 =
                        $@"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.{field1}, car.vin)
                        define
                        entity bicycle is FROM mv:Bicycle where bicyle.{field1} = {formattedValue1} AND bicycle.timestamp = '{formattedTime}',
                        entity car is FROM mv:Car where car.speed = 0 or car.acceleration = 0 and car.clusterId = bicycle.clusterId,
                        entity person is FROM mv:Person where person.{field2} = {formattedValue2} and person.personId = bicycle.personId;";
                        queries.Add(query2);

                    }
                    else if (value1 is int || value1 is double || value1 is float)
                    {
                        formattedValue1 = value1.ToString();

                       string [] operand = new string[] { "=", ">=" };
                        foreach (string operandValue in operand)
                            {
                            string query1 =
                            $@"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.{field1})
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.timestamp = '{formattedTime}' and bicycle.{field1} >= {formattedValue1},
                            entity car is FROM mv:Car where car.speed {operandValue} 0 and car.presenceOfPersonInside = ""true"" and car.clusterId = bicycle.clusterId,
                            entity person is FROM mv:Person where person.{field2} = {formattedValue2} and person.personId = bicycle.personId;";
                            queries.Add(query1);

                            string query2 =
                            $@"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.{field1}, car.vin)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.timestamp = '{formattedTime}' and bicycle.{field1} >= {formattedValue1}',
                            entity car is FROM mv:Car where car.speed {operandValue} 0 or car.acceleration = {operandValue} and car.clusterId = bicycle.clusterId;";
                            queries.Add(query2);

                        }                                             
                    }

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
                string formattedValue1;

                if (value1 is bool || value1 is string || value1 is Enum)
                {
                    formattedValue1 = $"\"{value1}\"";

                    string query1 =
                    $@"
                    prefix mv:http://mobivoc.org
                    pull (car.{field1})
                    define
                    entity car is FROM mv:Car where car.{field1} = {formattedValue1} AND car.timestamp = '{timeVal}',
                    entity bicycle is FROM mv:Bicycle where bicycle.speed> 0 and bicycle.proximityToVehicle = {GenerateRandomNumericValue(0,100)},
                    entity person is FROM mv:Person where person.{ field2} = { formattedValue2} and person.personId = bicycle.personId; ";
                    queries.Add(query1);

                    string query2 =
                    $@"
                    prefix mv:http://mobivoc.org
                    pull (car.{field1}, bicycle.id)
                    define
                    entity car is FROM mv:Car where car.{field1} = {formattedValue1} AND car.timestamp = '{timeVal}',
                    entity bicycle is FROM mv:Bicycle where bicycle.speed > 5 and {GenerateRandomNumericValue(0, 100)};";
                    queries.Add(query2);

                }

                else if (value1 is int || value1 is double || value1 is float)
                {

                    formattedValue1 = value1.ToString();

                    string[] operand = new string[] { "=", ">=" };

                    foreach (string operandValue in operand)
                    {
                        string query1 =
                        $@"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.{field1}, car.vin)
                        define
                        entity car is FROM mv:Car where car.{field1} {operandValue} {formattedValue1} AND car.timestamp = '{timeVal}',
                        entity bicycle is FROM mv:Bicycle where bicycle.speed {operandValue} 0 and bicycle.proximityToVehicle = {GenerateRandomNumericValue(0, 100)},
                        entity person is FROM mv:Person where person.{ field2} = { formattedValue2} and person.personId = bicycle.personId; ";
                        queries.Add(query1);

                    }                        
                }
            }
            return queries;
        }
    }
}
