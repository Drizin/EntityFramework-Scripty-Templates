#r "..\packages\Newtonsoft.Json.12.0.2\lib\net45\Newtonsoft.Json.dll"
#r "..\packages\EntityFramework.6.2.0\lib\net45\EntityFramework.dll"

#load "EFReversePOCOGenerator.csx.cs"

var generator = new EFReversePOCOGenerator(
    context: Context,
    connectionString: @"Data Source=LENOVOFLEX5\SQLEXPRESS;Initial Catalog=northwind;Integrated Security=True;Application Name=EntityFramework Reverse POCO Generator",
    providerName: "System.Data.SqlClient",
    targetFrameworkVersion: 4.6m
    );
await generator.OutputProjectStructure();
