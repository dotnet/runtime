using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

class Program
{

    private static void UnhandledException(string msg)
    {
#if (WINDOWS)
        DisableErrorDialog();
#endif
        A(msg);
    }

    private static void FailFast(string msg)
    {
#if (WINDOWS)
        DisableErrorDialog();
#endif
        try
        {
            A(msg);
        }
        catch (ArgumentException ex)
        {
            Environment.FailFast(msg, ex);
        }
    }

    private static void A(string msg)
    {
        B(msg);
    }

    private static void B(string msg)
    {
        throw new ArgumentException(msg);
    }

    private static string RandomCookie()
    {
        // Generate a random cookie to be used as a message to be passed as exception parameter
        Random random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 10)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static bool LaunchTest(string testName, string[] logEntriesToCheck, string randomCookie)
    {
        EventLog logBefore = new EventLog("Application");
        int logBeforeCount = logBefore.Entries.Count;

        DateTime dt = DateTime.Now;

        Process testProcess = new Process();
        string currentPath = Directory.GetCurrentDirectory();
        string corerunPath = Environment.GetEnvironmentVariable("CORE_ROOT") + "\\corerun.exe";

        testProcess.StartInfo.FileName = corerunPath;
        testProcess.StartInfo.Arguments = currentPath + "\\WindowsEventLog.exe " + testName + " " + randomCookie;
        testProcess.StartInfo.EnvironmentVariables["CORE_ROOT"] = Environment.GetEnvironmentVariable("CORE_ROOT");
        testProcess.StartInfo.UseShellExecute = false;

        testProcess.Start();
        testProcess.WaitForExit();

        Thread.Sleep(2000);

        EventLog logAfter = new EventLog("Application");

        Console.WriteLine("Found {0} entries in Event Log", logAfter.Entries.Count);

        foreach (EventLogEntry entry in logAfter.Entries)
        {
            int checkCount = 0;
            if (entry.TimeGenerated > dt.AddMinutes(-3))  // Grant some leeway in the time error was logged
            {
                String source = entry.Source;
                String message = entry.Message;

                foreach (string logEntry in logEntriesToCheck)
                {
                    Console.WriteLine("Checking for existence of : " + logEntry);
                    if (message.Contains(logEntry))
                        checkCount += 1;
                    else
                        Console.WriteLine("Couldn't find it in: " + message);
                }

                if (source.Contains(".NET Runtime") && checkCount == logEntriesToCheck.Length && logBeforeCount < logAfter.Entries.Count)
                {
                    return true;
                }
                else if (source.Contains(".NET Runtime"))
                {
                    Console.WriteLine("***      Event Log       ***");
                    Console.WriteLine(message);
                }
            }
        }
        return false;
    }

#if (WINDOWS)
    [DllImport("kernel32.dll")]
    private static extern int SetErrorMode(uint uMode);

    private static void DisableErrorDialog()
    {
        uint SEM_NOGPFAULTERRORBOX = 0x0002;
        SetErrorMode(SEM_NOGPFAULTERRORBOX);
    }
#endif

    private static bool RunUnhandledExceptionTest()
    {
        string cookie = RandomCookie();
        string[] logEntriesToCheck = { "unhandled exception", "ArgumentException", cookie };
        return LaunchTest("UnhandledException", logEntriesToCheck, cookie);
    }

    private static bool RunFailFastTest()
    {
        string cookie = RandomCookie();
        string[] logEntriesToCheck = { "The application requested process termination through System.Environment.FailFast(string message).", "ArgumentException", cookie };
        return LaunchTest("FailFast", logEntriesToCheck, cookie);
    }


    public static int Main(string[] args)
    {
        if (args.Length == 0) // When invoked with no args, launch itself with appropriate args to cause various exceptions
        {
            if (!System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows"))
            {
                Console.WriteLine("WindowsEventLog Test: Passing on all non-Windows platform");
                return 100;
            }

            if (!RunUnhandledExceptionTest())
            {
                Console.WriteLine("WindowsEventLog Test: UnhandledExceptionTest failed.");
                return 1;
            }

            if (!RunFailFastTest())
            {
                Console.WriteLine("WindowsEventLog Test: FailFastTest failed.");
                return 1;
            }

            return 100;
        }

        Debug.Assert(args.Length == 2);

        if (args[0] == "UnhandledException")
        {
            UnhandledException(args[1]);
        }
        else if (args[0] == "FailFast")
        {
            FailFast(args[1]);
        }

        return 100; // Should never reach here 
    }
}
