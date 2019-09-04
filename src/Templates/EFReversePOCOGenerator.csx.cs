using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Pluralization;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Scripty.Core;

namespace EntityFramework_Scripty_Templates
{
    public class EFReversePOCOGenerator
    {
        private EFReversePOCOGenerator() { }
        public EFReversePOCOGenerator(ScriptContext context, string connectionString, string providerName, decimal targetFrameworkVersion)
        {
            this._context = context;

            Settings.ConnectionString = connectionString;
            Settings.ProviderName = providerName;
            Settings.TargetFrameworkVersion = targetFrameworkVersion;
        }
        public static EFReversePOCOGenerator CreateFakeDebugGenerator(string connectionString, string providerName, decimal targetFrameworkVersion)
        {
            Settings.ConnectionString = connectionString;
            Settings.ProviderName = providerName;
            Settings.TargetFrameworkVersion = targetFrameworkVersion;
            return new EFReversePOCOGenerator();
        }

        private readonly ScriptContext _context;
        private Scripty.Core.Output.OutputFile _output;

        #region Configurable Callback methods
        Action<Scripty.Core.Output.OutputFile, Table> WritePocoClassAttributes;
        Action<Scripty.Core.Output.OutputFile, Table> WritePocoClassExtendedComments;
        Action<Scripty.Core.Output.OutputFile, Table> WritePocoBaseClasses;
        Action<Scripty.Core.Output.OutputFile, Table> WritePocoBaseClassBody;
        Action<Scripty.Core.Output.OutputFile, Column> WritePocoColumn;
        #endregion

        public void Configure()
        {
            #region All this code mostly came from Simon Hughes T4 templates (with minor adjustments) - Database.tt - see https://github.com/sjh37/EntityFramework-Reverse-POCO-Code-First-Generator
            // v2.37.4
            // Please make changes to the settings below.
            // All you have to do is save this file, and the output file(s) is/are generated. Compiling does not regenerate the file(s).
            // A course for this generator is available on Pluralsight at https://www.pluralsight.com/courses/code-first-entity-framework-legacy-databases

            // Main settings **********************************************************************************************************************
            Settings.ConnectionStringName = "MyDbContext";   // Searches for this connection string in config files listed below in the ConfigFilenameSearchOrder setting
                                                             // ConnectionStringName is the only required setting.
            Settings.CommandTimeout = 600; // SQL Command timeout in seconds. 600 is 10 minutes, 0 will wait indefinately. Some databases can be slow retrieving schema information.
                                           // As an alternative to ConnectionStringName above, which must match your app/web.config connection string name, you can override them below
                                           // Settings.ConnectionString = "Data Source=(local);Initial Catalog=Northwind;Integrated Security=True;Application Name=EntityFramework Reverse POCO Generator";
                                           // Settings.ProviderName = "System.Data.SqlClient";

            Settings.Namespace = "EntityFramework_Reverse_POCO_Generator"; // Override the default namespace here
            Settings.DbContextName = "MyDbContext"; // Note: If generating separate files, please give the db context a different name from this tt filename.
                                                    //Settings.DbContextInterfaceName = "IMyDbContext"; // Defaults to "I" + DbContextName or set string empty to not implement any interface.
            Settings.DbContextInterfaceBaseClasses = "System.IDisposable";    // Specify what the base classes are for your database context interface
            Settings.DbContextBaseClass = "System.Data.Entity.DbContext";   // Specify what the base class is for your DbContext. For ASP.NET Identity use "Microsoft.AspNet.Identity.EntityFramework.IdentityDbContext<Microsoft.AspNet.Identity.EntityFramework.IdentityUser>";
            Settings.AddParameterlessConstructorToDbContext = true; // If true, then DbContext will have a default (parameterless) constructor which automatically passes in the connection string name, if false then no parameterless constructor will be created.
                                                                    //Settings.DefaultConstructorArgument = null; // Defaults to "Name=" + ConnectionStringName, use null in order not to call the base constructor
            Settings.ConfigurationClassName = "Configuration"; // Configuration, Mapping, Map, etc. This is appended to the Poco class name to configure the mappings.
            Settings.FilenameSearchOrder = new[] { "app.config", "web.config" }; // Add more here if required. The config files are searched for in the local project first, then the whole solution second.
            Settings.GenerateSeparateFiles = false;
            Settings.EntityClassesModifiers = "public"; // "public partial";
            Settings.ConfigurationClassesModifiers = "public"; // "public partial";
            Settings.DbContextClassModifiers = "public"; // "public partial";
            Settings.DbContextInterfaceModifiers = "public"; // "public partial";
            Settings.MigrationClassModifiers = "internal sealed";
            Settings.ResultClassModifiers = "public"; // "public partial";
            Settings.UseMappingTables = true; // If true, mapping will be used and no mapping tables will be generated. If false, all tables will be generated.
            Settings.UsePascalCase = true;    // This will rename the generated C# tables & properties to use PascalCase. If false table & property names will be left alone.
            Settings.UseDataAnnotations = false; // If true, will add data annotations to the poco classes.
            Settings.UseDataAnnotationsWithFluent = false; // If true, then non-Entity Framework-specific DataAnnotations (like [Required] and [StringLength]) will be applied to Entities even if UseDataAnnotations is false.
            Settings.UsePropertyInitializers = false; // Removes POCO constructor and instead uses C# 6 property initialisers to set defaults
            Settings.UseLazyLoading = true; // Marks all navigation properties as virtual or not, to support or disable EF Lazy Loading feature
            Settings.UseInheritedBaseInterfaceFunctions = false; // If true, the main DBContext interface functions will come from the DBContextInterfaceBaseClasses and not generated. If false, the functions will be generated.
            Settings.IncludeComments = CommentsStyle.AtEndOfField; // Adds comments to the generated code
            Settings.IncludeExtendedPropertyComments = CommentsStyle.InSummaryBlock; // Adds extended properties as comments to the generated code
            Settings.IncludeConnectionSettingComments = true; // Add comments describing connection settings used to generate file
            Settings.IncludeViews = true;
            Settings.IncludeSynonyms = false;
            Settings.IncludeStoredProcedures = true;
            Settings.IncludeTableValuedFunctions = false; // If true, you must set IncludeStoredProcedures = true, and install the "EntityFramework.CodeFirstStoreFunctions" Nuget Package.
            Settings.DisableGeographyTypes = false; // Turns off use of System.Data.Entity.Spatial.DbGeography and System.Data.Entity.Spatial.DbGeometry as OData doesn't support entities with geometry/geography types.
                                                    //Settings.CollectionInterfaceType = "System.Collections.Generic.List"; // Determines the declaration type of collections for the Navigation Properties. ICollection is used if not set.
            Settings.CollectionType = "System.Collections.Generic.List";  // Determines the type of collection for the Navigation Properties. "ObservableCollection" for example. Add "System.Collections.ObjectModel" to AdditionalNamespaces if setting the CollectionType = "ObservableCollection".
            Settings.NullableShortHand = true; //true => T?, false => Nullable<T>
            Settings.AddIDbContextFactory = true; // Will add a default IDbContextFactory<DbContextName> implementation for easy dependency injection
            Settings.AddUnitTestingDbContext = true; // Will add a FakeDbContext and FakeDbSet for easy unit testing
            Settings.IncludeQueryTraceOn9481Flag = false; // If SqlServer 2014 appears frozen / take a long time when this file is saved, try setting this to true (you will also need elevated privileges).
            Settings.IncludeCodeGeneratedAttribute = true; // If true, will include the GeneratedCode attribute, false to remove it.
            Settings.UsePrivateSetterForComputedColumns = true; // If the columns is computed, use a private setter.
            Settings.AdditionalNamespaces = new[] { "" };  // To include extra namespaces, include them here. i.e. "Microsoft.AspNet.Identity.EntityFramework"
            Settings.AdditionalContextInterfaceItems = new[] // To include extra db context interface items, include them here. Also set DbContextClassModifiers="public partial", and implement the partial DbContext class functions.
            {
        ""  //  example: "void SetAutoDetectChangesEnabled(bool flag);"
    };
            // If you need to serialize your entities with the JsonSerializer from Newtonsoft, this would serialize
            // all properties including the Reverse Navigation and Foreign Keys. The simplest way to exclude them is
            // to use the data annotation [JsonIgnore] on reverse navigation and foreign keys.
            // For more control, take a look at ForeignKeyAnnotationsProcessing() further down
            Settings.AdditionalReverseNavigationsDataAnnotations = new string[] // Data Annotations for all ReverseNavigationProperty.
            {
                // "JsonIgnore" // Also add "Newtonsoft.Json" to the AdditionalNamespaces array above
            };
            Settings.AdditionalForeignKeysDataAnnotations = new string[] // Data Annotations for all ForeignKeys.
            {
                // "JsonIgnore" // Also add "Newtonsoft.Json" to the AdditionalNamespaces array above
            };
            Settings.ColumnNameToDataAnnotation = new Dictionary<string, string>
    {
        // This is used when UseDataAnnotations == true or UseDataAnnotationsWithFluent == true;
        // It is used to set a data annotation on a column based on the columns name.
        // Make sure the column name is lowercase in the following array, regardless of how it is in the database
        // Column name       DataAnnotation to add
        { "email",           "EmailAddress" },
        { "emailaddress",    "EmailAddress" },
        { "creditcard",      "CreditCard" },
        { "url",             "Url" },
        { "fax",             "Phone" },
        { "phone",           "Phone" },
        { "phonenumber",     "Phone" },
        { "mobile",          "Phone" },
        { "mobilenumber",    "Phone" },
        { "telephone",       "Phone" },
        { "telephonenumber", "Phone" },
        { "password",        "DataType(DataType.Password)" },
        { "username",        "DataType(DataType.Text)" },
        { "postcode",        "DataType(DataType.PostalCode)" },
        { "postalcode",      "DataType(DataType.PostalCode)" },
        { "zip",             "DataType(DataType.PostalCode)" },
        { "zipcode",         "DataType(DataType.PostalCode)" }
    };
            Settings.ColumnTypeToDataAnnotation = new Dictionary<string, string>
    {
        // This is used when UseDataAnnotations == true or UseDataAnnotationsWithFluent == true;
        // It is used to set a data annotation on a column based on the columns's MS SQL type.
        // Make sure the column name is lowercase in the following array, regardless of how it is in the database
        // Column name       DataAnnotation to add
        { "date",            "DataType(DataType.Date)" },
        { "datetime",        "DataType(DataType.DateTime)" },
        { "datetime2",       "DataType(DataType.DateTime)" },
        { "datetimeoffset",  "DataType(DataType.DateTime)" },
        { "smallmoney",      "DataType(DataType.Currency)" },
        { "money",           "DataType(DataType.Currency)" }
    };

            // Migrations *************************************************************************************************************************
            Settings.MigrationConfigurationFileName = ""; // null or empty to not create migrations
            Settings.MigrationStrategy = "MigrateDatabaseToLatestVersion"; // MigrateDatabaseToLatestVersion, CreateDatabaseIfNotExists or DropCreateDatabaseIfModelChanges
            Settings.ContextKey = ""; // Sets the string used to distinguish migrations belonging to this configuration from migrations belonging to other configurations using the same database. This property enables migrations from multiple different models to be applied to applied to a single database.
            Settings.AutomaticMigrationsEnabled = true;
            Settings.AutomaticMigrationDataLossAllowed = true; // if true, can drop fields and lose data during automatic migration

            // Pluralization **********************************************************************************************************************
            // To turn off pluralization, use:
            //      Inflector.PluralizationService = null;
            // Default pluralization, use:
            //      Inflector.PluralizationService = new EnglishPluralizationService();
            // For Spanish pluralization:
            //      1. Intall the "EF6.Contrib" Nuget Package.
            //      2. Add the following to the top of this file and adjust path, and remove the space between the angle bracket and # at the beginning and end.
            //         < #@ assembly name="your full path to \EntityFramework.Contrib.dll" # >
            //      3. Change the line below to: Inflector.PluralizationService = new SpanishPluralizationService();
            Inflector.PluralizationService = new EnglishPluralizationService();
            // If pluralisation does not do the right thing, override it here by adding in a custom entry.
            //Inflector.PluralizationService = new EnglishPluralizationService(new[]
            //{
            //    // Create custom ("Singular", "Plural") forms for one-off words as needed.
            //    new CustomPluralizationEntry("Course", "Courses"),
            //    new CustomPluralizationEntry("Status", "Status") // Use same value to prevent pluralisation
            //});


            // Elements to generate ***************************************************************************************************************
            // Add the elements that should be generated when the template is executed.
            // Multiple projects can now be used that separate the different concerns.
            Settings.ElementsToGenerate = Elements.Poco | Elements.Context | Elements.UnitOfWork | Elements.PocoConfiguration;

            // Use these namespaces to specify where the different elements now live. These may even be in different assemblies.
            // Please note this does not create the files in these locations, it only adds a using statement to say where they are.
            // The way to do this is to add the "EntityFramework Reverse POCO Code First Generator" into each of these folders.
            // Then set the .tt to only generate the relevant section you need by setting
            //      ElementsToGenerate = Elements.Poco; in your Entity folder,
            //      ElementsToGenerate = Elements.Context | Elements.UnitOfWork; in your Context folder,
            //      ElementsToGenerate = Elements.PocoConfiguration; in your Maps folder.
            //      PocoNamespace = "YourProject.Entities";
            //      ContextNamespace = "YourProject.Context";
            //      UnitOfWorkNamespace = "YourProject.Context";
            //      PocoConfigurationNamespace = "YourProject.Maps";
            // You also need to set the following to the namespace where they now live:
            Settings.PocoNamespace = "";
            Settings.ContextNamespace = "";
            Settings.UnitOfWorkNamespace = "";
            Settings.PocoConfigurationNamespace = "";


            // Schema *****************************************************************************************************************************
            // If there are multiple schemas, then the table name is prefixed with the schema, except for dbo.
            // Ie. dbo.hello will be Hello.
            //     abc.hello will be AbcHello.
            Settings.PrependSchemaName = true;   // Control if the schema name is prepended to the table name

            // Table Suffix ***********************************************************************************************************************
            // Prepends the suffix to the generated classes names
            // Ie. If TableSuffix is "Dto" then Order will be OrderDto
            //     If TableSuffix is "Entity" then Order will be OrderEntity
            Settings.TableSuffix = null;

            // Filtering **************************************************************************************************************************
            // Use the following table/view name regex filters to include or exclude tables/views
            // Exclude filters are checked first and tables matching filters are removed.
            //  * If left null, none are excluded.
            //  * If not null, any tables matching the regex are excluded.
            // Include filters are checked second.
            //  * If left null, all are included.
            //  * If not null, only the tables matching the regex are included.
            // For clarity: if you want to include all the customer tables, but not the customer billing tables.
            //      TableFilterInclude = new Regex("^[Cc]ustomer.*"); // This includes all the customer and customer billing tables
            //      TableFilterExclude = new Regex(".*[Bb]illing.*"); // This excludes all the billing tables
            //
            // Example:     TableFilterExclude = new Regex(".*auto.*");
            //              TableFilterInclude = new Regex("(.*_FR_.*)|(data_.*)");
            //              TableFilterInclude = new Regex("^table_name1$|^table_name2$|etc");
            //              ColumnFilterExclude = new Regex("^FK_.*$");
            Settings.SchemaFilterExclude = null;
            Settings.SchemaFilterInclude = null;
            Settings.TableFilterExclude = null;
            Settings.TableFilterInclude = null;
            Settings.ColumnFilterExclude = null;

            // Filtering of tables using a function. This can be used in conjunction with the Regex's above.
            // Regex are used first to filter the list down, then this function is run last.
            // Return true to include the table, return false to exclude it.
            Settings.TableFilter = (Table t) =>
            {
                // Example: Exclude any table in dbo schema with "order" in its name.
                //if(t.Schema.Equals("dbo", StringComparison.InvariantCultureIgnoreCase) && t.NameHumanCase.ToLowerInvariant().Contains("order"))
                //    return false;

                return true;
            };


            // Stored Procedures ******************************************************************************************************************
            // Use the following regex filters to include or exclude stored procedures
            Settings.StoredProcedureFilterExclude = null;
            Settings.StoredProcedureFilterInclude = null;

            // Filtering of stored procedures using a function. This can be used in conjunction with the Regex's above.
            // Regex are used first to filter the list down, then this function is run last.
            // Return true to include the stored procedure, return false to exclude it.
            Settings.StoredProcedureFilter = (StoredProcedure sp) =>
            {
                // Example: Exclude any stored procedure in dbo schema with "order" in its name.
                //if(sp.Schema.Equals("dbo", StringComparison.InvariantCultureIgnoreCase) && sp.NameHumanCase.ToLowerInvariant().Contains("order"))
                //    return false;

                return true;
            };


            // Table renaming *********************************************************************************************************************
            // Use the following function to rename tables such as tblOrders to Orders, Shipments_AB to Shipments, etc.
            // Example:
            Settings.TableRename = (string name, string schema, bool isView) =>
            {
                // Example
                //if (name.StartsWith("tbl"))
                //    name = name.Remove(0, 3);
                //name = name.Replace("_AB", "");

                //if(isView)
                //    name = name + "View";

                // If you turn pascal casing off (UsePascalCase = false), and use the pluralisation service, and some of your
                // tables names are all UPPERCASE, some words ending in IES such as CATEGORIES get singularised as CATEGORy.
                // Therefore you can make them lowercase by using the following
                // return Inflector.MakeLowerIfAllCaps(name);

                // If you are using the pluralisation service and you want to rename a table, make sure you rename the table to the plural form.
                // For example, if the table is called Treez (with a z), and your pluralisation entry is
                //     new CustomPluralizationEntry("Tree", "Trees")
                // Use this TableRename function to rename Treez to the plural (not singular) form, Trees:
                // if (name == "Treez") return "Trees";

                return name;
            };

            // Mapping Table renaming *********************************************************************************************************************
            // By default, name of the properties created relate to the table the foreign key points to and not the mapping table.
            // Use the following function to rename the properties created by ManytoMany relationship tables especially if you have 2 relationships between the same tables.
            // Example:
            Settings.MappingTableRename = (string mappingtable, string tablename, string entityname) =>
            {

                // Examples:
                // If you have two mapping tables such as one being UserRequiredSkills snd one being UserOptionalSkills, this would change the name of one property
                // if (mappingtable == "UserRequiredSkills" and tablename == "User")
                //    return "RequiredSkills";

                // or if you want to give the same property name on both classes
                // if (mappingtable == "UserRequiredSkills")
                //    return "UserRequiredSkills";

                return entityname;
            };

            // Column modification*****************************************************************************************************************
            // Use the following list to replace column byte types with Enums.
            // As long as the type can be mapped to your new type, all is well.
            //Settings.EnumDefinitions.Add(new EnumDefinition { Schema = "dbo", Table = "match_table_name", Column = "match_column_name", EnumType = "name_of_enum" });
            //Settings.EnumDefinitions.Add(new EnumDefinition { Schema = "dbo", Table = "OrderHeader", Column = "OrderStatus", EnumType = "OrderStatusType" }); // This will replace OrderHeader.OrderStatus type to be an OrderStatusType enum
            //Settings.EnumDefinitions.Add(new EnumDefinition { Schema = "dbo", Table = "*", Column = "OrderStatus", EnumType = "OrderStatusType" }); // This will replace any table *.OrderStatus type to be an OrderStatusType enum

            // Use the following function if you need to apply additional modifications to a column
            // eg. normalise names etc.
            Settings.UpdateColumn = (Column column, Table table) =>
            {
                // Rename column
                //if (column.IsPrimaryKey && column.NameHumanCase == "PkId")
                //    column.NameHumanCase = "Id";

                // .IsConcurrencyToken() must be manually configured. However .IsRowVersion() can be automatically detected.
                //if (table.NameHumanCase.Equals("SomeTable", StringComparison.InvariantCultureIgnoreCase) && column.NameHumanCase.Equals("SomeColumn", StringComparison.InvariantCultureIgnoreCase))
                //    column.IsConcurrencyToken = true;

                // Remove table name from primary key
                //if (column.IsPrimaryKey && column.NameHumanCase.Equals(table.NameHumanCase + "Id", StringComparison.InvariantCultureIgnoreCase))
                //    column.NameHumanCase = "Id";

                // Remove column from poco class as it will be inherited from a base class
                //if (column.IsPrimaryKey && table.NameHumanCase.Equals("SomeTable", StringComparison.InvariantCultureIgnoreCase))
                //    column.Hidden = true;

                // Use the extended properties to perform tasks to column
                //if (column.ExtendedProperty == "HIDE")
                //    column.Hidden = true;

                // Apply the "override" access modifier to a specific column.
                // if (column.NameHumanCase == "id")
                //    column.OverrideModifier = true;
                // This will create: public override long id { get; set; }

                // Perform Enum property type replacement
                var enumDefinition = Settings.EnumDefinitions.FirstOrDefault(e =>
                    (e.Schema.Equals(table.Schema, StringComparison.InvariantCultureIgnoreCase)) &&
                    (e.Table == "*" || e.Table.Equals(table.Name, StringComparison.InvariantCultureIgnoreCase) || e.Table.Equals(table.NameHumanCase, StringComparison.InvariantCultureIgnoreCase)) &&
                    (e.Column.Equals(column.Name, StringComparison.InvariantCultureIgnoreCase) || e.Column.Equals(column.NameHumanCase, StringComparison.InvariantCultureIgnoreCase)));

                if (enumDefinition != null)
                {
                    column.PropertyType = enumDefinition.EnumType;
                    if (!string.IsNullOrEmpty(column.Default))
                        column.Default = "(" + enumDefinition.EnumType + ") " + column.Default;
                }

                return column;
            };


            // Using Views *****************************************************************************************************************
            // SQL Server does not support the declaration of primary-keys in VIEWs. Entity Framework's EDMX designer (and this T4 template)
            // assume that all non-null columns in a VIEW are primary-key columns, this will be incorrect for most non-trivial applications.
            // This callback will be invoked for each VIEW found in the database. Use it to declare which columns participate in that VIEW's
            // primary-key by setting 'IsPrimaryKey = true'.
            // If no columns are marked with 'IsPrimaryKey = true' then this T4 template defaults to marking all non-NULL columns as primary key columns.
            // To set-up Foreign-Key relationships between VIEWs and Tables (or even other VIEWs) use the 'AddForeignKeys' callback below.
            Settings.ViewProcessing = (Table view) =>
            {
                // Below is example code for the Northwind database that configures the 'VIEW [Orders Qry]' and 'VIEW [Invoices]'
                //switch(view.Name)
                //{
                //case "Orders Qry":
                //    // VIEW [Orders Qry] uniquely identifies rows with the 'OrderID' column:
                //    view.Columns.Single( col => col.Name == "OrderID" ).IsPrimaryKey = true;
                //    break;
                //case "Invoices":
                //    // VIEW [Invoices] has a composite primary key (OrderID+ProductID), so both columns must be marked as a Primary Key:
                //    foreach( Column col in view.Columns.Where( c => c.Name == "OrderID" || c.Name == "ProductID" ) ) col.IsPrimaryKey = true;
                //    break;
                //}
            };

            Settings.AddForeignKeys = (List<ForeignKey> foreignKeys, Tables tablesAndViews) =>
            {
                // In Northwind:
                // [Orders] (Table) to [Invoices] (View) is one-to-many using Orders.OrderID = Invoices.OrderID
                // [Order Details] (Table) to [Invoices] (View) is one-to-zeroOrOne - but uses a composite-key: ( [Order Details].OrderID,ProductID = [Invoices].OrderID,ProductID )
                // [Orders] (Table) to [Orders Qry] (View) is one-to-zeroOrOne ( [Orders].OrderID = [Orders Qry].OrderID )

                // AddRelationship is a helper function that creates ForeignKey objects and adds them to the foreignKeys list:
                //AddRelationship( foreignKeys, tablesAndViews, "orders_to_invoices"      , "dbo", "Orders"       , "OrderID"                       , "dbo", "Invoices", "OrderID" );
                //AddRelationship( foreignKeys, tablesAndViews, "orderDetails_to_invoices", "dbo", "Order Details", new[] { "OrderID", "ProductID" }, "dbo", "Invoices",  new[] { "OrderID", "ProductID" } );
                //AddRelationship( foreignKeys, tablesAndViews, "orders_to_ordersQry"     , "dbo", "Orders"       , "OrderID"                       , "dbo", "Orders Qry", "OrderID" );
            };

            // StoredProcedure renaming ************************************************************************************************************
            // Use the following function to rename stored procs such as sp_CreateOrderHistory to CreateOrderHistory, my_sp_shipments to Shipments, etc.
            // Example:
            /*Settings.StoredProcedureRename = (sp) =>
            {
                if (sp.NameHumanCase.StartsWith("sp_"))
                    return sp.NameHumanCase.Remove(0, 3);
                return sp.NameHumanCase.Replace("my_sp_", "");
            };*/
            Settings.StoredProcedureRename = (sp) => sp.NameHumanCase;   // Do nothing by default

            // Use the following function to rename the return model automatically generated for stored procedure.
            // By default it's <proc_name>ReturnModel.
            // Example:
            /*Settings.StoredProcedureReturnModelRename = (name, sp) =>
            {
                if (sp.NameHumanCase.Equals("ComputeValuesForDate", StringComparison.InvariantCultureIgnoreCase))
                    return "ValueSet";
                if (sp.NameHumanCase.Equals("SalesByYear", StringComparison.InvariantCultureIgnoreCase))
                    return "SalesSet";

                return name;
            };*/
            Settings.StoredProcedureReturnModelRename = (name, sp) => name; // Do nothing by default

            // StoredProcedure return types *******************************************************************************************************
            // Override generation of return models for stored procedures that return entities.
            // If a stored procedure returns an entity, add it to the list below.
            // This will suppress the generation of the return model, and instead return the entity.
            // Example:                       Proc name      Return this entity type instead
            //StoredProcedureReturnTypes.Add("SalesByYear", "SummaryOfSalesByYear");


            // Callbacks **********************************************************************************************************************
            // This method will be called right before we write the POCO header.
            this.WritePocoClassAttributes = (o, t) =>
            {
                if (Settings.UseDataAnnotations)
                {
                    foreach (var dataAnnotation in t.DataAnnotations)
                    {
                        o?.WriteLine("    [" + dataAnnotation + "]");
                    }
                }

                // Example:
                // if(t.ClassName.StartsWith("Order"))
                //     WriteLine("    [SomeAttribute]");
            };

            // This method will be called right before we write the POCO header.
            this.WritePocoClassExtendedComments = (o, t) =>
            {
                if (Settings.IncludeExtendedPropertyComments != CommentsStyle.None && !string.IsNullOrEmpty(t.ExtendedProperty))
                {
                    var lines = t.ExtendedProperty
                        .Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                    o?.WriteLine("    ///<summary>");
                    foreach (var line in lines.Select(x => x.Replace("///", string.Empty).Trim()))
                    {
                        o?.WriteLine("    /// {0}", System.Security.SecurityElement.Escape(line));
                    }
                    o?.WriteLine("    ///</summary>");
                }
            };

            // Writes optional base classes
            this.WritePocoBaseClasses = (o, t) =>
            {
                //if (t.ClassName == "User")
                //    return ": IdentityUser<int, CustomUserLogin, CustomUserRole, CustomUserClaim>";

                // Or use the maker class to dynamically build more complex definitions
                /* Example:
                var r = new BaseClassMaker("POCO.Sample.Data.MetaModelObject");
                r.AddInterface("POCO.Sample.Data.IObjectWithTableName");
                r.AddInterface("POCO.Sample.Data.IObjectWithId",
                    t.Columns.Any(x => x.IsPrimaryKey && !x.IsNullable && x.NameHumanCase.Equals("Id", StringComparison.InvariantCultureIgnoreCase) && x.PropertyType == "long"));
                r.AddInterface("POCO.Sample.Data.IObjectWithUserId",
                    t.Columns.Any(x => !x.IsPrimaryKey && !x.IsNullable && x.NameHumanCase.Equals("UserId", StringComparison.InvariantCultureIgnoreCase) && x.PropertyType == "long"));
                return r.ToString();
                */
                o?.Write("");
            };

            // Writes any boilerplate stuff inside the POCO class
            this.WritePocoBaseClassBody = (o, t) =>
            {
                // Do nothing by default
                // Example:
                // WriteLine("        // " + t.ClassName);
            };

            this.WritePocoColumn = (o, c) =>
            {
                bool commentWritten = false;
                if ((Settings.IncludeExtendedPropertyComments == CommentsStyle.InSummaryBlock ||
                     Settings.IncludeComments == CommentsStyle.InSummaryBlock) &&
                    !string.IsNullOrEmpty(c.SummaryComments))
                {
                    o?.WriteLine(string.Empty);
                    o?.WriteLine("///<summary>");
                    o?.WriteLine("/// {0}", System.Security.SecurityElement.Escape(c.SummaryComments));
                    o?.WriteLine("///</summary>");
                    commentWritten = true;
                }
                if (Settings.UseDataAnnotations || Settings.UseDataAnnotationsWithFluent)
                {
                    if (c.Ordinal > 1 && !commentWritten)
                        o?.WriteLine(string.Empty);    // Leave a blank line before the next property

                    foreach (var dataAnnotation in c.DataAnnotations)
                    {
                        o?.WriteLine("        [" + dataAnnotation + "]");
                    }
                }

                // Example of adding a [Required] data annotation attribute to all non-null fields
                //if (!c.IsNullable)
                //    return "        [System.ComponentModel.DataAnnotations.Required] " + c.Entity;

                o?.WriteLine(c.Entity);
            };

            Settings.ForeignKeyFilter = (ForeignKey fk) =>
            {
                // Return null to exclude this foreign key, or set IncludeReverseNavigation = false
                // to include the foreign key but not generate reverse navigation properties.
                // Example, to exclude all foreign keys for the Categories table, use:
                // if (fk.PkTableName == "Categories")
                //    return null;

                // Example, to exclude reverse navigation properties for tables ending with Type, use:
                // if (fk.PkTableName.EndsWith("Type"))
                //    fk.IncludeReverseNavigation = false;

                // You can also change the access modifier of the foreign-key's navigation property:
                // if(fk.PkTableName == "Categories") fk.AccessModifier = "internal";

                return fk;
            };

            Settings.ForeignKeyProcessing = (foreignKeys, fkTable, pkTable, anyNullableColumnInForeignKey) =>
            {
                var foreignKey = foreignKeys.First();

                // If using data annotations and to include the [Required] attribute in the foreign key, enable the following
                //if (!anyNullableColumnInForeignKey)
                //   foreignKey.IncludeRequiredAttribute = true;

                return foreignKey;
            };

            Settings.ForeignKeyName = (tableName, foreignKey, foreignKeyName, relationship, attempt) =>
            {
                string fkName;

                // 5 Attempts to correctly name the foreign key
                switch (attempt)
                {
                    case 1:
                        // Try without appending foreign key name
                        fkName = tableName;
                        break;

                    case 2:
                        // Only called if foreign key name ends with "id"
                        // Use foreign key name without "id" at end of string
                        fkName = foreignKeyName.Remove(foreignKeyName.Length - 2, 2);
                        break;

                    case 3:
                        // Use foreign key name only
                        fkName = foreignKeyName;
                        break;

                    case 4:
                        // Use table name and foreign key name
                        fkName = tableName + "_" + foreignKeyName;
                        break;

                    case 5:
                        // Used in for loop 1 to 99 to append a number to the end
                        fkName = tableName;
                        break;

                    default:
                        // Give up
                        fkName = tableName;
                        break;
                }

                // Apply custom foreign key renaming rules. Can be useful in applying pluralization.
                // For example:
                /*if (tableName == "Employee" && foreignKey.FkColumn == "ReportsTo")
                    return "Manager";

                if (tableName == "Territories" && foreignKey.FkTableName == "EmployeeTerritories")
                    return "Locations";

                if (tableName == "Employee" && foreignKey.FkTableName == "Orders" && foreignKey.FkColumn == "EmployeeID")
                    return "ContactPerson";
                */

                // FK_TableName_FromThisToParentRelationshipName_FromParentToThisChildsRelationshipName
                // (e.g. FK_CustomerAddress_Customer_Addresses will extract navigation properties "address.Customer" and "customer.Addresses")
                // Feel free to use and change the following
                /*if (foreignKey.ConstraintName.StartsWith("FK_") && foreignKey.ConstraintName.Count(x => x == '_') == 3)
                {
                    var parts = foreignKey.ConstraintName.Split('_');
                    if (!string.IsNullOrWhiteSpace(parts[2]) && !string.IsNullOrWhiteSpace(parts[3]) && parts[1] == foreignKey.FkTableName)
                    {
                        if (relationship == Relationship.OneToMany)
                            fkName = parts[3];
                        else if (relationship == Relationship.ManyToOne)
                            fkName = parts[2];
                    }
                }*/

                return fkName;
            };

            Settings.ForeignKeyAnnotationsProcessing = (Table fkTable, Table pkTable, string propName, string fkPropName) =>
            {
                /* Example:
                // Each navigation property that is a reference to User are left intact
                if (pkTable.NameHumanCase.Equals("User") && propName.Equals("User"))
                    return null;

                // all the others are marked with this attribute
                return new[] { "System.Runtime.Serialization.IgnoreDataMember" };
                */

                // Example, to include Inverse Property when using Data Annotations, use:
                // if (Settings.UseDataAnnotations && fkPropName != string.Empty)
                //     return new[] { "InverseProperty(\"" + fkPropName + "\")" };

                return null;
            };

            // Return true to include this table in the db context
            Settings.ConfigurationFilter = (Table t) =>
            {
                return true;
            };

            // That's it, nothing else to configure ***********************************************************************************************

            #endregion
        }
        List<string> usingsContext;
        public void Generate()
        {
            #region All this code mostly came from Simon Hughes T4 templates (with minor adjustments) - Database.tt - see https://github.com/sjh37/EntityFramework-Reverse-POCO-Code-First-Generator
            // Read schema
            var factory = GetDbProviderFactory();
            Settings.Tables = LoadTables(factory);
            Settings.StoredProcs = LoadStoredProcs(factory);

            if (Settings.Tables.Count == 0 && Settings.StoredProcs.Count > 0)
                return;

            #region All this code mostly came from Simon Hughes T4 templates (with minor adjustments) - EF.Reverse.POCO.ttinclude - see https://github.com/sjh37/EntityFramework-Reverse-POCO-Code-First-Generator
            #region Namespace / whole file (Line 31 to 1226)
            //_output?.WriteLine($"namespace { Settings.Namespace }"); // Line 31 // this is in StartNewFile()
            //_output?.WriteLine("{"); // this is in StartNewFile()
            //using (_output?.WithIndent()) // this is in StartNewFile()
            {
                usingsContext = new List<string>();
                var usingsAll = new List<string>();
                usingsAll.AddRange(Settings.AdditionalNamespaces.Where(x => !string.IsNullOrEmpty(x)));
                if ((Settings.ElementsToGenerate.HasFlag(Elements.PocoConfiguration) ||
                     Settings.ElementsToGenerate.HasFlag(Elements.Context) ||
                     Settings.ElementsToGenerate.HasFlag(Elements.UnitOfWork)) &&
                    (!Settings.ElementsToGenerate.HasFlag(Elements.Poco) && !string.IsNullOrWhiteSpace(Settings.PocoNamespace)))
                    usingsAll.Add(Settings.PocoNamespace);

                if (Settings.ElementsToGenerate.HasFlag(Elements.PocoConfiguration) &&
                    (!Settings.ElementsToGenerate.HasFlag(Elements.Context) && !string.IsNullOrWhiteSpace(Settings.ContextNamespace)))
                    usingsAll.Add(Settings.ContextNamespace);

                if (Settings.ElementsToGenerate.HasFlag(Elements.Context) &&
                    (!Settings.ElementsToGenerate.HasFlag(Elements.UnitOfWork) && !string.IsNullOrWhiteSpace(Settings.UnitOfWorkNamespace)))
                    usingsAll.Add(Settings.UnitOfWorkNamespace);

                if (Settings.ElementsToGenerate.HasFlag(Elements.Context) &&
                    (!Settings.ElementsToGenerate.HasFlag(Elements.PocoConfiguration) && !string.IsNullOrWhiteSpace(Settings.PocoConfigurationNamespace)))
                    usingsAll.Add(Settings.PocoConfigurationNamespace);

                if (Settings.ElementsToGenerate.HasFlag(Elements.Context))
                {
                    if (Settings.AddUnitTestingDbContext || Settings.StoredProcs.Any())
                    {
                        usingsContext.Add("System.Linq");
                    }
                }
                if (!Settings.GenerateSeparateFiles)
                {
                    usingsAll.AddRange(usingsContext);
                }

                foreach (var usingStatement in usingsAll.Distinct().OrderBy(x => x))
                    _output?.WriteLine($"using { usingStatement };");
                Console.WriteLine("Unit of Work...");
                #region Unit of Work (Line 77 to 200)
                if (Settings.ElementsToGenerate.HasFlag(Elements.UnitOfWork) && !string.IsNullOrWhiteSpace(Settings.DbContextInterfaceName)) // Line 72
                {
                    if (Settings.GenerateSeparateFiles)
                        StartNewFile(Settings.DbContextInterfaceName + Settings.FileExtension);
                    else
                        StartNewFile(_context.Output.FilePath);
                    if (!Settings.GenerateSeparateFiles)
                        _output?.WriteLine("#region Unit of work"); // line 77

                    _output?.WriteLine($"{Settings.DbContextInterfaceModifiers ?? "public partial"} interface {Settings.DbContextInterfaceName} : {Settings.DbContextInterfaceBaseClasses}");
                    _output?.WriteLine("{");
                    using (_output?.WithIndent())
                    {
                        foreach (Table tbl in Settings.Tables.Where(t => !t.IsMapping && t.HasPrimaryKey).OrderBy(x => x.NameHumanCase))
                        {
                            _output?.Write($"System.Data.Entity.DbSet<{ tbl.NameHumanCaseWithSuffix() }> { Inflector.MakePlural(tbl.NameHumanCase) } {{ get; set; }}");
                            if (Settings.IncludeComments != CommentsStyle.None)
                                _output?.WriteLine($" // {tbl.Name}");
                            else
                                _output?.WriteLine($"");
                        }
                        _output?.WriteLine(); //TODO: Fix in Scripty - empty lines shouldn't render Indent padding.

                        foreach (string s in Settings.AdditionalContextInterfaceItems.Where(x => !string.IsNullOrEmpty(x)))
                            _output?.WriteLine(s);
                        if (!Settings.UseInheritedBaseInterfaceFunctions)
                        {
                            _output?.WriteLine("int SaveChanges();");
                            if (Settings.IsSupportedFrameworkVersion("4.5"))
                            {
                                _output?.WriteLine("System.Threading.Tasks.Task<int> SaveChangesAsync();");
                                _output?.WriteLine("System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken);");
                            }
                            WriteTextBlock(_output, $@"
                                System.Data.Entity.Infrastructure.DbChangeTracker ChangeTracker {{ get; }}
                                System.Data.Entity.Infrastructure.DbContextConfiguration Configuration {{ get; }}
                                System.Data.Entity.Database Database {{ get; }}
                                System.Data.Entity.Infrastructure.DbEntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
                                System.Data.Entity.Infrastructure.DbEntityEntry Entry(object entity);
                                System.Collections.Generic.IEnumerable<System.Data.Entity.Validation.DbEntityValidationResult> GetValidationErrors();
                                System.Data.Entity.DbSet Set(System.Type entityType);
                                System.Data.Entity.DbSet<TEntity> Set<TEntity>() where TEntity : class;
                                string ToString();
                                ");
                        }
                        if (Settings.StoredProcs.Any())
                        {
                            _output?.WriteLine($"");
                            _output?.WriteLine("// Stored Procedures"); // Line 117
                            foreach (StoredProcedure sp in Settings.StoredProcs.Where(s => !s.IsTVF).OrderBy(x => x.NameHumanCase))
                            {
                                int returnModelsCount = sp.ReturnModels.Count;
                                if (returnModelsCount == 1)
                                {
                                    _output?.WriteLine($"{WriteStoredProcReturnType(sp)} {WriteStoredProcFunctionName(sp)}({WriteStoredProcFunctionParams(sp, false)});");
                                    _output?.WriteLine($"{WriteStoredProcReturnType(sp)} {WriteStoredProcFunctionName(sp)}({WriteStoredProcFunctionParams(sp, true)});");
                                }
                                else
                                {
                                    _output?.WriteLine($"{WriteStoredProcReturnType(sp)} {WriteStoredProcFunctionName(sp)}({WriteStoredProcFunctionParams(sp, false)});");
                                }
                                if (Settings.IsSupportedFrameworkVersion("4.5"))
                                {
                                    if (StoredProcHasOutParams(sp) || sp.ReturnModels.Count == 0)
                                    {
                                        _output?.WriteLine($"// <#=WriteStoredProcFunctionName(sp)#>Async cannot be created due to having out parameters, or is relying on the procedure result (<#=WriteStoredProcReturnType(sp)#>)");
                                    }
                                    else
                                    {
                                        _output?.WriteLine($"System.Threading.Tasks.Task<{WriteStoredProcReturnType(sp)}> {WriteStoredProcFunctionName(sp)}Async({WriteStoredProcFunctionParams(sp, false)});");
                                    }
                                }
                                _output?.WriteLine();
                            }
                            if (Settings.IncludeTableValuedFunctions)
                            {
                                _output?.WriteLine("// Table Valued Functions");
                                foreach (StoredProcedure sp in Settings.StoredProcs.Where(s => s.IsTVF).OrderBy(x => x.NameHumanCase))
                                {
                                    string spExecName = WriteStoredProcFunctionName(sp);
                                    string spReturnClassName = WriteStoredProcReturnModelName(sp);
                                    _output?.WriteLine($"[System.Data.Entity.DbFunction(\" { Settings.DbContextName} \", \"{ sp.Name} \")]");
                                    _output?.Write($"[CodeFirstStoreFunctions.DbFunctionDetails(DatabaseSchema = \"{sp.Schema}\"");
                                    if (sp.ReturnModels.Count == 1 && sp.ReturnModels[0].Count == 1)
                                        _output?.Write($", ResultColumnName = \"<{sp.ReturnModels[0][0].ColumnName});(");
                                    _output?.WriteLine(")]");
                                    _output?.WriteLine($"System.Linq.IQueryable<{ spReturnClassName }> { spExecName }({WriteStoredProcFunctionParams(sp, false)});");
                                }
                            }

                        }
                    }
                    _output?.WriteLine("}");
                }

                Console.WriteLine("Db Migration Configuration...");
                #region Db Migration Configuration (Line 161 to 196)
                if (!string.IsNullOrWhiteSpace(Settings.MigrationConfigurationFileName))
                {
                    if (Settings.GenerateSeparateFiles)
                        StartNewFile(Settings.MigrationConfigurationFileName + Settings.FileExtension);
                    if (!Settings.GenerateSeparateFiles)
                    {
                        WriteTextBlock(_output, $@"
                            // ************************************************************************
                            // Db Migration Configuration
                        ");
                    }
                    if (Settings.IncludeCodeGeneratedAttribute)
                        _output?.WriteLine(CodeGeneratedAttribute);
                    _output?.WriteLine($"{Settings.MigrationClassModifiers} class {Settings.MigrationConfigurationFileName}: System.Data.Entity.Migrations.DbMigrationsConfiguration<{Settings.DbContextName }> ");
                    _output?.WriteLine("{");
                    using (_output?.WithIndent())
                    {
                        _output?.WriteLine($"public {Settings.MigrationConfigurationFileName}()");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            _output?.WriteLine($"AutomaticMigrationsEnabled = { Settings.AutomaticMigrationsEnabled.ToString() };");
                            _output?.WriteLine($"AutomaticMigrationDataLossAllowed = { Settings.AutomaticMigrationDataLossAllowed.ToString() };");
                            if (!string.IsNullOrEmpty(Settings.ContextKey))
                                _output?.WriteLine($@"ContextKey = ""{ Settings.ContextKey }"";");
                        }
                        _output?.WriteLine("}");
                        WriteTextBlock(_output, @"
                            //protected override void Seed(<#=Settings.DbContextName#> context)
                            //{

                                // This method will be called after migrating to the latest version.

                                // You can use the DbSet<T>.AddOrUpdate() helper extension method
                                // to avoid creating duplicate seed data. E.g.
                                //
                                //   context.People.AddOrUpdate(
                                //     p => p.FullName,
                                //     new Person { FullName = ""Andrew Peters"" },
                                //     new Person { FullName = ""Brice Lambson"" },
                                //     new Person { FullName = ""Rowan Miller"" }
                                //   );
                                //
                            //}
                            ");
                    }
                    _output?.WriteLine("}");
                }
                #endregion Db Migration Configuration (Line 161 to 196)

                if (Settings.ElementsToGenerate.HasFlag(Elements.UnitOfWork) && !string.IsNullOrWhiteSpace(Settings.DbContextInterfaceName) && !Settings.GenerateSeparateFiles)
                    _output?.WriteLine("#endregion\n"); // line 200
                #endregion Unit of Work (Line 77 to 200)


                Console.WriteLine("Database context...");
                #region Database context (Line 203 to 509)
                if (Settings.ElementsToGenerate.HasFlag(Elements.Context))
                {
                    if (Settings.GenerateSeparateFiles)
                        StartNewFile(Settings.DbContextName + Settings.FileExtension);
                    if (!Settings.GenerateSeparateFiles)
                        _output?.WriteLine("#region Database context\n"); // line 206
                    else foreach (var usingStatement in usingsContext.Distinct().OrderBy(x => x))
                            _output?.WriteLine($"using { usingStatement };");
                    if (Settings.IncludeCodeGeneratedAttribute)
                        _output?.WriteLine(CodeGeneratedAttribute);
                    _output?.WriteLine($"{ Settings.DbContextClassModifiers } class {Settings.DbContextName} : {Settings.DbContextBaseClass}{ (string.IsNullOrWhiteSpace(Settings.DbContextInterfaceName) ? "" : ", " + Settings.DbContextInterfaceName)}");
                    _output?.WriteLine("{");
                    using (_output?.WithIndent())
                    {
                        foreach (Table tbl in Settings.Tables.Where(t => !t.IsMapping && t.HasPrimaryKey).OrderBy(x => x.NameHumanCase))
                        {
                            // 220 to 
                            _output?.Write($"public System.Data.Entity.DbSet<{tbl.NameHumanCaseWithSuffix()}> {Inflector.MakePlural(tbl.NameHumanCase)} {{ get; set; }}");
                            if (Settings.IncludeComments != CommentsStyle.None)
                                _output?.WriteLine($" // {tbl.Name}");
                            else
                                _output?.WriteLine($"");
                        }

                        _output?.WriteLine($"");
                        _output?.WriteLine($"static {Settings.DbContextName}()");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            if (string.IsNullOrWhiteSpace(Settings.MigrationConfigurationFileName))
                                _output?.WriteLine($"System.Data.Entity.Database.SetInitializer<{Settings.DbContextName}>(null);");
                            else
                                _output?.WriteLine($"System.Data.Entity.Database.SetInitializer(new System.Data.Entity.{Settings.MigrationStrategy}<{Settings.DbContextName}{ (Settings.MigrationStrategy == "MigrateDatabaseToLatestVersion" ? ", " + Settings.MigrationConfigurationFileName : "") }>());");
                        }
                        _output?.WriteLine("}");
                        _output?.WriteLine("");
                        if (Settings.AddParameterlessConstructorToDbContext)
                        {
                            _output?.Write($"public {Settings.DbContextName}()");
                            if (Settings.DefaultConstructorArgument != null)
                                _output?.WriteLine($" : base({Settings.DefaultConstructorArgument})");
                            else
                                _output?.WriteLine();
                            _output?.WriteLine("{");
                            using (_output?.WithIndent())
                            {
                                if (Settings.DbContextClassIsPartial())
                                    _output?.WriteLine("InitializePartial();");
                            }
                            _output?.WriteLine("}");
                        }

                        _output?.WriteLine("");
                        _output?.WriteLine($"public {Settings.DbContextName}(string connectionString) : base(connectionString)");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            if (Settings.DbContextClassIsPartial())
                                _output?.WriteLine("InitializePartial();");
                        }
                        _output?.WriteLine("}");

                        _output?.WriteLine("");
                        _output?.WriteLine($"public {Settings.DbContextName}(string connectionString, System.Data.Entity.Infrastructure.DbCompiledModel model) : base(connectionString, model)");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            if (Settings.DbContextClassIsPartial())
                                _output?.WriteLine("InitializePartial();");
                        }
                        _output?.WriteLine("}");

                        _output?.WriteLine("");
                        _output?.WriteLine($"public {Settings.DbContextName}(System.Data.Common.DbConnection existingConnection, bool contextOwnsConnection) : base(existingConnection, contextOwnsConnection)");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            if (Settings.DbContextClassIsPartial())
                                _output?.WriteLine("InitializePartial();");
                        }
                        _output?.WriteLine("}");

                        _output?.WriteLine("");
                        _output?.WriteLine($"public {Settings.DbContextName}(System.Data.Common.DbConnection existingConnection, System.Data.Entity.Infrastructure.DbCompiledModel model, bool contextOwnsConnection) : base(existingConnection, model, contextOwnsConnection)");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            if (Settings.DbContextClassIsPartial())
                                _output?.WriteLine("InitializePartial();");
                        }
                        _output?.WriteLine("}");

                        if (!Settings.DbContextBaseClass.Contains("IdentityDbContext"))
                        {
                            _output?.WriteLine("");
                            _output?.WriteLine($"public {Settings.DbContextName}(System.Data.Entity.Core.Objects.ObjectContext objectContext, bool dbContextOwnsObjectContext) : base(objectContext, dbContextOwnsObjectContext)");
                            _output?.WriteLine("{");
                            using (_output?.WithIndent())
                            {
                                if (Settings.DbContextClassIsPartial())
                                    _output?.WriteLine("InitializePartial();");
                            }
                            _output?.WriteLine("}");
                        }

                        _output?.WriteLine("");
                        _output?.WriteLine($"protected override void Dispose(bool disposing)");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            if (Settings.DbContextClassIsPartial())
                                _output?.WriteLine("DisposePartial(disposing);");
                            _output?.WriteLine("base.Dispose(disposing);");
                        }
                        _output?.WriteLine("}");

