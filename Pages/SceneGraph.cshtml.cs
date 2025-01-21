using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;

namespace WebApplication1.Pages
{
    public class SceneGraphModel : PageModel
    {

        private readonly IWebHostEnvironment _environment;

        public SceneGraphModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet()
        {
            var fileName = "my_uploaded_file.csv"; // Name of your CSV file
            var filePath = Path.Combine(_environment.WebRootPath, "excelfiles", fileName);
            var subjectColumn = "source_label"; // Replace with your subject column name
            var relationColumn = "relationship_label"; // Replace with your relation column name
            var objectColumn = "target_label"; // Replace with your object column name

            var csvData = GetCSVColumnData(filePath, subjectColumn, relationColumn, objectColumn);
            var groupedCSVData = GroupCSVDataBySpatial(csvData);
            ViewData["GroupedCSVData"] = groupedCSVData; // Store in ViewData
        }

        public List<string> GetCSVColumnData(string filePath, string subjectColumn, string relationColumn, string objectColumn)
        {
            var csvData = new List<string>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = context =>
                {
                    // Handle bad data (optional), or simply ignore it by doing nothing
                }
            };

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    var subject = csv.GetField(subjectColumn);
                    var relation = csv.GetField(relationColumn);
                    var obj = csv.GetField(objectColumn);

                    if (subject != null && relation != null && obj != null)
                    {
                        var combinedValue = $"{subject} - {relation} - {obj}";
                        csvData.Add(combinedValue);
                    }
                }
            }

            return csvData;
        }

                public Dictionary<string, List<string>> GroupCSVDataBySpatial(List<string> csvData)
        {
            var groupedData = new Dictionary<string, List<string>>();

            foreach (var combinedValue in csvData)
            {
                var parts = combinedValue.Split(" - ");
                var subject = parts[0];

                var heading = subject.ToLower().Contains("state") ? "Current States" : subject;
                if (groupedData.ContainsKey(heading))
                {
                    groupedData[heading].Add(combinedValue);
                }
                else
                {
                    groupedData[heading] = new List<string> { combinedValue };
                }
            }

            return groupedData;
        }


    }
}
