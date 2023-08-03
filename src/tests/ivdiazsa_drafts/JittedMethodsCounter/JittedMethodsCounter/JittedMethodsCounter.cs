// TODO: Add the Microsoft License header here.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class JittedMethodsCounter
{
    static int Main(string[] args)
    {
        // Let's draft what we need to do for this test in plain terms.
        //
        // ! 1) We need to build and publish the HelloWorld app. (MSBuild maybe?)
        // * 2) We need to run the HelloWorld app's executable.
        // * 3) We need to capture its output.
        // * 4) We need to read said output. More precisely, the last line.
        // * 5) We need to parse the last line to get the last JIT'd method's number.
        // * 6) We need to compare this number against a preset expected result.
        // * 7) From there, decide whether the test passed or not.

        string appName = "HelloWorld";
        string jitOutputFile = "jits.txt";
        int testReturnCode = 0;

        int appResult = RunHelloWorldApp(appName, jitOutputFile);
        int jits = GetNumberOfJittedMethods(jitOutputFile);

        Console.WriteLine($"\nApp's Exit Code: {appResult}");
        Console.WriteLine($"App's Total Jitted Methods: {jits}");

        if (jits < 100)
        {
            testReturnCode = 100;
            Console.WriteLine("Less than 100 methods were Jitted so the test"
                              + " has passed!");
        }
        else
        {
            testReturnCode = 101;
            Console.WriteLine("More than 100 methods were Jitted so the test"
                              + " does not pass.");
        }

        return testReturnCode;
    }
 
    private static int RunHelloWorldApp(string appName, string jitOutputFile)
    {
        int exitCode = -1;

        using (Process app = new Process())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = appName,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            startInfo.EnvironmentVariables.Add("DOTNET_JitDisasmSummary", "1");
            startInfo.EnvironmentVariables.Add("DOTNET_TieredCompilation", "0");
            startInfo.EnvironmentVariables.Add("DOTNET_ReadyToRun", "1");
            startInfo.EnvironmentVariables.Add("DOTNET_JitStdOutFile", jitOutputFile);

            app.StartInfo = startInfo;
            app.Start();
            app.WaitForExit();

            exitCode = app.ExitCode;
        }

        return exitCode;
    }

    private static int GetNumberOfJittedMethods(string jitOutputFile)
    {
        string[] tokens = File.ReadLines(jitOutputFile)
                              .Last()
                              .Split(":");

        int numJittedMethods = Int32.Parse(tokens[0]);
        return numJittedMethods;
    }
}
