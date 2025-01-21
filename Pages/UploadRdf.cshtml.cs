using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebApplication1.Pages
{
    public class UploadRdfModel : PageModel
    {
        [BindProperty]
        public IFormFile RdfFile { get; set; }

        public string Message { get; set; }

        [BindProperty]
        public string EncodedData { get; set; }

        public List<(string Entity, string State, List<string> Relation, string Obj, string Perception)> Data { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {

            if (RdfFile == null || RdfFile.Length == 0)
            {
                Message = "Please select a valid RDF file.";
                return Page();
            }

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);
            string filePath = Path.Combine(uploadsFolder, RdfFile.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await RdfFile.CopyToAsync(stream);
            }

            try
            {
                string rdfData;
                using (StreamReader reader = new StreamReader(filePath))
                {
                    rdfData = reader.ReadToEnd();
                }

                var data = new List<(string Entity, string State, List<string> Relation, string Obj, string Perception)>();

                var matches = Regex.Matches(rdfData, @"<owl:NamedIndividual rdf:about=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#Bicycle\d+"">([\s\S]*?)</owl:NamedIndividual>");

                foreach (Match match in matches)
                {
                    string state = Regex.Match(match.Groups[1].Value, @"<KnowledgeGraph:hasState rdf:resource=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#([^""]+)""/>").Groups[1].Value;
                    state = string.IsNullOrEmpty(state) ? "null" : Regex.Replace(state, @"\d+", ""); // Remove numeric part

                    string action = Regex.Match(match.Groups[1].Value, @"<KnowledgeGraph:performAction rdf:resource=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#([^""]+)""/>").Groups[1].Value;
                    action = string.IsNullOrEmpty(action) ? "null" : Regex.Replace(action, @"\d+", ""); // Remove numeric part

                    string @event = Regex.Match(match.Groups[1].Value, @"<KnowledgeGraph:performEvent rdf:resource=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#([^""]+)""/>").Groups[1].Value;
                    @event = string.IsNullOrEmpty(@event) ? "null" : Regex.Replace(@event, @"\d+", ""); // Remove numeric part

                    var situationMatches = Regex.Matches(match.Groups[1].Value, @"<KnowledgeGraph:inSituation rdf:resource=""(http://www.semanticweb.org/ardesilva/ontologies/2024/4/ravdVersion1605#[^""]+)""/>")
                                                .Cast<Match>()
                                                .Select(m => m.Groups[1].Value.Split('#')[1])
                                                .ToList();
                    var situations = situationMatches.Any() ? string.Join(", ", situationMatches) : "null";

                    var specificKeywords = new List<string> { "Puddle", "Intersection", "Crossing", "Dooring", "Accident", "Rain", "MovingVehicleBehind", "ObstacleAhead"};
                    var matchedKeywords = specificKeywords.Where(kw => situationMatches.Any(sm => sm.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

                    var relations = new[] { "isAhead", "isBehind", "rightOf", "leftOf" };
                    var relationResults = new List<string>();
                    var objectResults = new List<string>();

                    foreach (var relation in relations)
                    {
                        var matchRelation = Regex.Match(match.Groups[1].Value, $@"<KnowledgeGraph:{relation} rdf:resource=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#([^""]+)""/>").Groups[1].Value;
                        if (!string.IsNullOrEmpty(matchRelation))
                        {
                            relationResults.Add(relation);
                            objectResults.Add(Regex.Replace(matchRelation, @"\d+", ""));
                        }
                    }

                    var combinedList = relationResults.Concat(new List<string> { action, @event }).Concat(matchedKeywords).Where(s => s != "null").ToList();
                    data.Add(("Bicycle", state, combinedList, string.Join(", ", objectResults), "ThirdPerson"));
                }

                var carMatches = Regex.Matches(rdfData, @"<owl:NamedIndividual rdf:about=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#Car\d+"">([\s\S]*?)</owl:NamedIndividual>");

                foreach (Match match in carMatches)
                {
                    string state = Regex.Match(match.Groups[1].Value, @"<KnowledgeGraph:hasState rdf:resource=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#([^""]+)""/>").Groups[1].Value;
                    state = string.IsNullOrEmpty(state) ? "null" : Regex.Replace(state, @"\d+", ""); // Remove numeric part

                    string action = Regex.Match(match.Groups[1].Value, @"<KnowledgeGraph:performAction rdf:resource=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#([^""]+)""/>").Groups[1].Value;
                    action = string.IsNullOrEmpty(action) ? "null" : Regex.Replace(action, @"\d+", ""); // Remove numeric part

                    string @event = Regex.Match(match.Groups[1].Value, @"<KnowledgeGraph:performEvent rdf:resource=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#([^""]+)""/>").Groups[1].Value;
                    @event = string.IsNullOrEmpty(@event) ? "null" : Regex.Replace(@event, @"\d+", ""); // Remove numeric part

                    var situationMatches = Regex.Matches(match.Groups[1].Value, @"<KnowledgeGraph:inSituation rdf:resource=""([^""]+)""/>")
                                            .Cast<Match>()
                                            .Select(m => m.Groups[1].Value.Split('#')[1])
                                            .ToList();
                    var situations = situationMatches.Any() ? string.Join(", ", situationMatches) : "null";

                    var specificKeywords = new List<string> { "Puddle", "Intersection", "Crossing", "Dooring", "Accident", "Rain", "MovingVehicleBehind", "ObstacleAhead"};
                    var matchedKeywords = specificKeywords.Where(kw => situationMatches.Any(sm => sm.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

                    var relations = new[] { "isAhead", "isBehind", "rightOf", "leftOf" };
                    var relationResults = new List<string>();
                    var objectResults = new List<string>();

                    foreach (var relation in relations)
                    {
                        var matchRelation = Regex.Match(match.Groups[1].Value, $@"<KnowledgeGraph:{relation} rdf:resource=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#([^""]+)""/>").Groups[1].Value;
                        if (!string.IsNullOrEmpty(matchRelation))
                        {
                            relationResults.Add(relation);
                            objectResults.Add(Regex.Replace(matchRelation, @"\d+", ""));
                        }
                    }

                    var combinedList = relationResults.Concat(new List<string> { action, @event }).Concat(matchedKeywords).Where(s => s != "null").ToList();
                    data.Add(("Car", state, combinedList, string.Join(", ", objectResults), "ThirdPerson"));
                }


                var RiderMatches = Regex.Matches(rdfData, @"<owl:NamedIndividual rdf:about=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#Rider\d+"">([\s\S]*?)</owl:NamedIndividual>");

                foreach (Match match in RiderMatches)
                {
                    string action = GetPropertyValue(match.Groups[1].Value, "performAction");
                    string @event = GetPropertyValue(match.Groups[1].Value, "performEvent");

                    var relations = new[] { "isAhead", "isBehind", "rightOf", "leftOf" };
                    var relationResults = new List<string>();

                    foreach (var relation in relations)
                    {
                        var matchRelation = Regex.Match(match.Groups[1].Value, $@"<KnowledgeGraph:{relation} rdf:resource=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#([^""]+)""/>").Groups[1].Value;
                        if (!string.IsNullOrEmpty(matchRelation))
                        {
                            relationResults.Add(relation);
                        }
                    }

                    var combinedList = relationResults.Concat(new List<string> { action, @event }).Where(s => s != "null").ToList();

                    data.Add(("Rider", null, combinedList, null, "ThirdPerson"));
                }

                var DriverMatches = Regex.Matches(rdfData, @"<owl:NamedIndividual rdf:about=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#Driver\d+"">([\s\S]*?)</owl:NamedIndividual>");

                foreach (Match match in DriverMatches)
                {
                    string action = GetPropertyValue(match.Groups[1].Value, "performAction");
                    string @event = GetPropertyValue(match.Groups[1].Value, "performEvent");

                    var relations = new[] { "isAhead", "isBehind", "rightOf", "leftOf" };
                    var relationResults = new List<string>();

                    foreach (var relation in relations)
                    {
                        var matchRelation = Regex.Match(match.Groups[1].Value, $@"<KnowledgeGraph:{relation} rdf:resource=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#([^""]+)""/>").Groups[1].Value;
                        if (!string.IsNullOrEmpty(matchRelation))
                        {
                            relationResults.Add(relation);
                        }
                    }

                    var combinedList = relationResults.Concat(new List<string> { action, @event }).Where(s => s != "null").ToList();

                    data.Add(("Driver", null, combinedList, null, "ThirdPerson"));
                }

                // Helper method to extract property value
                string GetPropertyValue(string input, string propertyName)
                {
                    string value = Regex.Match(input, $@"<KnowledgeGraph:{propertyName} rdf:resource=""http://www.semanticweb.org/ardesilva/KnowledgeGraph#([^""]+)""/>").Groups[1].Value;
                    return string.IsNullOrEmpty(value) ? "null" : Regex.Replace(value, @"\d+", ""); // Remove numeric part
                }

                // Assign the data to the Data property
                Data = data;
                // Serialize to JSON
                string jsonData = JsonConvert.SerializeObject(data);

                var serializedData = JsonConvert.SerializeObject(data);
                EncodedData = System.Web.HttpUtility.UrlEncode(serializedData);

            }
            catch (Exception ex)
            {
                Message = $"Failed to process RDF file: {ex.Message}";
            }

            return Page();
        }

    }
}
