using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreclrTestLib
{
    public class MobileAppHandler
    {
        // See https://github.com/dotnet/xharness/blob/main/src/Microsoft.DotNet.XHarness.Common/CLI/ExitCode.cs
        // 78 - PACKAGE_INSTALLATION_FAILURE
        // 81 - DEVICE_NOT_FOUND
        // 82 - RETURN_CODE_NOT_SET
        // 83 - APP_LAUNCH_FAILURE
        // 84 - DEVICE_FILE_COPY_FAILURE
        // 86 - PACKAGE_INSTALLATION_TIMEOUT
        // 88 - SIMULATOR_FAILURE
        // 89 - DEVICE_FAILURE
        // 90 - APP_LAUNCH_TIMEOUT
        // 91 - ADB_FAILURE
        private static readonly int[] _knownExitCodes = new int[] { 78, 81, 82, 83, 84, 86, 88, 89, 90, 91 };

        public int InstallMobileApp(string platform, string category, string testBinaryBase, string reportBase, string targetOS)
        {
            return HandleMobileApp("install", platform, category, testBinaryBase, reportBase, targetOS);
        }

        public int UninstallMobileApp(string platform, string category, string testBinaryBase, string reportBase, string targetOS)
        {
            return HandleMobileApp("uninstall", platform, category, testBinaryBase, reportBase, targetOS);
        }

        private static int HandleMobileApp(string action, string platform, string category, string testBinaryBase, string reportBase, string targetOS)
        {
            int exitCode = -100;

            string outputFile = Path.Combine(reportBase, action, $"{category}_{action}.output.txt");
            string errorFile = Path.Combine(reportBase, action, $"{category}_{action}.error.txt");
            bool platformValueFlag = true;
            bool actionValueFlag = true;

            Directory.CreateDirectory(Path.Combine(reportBase, action));
            var outputStream = new FileStream(outputFile, FileMode.Create);
            var errorStream = new FileStream(errorFile, FileMode.Create);

            using (var outputWriter = new StreamWriter(outputStream))
            using (var errorWriter = new StreamWriter(errorStream))
            {
                if ((platform != "android") && (platform != "apple"))
                {
                    outputWriter.WriteLine($"Incorrect value of platform. Provided {platform}. Valid strings are android and apple.");
                    platformValueFlag = false;
                }

                if ((action != "install") && (action != "uninstall"))
                {
                    outputWriter.WriteLine($"Incorrect value of action. Provided {action}. Valid strings are install and uninstall.");
                    actionValueFlag = false;
                }

                if (platformValueFlag && actionValueFlag)
                {
                    int timeout = 240000; // Set timeout to 4 mins, because the installation on Android arm64/32 devices could take up to 10 mins on CI
                    string dotnetCmd_raw = System.Environment.GetEnvironmentVariable("__TestDotNetCmd");
                    string xharnessCmd_raw = System.Environment.GetEnvironmentVariable("XHARNESS_CLI_PATH");
                    string dotnetCmd = string.IsNullOrEmpty(dotnetCmd_raw) ? "dotnet" : dotnetCmd_raw;
                    string xharnessCmd = string.IsNullOrEmpty(xharnessCmd_raw) ? "xharness" : $"exec {xharnessCmd_raw}";
                    string appExtension = platform == "android" ? "apk" : "app";

                    string cmdStr = $"{dotnetCmd} {xharnessCmd} {platform} {action}";

                    if (platform == "android")
                    {
                        cmdStr += $" --package-name=net.dot.{category}";

                        if (action == "install")
                        {
                            cmdStr += $" --app={testBinaryBase}/{category}.{appExtension} --output-directory={reportBase}/{action}";
                        }
                    }
                    else // platform is apple
                    {
                        string targetString = "";

                        switch (targetOS) {
                            case "ios":
                                targetString = "ios-device";
                                break;
                            case "iossimulator":
                                targetString = "ios-simulator-64";
                                break;
                            case "tvos":
                                targetString = "tvos-device";
                                break;
                            case "tvossimulator":
                                targetString = "tvos-simulator";
                                break;
                        }

                        cmdStr += $" --output-directory={reportBase}/{action} --target={targetString}";

                        if (action == "install")
                        {
                            cmdStr += $" --app={testBinaryBase}/{category}.{appExtension}";
                        }
                        else // action is uninstall
                        {
                            cmdStr += $" --app=net.dot.{category}";
                        }
                    }

                    if (action == "install")
                    {
                        cmdStr += " --timeout 00:02:30";
                    }

                    using (Process process = new Process())
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            process.StartInfo.FileName = "cmd.exe";
                        }
                        else
                        {
                            process.StartInfo.FileName = "/bin/bash";
                        }

                        process.StartInfo.Arguments = ConvertCmd2Arg(cmdStr);
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;

                        DateTime startTime = DateTime.Now;
                        process.Start();

                        var cts = new CancellationTokenSource();
                        Task copyOutput = process.StandardOutput.BaseStream.CopyToAsync(outputStream, 4096, cts.Token);
                        Task copyError = process.StandardError.BaseStream.CopyToAsync(errorStream, 4096, cts.Token);

                        if (process.WaitForExit(timeout))
                        {
                            // Process completed.
                            exitCode = process.ExitCode;
                            CheckExitCode(exitCode, testBinaryBase, category, outputWriter);
                            Task.WaitAll(copyOutput, copyError);
                        }
                        else
                        {
                            //Time out.
                            DateTime endTime = DateTime.Now;

                            try
                            {
                                cts.Cancel();
                            }
                            catch {}

                            outputWriter.WriteLine("\ncmdLine:{0} Timed Out (timeout in milliseconds: {1}, start: {2}, end: {3})",
                                    cmdStr, timeout, startTime.ToString(), endTime.ToString());
                            errorWriter.WriteLine("\ncmdLine:{0} Timed Out (timeout in milliseconds: {1}, start: {2}, end: {3})",
                                    cmdStr, timeout, startTime.ToString(), endTime.ToString());

                            process.Kill(entireProcessTree: true);
                        }
                    }
                }

                outputWriter.WriteLine("xharness exitcode is : " + exitCode.ToString());
                outputWriter.Flush();
                errorWriter.Flush();
            }

            return exitCode;
        }

        private static string ConvertCmd2Arg(string cmd)
        {
            cmd.Replace("\"", "\"\"");

            string cmdPrefix;
            if(OperatingSystem.IsWindows())
            {
                cmdPrefix = "/c";
            }
            else
            {
                cmdPrefix = "-c";
            }

            return $"{cmdPrefix} \"{cmd}\"";
        }

        private static void CreateRetryFile(string fileName, int exitCode, string appName)
        {
            using (StreamWriter writer = new StreamWriter(fileName))  
            {
                writer.WriteLine($"appName: {appName}; exitCode: {exitCode}"); 
            }
        }

        public static void CheckExitCode(int exitCode, string testBinaryBase, string category, StreamWriter outputWriter)
        {
            if (_knownExitCodes.Contains(exitCode))
            {
                CreateRetryFile($"{testBinaryBase}/.retry", exitCode, category);
                outputWriter.WriteLine("\nInfra issue was detected and a work item retry was requested");
            }
        }

        public static bool IsRetryRequested(string testBinaryBase)
        {
            return File.Exists($"{testBinaryBase}/.retry");
        }
    }
}
