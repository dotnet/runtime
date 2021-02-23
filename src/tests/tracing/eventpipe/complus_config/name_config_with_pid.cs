using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

class NameConfigWithPid
{
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "waitforinput")
        {
            Console.Error.WriteLine("WaitingForInput in ErrorStream");
            Console.WriteLine("WaitingForInput");
            Console.ReadLine();
            return 100;
        }
        else
        {
            Process process = new Process();
            string corerun = Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), "corerun");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                corerun = corerun + ".exe";

            // Use dll directory as temp directory
            string tempDir = Path.GetDirectoryName(typeof(NameConfigWithPid).Assembly.Location);
            string outputPathPattern = Path.Combine(tempDir, "eventPipeStream{pid}_{pid}.nettrace");

            foreach (string oldFile in Directory.EnumerateFiles(tempDir, "eventPipeStream*"))
            {
                File.Delete(oldFile);
            }

            process.StartInfo.FileName = corerun;
            process.StartInfo.Arguments = typeof(NameConfigWithPid).Assembly.Location + " waitforinput";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;

            process.StartInfo.Environment.Add("COMPlus_EnableEventPipe", "1");
            process.StartInfo.Environment.Add("COMPlus_EventPipeConfig", "Microsoft-Windows-DotNETRuntime:4c14fccbd:4");
            process.StartInfo.Environment.Add("COMPlus_EventPipeOutputPath", outputPathPattern);

            process.Start();

            process.StandardError.ReadLine();
            uint pid = (uint)process.Id;
            string expectedPath = outputPathPattern.Replace("{pid}", pid.ToString());

            process.StandardInput.WriteLine("input");
            process.WaitForExit();
            if (!File.Exists(expectedPath))
            {
                Console.WriteLine($"{expectedPath} not found");
                return 1;
            }
            else
            {
                Console.WriteLine($"{expectedPath} found");
                return 100; 
            }
        }
    }
}