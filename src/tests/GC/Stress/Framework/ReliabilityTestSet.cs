// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class ReliabilityTestSet
{
    private int _maximumTestLoops = 0;									// default run based on time.
    private int _maximumExecutionTime = 60;	                        // 60 minute run by default.
    private int _maximumWaitTime = 10;	                            // 10 minute wait by default.
    private int _percentPassIsPass = System.Environment.GetEnvironmentVariable("PERCENTPASSISPASS") == null ? -1 : Convert.ToInt32(System.Environment.GetEnvironmentVariable("PERCENTPASSISPASS"));
    private int[] _minPercentCPUStaggered_times = null;
    private int[] _minPercentCPUStaggered_usage = null;
    private int _minimumCPUpercent = 0, _minimumMemoryPercent = 0, _minimumTestsRunning = 0, _maximumTestsRunning = -1;			// minimum CPU & memory requirements.
    private ReliabilityTest[] _tests;
    private string[] _discoveryPaths = null;
    private string _friendlyName;
    private bool _enablePerfCounters = true, _disableLogging = false, _installDetours = false;
    private bool _suppressConsoleOutputFromTests = false;
    private bool _debugBreakOnTestHang = true;
    private bool _debugBreakOnBadTest = false;
    private bool _debugBreakOnOutOfMemory = false;
    private bool _debugBreakOnPathTooLong = false;
    private bool _debugBreakOnMissingTest = false;
    private TestStartModeEnum _testStartMode = TestStartModeEnum.AppDomainLoader;
    private string _defaultDebugger, _defaultDebuggerOptions;
    private int _ulGeneralUnloadPercent = 0, _ulAppDomainUnloadPercent = 0, _ulAssemblyLoadPercent = 0, _ulWaitTime = 0;
    private bool _reportResults = false;
    private string _reportResultsTo = "http://clrqa/SmartAPI/result.asmx";
    private Guid _bvtCategory = Guid.Empty;
    private string _ccFailMail;
    private AppDomainLoaderMode _adLoaderMode = AppDomainLoaderMode.Normal;
    private AssemblyLoadContextLoaderMode _alcLoaderMode = AssemblyLoadContextLoaderMode.Normal;
    private int _numAppDomains = 10; //used for roundRobin scheduling, our app domain index
    private int _numAssemblyLoadContexts = 10; //used for roundRobin scheduling, our AssemblyLoadContext index
    private Random _rand = new Random();
    private LoggingLevels _loggingLevel = LoggingLevels.All;	// by default log everything


    public ReliabilityTest[] Tests
    {
        get
        {
            return (_tests);
        }

        set
        {
            _tests = value;
        }
    }

    public int MaximumLoops
    {
        get
        {
            return (_maximumTestLoops);
        }
        set
        {
            _maximumTestLoops = value;
        }
    }

    public LoggingLevels LoggingLevel
    {
        get
        {
            return (_loggingLevel);
        }
        set
        {
            _loggingLevel = value;
        }
    }
    /// <summary>
    /// Maximum execution time, in minutes.
    /// </summary>
    public int MaximumTime
    {
        get
        {
            return (_maximumExecutionTime);
        }
        set
        {
            _maximumExecutionTime = value;
        }
    }

    /// <summary>
    /// Maximum wait time, in minutes.
    /// </summary>
    public int MaximumWaitTime
    {
        get
        {
            return (_maximumWaitTime);
        }
        set
        {
            _maximumWaitTime = value;
        }
    }

    public string FriendlyName
    {
        get
        {
            return (_friendlyName);
        }
        set
        {
            _friendlyName = value;
        }
    }
    public string[] DiscoveryPaths
    {
        get
        {
            return (_discoveryPaths);
        }
        set
        {
            _discoveryPaths = value;
        }
    }

    public int MinPercentCPU
    {
        get
        {
            return (_minimumCPUpercent);
        }
        set
        {
            _minimumCPUpercent = value;
        }
    }

    public int GetCurrentMinPercentCPU(TimeSpan timeRunning)
    {
        if (_minPercentCPUStaggered_usage == null)
        {
            return (MinPercentCPU);
        }

        int curCpu = _minPercentCPUStaggered_usage[0];
        int curTime = 0;
        for (int i = 1; i < _minPercentCPUStaggered_usage.Length; i++)
        {
            curTime += _minPercentCPUStaggered_times[i - 1];
            if (curTime > timeRunning.TotalMinutes)
            {
                // we're in this time zone, return our current cpu
                return (curCpu);
            }

            // we're in a later time zone, keep looking...

            curCpu = _minPercentCPUStaggered_usage[i];
        }
        return (curCpu);
    }

    public string MinPercentCPUStaggered
    {
        set
        {
            string[] minPercentCPUStaggered = value.Split(';');
            _minPercentCPUStaggered_times = new int[minPercentCPUStaggered.Length];
            _minPercentCPUStaggered_usage = new int[minPercentCPUStaggered.Length];

            for (int i = 0; i < minPercentCPUStaggered.Length; i++)
            {
                string[] split = minPercentCPUStaggered[i].Split(':');
                string time = split[0];
                string usage = split[1];

                _minPercentCPUStaggered_times[i] = GetValue(time);
                _minPercentCPUStaggered_usage[i] = GetValue(usage);
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
    private int GetValue(string times)
    {
        times = times.Trim();
        if (String.Compare(times, 0, "rand(", 0, 5) == 0)
        {
            string trimmedTimes = times.Substring(5).Trim();
            string[] values = trimmedTimes.Split(new char[] { ',' });
            int min = Convert.ToInt32(values[0]);
            int max = Convert.ToInt32(values[1].Substring(0, values[1].Length - 1));   // remove the ending )
            return (_rand.Next(min, max));
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
            return (_minimumMemoryPercent);
        }
        set
        {
            _minimumMemoryPercent = value;
        }
    }


    public int MinTestsRunning
    {
        get
        {
            return (_minimumTestsRunning);
        }
        set
        {
            _minimumTestsRunning = value;
        }
    }

    public int MaxTestsRunning
    {
        get
        {
            return (_maximumTestsRunning);
        }
        set
        {
            _maximumTestsRunning = value;
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
            _enablePerfCounters = value;
        }
    }

    public bool DisableLogging
    {
        get
        {
            return (_disableLogging);
        }

        set
        {
            _disableLogging = value;
        }
    }

    public bool InstallDetours
    {
        get
        {
            return (_installDetours);
        }
        set
        {
            _installDetours = value;
        }
    }

    public TestStartModeEnum DefaultTestStartMode
    {
        get
        {
            return (_testStartMode);
        }
        set
        {
            _testStartMode = value;
        }
    }

    public int PercentPassIsPass
    {
        get
        {
            return (_percentPassIsPass);
        }
        set
        {
            _percentPassIsPass = value;
        }
    }

    public AppDomainLoaderMode AppDomainLoaderMode
    {
        get
        {
            return (_adLoaderMode);
        }
        set
        {
            _adLoaderMode = value;
        }
    }

    public AssemblyLoadContextLoaderMode AssemblyLoadContextLoaderMode
    {
        get
        {
            return (_alcLoaderMode);
        }
        set
        {
            _alcLoaderMode = value;
        }
    }

    /// <summary>
    /// Used for round-robin scheduling.  Number of AssemblyLoadContexts domains to schedule tests into.
    /// </summary>
    public int NumAssemblyLoadContexts
    {
        get
        {
            return (_numAssemblyLoadContexts);
        }
        set
        {
            _numAssemblyLoadContexts = value;
        }
    }

    /// <summary>
    /// Used for round-robin scheduling.  Number of app domains to schedule tests into.
    /// </summary>
    public int NumAppDomains
    {
        get
        {
            return (_numAppDomains);
        }
        set
        {
            _numAppDomains = value;
        }
    }

    public string DefaultDebugger
    {
        get
        {
            return (_defaultDebugger);
        }
        set
        {
            _defaultDebugger = value;
        }
    }

    public string DefaultDebuggerOptions
    {
        get
        {
            return (_defaultDebuggerOptions);
        }
        set
        {
            _defaultDebuggerOptions = value;
        }
    }
    public int ULGeneralUnloadPercent
    {
        get
        {
            return (_ulGeneralUnloadPercent);
        }

        set
        {
            _ulGeneralUnloadPercent = value;
        }
    }
    public int ULAppDomainUnloadPercent
    {
        get
        {
            return (_ulAppDomainUnloadPercent);
        }

        set
        {
            _ulAppDomainUnloadPercent = value;
        }
    }
    public int ULAssemblyLoadPercent
    {
        get
        {
            return (_ulAssemblyLoadPercent);
        }

        set
        {
            _ulAssemblyLoadPercent = value;
        }
    }
    public int ULWaitTime
    {
        get
        {
            return (_ulWaitTime);
        }

        set
        {
            _ulWaitTime = value;
        }
    }
    public bool ReportResults
    {
        get
        {
            if (_reportResults && Environment.GetEnvironmentVariable("RF_NOREPORT") == null)
            {
                _reportResults = false;
            }
            return (_reportResults);
        }
        set
        {
            _reportResults = value;
        }
    }
    public string ReportResultsTo
    {
        get
        {
            return (_reportResultsTo);
        }
        set
        {
            _reportResultsTo = value;
        }
    }
    public Guid BvtCategory
    {
        get
        {
            return (_bvtCategory);
        }
        set
        {
            _bvtCategory = value;
        }
    }

    public string CCFailMail
    {
        get
        {
            return (_ccFailMail);
        }
        set
        {
            _ccFailMail = value;
        }
    }

    public bool SuppressConsoleOutputFromTests
    {
        get
        {
            return _suppressConsoleOutputFromTests;
        }
        set
        {
            _suppressConsoleOutputFromTests = value;
        }
    }

    public bool DebugBreakOnTestHang
    {
        get
        {
            return _debugBreakOnTestHang;
        }
        set
        {
            _debugBreakOnTestHang = value;
        }
    }

    public bool DebugBreakOnBadTest
    {
        get
        {
            return _debugBreakOnBadTest;
        }
        set
        {
            _debugBreakOnBadTest = value;
        }
    }

    public bool DebugBreakOnOutOfMemory
    {
        get
        {
            return _debugBreakOnOutOfMemory;
        }
        set
        {
            _debugBreakOnOutOfMemory = value;
        }
    }

    public bool DebugBreakOnPathTooLong
    {
        get
        {
            return _debugBreakOnPathTooLong;
        }
        set
        {
            _debugBreakOnPathTooLong = value;
        }
    }

    public bool DebugBreakOnMissingTest
    {
        get
        {
            return _debugBreakOnMissingTest;
        }
        set
        {
            _debugBreakOnMissingTest = value;
        }
    }
}
