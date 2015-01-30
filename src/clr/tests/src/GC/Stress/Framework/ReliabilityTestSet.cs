// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class ReliabilityTestSet
{
    int maximumTestLoops = 0;									// default run based on time.
    int maximumExecutionTime = 60;	                        // 60 minute run by default.
    int percentPassIsPass = System.Environment.GetEnvironmentVariable("PERCENTPASSISPASS") == null ? -1 : Convert.ToInt32(System.Environment.GetEnvironmentVariable("PERCENTPASSISPASS"));
    int[] minPercentCPUStaggered_times = null;
    int[] minPercentCPUStaggered_usage = null;
    int minimumCPUpercent = 0, minimumMemoryPercent = 0, minimumTestsRunning = 0, maximumTestsRunning = -1;			// minimum CPU & memory requirements.
    ReliabilityTest[] tests;
    string[] discoveryPaths = null;
    string friendlyName;
    bool enablePerfCounters = true, disableLogging = false, installDetours = false;
    bool suppressConsoleOutputFromTests = false;
    bool debugBreakOnTestHang = true;
    bool debugBreakOnBadTest = false;
    bool debugBreakOnOutOfMemory = false;
    bool debugBreakOnPathTooLong = false;
    bool debugBreakOnMissingTest = false;
    TestStartModeEnum testStartMode = TestStartModeEnum.AppDomainLoader;
    private string defaultDebugger, defaultDebuggerOptions;
    int ulGeneralUnloadPercent = 0, ulAppDomainUnloadPercent = 0, ulAssemblyLoadPercent = 0, ulWaitTime = 0;
    bool reportResults = false;
    string reportResultsTo = "http://clrqa/SmartAPI/result.asmx";
    Guid bvtCategory = Guid.Empty;
    string ccFailMail;
    AppDomainLoaderMode adLoaderMode = AppDomainLoaderMode.Normal;
    int numAppDomains = 10; //used for roundRobin scheduling, our app domain index
    Random rand = new Random();
    LoggingLevels loggingLevel = LoggingLevels.All;	// by default log everything


    public ReliabilityTest[] Tests
    {
        get
        {
            return (tests);
        }

        set
        {
            tests = value;
        }
    }

    public int MaximumLoops
    {
        get
        {
            return (maximumTestLoops);
        }
        set
        {
            maximumTestLoops = value;
        }
    }

    public LoggingLevels LoggingLevel
    {
        get
        {
            return (loggingLevel);
        }
        set
        {
            loggingLevel = value;
        }
    }
    /// <summary>
    /// Maximum execution time, in minutes.
    /// </summary>
    public int MaximumTime
    {
        get
        {
            return (maximumExecutionTime);
        }
        set
        {
            maximumExecutionTime = value;
        }
    }

    public string FriendlyName
    {
        get
        {
            return (friendlyName);
        }
        set
        {
            friendlyName = value;
        }
    }
    public string[] DiscoveryPaths
    {
        get
        {
            return (discoveryPaths);
        }
        set
        {
            discoveryPaths = value;
        }
    }

    public int MinPercentCPU
    {
        get
        {
            return (minimumCPUpercent);
        }
        set
        {
            minimumCPUpercent = value;
        }

    }

    public int GetCurrentMinPercentCPU(TimeSpan timeRunning)
    {
        if (minPercentCPUStaggered_usage == null)
        {
            return (MinPercentCPU);
        }

        int curCpu = minPercentCPUStaggered_usage[0];
        int curTime = 0;
        for (int i = 1; i < minPercentCPUStaggered_usage.Length; i++)
        {
            curTime += minPercentCPUStaggered_times[i - 1];
            if (curTime > timeRunning.TotalMinutes)
            {
                // we're in this time zone, return our current cpu
                return (curCpu);
            }

            // we're in a later time zone, keep looking...

            curCpu = minPercentCPUStaggered_usage[i];
        }
        return (curCpu);
    }

    public string MinPercentCPUStaggered
    {
        set
        {
            string[] minPercentCPUStaggered = value.Split(';');
            minPercentCPUStaggered_times = new int[minPercentCPUStaggered.Length];
            minPercentCPUStaggered_usage = new int[minPercentCPUStaggered.Length];

            for (int i = 0; i < minPercentCPUStaggered.Length; i++)
            {

                string[] split = minPercentCPUStaggered[i].Split(':');
                string time = split[0];
                string usage = split[1];

                minPercentCPUStaggered_times[i] = GetValue(time);
                minPercentCPUStaggered_usage[i] = GetValue(usage);
            }
        }
    }

    /// <summary>
    /// Gets the integer value or the random value specified in the string.

    /// accepted format:
    ///          30:50            run for 30 minutes at 50% usage
    ///          rand(30,60):50   run at 50% usage for somewhere between 30 and 60 minutes.
    ///          30:rand(50, 75)  run for 30 minutes at somewhere between 50 and 75 % CPU usage
    ///          rand(30, 60) : rand(50, 75) run for somewhere between 30-60 minutes at 50-75% CPU usage.
    /// </summary>
    /// <param name="times"></param>
    /// <returns></returns>
    int GetValue(string times)
    {
        times = times.Trim();
        if (String.Compare(times, 0, "rand(", 0, 5) == 0)
        {
            string trimmedTimes = times.Substring(5).Trim();
            string[] values = trimmedTimes.Split(new char[] { ',' });
            int min = Convert.ToInt32(values[0]);
            int max = Convert.ToInt32(values[1].Substring(0, values[1].Length - 1));   // remove the ending )
            return (rand.Next(min, max));
        }
        else
        {
            return (Convert.ToInt32(times));
        }
    }

    public int MinPercentMem
    {
        get
        {
            return (minimumMemoryPercent);
        }
        set
        {
            minimumMemoryPercent = value;
        }
    }


    public int MinTestsRunning
    {
        get
        {
            return (minimumTestsRunning);
        }
        set
        {
            minimumTestsRunning = value;
        }
    }

    public int MaxTestsRunning
    {
        get
        {
            return (maximumTestsRunning);
        }
        set
        {
            maximumTestsRunning = value;
        }
    }

    public bool EnablePerfCounters
    {
        get
        {
#if PROJECTK_BUILD
            return false;
#else
            if (enablePerfCounters)
            {
                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    enablePerfCounters = false;
                }
            }
            return (enablePerfCounters);
#endif
        }
        set
        {
            enablePerfCounters = value;
        }
    }

    public bool DisableLogging
    {
        get
        {
            return (disableLogging);
        }

        set
        {
            disableLogging = value;
        }
    }

    public bool InstallDetours
    {
        get
        {
            return (installDetours);
        }
        set
        {
            installDetours = value;
        }
    }

    public TestStartModeEnum DefaultTestStartMode
    {
        get
        {
            return (testStartMode);
        }
        set
        {
            testStartMode = value;
        }
    }

    public int PercentPassIsPass
    {
        get
        {
            return (percentPassIsPass);
        }
        set
        {
            percentPassIsPass = value;
        }
    }

    public AppDomainLoaderMode AppDomainLoaderMode
    {
        get
        {
            return (this.adLoaderMode);
        }
        set
        {
            adLoaderMode = value;
        }
    }

    /// <summary>
    /// Used for round-robin scheduling.  Number of app domains to schedule tests into.
    /// </summary>
    public int NumAppDomains
    {
        get
        {
            return (numAppDomains);
        }
        set
        {
            numAppDomains = value;
        }
    }

    public string DefaultDebugger
    {
        get
        {
            return (defaultDebugger);
        }
        set
        {
            defaultDebugger = value;
        }
    }

    public string DefaultDebuggerOptions
    {
        get
        {
            return (defaultDebuggerOptions);
        }
        set
        {
            defaultDebuggerOptions = value;
        }
    }
    public int ULGeneralUnloadPercent
    {
        get
        {
            return (ulGeneralUnloadPercent);
        }

        set
        {
            ulGeneralUnloadPercent = value;
        }
    }
    public int ULAppDomainUnloadPercent
    {
        get
        {
            return (ulAppDomainUnloadPercent);
        }

        set
        {
            ulAppDomainUnloadPercent = value;
        }
    }
    public int ULAssemblyLoadPercent
    {
        get
        {
            return (ulAssemblyLoadPercent);
        }

        set
        {
            ulAssemblyLoadPercent = value;
        }
    }
    public int ULWaitTime
    {
        get
        {
            return (ulWaitTime);
        }

        set
        {
            ulWaitTime = value;
        }
    }
    public bool ReportResults
    {
        get
        {
            if (reportResults && Environment.GetEnvironmentVariable("RF_NOREPORT") == null)
            {
                reportResults = false;
            }
            return (reportResults);
        }
        set
        {
            reportResults = value;
        }
    }
    public string ReportResultsTo
    {
        get
        {
            return (reportResultsTo);
        }
        set
        {
            reportResultsTo = value;
        }
    }
    public Guid BvtCategory
    {
        get
        {
            return (bvtCategory);
        }
        set
        {
            bvtCategory = value;
        }
    }

    public string CCFailMail
    {
        get
        {
            return (ccFailMail);
        }
        set
        {
            ccFailMail = value;
        }
    }

    public bool SuppressConsoleOutputFromTests
    {
        get
        {
            return suppressConsoleOutputFromTests;
        }
        set
        {
            suppressConsoleOutputFromTests = value;
        }
    }

    public bool DebugBreakOnTestHang
    {
        get
        {
            return debugBreakOnTestHang;
        }
        set
        {
            debugBreakOnTestHang = value;
        }
    }

    public bool DebugBreakOnBadTest
    {
        get
        {
            return debugBreakOnBadTest;
        }
        set
        {
            debugBreakOnBadTest = value;
        }
    }

    public bool DebugBreakOnOutOfMemory
    {
        get
        {
            return debugBreakOnOutOfMemory;
        }
        set
        {
            debugBreakOnOutOfMemory = value;
        }
    }

    public bool DebugBreakOnPathTooLong
    {
        get
        {
            return debugBreakOnPathTooLong;
        }
        set
        {
            debugBreakOnPathTooLong = value;
        }
    }

    public bool DebugBreakOnMissingTest
    {
        get
        {
            return debugBreakOnMissingTest;
        }
        set
        {
            debugBreakOnMissingTest = value;
        }
    }
}
