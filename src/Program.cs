using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework_Scripty_Templates
{
    /// <summary>
    /// Scripty templates to generate an Entity Framework model from an Existing Database
    /// 
    /// These are based on Simon Hughes T4 templates
    /// - see https://github.com/sjh37/EntityFramework-Reverse-POCO-Code-First-Generator / http://www.reversepoco.com/
    ///
    /// Instead of using T4 templates (which don't have good support in .NET Core, and don't have good debugging tools), I decided to use Scripty (by Dave Glick)
    /// - see https://github.com/daveaglick/Scripty
    /// Scripty templates are based on Roslyn so they can be created using regular C#, using Visual Studio, intellisense, compiled, etc. Much easier than T4.
    /// 
    /// I'm not rewriting this "just because", but mostly because I want to convert those templates to Entity Framework Core.
    /// 
    /// Created by Richard Drizin
    /// Contact me at http://drizin.io
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {

            RunAllTemplates();

            Console.WriteLine(string.Format("You can embed the execution of this Scripty template into your project using CLI interface (see below) or using Cake"));
            Console.WriteLine(string.Format(""));
            Console.WriteLine(string.Format("If you want to run Scripty from command-line:"));
            Console.WriteLine(string.Format("1) Download Scripty at https://github.com/daveaglick/Scripty"));
            Console.WriteLine(string.Format("2) Build it"));
            Console.WriteLine(string.Format("3) Run {0} YourCsProj.csproj GenerateEFModel.csx", @"Scripty\src\Scripty\bin\Debug\Scripty.exe"));
            Console.ReadLine();
        }

        static void FindAndRunTemplate()
        {
            #region Find CSPROJ, SLN, CSX
            DirectoryInfo di = new DirectoryInfo(Environment.CurrentDirectory);
            string csProj = null; string sln = null; string csx = null;
            string csProjName = "EntityFramework-Scripty-Templates.csproj";
            string slnProjName = "EntityFramework-Scripty-Templates.sln";
            string csxName = "Templates\\GenerateEFModel.csx";
            while ((csProj == null || sln == null || csx == null) && di.Parent != null)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(di.FullName, csProjName)))
                    csProj = System.IO.Path.Combine(di.FullName, csProjName);
                if (System.IO.File.Exists(System.IO.Path.Combine(di.FullName, slnProjName)))
                    sln = System.IO.Path.Combine(di.FullName, slnProjName);
                if (System.IO.File.Exists(System.IO.Path.Combine(di.FullName, csxName)))
                    csx = System.IO.Path.Combine(di.FullName, csxName);
                di = di.Parent;
            }
            #endregion
            var scripty = new Scripty.Core.ScriptEngine(csProj);
            var result = scripty.Evaluate(new Scripty.Core.ScriptSource(csx, File.ReadAllText(csx))).Result;
        }

        static void RunAllTemplates()
        {
            #region Find all CSX templates
            Console.WriteLine(string.Format("Searching for Templates..."));
            DirectoryInfo di = new DirectoryInfo(Environment.CurrentDirectory);
            FileInfo[] scripts = new FileInfo[0];
            while (scripts.Length == 0 && di.Parent != null)
            {
                if (System.IO.Directory.Exists(System.IO.Path.Combine(di.FullName, "Templates")))
                    scripts = di.GetFiles("Templates\\*.csx");
                di = di.Parent;
            }
            if (scripts.Length == 0)
            {
                Console.WriteLine(string.Format("Can't find Templates folder"));
                Console.ReadLine();
                Environment.Exit(-1);
            }
            #endregion

            // Any dummy path is fine as long as we only use BuildAction.GenerateOnly (so Scripty don't need to manipulate csproj file)
            var scripty = new Scripty.Core.ScriptEngine("C:\\dummy");

            foreach (var script in scripts)
            {
                Console.Write(string.Format("Regenerating {0}...", script.FullName));
                var result = scripty.Evaluate(new Scripty.Core.ScriptSource(script.FullName, File.ReadAllText(script.FullName))).Result;
                Console.WriteLine(string.Format("done! ({0} file(s): {1})", result.OutputFiles.Count, string.Join(", ", result.OutputFiles.Select(o => o.FilePath))));
            }
        }
    }
}