                        if (!Settings.IsSqlCe)
                        {
                            WriteTextBlock(_output, $@"

                                    public bool IsSqlParameterNull(System.Data.SqlClient.SqlParameter param)
                                    {{
                                        var sqlValue = param.SqlValue;
                                        var nullableValue = sqlValue as System.Data.SqlTypes.INullable;
                                        if (nullableValue != null)
                                            return nullableValue.IsNull;
                                        return (sqlValue == null || sqlValue == System.DBNull.Value);
                                    }}
                                    ");
                        }

                        _output?.WriteLine();
                        _output?.WriteLine($"protected override void OnModelCreating(System.Data.Entity.DbModelBuilder modelBuilder)");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            _output?.WriteLine($"base.OnModelCreating(modelBuilder);");
                            if (Settings.StoredProcs.Any() && Settings.IncludeTableValuedFunctions)
                            {
                                _output?.WriteLine($"modelBuilder.Conventions.Add(new CodeFirstStoreFunctions.FunctionsConvention<{Settings.DbContextName}>(\"dbo\"));");
                                foreach (var sp in Settings.StoredProcs.Where(s => s.IsTVF && !Settings.StoredProcedureReturnTypes.ContainsKey(s.NameHumanCase) && !Settings.StoredProcedureReturnTypes.ContainsKey(s.Name)).OrderBy(x => x.NameHumanCase))
                                {
                                    _output?.WriteLine($"modelBuilder.ComplexType<{WriteStoredProcReturnModelName(sp)}>();");
                                }
                            }
                            foreach (var tbl in Settings.Tables.Where(t => !t.IsMapping && t.HasPrimaryKey).Where(Settings.ConfigurationFilter).OrderBy(x => x.NameHumanCase))
                            {
                                _output?.WriteLine($"modelBuilder.Configurations.Add(new {tbl.NameHumanCaseWithSuffix() + Settings.ConfigurationClassName}());");
                            }
                            if (Settings.DbContextClassIsPartial())
                                _output?.WriteLine("OnModelCreatingPartial(modelBuilder);");
                        }
                        _output?.WriteLine("}");

                        _output?.WriteLine("");
                        _output?.WriteLine("public static System.Data.Entity.DbModelBuilder CreateModel(System.Data.Entity.DbModelBuilder modelBuilder, string schema)");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            foreach (var tbl in Settings.Tables.Where(t => !t.IsMapping && t.HasPrimaryKey).Where(Settings.ConfigurationFilter).OrderBy(x => x.NameHumanCase))
                            {
                                _output?.WriteLine($"modelBuilder.Configurations.Add(new {tbl.NameHumanCaseWithSuffix() + Settings.ConfigurationClassName}(schema));");
                            }
                            if (Settings.DbContextClassIsPartial())
                                _output?.WriteLine("OnCreateModelPartial(modelBuilder, schema);");
                            _output?.WriteLine("return modelBuilder;");
                        }
                        _output?.WriteLine("}");

                        if (Settings.DbContextClassIsPartial()) // Line 337
                        {
                            WriteTextBlock(_output, $@"
                                partial void InitializePartial();
                                partial void DisposePartial(bool disposing);
                                partial void OnModelCreatingPartial(System.Data.Entity.DbModelBuilder modelBuilder);
		                        static partial void OnCreateModelPartial(System.Data.Entity.DbModelBuilder modelBuilder, string schema);
                                ");

                        }

                        #region Stored Procedures (Line 344 to 487)
                        if (Settings.StoredProcs.Any()) // Line 344
                        {
                            _output?.WriteLine();
                            _output?.WriteLine("// Stored Procedures");
                            foreach (var sp in Settings.StoredProcs.Where(s => !s.IsTVF).OrderBy(x => x.NameHumanCase)) // Line 349
                            {
                                string spReturnClassName = WriteStoredProcReturnModelName(sp);
                                string spExecName = WriteStoredProcFunctionName(sp);
                                int returnModelsCount = sp.ReturnModels.Count;
                                #region 354 to 486

                                if (returnModelsCount > 0) // Line 354?
                                {
                                    if (returnModelsCount == 1) // Line 356
                                    {
                                        // Line 358 to 362
                                        WriteTextBlock(_output, $@"
                                            public {WriteStoredProcReturnType(sp) } {WriteStoredProcFunctionName(sp) }({WriteStoredProcFunctionParams(sp, false) })
                                            {{
                                                int procResult;
                                                return { spExecName }({WriteStoredProcFunctionOverloadCall(sp) });
                                            }}

                                        ");
                                    }
                                    if (returnModelsCount == 1)
                                        _output?.WriteLine($@"public {WriteStoredProcReturnType(sp) } {WriteStoredProcFunctionName(sp) }({WriteStoredProcFunctionParams(sp, true) })");
                                    else
                                        _output?.WriteLine($@"public {WriteStoredProcReturnType(sp) } {WriteStoredProcFunctionName(sp) }({WriteStoredProcFunctionParams(sp, false) })");
                                    _output?.WriteLine("{");
                                    using (_output?.WithIndent())
                                    {
                                        WriteStoredProcFunctionDeclareSqlParameter(_output, sp, true);
                                        if (returnModelsCount == 1)
                                        {
                                            var exec = string.Format("EXEC @procResult = [{0}].[{1}] {2}", sp.Schema, sp.Name, WriteStoredProcFunctionSqlAtParams(sp));
                                            _output?.WriteLine($"var procResultData = Database.SqlQuery<{ spReturnClassName }>(\"{ exec }\", { WriteStoredProcFunctionSqlParameterAnonymousArray(sp, true) }).ToList();");
                                            WriteStoredProcFunctionSetSqlParameters(_output, sp, false);
                                            _output?.WriteLine("procResult = (int) procResultParam.Value;");
                                        }
                                        else
                                        {
                                            var exec = string.Format("[{0}].[{1}]", sp.Schema, sp.Name);
                                            WriteStoredProcFunctionSetSqlParameters(_output, sp, false);
                                            WriteTextBlock(_output, $@"
                                            var procResultData = new { spReturnClassName }();
                                            var cmd = Database.Connection.CreateCommand();
                                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                                            cmd.CommandText = ""{ exec }"";");
                                            foreach (var p in sp.Parameters.OrderBy(x => x.Ordinal))
                                                _output?.WriteLine($@"cmd.Parameters.Add({ WriteStoredProcSqlParameterName(p) });");
                                            _output?.WriteLine("try");
                                            _output?.WriteLine("{");
                                            using (_output?.WithIndent())
                                            {
                                                _output?.WriteLine($@"System.Data.Entity.Infrastructure.Interception.DbInterception.Dispatch.Connection.Open(Database.Connection, new System.Data.Entity.Infrastructure.Interception.DbInterceptionContext());");
                                                _output?.WriteLine($@"var reader = cmd.ExecuteReader();");
                                                _output?.WriteLine($@"var objectContext = ((System.Data.Entity.Infrastructure.IObjectContextAdapter) this).ObjectContext;");
                                                _output?.WriteLine($@"");
                                                int n = 0;
                                                var returnModelCount = sp.ReturnModels.Count;
                                                foreach (var returnModel in sp.ReturnModels)
                                                {
                                                    n++;
                                                    _output?.WriteLine($@"procResultData.ResultSet{ n } = objectContext.Translate<{ spReturnClassName }.ResultSetModel{ n }>(reader).ToList();");
                                                    if (n < returnModelCount)
                                                        _output?.WriteLine($@"reader.NextResult();");
                                                }
                                                _output?.WriteLine($@"reader.Close();");
                                                WriteStoredProcFunctionSetSqlParameters(_output, sp, false);
                                            }
                                            _output?.WriteLine("}");
                                            _output?.WriteLine("finally");
                                            _output?.WriteLine("{");
                                            using (_output?.WithIndent())
                                            {
                                                _output?.WriteLine("System.Data.Entity.Infrastructure.Interception.DbInterception.Dispatch.Connection.Close(Database.Connection, new System.Data.Entity.Infrastructure.Interception.DbInterceptionContext());");
                                            }
                                            _output?.WriteLine("}");
                                        }
                                        _output?.WriteLine("return procResultData;");
                                    }
                                    _output?.WriteLine("}"); // Line 417
                                    _output?.WriteLine();
                                } // Line 419?
                                else
                                {
                                    _output?.WriteLine($@"public int { spExecName }({WriteStoredProcFunctionParams(sp, true) })");
                                    _output?.WriteLine("{");
                                    WriteStoredProcFunctionDeclareSqlParameter(_output, sp, true);
                                    _output?.WriteLine($@"Database.ExecuteSqlCommand(System.Data.Entity.TransactionalBehavior.DoNotEnsureTransaction, ""EXEC @procResult = [{sp.Schema }].[{ sp.Name } { WriteStoredProcFunctionSqlAtParams(sp) }"", { WriteStoredProcFunctionSqlParameterAnonymousArray(sp, true) });");
                                    WriteStoredProcFunctionSetSqlParameters(_output, sp, false);
                                    _output?.WriteLine("return (int) procResultParam.Value;");
                                    _output?.WriteLine("}");
                                } // Line 430?
                                // Async
                                if (Settings.IsSupportedFrameworkVersion("4.5") && !StoredProcHasOutParams(sp) && returnModelsCount > 0) // Line 432
                                {
                                    _output?.WriteLine($@"public async System.Threading.Tasks.Task<{WriteStoredProcReturnType(sp) }> {WriteStoredProcFunctionName(sp) }Async({WriteStoredProcFunctionParams(sp, false) })");
                                    _output?.WriteLine("{");
                                    using (_output?.WithIndent())
                                    {
                                        WriteStoredProcFunctionDeclareSqlParameter(_output, sp, false);
                                        if (returnModelsCount == 1)
                                        {
                                            var parameters = WriteStoredProcFunctionSqlParameterAnonymousArray(sp, false);
                                            if (!string.IsNullOrWhiteSpace(parameters))
                                                parameters = ", " + parameters;
                                            var exec = string.Format("EXEC [{0}].[{1}] {2}", sp.Schema, sp.Name, WriteStoredProcFunctionSqlAtParams(sp));
                                            _output?.WriteLine($@"var procResultData = await Database.SqlQuery<{ spReturnClassName }>(""{ exec }""{ parameters }).ToListAsync();");
                                            WriteStoredProcFunctionSetSqlParameters(_output, sp, false);
                                        }
                                        else
                                        {
                                            var exec = string.Format("[{0}].[{1}]", sp.Schema, sp.Name);
                                            WriteStoredProcFunctionSetSqlParameters(_output, sp, false);
                                            _output?.WriteLine($@"var procResultData = new { spReturnClassName }();");
                                            _output?.WriteLine("var cmd = Database.Connection.CreateCommand();");
                                            _output?.WriteLine("cmd.CommandType = System.Data.CommandType.StoredProcedure;");
                                            _output?.WriteLine($@"cmd.CommandText = ""{ exec }"";");
                                            foreach (var p in sp.Parameters.OrderBy(x => x.Ordinal))
                                                _output?.WriteLine($@"cmd.Parameters.Add({ WriteStoredProcSqlParameterName(p) });");
                                            _output?.WriteLine($@"try");
                                            _output?.WriteLine("{");
                                            using (_output?.WithIndent())
                                            {
                                                _output?.WriteLine($@"await System.Data.Entity.Infrastructure.Interception.DbInterception.Dispatch.Connection.OpenAsync(Database.Connection, new System.Data.Entity.Infrastructure.Interception.DbInterceptionContext(), new System.Threading.CancellationToken()).ConfigureAwait(false);");
                                                _output?.WriteLine($@"var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);");
                                                _output?.WriteLine($@"var objectContext = ((System.Data.Entity.Infrastructure.IObjectContextAdapter) this).ObjectContext;");
                                                int n = 0;
                                                var returnModelCount = sp.ReturnModels.Count;
                                                foreach (var returnModel in sp.ReturnModels)
                                                {
                                                    n++;
                                                    _output?.WriteLine($@"procResultData.ResultSet{ n } = objectContext.Translate<{ spReturnClassName }.ResultSetModel{ n }>(reader).ToList();");
                                                    if (n < returnModelCount)
                                                        _output?.WriteLine($@"await reader.NextResultAsync().ConfigureAwait(false);");
                                                }
                                            }
                                            _output?.WriteLine("}");
                                            _output?.WriteLine("finally");
                                            _output?.WriteLine("{");
                                            _output?.WriteLine("    System.Data.Entity.Infrastructure.Interception.DbInterception.Dispatch.Connection.Close(Database.Connection, new System.Data.Entity.Infrastructure.Interception.DbInterceptionContext());");
                                            _output?.WriteLine("}");
                                        }
                                        _output?.WriteLine("return procResultData;");
                                    }
                                    _output?.WriteLine("}");
                                } // 486?
                                #endregion
                            } // Line 486?
                        } // Line 487
                        #endregion Stored Procedures (Line 344 to 487)

                        Console.WriteLine("IncludeTableValuedFunctions...");
                        #region IncludeTableValuedFunctions (488 to 509)
                        if (Settings.IncludeTableValuedFunctions)
                        {
                            _output?.WriteLine("// Table Valued Functions");
                            foreach (var sp in Settings.StoredProcs.Where(s => s.IsTVF).OrderBy(x => x.NameHumanCase))
                            {
                                string spExecName = WriteStoredProcFunctionName(sp);
                                string spReturnClassName = WriteStoredProcReturnModelName(sp);
                                _output?.WriteLine($@"[System.Data.Entity.DbFunction(""{Settings.DbContextName}"", ""{sp.Name}"")]");
                                _output?.WriteLine($@"[CodeFirstStoreFunctions.DbFunctionDetails(DatabaseSchema = ""{sp.Schema}""");
                                if (sp.ReturnModels.Count == 1 && sp.ReturnModels[0].Count == 1)
                                    _output?.Write($", ResultColumnName = \"<{sp.ReturnModels[0][0].ColumnName});(");
                                _output?.WriteLine(")]");
                                _output?.WriteLine($"public IQueryable<{ spReturnClassName }> { spExecName }({WriteStoredProcFunctionParams(sp, false)});");
                                _output?.WriteLine("{");
                                using (_output?.WithIndent())
                                {
                                    string procParameters = WriteTableValuedFunctionDeclareSqlParameter(sp);
                                    if (!string.IsNullOrEmpty(procParameters))
                                        _output?.WriteLine(procParameters);
                                    _output?.WriteLine($@"return ((System.Data.Entity.Infrastructure.IObjectContextAdapter)this).ObjectContext.CreateQuery<{spReturnClassName}>(""[{ Settings.DbContextName}].[{sp.Name}]({ WriteStoredProcFunctionSqlAtParams(sp) })"", { WriteTableValuedFunctionSqlParameterAnonymousArray(sp) });");
                                }
                                _output?.WriteLine("}");
                            }
                        }
                        #endregion



                    }
                    _output?.WriteLine("}");
                    if (Settings.ElementsToGenerate.HasFlag(Elements.Context) && !Settings.GenerateSeparateFiles)
                        _output?.WriteLine("#endregion\n"); // line 509
                }
                #endregion Database context (Line 203 to 509)

                Console.WriteLine("Database context factory...");
                #region Database context factory (Line 511 to 532)
                if (Settings.ElementsToGenerate.HasFlag(Elements.Context) && Settings.AddIDbContextFactory)
                {
                    if (Settings.GenerateSeparateFiles)
                        StartNewFile(Settings.DbContextName + "Factory" + Settings.FileExtension);
                    if (!Settings.GenerateSeparateFiles)
                        _output?.WriteLine("#region Database context factory\n"); // line 517
                    WriteTextBlock(_output, $@"
                        { Settings.DbContextClassModifiers } class { Settings.DbContextName + "Factory" } : System.Data.Entity.Infrastructure.IDbContextFactory<{ Settings.DbContextName }>
                        {{
                            public { Settings.DbContextName } Create()
                            {{
                                return new { Settings.DbContextName }();
                            }}
                        }}
                        ");
                    if (!Settings.GenerateSeparateFiles)
                    {
                        _output?.WriteLine("\n");
                        _output?.WriteLine("#endregion\n"); // line 529

                    }

                }
                #endregion Database context factory (Line 511 to 532)


                Console.WriteLine("Fake Database context...");
                #region Fake Database context (Line 533 to 1002)
                if (Settings.ElementsToGenerate.HasFlag(Elements.Context) && Settings.AddUnitTestingDbContext)
                {
                    if (Settings.GenerateSeparateFiles)
                        StartNewFile("Fake" + Settings.DbContextName + Settings.FileExtension);
                    if (!Settings.GenerateSeparateFiles)
                        _output?.WriteLine("#region Fake Database context"); // line 538
                    else foreach (var usingStatement in usingsContext.Distinct().OrderBy(x => x))
                            _output?.WriteLine($"using { usingStatement };");
                    if (Settings.IncludeCodeGeneratedAttribute)
                        _output?.WriteLine(CodeGeneratedAttribute);

                    Console.WriteLine("548...");
                    #region 548 to 1000
                    _output?.WriteLine($"{ Settings.DbContextClassModifiers } class Fake{Settings.DbContextName}{ (string.IsNullOrWhiteSpace(Settings.DbContextInterfaceName) ? "" : " : " + Settings.DbContextInterfaceName)}");
                    _output?.WriteLine("{");
                    using (_output?.WithIndent())
                    {
                        foreach (Table tbl in Settings.Tables.Where(t => !t.IsMapping && t.HasPrimaryKey).OrderBy(x => x.NameHumanCase)) // Line 551
                        {
                            _output?.WriteLine($"public System.Data.Entity.DbSet<{tbl.NameHumanCaseWithSuffix()}> {Inflector.MakePlural(tbl.NameHumanCase)} {{ get; set; }}");
                        }

                        _output?.WriteLine($"");
                        _output?.WriteLine($"public Fake{Settings.DbContextName}()");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            _output?.WriteLine("_changeTracker = null;");
                            _output?.WriteLine("_configuration = null;");
                            _output?.WriteLine("_database = null;");

                            foreach (Table tbl in Settings.Tables.Where(t => !t.IsMapping && t.HasPrimaryKey).OrderBy(x => x.NameHumanCase)) // Line 564
                                _output?.WriteLine($@"{Inflector.MakePlural(tbl.NameHumanCase) } = new FakeDbSet<{tbl.NameHumanCaseWithSuffix() }>({ string.Join(", ", tbl.PrimaryKeys.Select(x => "\"" + x.NameHumanCase + "\"")) });");
                            if (Settings.DbContextClassIsPartial())
                                _output?.WriteLine("InitializePartial();");
                        }
                        _output?.WriteLine("}");
                        _output?.WriteLine("");

