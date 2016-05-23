using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.DotNet.ProjectModel;
using System.Linq;

namespace MultiProjectValidator
{
    public class Program
    {
        public static int Main(string[] args)
        {

            string rootPath = null;

            try
            {
                rootPath = ParseAndValidateArgs(args);
            }
            catch
            {
                return 1;
            }

            List<ProjectContext> projects = null;
            try
            {
                projects = ProjectLoader.Load(rootPath);
            }
            catch(Exception)
            {
                Console.WriteLine("Failed to load projects from path: " + rootPath);
                return 1;
            }

            var analyzer = ProjectAnalyzer.Create(projects);
            var analysisResults = analyzer.DoAnalysis();
            var failed = analysisResults.Where((a) => !a.Passed).Any();

            PrintAnalysisResults(analysisResults);

            return failed ? 1 : 0;
        }

        private static void PrintAnalysisResults(List<AnalysisResult> analysisResults)
        {
            Console.WriteLine("Project Validation Results");

            var failedCount = analysisResults.Where((a) => !a.Passed).Count();
            var passedCount = analysisResults.Where((a) => a.Passed).Count();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{passedCount} Successful Analysis Rules");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{failedCount} Failed Analysis Rules");
            
            if (failedCount != 0)
            {
                Console.WriteLine("Failure Messages");

                foreach(var result in analysisResults)
                {
                    if (!result.Passed)
                    {
                        foreach(var message in result.Messages)
                        {
                            Console.WriteLine(message);
                        }
                    }
                }
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static bool AnyAnalysisFailed(List<AnalysisResult> analysisResults)
        {
            foreach (var result in analysisResults)
            {
                if (!result.Passed)
                {
                    return true;
                }
            }
            return false;
        }

        private static string ParseAndValidateArgs(string[] args)
        {
            if (args.Length != 1)
            {
                PrintHelp();
                throw new Exception();
            }

            string rootPath = args[0];

            if (!Directory.Exists(rootPath))
            {
                Console.WriteLine("Root Directory does not exist: " + rootPath);
                throw new Exception();
            }

            return rootPath;
        }
        
        private static void PrintHelp()
        {
            var help = @"
Multi-Project Validator

Description:
This tool recursively searches for project.json's from the given root path.
It then applies a set of analysis rules, determines whether they pass/fail
and then sets exit code to reflect.

Note:
Ensure all analyzed project.json have been restored prior to running this tool.

Usage:
pjvalidate [root path of recursive search]";

            Console.WriteLine(help);
        }
    }
}
