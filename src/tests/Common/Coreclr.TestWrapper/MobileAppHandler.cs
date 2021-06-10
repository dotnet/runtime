using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CoreclrTestLib
{
    public class MobileAppHandler
    {
        public void InstallMobileApp(string platform, string category, string testBinaryBase, string reportBase)
        {
            HandleMobileApp("install", platform, category, testBinaryBase, reportBase);
        }

        public void UninstallMobileApp(string platform, string category, string testBinaryBase, string reportBase)
        {
            HandleMobileApp("uninstall", platform, category, testBinaryBase, reportBase);
        }

        private static void HandleMobileApp(string action, string platform, string category, string testBinaryBase, string reportBase)
        {
            //install or uninstall mobile app
            string outputFile = Path.Combine(reportBase, action, $"{category}_{action}.output.txt");
            string errorFile = Path.Combine(reportBase, action, $"{category}_{action}.error.txt");
            string dotnetCmd_raw = System.Environment.GetEnvironmentVariable("__TestDotNetCmd");
            string dotnetCmd;
            string xharnessCmd_raw = System.Environment.GetEnvironmentVariable("XHARNESS_CLI_PATH");
            string xharnessCmd;
            string cmdStr;
            string appExtension;
            int timeout = 240000; // Set timeout to 4 mins, because the installation on Android arm64/32 devices could take up to 4 mins on CI

            if(String.IsNullOrEmpty(dotnetCmd_raw))
            {
                dotnetCmd = "dotnet";
            }
            else
            {
                dotnetCmd = dotnetCmd_raw;
            }

            if(String.IsNullOrEmpty(xharnessCmd_raw))
            {
                xharnessCmd = "xharness";
            }
            else
            {
                xharnessCmd = $"exec {xharnessCmd_raw}";
            }

            if(platform == "android")
            {
                appExtension = "apk";
            }
            else
            {
                appExtension = "app";
            }

            cmdStr = $"{dotnetCmd} {xharnessCmd} {platform} {action} --package-name=net.dot.{category} --app={testBinaryBase}/{category}.{appExtension} --output-directory={reportBase}/install";

            Directory.CreateDirectory(Path.Combine(reportBase, action));
            var outputStream = new FileStream(outputFile, FileMode.Create);
            var errorStream = new FileStream(errorFile, FileMode.Create);

            using (var outputWriter = new StreamWriter(outputStream))
            using (var errorWriter = new StreamWriter(errorStream))
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
                    Task.WaitAll(copyOutput, copyError);
                }
                else
                {
                    //Time out
                    DateTime endTime = DateTime.Now;

                    try
                    {
                        cts.Cancel();
                    }
                    catch {}

                    outputWriter.WriteLine("\ncmdLine:{0} Timed Out (timeout in milliseconds: {1}, start: {3}, end: {4})",
                            cmdStr, timeout, startTime.ToString(), endTime.ToString());
                    errorWriter.WriteLine("\ncmdLine:{0} Timed Out (timeout in milliseconds: {1}, start: {3}, end: {4})",
                            cmdStr, timeout, startTime.ToString(), endTime.ToString());
                    
                    process.Kill(entireProcessTree: true);
                }

                outputWriter.Flush();
                errorWriter.Flush();
            }
        }

        private static string ConvertCmd2Arg(string cmd)
        {
            cmd.Replace("\"", "\"\"");
            var result = $"-c \"{cmd}\"";
            return result;
        }
    }
}