                        WriteTextBlock(_output, $@"
                            public int SaveChangesCount {{ get; private set; }}
                            public int SaveChanges()
                            {{
                                ++SaveChangesCount;
                                return 1;
                            }}
                        ");

                        if (Settings.IsSupportedFrameworkVersion("4.5"))
                        {

                            WriteTextBlock(_output, $@"
                                public System.Threading.Tasks.Task<int> SaveChangesAsync()
                                {{
                                    ++SaveChangesCount;
                                    return System.Threading.Tasks.Task<int>.Factory.StartNew(() => 1);
                                }}

                                public System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken)
                                {{
                                    ++SaveChangesCount;
                                    return System.Threading.Tasks.Task<int>.Factory.StartNew(() => 1, cancellationToken);
                                }}
                        ");
                        }

                        if (Settings.DbContextClassIsPartial())
                            _output?.WriteLine("partial void InitializePartial();");

                        WriteTextBlock(_output, @"
                            protected virtual void Dispose(bool disposing)
                            {
                            }

                            public void Dispose()
                            {
                                Dispose(true);
                            }

                            private System.Data.Entity.Infrastructure.DbChangeTracker _changeTracker;
                            public System.Data.Entity.Infrastructure.DbChangeTracker ChangeTracker { get { return _changeTracker; } }
                            private System.Data.Entity.Infrastructure.DbContextConfiguration _configuration;
                            public System.Data.Entity.Infrastructure.DbContextConfiguration Configuration { get { return _configuration; } }
                            private System.Data.Entity.Database _database;
                            public System.Data.Entity.Database Database { get { return _database; } }
                            public System.Data.Entity.Infrastructure.DbEntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class
                            {
                                throw new System.NotImplementedException();
                            }
                            public System.Data.Entity.Infrastructure.DbEntityEntry Entry(object entity)
                            {
                                throw new System.NotImplementedException();
                            }
                            public System.Collections.Generic.IEnumerable<System.Data.Entity.Validation.DbEntityValidationResult> GetValidationErrors()
                            {
                                throw new System.NotImplementedException();
                            }
                            public System.Data.Entity.DbSet Set(System.Type entityType)
                            {
                                throw new System.NotImplementedException();
                            }
                            public System.Data.Entity.DbSet<TEntity> Set<TEntity>() where TEntity : class
                            {
                                throw new System.NotImplementedException();
                            }
                            public override string ToString()
                            {
                                throw new System.NotImplementedException();
                            }
                            ");


                        if (Settings.StoredProcs.Any()) // Line 639
                        {
                            _output?.WriteLine("// Stored Procedures");
                            foreach (StoredProcedure sp in Settings.StoredProcs.Where(s => !s.IsTVF).OrderBy(x => x.NameHumanCase)) // Line 644
                            {
                                string spReturnClassName = WriteStoredProcReturnModelName(sp);
                                string spExecName = WriteStoredProcFunctionName(sp);
                                int returnModelsCount = sp.ReturnModels.Count;
                                #region Lines 649 to 687
                                if (returnModelsCount > 0)
                                {
                                    _output?.WriteLine($@"public {WriteStoredProcReturnType(sp) } {WriteStoredProcFunctionName(sp) }({WriteStoredProcFunctionParams(sp, false) })");
                                    _output?.WriteLine("{");
                                    using (_output?.WithIndent())
                                    {
                                        _output?.WriteLine($@"int procResult;");
                                        _output?.WriteLine($@"return {spExecName }({WriteStoredProcFunctionOverloadCall(sp) });");
                                    }
                                    _output?.WriteLine("}");

                                    _output?.WriteLine("");

                                    _output?.WriteLine($@"public {WriteStoredProcReturnType(sp) } {WriteStoredProcFunctionName(sp) }({WriteStoredProcFunctionParams(sp, true) })");
                                    _output?.WriteLine("{");
                                    using (_output?.WithIndent())
                                    {
                                        WriteStoredProcFunctionSetSqlParameters(_output, sp, true);
                                        _output?.WriteLine($@"procResult = 0;");
                                        _output?.WriteLine($@"return new {WriteStoredProcReturnType(sp) }();");
                                    }
                                    _output?.WriteLine("}");

                                    if (Settings.IsSupportedFrameworkVersion("4.5") && !StoredProcHasOutParams(sp) && returnModelsCount > 0)
                                    {
                                        WriteTextBlock(_output, $@"
                                        public System.Threading.Tasks.Task<{WriteStoredProcReturnType(sp)}> {WriteStoredProcFunctionName(sp) }Async({WriteStoredProcFunctionParams(sp, false) })
                                        {{
                                            int procResult;
                                            return System.Threading.Tasks.Task.FromResult({ spExecName }({WriteStoredProcFunctionOverloadCall(sp) }));
                                        }}
                                        ");
                                    }
                                }
                                else
                                {
                                    _output?.WriteLine($@"public int { spExecName }(<#=WriteStoredProcFunctionParams(sp, true) #>)");
                                    _output?.WriteLine("{");
                                    using (_output?.WithIndent())
                                    {
                                        WriteStoredProcFunctionSetSqlParameters(_output, sp, true);
                                        _output?.WriteLine($@"return 0;");
                                    }
                                    _output?.WriteLine("}");


                                    if (Settings.IsSupportedFrameworkVersion("4.5") && !StoredProcHasOutParams(sp) && returnModelsCount > 0)
                                    {
                                        _output?.WriteLine($@"public System.Threading.Tasks.Task<int> { spExecName }Async({WriteStoredProcFunctionParams(sp, false) })");
                                        _output?.WriteLine("{");
                                        using (_output?.WithIndent())
                                        {
                                            WriteStoredProcFunctionSetSqlParameters(_output, sp, true);
                                            _output?.WriteLine($@"return System.Threading.Tasks.Task.FromResult(0);");
                                        }
                                        _output?.WriteLine("}");
                                    }
                                }
                                #endregion Lines 649 to 687
                            }
                        }
                        #region IncludeTableValuedFunctions (Lines 688 to 705)
                        if (Settings.IncludeTableValuedFunctions)
                        {
                            _output?.WriteLine("// Table Valued Functions");
                            foreach (StoredProcedure spTvf in Settings.StoredProcs.Where(s => s.IsTVF).OrderBy(x => x.NameHumanCase))
                            {
                                string spExecNamespTvf = WriteStoredProcFunctionName(spTvf);
                                string spReturnClassName = WriteStoredProcReturnModelName(spTvf);
                                WriteTextBlock(_output, $@"
                                    [System.Data.Entity.DbFunction(""{ Settings.DbContextName}"", ""{ spTvf.Name}"")]
                                    public IQueryable<{ spReturnClassName }> { spExecNamespTvf } ({WriteStoredProcFunctionParams(spTvf, false)})
                                    {{
                                        return new System.Collections.Generic.List<{ spReturnClassName }>().AsQueryable();
                                    }}
                                    ");
                            }
                        }
                        #endregion IncludeTableValuedFunctions (Lines 688 to 705)


                    }
                    _output?.WriteLine("}"); // end of DbContextName


                    if (Settings.GenerateSeparateFiles)
                        StartNewFile("FakeDbSet" + Settings.FileExtension);
                    if (Settings.GenerateSeparateFiles)
                        _output?.WriteLine("using System.Linq;");
                    WriteTextBlock(_output, @"
                        // ************************************************************************
                        // Fake DbSet
                        // Implementing Find:
                        //      The Find method is difficult to implement in a generic fashion. If
                        //      you need to test code that makes use of the Find method it is
                        //      easiest to create a test DbSet for each of the entity types that
                        //      need to support find. You can then write logic to find that
                        //      particular type of entity, as shown below:
                        //      public class FakeBlogDbSet : FakeDbSet<Blog>
                        //      {
                        //          public override Blog Find(params object[] keyValues)
                        //          {
                        //              var id = (int) keyValues.Single();
                        //              return this.SingleOrDefault(b => b.BlogId == id);
                        //          }
                        //      }
                        //      Read more about it here: https://msdn.microsoft.com/en-us/data/dn314431.aspx
                        ");
                    if (Settings.IncludeCodeGeneratedAttribute)
                        _output?.WriteLine(CodeGeneratedAttribute);
                    _output?.WriteLine($@"{ Settings.DbContextClassModifiers } class FakeDbSet<TEntity> : System.Data.Entity.DbSet<TEntity>, IQueryable, System.Collections.Generic.IEnumerable<TEntity>{ (Settings.IsSupportedFrameworkVersion("4.5") ? ", System.Data.Entity.Infrastructure.IDbAsyncEnumerable<TEntity>":"") } where TEntity : class");
                    _output?.WriteLine("{");
                    using (_output?.WithIndent())
                    {
                        _output?.WriteLine("private readonly System.Reflection.PropertyInfo[] _primaryKeys;");
                        _output?.WriteLine("private readonly System.Collections.ObjectModel.ObservableCollection<TEntity> _data;");
                        _output?.WriteLine("private readonly IQueryable _query;");

                        _output?.WriteLine("");
                        _output?.WriteLine("public FakeDbSet()");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            _output?.WriteLine("_data = new System.Collections.ObjectModel.ObservableCollection<TEntity>();");
                            _output?.WriteLine("_query = _data.AsQueryable();");
                            if (Settings.DbContextClassIsPartial())
                                _output?.WriteLine("InitializePartial();");
                        }
                        _output?.WriteLine("}");

                        _output?.WriteLine("");
                        _output?.WriteLine("public FakeDbSet(params string[] primaryKeys)");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            _output?.WriteLine("_primaryKeys = typeof(TEntity).GetProperties().Where(x => primaryKeys.Contains(x.Name)).ToArray();");
                            _output?.WriteLine("_data = new System.Collections.ObjectModel.ObservableCollection<TEntity>();");
                            _output?.WriteLine("_query = _data.AsQueryable();");
                            if (Settings.DbContextClassIsPartial())
                                _output?.WriteLine("InitializePartial();");
                        }
                        _output?.WriteLine("}");


                        WriteTextBlock(_output, $@"
                            public override TEntity Find(params object[] keyValues)
                            {{
                                if (_primaryKeys == null)
                                    throw new System.ArgumentException(""No primary keys defined"");
                                if (keyValues.Length != _primaryKeys.Length)
                                    throw new System.ArgumentException(""Incorrect number of keys passed to Find method"");

                                var keyQuery = this.AsQueryable();
                                keyQuery = keyValues
                                    .Select((t, i) => i)
                                    .Aggregate(keyQuery,
                                        (current, x) =>
                                            current.Where(entity => _primaryKeys[x].GetValue(entity, null).Equals(keyValues[x])));

                                return keyQuery.SingleOrDefault();
                            }}

                            ");

                        if (Settings.IsSupportedFrameworkVersion("4.5"))
                        {
                            WriteTextBlock(_output, $@"
                            public override System.Threading.Tasks.Task<TEntity> FindAsync(System.Threading.CancellationToken cancellationToken, params object[] keyValues)
                            {{
                                return System.Threading.Tasks.Task<TEntity>.Factory.StartNew(() => Find(keyValues), cancellationToken);
                            }}

                            public override System.Threading.Tasks.Task<TEntity> FindAsync(params object[] keyValues)
                            {{
                                return System.Threading.Tasks.Task<TEntity>.Factory.StartNew(() => Find(keyValues));
                            }}
");

                        }

                        WriteTextBlock(_output, $@"
                            public override System.Collections.Generic.IEnumerable<TEntity> AddRange(System.Collections.Generic.IEnumerable<TEntity> entities)
                            {{
                                if (entities == null) throw new System.ArgumentNullException(""entities"");
                                var items = entities.ToList();
                                foreach (var entity in items)
                                {{
                                    _data.Add(entity);
                                }}
                                return items;
                            }}

                            public override TEntity Add(TEntity item)
                            {{
                                if (item == null) throw new System.ArgumentNullException(""item"");
                                _data.Add(item);
                                return item;
                            }}

                            public override System.Collections.Generic.IEnumerable<TEntity> RemoveRange(System.Collections.Generic.IEnumerable<TEntity> entities)
                            {{
                                if (entities == null) throw new System.ArgumentNullException(""entities"");
                                var items = entities.ToList();
                                foreach (var entity in items)
                                {{
                                    _data.Remove(entity);
                                }}
                                return items;
                            }}

                            public override TEntity Remove(TEntity item)
                            {{
                                if (item == null) throw new System.ArgumentNullException(""item"");
                                _data.Remove(item);
                                return item;
                            }}

                            public override TEntity Attach(TEntity item)
                            {{
                                if (item == null) throw new System.ArgumentNullException(""item"");
                                _data.Add(item);
                                return item;
                            }}

                            public override TEntity Create()
                            {{
                                return System.Activator.CreateInstance<TEntity>();
                            }}

                            public override TDerivedEntity Create<TDerivedEntity>()
                            {{
                                return System.Activator.CreateInstance<TDerivedEntity>();
                            }}

                            public override System.Collections.ObjectModel.ObservableCollection<TEntity> Local
                            {{
                                get {{ return _data; }}
                            }}

                            System.Type IQueryable.ElementType
                            {{
                                get {{ return _query.ElementType; }}
                            }}

                            System.Linq.Expressions.Expression IQueryable.Expression
                            {{
                                get {{ return _query.Expression; }}
                            }}

                            IQueryProvider IQueryable.Provider
                            {{
                                get {{ {(Settings.IsSupportedFrameworkVersion("4.5") ? "return new FakeDbAsyncQueryProvider<TEntity>(_query.Provider);" : "_query.Provider;")} }}
                            }}     

                            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                            {{
                                return _data.GetEnumerator();
                            }}

                            System.Collections.Generic.IEnumerator<TEntity> System.Collections.Generic.IEnumerable<TEntity>.GetEnumerator()
                            {{
                                return _data.GetEnumerator();
                            }}
                            { (Settings.IsSupportedFrameworkVersion("4.5") ?
                                $@"
                            System.Data.Entity.Infrastructure.IDbAsyncEnumerator<TEntity> System.Data.Entity.Infrastructure.IDbAsyncEnumerable<TEntity>.GetAsyncEnumerator()
                            {{
                                return new FakeDbAsyncEnumerator<TEntity>(_data.GetEnumerator());
                            }}" : "")
                            }

                            { (Settings.DbContextClassIsPartial() ? "partial void InitializePartial();":"") }
                            ");
                    }
                    _output?.WriteLine("}"); // Line 882
                    _output?.WriteLine("");

                    if (Settings.IncludeCodeGeneratedAttribute)
                        _output?.WriteLine(CodeGeneratedAttribute);
                    _output?.WriteLine($@"{ Settings.DbContextClassModifiers } class FakeDbAsyncQueryProvider<TEntity> : System.Data.Entity.Infrastructure.IDbAsyncQueryProvider");
                    _output?.WriteLine("{");
                    using (_output?.WithIndent())
                    {
                        WriteTextBlock(_output, $@"
                            private readonly IQueryProvider _inner;

                            public FakeDbAsyncQueryProvider(IQueryProvider inner)
                            {{
                                _inner = inner;
                            }}

                            public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
                            {{
                                var m = expression as System.Linq.Expressions.MethodCallExpression;
                                if (m != null)
                                {{
                                    var resultType = m.Method.ReturnType; // it shoud be IQueryable<T>
                                    var tElement = resultType.GetGenericArguments()[0];
                                    var queryType = typeof(FakeDbAsyncEnumerable<>).MakeGenericType(tElement);
                                    return (IQueryable) System.Activator.CreateInstance(queryType, expression);
                                }}
                                return new FakeDbAsyncEnumerable<TEntity>(expression);
                            }}

                            public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
                            {{
                                var queryType = typeof(FakeDbAsyncEnumerable<>).MakeGenericType(typeof(TElement));
                                return (IQueryable<TElement>)System.Activator.CreateInstance(queryType, expression);
                            }}

                            public object Execute(System.Linq.Expressions.Expression expression)
                            {{
                                return _inner.Execute(expression);
                            }}

                            public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
                            {{
                                return _inner.Execute<TResult>(expression);
                            }}

                            public System.Threading.Tasks.Task<object> ExecuteAsync(System.Linq.Expressions.Expression expression, System.Threading.CancellationToken cancellationToken)
                            {{
                                return System.Threading.Tasks.Task.FromResult(Execute(expression));
                            }}

                            public System.Threading.Tasks.Task<TResult> ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, System.Threading.CancellationToken cancellationToken)
                            {{
                                return System.Threading.Tasks.Task.FromResult(Execute<TResult>(expression));
                            }}
                            ");
                    }
                    _output?.WriteLine("}");



                    if (Settings.IncludeCodeGeneratedAttribute) // 937
                        _output?.WriteLine(CodeGeneratedAttribute);
                    _output?.WriteLine($@"{ Settings.DbContextClassModifiers } class FakeDbAsyncEnumerable<T> : EnumerableQuery<T>, System.Data.Entity.Infrastructure.IDbAsyncEnumerable<T>, IQueryable<T>");
                    _output?.WriteLine("{");
                    using (_output?.WithIndent())
                    {
                        WriteTextBlock(_output, $@"
                            public FakeDbAsyncEnumerable(System.Collections.Generic.IEnumerable<T> enumerable) : base(enumerable)
                            {{ }}

                            public FakeDbAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression)
                            {{ }}

                            public System.Data.Entity.Infrastructure.IDbAsyncEnumerator<T> GetAsyncEnumerator()
                            {{
                                return new FakeDbAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
                            }}

                            System.Data.Entity.Infrastructure.IDbAsyncEnumerator System.Data.Entity.Infrastructure.IDbAsyncEnumerable.GetAsyncEnumerator()
                            {{
                                return GetAsyncEnumerator();
                            }}

                            IQueryProvider IQueryable.Provider
                            {{
                                get {{ return new FakeDbAsyncQueryProvider<T>(this); }}
                            }}
                            ");
                    }
                    _output?.WriteLine("}");

                    if (Settings.IncludeCodeGeneratedAttribute) // 937
                        _output?.WriteLine(CodeGeneratedAttribute);
                    _output?.WriteLine($@"{ Settings.DbContextClassModifiers } class FakeDbAsyncEnumerator<T> : System.Data.Entity.Infrastructure.IDbAsyncEnumerator<T>");
                    _output?.WriteLine("{");
                    using (_output?.WithIndent())
                    {
                        WriteTextBlock(_output, $@"
                            private readonly System.Collections.Generic.IEnumerator<T> _inner;

                            public FakeDbAsyncEnumerator(System.Collections.Generic.IEnumerator<T> inner)
                            {{
                                _inner = inner;
                            }}

                            public void Dispose()
                            {{
                                _inner.Dispose();
                            }}

                            public System.Threading.Tasks.Task<bool> MoveNextAsync(System.Threading.CancellationToken cancellationToken)
                            {{
                                return System.Threading.Tasks.Task.FromResult(_inner.MoveNext());
                            }}

                            public T Current
                            {{
                                get {{ return _inner.Current; }}
                            }}

                            object System.Data.Entity.Infrastructure.IDbAsyncEnumerator.Current
                            {{
                                get {{ return Current; }}
                            }}
                            ");
                    }
                    _output?.WriteLine("}");


                    if (Settings.ElementsToGenerate.HasFlag(Elements.Context) && !Settings.GenerateSeparateFiles)
                        _output?.WriteLine("#endregion"); // line 1000
                    #endregion
                }
                #endregion Fake Database context (Line 533 to 1002)

                Console.WriteLine("POCO classes...");
                #region POCO classes (Line 1003 to 1112)
                if (Settings.ElementsToGenerate.HasFlag(Elements.Poco))
                {
                    if (!Settings.GenerateSeparateFiles)
                        _output?.WriteLine("#region POCO classes\n"); // line 1005
                    foreach (Table tbl in Settings.Tables.Where(t => !t.IsMapping).OrderBy(x => x.NameHumanCase))
                    {
                        if (Settings.GenerateSeparateFiles)
                            StartNewFile(tbl.NameHumanCaseWithSuffix() + Settings.FileExtension);
                        if (!tbl.HasPrimaryKey)
                        {
                            _output?.WriteLine($"// The table '{tbl.Name}' is not usable by entity framework because it");
                            _output?.WriteLine($"// does not have a primary key. It is listed here for completeness.");
                        }
                        if (Settings.IncludeComments != CommentsStyle.None)
                            _output?.WriteLine($"// {tbl.Name}");
                        WritePocoClassExtendedComments(_output, tbl); // Line 1019
                        WritePocoClassAttributes(_output, tbl); // Line 1020
                        if (Settings.IncludeCodeGeneratedAttribute)
                            _output?.WriteLine(CodeGeneratedAttribute);
                        _output?.Write($"{ Settings.EntityClassesModifiers } class {tbl.NameHumanCaseWithSuffix()}");
                        if (this.WritePocoBaseClasses != null)
                            this.WritePocoBaseClasses(_output, tbl);
                        _output?.WriteLine();
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            this.WritePocoBaseClassBody(_output, tbl); // Line 1025
                            foreach (Column col in tbl.Columns.OrderBy(x => x.Ordinal).Where(x => !x.Hidden))
                                this.WritePocoColumn(_output, col);
                            Console.WriteLine("ReverseNavigationProperty...");
                            #region ReverseNavigationProperty (Line 1032 to 1055)
                            if (tbl.ReverseNavigationProperty.Count() > 0)
                            {
                                _output?.WriteLine("");
                                if (Settings.IncludeComments != CommentsStyle.None)
                                    _output?.WriteLine($"// Reverse navigation\n");
                                foreach (var s in tbl.ReverseNavigationProperty.OrderBy(x => x.Definition))
                                {
                                    if (Settings.IncludeComments != CommentsStyle.None)
                                    {
                                        _output?.WriteLine($"/// <summary>");
                                        _output?.WriteLine($"/// {s.Comments ?? "" }");
                                        _output?.WriteLine($"/// </summary>");
                                    }
                                    foreach (var rnpda in Settings.AdditionalReverseNavigationsDataAnnotations)
                                        _output?.WriteLine($"[{rnpda }]");
                                    if (s.AdditionalDataAnnotations != null)
                                    {
                                        foreach (var fkda in s.AdditionalDataAnnotations)
                                        {
                                            _output?.WriteLine($"[{fkda }]");
                                        }
                                    }
                                    _output?.WriteLine($"{s.Definition }");
                                }
                            }
                            #endregion ReverseNavigationProperty (Line 1032 to 1055)

                            Console.WriteLine("ForeignKeys...");
                            #region ForeignKeys - (Line 1056 to 1077)
                            if (tbl.HasForeignKey)
                            {
                                if (Settings.IncludeComments != CommentsStyle.None && tbl.Columns.SelectMany(x => x.EntityFk).Any())
                                    _output?.WriteLine("// Foreign keys");
                                foreach (var entityFk in tbl.Columns.SelectMany(x => x.EntityFk).OrderBy(o => o.Definition))
                                {
                                    if (Settings.IncludeComments != CommentsStyle.None)
                                    {
                                        _output?.WriteLine($"/// <summary>");
                                        _output?.WriteLine($"/// {entityFk.Comments ?? "" }");
                                        _output?.WriteLine($"/// </summary>");
                                    }
                                    foreach (var fkda in Settings.AdditionalForeignKeysDataAnnotations)
                                        _output?.WriteLine($"[{fkda }]");
                                    if (entityFk.AdditionalDataAnnotations != null)
                                    {
                                        foreach (var fkda in entityFk.AdditionalDataAnnotations)
                                        {
                                            _output?.WriteLine($"[{fkda }]");
                                        }
                                    }
                                    _output?.WriteLine($"{entityFk.Definition }");
                                }
                            }
                            #endregion ForeignKeys - (Line 1056 to 1077)

                            Console.WriteLine("UsePropertyInitializers...");
                            #region POCO UsePropertyInitializers (Line 1079 to 1104)
                            if (!Settings.UsePropertyInitializers)
                            {
                                if (tbl.Columns.Where(c => c.Default != string.Empty && !c.Hidden).Count() > 0 || tbl.ReverseNavigationCtor.Count() > 0 || Settings.EntityClassesArePartial())
                                {
                                    _output?.WriteLine($"public {tbl.NameHumanCaseWithSuffix()}()");
                                    _output?.WriteLine("{");
                                    using (_output?.WithIndent())
                                    {
                                        foreach (var col in tbl.Columns.OrderBy(x => x.Ordinal).Where(c => c.Default != string.Empty && !c.Hidden))
                                            _output?.WriteLine($"{col.NameHumanCase } = {col.Default };");
                                        foreach (string s in tbl.ReverseNavigationCtor)
                                            _output?.WriteLine(s);
                                        if (Settings.EntityClassesArePartial())
                                            _output?.WriteLine("InitializePartial();");
                                    }
                                    _output?.WriteLine("}");
                                    if (Settings.EntityClassesArePartial())
                                        _output?.WriteLine("partial void InitializePartial();");
                                }
                            }
                            #endregion POCO UsePropertyInitializers (Line 1079 to 1104)


                        }
                        _output?.WriteLine("}");
                    }
                }
                if (Settings.ElementsToGenerate.HasFlag(Elements.Poco) && !Settings.GenerateSeparateFiles)
                    _output?.WriteLine("#endregion\n"); // line 1110
                #endregion POCO classes (Line 1003 to 1112)

                Console.WriteLine("POCO Configuration...");
                #region POCO Configuration (Line 1113 to 1178)
                if (Settings.ElementsToGenerate.HasFlag(Elements.PocoConfiguration))
                {
                    if (!Settings.GenerateSeparateFiles)
                        _output?.WriteLine("#region POCO Configuration\n"); // line 1115
                    foreach (var tbl in Settings.Tables.Where(t => !t.IsMapping && t.HasPrimaryKey).OrderBy(x => x.NameHumanCase))
                    {
                        if (Settings.GenerateSeparateFiles)
                            StartNewFile(tbl.NameHumanCaseWithSuffix() + Settings.ConfigurationClassName + Settings.FileExtension);
                        if (Settings.IncludeComments != CommentsStyle.None)
                            _output?.WriteLine($"// {tbl.Name}");
                        if (Settings.IncludeCodeGeneratedAttribute)
                            _output?.WriteLine(CodeGeneratedAttribute);
                        _output?.WriteLine($"{ Settings.ConfigurationClassesModifiers } class {tbl.NameHumanCaseWithSuffix() + Settings.ConfigurationClassName} : System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<{tbl.NameHumanCaseWithSuffix()}>");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            _output?.WriteLine($"public {tbl.NameHumanCaseWithSuffix() + Settings.ConfigurationClassName}() : this(\"{ tbl.Schema ?? ""}\")");
                            _output?.WriteLine("{");
                            _output?.WriteLine("}");

                            _output?.WriteLine($"public {tbl.NameHumanCaseWithSuffix() + Settings.ConfigurationClassName}(string schema)");
                            _output?.WriteLine("{");
                            using (_output?.WithIndent())
                            {
                                if (!Settings.UseDataAnnotations)
                                {
                                    if (!string.IsNullOrEmpty(tbl.Schema))
                                        _output?.WriteLine($"ToTable(\"{ tbl.Name}\", schema);");
                                    else
                                        _output?.WriteLine($"ToTable(\"{ tbl.Name}\");");
                                }
                                if (!Settings.UseDataAnnotations)
                                    _output?.WriteLine($"HasKey({tbl.PrimaryKeyNameHumanCase()});\n");
                                foreach (var col in tbl.Columns.Where(x => !x.Hidden && !string.IsNullOrEmpty(x.Config)).OrderBy(x => x.Ordinal))
                                    _output?.WriteLine(col.Config);

                                Console.WriteLine("ForeignKeys 1151 ...");
                                #region ForeignKeys (Line 1151 to 1160)
                                if (tbl.HasForeignKey)
                                {
                                    if (Settings.IncludeComments != CommentsStyle.None && tbl.Columns.SelectMany(x => x.ConfigFk).Any())
                                        _output?.WriteLine($"// Foreign keys");
                                    foreach (var configFk in tbl.Columns.SelectMany(x => x.ConfigFk).OrderBy(o => o))
                                    {
                                        _output?.WriteLine(configFk);
                                    }
                                }
                                #endregion ForeignKeys (Line 1151 to 1160)
                                foreach (string s in tbl.MappingConfiguration)
                                    _output?.WriteLine(s);
                                if (Settings.DbContextClassIsPartial())
                                    _output?.WriteLine("InitializePartial();");
                            }
                            _output?.WriteLine("}");
                            if (Settings.EntityClassesArePartial())
                                _output?.WriteLine("partial void InitializePartial();");
                        }
                        _output?.WriteLine("}"); // Line 1172
                    }
                } // Line 1174
                if (Settings.ElementsToGenerate.HasFlag(Elements.PocoConfiguration) && !Settings.GenerateSeparateFiles)
                    _output?.WriteLine("#endregion\n"); // line 1176
                #endregion POCO Configuration (Line 1113 to 1178)

                Console.WriteLine("Stored procedure return models...");
                #region Stored procedure return models (Line 1179 to 1124)
                if (Settings.StoredProcs.Any() && Settings.ElementsToGenerate.HasFlag(Elements.Poco))
                {
                    if (!Settings.GenerateSeparateFiles)
                        _output?.WriteLine("#region Stored procedure return models\n"); // line 1181
                    foreach (var sp in Settings.StoredProcs.Where(x => x.ReturnModels.Count > 0 && x.ReturnModels.Any(returnColumns => returnColumns.Any()) && !Settings.StoredProcedureReturnTypes.ContainsKey(x.NameHumanCase) && !Settings.StoredProcedureReturnTypes.ContainsKey(x.Name)).OrderBy(x => x.NameHumanCase))
                    {
                        string spReturnClassName = WriteStoredProcReturnModelName(sp);
                        if (Settings.GenerateSeparateFiles)
                            StartNewFile(spReturnClassName + Settings.FileExtension);
                        if (Settings.IncludeCodeGeneratedAttribute)
                            _output?.WriteLine(CodeGeneratedAttribute);
                        _output?.WriteLine($"{Settings.ResultClassModifiers } class { spReturnClassName }");
                        _output?.WriteLine("{");
                        using (_output?.WithIndent())
                        {
                            var returnModelCount = sp.ReturnModels.Count;
                            if (returnModelCount < 2)
                            {
                                foreach (var returnColumn in sp.ReturnModels.First())
                                    _output?.WriteLine(WriteStoredProcReturnColumn(returnColumn));
                            }
                            else
                            {
                                int model = 0;
                                foreach (var returnModel in sp.ReturnModels)
                                {
                                    model++;
                                    _output?.WriteLine($"public class ResultSetModel{ model }");
                                    _output?.WriteLine("{");
                                    using (_output?.WithIndent())
                                    {
                                        foreach (var returnColumn in returnModel)
                                            _output?.WriteLine(WriteStoredProcReturnColumn(returnColumn));
                                    }
                                    _output?.WriteLine("}");
                                    _output?.WriteLine($"public System.Collections.Generic.List<ResultSetModel{ model }> ResultSet{ model };");
                                }

                            }

                        }
                        _output?.WriteLine("}");
                        _output?.WriteLine();
                    }
                }
                if (Settings.StoredProcs.Any() && Settings.ElementsToGenerate.HasFlag(Elements.Poco) && !Settings.GenerateSeparateFiles)
                    _output?.WriteLine("#endregion\n"); // line 1222
                #endregion Stored procedure return models (Line 1179 to 1124)

                FinishCurrentFile();
            }
            //_output?.WriteLine("}"); // this is in FinishCurrentFile()
            #endregion Namespace / whole file (Line 31 to 1226)
            //_output?.WriteLine("// </auto-generated>"); // this is in FinishCurrentFile()

            #endregion
            #endregion
        }

        #region All this code mostly came from Simon Hughes T4 templates (with minor adjustments) - EF.Reverse.POCO.Core.ttinclude - see https://github.com/sjh37/EntityFramework-Reverse-POCO-Code-First-Generator

        [Flags]
        public enum CommentsStyle
        {
            None,
            InSummaryBlock,
            AtEndOfField
        };

        // Settings to allow selective code generation
        [Flags]
        public enum Elements
        {
            None = 0,
            Poco = 1,
            Context = 2,
            UnitOfWork = 4,
            PocoConfiguration = 8
        };

        // Settings - edit these in the main <name>.tt file *******************************************************************************
        public static class Settings
        {
            // Main settings
            public static string ConnectionStringName;
            public static string ConnectionString;
            public static string ProviderName;
            public static string Namespace;
            public static int CommandTimeout = 0;

            public static bool IncludeViews;
            public static bool IncludeSynonyms;
            public static bool IncludeStoredProcedures;
            public static bool IncludeTableValuedFunctions;
            public static bool AddIDbContextFactory;
            public static bool AddUnitTestingDbContext;
            public static string DbContextName;

            private static string _dbContextInterfaceName;
            public static string DbContextInterfaceName
            {
                get { return _dbContextInterfaceName ?? ("I" + DbContextName); }
                set { _dbContextInterfaceName = value; }
            }

            public static string DbContextInterfaceBaseClasses;
            public static string DbContextBaseClass;

            public static bool AddParameterlessConstructorToDbContext = true;
            private static bool _explicitDefaultConstructorArgument;
            private static string _defaultConstructorArgument;
            public static string DefaultConstructorArgument
            {
                get { return _explicitDefaultConstructorArgument ? _defaultConstructorArgument : string.Format('"' + "Name={0}" + '"', ConnectionStringName); }
                set { _explicitDefaultConstructorArgument = true; _defaultConstructorArgument = value; }
            }

            public static string ConfigurationClassName = "Configuration";
            public static string CollectionInterfaceType = "System.Collections.Generic.ICollection";
            public static string CollectionType = "System.Collections.Generic.List";
            public static bool NullableShortHand;
            public static bool UseDataAnnotations;
            public static bool UseDataAnnotationsWithFluent;
            public static string EntityClassesModifiers = "public partial";
            public static string ConfigurationClassesModifiers = "internal";
            public static string DbContextClassModifiers = "public partial";
            public static string DbContextInterfaceModifiers = "public partial";
            public static string MigrationClassModifiers = "internal sealed";
            public static string ResultClassModifiers = "public partial";
            public static bool DbContextClassIsPartial()
            {
                return DbContextClassModifiers != null && DbContextClassModifiers.Contains("partial");
            }

            public static bool EntityClassesArePartial()
            {
                return EntityClassesModifiers != null && EntityClassesModifiers.Contains("partial");
            }

            public static bool ConfigurationClassesArePartial()
            {
                return ConfigurationClassesModifiers != null && ConfigurationClassesModifiers.Contains("partial");
            }
            public static bool GenerateSeparateFiles;
            public static bool UseMappingTables;
            public static bool UsePropertyInitializers;
            public static bool IsSqlCe;
            public static string FileExtension = ".cs";
            public static bool UsePascalCase;
            public static bool UsePrivateSetterForComputedColumns;
            public static CommentsStyle IncludeComments = CommentsStyle.AtEndOfField;
            public static bool IncludeQueryTraceOn9481Flag;
            public static CommentsStyle IncludeExtendedPropertyComments = CommentsStyle.InSummaryBlock;
            public static bool IncludeConnectionSettingComments;
            public static bool DisableGeographyTypes;
            public static bool PrependSchemaName;
            public static string TableSuffix;
            public static Regex SchemaFilterExclude;
            public static Regex SchemaFilterInclude;
            public static Regex TableFilterExclude;
            public static Regex TableFilterInclude;
            public static Regex StoredProcedureFilterExclude;
            public static Regex StoredProcedureFilterInclude;
            public static Func<Table, bool> TableFilter;
            public static Func<StoredProcedure, bool> StoredProcedureFilter;
            public static Func<Table, bool> ConfigurationFilter;
            public static Dictionary<string, string> StoredProcedureReturnTypes = new Dictionary<string, string>();
            public static Regex ColumnFilterExclude;
            public static bool UseLazyLoading;
            public static bool UseInheritedBaseInterfaceFunctions = false;
            public static string[] FilenameSearchOrder;
            public static string[] AdditionalNamespaces;
            public static string[] AdditionalContextInterfaceItems;
            public static string[] AdditionalReverseNavigationsDataAnnotations;
            public static string[] AdditionalForeignKeysDataAnnotations;
            //public static string ConfigFilePath;
            public static Func<string, string, bool, string> TableRename;
            public static Func<string, string, string, string> MappingTableRename;
            public static Func<StoredProcedure, string> StoredProcedureRename;
            public static Func<string, StoredProcedure, string> StoredProcedureReturnModelRename;
            public static Func<Column, Table, Column> UpdateColumn;
            public static Func<IList<ForeignKey>, Table, Table, bool, ForeignKey> ForeignKeyProcessing;
            public static Func<Table, Table, string, string, string[]> ForeignKeyAnnotationsProcessing;
            public static Func<ForeignKey, ForeignKey> ForeignKeyFilter;
            public static Func<string, ForeignKey, string, Relationship, short, string> ForeignKeyName;
            public static Action<Table> ViewProcessing;
            public static Action<List<ForeignKey>, Tables> AddForeignKeys;
            public static string MigrationConfigurationFileName;
            public static string MigrationStrategy = "MigrateDatabaseToLatestVersion";
            public static string ContextKey;
            public static bool AutomaticMigrationsEnabled;
            public static bool AutomaticMigrationDataLossAllowed;
            public static List<EnumDefinition> EnumDefinitions = new List<EnumDefinition>();
            public static Dictionary<string, string> ColumnNameToDataAnnotation;
            public static Dictionary<string, string> ColumnTypeToDataAnnotation;
            public static bool IncludeCodeGeneratedAttribute;
            public static Tables Tables;
            public static List<StoredProcedure> StoredProcs;

            public static Elements ElementsToGenerate;
            public static string PocoNamespace, ContextNamespace, UnitOfWorkNamespace, PocoConfigurationNamespace;

            public static decimal TargetFrameworkVersion;
            public static Func<string, bool> IsSupportedFrameworkVersion = (string frameworkVersion) =>
            {
                var nfi = CultureInfo.InvariantCulture.NumberFormat;
                var isSupported = decimal.Parse(frameworkVersion, nfi);
                return isSupported <= TargetFrameworkVersion;
            };
        };

        const string CodeGeneratedAttribute = "[System.CodeDom.Compiler.GeneratedCode(\"EF.Reverse.POCO.Generator\", \"2.37.4.0\")]";
        //const string DataDirectory = "|DataDirectory|";

        static readonly List<string> NotNullable = new List<string>
    {
        "string",
        "byte[]",
        "datatable",
        "system.data.datatable",
        "object",
        "microsoft.sqlserver.types.sqlgeography",
        "microsoft.sqlserver.types.sqlgeometry",
        "system.data.entity.spatial.dbgeography",
        "system.data.entity.spatial.dbgeometry",
        "system.data.entity.hierarchy.hierarchyid"
    };

        static readonly List<string> ReservedKeywords = new List<string>
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
        "checked", "class", "const", "continue", "decimal", "default", "delegate", "do",
        "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed",
        "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator",
        "out", "override", "params", "private", "protected", "public", "readonly", "ref",
        "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong",
        "unchecked", "unsafe", "ushort", "using", "virtual", "volatile", "void", "while"
    };
        private static readonly Regex RxCleanUp = new Regex(@"[^\w\d\s_-]", RegexOptions.Compiled);

        private static readonly Func<string, string> CleanUp = (str) =>
        {
            // Replace punctuation and symbols in variable names as these are not allowed.
            int len = str.Length;
            if (len == 0)
                return str;
            var sb = new StringBuilder();
            bool replacedCharacter = false;
            for (int n = 0; n < len; ++n)
            {
                char c = str[n];
                if (c != '_' && c != '-' && (char.IsSymbol(c) || char.IsPunctuation(c)))
                {
                    int ascii = c;
                    sb.AppendFormat("{0}", ascii);
                    replacedCharacter = true;
                    continue;
                }
                sb.Append(c);
            }
            if (replacedCharacter)
                str = sb.ToString();

            // Remove non alphanumerics
            str = RxCleanUp.Replace(str, "");
            if (char.IsDigit(str[0]))
                str = "C" + str;

            return str;
        };

        private static readonly Func<string, string> ToDisplayName = (str) =>
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            var sb = new StringBuilder();
            str = Regex.Replace(str, @"[^a-zA-Z0-9]", " "); // Anything that is not a letter or digit, convert to a space
            str = Regex.Replace(str, @"[A-Z]{2,}", " $+ "); // Any word that is upper case

            var hasUpperCased = false;
            var lastChar = '\0';
            foreach (var original in str.Trim())
            {
                var c = original;
                if (lastChar == '\0')
                {
                    c = char.ToUpperInvariant(original);
                }
                else
                {
                    var isLetter = char.IsLetter(original);
                    var isDigit = char.IsDigit(original);
                    var isWhiteSpace = !isLetter && !isDigit;

                    // Is this char is different to last time
                    var isDifferent = false;
                    if (isLetter && !char.IsLetter(lastChar))
                        isDifferent = true;
                    else if (isDigit && !char.IsDigit(lastChar))
                        isDifferent = true;
                    else if (char.IsUpper(original) && !char.IsUpper(lastChar))
                        isDifferent = true;

                    if (isDifferent || isWhiteSpace)
                        sb.Append(' '); // Add a space

                    if (hasUpperCased && isLetter)
                        c = char.ToLowerInvariant(original);
                }
                lastChar = original;
                if (!hasUpperCased && char.IsUpper(c))
                    hasUpperCased = true;
                sb.Append(c);
            }
            str = sb.ToString();
            str = Regex.Replace(str, @"\s+", " ").Trim(); // Multiple white space to one space
            str = Regex.Replace(str, @"\bid\b", "ID"); //  Make ID word uppercase
            return str;
        };

