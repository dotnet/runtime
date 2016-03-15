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

        public int RunTest(string executable, string outputFile, string errorFile)
        {
            Debug.Assert(outputFile != errorFile);

            int exitCode = -100;
            int timeout = 1000 * 60*10;

            var outputStream = new FileStream(outputFile, FileMode.Create);
            var errorStream = new FileStream(errorFile, FileMode.Create);

            using (var outputWriter = new StreamWriter(outputStream))
            using (var errorWriter = new StreamWriter(errorStream))
            using (Process process = new Process())
            {
                process.StartInfo.FileName = executable;
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
