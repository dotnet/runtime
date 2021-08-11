// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#define USE_INSTRUMENTATION
using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Collections.Generic;

/// <summary>
/// Classed used for all logging infrastructure
/// </summary>
internal class RFLogging
#if !PROJECTK_BUILD
    : MarshalByRefObject
#endif
{
    private Queue<string> _messageQueue;
    private Queue<string> _instrumentationMessageQueue;
    private Thread _loggingThread;
    private volatile bool _closeLogFile;

    private ASCIIEncoding _encoding = new ASCIIEncoding();
    private bool _noStatusWarningDisplayed = false;
    private FileStream _logFile = null;
    private bool _reportResults = true;
#if USE_INSTRUMENTATION
    private FileStream _instrumentationLogFile = null;
#endif

    private const string logDirectory = "Logs";

    public RFLogging()
    {
        CreateInstrumentationLog();

        _messageQueue = new Queue<string>(25000);
        _instrumentationMessageQueue = new Queue<string>(25000);
        _closeLogFile = false;
        _loggingThread = new Thread(new ThreadStart(LogWorker));
        _loggingThread.IsBackground = true;
#if !PROJECTK_BUILD
        loggingThread.Priority = ThreadPriority.Highest;
#endif
        _loggingThread.Start();
    }

    private void LogWorker()
    {
        while (true)
        {
            bool cachedCloseLogFile = _closeLogFile; // The CloseLog method will set closeLogFile to true indicating we should close the log file
                                                     // This value is cached here so we can write all of the remaining messages to log before closing it
            int messageQueueCount = _messageQueue.Count;
            int instrumentationQueueCount = _instrumentationMessageQueue.Count;

            if (_logFile != null && messageQueueCount > 0)
            {
                try
                {
                    string text;
                    for (int i = messageQueueCount; i > 0; i--)
                    {
                        try
                        {
                            lock (_messageQueue)
                            {
                                text = _messageQueue.Dequeue();
                            }
                        }
                        catch (InvalidOperationException) { text = null; }

                        if (!String.IsNullOrEmpty(text))
                        {
                            _logFile.Write(_encoding.GetBytes(text), 0, text.Length);
                            text = null;
                        }
                    }
                    _logFile.Flush();
                }
                catch (IOException e)
                {
                    ReliabilityFramework.MyDebugBreak(String.Format("LogWorker IOException:{0}", e.ToString()));
                    //Disk may be full so simply stop logging
                }
            }


            if (cachedCloseLogFile)
            {
                if (null != _logFile)
                {
#if !PROJECTK_BUILD
                    logFile.Close();
#endif
                    _logFile = null;
                }
                _closeLogFile = false;
            }

            if (_instrumentationLogFile != null && instrumentationQueueCount > 0)
            {
                try
                {
                    string text;
                    for (int i = instrumentationQueueCount; i > 0; i--)
                    {
                        try
                        {
                            lock (_instrumentationMessageQueue)
                            {
                                text = _instrumentationMessageQueue.Dequeue();
                            }
                        }
                        catch (InvalidOperationException) { text = null; }

                        if (!String.IsNullOrEmpty(text))
                        {
                            _instrumentationLogFile.Write(_encoding.GetBytes(text), 0, text.Length);
                            text = null;
                        }
                    }
                    _instrumentationLogFile.Flush();
                }
                catch (IOException e)
                {
                    ReliabilityFramework.MyDebugBreak(String.Format("LogWorker IOException:{0}", e.ToString()));
                }
            }
        }
    }

    private void CreateInstrumentationLog()
    {
        if (_instrumentationLogFile == null)
        {
            try
            {
                string logFilename = Path.Combine(logDirectory, "instrmentation.log");
                while (File.Exists(logFilename))
                {
                    logFilename = Path.Combine(logDirectory, "instrmentation.log-" + DateTime.Now.ToString().Replace('/', '-').Replace(':', '.'));
                }

                string logDirname = Path.GetDirectoryName(logFilename);
                if (!Directory.Exists(logDirname))
                    Directory.CreateDirectory(logDirname);
                _instrumentationLogFile = File.Open(logFilename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            catch
            {
                _instrumentationLogFile = null;
                return;
            }
        }
    }

    /// <summary>
    /// This method will open our log file for writing.  If for any reason we can't open the log file the error is printed on the screen
    /// and we'll simply refuse to log the data.   The log file's name is the current test's friendly name.  If a log file is already opened
    /// that log file will be closed & replaced with this one.
    /// </summary>
    public void OpenLog(string name)
    {
        if (_logFile != null)
        {
#if !PROJECTK_BUILD
            logFile.Close();
#endif
            _logFile = null;
        }
        // open the log file if the user hasn't disabled it.
        string filename = null;
        try
        {
            bool fRetry;
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            do
            {
                fRetry = false;

                string safeName = Path.Combine(logDirectory, name.Replace('\\', ' ').Replace('*', ' ').Replace('?', ' ').Replace('>', ' ').Replace('<', ' ').Replace('|', ' ').Replace(':', ' ').Replace('/', ' ').Replace('"', ' '));
                filename = safeName + ".log";
                if (File.Exists(filename))
                {
                    filename = String.Format("{0} - {1}.log", safeName, DateTime.Now.ToString().Replace('/', '-').Replace(':', '.'));
                }
                try
                {
                    string dirname = Path.GetDirectoryName(filename);
                    if (!Directory.Exists(dirname))
                        Directory.CreateDirectory(dirname);
                    _logFile = File.Open(filename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                catch (IOException e)
                {
                    Console.WriteLine("Failed to open file: {0} {1}", filename, e);
                    fRetry = true;
                }
            }
            while (fRetry);
            WriteToLog("<TestRun>\r\n");
        }
        catch (ArgumentException)
        {
            Console.WriteLine("RFLogging - Blank or Empty FriendlyName, logging is disabled: {0}", name);
            _logFile = null;
        }
        catch (PathTooLongException)
        {
            Console.WriteLine("RFLogging - FriendlyName is too long, logging is disabled: {0}", name);
            _logFile = null;
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.WriteLine(ex.ToString());
            Console.WriteLine("RFLogging - Friendly name contains drive or directory specifiers, logging is disabled: {0} {1}", name, filename);
            _logFile = null;
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("RFLogging - Unauthorized access to log file, please change the test name or fix the current directory: {0}.log", name);
            _logFile = null;
        }
        catch (NotSupportedException)
        {
            Console.WriteLine("RFLogging - The friendly test name contains a : in the string, try again: {0}", name);
            _logFile = null;
        }
    }

    /// <summary>
    /// Closes the main stress log file
    /// </summary>
    public void CloseLog()
    {
        if (null != _logFile)
        {
            WriteToLog("</TestRun>\r\n");

            _closeLogFile = true;

            for (int i = 0; i < 60 && _closeLogFile; ++i)
            {
                Thread.Sleep(100);
            }

            if (_closeLogFile)
            {
                throw new InvalidOperationException("Log was not closed after waiting 1 minute.");
            }
        }
    }

    /// <summary>
    /// Writes the StartupInfo element of the stress log.
    /// </summary>
    /// <param name="value"></param>
    public void WriteStartupInfo(int randSeed)
    {
        WriteToLog("    <StartupInfo>\r\n        <RandomSeed>" + randSeed.ToString() + "</RandomSeed>\r\n    </StartupInfo>\r\n");
    }

    /// <summary>
    /// Writes the performance stats into the stress log
    /// </summary>
    public void WritePerfStats(float pagesVal, float pageFaultsVal, float ourPageFaultsVal, float cpuVal, float memVal, bool testStartPrevented)
    {
        WriteToLog(
            String.Format("    <PerfStats CPU=\"{0}\" Pages=\"{1}\" PageFaults=\"{2}\" OurPageFaults=\"{3}\" TestStartPrevented=\"{4}\" />\r\n",
            cpuVal,
            pagesVal,
            pageFaultsVal,
            ourPageFaultsVal,
            testStartPrevented
            ));
    }

    /// <summary>
    /// Writes the start of a test into the log
    /// </summary>
    public void WriteTestStart(ReliabilityTest test)
    {
        WriteToLog(String.Format("    <TestStart DateTime=\"{0}\" TestId=\"{1}\" /> \r\n", DateTime.Now, test.RefOrID));
    }

    /// <summary>
    /// Writes the detection of a race into the log
    /// </summary>
    public void WriteTestRace(ReliabilityTest test)
    {
        WriteToLog(String.Format("<TestRace DateTime=\"{0}\" TestId=\"\"/>\r\n", DateTime.Now, test.RefOrID));
    }

    /// <summary>
    /// Writes a pre-command failure into the log
    /// </summary>
    public void WritePreCommandFailure(ReliabilityTest test, string command, string commandType)
    {
        WriteToLog(String.Format("    <PreCommandFailure Command=\"\" CommandType=\"{1}\" TestId=\"{2}\"",
            command,
            commandType,
            test.RefOrID));
    }

    /// <summary>
    /// Writes a test failure into the log
    /// </summary>
    public void WriteTestFail(ReliabilityTest test, string message)
    {
        string testName = (test == null) ? "Harness" : test.RefOrID;
        WriteToLog(String.Format("    <TestFail DateTime=\"{0}\" TestId=\"{1}\" Description=\"{2}\" />\r\n",
            DateTime.Now,
            testName,
            message));
    }

    /// <summary>
    /// Writes a test pass into the log.
    /// </summary>
    public void WriteTestPass(ReliabilityTest test, string message)
    {
        string testName = (test == null) ? "Harness" : test.RefOrID;
        WriteToLog(String.Format("    <TestPass DateTime=\"{0}\" TestId=\"{1}\" Description=\"{2}\" />\r\n",
            DateTime.Now,
            testName,
            message));
    }

    // Updates the stress information with the latest time stamp
    public void RecordTimeStamp()
    {
        if (File.Exists(Environment.ExpandEnvironmentVariables("%SCRIPTSDIR%\\record.js")))
        {
            ProcessStartInfo psi = new ProcessStartInfo("cscript.exe", Environment.ExpandEnvironmentVariables("//b //nologo %SCRIPTSDIR%\\record.js -i %STRESSID% -a UPDATE_RECORD -s RUNNING"));
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            Process p = Process.Start(psi);
            p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            p.Dispose();
        }
    }



    /// <summary>
    /// This writes a line of text to the log file.  If the log file is not opened no action is taken.
    /// </summary>
    /// <param name="text">the text to write to the logfile</param>
    private void WriteToLog(string text)
    {
        WriteToReport();

        if (_logFile != null)
        {
            try
            {
                lock (_messageQueue)
                {
                    _messageQueue.Enqueue(text);
                }
            }
            catch (IOException) { /*Eat exceptions for IO */ }
            catch (InvalidOperationException) { /*Eat exceptions if we can't queue */}
        }
    }

    private void WriteToReport()
    {
        if (ReportResults)
        {
            try
            {
                if (File.Exists(Environment.ExpandEnvironmentVariables("%SCRIPTSDIR%\\record.js")))
                {
                    ProcessStartInfo psi = new ProcessStartInfo("cscript.exe", Environment.ExpandEnvironmentVariables("//b //nologo %SCRIPTSDIR%\\record.js -i %STRESSID% -a UPDATE_RECORD -s RUNNING"));
                    psi.UseShellExecute = false;
                    psi.RedirectStandardOutput = true;

                    Process p = Process.Start(psi);
                    p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        string msg = String.Format("cscript.exe " + Environment.ExpandEnvironmentVariables("//b //nologo %SCRIPTSDIR%\\record.js -i %STRESSID% -a UPDATE_RECORD -s RUNNING\r\nWARNING: Status update did not return success!"), p.ExitCode);
                        WriteToInstrumentationLog(null, LoggingLevels.UrtFrameworks, msg);
                    }
                    p.Dispose();
                }
                else if (!_noStatusWarningDisplayed)
                {
                    _noStatusWarningDisplayed = true;
                    Console.WriteLine("WARNING: record.js does not exist, not updating status...");
                }
            }
            catch (Exception e)
            {
                string msg = String.Format("WARNING: Status update did not return success (exception thrown {0})!", e);
                WriteToInstrumentationLog(null, LoggingLevels.UrtFrameworks, msg);
                Console.WriteLine(msg);
            }
        }
    }

    public bool ReportResults
    {
        get
        {
            return (_reportResults);
        }
        set
        {
            _reportResults = value;
        }
    }

    public void LogNoResultReporter(bool fReportResults)
    {
        if (!_noStatusWarningDisplayed)
        {
            _noStatusWarningDisplayed = true;
            if (fReportResults)
            {
                Console.WriteLine("WARNING: record.js does not exist, not updating status...");
            }
        }
    }

    /// <summary>
    /// Writes a trace line to the instrumentation log.  The instrmentation log is primarily
    /// used for deeper understanding what has happened during the stress run.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="str"></param>
    public void WriteToInstrumentationLog(ReliabilityTestSet curTestSet, LoggingLevels level, string str)
    {
        if (curTestSet == null || (curTestSet.LoggingLevel & level) != 0)
        {
            str = String.Format("[{0} {2}] {1}\r\n", DateTime.Now.ToString(), str, Thread.CurrentThread.ManagedThreadId);
            try
            {
                lock (_instrumentationMessageQueue)
                {
                    _instrumentationMessageQueue.Enqueue(str);
                }
            }
            catch (IOException) { /*Eat exceptions for IO */ }
            catch (InvalidOperationException) { /*Eat exceptions if we can't queue */}
        }
    }
}

