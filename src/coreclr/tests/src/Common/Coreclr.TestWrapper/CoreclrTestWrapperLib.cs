// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
namespace CoreclrTestLib
{
    public class CoreclrTestWrapperLib
    {
        public const int EXIT_SUCCESS_CODE = 0;

        public int RunTest(string cmdLine, string outputfile, string errorfile)
        {
            System.IO.TextWriter output_file = new System.IO.StreamWriter(new FileStream(outputfile, FileMode.Create));
            System.IO.TextWriter err_file = new System.IO.StreamWriter(new FileStream(errorfile, FileMode.Create));

            int exitCode = -100;
            int timeout = 1000 * 60*10;
            using (Process process = new Process())
            {
                process.StartInfo.FileName = cmdLine;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                                

                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            try
                            {
                                outputWaitHandle.Set();
                            }
                            catch (ObjectDisposedException)
                            {
                                // Noop for access after timeout.
                            }
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            try
                            {
                                errorWaitHandle.Set();
                            }
                            catch (ObjectDisposedException)
                            {
                                // Noop for access after timeout.
                            }
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (process.WaitForExit(timeout) &&
                        outputWaitHandle.WaitOne(timeout) &&
                        errorWaitHandle.WaitOne(timeout))
                    {
                        // Process completed. Check process.ExitCode here.
                        exitCode = process.ExitCode;
                    }
                    else
                    {
                        // Timed out.
                        output.AppendLine("cmdLine:" + cmdLine + " Timed Out");
                        error.AppendLine("cmdLine:" + cmdLine + " Timed Out");
                    }

                   output_file.WriteLine(output.ToString());
                   output_file.WriteLine("Test Harness Exitcode is : " + exitCode.ToString());
                   output_file.Flush();

                   err_file.WriteLine(error.ToString());
                   err_file.Flush();

                   output_file.Dispose();
                   err_file.Dispose();
                }
            }

            return exitCode;
        }

        
    }
}
