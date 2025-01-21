using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;

namespace WebApplication1.Pages
{
    public class TypeTwoQueriesModel : PageModel
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
        public string GeneratedQueries { get; private set; }

        [BindProperty(SupportsGet = true)]
        public string Data { get; set; }

        public List<(string Entity, string State, List<string> Relation, string Obj, string Perception)> DeserializedData { get; set; }
        public List<string> EntityStates { get; set; }
        

        public async Task<IActionResult> OnGet()
        {

            List<(string Entity, string State, List<string> Relations, string Obj, string Perception)> ExtractedEntityAndStates = new List<(string, string, List<string>, string, string)>
                {
                     ("Bicycle","Accident",new List<string>{"collision"}, "Car", ""),
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

            if (entity == "Car" && (relation.Contains("ahead") || relation.Contains("behind")
                || relation.Contains("leftOf") || relation.Contains("infrontOf")) && obj == "Bicycle")
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
                        entity car is FROM mv:Car where car.proximityToBicycle {operandValue} {GenerateRandomNumericValue(10, 30)}
                        and car.speed = 0;";
                        queries.Add(query1);

                        string query2 = @$"
                        prefix mv:http://mobivoc.org
                        pull (car.vin, car.location)
                        define
                        entity car is FROM mv:Car where car.proximityToBicycle {operandValue} {GenerateRandomNumericValue(10, 30)}
                        and car.speed = 0;";
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
                        entity car is FROM mv:Car where car.proximityToBicycle {operandValue} {GenerateRandomNumericValue(10, 30)}
                        and car.vin = ""{GenerateRandomId ("Car")}"" and car.speed = 0;";
                        queries.Add(query1);

                        string query2 = @$"
                        prefix mv:http://mobivoc.org
                        pull (car.vin, car.location)
                        define
                        entity car is FROM mv:Car where car.proximityToBicycle {operandValue} {GenerateRandomNumericValue(10, 30)}
                        and car.vin = ""{GenerateRandomId("Car")}"" and car.speed = 0; ";
                        queries.Add(query2);
                    }

                }

            }

            if (entity == "Car" && relation.Contains("open"))
            {
                string query2 = @$"
                prefix mv:http://mobivoc.org
                pull (car.vin)
                define
                entity car is FROM mv:Car where car.vehicleDoorStatus = ""unlock"" and car.speed = 0";
                queries.Add(query2);

                string query34 = @$"
                prefix mv:http://mobivoc.org
                pull (car.vin, car.location)
                define
                entity car is FROM mv:Car where car.vehicleDoorStatus = ""unlock"" and car.proximityToBicycle <= 30";
                queries.Add(query34);

                string query35 = @$"
                prefix mv:http://mobivoc.org
                pull (car.vin)
                define
                entity car is FROM mv:Car where car.vehicleDoorStatus = ""unlock"" and car.proximityToBicycle >= 30";
                queries.Add(query35);
            }

            if (entity == "Bicycle" && (obj == "Bicycle" || obj == "Car"))
            {
                if (!string.IsNullOrEmpty(relation))
                {
                    if ((relation.Contains("ahead") || relation.Contains("behind")
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
                                entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle {operandValue} {GenerateRandomNumericValue(15, 40)}
                                and bicycle.speed {operandValue} {GenerateRandomNumericValue(1, 8)}";
                                queries.Add(query1);

                                string query2 = @$"
                                prefix mv:http://mobivoc.org
                                pull (bicycle.id, bicycle.location)
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle {operandValue} {GenerateRandomNumericValue(15, 40)}
                                and bicycle.speed {operandValue} {GenerateRandomNumericValue(1, 8)}";
                                queries.Add(query2);
                            }

                            string query3 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)}
                            and bicycle.speed > 5";
                            queries.Add(query3);

                            string query4 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.conditionOfBrakes)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle <= {GenerateRandomNumericValue(15, 50)}
                            and bicycle.conditionOfBrakes = ""Poor""";
                            queries.Add(query4);

                            string query5 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.wearingOfTyres)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)}
                            and bicycle.wearingOfTyres = ""Poor""";
                            queries.Add(query5);

                            string query6 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.trafficCondition)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle <= {GenerateRandomNumericValue(15, 50)}
                            and bicycle.trafficCondition. = ""High""";
                            queries.Add(query6);

                            string query7 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.speed, bicycle.location)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)}
                            and bicycle.speed > 5";
                            queries.Add(query7);

                        }

                        if (perception != "")
                        {
                            foreach (string operandValue in operand)
                            {
                                string query1 = @$"
                                prefix mv:http://mobivoc.org
                                pull (bicycle.*)
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle {operandValue} {GenerateRandomNumericValue(15, 40)}
                                and bicycle.speed {operandValue} {GenerateRandomNumericValue(1, 8)} and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                                queries.Add(query1);

                                string query2 = @$"
                                prefix mv:http://mobivoc.org
                                pull (bicycle.id, bicycle.location)
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle {operandValue} {GenerateRandomNumericValue(15, 40)}
                                and bicycle.speed {operandValue} {GenerateRandomNumericValue(1, 8)} and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                                queries.Add(query2);
                            }

                            string query3 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)}
                            and bicycle.speed > 5 and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                            queries.Add(query3);

                            string query4 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.conditionOfBrakes)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle <= {GenerateRandomNumericValue(15, 50)}
                            and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                            queries.Add(query4);

                            string query5 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.wearingOfTyres)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)}
                            and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                            queries.Add(query5);

                            string query6 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.trafficCondition)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle <= {GenerateRandomNumericValue(15, 50)}
                            and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                            queries.Add(query6);

                            string query7 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id,bicycle.speed, bicycle.location)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)}
                            and bicycle.speed > 5 and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                            queries.Add(query7);

                        }
                    }

                    if (relation.Contains("rightOf") || relation.Contains("leftOf"))
                    {
                        string query1 = @$"
                                prefix mv:http://mobivoc.org
                                pull (bicycle.*)
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle >= {GenerateRandomNumericValue(15, 40)}
                                and bicycle.speed >= {GenerateRandomNumericValue(1, 8)}";
                                queries.Add(query1);

                        string query2 = @$"
                                prefix mv:http://mobivoc.org
                                pull (bicycle.id, bicycle.location)
                                define
                                entity bicycle is FROM mv:Bicycle where bicycle.proximityToVehicle <= {GenerateRandomNumericValue(15, 40)}
                                and bicycle.speed <= {GenerateRandomNumericValue(1, 8)}";
                                queries.Add(query2);
                    }
                }
            }


            if (entity == "Bicycle" && relation.Contains("Rain"))
            {
                if (perception == "")
                {
                    string query3 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.speed)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.speed >= 5";
                    queries.Add(query3);

                    string query4 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.conditionOfBrakes)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.conditionOfBrakes = ""Poor""";
                    queries.Add(query4);

                    string query5 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.wearingOfTyres)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.wearingOfTyres = ""Poor""";
                    queries.Add(query5);

                    string query6 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id, bicycle.trafficCondition)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.trafficCondition = ""High"" ";
                    queries.Add(query6);

                    string query7 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id, bicycle.roadCondition)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.roadCondition = ""Wet"" ";
                    queries.Add(query7);

                    string query8 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.speed, bicycle.location)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.speed >= 5";
                    queries.Add(query8);
                }                

                if (perception != "")
                {
                    string query3 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.speed)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                    queries.Add(query3);

                    string query4 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.conditionOfBrakes)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                    queries.Add(query4);

                    string query5 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.wearingOfTyres)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                    queries.Add(query5);

                    string query6 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id, bicycle.trafficCondition)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.id = ""{GenerateRandomId("Bicycle")}"" ";
                    queries.Add(query6);

                    string query7 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id, bicycle.roadCondition)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.roadCondition = ""Wet"" and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                    queries.Add(query7);

                    string query8 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id,bicycle.speed, bicycle.location)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.weatherCondition = ""Raining"" and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                    queries.Add(query8);

                }

            }

            if (entity == "Bicycle" && relation.Contains("dooring") || relation.Contains("collision") ||
                relation.Contains("falling"))
            {
                if (perception == "")
                {
                    string query4 = @$"
                    prefix mv:http://mobivoc.org
                    pull (distinct(bicycle.id))
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true"" and bicycle.conditionOfBrakes = ""Poor"" ";
                    queries.Add(query4);

                    string query8 = @$"
                    prefix mv:http://mobivoc.org
                    pull (distinct(bicycle.id, bicycle.conditionOfBrakes))
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true"" and bicycle.conditionOfBrakes = ""Poor"" ";
                    queries.Add(query8);

                    string query6 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id, bicycle.location)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true""";
                    queries.Add(query6);

                    string query2 = @$"
                    prefix mv:http://mobivoc.org
                    pull (distinct(person.id))
                    define
                    entity person is FROM mv:Person where person.heartRate > 80 and person.injuryLevel = {GenerateRandomNumericValue(1, 5)}";
                    queries.Add(query2);

                    string query3 = @$"
                    prefix mv:http://mobivoc.org
                    pull (distinct(person.id))
                    define
                    entity person is FROM mv:Person where person.injuryLevel = {GenerateRandomNumericValue(1, 4)} 
                    and person.collisionState = ""true""";
                    queries.Add(query3);

                }


                if (entity == "Car" && relation.Contains("dooring") || relation.Contains("collision"))

                {
                    if (perception == "")
                    {
                    string query4 = @$"
                    prefix mv:http://mobivoc.org
                    pull (distinct(car.vin))
                    define
                    entity car is FROM mv:Car where car.collisionState = ""true"" ";
                        queries.Add(query4);


                    string query6 = @$"
                    prefix mv:http://mobivoc.org
                    pull (car.vin, car.location)
                    define
                    entity car is FROM mv:Car where car.collisionState = ""true""";
                        queries.Add(query6);                       

                    }
                }


                    if (perception != "")
                {
                    string query4 = @$"
                    prefix mv:http://mobivoc.org
                    pull ((bicycle.location))
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true"" and bicycle.conditionOfBrakes = ""Poor"" and bicycle.id = {GenerateRandomId ("Bicycle")}";
                    queries.Add(query4);

                    string query6 = @$"
                    prefix mv:http://mobivoc.org
                    pull (bicycle.id, bicycle.location)
                    define
                    entity bicycle is FROM mv:Bicycle where bicycle.collisionState = ""true"" and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                    queries.Add(query6);

                    string query2 = @$"
                    prefix mv:http://mobivoc.org
                    pull (person.id, person.injuryLevel)
                    define
                    entity person is FROM mv:Person where person.heartRate > 80 and person.injuryLevel = {GenerateRandomNumericValue(1, 5)} and person.id = ""{GenerateRandomId("Person")}""";
                    queries.Add(query2);

                    string query3 = @$"
                    prefix mv:http://mobivoc.org
                    pull (person.id,person.heartRate)
                    define
                    entity person is FROM mv:Person where person.injuryLevel = {GenerateRandomNumericValue(1, 4)} and person.collisionState = ""true"" and person.id = ""{GenerateRandomId("Person")}"" ";
                    queries.Add(query3);

                }

            }

                if (entity == "Bicycle" && relation.Contains("approach"))
            {
                if (!string.IsNullOrEmpty(obj))
                {
                    if (obj.Contains("Car"))
                    {
                        string query2 = @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.Id)
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.speed >= 3 and bicycle.proximityToVehicle < {GenerateRandomNumericValue(15, 50)}";
                        queries.Add(query2);
                    }
                }
                if (!string.IsNullOrEmpty(obj))
                {

                    if (perception == "")
                    {
                        if (obj.Contains("puddle") || obj.Contains("pothole") || obj.Contains("fallenBranch") || obj.Contains("pedestrian"))

                        {
                            string query9 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.Id)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.speed >= 3 and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query9);

                            string query10 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.Id)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Poor"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query10);

                            string query11 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.Id)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.wearingOfTyres = ""Poor"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query11);

                            string query8 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.Id,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.roadCondition = ""Wet"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query8);

                            string query12 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.Id,bicycle.speed, bicycle.location)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.roadCondition = ""Wet"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query12);
                        }

                        if (obj.Contains("intersection") || obj.Contains("crossing"))
                        {
                            string query4 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.Id,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.speed >= {GenerateRandomNumericValue(2, 5)} and bicycle.proximityToRoadJunction < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query4);

                            string query7 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.Id,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.conditionOfBrakes = ""Poor"" and bicycle.proximityToRoadJunction <= {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query7);

                            string query8 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.Id,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.trafficCondition = ""High"" and bicycle.proximityToRoadJunction < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query8);
                        }
                    }

                    if (perception != "")
                    {
                        if (obj.Contains("puddle") || obj.Contains("pothole")|| obj.Contains("fallenBranch") || obj.Contains("pedestrian"))

                        {
                            string query9 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.id = {GenerateRandomId ("Bicycle")} and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query9);

                            string query10 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.conditionOfBrakes)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query10);

                            string query11 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.wearingOfTyres)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query11);

                            string query8 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.roadCondition,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.roadCondition = ""Wet"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)} and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                            queries.Add(query8);

                            string query12 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.Id,bicycle.speed, bicycle.location)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.roadCondition = ""Wet"" and bicycle.proximityToObstacle < {GenerateRandomNumericValue(15, 50)} and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                            queries.Add(query12);
                        }

                        if (obj.Contains("intersection") || obj.Contains("crossing"))
                        {
                            string query4 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.Id,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"" and bicycle.proximityToRoadJunction < {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query4);

                            string query7 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.Id,bicycle.conditionOfBrakes)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"" and bicycle.proximityToRoadJunction <= {GenerateRandomNumericValue(15, 50)}";
                            queries.Add(query7);

                            string query8 = @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.Id,bicycle.speed)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.trafficCondition = ""High"" and bicycle.proximityToRoadJunction < {GenerateRandomNumericValue(15, 50)} and bicycle.id = ""{GenerateRandomId("Bicycle")}""";
                            queries.Add(query8);
                        }
                    }

                }                
            }

            return queries;
        }

        public string GenerateQueries(string entity, string state, string perception, string obj)
            {

            var personId = GenerateRandomId("Person");
            string[] bicycleRelatedAttributes = { "speed", "conditionOfBrakes", "wearingOfTyres",
            "roadCondition","trafficCondition"};

            Dictionary<string, object> bicycleRelatedAttributeValues = new Dictionary<string, object>
        {
            { "conditionOfBrakes", GenerateRandomBrakesEnum() },
            { "wearingOfTyres", GenerateRandomConditionEnum() },
            { "roadCondition", GenerateRandomRoadConditionEnum() },
            { "trafficCondition", GenerateRandomConditionEnum() },
            { "proximityToVehicle", GenerateRandomNumericValue(0, 100)} };

            if (state == "Riding" || state == "KeepingLane")
            {
                bicycleRelatedAttributeValues["speed"] = GenerateRandomNumericValue(0, 10);
            }
            else if (state == "Stopping")

            {
                bicycleRelatedAttributeValues["speed"] = 0;
            }

            string[] carRelatedAttributes = { "speed", "engineStatus", "presenceOfPersonInside", "vehicleDoorStatus" };
            Dictionary<string, object> carRelatedAttributeValues = new Dictionary<string, object>


        {
            {"vehicleDoorStatus",GenerateRandomBoolean()  }
        };
            if (state == "Driving")
            {
                carRelatedAttributeValues["speed"] = GenerateRandomNumericValue(1, 10);
                carRelatedAttributeValues["presenceOfPersonInside"] = 1;
                carRelatedAttributeValues["engineStatus"] = 1;

            }
            else if (state == "Parking")
            {
                carRelatedAttributeValues["speed"] = 0;
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

                if (entity == "Bicycle" && (state == "Riding" || state == "KeepingLane"))
                {

                            for (int j = 0; j < bicycleRelatedAttributes.Length; j++)
                    {
                        string attributeName = bicycleRelatedAttributes[j];
                        object attributeValue = bicycleRelatedAttributeValues[attributeName];

                        List<string> bicycleQueries = GenerateQueryForBicycle(attributeName, attributeValue, perception);
                        foreach (string bicycleQuery in bicycleQueries)
                        {
                            queriesBuilder.AppendLine(bicycleQuery);
                        }

                        List<string> timestampBasedQueries = GenerateTimestampQuery(entity,state, bicycleRelatedAttributes[j], tempTimestamp, num);
                        foreach (string timestampBasedQuery in timestampBasedQueries)
                        {
                        queriesBuilder.AppendLine(timestampBasedQuery);
                        }
                }
            }

            if (entity == "Car" && state == "Driving") 
            {
                for (int j = 0; j < carRelatedAttributes.Length; j++)
                {
                    string attributeName = carRelatedAttributes[j];
                    object attributeValue = carRelatedAttributeValues[attributeName];

                    List<string> carQueries = GenerateQueriesForCar(attributeName, attributeValue, perception);
                    foreach (string carQuery in carQueries)
                    {
                        queriesBuilder.AppendLine(carQuery);
                    }

                    List<string> timestampBasedQueries = GenerateTimestampQuery(entity, state, bicycleRelatedAttributes[j], tempTimestamp, num);
                    foreach (string timestampBasedQuery in timestampBasedQueries)
                    {
                        queriesBuilder.AppendLine(timestampBasedQuery);
                    }
                }

            }

            if (entity == "Bicycle" && (state == "Riding" || state == "KeepingLane"))
            {
                for (int j = 0; j < personRelatedAttributes.Length; j++)
                {
                    string attributeName = personRelatedAttributes[j];
                    object attributeValue = personRelatedAttributeValues[attributeName];

                    List<string> personQueries = GenerateQueriesForPerson(attributeName, attributeValue, perception);
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

            private List<string> GenerateQueriesForCar(string field, object value, string perception)
            {
                string formattedValue;
                List<string> queries = new List<string>();

            if (value is string || value is Enum || value is bool)
            {
                formattedValue = $"\"{value}\"";               

                    string[] operators = new string[] { "AND", "OR" };

                    for (int i = 0; i < operators.Length; i++)
                    {
                        string operatorType = operators[i];
                    // Query with equality condition

                    if (field != "vin")
                    {
                        string query1 =
                        @$"
                        prefix mv:http://mobivoc.org
                        pull (car.*)
                        define
                        entity car is FROM mv:Car where car.vin = ""{GenerateRandomId("Car")}"" {operatorType} car.{field} = {formattedValue};";
                        queries.Add(query1);
                    } 
                    
                    if (perception == "")
                    {
                        string query2 =
                       @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(car.vin))
                        define
                        entity car is FROM mv:Car where car.{field} = {formattedValue} {operatorType} car.speed = {GenerateRandomNumericValue(0, 100)};";
                        queries.Add(query2);

                        string query3 =
                        @$"
                        prefix mv:http://mobivoc.org
                        pull (distinct(car.vin))
                        define
                        entity car is FROM mv:Car where car.{field} = {formattedValue} {operatorType} car.speed <= {GenerateRandomNumericValue(0, 100)};";
                        queries.Add(query3);
                    }

                    if (perception != "")
                    {
                        string query2 =
                       @$"
                        prefix mv:http://mobivoc.org
                        pull (car.location, car.speed)
                        define
                        entity car is FROM mv:Car where car.{field} = {formattedValue} and car.vin = {GenerateRandomId ("Car")};";
                        queries.Add(query2);

                        string query3 =
                        @$"
                        prefix mv:http://mobivoc.org
                        pull (car.location)
                        define
                        entity car is FROM mv:Car where car.{field} = {formattedValue} {operatorType} car.speed <= {GenerateRandomNumericValue(0, 100)}
                        and car.vin = ""{GenerateRandomId("Car")}"";";
                        queries.Add(query3);
                    }

                }
            }

            else if (value is int || value is double || value is float)
            {
                formattedValue = value.ToString();

                string[] operators = new string[] { "AND", "OR" };

                for (int i = 0; i < operators.Length; i++)
                {
                    string operatorType = operators[i];

                        // Query with equality condition
                        string query3 =
                        @$"
                        prefix mv:http://mobivoc.org
                        pull distinct((car.vin))
                        define
                        entity car is FROM mv:Car where car.vin = ""{GenerateRandomId("Car")}"" {operatorType} car.{field} >= {formattedValue};";
                        queries.Add(query3);
                    
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

                string[] operators = new string[] { "AND", "OR" };
                string[] operand;
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
                    
                    if(perception == "")
                    {
                        foreach (string operandValue in operand)
                        {
                            if (field != "physicalAge")
                            {
                                string query1 = @$"
                            prefix mv:http://mobivoc.org
                            pull (distinct(person.id))
                            define
                            entity person is FROM mv:Person where person.physicalAge {operandValue} {GenerateRandomNumericValue(15, 50)} {operatorType} person.{field} = {formattedValue};";
                                queries.Add(query1);

                            }

                            if (field != "heartRate")
                            {
                                string query2 = @$"
                            prefix mv:http://mobivoc.org
                            pull (distinct(person.id))
                            define
                            entity person is FROM mv:Person where person.heartRate {operandValue} {GenerateRandomNumericValue(70, 120)} {operatorType} person.{field} = {formattedValue};";
                                queries.Add(query2);
                            }
                        }
                    }

                    if (perception != "")
                    {
                        foreach (string operandValue in operand)
                        {
                            if (field != "physicalAge")
                            {
                                string query1 = @$"
                                prefix mv:http://mobivoc.org
                                pull (person.id, person.heartRate)
                                define
                                entity person is FROM mv:Person where person.physicalAge {operandValue} {GenerateRandomNumericValue(15, 50)} and person.id = ""{GenerateRandomId("Person")}"";";
                                queries.Add(query1);

                            }

                            if (field != "heartRate")
                            {
                                string query2 = @$"
                                prefix mv:http://mobivoc.org
                                pull (person.id, person.physicalAge)
                                define
                                entity person is FROM mv:Person where person.heartRate {operandValue} {GenerateRandomNumericValue(70, 120)} and person.id = ""{GenerateRandomId("Person")}"";";
                                queries.Add(query2);
                            }
                        }
                    }

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
                Random rand = new Random();
                string randomId = null;

                if (entity == "Bicycle")
                {
                    int randomNumber = rand.Next(0, 40); 
                    randomId = $"BIC{randomNumber:D2}"; 
                }
                else if (entity == "Car")
                {
                    int randomNumber = rand.Next(1, 4950); 
                    randomId = $"CAR{randomNumber:D2}"; 

                }
                else if (entity == "Person")
                {
                    int randomNumber = rand.Next(0, 40);
                    randomId = $"PERS{randomNumber:D2}";
            }

            return randomId;
            }

            private static bool GenerateRandomBoolean()
            {
                Random random = new Random();
                return random.Next(0, 2) == 1;
            }

            static List<string> GenerateQueryForBicycle(string field, object value, string perception)
            {
                string formattedValue;
                List<string> queries = new List<string>();

                if (value is string || value is Enum || value is bool)
                {
                    formattedValue = $"\"{value}\"";

                        if (field != "id")
                        {
                            // Query with equality condition
                            string query1 =
                            @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.*)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"" AND bicycle.{field} = {formattedValue};";
                            queries.Add(query1);
                        }

                        if(perception == "")
                        {
                            string query2 =
                            @$"
                            prefix mv:http://mobivoc.org
                            pull (distinct(bicycle.id))
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.{field} = {formattedValue} OR bicycle.speed >= {GenerateRandomNumericValue(0, 10)};";
                            queries.Add(query2);

                            string query3 =
                            @$"
                            prefix mv:http://mobivoc.org
                            pull (distinct(bicycle.id, bicycle.location))
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.{field} = {formattedValue} AND bicycle.speed <= {GenerateRandomNumericValue(0, 6)};";
                            queries.Add(query3);
                        }

                        if (perception != "")
                        {
                            string query2 =
                            @$"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.location)
                            define
                            entity bicycle is FROM mv:Bicycle where bicycle.{field} = {formattedValue} and bicycle.id = ""{GenerateRandomId("Bicycle")}"";";
                            queries.Add(query2);

                        }                  
                                
            }

                else if (value is int || value is double || value is float)
                {
                    formattedValue = value.ToString();
                        string query4 =
                        @$"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.id, bicycle.{field})
                        define
                        entity bicycle is FROM mv:Bicycle where bicycle.id = ""{GenerateRandomId("Bicycle")}"" AND bicycle.{field} >= {formattedValue};";
                        queries.Add(query4);                   
                                      
            }

                return queries;
            }

            static List<string> GenerateTimestampQuery(string entity,string state, string field, DateTime timestamp, int num)
            {
                List<string> queries = new List<string>();

                DateTime targetTimestamp = timestamp.AddMinutes(num);
                string formattedTime = targetTimestamp.ToString("dd/MM/yyyy  h:mm:00 tt");

                string[] operand = new string[] { "=", ">=", "<=" };

                    foreach (string operandValue in operand)
                    {
                        if (entity == "Bicycle" && state == "Riding" || state == "KeepingLane")
                        {
                           string query2 =
                            $@"
                            prefix mv:http://mobivoc.org
                            pull (bicycle.id)
                            define
                            entity bicycle is FROM mv:Bicycle
                            WHERE bicyle.speed {operandValue} {GenerateRandomNumericValue(1, 10)} AND bicycle.timestamp = '{formattedTime}';";
                            queries.Add(query2);

                    }

                    if (entity == "Bicycle" && state == "Stopping")
                    {
                        string query2 =
                         $@"
                         prefix mv:http://mobivoc.org
                         pull (bicycle.id)
                         define
                         entity bicycle is FROM mv:Bicycle
                         WHERE bicyle.speed = 0 AND bicycle.timestamp = '{formattedTime}';";
                        queries.Add(query2);
                    }

                    if (entity == "Car" && state == "Driving")
                    {
                        string query2 =
                        $@"
                        prefix mv:http://mobivoc.org
                        pull (car.vin)
                        define
                        entity car is FROM mv:Car;
                        WHERE car.speed {operandValue}  {GenerateRandomNumericValue(1, 100)} AND car.timestamp = '{formattedTime}';";
                        queries.Add(query2);
                    }
                }

                        if (entity == "Bicycle")
                    {
                        string query1 =
                        $@"
                        prefix mv:http://mobivoc.org
                        pull (bicycle.{field})
                        define
                        entity bicycle is FROM mv:Bicycle
                        WHERE bicycle.id = ""{GenerateRandomId("Bicycle")}"" AND bicycle.timestamp = '{formattedTime}';";            
                        queries.Add(query1);
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
                        prefix mv:http://mobivoc.org
                        pull (car.{field})
                        define
                        entity car is FROM mv:Car
                        WHERE car.vin = ""{GenerateRandomId("Car")}"" AND car.timestamp = '{timeVal}';";
                        queries.Add(query1);
                }
                return queries;
        }
    }
}