        public static void ArgumentNotNull<T>(T arg, string name) where T : class
        {
            if (arg == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        private static bool IsNullable(Column col)
        {
            return col.IsNullable && !NotNullable.Contains(col.PropertyType.ToLower());
        }

        public void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public void WriteLine(string message)
        {
            LogToOutput(message);
            //base.WriteLine(message);
        }

        public void Warning(string message)
        {
            LogToOutput(string.Format(CultureInfo.CurrentCulture, "Warning: {0}", message));
            //base.Warning(message);
        }
        public void Error(string message)
        {
            LogToOutput(string.Format(CultureInfo.CurrentCulture, "Error: {0}", message));
            throw new Exception(message);
            //base.Error(message);
        }

        private void LogToOutput(string message)
        {
            this._output?.WriteLine(message);
        }


        private static string ZapPassword()
        {
            var rx = new Regex("password=[^\";]*", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return rx.Replace(Settings.ConnectionString, "password=**zapped**;");
        }

        public void PrintError(String message, Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            while (ex != null)
            {
                sb.AppendLine(ex.Message);
                sb.AppendLine(ex.GetType().FullName);
                sb.AppendLine(ex.StackTrace);
                sb.AppendLine();
                ex = ex.InnerException;
            }
            String report = sb.ToString();

            Warning(message + " " + report);
            WriteLine("");
            WriteLine("// -----------------------------------------------------------------------------------------");
            WriteLine("// " + message);
            WriteLine("// -----------------------------------------------------------------------------------------");
            WriteLine(report);
            WriteLine("");
        }

        private DbProviderFactory GetDbProviderFactory()
        {
            WriteLine("// ------------------------------------------------------------------------------------------------");
            WriteLine("// This code was generated by EntityFramework Reverse POCO Generator (http://www.reversepoco.com/).");
            WriteLine("// Created by Simon Hughes (https://about.me/simon.hughes).");
            WriteLine("//");
            WriteLine("// Do not make changes directly to this file - edit the template instead.");
            WriteLine("//");
            if (Settings.IncludeConnectionSettingComments)
            {
                WriteLine("// The following connection settings were used to generate this file:");
                if (!string.IsNullOrEmpty(Settings.ConnectionStringName)) // && !string.IsNullOrEmpty(Settings.ConfigFilePath))
                {
                    //var solutionPath = Path.GetDirectoryName(GetSolution().FileName) + "\\";
                    //WriteLine("//     Configuration file:     \"{0}\"", Settings.ConfigFilePath.Replace(solutionPath, string.Empty));
                    WriteLine("//     Connection String Name: \"{0}\"", Settings.ConnectionStringName);
                }
                WriteLine("//     Connection String:      \"{0}\"", ZapPassword());
                WriteLine("// ------------------------------------------------------------------------------------------------");
            }

            if (!string.IsNullOrEmpty(Settings.ProviderName))
            {
                try
                {
                    return DbProviderFactories.GetFactory(Settings.ProviderName);
                }
                catch (Exception x)
                {
                    PrintError("Failed to load provider \"" + Settings.ProviderName + "\".", x);
                }
            }
            else
            {
                Warning("Failed to find providerName in the connection string");
                WriteLine("");
                WriteLine("// ------------------------------------------------------------------------------------------------");
                WriteLine("//  Failed to find providerName in the connection string");
                WriteLine("// ------------------------------------------------------------------------------------------------");
                WriteLine("");
            }
            return null;
        }

        private DbProviderFactory TryGetDbProviderFactory()
        {
            try
            {
                return DbProviderFactories.GetFactory(Settings.ProviderName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool IsSqlCeConnection(DbConnection connection)
        {
            if (connection.GetType().Name.ToLower() == "sqlceconnection")
                return true;
            return false;
        }

        private Tables LoadTables(DbProviderFactory factory)
        {
            if (factory == null || !(Settings.ElementsToGenerate.HasFlag(Elements.Poco) ||
                                    Settings.ElementsToGenerate.HasFlag(Elements.Context) ||
                                    Settings.ElementsToGenerate.HasFlag(Elements.UnitOfWork) ||
                                    Settings.ElementsToGenerate.HasFlag(Elements.PocoConfiguration)))
                return new Tables();

            try
            {
                using (var conn = factory.CreateConnection())
                {
                    if (conn == null)
                        return new Tables();

                    conn.ConnectionString = Settings.ConnectionString;
                    conn.Open();

                    Settings.IsSqlCe = IsSqlCeConnection(conn);

                    if (Settings.IsSqlCe)
                        Settings.PrependSchemaName = false;

                    var reader = new SqlServerSchemaReader(conn, factory) { Outer = this };
                    var tables = reader.ReadSchema();
                    var fkList = reader.ReadForeignKeys();
                    reader.IdentifyForeignKeys(fkList, tables);

                    foreach (var t in tables)
                    {
                        if (Settings.UseDataAnnotations || Settings.UseDataAnnotationsWithFluent)
                            t.SetupDataAnnotations();
                        t.Suffix = Settings.TableSuffix;
                    }

                    if (Settings.AddForeignKeys != null) Settings.AddForeignKeys(fkList, tables);

                    // Work out if there are any foreign key relationship naming clashes
                    reader.ProcessForeignKeys(fkList, tables, true);
                    if (Settings.UseMappingTables)
                        tables.IdentifyMappingTables(fkList, true);

                    // Now we know our foreign key relationships and have worked out if there are any name clashes,
                    // re-map again with intelligently named relationships.
                    tables.ResetNavigationProperties();

                    reader.ProcessForeignKeys(fkList, tables, false);
                    if (Settings.UseMappingTables)
                        tables.IdentifyMappingTables(fkList, false);

                    conn.Close();
                    return tables;
                }
            }
            catch (Exception x)
            {
                PrintError("Failed to read database schema in LoadTables().", x);
                return new Tables();
            }
        }

        /// <summary>AddRelationship overload for single-column foreign-keys.</summary>
        public static void AddRelationship(List<ForeignKey> fkList, Tables tablesAndViews, String name, String pkSchema, String pkTable, String pkColumn, String fkSchema, String fkTable, String fkColumn)
        {
            AddRelationship(fkList, tablesAndViews, name, pkSchema, pkTable, new String[] { pkColumn }, fkSchema, fkTable, new String[] { fkColumn });
        }

        public static void AddRelationship(List<ForeignKey> fkList, Tables tablesAndViews, String relationshipName, String pkSchema, String pkTableName, String[] pkColumns, String fkSchema, String fkTableName, String[] fkColumns)
        {
            // Argument validation:
            if (fkList == null) throw new ArgumentNullException("fkList");
            if (tablesAndViews == null) throw new ArgumentNullException("tablesAndViews");
            if (string.IsNullOrEmpty(relationshipName)) throw new ArgumentNullException("relationshipName");
            if (string.IsNullOrEmpty(pkSchema)) throw new ArgumentNullException("pkSchema");
            if (string.IsNullOrEmpty(pkTableName)) throw new ArgumentNullException("pkTableName");
            if (pkColumns == null) throw new ArgumentNullException("pkColumns");
            if (pkColumns.Length == 0 || pkColumns.Any(s => string.IsNullOrEmpty(s))) throw new ArgumentException("Invalid primary-key columns: No primary-key column names are specified, or at least one primary-key column name is empty.", "pkColumns");
            if (string.IsNullOrEmpty(fkSchema)) throw new ArgumentNullException("fkSchema");
            if (string.IsNullOrEmpty(fkTableName)) throw new ArgumentNullException("fkTableName");
            if (fkColumns == null) throw new ArgumentNullException("fkColumns");
            if (fkColumns.Length != pkColumns.Length || fkColumns.Any(s => string.IsNullOrEmpty(s))) throw new ArgumentException("Invalid foreign-key columns:Foreign-key column list has a different number of columns than the primary-key column list, or at least one foreign-key column name is empty.", "pkColumns");

            //////////////////

            Table pkTable = tablesAndViews.GetTable(pkTableName, pkSchema);
            if (pkTable == null) throw new ArgumentException("Couldn't find table " + pkSchema + "." + pkTableName);

            Table fkTable = tablesAndViews.GetTable(fkTableName, fkSchema);
            if (fkTable == null) throw new ArgumentException("Couldn't find table " + fkSchema + "." + fkTableName);

            // Ensure all columns exist:
            foreach (String pkCol in pkColumns)
            {
                if (pkTable.Columns.SingleOrDefault(c => c.Name == pkCol) == null) throw new ArgumentException("The relationship primary-key column \"" + pkCol + "\" does not exist in table or view " + pkSchema + "." + pkTableName);
            }
            foreach (String fkCol in fkColumns)
            {
                if (fkTable.Columns.SingleOrDefault(c => c.Name == fkCol) == null) throw new ArgumentException("The relationship foreign-key column \"" + fkCol + "\" does not exist in table or view " + fkSchema + "." + fkTableName);
            }

            for (int i = 0; i < pkColumns.Length; i++)
            {
                String pkc = pkColumns[i];
                String fkc = fkColumns[i];

                String pkTableNameFiltered = Settings.TableRename(pkTableName, pkSchema, pkTable.IsView); // TODO: This can probably be done-away with. Is `AddRelationship` called before or after table.NameFiltered is set?

                ForeignKey fk = new ForeignKey(
                    fkTableName: fkTable.Name,
                    fkSchema: fkSchema,
                    pkTableName: pkTable.Name,
                    pkSchema: pkSchema,
                    fkColumn: fkc,
                    pkColumn: pkc,
                    constraintName: "AddRelationship: " + relationshipName,
                    pkTableNameFiltered: pkTableNameFiltered,
                    ordinal: Int32.MaxValue,
                    cascadeOnDelete: false,
                    isNotEnforced: false
                );
                fk.IncludeReverseNavigation = true;

                fkList.Add(fk);
                fkTable.HasForeignKey = true;
            }
        }

        private List<StoredProcedure> LoadStoredProcs(DbProviderFactory factory)
        {
            if (factory == null || !Settings.IncludeStoredProcedures)
                return new List<StoredProcedure>();

            try
            {
                using (var conn = factory.CreateConnection())
                {
                    if (conn == null)
                        return new List<StoredProcedure>();

                    conn.ConnectionString = Settings.ConnectionString;
                    conn.Open();

                    if (Settings.IsSqlCe)
                        return new List<StoredProcedure>();

                    var reader = new SqlServerSchemaReader(conn, factory) { Outer = this };
                    var storedProcs = reader.ReadStoredProcs();
                    conn.Close();

                    // Remove unrequired stored procs
                    for (int i = storedProcs.Count - 1; i >= 0; i--)
                    {
                        if (Settings.SchemaFilterInclude != null && !Settings.SchemaFilterInclude.IsMatch(storedProcs[i].Schema))
                        {
                            storedProcs.RemoveAt(i);
                            continue;
                        }
                        if (Settings.StoredProcedureFilterInclude != null && !Settings.StoredProcedureFilterInclude.IsMatch(storedProcs[i].Name))
                        {
                            storedProcs.RemoveAt(i);
                            continue;
                        }
                        if (!Settings.StoredProcedureFilter(storedProcs[i]))
                        {
                            storedProcs.RemoveAt(i);
                            continue;
                        }
                    }

                    using (var sqlConnection = new SqlConnection(Settings.ConnectionString))
                    {
                        foreach (var proc in storedProcs)
                            reader.ReadStoredProcReturnObject(sqlConnection, proc);
                    }

                    // Remove stored procs where the return model type contains spaces and cannot be mapped
                    // Also need to remove any TVF functions with parameters that are non scalar types, such as DataTable
                    var validStoredProcedures = new List<StoredProcedure>();
                    foreach (var sp in storedProcs)
                    {
                        if (!sp.ReturnModels.Any())
                        {
                            validStoredProcedures.Add(sp);
                            continue;
                        }

                        if (sp.ReturnModels.Any(returnColumns => returnColumns.Any(c => c.ColumnName.Contains(" "))))
                            continue;

                        if (sp.IsTVF && sp.Parameters.Any(c => c.PropertyType == "System.Data.DataTable"))
                            continue;

                        validStoredProcedures.Add(sp);
                    }
                    return validStoredProcedures;
                }
            }
            catch (Exception ex)
            {
                PrintError("Failed to read database schema for stored procedures.", ex);
                return new List<StoredProcedure>();
            }
        }

        public enum Relationship
        {
            OneToOne,
            OneToMany,
            ManyToOne,
            ManyToMany,
            DoNotUse
        }

        // Calculates the relationship between a child table and it's parent table.
        public static Relationship CalcRelationship(Table parentTable, Table childTable, List<Column> childTableCols, List<Column> parentTableCols)
        {
            if (childTableCols.Count == 1 && parentTableCols.Count == 1)
                return CalcRelationshipSingle(parentTable, childTable, childTableCols.First(), parentTableCols.First());

            // This relationship has multiple composite keys

            // childTable FK columns are exactly the primary key (they are part of primary key, and no other columns are primary keys) //TODO: we could also check if they are an unique index
            bool childTableColumnsAllPrimaryKeys = (childTableCols.Count == childTableCols.Count(x => x.IsPrimaryKey)) && (childTableCols.Count == childTable.PrimaryKeys.Count());

            // parentTable columns are exactly the primary key (they are part of primary key, and no other columns are primary keys) //TODO: we could also check if they are an unique index
            bool parentTableColumnsAllPrimaryKeys = (parentTableCols.Count == parentTableCols.Count(x => x.IsPrimaryKey)) && (parentTableCols.Count == parentTable.PrimaryKeys.Count());

            // childTable FK columns are not only FK but also the whole PK (not only part of PK); parentTable columns are the whole PK (not only part of PK) - so it's 1:1
            if (childTableColumnsAllPrimaryKeys && parentTableColumnsAllPrimaryKeys)
                return Relationship.OneToOne;

            return Relationship.ManyToOne;
        }

        // Calculates the relationship between a child table and it's parent table.
        public static Relationship CalcRelationshipSingle(Table parentTable, Table childTable, Column childTableCol, Column parentTableCol)
        {
            if (!childTableCol.IsPrimaryKey && !childTableCol.IsUniqueConstraint)
                return Relationship.ManyToOne;

            if (!parentTableCol.IsPrimaryKey && !parentTableCol.IsUniqueConstraint)
                return Relationship.ManyToOne;

            if (childTable.PrimaryKeys.Count() != 1)
                return Relationship.ManyToOne;

            if (parentTable.PrimaryKeys.Count() != 1)
                return Relationship.ManyToOne;

            return Relationship.OneToOne;
        }

        public class EnumDefinition
        {
            public string Schema;
            public string Table;
            public string Column;
            public string EnumType;
        }

        #region Nested type: Column

        public class PropertyAndComments
        {
            public string Definition;
            public string Comments;
            public string[] AdditionalDataAnnotations;
        }

        public class Column
        {
            public string Name; // Raw name of the column as obtained from the database
            public string NameHumanCase; // Name adjusted for C# output
            public string DisplayName;  // Name used in the data annotation [Display(Name = "<DisplayName> goes here")]
            public bool OverrideModifier = false; // Adds 'override' to the property declaration

            public int DateTimePrecision;
            public string Default;
            public int MaxLength;
            public int Precision;
            public string SqlPropertyType;
            public string PropertyType;
            public int Scale;
            public int Ordinal;
            public int PrimaryKeyOrdinal;
            public string ExtendedProperty;
            public string SummaryComments;
            public string UniqueIndexName;
            public bool AllowEmptyStrings = true;

            public bool IsIdentity;
            public bool IsRowGuid;
            public bool IsComputed;
            public ColumnGeneratedAlwaysType GeneratedAlwaysType;
            public bool IsNullable;
            public bool IsPrimaryKey;
            public bool IsUniqueConstraint;
            public bool IsUnique;
            public bool IsStoreGenerated;
            public bool IsRowVersion;
            public bool IsConcurrencyToken; //  Manually set via callback
            public bool IsFixedLength;
            public bool IsUnicode;
            public bool IsMaxLength;
            public bool Hidden;
            public bool IsForeignKey;

            public string Config;
            public List<string> ConfigFk = new List<string>();
            public string Entity;
            public List<PropertyAndComments> EntityFk = new List<PropertyAndComments>();

            public List<string> DataAnnotations;
            public List<Index> Indexes = new List<Index>();

            public Table ParentTable;

            public void ResetNavigationProperties()
            {
                ConfigFk = new List<string>();
                EntityFk = new List<PropertyAndComments>();
            }

            private void SetupEntity()
            {
                var comments = string.Empty;
                if (Settings.IncludeComments != CommentsStyle.None)
                {
                    comments = Name;
                    if (IsPrimaryKey)
                    {
                        if (IsUniqueConstraint)
                            comments += " (Primary key via unique index " + UniqueIndexName + ")";
                        else
                            comments += " (Primary key)";
                    }

                    if (MaxLength > 0)
                        comments += string.Format(" (length: {0})", MaxLength);
                }

                var inlineComments = Settings.IncludeComments == CommentsStyle.AtEndOfField ? " // " + comments : string.Empty;

                SummaryComments = string.Empty;
                if (Settings.IncludeComments == CommentsStyle.InSummaryBlock && !string.IsNullOrEmpty(comments))
                {
                    SummaryComments = comments;
                }
                if (Settings.IncludeExtendedPropertyComments == CommentsStyle.InSummaryBlock && !string.IsNullOrEmpty(ExtendedProperty))
                {
                    if (string.IsNullOrEmpty(SummaryComments))
                        SummaryComments = ExtendedProperty;
                    else
                        SummaryComments += ". " + ExtendedProperty;
                }

                if (Settings.IncludeExtendedPropertyComments == CommentsStyle.AtEndOfField && !string.IsNullOrEmpty(ExtendedProperty))
                {
                    if (string.IsNullOrEmpty(inlineComments))
                        inlineComments = " // " + ExtendedProperty;
                    else
                        inlineComments += ". " + ExtendedProperty;
                }
                var initialization = Settings.UsePropertyInitializers ? (Default == string.Empty ? "" : string.Format(" = {0};", Default)) : "";
                Entity = string.Format(
                    "public {0}{1} {2} {{ get; {3}set; }}{4}{5}",
                    (OverrideModifier ? "override " : ""), WrapIfNullable(PropertyType, this), NameHumanCase, Settings.UsePrivateSetterForComputedColumns && IsComputed ? "private " : string.Empty, initialization, inlineComments
                );
            }

            private string WrapIfNullable(string propType, Column col)
            {
                if (!IsNullable(col))
                    return propType;
                return string.Format(Settings.NullableShortHand ? "{0}?" : "System.Nullable<{0}>", propType);
            }

            private void SetupConfig()
            {
                DataAnnotations = new List<string>();
                string databaseGeneratedOption = null;
                var schemaReference = Settings.UseDataAnnotations
                    ? string.Empty
                    : "System.ComponentModel.DataAnnotations.Schema.";

                bool isNewSequentialId = !string.IsNullOrEmpty(Default) && Default.ToLower().Contains("newsequentialid");
                bool isTemporalColumn = this.GeneratedAlwaysType != ColumnGeneratedAlwaysType.NotApplicable;

                if (IsIdentity || isNewSequentialId || isTemporalColumn) // Identity, instead of Computed, seems the best for Temporal `GENERATED ALWAYS` columns: https://stackoverflow.com/questions/40742142/entity-framework-not-working-with-temporal-table
                {
                    if (Settings.UseDataAnnotations || isNewSequentialId)
                        DataAnnotations.Add("DatabaseGenerated(DatabaseGeneratedOption.Identity)");
                    else
                        databaseGeneratedOption = string.Format(".HasDatabaseGeneratedOption({0}DatabaseGeneratedOption.Identity)", schemaReference);
                }
                else if (IsComputed)
                {
                    if (Settings.UseDataAnnotations)
                        DataAnnotations.Add("DatabaseGenerated(DatabaseGeneratedOption.Computed)");
                    else
                        databaseGeneratedOption = string.Format(".HasDatabaseGeneratedOption({0}DatabaseGeneratedOption.Computed)", schemaReference);
                }
                else if (IsPrimaryKey)
                {
                    if (Settings.UseDataAnnotations)
                        DataAnnotations.Add("DatabaseGenerated(DatabaseGeneratedOption.None)");
                    else
                        databaseGeneratedOption = string.Format(".HasDatabaseGeneratedOption({0}DatabaseGeneratedOption.None)", schemaReference);
                }

                var sb = new StringBuilder();

                if (Settings.UseDataAnnotations)
                    DataAnnotations.Add(string.Format("Column(@\"{0}\", Order = {1}, TypeName = \"{2}\")", Name, Ordinal, SqlPropertyType));
                else
                    sb.AppendFormat(".HasColumnName(@\"{0}\").HasColumnType(\"{1}\")", Name, SqlPropertyType);

                if (Settings.UseDataAnnotations && Indexes.Any())
                {
                    foreach (var index in Indexes)
                    {
                        DataAnnotations.Add(string.Format("Index(@\"{0}\", {1}, IsUnique = {2}, IsClustered = {3})",
                            index.IndexName,
                            index.KeyOrdinal,
                            index.IsUnique ? "true" : "false",
                            index.IsClustered ? "true" : "false"));
                    }
                }

                if (IsNullable)
                {
                    sb.Append(".IsOptional()");
                }
                else
                {
                    if (!IsComputed && (Settings.UseDataAnnotations || Settings.UseDataAnnotationsWithFluent))
                    {
                        if (PropertyType.Equals("string", StringComparison.InvariantCultureIgnoreCase) && this.AllowEmptyStrings)
                        {
                            DataAnnotations.Add("Required(AllowEmptyStrings = true)");
                        }
                        else
                        {
                            DataAnnotations.Add("Required");
                        }
                    }

                    if (!Settings.UseDataAnnotations)
                    {
                        sb.Append(".IsRequired()");
                    }
                }

                if (IsFixedLength || IsRowVersion)
                {
                    sb.Append(".IsFixedLength()");
                    // DataAnnotations.Add("????");
                }

                if (!IsUnicode)
                {
                    sb.Append(".IsUnicode(false)");
                    // DataAnnotations.Add("????");
                }

                if (!IsMaxLength && MaxLength > 0)
                {
                    var doNotSpecifySize = (Settings.IsSqlCe && MaxLength > 4000); // Issue #179

                    if (Settings.UseDataAnnotations || Settings.UseDataAnnotationsWithFluent)
                    {
                        DataAnnotations.Add(doNotSpecifySize ? "MaxLength" : string.Format("MaxLength({0})", MaxLength));

                        if (PropertyType.Equals("string", StringComparison.InvariantCultureIgnoreCase))
                            DataAnnotations.Add(string.Format("StringLength({0})", MaxLength));
                    }

                    if (!Settings.UseDataAnnotations)
                    {
                        if (doNotSpecifySize)
                        {
                            sb.Append(".HasMaxLength(null)");
                        }
                        else
                        {
                            sb.AppendFormat(".HasMaxLength({0})", MaxLength);
                        }
                    }
                }

                if (IsMaxLength)
                {
                    if (Settings.UseDataAnnotations || Settings.UseDataAnnotationsWithFluent)
                    {
                        DataAnnotations.Add("MaxLength");
                    }

                    if (!Settings.UseDataAnnotations)
                    {
                        sb.Append(".IsMaxLength()");
                    }
                }

                if ((Precision > 0 || Scale > 0) && PropertyType == "decimal")
                {
                    sb.AppendFormat(".HasPrecision({0},{1})", Precision, Scale);
                    // DataAnnotations.Add("????");
                }

                if (IsRowVersion)
                {
                    if (Settings.UseDataAnnotations)
                        DataAnnotations.Add("Timestamp");
                    else
                        sb.Append(".IsRowVersion()");
                }

                if (IsConcurrencyToken)
                {
                    sb.Append(".IsConcurrencyToken()");
                    // DataAnnotations.Add("????");
                }

                if (databaseGeneratedOption != null)
                    sb.Append(databaseGeneratedOption);

                var config = sb.ToString();
                if (!string.IsNullOrEmpty(config))
                    Config = string.Format("Property(x => x.{0}){1};", NameHumanCase, config);

                if (IsPrimaryKey && Settings.UseDataAnnotations)
                    DataAnnotations.Add("Key");

                string valueFromName, valueFromType;
                if (Settings.ColumnNameToDataAnnotation.TryGetValue(NameHumanCase.ToLowerInvariant(), out valueFromName))
                {
                    DataAnnotations.Add(valueFromName);
                    if (valueFromName.StartsWith("Display(Name", StringComparison.InvariantCultureIgnoreCase))
                        return; // Skip adding Display(Name = "") below
                }
                else if (Settings.ColumnTypeToDataAnnotation.TryGetValue(SqlPropertyType.ToLowerInvariant(), out valueFromType))
                {
                    DataAnnotations.Add(valueFromType);
                    if (valueFromType.StartsWith("Display(Name", StringComparison.InvariantCultureIgnoreCase))
                        return; // Skip adding Display(Name = "") below
                }

                DataAnnotations.Add(string.Format("Display(Name = \"{0}\")", DisplayName));
            }

            public void SetupEntityAndConfig()
            {
                SetupEntity();
                SetupConfig();
            }

            public void CleanUpDefault()
            {
                if (string.IsNullOrWhiteSpace(Default))
                {
                    Default = string.Empty;
                    return;
                }

                // Remove outer brackets
                while (Default.First() == '(' && Default.Last() == ')' && Default.Length > 2)
                {
                    Default = Default.Substring(1, Default.Length - 2);
                }

                // Remove unicode prefix
                if (IsUnicode && Default.StartsWith("N") && !Default.Equals("NULL", StringComparison.InvariantCultureIgnoreCase))
                    Default = Default.Substring(1, Default.Length - 1);

                if (Default.First() == '\'' && Default.Last() == '\'' && Default.Length >= 2)
                    Default = string.Format("\"{0}\"", Default.Substring(1, Default.Length - 2));

                string lower = Default.ToLower();
                string lowerPropertyType = PropertyType.ToLower();

                // Cleanup default
                switch (lowerPropertyType)
                {
                    case "bool":
                        Default = (Default == "0" || lower == "\"false\"" || lower == "false") ? "false" : "true";
                        break;

                    case "string":
                    case "datetime":
                    case "datetime2":
                    case "system.datetime":
                    case "timespan":
                    case "system.timespan":
                    case "datetimeoffset":
                    case "system.datetimeoffset":
                        if (Default.First() != '"')
                            Default = string.Format("\"{0}\"", Default);
                        if (Default.Contains('\\') || Default.Contains('\r') || Default.Contains('\n'))
                            Default = "@" + Default;
                        else
                            Default = string.Format("\"{0}\"", Default.Substring(1, Default.Length - 2).Replace("\"", "\\\"")); // #281 Default values must be escaped if contain double quotes
                        break;

                    case "long":
                    case "short":
                    case "int":
                    case "double":
                    case "float":
                    case "decimal":
                    case "byte":
                    case "guid":
                    case "system.guid":
                        if (Default.First() == '\"' && Default.Last() == '\"' && Default.Length > 2)
                            Default = Default.Substring(1, Default.Length - 2);
                        break;

                    case "byte[]":
                    case "system.data.entity.spatial.dbgeography":
                    case "system.data.entity.spatial.dbgeometry":
                        Default = string.Empty;
                        break;
                }

                // Ignore defaults we cannot interpret (we would need SQL to C# compiler)
                if (lower.StartsWith("create default"))
                {
                    Default = string.Empty;
                    return;
                }

                if (string.IsNullOrWhiteSpace(Default))
                {
                    Default = string.Empty;
                    return;
                }

                // Validate default
                switch (lowerPropertyType)
                {
                    case "long":
                        long l;
                        if (!long.TryParse(Default, out l))
                            Default = string.Empty;
                        break;

                    case "short":
                        short s;
                        if (!short.TryParse(Default, out s))
                            Default = string.Empty;
                        break;

                    case "int":
                        int i;
                        if (!int.TryParse(Default, out i))
                            Default = string.Empty;
                        break;

                    case "datetime":
                    case "datetime2":
                    case "system.datetime":
                        DateTime dt;
                        if (!DateTime.TryParse(Default, out dt))
                            Default = (lower.Contains("getdate()") || lower.Contains("sysdatetime")) ? "System.DateTime.Now" : (lower.Contains("getutcdate()") || lower.Contains("sysutcdatetime")) ? "System.DateTime.UtcNow" : string.Empty;
                        else
                            Default = string.Format("System.DateTime.Parse({0})", Default);
                        break;

                    case "datetimeoffset":
                    case "system.datetimeoffset":
                        DateTimeOffset dto;
                        if (!DateTimeOffset.TryParse(Default, out dto))
                            Default = (lower.Contains("getdate()") || lower.Contains("sysdatetimeoffset")) ? "System.DateTimeOffset.Now" : (lower.Contains("getutcdate()") || lower.Contains("sysutcdatetime")) ? "System.DateTimeOffset.UtcNow" : string.Empty;
                        else
                            Default = string.Format("System.DateTimeOffset.Parse({0})", Default);
                        break;

                    case "timespan":
                    case "system.timespan":
                        TimeSpan ts;
                        Default = TimeSpan.TryParse(Default, out ts) ? string.Format("System.TimeSpan.Parse({0})", Default) : string.Empty;
                        break;

                    case "double":
                        double d;
                        if (!double.TryParse(Default, out d))
                            Default = string.Empty;
                        if (Default.ToLowerInvariant().EndsWith("."))
                            Default += "0";
                        break;

                    case "float":
                        float f;
                        if (!float.TryParse(Default, out f))
                            Default = string.Empty;
                        if (!Default.ToLowerInvariant().EndsWith("f"))
                            Default += "f";
                        break;

                    case "decimal":
                        decimal dec;
                        if (!decimal.TryParse(Default, out dec))
                            Default = string.Empty;
                        else
                            Default += "m";
                        break;

                    case "byte":
                        byte b;
                        if (!byte.TryParse(Default, out b))
                            Default = string.Empty;
                        break;

                    case "bool":
                        bool x;
                        if (!bool.TryParse(Default, out x))
                            Default = string.Empty;
                        break;

                    case "string":
                        if (lower.Contains("newid()") || lower.Contains("newsequentialid()"))
                            Default = "System.Guid.NewGuid().ToString()";
                        if (lower.StartsWith("space("))
                            Default = "\"\"";
                        if (lower == "null")
                            Default = string.Empty;
                        break;

                    case "guid":
                    case "system.guid":
                        if (lower.Contains("newid()") || lower.Contains("newsequentialid()"))
                            Default = "System.Guid.NewGuid()";
                        else if (lower.Contains("null"))
                            Default = "null";
                        else
                            Default = string.Format("System.Guid.Parse(\"{0}\")", Default);
                        break;
                }
            }
        }

        #endregion

        #region Nested type: Stored Procedure

        public class StoredProcedure
        {
            public string Schema;
            public string Name;
            public string NameHumanCase;
            public List<StoredProcedureParameter> Parameters;
            public List<List<DataColumn>> ReturnModels;    // A list of return models, containing a list of return columns
            public bool IsTVF;

            public StoredProcedure()
            {
                Parameters = new List<StoredProcedureParameter>();
                ReturnModels = new List<List<DataColumn>>();
            }

            public static bool IsNullable(DataColumn col)
            {
                return col.AllowDBNull &&
                       !(NotNullable.Contains(col.DataType.Name.ToLower())
                       || NotNullable.Contains(col.DataType.Namespace.ToLower() + "." + col.DataType.Name.ToLower()));
            }

            public static string WrapTypeIfNullable(string propertyType, DataColumn col)
            {
                if (!IsNullable(col))
                    return propertyType;
                return string.Format(Settings.NullableShortHand ? "{0}?" : "System.Nullable<{0}>", propertyType);
            }

        }

        public enum StoredProcedureParameterMode
        {
            In,
            InOut,
            Out
        };

        public class StoredProcedureParameter
        {
            public int Ordinal;
            public StoredProcedureParameterMode Mode;
            public string Name;
            public string NameHumanCase;
            public string SqlDbType;
            public string PropertyType;
            public string UserDefinedTypeName;
            public int DateTimePrecision;
            public int MaxLength;
            public int Precision;
            public int Scale;
        }

        #endregion

        #region Nested type: Inflector

        /// <summary>
        /// Summary for the Inflector class
        /// </summary>
        public static class Inflector
        {
            public static IPluralizationService PluralizationService = null;

            /// <summary>
            /// Makes the plural.
            /// </summary>
            /// <param name="word">The word.</param>
            /// <returns></returns>
            public static string MakePlural(string word)
            {
                try
                {
                    if (string.IsNullOrEmpty(word))
                        return string.Empty;
                    if (PluralizationService == null)
                        return word;

                    if (word.Contains('_')) return MakePluralHelper(word, '_');
                    if (word.Contains(' ')) return MakePluralHelper(word, ' ');
                    if (word.Contains('-')) return MakePluralHelper(word, '-');

                    return PluralizationService.Pluralize(word);
                }
                catch (Exception)
                {
                    return word;
                }
            }

            private static string MakePluralHelper(string word, char split)
            {
                if (string.IsNullOrEmpty(word))
                    return string.Empty;
                var parts = word.Split(split);
                parts[parts.Length - 1] = PluralizationService.Pluralize(parts[parts.Length - 1]); // Pluralize just the last word
                return string.Join(split.ToString(), parts);
            }

            /// <summary>
            /// Makes the singular.
            /// </summary>
            /// <param name="word">The word.</param>
            /// <returns></returns>
            public static string MakeSingular(string word)
            {
                try
                {
                    if (string.IsNullOrEmpty(word))
                        return string.Empty;

                    if (PluralizationService == null)
                        return word;

                    if (word.Contains('_')) return MakeSingularHelper(word, '_');
                    if (word.Contains(' ')) return MakeSingularHelper(word, ' ');
                    if (word.Contains('-')) return MakeSingularHelper(word, '-');

                    return PluralizationService.Singularize(word);
                }
                catch (Exception)
                {
                    return word;
                }
            }

            private static string MakeSingularHelper(string word, char split)
            {
                if (string.IsNullOrEmpty(word))
                    return string.Empty;
                var parts = word.Split(split);
                parts[parts.Length - 1] = PluralizationService.Singularize(parts[parts.Length - 1]); // Pluralize just the last word
                return string.Join(split.ToString(), parts);
            }

            /// <summary>
            /// Converts the string to title case.
            /// </summary>
            /// <param name="word">The word.</param>
            /// <returns></returns>
            public static string ToTitleCase(string word)
            {
                if (string.IsNullOrEmpty(word))
                    return string.Empty;

                var s = Regex.Replace(ToHumanCase(AddUnderscores(word)), @"\b([a-z])", match => match.Captures[0].Value.ToUpperInvariant());
                var digit = false;
                var sb = new StringBuilder();
                foreach (var c in s)
                {
                    if (char.IsDigit(c))
                    {
                        digit = true;
                        sb.Append(c);
                    }
                    else
                    {
                        if (digit && char.IsLower(c))
                            sb.Append(char.ToUpperInvariant(c));
                        else
                            sb.Append(c);
                        digit = false;
                    }
                }
                return sb.ToString();
            }

            /// <summary>
            /// Converts the string to human case.
            /// </summary>
            /// <param name="lowercaseAndUnderscoredWord">The lowercase and underscored word.</param>
            /// <returns></returns>
            public static string ToHumanCase(string lowercaseAndUnderscoredWord)
            {
                if (string.IsNullOrEmpty(lowercaseAndUnderscoredWord))
                    return string.Empty;
                return MakeInitialCaps(Regex.Replace(lowercaseAndUnderscoredWord, @"_", " "));
            }


            /// <summary>
            /// Adds the underscores.
            /// </summary>
            /// <param name="pascalCasedWord">The pascal cased word.</param>
            /// <returns></returns>
            public static string AddUnderscores(string pascalCasedWord)
            {
                if (string.IsNullOrEmpty(pascalCasedWord))
                    return string.Empty;
                return Regex.Replace(Regex.Replace(Regex.Replace(pascalCasedWord, @"([A-Z]+)([A-Z][a-z])", "$1_$2"), @"([a-z\d])([A-Z])", "$1_$2"), @"[-\s]", "_").ToLowerInvariant();
            }

            /// <summary>
            /// Makes the initial caps.
            /// </summary>
            /// <param name="word">The word.</param>
            /// <returns></returns>
            public static string MakeInitialCaps(string word)
            {
                if (string.IsNullOrEmpty(word))
                    return string.Empty;
                return string.Concat(word.Substring(0, 1).ToUpperInvariant(), word.Substring(1).ToLowerInvariant());
            }

            /// <summary>
            /// Makes the initial character lowercase.
            /// </summary>
            /// <param name="word">The word.</param>
            /// <returns></returns>
            public static string MakeInitialLower(string word)
            {
                if (string.IsNullOrEmpty(word))
                    return string.Empty;
                return string.Concat(word.Substring(0, 1).ToLowerInvariant(), word.Substring(1));
            }

            public static string MakeLowerIfAllCaps(string word)
            {
                if (string.IsNullOrEmpty(word))
                    return string.Empty;
                return IsAllCaps(word) ? word.ToLowerInvariant() : word;
            }

            public static bool IsAllCaps(string word)
            {
                if (string.IsNullOrEmpty(word))
                    return false;
                return word.All(char.IsUpper);
            }
        }

        #endregion

        private abstract class SchemaReader
        {
            protected readonly DbCommand Cmd;

            protected SchemaReader(DbConnection connection, DbProviderFactory factory)
            {
                Cmd = factory.CreateCommand();
                if (Cmd != null)
                    Cmd.Connection = connection;
            }

            public EFReversePOCOGenerator Outer;
            public abstract Tables ReadSchema();
            public abstract List<StoredProcedure> ReadStoredProcs();
            public abstract List<ForeignKey> ReadForeignKeys();
            public abstract void ProcessForeignKeys(List<ForeignKey> fkList, Tables tables, bool checkForFkNameClashes);
            public abstract void IdentifyForeignKeys(List<ForeignKey> fkList, Tables tables);
            public abstract void ReadIndexes(Tables tables);
            public abstract void ReadExtendedProperties(Tables tables, bool commentsInSummaryBlock);

            protected void WriteLine(string o)
            {
                Outer.WriteLine(o);
            }

            protected bool IsFilterExcluded(Regex filterExclude, Regex filterInclude, string name)
            {
                if (filterExclude != null && filterExclude.IsMatch(name))
                    return true;
                if (filterInclude != null && !filterInclude.IsMatch(name))
                    return true;
                if (name.Contains('.'))    // EF does not allow tables to contain a period character
                    return true;
                return false;
            }
        }

        private class SqlServerSchemaReader : SchemaReader
        {
            private static string _sqlDatabaseEdition, _sqlDatabaseEngineEdition, _sqlDatabaseProductVersion;
            private static int _sqlDatabaseProductMajorVersion;

            private const string TableSQL = @"
SET NOCOUNT ON;
IF OBJECT_ID('tempdb..#Columns')     IS NOT NULL DROP TABLE #Columns;
IF OBJECT_ID('tempdb..#PrimaryKeys') IS NOT NULL DROP TABLE #PrimaryKeys;
IF OBJECT_ID('tempdb..#ForeignKeys') IS NOT NULL DROP TABLE #ForeignKeys;

SELECT
    c.TABLE_SCHEMA,
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.ORDINAL_POSITION,
    c.COLUMN_DEFAULT,
    sc.IS_NULLABLE,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.NUMERIC_PRECISION,
    c.NUMERIC_SCALE,
    c.DATETIME_PRECISION,

    ss.schema_id,
    st.object_id AS table_object_id,
    sv.object_id AS view_object_id,

    sc.is_identity,
    sc.is_rowguidcol,
    sc.is_computed, -- Computed columns are read-only, do not confuse it with a column with a DEFAULT expression (which can be re-assigned). See the IsStoreGenerated attribute.
    CONVERT( tinyint, [sc].[generated_always_type] ) AS generated_always_type -- SQL Server 2016 (13.x) or later. 0 = Not generated, 1 = AS_ROW_START, 2 = AS_ROW_END

INTO
    #Columns
FROM
    INFORMATION_SCHEMA.COLUMNS c

    INNER JOIN sys.schemas AS ss ON c.TABLE_SCHEMA = ss.[name]
    LEFT OUTER JOIN sys.tables AS st ON st.schema_id = ss.schema_id AND st.[name] = c.TABLE_NAME
    LEFT OUTER JOIN sys.views AS sv ON sv.schema_id = ss.schema_id AND sv.[name] = c.TABLE_NAME
    INNER JOIN sys.all_columns AS sc ON sc.object_id = COALESCE( st.object_id, sv.object_id ) AND c.COLUMN_NAME = sc.[name]

WHERE
   c.TABLE_NAME NOT IN ('EdmMetadata', '__MigrationHistory', '__RefactorLog', 'sysdiagrams')


CREATE NONCLUSTERED INDEX IX_EfPoco_Columns
    ON dbo.#Columns (TABLE_NAME)
    INCLUDE (
        TABLE_SCHEMA,COLUMN_NAME,ORDINAL_POSITION,COLUMN_DEFAULT,IS_NULLABLE,DATA_TYPE,CHARACTER_MAXIMUM_LENGTH,NUMERIC_PRECISION,NUMERIC_SCALE,DATETIME_PRECISION,
        schema_id, table_object_id, view_object_id,
        is_identity,is_rowguidcol,is_computed,generated_always_type
    );

-----------

SELECT
    u.TABLE_SCHEMA,
    u.TABLE_NAME,
    u.COLUMN_NAME,
    u.ORDINAL_POSITION
INTO
    #PrimaryKeys
FROM
    INFORMATION_SCHEMA.KEY_COLUMN_USAGE u
    INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc ON
        u.TABLE_SCHEMA COLLATE DATABASE_DEFAULT = tc.CONSTRAINT_SCHEMA COLLATE DATABASE_DEFAULT
        AND
        u.TABLE_NAME = tc.TABLE_NAME
        AND
        u.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
WHERE
    CONSTRAINT_TYPE = 'PRIMARY KEY';

SELECT DISTINCT
    u.TABLE_SCHEMA,
    u.TABLE_NAME,
    u.COLUMN_NAME
INTO
    #ForeignKeys
FROM
    INFORMATION_SCHEMA.KEY_COLUMN_USAGE u
    INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc ON
        u.TABLE_SCHEMA COLLATE DATABASE_DEFAULT = tc.CONSTRAINT_SCHEMA COLLATE DATABASE_DEFAULT
        AND
        u.TABLE_NAME = tc.TABLE_NAME
        AND
        u.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
WHERE
    CONSTRAINT_TYPE = 'FOREIGN KEY';

--------------------------

SELECT
    c.TABLE_SCHEMA AS SchemaName,
    c.TABLE_NAME AS TableName,
    t.TABLE_TYPE AS TableType,
    CONVERT( tinyint, ISNULL( tt.temporal_type, 0 ) ) AS TableTemporalType,

    c.ORDINAL_POSITION AS Ordinal,
    c.COLUMN_NAME AS ColumnName,
    c.IS_NULLABLE AS IsNullable,
    DATA_TYPE AS TypeName,
    ISNULL(CHARACTER_MAXIMUM_LENGTH, 0) AS [MaxLength],
    CAST(ISNULL(NUMERIC_PRECISION, 0) AS INT) AS [Precision],
    ISNULL(COLUMN_DEFAULT, '') AS [Default],
    CAST(ISNULL(DATETIME_PRECISION, 0) AS INT) AS DateTimePrecision,
    ISNULL(NUMERIC_SCALE, 0) AS Scale,

    c.is_identity AS IsIdentity,
    c.is_rowguidcol AS IsRowGuid,
    c.is_computed AS IsComputed,
    c.generated_always_type AS GeneratedAlwaysType,

    CONVERT( bit,
        CASE WHEN
            c.is_identity = 1 OR
            c.is_rowguidcol = 1 OR
            c.is_computed = 1 OR
            c.generated_always_type <> 0 OR
            c.DATA_TYPE IN ( 'rowversion', 'timestamp' ) OR
            ( c.DATA_TYPE = 'uniqueidentifier' AND c.COLUMN_DEFAULT LIKE '%newsequentialid%' )
            THEN 1
        ELSE
            0
        END
    ) AS IsStoreGenerated,

    CONVERT( bit, ISNULL( pk.ORDINAL_POSITION, 0 ) ) AS PrimaryKey,
    ISNULL(pk.ORDINAL_POSITION, 0) PrimaryKeyOrdinal,
    CONVERT( bit, CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END ) AS IsForeignKey

FROM
    #Columns c

    LEFT OUTER JOIN #PrimaryKeys pk ON
        c.TABLE_SCHEMA = pk.TABLE_SCHEMA AND
        c.TABLE_NAME   = pk.TABLE_NAME AND
        c.COLUMN_NAME  = pk.COLUMN_NAME

    LEFT OUTER JOIN #ForeignKeys fk ON
        c.TABLE_SCHEMA = fk.TABLE_SCHEMA AND
        c.TABLE_NAME   = fk.TABLE_NAME AND
        c.COLUMN_NAME  = fk.COLUMN_NAME

    INNER JOIN INFORMATION_SCHEMA.TABLES t ON
        c.TABLE_SCHEMA COLLATE DATABASE_DEFAULT = t.TABLE_SCHEMA COLLATE DATABASE_DEFAULT AND
        c.TABLE_NAME   COLLATE DATABASE_DEFAULT = t.TABLE_NAME   COLLATE DATABASE_DEFAULT

    LEFT OUTER JOIN
    (
        SELECT
            st.object_id,
            [st].[temporal_type] AS temporal_type
        FROM
            sys.tables AS st
    ) AS tt ON c.table_object_id = tt.object_id
";

            private const string SynonymTableSQLSetup = @"
SET NOCOUNT ON;
IF OBJECT_ID('tempdb..#SynonymDetails') IS NOT NULL DROP TABLE #SynonymDetails;
IF OBJECT_ID('tempdb..#SynonymTargets') IS NOT NULL DROP TABLE #SynonymTargets;

-- Synonyms
-- Create the #SynonymDetails temp table structure for later use
SELECT TOP (0)
    sc.name AS SchemaName,
    sn.name AS TableName,
    'SN' AS TableType,
    CONVERT( tinyint, 0 ) AS TableTemporalType,
    COLUMNPROPERTY(c.object_id, c.name, 'ordinal') AS Ordinal,
    c.name AS ColumnName,
    c.is_nullable AS IsNullable,
    ISNULL(TYPE_NAME(c.system_type_id), t.name) AS TypeName,
    ISNULL(COLUMNPROPERTY(c.object_id, c.name, 'charmaxlen'), 0) AS MaxLength,
    CAST(ISNULL(CONVERT(TINYINT, CASE WHEN c.system_type_id IN (48, 52, 56, 59, 60, 62, 106, 108, 122, 127) THEN c.precision END), 0) AS INT) AS Precision,
    ISNULL(CONVERT(NVARCHAR(4000), OBJECT_DEFINITION(c.default_object_id)), '') AS [Default],
    CAST(ISNULL(CONVERT(SMALLINT, CASE WHEN c.system_type_id IN (40, 41, 42, 43, 58, 61) THEN ODBCSCALE(c.system_type_id, c.scale) END), 0) AS INT) AS DateTimePrecision,
    ISNULL(CONVERT(INT, CASE WHEN c.system_type_id IN (40, 41, 42, 43, 58, 61) THEN NULL ELSE ODBCSCALE(c.system_type_id, c.scale) END), 0) AS Scale,
    c.is_identity AS IsIdentity,
    c.is_rowguidcol AS IsRowGuid,
    c.is_computed AS IsComputed,
    CONVERT( tinyint, [c].[generated_always_type] ) AS GeneratedAlwaysType,
    CAST(CASE
        WHEN COLUMNPROPERTY(OBJECT_ID(QUOTENAME(sc.NAME) + '.' + QUOTENAME(o.NAME)), c.NAME, 'IsIdentity') = 1 THEN 1
        WHEN COLUMNPROPERTY(OBJECT_ID(QUOTENAME(sc.NAME) + '.' + QUOTENAME(o.NAME)), c.NAME, 'IsComputed') = 1 THEN 1
        WHEN COLUMNPROPERTY(OBJECT_ID(QUOTENAME(sc.NAME) + '.' + QUOTENAME(o.NAME)), c.NAME, 'GeneratedAlwaysType') > 0 THEN 1
        WHEN ISNULL(TYPE_NAME(c.system_type_id), t.NAME) = 'TIMESTAMP' THEN 1
        WHEN ISNULL(TYPE_NAME(c.system_type_id), t.NAME) = 'UNIQUEIDENTIFIER' AND LOWER(ISNULL(CONVERT(NVARCHAR(4000), OBJECT_DEFINITION(c.default_object_id)), '')) LIKE '%newsequentialid%' THEN 1
        ELSE 0
    END AS BIT) AS IsStoreGenerated,
    CAST(CASE WHEN pk.ORDINAL_POSITION IS NULL THEN 0 ELSE 1 END AS BIT) AS PrimaryKey,
    ISNULL(pk.ORDINAL_POSITION, 0) PrimaryKeyOrdinal,
    CAST(CASE WHEN fk.COLUMN_NAME IS NULL THEN 0 ELSE 1 END AS BIT) AS IsForeignKey
INTO
    #SynonymDetails
FROM
    sys.synonyms sn
    INNER JOIN sys.COLUMNS c ON c.[object_id] = OBJECT_ID(sn.base_object_name)
    INNER JOIN sys.schemas sc ON sc.[schema_id] = sn.[schema_id]
    LEFT JOIN sys.types t ON c.user_type_id = t.user_type_id
    INNER JOIN sys.objects o ON c.[object_id] = o.[object_id]
    LEFT OUTER JOIN
    (
        SELECT
            u.TABLE_SCHEMA,
            u.TABLE_NAME,
            u.COLUMN_NAME,
            u.ORDINAL_POSITION
        FROM
            INFORMATION_SCHEMA.KEY_COLUMN_USAGE u
            INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc ON u.TABLE_SCHEMA = tc.CONSTRAINT_SCHEMA AND u.TABLE_NAME = tc.TABLE_NAME AND u.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
        WHERE
            CONSTRAINT_TYPE = 'PRIMARY KEY'
    ) pk
        ON sc.NAME = pk.TABLE_SCHEMA AND sn.name = pk.TABLE_NAME AND c.name = pk.COLUMN_NAME

    LEFT OUTER JOIN
    (
        SELECT DISTINCT
            u.TABLE_SCHEMA,
            u.TABLE_NAME,
            u.COLUMN_NAME
        FROM
            INFORMATION_SCHEMA.KEY_COLUMN_USAGE u
            INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc ON u.TABLE_SCHEMA = tc.CONSTRAINT_SCHEMA AND u.TABLE_NAME = tc.TABLE_NAME AND u.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
        WHERE
            CONSTRAINT_TYPE = 'FOREIGN KEY'
    ) fk
        ON sc.NAME = fk.TABLE_SCHEMA AND sn.name = fk.TABLE_NAME AND c.name = fk.COLUMN_NAME;

DECLARE @synonymDetailsQueryTemplate nvarchar(max) = 'USE [@synonmymDatabaseName];
INSERT INTO #SynonymDetails (
    SchemaName, TableName, TableType, TableTemporalType, Ordinal, ColumnName, IsNullable, TypeName, [MaxLength], [Precision], [Default], DateTimePrecision, Scale,
    IsIdentity, IsRowGuid, IsComputed, GeneratedAlwaysType, IsStoreGenerated, PrimaryKey, PrimaryKeyOrdinal, IsForeignKey
)
SELECT
    st.SynonymSchemaName AS SchemaName,
    st.SynonymName AS TableName,
    ''SN'' AS TableType,
    CONVERT( tinyint, ISNULL( tt.temporal_type, 0 ) ) AS TableTemporalType,

    COLUMNPROPERTY(c.object_id, c.name, ''ordinal'') AS Ordinal,
    c.name AS ColumnName,
    c.is_nullable AS IsNullable,
    ISNULL(TYPE_NAME(c.system_type_id), t.name) AS TypeName,
    ISNULL(COLUMNPROPERTY(c.object_id, c.name, ''charmaxlen''), 0) AS [MaxLength],
    CAST(ISNULL(CONVERT(TINYINT, CASE WHEN c.system_type_id IN (48, 52, 56, 59, 60, 62, 106, 108, 122, 127) THEN c.precision END), 0) AS INT) AS [Precision],
    ISNULL(CONVERT(NVARCHAR(4000), OBJECT_DEFINITION(c.default_object_id)), '''') AS [Default],
    CAST(ISNULL(CONVERT(SMALLINT, CASE WHEN c.system_type_id IN (40, 41, 42, 43, 58, 61) THEN ODBCSCALE(c.system_type_id, c.scale) END), 0) AS INT) AS DateTimePrecision,
    ISNULL(CONVERT(INT, CASE WHEN c.system_type_id IN (40, 41, 42, 43, 58, 61) THEN NULL ELSE ODBCSCALE(c.system_type_id, c.scale) END), 0) AS Scale,

    c.is_identity AS IsIdentity,
    c.is_rowguidcol AS IsRowGuid,
    c.is_computed AS IsComputed,
    CONVERT( tinyint, [c].[generated_always_type] ) AS GeneratedAlwaysType,

    CONVERT( bit,
        CASE
            WHEN
                c.is_identity = 1 OR
                c.is_rowguidcol = 1 OR
                c.is_computed = 1 OR
                [c].[generated_always_type] <> 0 OR
                t.name IN ( ''rowversion'', ''timestamp'' ) OR
                ( t.name = ''uniqueidentifier'' AND sd.definition LIKE ''%newsequentialid%'' )
                THEN 1
            ELSE 0
        END
    ) AS IsStoreGenerated,

    CAST(CASE WHEN pk.ORDINAL_POSITION IS NULL THEN 0  ELSE 1 END AS BIT) AS PrimaryKey,
    ISNULL(pk.ORDINAL_POSITION, 0) PrimaryKeyOrdinal,
    CAST(CASE WHEN fk.COLUMN_NAME IS NULL THEN 0 ELSE 1 END AS BIT) AS IsForeignKey
FROM
    #SynonymTargets st
    
    INNER JOIN sys.columns c ON c.[object_id] = st.base_object_id
    
    LEFT JOIN sys.types t ON c.user_type_id = t.user_type_id

    LEFT OUTER JOIN sys.default_constraints sd ON c.default_object_id = sd.object_id
    
    INNER JOIN sys.objects o ON c.[object_id] = o.[object_id]
    
    LEFT OUTER JOIN
    (
        SELECT
            u.TABLE_SCHEMA,
            u.TABLE_NAME,
            u.COLUMN_NAME,
            u.ORDINAL_POSITION
        FROM
            INFORMATION_SCHEMA.KEY_COLUMN_USAGE u
            INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc ON u.TABLE_SCHEMA = tc.CONSTRAINT_SCHEMA AND u.TABLE_NAME = tc.TABLE_NAME AND u.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
        WHERE
            CONSTRAINT_TYPE = ''PRIMARY KEY''
    ) AS pk ON
        st.SchemaName = pk.TABLE_SCHEMA AND
        st.ObjectName = pk.TABLE_NAME AND
        c.name        = pk.COLUMN_NAME
    
    LEFT OUTER JOIN
    (
        SELECT DISTINCT
            u.TABLE_SCHEMA,
            u.TABLE_NAME,
            u.COLUMN_NAME
        FROM
            INFORMATION_SCHEMA.KEY_COLUMN_USAGE u
            INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc ON
                u.TABLE_SCHEMA = tc.CONSTRAINT_SCHEMA AND
                u.TABLE_NAME = tc.TABLE_NAME AND
                u.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
        WHERE
            CONSTRAINT_TYPE = ''FOREIGN KEY''
    ) AS fk ON
        st.SchemaName = fk.TABLE_SCHEMA AND
        st.ObjectName = fk.TABLE_NAME AND
        c.name = fk.COLUMN_NAME

    LEFT OUTER JOIN
    (
        SELECT
            st.object_id,
            [st].[temporal_type] AS temporal_type
        FROM
            sys.tables AS st
    ) AS tt ON c.object_id = tt.object_id

WHERE
    st.DatabaseName = @synonmymDatabaseName;
'

-- Pull details about the synonym target from each database being referenced
SELECT
    sc.name AS SynonymSchemaName,
    sn.name AS SynonymName,
    sn.object_id,
    sn.base_object_name,
    OBJECT_ID(sn.base_object_name) AS base_object_id,
    PARSENAME(sn.base_object_name, 1) AS ObjectName,
    ISNULL(PARSENAME(sn.base_object_name, 2), sc.name) AS SchemaName,
    ISNULL(PARSENAME(sn.base_object_name, 3), DB_NAME()) AS DatabaseName,
    PARSENAME(sn.base_object_name, 4) AS ServerName
INTO
    #SynonymTargets
FROM
    sys.synonyms sn
    INNER JOIN sys.schemas sc ON sc.schema_id = sn.schema_id
WHERE
    ISNULL(PARSENAME(sn.base_object_name, 4), @@SERVERNAME) = @@SERVERNAME; -- Only populate info from current server

-- Loop through synonyms and populate #SynonymDetails
DECLARE @synonmymDatabaseName sysname = (SELECT TOP (1) DatabaseName FROM #SynonymTargets)
DECLARE @synonmymDetailsSelect nvarchar(max)
WHILE ( @synonmymDatabaseName IS NOT NULL)
BEGIN
    SET @synonmymDetailsSelect = REPLACE(@synonymDetailsQueryTemplate, '[@synonmymDatabaseName]', '[' + @synonmymDatabaseName + ']')
    --SELECT @synonmymDetailsSelect
    EXEC sp_executesql @stmt=@synonmymDetailsSelect, @params=N'@synonmymDatabaseName sysname', @synonmymDatabaseName=@synonmymDatabaseName
    DELETE FROM #SynonymTargets WHERE DatabaseName = @synonmymDatabaseName
    SET @synonmymDatabaseName = (SELECT TOP (1) DatabaseName FROM #SynonymTargets)
END
SET NOCOUNT OFF;
";

            private const string SynonymTableSQL = @"
UNION
-- Synonyms
SELECT
    SchemaName,
    TableName,
    TableType,
    CONVERT( tinyint, 0 ) AS TableTemporalType,

    Ordinal,
    ColumnName,
    IsNullable,
    TypeName,
    [MaxLength],
    [Precision],
    [Default],
    DateTimePrecision,
    Scale,

    IsIdentity,
    IsRowGuid,
    IsComputed,
    GeneratedAlwaysType,

    IsStoreGenerated,
    PrimaryKey,
    PrimaryKeyOrdinal,
    IsForeignKey
FROM
    #SynonymDetails";

            private const string ForeignKeySQL = @"
SELECT  fkData.FK_Table,
        fkData.FK_Column,
        fkData.PK_Table,
        fkData.PK_Column,
        fkData.Constraint_Name,
        fkData.fkSchema,
        fkData.pkSchema,
        fkData.primarykey,
        fkData.ORDINAL_POSITION,
        fkData.CascadeOnDelete,
        fkData.IsNotEnforced
FROM    (SELECT FK.name AS FK_Table,
                FkCol.name AS FK_Column,
                PK.name AS PK_Table,
                PkCol.name AS PK_Column,
                OBJECT_NAME(f.object_id) AS Constraint_Name,
                SCHEMA_NAME(FK.schema_id) AS fkSchema,
                SCHEMA_NAME(PK.schema_id) AS pkSchema,
                PkCol.name AS primarykey,
                k.constraint_column_id AS ORDINAL_POSITION,
                CASE WHEN f.delete_referential_action = 1 THEN 1
                     ELSE 0
                END AS CascadeOnDelete,
                f.is_disabled AS IsNotEnforced,
                ROW_NUMBER() OVER (PARTITION BY FK.name, FkCol.name, PK.name, PkCol.name, SCHEMA_NAME(FK.schema_id), SCHEMA_NAME(PK.schema_id) ORDER BY f.object_id) AS n
         FROM   sys.objects AS PK
                INNER JOIN sys.foreign_keys AS f
                    INNER JOIN sys.foreign_key_columns AS k
                        ON k.constraint_object_id = f.object_id
                    INNER JOIN sys.indexes AS i
                        ON f.referenced_object_id = i.object_id
                           AND f.key_index_id = i.index_id
                    ON PK.object_id = f.referenced_object_id
                INNER JOIN sys.objects AS FK
                    ON f.parent_object_id = FK.object_id
                INNER JOIN sys.columns AS PkCol
                    ON f.referenced_object_id = PkCol.object_id
                       AND k.referenced_column_id = PkCol.column_id
                INNER JOIN sys.columns AS FkCol
                    ON f.parent_object_id = FkCol.object_id
                       AND k.parent_column_id = FkCol.column_id) fkData
WHERE   fkData.n = 1 -- Remove duplicate FK's";

            private const string SynonymForeignKeySQLSetup = @"
SET NOCOUNT ON;
IF OBJECT_ID('tempdb..#SynonymFkDetails') IS NOT NULL DROP TABLE #SynonymFkDetails;
IF OBJECT_ID('tempdb..#SynonymTargets') IS NOT NULL DROP TABLE #SynonymTargets;

-- Create the #SynonymFkDetails temp table structure for later use
SELECT  FK.name AS FK_Table,
        FkCol.name AS FK_Column,
        PK.name AS PK_Table,
        PkCol.name AS PK_Column,
        OBJECT_NAME(f.object_id) AS Constraint_Name,
        SCHEMA_NAME(FK.schema_id) AS fkSchema,
        SCHEMA_NAME(PK.schema_id) AS pkSchema,
        PkCol.name AS primarykey,
        k.constraint_column_id AS ORDINAL_POSITION,
        CASE WHEN f.delete_referential_action = 1 THEN 1 ELSE 0 END as CascadeOnDelete,
        f.is_disabled AS IsNotEnforced
INTO    #SynonymFkDetails
FROM    sys.objects AS PK
        INNER JOIN sys.foreign_keys AS f
            INNER JOIN sys.foreign_key_columns AS k
                ON k.constraint_object_id = f.object_id
            INNER JOIN sys.indexes AS i
                ON f.referenced_object_id = i.object_id
                   AND f.key_index_id = i.index_id
            ON PK.object_id = f.referenced_object_id
        INNER JOIN sys.objects AS FK
            ON f.parent_object_id = FK.object_id
        INNER JOIN sys.columns AS PkCol
            ON f.referenced_object_id = PkCol.object_id
               AND k.referenced_column_id = PkCol.column_id
        INNER JOIN sys.columns AS FkCol
            ON f.parent_object_id = FkCol.object_id
               AND k.parent_column_id = FkCol.column_id
ORDER BY FK_Table, FK_Column

-- Get all databases referenced by synonyms.
SELECT DISTINCT PARSENAME(sn.base_object_name, 3) AS DatabaseName
INTO #SynonymTargets
FROM sys.synonyms sn
WHERE PARSENAME(sn.base_object_name, 3) != DB_NAME()
AND ISNULL(PARSENAME(sn.base_object_name, 4), @@SERVERNAME) = @@SERVERNAME -- Only populate info from current server
ORDER BY PARSENAME(sn.base_object_name, 3)

-- Create a query to execute for each referenced database
DECLARE @synonymFkDetailsQuery nvarchar(max) =
'
INSERT INTO #SynonymFkDetails (FK_Table, FK_Column, PK_Table, PK_Column, Constraint_Name, fkSchema, pkSchema, primarykey, ORDINAL_POSITION,
                             CascadeOnDelete, IsNotEnforced)
SELECT  FK.name AS FK_Table,
        FkCol.name AS FK_Column,
        PK.name AS PK_Table,
        PkCol.name AS PK_Column,
        OBJECT_NAME(f.object_id) AS Constraint_Name,
        SCHEMA_NAME(FK.schema_id) AS fkSchema,
        SCHEMA_NAME(PK.schema_id) AS pkSchema,
        PkCol.name AS primarykey,
        k.constraint_column_id AS ORDINAL_POSITION,
        CASE WHEN f.delete_referential_action = 1 THEN 1 ELSE 0 END as CascadeOnDelete,
        f.is_disabled AS IsNotEnforced
FROM    sys.objects AS PK
        INNER JOIN sys.foreign_keys AS f
            INNER JOIN sys.foreign_key_columns AS k
                ON k.constraint_object_id = f.object_id
            INNER JOIN sys.indexes AS i
                ON f.referenced_object_id = i.object_id
                   AND f.key_index_id = i.index_id
            ON PK.object_id = f.referenced_object_id
        INNER JOIN sys.objects AS FK
            ON f.parent_object_id = FK.object_id
        INNER JOIN sys.columns AS PkCol
            ON f.referenced_object_id = PkCol.object_id
               AND k.referenced_column_id = PkCol.column_id
        INNER JOIN sys.columns AS FkCol
            ON f.parent_object_id = FkCol.object_id
               AND k.parent_column_id = FkCol.column_id
ORDER BY FK_Table, FK_Column;
'

-- Loop through referenced databases and populate #SynonymFkDetails
DECLARE @synonmymDatabaseName sysname = (SELECT TOP (1) DatabaseName FROM #SynonymTargets)
DECLARE @synonymFkDetailsQueryWithDb nvarchar(max)
WHILE (@synonmymDatabaseName IS NOT NULL)
BEGIN
    SET @synonymFkDetailsQueryWithDb = 'USE [' + @synonmymDatabaseName + '] ' + @synonymFkDetailsQuery
    EXEC sp_executesql @stmt=@synonymFkDetailsQueryWithDb
    DELETE FROM #SynonymTargets WHERE DatabaseName = @synonmymDatabaseName
    SET @synonmymDatabaseName = (SELECT TOP (1) DatabaseName FROM #SynonymTargets)
END

SET NOCOUNT OFF;
";

            private const string SynonymForeignKeySQL = @"
UNION
-- Synonyms
SELECT FK_Table, FK_Column, PK_Table, PK_Column, Constraint_Name, fkSchema, pkSchema, primarykey, ORDINAL_POSITION,
       CascadeOnDelete, IsNotEnforced FROM #SynonymFkDetails";

            private const string ExtendedPropertySQL = @"
SELECT  s.name AS [schema],
    t.name AS [table],
    c.name AS [column],
    value AS [property]
FROM    sys.extended_properties AS ep
    INNER JOIN sys.tables AS t
        ON ep.major_id = t.object_id
    INNER JOIN sys.schemas AS s
        ON s.schema_id = t.schema_id
    LEFT JOIN sys.columns AS c
        ON ep.major_id = c.object_id
            AND ep.minor_id = c.column_id
WHERE   class = 1
ORDER BY t.name";

            private const string ExtendedPropertyTableExistsSQLCE = @"
SELECT  1
FROM    INFORMATION_SCHEMA.TABLES
WHERE   TABLE_NAME = '__ExtendedProperties'";

            private const string ExtendedPropertySQLCE = @"
SELECT  '' AS [schema],
    [ObjectName] AS [column],
    [ParentName] AS [table],
    [Value] AS [property]
FROM    [__ExtendedProperties]";

            private const string TableSQLCE = @"
SELECT  '' AS SchemaName,
    c.TABLE_NAME AS TableName,
    'BASE TABLE' AS TableType,
    CONVERT( tinyint, 0 ) AS TableTemporalType,
    c.ORDINAL_POSITION AS Ordinal,
    c.COLUMN_NAME AS ColumnName,
    CAST(CASE WHEN c.IS_NULLABLE = N'YES' THEN 1 ELSE 0 END AS BIT) AS IsNullable,
    CASE WHEN c.DATA_TYPE = N'rowversion' THEN 'timestamp' ELSE c.DATA_TYPE END AS TypeName,
    CASE WHEN c.CHARACTER_MAXIMUM_LENGTH IS NOT NULL THEN c.CHARACTER_MAXIMUM_LENGTH ELSE 0 END AS MaxLength,
    CASE WHEN c.NUMERIC_PRECISION IS NOT NULL THEN c.NUMERIC_PRECISION ELSE 0 END AS Precision,
    c.COLUMN_DEFAULT AS [Default],
    CASE WHEN c.DATA_TYPE = N'datetime' THEN 0 ELSE 0 END AS DateTimePrecision,
    CASE WHEN c.DATA_TYPE = N'datetime' THEN 0 WHEN c.NUMERIC_SCALE IS NOT NULL THEN c.NUMERIC_SCALE ELSE 0 END AS Scale,

    CAST(CASE WHEN c.AUTOINC_INCREMENT > 0 THEN 1 ELSE 0 END AS BIT) AS IsIdentity,
    CONVERT( bit, 0 ) as IsComputed,
    CONVERT( bit, 0 ) as IsRowGuid,
    CONVERT( tinyint, 0 ) AS GeneratedAlwaysType,
    CAST(CASE WHEN c.DATA_TYPE = N'rowversion' THEN 1 ELSE 0 END AS BIT) AS IsStoreGenerated,
    0 AS PrimaryKeyOrdinal,
    CAST(CASE WHEN u.TABLE_NAME IS NULL THEN 0 ELSE 1 END AS BIT) AS PrimaryKey,
    CONVERT( bit, 0 ) as IsForeignKey
FROM
    INFORMATION_SCHEMA.COLUMNS c
    INNER JOIN INFORMATION_SCHEMA.TABLES t ON c.TABLE_NAME = t.TABLE_NAME
    LEFT JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS cons ON cons.TABLE_NAME = c.TABLE_NAME
    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS u ON
        cons.CONSTRAINT_NAME = u.CONSTRAINT_NAME AND
        u.TABLE_NAME = c.TABLE_NAME AND
        u.COLUMN_NAME = c.COLUMN_NAME
WHERE
    t.TABLE_TYPE <> N'SYSTEM TABLE' AND
    cons.CONSTRAINT_TYPE = 'PRIMARY KEY'
ORDER BY
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.ORDINAL_POSITION";

            private const string ForeignKeySQLCE = @"
SELECT DISTINCT
    FK.TABLE_NAME AS FK_Table,
    FK.COLUMN_NAME AS FK_Column,
    PK.TABLE_NAME AS PK_Table,
    PK.COLUMN_NAME AS PK_Column,
    FK.CONSTRAINT_NAME AS Constraint_Name,
    '' AS fkSchema,
    '' AS pkSchema,
    PT.COLUMN_NAME AS primarykey,
    FK.ORDINAL_POSITION,
    CASE WHEN C.DELETE_RULE = 'CASCADE' THEN 1 ELSE 0 END AS CascadeOnDelete,
    CAST(0 AS BIT) AS IsNotEnforced
FROM    INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS C
    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS FK
        ON FK.CONSTRAINT_NAME = C.CONSTRAINT_NAME
    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS PK
        ON PK.CONSTRAINT_NAME = C.UNIQUE_CONSTRAINT_NAME
            AND PK.ORDINAL_POSITION = FK.ORDINAL_POSITION
    INNER JOIN (
                SELECT  i1.TABLE_NAME,
                        i2.COLUMN_NAME
                FROM    INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1
                        INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2
                            ON i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME
                WHERE   i1.CONSTRAINT_TYPE = 'PRIMARY KEY'
                ) PT
        ON PT.TABLE_NAME = PK.TABLE_NAME
WHERE   PT.COLUMN_NAME = PK.COLUMN_NAME
ORDER BY FK.TABLE_NAME, FK.COLUMN_NAME";

            private const string StoredProcedureSQL = @"
SELECT  R.SPECIFIC_SCHEMA,
        R.SPECIFIC_NAME,
        R.ROUTINE_TYPE,
        P.ORDINAL_POSITION,
        P.PARAMETER_MODE,
        P.PARAMETER_NAME,
        P.DATA_TYPE,
        ISNULL(P.CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,
        ISNULL(P.NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,
        ISNULL(P.NUMERIC_SCALE, 0) AS NUMERIC_SCALE,
        ISNULL(P.DATETIME_PRECISION, 0) AS DATETIME_PRECISION,
        P.USER_DEFINED_TYPE_SCHEMA + '.' + P.USER_DEFINED_TYPE_NAME AS USER_DEFINED_TYPE
FROM    INFORMATION_SCHEMA.ROUTINES R
        LEFT OUTER JOIN INFORMATION_SCHEMA.PARAMETERS P
            ON P.SPECIFIC_SCHEMA = R.SPECIFIC_SCHEMA
               AND P.SPECIFIC_NAME = R.SPECIFIC_NAME
WHERE   R.ROUTINE_TYPE = 'PROCEDURE'
        AND (
             P.IS_RESULT = 'NO'
             OR P.IS_RESULT IS NULL
            )
        AND R.SPECIFIC_SCHEMA + R.SPECIFIC_NAME IN (
            SELECT  SCHEMA_NAME(sp.schema_id) + sp.name
            FROM    sys.all_objects AS sp
                    LEFT OUTER JOIN sys.all_sql_modules AS sm
                        ON sm.object_id = sp.object_id
            WHERE   sp.type = 'P'
                    AND (CAST(CASE WHEN sp.is_ms_shipped = 1 THEN 1
                                   WHEN (
                                         SELECT major_id
                                         FROM   sys.extended_properties
                                         WHERE  major_id = sp.object_id
                                                AND minor_id = 0
                                                AND class = 1
                                                AND name = N'microsoft_database_tools_support'
                                        ) IS NOT NULL THEN 1
                                   ELSE 0
                              END AS BIT) = 0))

UNION ALL
SELECT  R.SPECIFIC_SCHEMA,
        R.SPECIFIC_NAME,
        R.ROUTINE_TYPE,
        P.ORDINAL_POSITION,
        P.PARAMETER_MODE,
        P.PARAMETER_NAME,
        P.DATA_TYPE,
        ISNULL(P.CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,
        ISNULL(P.NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,
        ISNULL(P.NUMERIC_SCALE, 0) AS NUMERIC_SCALE,
        ISNULL(P.DATETIME_PRECISION, 0) AS DATETIME_PRECISION,
        P.USER_DEFINED_TYPE_SCHEMA + '.' + P.USER_DEFINED_TYPE_NAME AS USER_DEFINED_TYPE
FROM    INFORMATION_SCHEMA.ROUTINES R
        LEFT OUTER JOIN INFORMATION_SCHEMA.PARAMETERS P
            ON P.SPECIFIC_SCHEMA = R.SPECIFIC_SCHEMA
               AND P.SPECIFIC_NAME = R.SPECIFIC_NAME
WHERE   R.ROUTINE_TYPE = 'FUNCTION'
        AND R.DATA_TYPE = 'TABLE'";

            private const string StoredProcedureSQLAzure = @"
SELECT  R.SPECIFIC_SCHEMA,
        R.SPECIFIC_NAME,
        R.ROUTINE_TYPE,
        P.ORDINAL_POSITION,
        P.PARAMETER_MODE,
        P.PARAMETER_NAME,
        P.DATA_TYPE,
        ISNULL(P.CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,
        ISNULL(P.NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,
        ISNULL(P.NUMERIC_SCALE, 0) AS NUMERIC_SCALE,
        ISNULL(P.DATETIME_PRECISION, 0) AS DATETIME_PRECISION,
        P.USER_DEFINED_TYPE_SCHEMA + '.' + P.USER_DEFINED_TYPE_NAME AS USER_DEFINED_TYPE
FROM    INFORMATION_SCHEMA.ROUTINES R
        LEFT OUTER JOIN INFORMATION_SCHEMA.PARAMETERS P
            ON P.SPECIFIC_SCHEMA = R.SPECIFIC_SCHEMA
               AND P.SPECIFIC_NAME = R.SPECIFIC_NAME
WHERE   R.ROUTINE_TYPE = 'PROCEDURE'
        AND (
             P.IS_RESULT = 'NO'
             OR P.IS_RESULT IS NULL
            )
        AND R.SPECIFIC_SCHEMA + R.SPECIFIC_NAME IN (
            SELECT  SCHEMA_NAME(sp.schema_id) + sp.name
            FROM    sys.all_objects AS sp
                    LEFT OUTER JOIN sys.all_sql_modules AS sm
                        ON sm.object_id = sp.object_id
            WHERE   sp.type = 'P'
                    AND sp.is_ms_shipped = 0)
UNION ALL
SELECT  R.SPECIFIC_SCHEMA,
        R.SPECIFIC_NAME,
        R.ROUTINE_TYPE,
        P.ORDINAL_POSITION,
        P.PARAMETER_MODE,
        P.PARAMETER_NAME,
        P.DATA_TYPE,
        ISNULL(P.CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,
        ISNULL(P.NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,
        ISNULL(P.NUMERIC_SCALE, 0) AS NUMERIC_SCALE,
        ISNULL(P.DATETIME_PRECISION, 0) AS DATETIME_PRECISION,
        P.USER_DEFINED_TYPE_SCHEMA + '.' + P.USER_DEFINED_TYPE_NAME AS USER_DEFINED_TYPE
FROM    INFORMATION_SCHEMA.ROUTINES R
        LEFT OUTER JOIN INFORMATION_SCHEMA.PARAMETERS P
            ON P.SPECIFIC_SCHEMA = R.SPECIFIC_SCHEMA
               AND P.SPECIFIC_NAME = R.SPECIFIC_NAME
WHERE   R.ROUTINE_TYPE = 'FUNCTION'
        AND R.DATA_TYPE = 'TABLE'
ORDER BY R.SPECIFIC_SCHEMA,
        R.SPECIFIC_NAME,
        P.ORDINAL_POSITION";

            private const string SynonymStoredProcedureSQLSetup = @"
SET NOCOUNT ON;
IF OBJECT_ID('tempdb..#SynonymStoredProcedureDetails') IS NOT NULL DROP TABLE #SynonymStoredProcedureDetails;
IF OBJECT_ID('tempdb..#SynonymTargets') IS NOT NULL DROP TABLE #SynonymTargets;

-- Create the ##SynonymStoredProcedureDetails temp table structure for later use
SELECT  TOP (0) R.SPECIFIC_SCHEMA,
        R.SPECIFIC_NAME,
        R.ROUTINE_TYPE,
        P.ORDINAL_POSITION,
        P.PARAMETER_MODE,
        P.PARAMETER_NAME,
        P.DATA_TYPE,
        ISNULL(P.CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,
        ISNULL(P.NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,
        ISNULL(P.NUMERIC_SCALE, 0) AS NUMERIC_SCALE,
        ISNULL(P.DATETIME_PRECISION, 0) AS DATETIME_PRECISION,
        P.USER_DEFINED_TYPE_SCHEMA + '.' + P.USER_DEFINED_TYPE_NAME AS USER_DEFINED_TYPE
INTO    #SynonymStoredProcedureDetails
FROM    INFORMATION_SCHEMA.ROUTINES R
        LEFT OUTER JOIN INFORMATION_SCHEMA.PARAMETERS P
            ON P.SPECIFIC_SCHEMA = R.SPECIFIC_SCHEMA
               AND P.SPECIFIC_NAME = R.SPECIFIC_NAME
WHERE   R.ROUTINE_TYPE = 'PROCEDURE'
        AND (
             P.IS_RESULT = 'NO'
             OR P.IS_RESULT IS NULL
            )
        AND R.SPECIFIC_SCHEMA + R.SPECIFIC_NAME IN (
            SELECT  SCHEMA_NAME(sp.schema_id) + sp.name
            FROM    sys.all_objects AS sp
                    LEFT OUTER JOIN sys.all_sql_modules AS sm
                        ON sm.object_id = sp.object_id
            WHERE   sp.type = 'P'
                    AND (CAST(CASE WHEN sp.is_ms_shipped = 1 THEN 1
                                   WHEN (
                                         SELECT major_id
                                         FROM   sys.extended_properties
                                         WHERE  major_id = sp.object_id
                                                AND minor_id = 0
                                                AND class = 1
                                                AND name = N'microsoft_database_tools_support'
                                        ) IS NOT NULL THEN 1
                                   ELSE 0
                              END AS BIT) = 0))

-- Get all databases referenced by synonyms.
SELECT DISTINCT PARSENAME(sn.base_object_name, 3) AS DatabaseName
INTO #SynonymTargets
FROM sys.synonyms sn
WHERE PARSENAME(sn.base_object_name, 3) != DB_NAME()
AND ISNULL(PARSENAME(sn.base_object_name, 4), @@SERVERNAME) = @@SERVERNAME -- Only populate info from current server
ORDER BY PARSENAME(sn.base_object_name, 3)

-- Create a query to execute for each referenced database
DECLARE @synonymStoredProcedureDetailsQuery nvarchar(max) =
'
INSERT INTO #SynonymStoredProcedureDetails (SPECIFIC_SCHEMA, SPECIFIC_NAME, ROUTINE_TYPE, ORDINAL_POSITION, PARAMETER_MODE, PARAMETER_NAME, DATA_TYPE
                                            , CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, DATETIME_PRECISION, USER_DEFINED_TYPE)
SELECT  R.SPECIFIC_SCHEMA,
        R.SPECIFIC_NAME,
        R.ROUTINE_TYPE,
        P.ORDINAL_POSITION,
        P.PARAMETER_MODE,
        P.PARAMETER_NAME,
        P.DATA_TYPE,
        ISNULL(P.CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,
        ISNULL(P.NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,
        ISNULL(P.NUMERIC_SCALE, 0) AS NUMERIC_SCALE,
        ISNULL(P.DATETIME_PRECISION, 0) AS DATETIME_PRECISION,
        P.USER_DEFINED_TYPE_SCHEMA + ''.'' + P.USER_DEFINED_TYPE_NAME AS USER_DEFINED_TYPE
FROM    INFORMATION_SCHEMA.ROUTINES R
        LEFT OUTER JOIN INFORMATION_SCHEMA.PARAMETERS P
            ON P.SPECIFIC_SCHEMA = R.SPECIFIC_SCHEMA
               AND P.SPECIFIC_NAME = R.SPECIFIC_NAME
WHERE   R.ROUTINE_TYPE = ''PROCEDURE''
        AND (
             P.IS_RESULT = ''NO''
             OR P.IS_RESULT IS NULL
            )
        AND R.SPECIFIC_SCHEMA + R.SPECIFIC_NAME IN (
            SELECT  SCHEMA_NAME(sp.schema_id) + sp.name
            FROM    sys.all_objects AS sp
                    LEFT OUTER JOIN sys.all_sql_modules AS sm
                        ON sm.object_id = sp.object_id
            WHERE   sp.type = ''P''
                    AND (CAST(CASE WHEN sp.is_ms_shipped = 1 THEN 1
                                   WHEN (
                                         SELECT major_id
                                         FROM   sys.extended_properties
                                         WHERE  major_id = sp.object_id
                                                AND minor_id = 0
                                                AND class = 1
                                                AND name = N''microsoft_database_tools_support''
                                        ) IS NOT NULL THEN 1
                                   ELSE 0
                              END AS BIT) = 0))

UNION ALL
SELECT  R.SPECIFIC_SCHEMA,
        R.SPECIFIC_NAME,
        R.ROUTINE_TYPE,
        P.ORDINAL_POSITION,
        P.PARAMETER_MODE,
        P.PARAMETER_NAME,
        P.DATA_TYPE,
        ISNULL(P.CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,
        ISNULL(P.NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,
        ISNULL(P.NUMERIC_SCALE, 0) AS NUMERIC_SCALE,
        ISNULL(P.DATETIME_PRECISION, 0) AS DATETIME_PRECISION,
        P.USER_DEFINED_TYPE_SCHEMA + ''.'' + P.USER_DEFINED_TYPE_NAME AS USER_DEFINED_TYPE
FROM    INFORMATION_SCHEMA.ROUTINES R
        LEFT OUTER JOIN INFORMATION_SCHEMA.PARAMETERS P
            ON P.SPECIFIC_SCHEMA = R.SPECIFIC_SCHEMA
               AND P.SPECIFIC_NAME = R.SPECIFIC_NAME
WHERE   R.ROUTINE_TYPE = ''FUNCTION''
        AND R.DATA_TYPE = ''TABLE''
ORDER BY R.SPECIFIC_SCHEMA,
        R.SPECIFIC_NAME,
        P.ORDINAL_POSITION
'

-- Loop through referenced databases and populate #SynonymStoredProcedureDetails
DECLARE @synonmymDatabaseName sysname = (SELECT TOP (1) DatabaseName FROM #SynonymTargets)
DECLARE @synonymStoredProcedureDetailsQueryWithDb nvarchar(max)
WHILE (@synonmymDatabaseName IS NOT NULL)
BEGIN
    SET @synonymStoredProcedureDetailsQueryWithDb = 'USE [' + @synonmymDatabaseName + '] ' + @synonymStoredProcedureDetailsQuery
    EXEC sp_executesql @stmt=@synonymStoredProcedureDetailsQueryWithDb
    DELETE FROM #SynonymTargets WHERE DatabaseName = @synonmymDatabaseName
    SET @synonmymDatabaseName = (SELECT TOP (1) DatabaseName FROM #SynonymTargets)
END

SET NOCOUNT OFF;
";

            private const string SynonymStoredProcedureSQL = @"
UNION
-- Synonyms
SELECT SPECIFIC_SCHEMA, SPECIFIC_NAME, ROUTINE_TYPE, ORDINAL_POSITION, PARAMETER_MODE, PARAMETER_NAME, DATA_TYPE
    , CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, DATETIME_PRECISION, USER_DEFINED_TYPE FROM #SynonymStoredProcedureDetails";

            private const string IndexSQL = @"
SELECT  SCHEMA_NAME(t.schema_id) AS TableSchema,
    t.name AS TableName,
    ind.name AS IndexName,
    ic.key_ordinal AS KeyOrdinal,
    col.name AS ColumnName,
    ind.is_unique AS IsUnique,
    ind.is_primary_key AS IsPrimaryKey,
    ind.is_unique_constraint AS IsUniqueConstraint,
    CASE WHEN ind.[type] = 1 AND ind.is_primary_key = 1 THEN 1 ELSE 0 END AS IsClustered,
    (
        SELECT COUNT(1)
        FROM   sys.index_columns i
        WHERE  i.object_id = ind.object_id
            AND i.index_id = ind.index_id
    ) AS ColumnCount
FROM    sys.tables t
    INNER JOIN sys.indexes ind
        ON ind.object_id = t.object_id
    INNER JOIN sys.index_columns ic
        ON ind.object_id = ic.object_id
            AND ind.index_id = ic.index_id
    INNER JOIN sys.columns col
        ON ic.object_id = col.object_id
            AND ic.column_id = col.column_id
WHERE   t.is_ms_shipped = 0
    AND ind.ignore_dup_key = 0
    AND ic.key_ordinal > 0
    AND t.name NOT LIKE 'sysdiagram%'";

            public SqlServerSchemaReader(DbConnection connection, DbProviderFactory factory)
                : base(connection, factory)
            {
            }

            private static string IncludeQueryTraceOn9481()
            {
                if (Settings.IncludeQueryTraceOn9481Flag)
                    return @"
OPTION (QUERYTRACEON 9481)";
                return string.Empty;
            }

            private void ReadDatabaseEdition()
            {
                if (Settings.IsSqlCe || !string.IsNullOrEmpty(_sqlDatabaseEdition))
                    return;

                if (Cmd == null)
                    return;

                Cmd.CommandText = @"
SELECT  SERVERPROPERTY('Edition') AS Edition,
        CASE SERVERPROPERTY('EngineEdition')
          WHEN 1 THEN 'Personal'
          WHEN 2 THEN 'Standard'
          WHEN 3 THEN 'Enterprise'
          WHEN 4 THEN 'Express'
          WHEN 5 THEN 'Azure'
          ELSE 'Unknown'
        END AS EngineEdition,
        SERVERPROPERTY('productversion') AS ProductVersion;";

                Cmd.CommandTimeout = Settings.CommandTimeout;

                using (DbDataReader rdr = Cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        _sqlDatabaseEdition = rdr["Edition"].ToString();
                        _sqlDatabaseEngineEdition = rdr["EngineEdition"].ToString();
                        _sqlDatabaseProductVersion = rdr["ProductVersion"].ToString();
                        _sqlDatabaseProductMajorVersion = int.Parse(_sqlDatabaseProductVersion.Substring(0, 2).Replace(".", string.Empty));
                    }
                }
            }

            private void WriteConnectionSettingComments()
            {
                if (Settings.IncludeConnectionSettingComments)
                {
                    if (Settings.IsSqlCe)
                    {
                        WriteLine("// Database Edition : SqlCE");
                    }
                    else
                    {
                        WriteLine("// Database Edition        : " + _sqlDatabaseEdition);
                        WriteLine("// Database Engine Edition : " + _sqlDatabaseEngineEdition);
                        WriteLine("// Database Version        : " + _sqlDatabaseProductVersion);
                    }
                    WriteLine("");
                }
            }

            private bool IsAzure()
            {
                return _sqlDatabaseEngineEdition == "Azure";
            }

            private String GetReadSchemaSql()
            {
                if (Settings.IsSqlCe)
                {
                    return TableSQLCE;
                }

                String sql;
                if (Settings.IncludeSynonyms)
                {
                    sql = SynonymTableSQLSetup + TableSQL + SynonymTableSQL + IncludeQueryTraceOn9481();
                }
                else
                {
                    sql = TableSQL + IncludeQueryTraceOn9481();
                }

                ReadDatabaseEdition();
                var temporalTableSupport = _sqlDatabaseProductMajorVersion >= 13;
                if (!temporalTableSupport)
                {
                    // Replace the column names (only present in SQL Server 2016 or later) with literal constants so the query works with older versions of SQL Server.
                    sql = sql
                        .Replace("[sc].[generated_always_type]", "0")
                        .Replace("[c].[generated_always_type]", "0")
                        .Replace("[st].[temporal_type]", "0");
                }

                return sql;
            }

            public override Tables ReadSchema()
            {
                ReadDatabaseEdition();
                WriteConnectionSettingComments();

                var result = new Tables();
                if (Cmd == null)
                    return result;

                Cmd.CommandText = this.GetReadSchemaSql();

                if (!Settings.IsSqlCe) Cmd.CommandTimeout = Settings.CommandTimeout;

                using (var rdr = Cmd.ExecuteReader())
                {
                    var rxClean = new Regex("^(event|Equals|GetHashCode|GetType|ToString|repo|Save|IsNew|Insert|Update|Delete|Exists|SingleOrDefault|Single|First|FirstOrDefault|Fetch|Page|Query)$");
                    var lastTable = string.Empty;
                    Table table = null;
                    while (rdr.Read())
                    {
                        string schema = rdr["SchemaName"].ToString().Trim();
                        if (IsFilterExcluded(Settings.SchemaFilterExclude, Settings.SchemaFilterInclude, schema))
                            continue;

                        string tableName = rdr["TableName"].ToString().Trim();
                        if (IsFilterExcluded(Settings.TableFilterExclude, Settings.TableFilterInclude, tableName))
                            continue;

                        if (lastTable != tableName || table == null)
                        {
                            // The data from the database is not sorted
                            table = result.Find(x => x.Name == tableName && x.Schema == schema);
                            if (table == null)
                            {
                                String tableType = ((String)rdr["TableType"]).Trim();
                                TableTemporalType tableTemporalType = (TableTemporalType)(Byte)rdr["TableTemporalType"];

                                table = new Table
                                {
                                    Name = tableName,
                                    Schema = schema,
                                    IsView = string.Compare(tableType, "View", StringComparison.OrdinalIgnoreCase) == 0,
                                    TemporalType = tableTemporalType,

                                    // Will be set later
                                    HasForeignKey = false,
                                    HasNullableColumns = false
                                };

                                if (!Settings.IncludeViews && table.IsView)
                                    continue;

                                tableName = Settings.TableRename(tableName, schema, table.IsView);
                                if (IsFilterExcluded(Settings.TableFilterExclude, null, tableName)) // Retest exclusion filter after table rename
                                    continue;

                                // Handle table names with underscores - singularise just the last word
                                table.ClassName = Inflector.MakeSingular(CleanUp(tableName));
                                var titleCase = (Settings.UsePascalCase ? Inflector.ToTitleCase(table.ClassName) : table.ClassName).Replace(" ", "").Replace("$", "").Replace(".", "");
                                table.NameHumanCase = titleCase;

                                if (Settings.PrependSchemaName && string.Compare(table.Schema, "dbo", StringComparison.OrdinalIgnoreCase) != 0)
                                    table.NameHumanCase = table.Schema + "_" + table.NameHumanCase;

                                // Check for table or C# name clashes
                                if (ReservedKeywords.Contains(table.NameHumanCase) ||
                                    (Settings.UsePascalCase && result.Find(x => x.NameHumanCase == table.NameHumanCase) != null))
                                {
                                    table.NameHumanCase += "1";
                                }

                                if (!Settings.TableFilter(table))
                                    continue;

                                result.Add(table);
                            }
                        }

                        var col = CreateColumn(rdr, rxClean, table, Settings.ColumnFilterExclude);
                        if (col != null)
                            table.Columns.Add(col);
                    }
                }
                // Check for property name clashes in columns
                foreach (Column c in result.SelectMany(tbl => tbl.Columns.Where(c => tbl.Columns.FindAll(x => x.NameHumanCase == c.NameHumanCase).Count > 1)))
                {
                    int n = 1;
                    var original = c.NameHumanCase;
                    c.NameHumanCase = original + n++;

                    // Check if the above resolved the name clash, if not, use next value
                    while (c.ParentTable.Columns.Count(c2 => c2.NameHumanCase == c.NameHumanCase) > 1)
                        c.NameHumanCase = original + n++;
                }

                if (Settings.IncludeExtendedPropertyComments != CommentsStyle.None)
                    ReadExtendedProperties(result, Settings.IncludeExtendedPropertyComments == CommentsStyle.InSummaryBlock);

                ReadIndexes(result);

                foreach (Table tbl in result)
                {
                    if (tbl.IsView && Settings.ViewProcessing != null) Settings.ViewProcessing(tbl);
                    tbl.SetPrimaryKeys();
                    foreach (Column c in tbl.Columns)
                        Settings.UpdateColumn(c, tbl);
                    tbl.Columns.ForEach(x => x.SetupEntityAndConfig());
                }

                return result;
            }

            public override List<ForeignKey> ReadForeignKeys()
            {
                var fkList = new List<ForeignKey>();
                if (Cmd == null)
                    return fkList;

                Cmd.CommandText = ForeignKeySQL + IncludeQueryTraceOn9481();

                if (Settings.IncludeSynonyms)
                    Cmd.CommandText = SynonymForeignKeySQLSetup + ForeignKeySQL + SynonymForeignKeySQL + IncludeQueryTraceOn9481();

                if (Cmd.GetType().Name == "SqlCeCommand")
                    Cmd.CommandText = ForeignKeySQLCE;
                else
                    Cmd.CommandTimeout = Settings.CommandTimeout;

                using (DbDataReader rdr = Cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string fkTableName = rdr["FK_Table"].ToString();
                        string fkSchema = rdr["fkSchema"].ToString();
                        string pkTableName = rdr["PK_Table"].ToString();
                        string pkSchema = rdr["pkSchema"].ToString();
                        string fkColumn = rdr["FK_Column"].ToString();
                        string pkColumn = rdr["PK_Column"].ToString();
                        string constraintName = rdr["Constraint_Name"].ToString();
                        int ordinal = (int)rdr["ORDINAL_POSITION"];
                        bool cascadeOnDelete = ((int)rdr["CascadeOnDelete"]) == 1;
                        bool isNotEnforced = (bool)rdr["IsNotEnforced"];

                        string pkTableNameFiltered = Settings.TableRename(pkTableName, pkSchema, false);

                        var fk = new ForeignKey(fkTableName, fkSchema, pkTableName, pkSchema, fkColumn, pkColumn, constraintName, pkTableNameFiltered, ordinal, cascadeOnDelete, isNotEnforced);

                        var filteredFk = Settings.ForeignKeyFilter(fk);
                        if (filteredFk != null)
                            fkList.Add(filteredFk);
                    }
                }

                return fkList;
            }

            // When a table has no primary keys, all the NOT NULL columns are set as being the primary key.
            // This function reads the unique indexes for a table, and correctly sets the columns being used as primary keys.
            public override void ReadIndexes(Tables tables)
            {
                if (Cmd == null)
                    return;

                if (Cmd.GetType().Name == "SqlCeCommand")
                    return;

                Cmd.CommandText = IndexSQL + IncludeQueryTraceOn9481();
                Cmd.CommandTimeout = Settings.CommandTimeout;

                var list = new List<Index>();
                using (DbDataReader rdr = Cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var index = new Index
                        {
                            Schema = rdr["TableSchema"].ToString().Trim(),
                            TableName = rdr["TableName"].ToString().Trim(),
                            IndexName = rdr["IndexName"].ToString().Trim(),
                            KeyOrdinal = (byte)rdr["KeyOrdinal"],
                            ColumnName = rdr["ColumnName"].ToString().Trim(),
                            ColumnCount = (int)rdr["ColumnCount"],
                            IsUnique = (bool)rdr["IsUnique"],
                            IsPrimaryKey = (bool)rdr["IsPrimaryKey"],
                            IsUniqueConstraint = (bool)rdr["IsUniqueConstraint"],
                            IsClustered = ((int)rdr["IsClustered"]) == 1
                        };

                        list.Add(index);
                    }
                }

                Table t = null;
                var indexTables = list
                    .Select(x => new { x.Schema, x.TableName })
                    .Distinct();

                foreach (var indexTable in indexTables)
                {
                    // Lookup table
                    if (t == null || t.Name != indexTable.TableName || t.Schema != indexTable.Schema)
                        t = tables.Find(x => x.Name == indexTable.TableName && x.Schema == indexTable.Schema);

                    if (t == null)
                        continue;

                    // Find indexes for table
                    var indexes =
                        list.Where(x => x.Schema == indexTable.Schema && x.TableName == indexTable.TableName)
                            .OrderBy(o => o.ColumnCount)
                            .ThenBy(o => o.KeyOrdinal)
                            .ToList();

                    // Set index on column
                    foreach (var index in indexes)
                    {
                        var col = t.Columns.Find(x => x.Name == index.ColumnName);
                        if (col == null)
                            continue;

                        col.Indexes.Add(index);

                        col.IsPrimaryKey = col.IsPrimaryKey || index.IsPrimaryKey;
                        col.IsUniqueConstraint = col.IsUniqueConstraint || (index.IsUniqueConstraint && index.ColumnCount == 1);
                        col.IsUnique = col.IsUnique || (index.IsUnique && index.ColumnCount == 1);
                    }

                    // Check if table has any primary keys
                    if (t.PrimaryKeys.Any())
                        continue; // Already has a primary key, ignore this unique index / constraint

                    // Find unique indexes for table
                    var uniqueIndexKeys = indexes
                        .Where(x => x.IsUnique || x.IsPrimaryKey || x.IsUniqueConstraint)
                        .OrderBy(o => o.ColumnCount)
                        .ThenBy(o => o.KeyOrdinal);

                    // Process only the first index with the lowest unique column count
                    string indexName = null;
                    foreach (var key in uniqueIndexKeys)
                    {
                        if (indexName == null)
                            indexName = key.IndexName;

                        if (indexName != key.IndexName)
                            break; // First unique index with lowest column count has been processed, exit.

                        var col = t.Columns.Find(x => x.Name == key.ColumnName);
                        if (col != null && !col.IsNullable && !col.Hidden && !col.IsPrimaryKey)
                        {
                            col.IsPrimaryKey = true;
                            col.IsUniqueConstraint = true;
                            col.IsUnique = true;
                            col.UniqueIndexName = indexName;
                        }
                    }
                }
            }

            public override void ReadExtendedProperties(Tables tables, bool commentsInSummaryBlock)
            {
                if (Cmd == null)
                    return;

                if (Cmd.GetType().Name == "SqlCeCommand")
                {
                    Cmd.CommandText = ExtendedPropertyTableExistsSQLCE;
                    var obj = Cmd.ExecuteScalar();
                    if (obj == null)
                        return;

                    Cmd.CommandText = ExtendedPropertySQLCE;
                }
                else
                {
                    if (IsAzure())
                        return;

                    Cmd.CommandText = ExtendedPropertySQL + IncludeQueryTraceOn9481();
                    Cmd.CommandTimeout = Settings.CommandTimeout;
                }

                using (DbDataReader rdr = Cmd.ExecuteReader())
                {
                    Table t = null;
                    while (rdr.Read())
                    {
                        string schema = rdr["schema"].ToString().Trim();
                        string tableName = rdr["table"].ToString().Trim();
                        string column = rdr["column"].ToString().Trim();
                        string extendedProperty = rdr["property"].ToString().Trim();

                        if (string.IsNullOrEmpty(extendedProperty))
                            continue;

                        if (t == null || t.Name != tableName || t.Schema != schema)
                            t = tables.Find(x => x.Name == tableName && x.Schema == schema);

                        if (t != null)
                        {
                            if (string.IsNullOrEmpty(column))
                            {
                                // Table level extended comment
                                t.ExtendedProperty = Regex.Replace(extendedProperty, "[\r\n]+", "\r\n    /// ");
                            }
                            else
                            {
                                var col = t.Columns.Find(x => x.Name == column);
                                if (col != null)
                                {
                                    // Column level extended comment
                                    if (commentsInSummaryBlock)
                                        col.ExtendedProperty = Regex.Replace(extendedProperty, "[\r\n]+", "\r\n        /// ");
                                    else
                                        col.ExtendedProperty = Regex.Replace(Regex.Replace(extendedProperty, "[\r\n]+", " "), "\\s+", " ");
                                }
                            }
                        }
                    }
                }
            }

            public override List<StoredProcedure> ReadStoredProcs()
            {
                var result = new List<StoredProcedure>();
                if (Cmd == null)
                    return result;

                if (Cmd.GetType().Name == "SqlCeCommand")
                    return result;

                if (IsAzure())
                    Cmd.CommandText = StoredProcedureSQLAzure + IncludeQueryTraceOn9481();
                else if (Settings.IncludeSynonyms)
                    Cmd.CommandText = SynonymStoredProcedureSQLSetup + StoredProcedureSQL + SynonymStoredProcedureSQL + IncludeQueryTraceOn9481();
                else
                    Cmd.CommandText = StoredProcedureSQL + IncludeQueryTraceOn9481();

                Cmd.CommandTimeout = Settings.CommandTimeout;

                using (DbDataReader rdr = Cmd.ExecuteReader())
                {
                    var lastSp = string.Empty;
                    StoredProcedure sp = null;
                    while (rdr.Read())
                    {
                        var spType = rdr["ROUTINE_TYPE"].ToString().Trim().ToUpper();
                        var isTVF = (spType == "FUNCTION");
                        if (isTVF && !Settings.IncludeTableValuedFunctions)
                            continue;

                        string schema = rdr["SPECIFIC_SCHEMA"].ToString().Trim();
                        if (Settings.SchemaFilterExclude != null && Settings.SchemaFilterExclude.IsMatch(schema))
                            continue;

                        string spName = rdr["SPECIFIC_NAME"].ToString().Trim();
                        var fullname = schema + "." + spName;
                        if (Settings.StoredProcedureFilterExclude != null && (Settings.StoredProcedureFilterExclude.IsMatch(spName) || Settings.StoredProcedureFilterExclude.IsMatch(fullname)))
                            continue;

                        if (lastSp != fullname || sp == null)
                        {
                            lastSp = fullname;
                            sp = new StoredProcedure
                            {
                                IsTVF = isTVF,
                                Name = spName,
                                NameHumanCase = (Settings.UsePascalCase ? Inflector.ToTitleCase(spName) : spName).Replace(" ", "").Replace("$", ""),
                                Schema = schema
                            };
                            sp.NameHumanCase = CleanUp(sp.NameHumanCase);
                            if ((string.Compare(schema, "dbo", StringComparison.OrdinalIgnoreCase) != 0) && Settings.PrependSchemaName)
                                sp.NameHumanCase = schema + "_" + sp.NameHumanCase;

                            sp.NameHumanCase = Settings.StoredProcedureRename(sp);
                            if (Settings.StoredProcedureFilterExclude != null && (Settings.StoredProcedureFilterExclude.IsMatch(sp.NameHumanCase) || Settings.StoredProcedureFilterExclude.IsMatch(schema + "." + sp.NameHumanCase)))
                                continue;

                            result.Add(sp);
                        }

                        if (rdr["DATA_TYPE"] != null && rdr["DATA_TYPE"] != DBNull.Value)
                        {
                            var typename = rdr["DATA_TYPE"].ToString().Trim().ToLower();
                            var scale = (int)rdr["NUMERIC_SCALE"];
                            var precision = (int)((byte)rdr["NUMERIC_PRECISION"]);
                            var parameterMode = rdr["PARAMETER_MODE"].ToString().Trim().ToUpper();

                            var param = new StoredProcedureParameter
                            {
                                Ordinal = (int)rdr["ORDINAL_POSITION"],
                                Mode = parameterMode == "IN" ? StoredProcedureParameterMode.In : StoredProcedureParameterMode.InOut,
                                Name = rdr["PARAMETER_NAME"].ToString().Trim(),
                                SqlDbType = GetSqlDbType(typename),
                                PropertyType = GetPropertyType(typename),
                                DateTimePrecision = (short)rdr["DATETIME_PRECISION"],
                                MaxLength = (int)rdr["CHARACTER_MAXIMUM_LENGTH"],
                                Precision = precision,
                                Scale = scale,
                                UserDefinedTypeName = rdr["USER_DEFINED_TYPE"].ToString().Trim()
                            };

                            var clean = CleanUp(param.Name.Replace("@", ""));
                            if (!string.IsNullOrEmpty(clean))
                            {
                                param.NameHumanCase = Inflector.MakeInitialLower((Settings.UsePascalCase ? Inflector.ToTitleCase(clean) : clean).Replace(" ", ""));

                                if (ReservedKeywords.Contains(param.NameHumanCase))
                                    param.NameHumanCase = "@" + param.NameHumanCase;

                                sp.Parameters.Add(param);
                            }
                        }
                    }
                }
                return result;
            }

            public void ReadStoredProcReturnObject(SqlConnection sqlConnection, StoredProcedure proc)
            {
                try
                {
                    const string structured = "Structured";
                    var sb = new StringBuilder();
                    sb.AppendLine();
                    sb.AppendLine("SET FMTONLY OFF; SET FMTONLY ON;");
                    if (proc.IsTVF)
                    {
                        foreach (var param in proc.Parameters.Where(x => x.SqlDbType.Equals(structured, StringComparison.InvariantCultureIgnoreCase)))
                            sb.AppendLine(string.Format("DECLARE {0} {1};", param.Name, param.UserDefinedTypeName));

                        sb.Append(string.Format("SELECT * FROM [{0}].[{1}](", proc.Schema, proc.Name));
                        foreach (var param in proc.Parameters)
                            sb.Append(string.Format("{0}, ", param.SqlDbType.Equals(structured, StringComparison.InvariantCultureIgnoreCase) ? param.Name : "default"));

                        if (proc.Parameters.Count > 0)
                            sb.Length -= 2;

                        sb.AppendLine(");");
                    }
                    else
                    {
                        foreach (var param in proc.Parameters)
                            sb.AppendLine(string.Format("DECLARE {0} {1};", param.Name, param.SqlDbType.Equals(structured, StringComparison.InvariantCultureIgnoreCase) ? param.UserDefinedTypeName : param.SqlDbType));

                        sb.Append(string.Format("exec [{0}].[{1}] ", proc.Schema, proc.Name));
                        foreach (var param in proc.Parameters)
                            sb.Append(string.Format("{0}, ", param.Name));

                        if (proc.Parameters.Count > 0)
                            sb.Length -= 2;

                        sb.AppendLine(";");
                    }
                    sb.AppendLine("SET FMTONLY OFF; SET FMTONLY OFF;");

                    var ds = new DataSet();
                    using (var sqlAdapter = new SqlDataAdapter(sb.ToString(), sqlConnection))
                    {
                        if (sqlConnection.State != ConnectionState.Open)
                            sqlConnection.Open();
                        sqlAdapter.SelectCommand.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo);
                        sqlConnection.Close();
                        sqlAdapter.FillSchema(ds, SchemaType.Source, "MyTable");
                    }

                    // Tidy up parameters
                    foreach (var p in proc.Parameters)
                        p.NameHumanCase = Regex.Replace(p.NameHumanCase, @"[^A-Za-z0-9@\s]*", "");

                    for (var count = 0; count < ds.Tables.Count; count++)
                    {
                        proc.ReturnModels.Add(ds.Tables[count].Columns.Cast<DataColumn>().ToList());
                    }
                }
                catch (Exception)
                {
                    // Stored procedure does not have a return type
                }
            }

            public override void ProcessForeignKeys(List<ForeignKey> fkList, Tables tables, bool checkForFkNameClashes)
            {
                var constraints = fkList.Select(x => x.FkSchema + "." + x.ConstraintName).Distinct();
                foreach (var constraint in constraints)
                {
                    var foreignKeys = fkList
                        .Where(x => string.Format("{0}.{1}", x.FkSchema, x.ConstraintName).Equals(constraint, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();

                    var foreignKey = foreignKeys.First();
                    Table fkTable = tables.GetTable(foreignKey.FkTableName, foreignKey.FkSchema);
                    if (fkTable == null || fkTable.IsMapping || !fkTable.HasForeignKey)
                        continue;

                    Table pkTable = tables.GetTable(foreignKey.PkTableName, foreignKey.PkSchema);
                    if (pkTable == null || pkTable.IsMapping)
                        continue;

                    var fkCols = foreignKeys.Select(x => new
                    {
                        fk = x,
                        col = fkTable.Columns.Find(n => string.Equals(n.Name, x.FkColumn, StringComparison.InvariantCultureIgnoreCase))
                    })
                        .Where(x => x.col != null)
                        .OrderBy(o => o.fk.Ordinal)
                        .ToList();

                    if (!fkCols.Any())
                        continue;

                    //if(EF6)
                    {
                        // Check FK has same number of columns as the primary key it points to
                        var pks = pkTable.PrimaryKeys.OrderBy(x => x.PropertyType).ThenBy(y => y.Name).ToArray();
                        var cols = fkCols.Select(x => x.col).OrderBy(x => x.PropertyType).ThenBy(y => y.Name).ToArray();
                        if (pks.Length != cols.Length)
                            continue;

                        // EF6 - Cannot have a FK to a non-primary key
                        if (pks.Where((pk, ef6Check) => pk.PropertyType != cols[ef6Check].PropertyType).Any())
                            continue;
                    }

                    var pkCols = foreignKeys.Select(x => pkTable.Columns.Find(n => string.Equals(n.Name, x.PkColumn, StringComparison.InvariantCultureIgnoreCase)))
                                            .Where(x => x != null)
                                            .OrderBy(o => o.Ordinal)
                                            .ToList();

                    if (!pkCols.Any())
                        continue;

                    // EF6 - Cannot have a FK to a non-primary key
                    if (!pkCols.All(c => c.IsPrimaryKey))
                        continue;

                    var relationship = CalcRelationship(pkTable, fkTable, fkCols.Select(c => c.col).ToList(), pkCols);
                    if (relationship == Relationship.DoNotUse)
                        continue;

                    if (fkCols.All(x => !x.col.IsNullable && !x.col.Hidden) && pkCols.All(x => x.IsPrimaryKey || x.IsUnique))
                    {
                        foreach (var fk in fkCols)
                            fk.fk.IncludeRequiredAttribute = true;
                    }

                    foreignKey = Settings.ForeignKeyProcessing(foreignKeys, fkTable, pkTable, fkCols.Any(x => x.col.IsNullable));

                    string pkTableHumanCaseWithSuffix = foreignKey.PkTableHumanCase(pkTable.Suffix);
                    string pkTableHumanCase = foreignKey.PkTableHumanCase(null);
                    string pkPropName = fkTable.GetUniqueColumnName(pkTableHumanCase, foreignKey, checkForFkNameClashes, true, Relationship.ManyToOne);
                    bool fkMakePropNameSingular = (relationship == Relationship.OneToOne);
                    string fkPropName = pkTable.GetUniqueColumnName(fkTable.NameHumanCase, foreignKey, checkForFkNameClashes, fkMakePropNameSingular, Relationship.OneToMany);

                    var dataAnnotation = string.Empty;
                    if (Settings.UseDataAnnotationsWithFluent && !Settings.UseDataAnnotations)
                    {
                        dataAnnotation = foreignKey.IncludeRequiredAttribute ? "[Required] " : string.Empty;
                    }
                    else if (Settings.UseDataAnnotations)
                    {
                        dataAnnotation = string.Format("[ForeignKey(\"{0}\"){1}] ",
                            string.Join(", ", fkCols.Select(x => x.col.NameHumanCase).Distinct().ToArray()),
                            foreignKey.IncludeRequiredAttribute ? ", Required" : string.Empty
                        );

                        if (!checkForFkNameClashes &&
                            relationship == Relationship.OneToOne &&
                            foreignKey.IncludeReverseNavigation &&
                            fkCols.All(x => x.col.IsPrimaryKey))
                        {
                            var principalEndAttribute = string.Format("ForeignKey(\"{0}\")", pkPropName);
                            foreach (var fk in fkCols)
                            {
                                if (!fk.col.DataAnnotations.Contains(principalEndAttribute))
                                    fk.col.DataAnnotations.Add(principalEndAttribute);
                            }
                        }
                    }

                    var fkd = new PropertyAndComments
                    {
                        AdditionalDataAnnotations = Settings.ForeignKeyAnnotationsProcessing(fkTable, pkTable, pkPropName, fkPropName),
                        Definition = string.Format("{0}public {1}{2} {3} {4}{5}", dataAnnotation,
                            Table.GetLazyLoadingMarker(),
                            pkTableHumanCaseWithSuffix,
                            pkPropName,
                            "{ get; set; }",
                            Settings.IncludeComments != CommentsStyle.None ? " // " + foreignKey.ConstraintName : string.Empty),
                        Comments = string.Format("Parent {0} pointed by [{1}].({2}) ({3})",
                            pkTableHumanCase,
                            fkTable.Name,
                            string.Join(", ", fkCols.Select(x => "[" + x.col.NameHumanCase + "]").Distinct().ToArray()),
                            foreignKey.ConstraintName)
                    };

                    var firstFKCol = fkCols.First();
                    firstFKCol.col.EntityFk.Add(fkd);

                    string manyToManyMapping, mapKey;
                    if (foreignKeys.Count > 1)
                    {
                        manyToManyMapping = string.Format("c => new {{ {0} }}", string.Join(", ", fkCols.Select(x => "c." + x.col.NameHumanCase).Distinct().ToArray()));
                        mapKey = string.Format("{0}", string.Join(",", fkCols.Select(x => "\"" + x.col.Name + "\"").Distinct().ToArray()));
                    }
                    else
                    {
                        manyToManyMapping = string.Format("c => c.{0}", firstFKCol.col.NameHumanCase);
                        mapKey = string.Format("\"{0}\"", firstFKCol.col.Name);
                    }

                    if (!Settings.UseDataAnnotations)
                    {
                        List<Column> fkCols2 = fkCols.Select(c => c.col).ToList();

                        string rel = GetRelationship(relationship, fkCols2, pkCols, pkPropName, fkPropName, manyToManyMapping, mapKey, foreignKey.CascadeOnDelete, foreignKey.IncludeReverseNavigation, foreignKey.IsNotEnforced);
                        string com = Settings.IncludeComments != CommentsStyle.None ? " // " + foreignKey.ConstraintName : string.Empty;
                        firstFKCol.col.ConfigFk.Add(string.Format("{0};{1}", rel, com));
                    }

                    if (foreignKey.IncludeReverseNavigation)
                    {
                        pkTable.AddReverseNavigation(relationship, pkTableHumanCase, fkTable, fkPropName, string.Format("{0}.{1}", fkTable.Name, foreignKey.ConstraintName), foreignKeys);
                    }
                }
            }

            public override void IdentifyForeignKeys(List<ForeignKey> fkList, Tables tables)
            {
                foreach (var foreignKey in fkList)
                {
                    Table fkTable = tables.GetTable(foreignKey.FkTableName, foreignKey.FkSchema);
                    if (fkTable == null)
                        continue;   // Could be filtered out

                    Table pkTable = tables.GetTable(foreignKey.PkTableName, foreignKey.PkSchema);
                    if (pkTable == null)
                        continue;   // Could be filtered out

                    Column fkCol = fkTable.Columns.Find(n => string.Equals(n.Name, foreignKey.FkColumn, StringComparison.InvariantCultureIgnoreCase));
                    if (fkCol == null)
                        continue;   // Could not find fk column

                    Column pkCol = pkTable.Columns.Find(n => string.Equals(n.Name, foreignKey.PkColumn, StringComparison.InvariantCultureIgnoreCase));
                    if (pkCol == null)
                        continue;   // Could not find pk column

                    fkTable.HasForeignKey = true;
                }
            }

            private static string GetRelationship(Relationship relationship, IList<Column> fkCols, IList<Column> pkCols, string pkPropName, string fkPropName, string manyToManyMapping, string mapKey, bool cascadeOnDelete, bool includeReverseNavigation, bool isNotEnforced)
            {
                string hasMethod = GetHasMethod(relationship, fkCols, pkCols, isNotEnforced);
                string withMethod = GetWithMethod(relationship, fkCols, fkPropName, manyToManyMapping, mapKey, includeReverseNavigation);

                return string.Format("{0}(a => a.{1}){2}{3}",
                    hasMethod,
                    pkPropName,
                    withMethod,
                    cascadeOnDelete ? string.Empty : ".WillCascadeOnDelete(false)");
            }

            // HasOptional
            // HasRequired
            // HasMany
            private static string GetHasMethod(Relationship relationship, IList<Column> fkCols, IList<Column> pkCols, bool isNotEnforced)
            {
                bool withMany = (relationship == Relationship.ManyToOne || relationship == Relationship.ManyToMany);
                bool fkIsNullable = fkCols.Any(c => c.IsNullable);
                bool pkIsUnique = pkCols.Any(c => c.IsUnique || c.IsUniqueConstraint || c.IsPrimaryKey);

                if (withMany || pkIsUnique)
                {
                    if (fkIsNullable || isNotEnforced)
                    {
                        return "HasOptional";
                    }
                    else
                    {
                        return "HasRequired";
                    }
                }
                else
                {
                    return "HasMany";
                }

            }

            // WithOptional
            // WithRequired
            // WithMany
            // WithRequiredPrincipal
            // WithRequiredDependent
            private static string GetWithMethod(Relationship relationship, IList<Column> fkCols, string fkPropName, string manyToManyMapping, string mapKey, bool includeReverseNavigation)
            {
                string withParam = includeReverseNavigation ? string.Format("b => b.{0}", fkPropName) : string.Empty;
                switch (relationship)
                {
                    case Relationship.OneToOne:
                        return string.Format(".WithOptional({0})", withParam);

                    case Relationship.OneToMany:
                        return string.Format(".WithRequiredDependent({0})", withParam);

                    case Relationship.ManyToOne:
                        if (!fkCols.Any(c => c.Hidden))
                            return string.Format(".WithMany({0}).HasForeignKey({1})", withParam, manyToManyMapping);   // Foreign Key Association
                        return string.Format(".WithMany({0}).Map(c => c.MapKey({1}))", withParam, mapKey);  // Independent Association

                    case Relationship.ManyToMany:
                        return string.Format(".WithMany({0}).HasForeignKey({1})", withParam, manyToManyMapping);

                    default:
                        throw new ArgumentOutOfRangeException("relationship");
                }
            }

            private static Column CreateColumn(IDataRecord rdr, Regex rxClean, Table table, Regex columnFilterExclude)
            {
                if (rdr == null)
                    throw new ArgumentNullException("rdr");

                string typename = rdr["TypeName"].ToString().Trim().ToLower();
                int rdrScale = (int)rdr["Scale"];
                bool rdrIsNullable = (bool)rdr["IsNullable"];
                int rdrMaxLength = (int)rdr["MaxLength"];
                int rdrDtp = (int)rdr["DateTimePrecision"];
                int rdrPrecision = (int)rdr["Precision"];
                bool rdrIsIdentity = (bool)rdr["IsIdentity"];
                bool rdrIsComputed = (bool)rdr["IsComputed"];
                bool rdrIsRowGuid = (bool)rdr["IsRowGuid"];
                byte rdrGat = (byte)rdr["GeneratedAlwaysType"];
                bool rdrIsg = (bool)rdr["IsStoreGenerated"];
                int rdrPko = (int)rdr["PrimaryKeyOrdinal"];
                bool rdrIsPk = (bool)rdr["PrimaryKey"];
                bool rdrIsFk = (bool)rdr["IsForeignKey"];

                var col = new Column
                {
                    Ordinal = (int)rdr["Ordinal"],
                    Name = rdr["ColumnName"].ToString().Trim(),
                    IsNullable = rdrIsNullable,
                    PropertyType = GetPropertyType(typename),
                    SqlPropertyType = typename,
                    MaxLength = rdrMaxLength,
                    Precision = rdrPrecision,
                    Default = rdr["Default"].ToString().Trim(),
                    DateTimePrecision = rdrDtp,
                    Scale = rdrScale,

                    IsIdentity = rdrIsIdentity,
                    IsRowGuid = rdrIsRowGuid,
                    IsComputed = rdrIsComputed,
                    GeneratedAlwaysType = (ColumnGeneratedAlwaysType)rdrGat,
                    IsStoreGenerated = rdrIsg,

                    IsPrimaryKey = rdrIsPk,
                    PrimaryKeyOrdinal = rdrPko,
                    IsForeignKey = rdrIsFk,
                    ParentTable = table
                };

                if (col.MaxLength == -1 && (col.SqlPropertyType.EndsWith("varchar", StringComparison.InvariantCultureIgnoreCase) || col.SqlPropertyType.EndsWith("varbinary", StringComparison.InvariantCultureIgnoreCase)))
                    col.SqlPropertyType += "(max)";

                if (col.IsPrimaryKey && !col.IsIdentity && col.IsStoreGenerated && typename == "uniqueidentifier")
                {
                    col.IsStoreGenerated = false;
                    col.IsIdentity = true;
                }

                var fullName = string.Format("{0}.{1}.{2}", table.Schema, table.Name, col.Name);
                if (columnFilterExclude != null && !col.IsPrimaryKey && (columnFilterExclude.IsMatch(col.Name) || columnFilterExclude.IsMatch(fullName)))
                    col.Hidden = true;

                col.IsFixedLength = (typename == "char" || typename == "nchar");
                col.IsUnicode = !(typename == "char" || typename == "varchar" || typename == "text");
                col.IsMaxLength = (typename == "ntext");

                col.IsRowVersion = col.IsStoreGenerated && !col.IsNullable && typename == "timestamp";
                if (col.IsRowVersion)
                    col.MaxLength = 8;

                if (typename == "hierarchyid")
                    col.MaxLength = 0;

                col.CleanUpDefault();
                col.NameHumanCase = CleanUp(col.Name);
                col.NameHumanCase = rxClean.Replace(col.NameHumanCase, "_$1");

                if (ReservedKeywords.Contains(col.NameHumanCase))
                    col.NameHumanCase = "@" + col.NameHumanCase;

                col.DisplayName = ToDisplayName(col.Name);

                var titleCase = (Settings.UsePascalCase ? Inflector.ToTitleCase(col.NameHumanCase) : col.NameHumanCase).Replace(" ", "");
                if (titleCase != string.Empty)
                    col.NameHumanCase = titleCase;

                // Make sure property name doesn't clash with class name
                if (col.NameHumanCase == table.NameHumanCase)
                    col.NameHumanCase = col.NameHumanCase + "_";

                if (char.IsDigit(col.NameHumanCase[0]))
                    col.NameHumanCase = "_" + col.NameHumanCase;

                table.HasNullableColumns = IsNullable(col);

                // If PropertyType is empty, return null. Most likely ignoring a column due to legacy (such as OData not supporting spatial types)
                if (string.IsNullOrEmpty(col.PropertyType))
                    return null;

                return col;
            }

            private static string GetSqlDbType(string sqlType)
            {
                var sysType = "VarChar";
                switch (sqlType)
                {
                    case "hierarchyid":
                        sysType = "VarChar";
                        break;

                    case "bigint":
                        sysType = "BigInt";
                        break;

                    case "binary":
                        sysType = "Binary";
                        break;

                    case "bit":
                        sysType = "Bit";
                        break;

                    case "char":
                        sysType = "Char";
                        break;

                    case "datetime":
                        sysType = "DateTime";
                        break;

                    case "decimal":
                    case "numeric":
                        sysType = "Decimal";
                        break;

                    case "float":
                        sysType = "Float";
                        break;

                    case "image":
                        sysType = "Image";
                        break;

                    case "int":
                        sysType = "Int";
                        break;

                    case "money":
                        sysType = "Money";
                        break;

                    case "nchar":
                        sysType = "NChar";
                        break;

                    case "ntext":
                        sysType = "NText";
                        break;

                    case "nvarchar":
                        sysType = "NVarChar";
                        break;

                    case "real":
                        sysType = "Real";
                        break;

                    case "uniqueidentifier":
                        sysType = "UniqueIdentifier";
                        break;

                    case "smalldatetime":
                        sysType = "SmallDateTime";
                        break;

                    case "smallint":
                        sysType = "SmallInt";
                        break;

                    case "smallmoney":
                        sysType = "SmallMoney";
                        break;

                    case "text":
                        sysType = "Text";
                        break;

                    case "timestamp":
                        sysType = "Timestamp";
                        break;

                    case "tinyint":
                        sysType = "TinyInt";
                        break;

                    case "varbinary":
                        sysType = "VarBinary";
                        break;

                    case "varchar":
                        sysType = "VarChar";
                        break;

                    case "variant":
                        sysType = "Variant";
                        break;

                    case "xml":
                        sysType = "Xml";
                        break;

                    case "udt":
                        sysType = "Udt";
                        break;

                    case "table type":
                    case "structured":
                        sysType = "Structured";
                        break;

                    case "date":
                        sysType = "Date";
                        break;

                    case "time":
                        sysType = "Time";
                        break;

                    case "datetime2":
                        sysType = "DateTime2";
                        break;

                    case "datetimeoffset":
                        sysType = "DateTimeOffset";
                        break;
                }
                return sysType;
            }

            private static string GetPropertyType(string sqlType)
            {
                var sysType = "string";
                switch (sqlType)
                {
                    case "hierarchyid":
                        sysType = "System.Data.Entity.Hierarchy.HierarchyId";
                        break;
                    case "bigint":
                        sysType = "long";
                        break;
                    case "smallint":
                        sysType = "short";
                        break;
                    case "int":
                        sysType = "int";
                        break;
                    case "uniqueidentifier":
                        sysType = "System.Guid";
                        break;
                    case "smalldatetime":
                    case "datetime":
                    case "datetime2":
                    case "date":
                        sysType = "System.DateTime";
                        break;
                    case "datetimeoffset":
                        sysType = "System.DateTimeOffset";
                        break;
                    case "table type":
                        sysType = "System.Data.DataTable";
                        break;
                    case "time":
                        sysType = "System.TimeSpan";
                        break;
                    case "float":
                        sysType = "double";
                        break;
                    case "real":
                        sysType = "float";
                        break;
                    case "numeric":
                    case "smallmoney":
                    case "decimal":
                    case "money":
                        sysType = "decimal";
                        break;
                    case "tinyint":
                        sysType = "byte";
                        break;
                    case "bit":
                        sysType = "bool";
                        break;
                    case "image":
                    case "binary":
                    case "varbinary":
                    case "varbinary(max)":
                    case "timestamp":
                        sysType = "byte[]";
                        break;
                    case "geography":
                        sysType = Settings.DisableGeographyTypes ? "" : "System.Data.Entity.Spatial.DbGeography";
                        break;
                    case "geometry":
                        sysType = Settings.DisableGeographyTypes ? "" : "System.Data.Entity.Spatial.DbGeometry";
                        break;
                }
                return sysType;
            }
        }

        public class ForeignKey
        {
            public string FkTableName { get; private set; }
            public string FkSchema { get; private set; }
            public string PkTableName { get; private set; }
            public string PkTableNameFiltered { get; private set; }
            public string PkSchema { get; private set; }
            public string FkColumn { get; private set; }
            public string PkColumn { get; private set; }
            public string ConstraintName { get; private set; }
            public int Ordinal { get; private set; }
            public bool CascadeOnDelete { get; private set; }

            // User settable via ForeignKeyFilter callback
            public string AccessModifier { get; set; }
            public bool IncludeReverseNavigation { get; set; }
            public bool IncludeRequiredAttribute { get; set; }
            public bool IsNotEnforced { get; set; }

            public ForeignKey(string fkTableName, string fkSchema, string pkTableName, string pkSchema, string fkColumn, string pkColumn, string constraintName, string pkTableNameFiltered, int ordinal, bool cascadeOnDelete, bool isNotEnforced)
            {
                ConstraintName = constraintName;
                PkColumn = pkColumn;
                FkColumn = fkColumn;
                PkSchema = pkSchema;
                PkTableName = pkTableName;
                FkSchema = fkSchema;
                FkTableName = fkTableName;
                PkTableNameFiltered = pkTableNameFiltered;
                Ordinal = ordinal;
                CascadeOnDelete = cascadeOnDelete;
                IsNotEnforced = isNotEnforced;

                IncludeReverseNavigation = true;
            }

            public string PkTableHumanCase(string suffix)
            {
                var singular = Inflector.MakeSingular(PkTableNameFiltered);
                var pkTableHumanCase = (Settings.UsePascalCase ? Inflector.ToTitleCase(singular) : singular).Replace(" ", "").Replace("$", "");
                if (string.Compare(PkSchema, "dbo", StringComparison.OrdinalIgnoreCase) != 0 && Settings.PrependSchemaName)
                    pkTableHumanCase = PkSchema + "_" + pkTableHumanCase;
                pkTableHumanCase += suffix;
                return pkTableHumanCase;
            }
        }

        public class Index
        {
            public string Schema;
            public string TableName;
            public string IndexName;
            public byte KeyOrdinal;
            public string ColumnName;
            public int ColumnCount;
            public bool IsUnique;
            public bool IsPrimaryKey;
            public bool IsUniqueConstraint;
            public bool IsClustered;
        }

        public enum TableTemporalType
        {
            None,
            Verioned,
            History
        }

        public enum ColumnGeneratedAlwaysType
        {
            NotApplicable = 0,
            AsRowStart = 1,
            AsRowEnd = 2
        }

        public class Table
        {
            public string Name;
            public string NameHumanCase;
            public string Schema;
            public string Type;
            public string ClassName;
            public string Suffix;
            public string ExtendedProperty;
            public bool IsMapping;
            public bool IsView;
            public bool HasForeignKey;
            public bool HasNullableColumns;
            public bool HasPrimaryKey;
            public TableTemporalType TemporalType;

            public List<Column> Columns;
            public List<PropertyAndComments> ReverseNavigationProperty;
            public List<string> MappingConfiguration;
            public List<string> ReverseNavigationCtor;
            public List<string> ReverseNavigationUniquePropName;
            public List<string> ReverseNavigationUniquePropNameClashes;
            public List<string> DataAnnotations;

            public Table()
            {
                Columns = new List<Column>();
                ResetNavigationProperties();
                ReverseNavigationUniquePropNameClashes = new List<string>();
                DataAnnotations = new List<string>();
            }

            internal static string GetLazyLoadingMarker()
            {
                return Settings.UseLazyLoading ? "virtual " : string.Empty;
            }

            public string NameHumanCaseWithSuffix()
            {
                return NameHumanCase + Suffix;
            }

            public void ResetNavigationProperties()
            {
                MappingConfiguration = new List<string>();
                ReverseNavigationProperty = new List<PropertyAndComments>();
                ReverseNavigationCtor = new List<string>();
                ReverseNavigationUniquePropName = new List<string>();
                foreach (var col in Columns)
                    col.ResetNavigationProperties();
            }

            public void SetPrimaryKeys()
            {
                HasPrimaryKey = Columns.Any(x => x.IsPrimaryKey);
                if (HasPrimaryKey)
                    return; // Table has at least one primary key

                // This table is not allowed in EntityFramework as it does not have a primary key.
                // Therefore generate a composite key from all non-null fields.
                foreach (var col in Columns.Where(x => !x.IsNullable && !x.Hidden))
                {
                    col.IsPrimaryKey = true;
                    HasPrimaryKey = true;
                }
            }

            public IEnumerable<Column> PrimaryKeys
            {
                get
                {
                    return Columns
                        .Where(x => x.IsPrimaryKey)
                        .OrderBy(x => x.PrimaryKeyOrdinal)
                        .ThenBy(x => x.Ordinal)
                        .ToList();
                }
            }

            public string PrimaryKeyNameHumanCase()
            {
                var data = PrimaryKeys.Select(x => "x." + x.NameHumanCase).ToList();
                var n = data.Count;
                if (n == 0)
                    return string.Empty;
                if (n == 1)
                    return "x => " + data.First();
                // More than one primary key
                return string.Format("x => new {{ {0} }}", string.Join(", ", data));
            }

            public Column this[string columnName]
            {
                get { return GetColumn(columnName); }
            }

            public Column GetColumn(string columnName)
            {
                return Columns.SingleOrDefault(x => string.Compare(x.Name, columnName, StringComparison.OrdinalIgnoreCase) == 0);
            }

            public string GetUniqueColumnName(string tableNameHumanCase, ForeignKey foreignKey, bool checkForFkNameClashes, bool makeSingular, Relationship relationship)
            {
                var addReverseNavigationUniquePropName = checkForFkNameClashes && (Name == foreignKey.FkTableName || (Name == foreignKey.PkTableName && foreignKey.IncludeReverseNavigation));
                if (ReverseNavigationUniquePropName.Count == 0)
                {
                    ReverseNavigationUniquePropName.Add(NameHumanCase);
                    ReverseNavigationUniquePropName.AddRange(Columns.Select(c => c.NameHumanCase));
                }

                if (!makeSingular)
                    tableNameHumanCase = Inflector.MakePlural(tableNameHumanCase);

                if (checkForFkNameClashes && ReverseNavigationUniquePropName.Contains(tableNameHumanCase) && !ReverseNavigationUniquePropNameClashes.Contains(tableNameHumanCase))
                    ReverseNavigationUniquePropNameClashes.Add(tableNameHumanCase); // Name clash

                // Attempt 1
                string fkName = (Settings.UsePascalCase ? Inflector.ToTitleCase(foreignKey.FkColumn) : foreignKey.FkColumn).Replace(" ", "").Replace("$", "");
                string name = Settings.ForeignKeyName(tableNameHumanCase, foreignKey, fkName, relationship, 1);
                string col;
                if (!ReverseNavigationUniquePropNameClashes.Contains(name) && !ReverseNavigationUniquePropName.Contains(name))
                {
                    if (addReverseNavigationUniquePropName || !checkForFkNameClashes)
                    {
                        ReverseNavigationUniquePropName.Add(name);
                    }

                    return name;
                }

                if (Name == foreignKey.FkTableName)
                {
                    // Attempt 2
                    if (fkName.ToLowerInvariant().EndsWith("id"))
                    {
                        col = Settings.ForeignKeyName(tableNameHumanCase, foreignKey, fkName, relationship, 2);
                        if (checkForFkNameClashes && ReverseNavigationUniquePropName.Contains(col) &&
                            !ReverseNavigationUniquePropNameClashes.Contains(col))
                            ReverseNavigationUniquePropNameClashes.Add(col); // Name clash

                        if (!ReverseNavigationUniquePropNameClashes.Contains(col) &&
                            !ReverseNavigationUniquePropName.Contains(col))
                        {
                            if (addReverseNavigationUniquePropName || !checkForFkNameClashes)
                            {
                                ReverseNavigationUniquePropName.Add(col);
                            }

                            return col;
                        }
                    }

                    // Attempt 3
                    col = Settings.ForeignKeyName(tableNameHumanCase, foreignKey, fkName, relationship, 3);
                    if (checkForFkNameClashes && ReverseNavigationUniquePropName.Contains(col) &&
                        !ReverseNavigationUniquePropNameClashes.Contains(col))
                        ReverseNavigationUniquePropNameClashes.Add(col); // Name clash

                    if (!ReverseNavigationUniquePropNameClashes.Contains(col) &&
                        !ReverseNavigationUniquePropName.Contains(col))
                    {
                        if (addReverseNavigationUniquePropName || !checkForFkNameClashes)
                        {
                            ReverseNavigationUniquePropName.Add(col);
                        }

                        return col;
                    }
                }

                // Attempt 4
                col = Settings.ForeignKeyName(tableNameHumanCase, foreignKey, fkName, relationship, 4);
                if (checkForFkNameClashes && ReverseNavigationUniquePropName.Contains(col) && !ReverseNavigationUniquePropNameClashes.Contains(col))
                    ReverseNavigationUniquePropNameClashes.Add(col); // Name clash

                if (!ReverseNavigationUniquePropNameClashes.Contains(col) && !ReverseNavigationUniquePropName.Contains(col))
                {
                    if (addReverseNavigationUniquePropName || !checkForFkNameClashes)
                    {
                        ReverseNavigationUniquePropName.Add(col);
                    }

                    return col;
                }

                // Attempt 5
                for (int n = 1; n < 99; ++n)
                {
                    col = Settings.ForeignKeyName(tableNameHumanCase, foreignKey, fkName, relationship, 5) + n;

                    if (ReverseNavigationUniquePropName.Contains(col))
                        continue;

                    if (addReverseNavigationUniquePropName || !checkForFkNameClashes)
                    {
                        ReverseNavigationUniquePropName.Add(col);
                    }

                    return col;
                }

                // Give up
                return Settings.ForeignKeyName(tableNameHumanCase, foreignKey, fkName, relationship, 6);
            }

            public void AddReverseNavigation(Relationship relationship, string fkName, Table fkTable, string propName, string constraint, List<ForeignKey> fks, Table mappingTable = null)
            {
                var fkNames = "";
                switch (relationship)
                {
                    case Relationship.OneToOne:
                    case Relationship.OneToMany:
                    case Relationship.ManyToOne:
                        fkNames = (fks.Count > 1 ? "(" : "") + string.Join(", ", fks.Select(x => "[" + x.FkColumn + "]").Distinct().ToArray()) + (fks.Count > 1 ? ")" : "");
                        break;
                    case Relationship.ManyToMany:
                        break;
                }
                string accessModifier = fks != null && fks.FirstOrDefault() != null ? (fks.FirstOrDefault().AccessModifier ?? "public") : "public";
                switch (relationship)
                {
                    case Relationship.OneToOne:
                        ReverseNavigationProperty.Add(
                            new PropertyAndComments()
                            {
                                AdditionalDataAnnotations = Settings.ForeignKeyAnnotationsProcessing(fkTable, this, propName, string.Empty),
                                Definition = string.Format("{0} {1}{2} {3} {{ get; set; }}{4}", accessModifier, GetLazyLoadingMarker(), fkTable.NameHumanCaseWithSuffix(), propName, Settings.IncludeComments != CommentsStyle.None ? " // " + constraint : string.Empty),
                                Comments = string.Format("Parent (One-to-One) {0} pointed by [{1}].{2} ({3})", NameHumanCaseWithSuffix(), fkTable.Name, fkNames, fks.First().ConstraintName)
                            }
                        );
                        break;

                    case Relationship.OneToMany:
                        ReverseNavigationProperty.Add(
                            new PropertyAndComments()
                            {
                                AdditionalDataAnnotations = Settings.ForeignKeyAnnotationsProcessing(fkTable, this, propName, string.Empty),
                                Definition = string.Format("{0} {1}{2} {3} {{ get; set; }}{4}", accessModifier, GetLazyLoadingMarker(), fkTable.NameHumanCaseWithSuffix(), propName, Settings.IncludeComments != CommentsStyle.None ? " // " + constraint : string.Empty),
                                Comments = string.Format("Parent {0} pointed by [{1}].{2} ({3})", NameHumanCaseWithSuffix(), fkTable.Name, fkNames, fks.First().ConstraintName)
                            }
                        );
                        break;

                    case Relationship.ManyToOne:
                        string initialization1 = string.Empty;
                        if (Settings.UsePropertyInitializers)
                            initialization1 = string.Format(" = new {0}<{1}>();", Settings.CollectionType, fkTable.NameHumanCaseWithSuffix());
                        ReverseNavigationProperty.Add(
                            new PropertyAndComments()
                            {
                                AdditionalDataAnnotations = Settings.ForeignKeyAnnotationsProcessing(fkTable, this, propName, string.Empty),
                                Definition = string.Format("{0} {1}{2}<{3}> {4} {{ get; set; }}{5}{6}", accessModifier, GetLazyLoadingMarker(), Settings.CollectionInterfaceType, fkTable.NameHumanCaseWithSuffix(), propName, initialization1, Settings.IncludeComments != CommentsStyle.None ? " // " + constraint : string.Empty),
                                Comments = string.Format("Child {0} where [{1}].{2} point to this entity ({3})", Inflector.MakePlural(fkTable.NameHumanCase), fkTable.Name, fkNames, fks.First().ConstraintName)
                            }
                        );
                        ReverseNavigationCtor.Add(string.Format("{0} = new {1}<{2}>();", propName, Settings.CollectionType, fkTable.NameHumanCaseWithSuffix()));
                        break;

                    case Relationship.ManyToMany:
                        string initialization2 = string.Empty;
                        if (Settings.UsePropertyInitializers)
                            initialization2 = string.Format(" = new {0}<{1}>();", Settings.CollectionType, fkTable.NameHumanCaseWithSuffix());
                        ReverseNavigationProperty.Add(
                            new PropertyAndComments()
                            {
                                AdditionalDataAnnotations = Settings.ForeignKeyAnnotationsProcessing(fkTable, this, propName, string.Empty),
                                Definition = string.Format("{0} {1}{2}<{3}> {4} {{ get; set; }}{5}{6}", accessModifier, GetLazyLoadingMarker(), Settings.CollectionInterfaceType, fkTable.NameHumanCaseWithSuffix(), propName, initialization2, Settings.IncludeComments != CommentsStyle.None ? " // Many to many mapping" : string.Empty),
                                Comments = string.Format("Child {0} (Many-to-Many) mapped by table [{1}]", Inflector.MakePlural(fkTable.NameHumanCase), mappingTable == null ? string.Empty : mappingTable.Name)
                            }
                        );

                        ReverseNavigationCtor.Add(string.Format("{0} = new {1}<{2}>();", propName, Settings.CollectionType, fkTable.NameHumanCaseWithSuffix()));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("relationship");
                }
            }

            public void AddMappingConfiguration(ForeignKey left, ForeignKey right, string leftPropName, string rightPropName)
            {
                MappingConfiguration.Add(string.Format(@"HasMany(t => t.{0}).WithMany(t => t.{1}).Map(m =>
            {{
                m.ToTable(""{2}""{5});
                m.MapLeftKey(""{3}"");
                m.MapRightKey(""{4}"");
            }});", leftPropName, rightPropName, left.FkTableName, left.FkColumn, right.FkColumn, Settings.IsSqlCe ? string.Empty : ", \"" + left.FkSchema + "\""));
            }

            public void IdentifyMappingTable(List<ForeignKey> fkList, Tables tables, bool checkForFkNameClashes)
            {
                IsMapping = false;

                var nonReadOnlyColumns = Columns.Where(c => !c.IsIdentity && !c.IsRowVersion && !c.IsStoreGenerated && !c.Hidden).ToList();

                // Ignoring read-only columns, it must have only 2 columns to be a mapping table
                if (nonReadOnlyColumns.Count != 2)
                    return;

                // Must have 2 primary keys
                if (nonReadOnlyColumns.Count(x => x.IsPrimaryKey) != 2)
                    return;

                // No columns should be nullable
                if (nonReadOnlyColumns.Any(x => x.IsNullable))
                    return;

                // Find the foreign keys for this table
                var foreignKeys = fkList.Where(x =>
                                                string.Compare(x.FkTableName, Name, StringComparison.OrdinalIgnoreCase) == 0 &&
                                                string.Compare(x.FkSchema, Schema, StringComparison.OrdinalIgnoreCase) == 0)
                                        .ToList();

                // Each column must have a foreign key, therefore check column and foreign key counts match
                if (foreignKeys.Select(x => x.FkColumn).Distinct().Count() != 2)
                    return;

                ForeignKey left = foreignKeys[0];
                ForeignKey right = foreignKeys[1];
                if (!left.IncludeReverseNavigation || !right.IncludeReverseNavigation)
                    return;

                Table leftTable = tables.GetTable(left.PkTableName, left.PkSchema);
                if (leftTable == null)
                    return;

                Table rightTable = tables.GetTable(right.PkTableName, right.PkSchema);
                if (rightTable == null)
                    return;

                var leftPropName = leftTable.GetUniqueColumnName(rightTable.NameHumanCase, right, checkForFkNameClashes, false, Relationship.ManyToOne); // relationship from the mapping table to each side is Many-to-One
                leftPropName = Settings.MappingTableRename(Name, leftTable.NameHumanCase, leftPropName);
                var rightPropName = rightTable.GetUniqueColumnName(leftTable.NameHumanCase, left, checkForFkNameClashes, false, Relationship.ManyToOne); // relationship from the mapping table to each side is Many-to-One
                rightPropName = Settings.MappingTableRename(Name, rightTable.NameHumanCase, rightPropName);
                leftTable.AddMappingConfiguration(left, right, leftPropName, rightPropName);

                IsMapping = true;
                rightTable.AddReverseNavigation(Relationship.ManyToMany, rightTable.NameHumanCase, leftTable, rightPropName, null, null, this);
                leftTable.AddReverseNavigation(Relationship.ManyToMany, leftTable.NameHumanCase, rightTable, leftPropName, null, null, this);
            }

            public void SetupDataAnnotations()
            {
                var schema = String.Empty;
                if (!Settings.IsSqlCe)
                    schema = String.Format(", Schema = \"{0}\"", Schema);
                DataAnnotations = new List<string>
            {
                HasPrimaryKey
                    ? string.Format("Table(\"{0}\"{1})", Name, schema)
                    : "NotMapped"
            };

            }
        }

        public class Tables : List<Table>
        {
            public Table GetTable(string tableName, string schema)
            {
                return this.SingleOrDefault(x =>
                    string.Compare(x.Name, tableName, StringComparison.OrdinalIgnoreCase) == 0 &&
                    string.Compare(x.Schema, schema, StringComparison.OrdinalIgnoreCase) == 0);
            }

            public void SetPrimaryKeys()
            {
                foreach (var tbl in this)
                {
                    tbl.SetPrimaryKeys();
                }
            }

            public void IdentifyMappingTables(List<ForeignKey> fkList, bool checkForFkNameClashes)
            {
                foreach (var tbl in this.Where(x => x.HasForeignKey))
                {
                    tbl.IdentifyMappingTable(fkList, this, checkForFkNameClashes);
                }
            }

            public void ResetNavigationProperties()
            {
                foreach (var tbl in this)
                {
                    tbl.ResetNavigationProperties();
                }
            }
        }

        // ***********************************************************************
        // ** Stored procedure callbacks

        public static readonly Func<StoredProcedure, string> WriteStoredProcFunctionName = sp => sp.NameHumanCase;

        public static readonly Func<StoredProcedure, bool> StoredProcHasOutParams = (sp) =>
        {
            return sp.Parameters.Any(x => x.Mode != StoredProcedureParameterMode.In);
        };

        public static readonly Func<StoredProcedure, bool, string> WriteStoredProcFunctionParams = (sp, includeProcResult) =>
        {
            var sb = new StringBuilder();
            int n = 1;
            int count = sp.Parameters.Count;
            foreach (var p in sp.Parameters.OrderBy(x => x.Ordinal))
            {
                sb.AppendFormat("{0}{1}{2} {3}{4}",
                    p.Mode == StoredProcedureParameterMode.In ? "" : "out ",
                    p.PropertyType,
                    NotNullable.Contains(p.PropertyType.ToLower()) ? string.Empty : "?",
                    p.NameHumanCase,
                    (n++ < count) ? ", " : string.Empty);
            }
            if (includeProcResult && sp.ReturnModels.Count > 0 && sp.ReturnModels.First().Count > 0)
                sb.AppendFormat((sp.Parameters.Count > 0 ? ", " : "") + "out int procResult");
            return sb.ToString();
        };

        public static readonly Func<StoredProcedure, string> WriteStoredProcFunctionOverloadCall = (sp) =>
        {
            var sb = new StringBuilder();
            foreach (var p in sp.Parameters.OrderBy(x => x.Ordinal))
            {
                sb.AppendFormat("{0}{1}, ",
                    p.Mode == StoredProcedureParameterMode.In ? "" : "out ",
                    p.NameHumanCase);
            }
            sb.Append("out procResult");
            return sb.ToString();
        };

        public static readonly Func<StoredProcedure, string> WriteStoredProcFunctionSqlAtParams = sp =>
        {
            var sb = new StringBuilder();
            int n = 1;
            int count = sp.Parameters.Count;
            foreach (var p in sp.Parameters.OrderBy(x => x.Ordinal))
            {
                sb.AppendFormat("{0}{1}{2}",
                    p.Name,
                    p.Mode == StoredProcedureParameterMode.In ? string.Empty : " OUTPUT",
                    (n++ < count) ? ", " : string.Empty);
            }
            return sb.ToString();
        };

        public static readonly Func<StoredProcedureParameter, string> WriteStoredProcSqlParameterName = p => p.NameHumanCase + "Param";

        public static readonly Action<Scripty.Core.Output.OutputFile, StoredProcedure, bool> WriteStoredProcFunctionDeclareSqlParameter = (o, sp, includeProcResult) =>
        {
            foreach (var p in sp.Parameters.OrderBy(x => x.Ordinal))
            {
                var isNullable = !NotNullable.Contains(p.PropertyType.ToLower());
                var getValueOrDefault = isNullable ? ".GetValueOrDefault()" : string.Empty;
                var isGeography = p.PropertyType == "System.Data.Entity.Spatial.DbGeography";

                o?.WriteLine(
                    string.Format("var {0} = new System.Data.SqlClient.SqlParameter", WriteStoredProcSqlParameterName(p))
                    + string.Format(" {{ ParameterName = \"{0}\", ", p.Name)
                    + (isGeography ? "UdtTypeName = \"geography\"" : string.Format("SqlDbType = System.Data.SqlDbType.{0}", p.SqlDbType))
                    + ", Direction = System.Data.ParameterDirection."
                    + (p.Mode == StoredProcedureParameterMode.In ? "Input" : "Output")
                    + (p.Mode == StoredProcedureParameterMode.In
                        ? ", Value = " + (isGeography
                            ? string.Format("Microsoft.SqlServer.Types.SqlGeography.Parse({0}.AsText())", p.NameHumanCase)
                              : p.NameHumanCase + getValueOrDefault)
                        : string.Empty)
                    + (p.MaxLength != 0 ? ", Size = " + p.MaxLength : string.Empty)
                    + ((p.Precision > 0 || p.Scale > 0) ? ", Precision = " + p.Precision + ", Scale = " + p.Scale : string.Empty)
                    + (p.PropertyType.ToLower().Contains("datatable") ? ", TypeName = \"" + p.UserDefinedTypeName + "\"" : string.Empty)
                    + " };");

                if (p.Mode == StoredProcedureParameterMode.In)
                {
                    o?.WriteLine(
                        isNullable
                            ? "if (!{0}.HasValue){1}                {0}Param.Value = System.DBNull.Value;{1}"
                            : "if ({0}Param.Value == null){1}                {0}Param.Value = System.DBNull.Value;{1}",
                        p.NameHumanCase, Environment.NewLine);
                }
            }
            if (includeProcResult && sp.ReturnModels.Count < 2)
                o?.WriteLine("var procResultParam = new System.Data.SqlClient.SqlParameter { ParameterName = \"@procResult\", SqlDbType = System.Data.SqlDbType.Int, Direction = System.Data.ParameterDirection.Output };");
        };

        public static readonly Func<StoredProcedure, string> WriteTableValuedFunctionDeclareSqlParameter = sp =>
        {
            var sb = new StringBuilder();
            foreach (var p in sp.Parameters.OrderBy(x => x.Ordinal))
            {
                sb.AppendLine(string.Format("var {0}Param = new System.Data.Entity.Core.Objects.ObjectParameter(\"{1}\", typeof({2})) {{ Value = (object){3} }};",
                    p.NameHumanCase,
                    p.Name.Substring(1),
                    p.PropertyType,
                    p.NameHumanCase + (p.Mode == StoredProcedureParameterMode.In && NotNullable.Contains(p.PropertyType.ToLowerInvariant()) ? string.Empty : " ?? System.DBNull.Value")));
            }
            return sb.ToString();
        };

        public static readonly Func<StoredProcedure, bool, string> WriteStoredProcFunctionSqlParameterAnonymousArray = (sp, includeProcResultParam) =>
        {
            var sb = new StringBuilder();
            bool hasParam = false;
            foreach (var p in sp.Parameters.OrderBy(x => x.Ordinal))
            {
                sb.Append(string.Format("{0}Param, ", p.NameHumanCase));
                hasParam = true;
            }
            if (includeProcResultParam)
                sb.Append("procResultParam");
            else if (hasParam)
                sb.Remove(sb.Length - 2, 2);
            return sb.ToString();
        };

        public static readonly Func<StoredProcedure, string> WriteTableValuedFunctionSqlParameterAnonymousArray = sp =>
        {
            if (sp.Parameters.Count == 0)
                return "new System.Data.Entity.Core.Objects.ObjectParameter[] { }";
            var sb = new StringBuilder();
            foreach (var p in sp.Parameters.OrderBy(x => x.Ordinal))
            {
                sb.Append(string.Format("{0}Param, ", p.NameHumanCase));
            }
            return sb.ToString().Substring(0, sb.Length - 2);
        };

        public static readonly Action<Scripty.Core.Output.OutputFile, StoredProcedure, bool> WriteStoredProcFunctionSetSqlParameters = (o, sp, isFake) =>
        {
            foreach (var p in sp.Parameters.Where(x => x.Mode != StoredProcedureParameterMode.In).OrderBy(x => x.Ordinal))
            {
                var Default = string.Format("default({0})", p.PropertyType);
                var notNullable = NotNullable.Contains(p.PropertyType.ToLower());

                if (isFake)
                    o?.WriteLine(string.Format("{0} = {1};", p.NameHumanCase, Default));
                else
                {
                    o?.WriteLine(string.Format("if (IsSqlParameterNull({0}Param))", p.NameHumanCase));
                    o?.WriteLine(string.Format("    {0} = {1};", p.NameHumanCase, notNullable ? Default : "null"));
                    o?.WriteLine("else");
                    o?.WriteLine(string.Format("    {0} = ({1}) {2}Param.Value;", p.NameHumanCase, p.PropertyType, p.NameHumanCase));
                }
            }
        };

        public static readonly Func<StoredProcedure, string> WriteStoredProcReturnModelName = sp =>
        {
            if (Settings.StoredProcedureReturnTypes.ContainsKey(sp.NameHumanCase))
                return Settings.StoredProcedureReturnTypes[sp.NameHumanCase];
            if (Settings.StoredProcedureReturnTypes.ContainsKey(sp.Name))
                return Settings.StoredProcedureReturnTypes[sp.Name];

            var name = string.Format("{0}ReturnModel", sp.NameHumanCase);
            if (Settings.StoredProcedureReturnModelRename != null)
            {
                var customName = Settings.StoredProcedureReturnModelRename(name, sp);
                if (!string.IsNullOrEmpty(customName))
                    name = customName;
            }

            return name;
        };

        public static readonly Func<DataColumn, string> WriteStoredProcReturnColumn = col =>
        {
            var columnName = ReservedKeywords.Contains(col.ColumnName) ? "@" + col.ColumnName : col.ColumnName;

            return string.Format("public {0} {1} {{ get; set; }}",
                StoredProcedure.WrapTypeIfNullable(
                    (col.DataType.Name.Equals("SqlHierarchyId") ? "Microsoft.SqlServer.Types." : col.DataType.Namespace + ".") +
                    col.DataType.Name, col),
                columnName);
        };

        public static readonly Func<StoredProcedure, string> WriteStoredProcReturnType = (sp) =>
        {
            var returnModelCount = sp.ReturnModels.Count;
            if (returnModelCount == 0)
                return "int";

            var spReturnClassName = WriteStoredProcReturnModelName(sp);
            return (returnModelCount == 1) ? string.Format("System.Collections.Generic.List<{0}>", spReturnClassName) : spReturnClassName;
        };

        /// <summary>
        /// Helper class in making dynamic class definitions easier.
        /// </summary>
        public sealed class BaseClassMaker
        {
            private string _typeName;
            private StringBuilder _interfaces;

            public BaseClassMaker(string baseClassName = null)
            {
                SetBaseClassName(baseClassName);
            }

            /// <summary>
            /// Sets the base-class name.
            /// </summary>
            public void SetBaseClassName(string typeName)
            {
                _typeName = typeName;
            }

            /// <summary>
            /// Appends additional implemented interface.
            /// </summary>
            public bool AddInterface(string typeName)
            {
                if (string.IsNullOrEmpty(typeName))
                    return false;

                if (_interfaces == null)
                {
                    _interfaces = new StringBuilder();
                }
                else
                {
                    if (_interfaces.Length > 0)
                    {
                        _interfaces.Append(", ");
                    }
                }

                _interfaces.Append(typeName);
                return true;
            }

            /// <summary>
            /// Conditionally appends additional implemented interface.
            /// </summary>
            public bool AddInterface(string interfaceName, bool condition)
            {
                if (condition)
                {
                    return AddInterface(interfaceName);
                }

                return false;
            }

            public override string ToString()
            {
                var hasInterfaces = _interfaces != null && _interfaces.Length > 0;

                if (string.IsNullOrEmpty(_typeName))
                {
                    return hasInterfaces ? " : " + _interfaces : string.Empty;
                }

                return hasInterfaces ? string.Concat(" : ", _typeName, ", ", _interfaces) : " : " + _typeName;
            }
        }

        #endregion




        public async Task OutputProjectStructure()
        {
            this.Configure();
            this.Generate();
        }

        #region StartNewFile / FinishCurrentFile - with header and footer
        void StartNewFile(string path)
        {
            FinishCurrentFile();
            if (_context != null)
                this._output = _context.Output[path]; // default file
            if (_output != null)
            {
                // If you embed this template into your project, you can change this to automatically add the generated file to your csproj and set it as Compilable code
                _output.BuildAction = Scripty.Core.Output.BuildAction.GenerateOnly;
                //_output?.BuildAction = Scripty.Core.Output.BuildAction.Compile;
            }
            WriteTextBlock(_output, $@"
                // <auto-generated>
                // ReSharper disable ConvertPropertyToExpressionBody
                // ReSharper disable DoNotCallOverridableMethodsInConstructor
                // ReSharper disable EmptyNamespace
                // ReSharper disable InconsistentNaming
                // ReSharper disable PartialMethodWithSinglePart
                // ReSharper disable PartialTypeWithSinglePart
                // ReSharper disable RedundantNameQualifier
                // ReSharper disable RedundantOverridenMember
                // ReSharper disable UseNameofExpression
                // TargetFrameworkVersion = { Settings.TargetFrameworkVersion }
                #pragma warning disable 1591    //  Ignore ""Missing XML Comment"" warning
                    ");

            if (Settings.ElementsToGenerate.HasFlag(Elements.Poco) || Settings.ElementsToGenerate.HasFlag(Elements.PocoConfiguration)) // Line 20
            {
                if (Settings.UseDataAnnotations || Settings.UseDataAnnotationsWithFluent)
                {
                    _output?.WriteLine("using System.ComponentModel.DataAnnotations;");
                }
                if (Settings.UseDataAnnotations)
                {
                    _output?.WriteLine("using System.ComponentModel.DataAnnotations.Schema;");
                }
            }

            _output?.WriteLine($"namespace { Settings.Namespace }"); // Line 31 // this is in StartNewFile()
            _output?.WriteLine("{"); // this is in StartNewFile()
            if (_output != null)
    	        _output.IndentLevel++;



        }
        void FinishCurrentFile()
        {
            if (this._output == null)
                return;
            if (_output != null)
            _output.IndentLevel--;
            _output?.WriteLine("}");
            _output?.WriteLine("// </auto-generated>");
        }

        #endregion

        #region Template Helpers
        /// <summary>
        /// Given a text block (multiple lines), this removes the left padding of the block, by calculating the minimum number of spaces which happens in EVERY line.
        /// Then, this method writes one by one each line, which in case will respect the current indent.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static void WriteTextBlock(Scripty.Core.Output.OutputFile output, string str)
        {
            //Console.WriteLine("[" + str + "]");

            var nonEmptyLines = str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).Where(line => line.TrimEnd().Length > 0);
            int minNumberOfSpaces = nonEmptyLines.Select(nonEmptyLine => nonEmptyLine.Length - nonEmptyLine.TrimStart().Length).Min();
            var allLines = str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < allLines.Length; i++)
            {
                string line = allLines[i];
                // to make templates more readable, let's assume that each block may have one empty line before and one empty line after it - ignore those empty lines
                if (i == 0 && line.TrimStart() == string.Empty)
                    continue;
                if (i == allLines.Length - 1 && line.TrimEnd() == string.Empty)
                    continue;

                //Console.WriteLine("[" + line.Substring(Math.Min(line.Length, minNumberOfSpaces)).TrimEnd() + "]");
                output?.WriteLine(line.Substring(Math.Min(line.Length, minNumberOfSpaces)).TrimEnd());
            }
        }

        #endregion

    }
}
