// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreclrTestLib
{
    public class CoreclrTestWrapperLib
    {
        public const int EXIT_SUCCESS_CODE = 0;
        public const string TIMEOUT_ENVIRONMENT_VAR = "__TestTimeout";
        
        // Default timeout set to 10 minutes
        public const int DEFAULT_TIMEOUT = 1000 * 60*10;
        public const string GC_STRESS_LEVEL = "__GCSTRESSLEVEL";

        public int RunTest(string executable, string outputFile, string errorFile)
        {
            Debug.Assert(outputFile != errorFile);

            int exitCode = -100;
            
            // If a timeout was given to us by an environment variable, use it instead of the default
            // timeout.
            string environmentVar = Environment.GetEnvironmentVariable(TIMEOUT_ENVIRONMENT_VAR);
            int timeout = environmentVar != null ? int.Parse(environmentVar) : DEFAULT_TIMEOUT;

            string gcstressVar = Environment.GetEnvironmentVariable(GC_STRESS_LEVEL);

            // Check if we are running in Windows
            string operatingSystem = System.Environment.GetEnvironmentVariable("OS");
            bool runningInWindows = (operatingSystem != null && operatingSystem.StartsWith("Windows"));

            var outputStream = new FileStream(outputFile, FileMode.Create);
            var errorStream = new FileStream(errorFile, FileMode.Create);

            using (var outputWriter = new StreamWriter(outputStream))
            using (var errorWriter = new StreamWriter(errorStream))
            using (Process process = new Process())
            {
                if (gcstressVar!=null)
                {
                    //Note: this is not the best way to set the Env, but since we are using 
                    //Desktop to start the tests, this Env will affect the test harness behavior
                    process.StartInfo.EnvironmentVariables["COMPlus_GCStress"] = gcstressVar;
                }

                // Windows can run the executable implicitly
                if (runningInWindows)
                {
                    process.StartInfo.FileName = executable;
                }
                // Non-windows needs to be told explicitly to run through /bin/bash shell
                else
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = executable;
                }

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();

                Task copyOutput = process.StandardOutput.BaseStream.CopyToAsync(outputStream);
                Task copyError = process.StandardError.BaseStream.CopyToAsync(errorStream);

                bool completed = process.WaitForExit(timeout);
                copyOutput.Wait(timeout);
                copyError.Wait(timeout);

                if (completed)
                {
                    // Process completed. Check process.ExitCode here.
                    exitCode = process.ExitCode;
                }
                else
                {
                    // Timed out.
                    outputWriter.WriteLine("cmdLine:" + executable + " Timed Out");
                    errorWriter.WriteLine("cmdLine:" + executable + " Timed Out");
                }

               outputWriter.WriteLine("Test Harness Exitcode is : " + exitCode.ToString());
               outputWriter.Flush();

               errorWriter.Flush();
            }

            return exitCode;
        }

        
    }
}
