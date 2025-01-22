# Scene Graph Query Generator

## 📄 Overview
The **Scene Graph Query Generator** is a Razor Pages web application designed for processing scene graph data from a CSV file and generating context-specific queries. The application reads the CSV, organizes the data into meaningful categories, extracts states of entities, and provides functionality to navigate to a query generation page.

---

## 🚀 Features

1. **CSV File Processing**  
   - Reads a CSV file (`my_uploaded_file.csv`) containing subject, relation, and object data.
   - Configurable column names for `source_label`, `relationship_label`, and `target_label`.

2. **Data Grouping**  
   - Groups CSV data based on spatial relationships.
   - Automatically identifies rows containing entity states (e.g., `EntityState - hasValue - StateValue`).

3. **Entity State Extraction**  
   - Extracts entities and their states from data rows with specific patterns.
   - Saves extracted data in JSON format using `TempData`.

4. **Query Navigation**  
   - Provides a button to navigate to the **Type Five Queries** page for query generation.

---

## 🛠️ Requirements

### Software Requirements
- **ASP.NET Core 6.0 or higher**
- **CsvHelper** for CSV parsing
- **EPPlus** for Excel file handling
- **Newtonsoft.Json** for JSON serialization

### File Requirements
- Place the CSV file (`my_uploaded_file.csv`) in the `wwwroot/excelfiles` directory.

### Installation
1. Install the required NuGet packages:
   ```bash
   dotnet add package CsvHelper
   dotnet add package EPPlus
   dotnet add package Newtonsoft.Json
