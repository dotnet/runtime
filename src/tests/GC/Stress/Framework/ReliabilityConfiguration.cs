// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Xml;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;

// General Notes:
// we use the same modem for our config here as we use for icorhost.exe.  There are 2 config files.  The 1st specifies the tests 
// that are available to the runtime.  The 2nd specifies our current setup based upon the settings in the 1st configuration file.

public enum TestStartModeEnum
{
    AppDomainLoader,
    ProcessLoader,
    AssemblyLoadContextLoader
}

public enum AppDomainLoaderMode
{
    FullIsolation,
    Normal,
    RoundRobin,
    Lazy
}

public enum AssemblyLoadContextLoaderMode
{
    FullIsolation,
    Normal,
    RoundRobin,
    Lazy
}

[Flags]
public enum LoggingLevels
{
    Default = 0x01,
    StartupShutdown = 0x02,
    AppDomain = 0x04,
    Tests = 0x08,
    SmartDotNet = 0x10,
    Logging = 0x20,
    UrtFrameworks = 0x40,
    TestStarter = 0x80,
    AssemblyLoadContext = 0x100,

    All = (0x01 | 0x02 | 0x04 | 0x08 | 0x10 | 0x20 | 0x80 | 0x100)
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// <summary>
/// ReliabilityConfig is responsible for parsing the available XML configuration files (both the primary config file & the concurrent
/// test config file.  We do not parse single test config files).  
/// </summary>
public class ReliabilityConfig : IEnumerable, IEnumerator
{
    private ArrayList _testSet = new ArrayList();					// this is the set of <Test... > tags we find.  There may be multiple test runs in one
    // config file.
    private ReliabilityTestSet _curTestSet;							// The test set we're currently filling in...
    private int _index = -1;											// Current test set index

    // Toplevel tags for all config files
    private const string configHost = "Host";
    private const string configInclude = "Include";
    private const string configIncludes = "Includes";

    internal class RFConfigOptions
    {
        internal const string RFConfigOptions_Test_MinMaxTestsUseCPUCount = "minMaxTestUseCPUCount";
        internal const string RFConfigOptions_Test_SuppressConsoleOutputFromTests = "suppressConsoleOutputFromTests";
        internal const string RFConfigOptions_Test_DebugBreakOnHang = "debugBreakOnTestHang";
        internal const string RFConfigOptions_Test_DebugBreakOnBadTest = "debugBreakOnBadTest";
        internal const string RFConfigOptions_Test_DebugBreakOnOutOfMemory = "debugBreakOnOutOfMemory";
        internal const string RFConfigOptions_Test_DebugBreakOnPathTooLong = "debugBreakOnPathTooLong";
        internal const string RFConfigOptions_Test_DebugBreakOnMissingTest = "debugBreakOnMissingTest";
    }

    // Various tags for the concurrent test configuration file.
    private const string concurrentConfigTest = "Test";
    private const string concurrentConfigAssembly = "Assembly";
    private const string configTestMinimumCPU = "minimumCPU";
    private const string configTestMinimumCPUStaggered = "minimumCPUStaggered";
    private const string configInstallDetours = "installDetours";
    private const string configLoggingLevel = "loggingLevel";
    private const string configTestMinimumMem = "minimumMem";
    private const string configTestMinimumTests = "minimumTests";
    private const string configTestMaximumTests = "maximumTests";
    private const string configTestDisableLogging = "disableLogging";
    private const string configEnablePerfCounters = "enablePerfCounters";
    private const string configDefaultDebugger = "defaultDebugger";
    private const string configDefaultDebuggerOptions = "defaultDebuggerOptions";
    private const string configDefaultTestStartMode = "defaultTestLoader";
    private const string configResultReporting = "resultReporting";
    private const string configResultReportingUrl = "resultReportingUrl";
    private const string configResultReportingBvtCategory = "resultBvtCategory";
    private const string configULAppDomainUnloadPercent = "ulAppDomainUnloadPercent";
    private const string configULGeneralUnloadPercent = "ulGeneralUnloadPercent";
    private const string configULAssemblyLoadPercent = "ulAssemblyLoadPercent";
    private const string configULWaitTime = "ulWaitTime";
    private const string configCcFailMail = "ccFailMail";
    private const string configAppDomainLoaderMode = "appDomainLoaderMode";
    private const string configAssemblyLoadContextLoaderMode = "assemblyLoadContextLoaderMode";
    private const string configRoundRobinAppDomainCount = "numAppDomains";
    private const string configRoundRobinAssemblyLoadContextCount = "numAssemblyLoadContexts";
    // Attributes for the <Assembly ...> tag
    private const string configAssemblyName = "id";
    private const string configAssemblyFilename = "filename";
    private const string configAssemblySuccessCode = "successCode";
    private const string configAssemblyEntryPoint = "entryPoint";
    private const string configAssemblyArguments = "arguments";
    private const string configAssemblyConcurrentCopies = "concurrentCopies";
    private const string configAssemblyBasePath = "basePath";
    private const string configAssemblyStatus = "status";
    private const string configAssemblyStatusDisabled = "disabled";
    private const string configAssemblyDebugger = "debugger";                 // "none", "cdb.exe", "windbg.exe", etc...  only w/ Process.Start test starter
    private const string configAssemblyDebuggerOptions = "debuggerOptions";   // cmd line options for debugger - only w/ Process.Start test starter
    private const string configAssemblySmartNetGuid = "guid";
    private const string configAssemblyDuration = "expectedDuration";
    private const string configAssemblyRequiresSDK = "requiresSDK";
    private const string configAssemblyTestLoader = "testLoader";
    private const string configAssemblyTestAttributes = "testAttrs";
    private const string configAssemblyTestOwner = "testOwner";
    private const string configAssemblyTestGroup = "group";
    private const string configAssemblyPreCommand = "precommand";
    private const string configAssemblyPostCommand = "postcommand";
    private const string configAssemblyCustomAction = "customAction";


    private const string configPercentPassIsPass = "percentPassIsPass";

    // Attributes for the <Include ...> tag
    private const string configIncludeFilename = "filename";

    // Attributes for the <Discovery ...> tag
    private const string configDiscovery = "Discovery";
    private const string configDiscoveryPath = "path";

    // Attributes related to debug mode...
    private const string debugConfigIncludeInlined = "INLINE";

    // Test start modes
    private const string configTestStartModeAppDomainLoader = "appDomainLoader";
    private const string configTestStartModeProcessLoader = "processLoader";
    private const string configTestStartModeAssemblyLoadContextLoader = "assemblyLoadContextLoader";
    private const string configTestStartModeTaskLoader = "taskLoader";

    // APp domain loader modes
    private const string configAppDomainLoaderModeFullIsolation = "fullIsolation";
    private const string configAppDomainLoaderModeNormal = "normal";
    private const string configAppDomainLoaderModeRoundRobin = "roundRobin";
    private const string configAppDomainLoaderModeLazy = "lazy";

    // AssemblyLoadContext loader modes
    private const string configAssemblyLoadContextLoaderModeFullIsolation = "fullIsolation";
    private const string configAssemblyLoadContextLoaderModeNormal = "normal";
    private const string configAssemblyLoadContextLoaderModeRoundRobin = "roundRobin";
    private const string configAssemblyLoadContextLoaderModeLazy = "lazy";


    /// <summary>
    /// The ReliabilityConfig constructor.  Takes 2 config files: The primary config & the test config file.  We then load these up
    /// and create all the properties on ourself so that the reliability harness knows what to do.
    /// </summary>
    public ReliabilityConfig(string testConfig)
    {
        GetTestsToRun(testConfig);
    }

    private bool GetTrueFalseOptionValue(string value, string configSettingName)
    {
        if (value == "true" || value == "1" || value == "yes")
        {
            return true;
        }
        else if (value == "false" || value == "0" || value == "no")
        {
            return false;
        }
        throw new Exception(String.Format("Unknown option value for {0}: {1}", configSettingName, value));
    }

    // returns time in minutes
    public static int ConvertTimeValueToTestRunTime(string timeValue)
    {
        int returnValue;

        if (timeValue.IndexOf(":") == -1)
        {
            // just a number of minutes
            returnValue = Convert.ToInt32(timeValue);
        }
        else
        {
            // time span
            try
            {
                returnValue = unchecked((int)(TimeSpan.Parse(timeValue).Ticks / TimeSpan.TicksPerMinute));
            }
            catch
            {
                throw new Exception(String.Format("Bad time span {0} for maximum execution time", timeValue));
            }
        }

        return returnValue;
    }


    /// <summary>
    /// Given a test configfile we find the tests that we actually want to run.
    /// </summary>
    private void GetTestsToRun(string testConfig)
    {
        int totalDepth = 0;							// used for debugging mode so we can keep proper indentation.
        ArrayList foundTests = new ArrayList();		// the array of tests we've found.			
        ArrayList discoveryPaths = new ArrayList();	// the array of discovery paths we've found.
        Stack xmlFileStack = new Stack();			// this stack keeps track of our include files.  		
        Stack testLevelStack = new Stack();

        try
        {
#if PROJECTK_BUILD
            FileStream fs = new FileStream(testConfig, FileMode.Open, FileAccess.Read, FileShare.Read);
            xmlFileStack.Push(XmlReader.Create(fs));
#else
            xmlFileStack.Push(new XmlTextReader(testConfig));
#endif
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("Could not open config file: {0}", testConfig);
            throw e;
        }

        do
        {
#if PROJECTK_BUILD
            XmlReader currentXML = (XmlReader)xmlFileStack.Pop();
#else
            XmlTextReader currentXML = (XmlTextReader)xmlFileStack.Pop();
#endif
            totalDepth -= currentXML.Depth;

            if (currentXML.Depth != 0)
            {
                IndentToDepth(totalDepth + currentXML.Depth - 1);	// -1 because we haven't done a .Read on the includes tag yet.
                XmlDebugOutLine("</" + configInclude + ">");
            }

            while (currentXML.Read())
            {
                switch (currentXML.NodeType)
                {
                    case XmlNodeType.Element:

                        bool isEmpty = currentXML.IsEmptyElement;

                        IndentToDepth(totalDepth + currentXML.Depth);
                        XmlDebugOut("<" + currentXML.Name);

                        switch (currentXML.Name)
                        {
                            case configInclude:		// user included a file in this file.  
                                string filename = null;
                                bool skipInclude = false;

                                while (currentXML.MoveToNextAttribute())
                                {
                                    XmlDebugOut(" " + currentXML.Name + "=\"" + currentXML.Value + "\"");
                                    switch (currentXML.Name)
                                    {
                                        case configIncludeFilename:
                                            filename = currentXML.Value;
                                            break;
                                        case debugConfigIncludeInlined:	// so we can consume the XML we spit out in debug mode- 
                                            // we ignore this include tag if it's been inlined.

                                            if (currentXML.Value.ToLower() == "true" || currentXML.Value == "1")
                                            {
                                                skipInclude = true;
                                            }
                                            break;
                                        default:
                                            throw new Exception("Unknown attribute on include tag!");
                                    }
                                }
                                if (skipInclude)
                                {
                                    XmlDebugOutLine(">");
                                    continue;
                                }

                                XmlDebugOut(" " + debugConfigIncludeInlined + "=\"true\">\r\n");

                                if (filename == null)
                                {
                                    throw new ArgumentException("Type or Filename not set on include file!  Both attributes must be set to properly include a file.");
                                }

                                xmlFileStack.Push(currentXML);	// save our current file.
                                totalDepth += currentXML.Depth;

                                filename = ConvertPotentiallyRelativeFilenameToFullPath(stripFilenameFromPath(currentXML.BaseURI), filename);
                                try
                                {
#if PROJECTK_BUILD
                                    currentXML = XmlReader.Create(filename);
#else
                                    currentXML = new XmlTextReader(filename);
#endif
                                }
                                catch (FileNotFoundException e)
                                {
                                    Console.WriteLine("Could not open included config file: {0}", filename);
                                    throw e;
                                }
                                continue;
                            case configIncludes:
                                if (isEmpty)
                                {
                                    XmlDebugOut("/>\r\n");
                                }
                                else
                                {
                                    XmlDebugOut(">\r\n");
                                }
                                continue; // note: we never push or pop includes off of our stack.
                            case configHost:
                                if (testLevelStack.Count == 0) // we'll skip this tag when it shows up in an included file.
                                {
                                    testLevelStack.Push(configHost);
                                    while (currentXML.MoveToNextAttribute())
                                    {
                                        switch (currentXML.Name)
                                        {
                                            case "xmlns:xsi":
                                            case "xmlns:xsd":
                                                break;
                                            default:
                                                throw new Exception("Unknown attribute on reliability tag: " + currentXML.Name);
                                        }
                                    }
                                }
                                else
                                {
                                    if (isEmpty)
                                    {
                                        XmlDebugOutLine("/>");
                                    }
                                    else
                                    {
                                        XmlDebugOutLine(">");
                                    }
                                    continue;
                                }
                                break;
                            case concurrentConfigTest:
                                if (testLevelStack.Count != 0 && (string)testLevelStack.Peek() != configHost)
                                {
                                    throw new ArgumentException("The test tag can only appear as a child to the reliabilityFramework tag or a top level tag.");
                                }

                                // save any info we've gathered about tests into the current test set
                                if (_curTestSet != null && foundTests != null && foundTests.Count > 0)
                                {
                                    _curTestSet.Tests = (ReliabilityTest[])foundTests.ToArray(typeof(ReliabilityTest));
                                    _curTestSet.DiscoveryPaths = (string[])discoveryPaths.ToArray(typeof(string));
                                    discoveryPaths.Clear();
                                    foundTests.Clear();
                                }

                                testLevelStack.Push(concurrentConfigTest);

                                _curTestSet = new ReliabilityTestSet();

                                // when running as an ordinary test, limit run time to 10 min.
                                bool limitTime = ReliabilityFramework.IsRunningAsUnitTest && !ReliabilityFramework.IsRunningLongGCTests;

                                if (limitTime)
                                {
                                    _curTestSet.MaximumTime = 10;
                                }

                                while (currentXML.MoveToNextAttribute())
                                {
                                    XmlDebugOut(" " + currentXML.Name + "=\"" + currentXML.Value + "\"");
                                    switch (currentXML.Name)
                                    {
                                        case "maximumTestRuns":
                                            _curTestSet.MaximumLoops = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case "maximumExecutionTime":
                                            string timeValue = currentXML.Value;

                                            if (!limitTime)
                                            {
                                                _curTestSet.MaximumTime = ConvertTimeValueToTestRunTime(timeValue);
                                            }

                                            break;
                                        case "maximumWaitTime":
                                            _curTestSet.MaximumWaitTime = ConvertTimeValueToTestRunTime(currentXML.Value);
                                            break;
                                        case "id":
                                            _curTestSet.FriendlyName = currentXML.Value;
                                            break;
                                        case "xmlns:xsi":
                                        case "xmlns:xsd":
                                            break;
                                        case configTestMinimumMem:
                                            _curTestSet.MinPercentMem = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configLoggingLevel:
                                            _curTestSet.LoggingLevel = (LoggingLevels)Convert.ToInt32(currentXML.Value.ToString(), 16);
                                            break;
                                        case configTestMinimumCPUStaggered:
                                            _curTestSet.MinPercentCPUStaggered = currentXML.Value;
                                            break;
                                        case configTestMinimumCPU:
                                            _curTestSet.MinPercentCPU = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configInstallDetours:
                                            if (currentXML.Value == "true" || currentXML.Value == "1" || currentXML.Value == "yes")
                                            {
                                                _curTestSet.InstallDetours = true;
                                            }
                                            else if (currentXML.Value == "false" || currentXML.Value == "0" || currentXML.Value == "no")
                                            {
                                                _curTestSet.InstallDetours = false;
                                            }
                                            else
                                            {
                                                throw new Exception("Unknown value for result reporting: " + currentXML.Value);
                                            }
                                            break;
                                        case configTestMinimumTests:
                                            _curTestSet.MinTestsRunning = Convert.ToInt32(currentXML.Value);
                                            break;

                                        case RFConfigOptions.RFConfigOptions_Test_MinMaxTestsUseCPUCount:
                                            if (GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_MinMaxTestsUseCPUCount))
                                            {
                                                string numProcessors = Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");
                                                int cpuCount;
                                                if (numProcessors == null)
                                                {
                                                    Console.WriteLine("NUMBER_OF_PROCESSORS environment variable not supplied, falling back to Environment");
                                                    cpuCount = Environment.ProcessorCount;
                                                }
                                                else
                                                {
                                                    cpuCount = Convert.ToInt32(Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS"));
                                                }

                                                if (cpuCount <= 0)
                                                    throw new Exception("Invalid Value when reading processor count: " + cpuCount);
                                                _curTestSet.MinTestsRunning = cpuCount;
                                                _curTestSet.MaxTestsRunning = (int)(cpuCount * 1.5);
                                            }
                                            break;
                                        case RFConfigOptions.RFConfigOptions_Test_SuppressConsoleOutputFromTests:
                                            _curTestSet.SuppressConsoleOutputFromTests = GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_SuppressConsoleOutputFromTests);
                                            break;

                                        case RFConfigOptions.RFConfigOptions_Test_DebugBreakOnHang:
                                            _curTestSet.DebugBreakOnTestHang = GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_DebugBreakOnHang);
                                            break;

                                        case RFConfigOptions.RFConfigOptions_Test_DebugBreakOnBadTest:
                                            _curTestSet.DebugBreakOnBadTest = GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_DebugBreakOnBadTest);
                                            break;

                                        case RFConfigOptions.RFConfigOptions_Test_DebugBreakOnOutOfMemory:
                                            _curTestSet.DebugBreakOnOutOfMemory = GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_DebugBreakOnOutOfMemory);
                                            break;

                                        case RFConfigOptions.RFConfigOptions_Test_DebugBreakOnPathTooLong:
                                            _curTestSet.DebugBreakOnPathTooLong = GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_DebugBreakOnPathTooLong);
                                            break;

                                        case RFConfigOptions.RFConfigOptions_Test_DebugBreakOnMissingTest:
                                            _curTestSet.DebugBreakOnMissingTest = GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_DebugBreakOnMissingTest);
                                            break;

                                        case configResultReporting:
                                            _curTestSet.ReportResults = GetTrueFalseOptionValue(currentXML.Value, configResultReporting);
                                            break;

                                        case configResultReportingUrl:
                                            _curTestSet.ReportResultsTo = currentXML.Value;
                                            break;
                                        case configResultReportingBvtCategory:
                                            try
                                            {
                                                _curTestSet.BvtCategory = new Guid(currentXML.Value);
                                            }
                                            catch (FormatException)
                                            {
                                                throw new Exception(String.Format("BVT Category Guid {0} is not in the correct form", currentXML.Value));
                                            }
                                            break;
                                        case configTestMaximumTests:
                                            _curTestSet.MaxTestsRunning = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configTestDisableLogging:
                                            _curTestSet.DisableLogging = GetTrueFalseOptionValue(currentXML.Value, configTestDisableLogging);
                                            break;

                                        case configEnablePerfCounters:
                                            _curTestSet.EnablePerfCounters = GetTrueFalseOptionValue(currentXML.Value, configEnablePerfCounters);
                                            break;

                                        case configDefaultTestStartMode:
                                            switch (currentXML.Value)
                                            {
                                                case configTestStartModeAppDomainLoader:
                                                    if (null != _curTestSet.DefaultDebugger || null != _curTestSet.DefaultDebuggerOptions)
                                                    {
                                                        throw new Exception(String.Format("{0} specified with default debugger or debugger options.  If you want a debugger per test please use {1}=\"{2}\" ",
                                                            configTestStartModeAppDomainLoader,
                                                            configDefaultTestStartMode,
                                                            configTestStartModeProcessLoader));
                                                    }
                                                    _curTestSet.DefaultTestStartMode = TestStartModeEnum.AppDomainLoader;
                                                    break;
                                                case configTestStartModeAssemblyLoadContextLoader:
                                                    _curTestSet.DefaultTestStartMode = TestStartModeEnum.AssemblyLoadContextLoader;
                                                    break;
                                                case configTestStartModeProcessLoader:
                                                    _curTestSet.DefaultTestStartMode = TestStartModeEnum.ProcessLoader;
                                                    break;
                                                default:
                                                    throw new Exception(String.Format("Unknown test starter {0} specified!", currentXML.Value));
                                            }
                                            break;
                                        case configRoundRobinAppDomainCount:
                                            try
                                            {
                                                _curTestSet.NumAppDomains = Convert.ToInt32(currentXML.Value);
                                                if (_curTestSet.NumAppDomains <= 0)
                                                {
                                                    throw new Exception("Number of app domains must be greater than zero!");
                                                }
                                            }
                                            catch
                                            {
                                                throw new Exception(String.Format("The value {0} is not an integer", currentXML.Value));
                                            }
                                            break;
                                        case configRoundRobinAssemblyLoadContextCount:
                                            try
                                            {
                                                _curTestSet.NumAssemblyLoadContexts = Convert.ToInt32(currentXML.Value);
                                                if (_curTestSet.NumAssemblyLoadContexts <= 0)
                                                {
                                                    throw new Exception("Number of AssemblyLoadContexts must be greater than zero!");
                                                }
                                            }
                                            catch
                                            {
                                                throw new Exception(String.Format("The value {0} is not an integer", currentXML.Value));
                                            }
                                            break;
                                        case configAppDomainLoaderMode:
                                            switch (currentXML.Value)
                                            {
                                                case configAppDomainLoaderModeFullIsolation:
                                                    _curTestSet.AppDomainLoaderMode = AppDomainLoaderMode.FullIsolation;
                                                    break;
                                                case configAppDomainLoaderModeNormal:
                                                    _curTestSet.AppDomainLoaderMode = AppDomainLoaderMode.Normal;
                                                    break;
                                                case configAppDomainLoaderModeRoundRobin:
                                                    _curTestSet.AppDomainLoaderMode = AppDomainLoaderMode.RoundRobin;
                                                    break;
                                                case configAppDomainLoaderModeLazy:
                                                    _curTestSet.AppDomainLoaderMode = AppDomainLoaderMode.Lazy;
                                                    break;

                                                default:
                                                    throw new Exception(String.Format("Unknown AD Loader mode {0} specified!", currentXML.Value));
                                            }
                                            break;
                                        case configAssemblyLoadContextLoaderMode:
                                            switch (currentXML.Value)
                                            {
                                                case configAssemblyLoadContextLoaderModeFullIsolation:
                                                    _curTestSet.AssemblyLoadContextLoaderMode = AssemblyLoadContextLoaderMode.FullIsolation;
                                                    break;
                                                case configAssemblyLoadContextLoaderModeNormal:
                                                    _curTestSet.AssemblyLoadContextLoaderMode = AssemblyLoadContextLoaderMode.Normal;
                                                    break;
                                                case configAssemblyLoadContextLoaderModeRoundRobin:
                                                    _curTestSet.AssemblyLoadContextLoaderMode = AssemblyLoadContextLoaderMode.RoundRobin;
                                                    break;
                                                case configAssemblyLoadContextLoaderModeLazy:
                                                    _curTestSet.AssemblyLoadContextLoaderMode = AssemblyLoadContextLoaderMode.Lazy;
                                                    break;

                                                default:
                                                    throw new Exception(String.Format("Unknown ALC Loader mode {0} specified!", currentXML.Value));
                                            }
                                            break;
                                        case configPercentPassIsPass:
                                            _curTestSet.PercentPassIsPass = Convert.ToInt32(currentXML.Value);
                                            break;

                                        case configDefaultDebugger:
                                            if (currentXML.Value.Length >= 7 && currentXML.Value.Substring(currentXML.Value.Length - 7).ToLower() == "cdb.exe")
                                            {
                                                _curTestSet.DefaultDebugger = currentXML.Value;
                                            }
                                            else if (currentXML.Value.Length >= 10 && currentXML.Value.Substring(currentXML.Value.Length - 7).ToLower() == "windbg.exe")
                                            {
                                                _curTestSet.DefaultDebugger = currentXML.Value;
                                            }
                                            else if (currentXML.Value.ToLower() == "none")
                                            {
                                                _curTestSet.DefaultDebugger = String.Empty;
                                            }
                                            else
                                            {
                                                throw new Exception("Unknown default debugger specified (" + currentXML.Value + ")");
                                            }
                                            break;
                                        case configDefaultDebuggerOptions:
                                            _curTestSet.DefaultDebuggerOptions = Environment.ExpandEnvironmentVariables(currentXML.Value);
                                            break;
                                        case configULAssemblyLoadPercent:
                                            _curTestSet.ULAssemblyLoadPercent = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configULAppDomainUnloadPercent:
                                            _curTestSet.ULAppDomainUnloadPercent = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configULGeneralUnloadPercent:
                                            _curTestSet.ULGeneralUnloadPercent = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configULWaitTime:
                                            _curTestSet.ULWaitTime = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configCcFailMail:
                                            _curTestSet.CCFailMail = currentXML.Value;
                                            break;
                                        default:
                                            throw new Exception("Unknown attribute (" + currentXML.Name + ") on " + concurrentConfigTest + " tag!");
                                    }
                                }

                                // Check to see if any of the test attribute environment variables are set,
                                // If so, then use the environment variables.
                                if ((Environment.GetEnvironmentVariable("TIMELIMIT") != null) && (Environment.GetEnvironmentVariable("TIMELIMIT") != ""))
                                    _curTestSet.MaximumTime = ConvertTimeValueToTestRunTime(Environment.GetEnvironmentVariable("TIMELIMIT"));

                                if ((Environment.GetEnvironmentVariable("MINCPU") != null) && (Environment.GetEnvironmentVariable("MINCPU") != ""))
                                    _curTestSet.MinPercentCPU = Convert.ToInt32(Environment.GetEnvironmentVariable("MINCPU"));


                                _testSet.Add(_curTestSet);
                                break;
                            case configDiscovery:
                                if (testLevelStack.Count == 0 || (string)testLevelStack.Peek() != concurrentConfigTest)
                                {
                                    throw new ArgumentException("The assembly tag can only appear as a child to the test tag (curent parent tag==" + (string)testLevelStack.Peek() + ").");
                                }


                                testLevelStack.Push(configDiscovery);

                                string path = null;
                                while (currentXML.MoveToNextAttribute())
                                {
                                    XmlDebugOut(" " + currentXML.Name + "=\"" + currentXML.Value + "\"");
                                    switch (currentXML.Name)
                                    {
                                        case configDiscoveryPath:
                                            path = currentXML.Value;
                                            break;
                                        default:
                                            throw new Exception("Unknown attribute on include tag (\"" + currentXML.Name + "\")!");
                                    }
                                }
                                discoveryPaths.Add(Environment.ExpandEnvironmentVariables(path));
                                break;
                            case concurrentConfigAssembly:
                                /***********************************************************************
                                 * Here's where we process an assembly & it's options.                 *
                                 ***********************************************************************/

                                bool disabled = false;

                                if (testLevelStack.Count == 0 || (string)testLevelStack.Peek() != concurrentConfigTest)
                                {
                                    throw new ArgumentException("The assembly tag can only appear as a child to the test tag (curent parent tag==" + (string)testLevelStack.Peek() + ").");
                                }
                                testLevelStack.Push(concurrentConfigAssembly);

                                ReliabilityTest rt = new ReliabilityTest(_curTestSet.SuppressConsoleOutputFromTests);
                                rt.TestStartMode = _curTestSet.DefaultTestStartMode;

                                // first we need to setup any default options which are set globally on
                                // the test start mode.
                                if (null != _curTestSet.DefaultDebugger)
                                {
                                    if (_curTestSet.DefaultTestStartMode != TestStartModeEnum.ProcessLoader)
                                    {
                                        throw new Exception(String.Format("{0} specified with default debugger or debugger options.  If you want a debugger per test please use {1}=\"{2}\" ",
                                            configTestStartModeAppDomainLoader,
                                            configDefaultTestStartMode,
                                            configTestStartModeProcessLoader));
                                    }
                                    rt.Debugger = _curTestSet.DefaultDebugger;
                                }

                                if (null != _curTestSet.DefaultDebuggerOptions)
                                {
                                    if (_curTestSet.DefaultTestStartMode != TestStartModeEnum.ProcessLoader)
                                    {
                                        throw new Exception(String.Format("{0} specified with default debugger or debugger options.  If you want a debugger per test please use {1}=\"{2}\" ",
                                            configTestStartModeAppDomainLoader,
                                            configDefaultTestStartMode,
                                            configTestStartModeProcessLoader));
                                    }
                                    rt.DebuggerOptions = _curTestSet.DefaultDebuggerOptions;
                                }


                                // then we need to process the individual options & overrides.
                                while (currentXML.MoveToNextAttribute())
                                {
                                    XmlDebugOut(" " + currentXML.Name + "=\"" + currentXML.Value + "\"");
                                    switch (currentXML.Name)
                                    {
                                        case configAssemblyName:
                                            rt.RefOrID = currentXML.Value;
                                            break;
                                        case configAssemblyBasePath:
                                            rt.BasePath = Environment.ExpandEnvironmentVariables(currentXML.Value);
                                            break;
                                        case configAssemblyRequiresSDK:
                                            if (String.Compare(currentXML.Value, "true", true) == 0 ||
                                                currentXML.Value == "1" ||
                                                String.Compare(currentXML.Value, "yes", true) == 0)
                                            {
                                                rt.RequiresSDK = true;
                                            }
                                            else if (String.Compare(currentXML.Value, "false", true) == 0 ||
                                                currentXML.Value == "0" ||
                                                String.Compare(currentXML.Value, "no", true) == 0)
                                            {
                                                rt.RequiresSDK = false;
                                            }
                                            else
                                            {
                                                throw new Exception("RequiresSDK has illegal value.  Must be true, 1, yes, false, 0, or no");
                                            }
                                            break;
                                        case configAssemblyFilename:
                                            rt.Assembly = Environment.ExpandEnvironmentVariables(currentXML.Value);
                                            Console.WriteLine("test is " + rt.Assembly);
                                            break;
                                        case configAssemblySuccessCode:
                                            rt.SuccessCode = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configAssemblyEntryPoint:
                                            rt.Arguments = currentXML.Value;
                                            break;
                                        case configAssemblyArguments:
                                            if (!string.IsNullOrEmpty(currentXML.Value))
                                                rt.Arguments = Environment.ExpandEnvironmentVariables(currentXML.Value);
                                            break;
                                        case configAssemblyConcurrentCopies:
                                            rt.ConcurrentCopies = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configAssemblyStatus:
                                            if (currentXML.Value == configAssemblyStatusDisabled)
                                            {
                                                disabled = true;
                                            }
                                            break;
                                        case configAssemblyDebugger:
                                            if (TestStartModeEnum.ProcessLoader != _curTestSet.DefaultTestStartMode)
                                            {
                                                throw new Exception(String.Format("{0} can only be set for test sets with {1}=\"{2}\" set.",
                                                    configAssemblyDebugger,
                                                    configDefaultTestStartMode,
                                                    configTestStartModeProcessLoader));
                                            }

                                            if (currentXML.Value.Length >= 7 && currentXML.Value.Substring(currentXML.Value.Length - 7).ToLower() == "cdb.exe")
                                            {
                                                rt.Debugger = currentXML.Value;
                                            }
                                            else if (currentXML.Value.Length >= 10 && currentXML.Value.Substring(currentXML.Value.Length - 7).ToLower() == "windbg.exe")
                                            {
                                                rt.Debugger = currentXML.Value;
                                            }
                                            else if (currentXML.Value.ToLower() == "none")
                                            {
                                                rt.Debugger = String.Empty;
                                            }
                                            else
                                            {
                                                throw new Exception("Unknown debugger specified (" + currentXML.Value + ")");
                                            }
                                            break;
                                        case configAssemblyDebuggerOptions:
                                            if (TestStartModeEnum.ProcessLoader != _curTestSet.DefaultTestStartMode)
                                            {
                                                throw new Exception(String.Format("{0} can only be set for test sets with {1}=\"{2}\" set.",
                                                    configAssemblyDebuggerOptions,
                                                    configDefaultTestStartMode,
                                                    configTestStartModeProcessLoader));
                                            }
                                            rt.DebuggerOptions = Environment.ExpandEnvironmentVariables(currentXML.Value);
                                            break;
                                        case configAssemblySmartNetGuid:
                                            try
                                            {
                                                rt.Guid = new Guid(currentXML.Value);
                                            }
                                            catch (FormatException)
                                            {
                                                throw new Exception(String.Format("The format for guid {0} on test {1} is invalid", currentXML.Value, rt.RefOrID));
                                            }
                                            break;
                                        case configAssemblyDuration:
                                            if (currentXML.Value.IndexOf(":") == -1)
                                            {
                                                // just a number of minutes
                                                rt.ExpectedDuration = Convert.ToInt32(currentXML.Value);
                                            }
                                            else
                                            {
                                                // time span
                                                try
                                                {
                                                    rt.ExpectedDuration = unchecked((int)(TimeSpan.Parse(currentXML.Value).Ticks / TimeSpan.TicksPerMinute));
                                                }
                                                catch
                                                {
                                                    throw new Exception(String.Format("Bad time span {0} for expected duration.", currentXML.Value));
                                                }
                                            }
                                            break;
                                        case configAssemblyTestAttributes:
                                            string[] attrs = currentXML.Value.Split(';');
                                            TestAttributes testAttrs = TestAttributes.None;
                                            for (int j = 0; j < attrs.Length; j++)
                                            {
                                                switch (attrs[j].ToLower())
                                                {
                                                    case "requiressta":
                                                        testAttrs |= TestAttributes.RequiresSTAThread;
                                                        break;
                                                    case "requiresmta":
                                                        testAttrs |= TestAttributes.RequiresMTAThread;
                                                        break;
                                                    default:
                                                        throw new Exception(String.Format("Unknown test attribute: {0}", attrs[j]));
                                                }
                                            }
                                            rt.TestAttrs = testAttrs;
                                            break;
                                        case configAssemblyTestLoader:
                                            switch (currentXML.Value)
                                            {
                                                case configTestStartModeAppDomainLoader:
                                                    if (null != rt.Debugger || null != rt.DebuggerOptions)
                                                    {
                                                        throw new Exception(String.Format("{0} specified with debugger or debugger options.  If you want a debugger per test please use {1}=\"{2}\" ",
                                                            configTestStartModeAppDomainLoader,
                                                            configDefaultTestStartMode,
                                                            configTestStartModeProcessLoader));
                                                    }
                                                    rt.TestStartMode = TestStartModeEnum.AppDomainLoader;
                                                    break;
                                                case configTestStartModeProcessLoader:
                                                    rt.TestStartMode = TestStartModeEnum.ProcessLoader;
                                                    break;
                                                case configTestStartModeAssemblyLoadContextLoader:
                                                    rt.TestStartMode = TestStartModeEnum.AssemblyLoadContextLoader;
                                                    break;
                                                default:
                                                    throw new Exception(String.Format("Unknown test starter {0} specified!", currentXML.Value));
                                            }
                                            break;
                                        case configAssemblyTestOwner:
                                            rt.TestOwner = currentXML.Value;
                                            break;
                                        case configAssemblyTestGroup:
                                            string groupName = currentXML.Value;

                                            // first, we want to see if another test has this group.  We store the group name in
                                            // our group List as the 1st entry.  If we find a group we set our List
                                            // arraylist to that same List (and add ourselves to it).  We're then all in
                                            // one group, the arraylist.
                                            int i = 0;
                                            for (i = 0; i < foundTests.Count; i++)
                                            {
                                                ReliabilityTest test = foundTests[i] as ReliabilityTest;
                                                Debug.Assert(test != null, "Non reliability test in foundTests array!");
                                                if (null != test.Group)
                                                {
                                                    string curGroupName = test.Group[0].ToString();
                                                    if (String.Compare(curGroupName, groupName, false) == 0)
                                                    {
                                                        test.Group.Add(rt);
                                                        rt.Group = test.Group;
                                                        break;
                                                    }
                                                }
                                            }

                                            if (rt.Group == null)
                                            {
                                                // this is the first test in this group
                                                rt.Group = new List<ReliabilityTest>();
                                                rt.Group.Add(rt);
                                            }
                                            break;
                                        case configAssemblyPostCommand:
                                            if (rt.PostCommands == null)
                                            {
                                                // first pre command on this test
                                                rt.PostCommands = new List<string>();
                                            }
                                            rt.PostCommands.Add(Environment.ExpandEnvironmentVariables(currentXML.Value));
                                            break;
                                        case configAssemblyPreCommand:
                                            if (rt.PreCommands == null)
                                            {
                                                // first pre command on this test
                                                rt.PreCommands = new List<string>();
                                            }
                                            rt.PreCommands.Add(Environment.ExpandEnvironmentVariables(currentXML.Value));
                                            break;
                                        case configAssemblyCustomAction:
                                            switch (currentXML.Value)
                                            {
                                                case "LegacySecurityPolicy":
                                                    rt.CustomAction = CustomActionType.LegacySecurityPolicy;
                                                    break;
                                                default:
                                                    throw new Exception(String.Format("Unknown custom action: {0}", currentXML.Value));
                                            }
                                            break;
                                        default:
                                            throw new Exception("Unknown attribute on assembly tag (" + currentXML.Name + "=" + currentXML.Value + ")");
                                    }
                                }

                                // if the test is disabled or it requires the SDK to be installed & 
                                // we don't have the SDK installed then don't add it to our list
                                // of tests to run.
                                if (disabled || (rt.RequiresSDK == true && Environment.GetEnvironmentVariable("INSTALL_SDK") == null))
                                {
                                    break;
                                }

                                int testCopies = 1;
                                if (_curTestSet.AppDomainLoaderMode == AppDomainLoaderMode.FullIsolation ||
                                    _curTestSet.AssemblyLoadContextLoaderMode == AssemblyLoadContextLoaderMode.FullIsolation)
                                {
                                    // in this mode each copy of the test is ran in it's own app domain or AssemblyLoadContext,
                                    // fully isolated from all other copies of the test.  If the user
                                    // specified a cloning level we need to duplicate the test.
                                    testCopies = rt.ConcurrentCopies;
                                    rt.ConcurrentCopies = 1;
                                }
                                else if (_curTestSet.AppDomainLoaderMode == AppDomainLoaderMode.RoundRobin ||
                                         _curTestSet.AssemblyLoadContextLoaderMode == AssemblyLoadContextLoaderMode.RoundRobin)
                                {
                                    // In this mode each test is ran in an app domain w/ other tests.
                                    testCopies = rt.ConcurrentCopies;
                                    rt.ConcurrentCopies = 1;
                                }
                                else
                                {
                                    // Normal mode - tests are ran in app domains / AssemblyLoadContexts w/ copies of themselves
                                }

                                string refOrId = rt.RefOrID;
                                if (rt.RefOrID == null || rt.RefOrID == String.Empty)
                                {
                                    refOrId = rt.Assembly + rt.Arguments;
                                }

                                for (int j = 0; j < testCopies; j++)
                                {
                                    if (testCopies > 1)
                                    {
                                        rt.RefOrID = String.Format("{0} Copy {1}", refOrId, j);
                                    }
                                    else
                                    {
                                        rt.RefOrID = refOrId;
                                    }

                                    bool fRetry;
                                    do
                                    {
                                        fRetry = false;
                                        for (int i = 0; i < foundTests.Count; i++)
                                        {
                                            if (((ReliabilityTest)foundTests[i]).RefOrID == rt.RefOrID)
                                            {
                                                rt.RefOrID = rt.RefOrID + "_" + i.ToString();
                                                fRetry = true;
                                                break;
                                            }
                                        }
                                    } while (fRetry);

                                    ReliabilityTest clone = (ReliabilityTest)rt.Clone();
                                    clone.Index = foundTests.Add(clone);
                                }
                                break;
                            default:
                                throw new ArgumentException("Unknown node (\"" + currentXML.NodeType + "\") named \"" + currentXML.Name + "\"=\"" + currentXML.Value + "\" in config file!");
                        } // end of switch(currentXML.Name)
                        if (isEmpty)
                        {
                            XmlDebugOut("/>\r\n");
                            testLevelStack.Pop();
                        }
                        else
                        {
                            XmlDebugOut(">\r\n");
                        }
                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.ProcessingInstruction:
                    case XmlNodeType.Comment:
                    case XmlNodeType.Document:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        break;
                    case XmlNodeType.EndElement:
                        IndentToDepth(totalDepth + currentXML.Depth);
                        XmlDebugOutLine("</" + currentXML.Name + ">");

                        // note: we never pop or push the includes tag.  It's a special 'hidden' tag
                        // we should also never have to pop a configInclude tag, but it might happen
                        if (currentXML.Name != configIncludes && currentXML.Name != configInclude && currentXML.Name != configHost)
                        {
                            testLevelStack.Pop();
                        }
                        break;
                } // end of switch(currentXML.NodeType)
            } // end of while(currentXML.Read())
        } while (xmlFileStack.Count > 0);

        if (_curTestSet != null && foundTests != null && foundTests.Count > 0)
        {
            _curTestSet.Tests = (ReliabilityTest[])foundTests.ToArray(typeof(ReliabilityTest));
            _curTestSet.DiscoveryPaths = (string[])discoveryPaths.ToArray(typeof(string));
            discoveryPaths.Clear();
            foundTests.Clear();
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Filename processing helper functions.
    /// <summary>
    /// given a base path & a potentially relative path we'll convert the potentially 
    /// </summary>
    public static string ConvertPotentiallyRelativeFilenameToFullPath(string basepath, string path)
    {
        string trimmedPath = path.Trim();	// remove excess whitespace.

#if PROJECTK_BUILD
        if (String.Compare("file://", 0, trimmedPath, 0, 7, StringComparison.OrdinalIgnoreCase) == 0)	// strip file:// from the front if it exists.
#else
        if (String.Compare("file://", 0, trimmedPath, 0, 7, true) == 0)	// strip file:// from the front if it exists.
#endif
        {
            trimmedPath = trimmedPath.Substring(7);
        }

        if (trimmedPath.Trim()[1] == ':' || (trimmedPath.Trim()[0] == '\\' && trimmedPath.Trim()[0] == '\\'))	// we have a drive & UNC
        {
            return (path);
        }


        if (basepath.LastIndexOf(Path.PathSeparator) == (basepath.Length - 1))
        {
            return (basepath + trimmedPath);
        }
        else
        {
            return Path.Combine(basepath, trimmedPath);
        }
    }

    /// <summary>
    /// given a path with a filename on it we remove the filename (to get just the base path).
    /// </summary>
    private string stripFilenameFromPath(string path)
    {
        string trimmedPath = path.Trim();

        if (trimmedPath.LastIndexOf("\\") == -1)
        {
            if (trimmedPath.LastIndexOf("/") == -1)
            {
                return (trimmedPath);	// nothing to strip.
            }
#if PROJECTK_BUILD
            if (String.Compare("file://", 0, trimmedPath, 0, 7, StringComparison.OrdinalIgnoreCase) == 0)
#else
            if (String.Compare("file://", 0, trimmedPath, 0, 7, true) == 0)
#endif 
            {
                return (trimmedPath.Substring(0, trimmedPath.LastIndexOf("/")));
            }

            return (trimmedPath);
        }
        return (trimmedPath.Substring(0, trimmedPath.LastIndexOf("\\")));
    }


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Enumerator related - allow enumerating over multiple test runs within the config file.
    //

    /// <summary>
    /// Gets the test set enumerator
    /// </summary>
    /// <returns></returns>
    public IEnumerator GetEnumerator()
    {
        return (this);
    }

    /// <summary>
    /// Get the current test set
    /// </summary>
    public object Current
    {
        get
        {
            if (_index < _testSet.Count && _index >= 0)
            {
                return (_testSet[_index]);
            }

            throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Move to the next test set
    /// </summary>
    /// <returns></returns>
    public bool MoveNext()
    {
        _index++;
        if (_index >= _testSet.Count)
        {
            return (false);
        }

        return (true);
    }

    /// <summary>
    /// Reset the enumerator to the 1st position
    /// </summary>
    public void Reset()
    {
        _index = 0;
    }


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Debugging helper functions - these are only ever called when we compile with DEBUG_XML defined.
    //
    //

    [Conditional("DEBUG_XML")]
    private void IndentToDepth(int depth)
    {
        while ((depth--) > 0)
        {
            Console.Write("	");
        }
    }

    [Conditional("DEBUG_XML")]
    private void XmlDebugOut(string output)
    {
        Console.Write(output);
    }

    [Conditional("DEBUG_XML")]
    private void XmlDebugOutLine(string output)
    {
        Console.WriteLine(output);
    }
}

[Flags]
public enum TestAttributes
{
    None = 0x00000000,
    RequiresSTAThread = 0x00000001,
    RequiresMTAThread = 0x00000002,
    RequiresUnknownThread = 0x00000004,
    RequiresThread = (RequiresSTAThread | RequiresMTAThread | RequiresUnknownThread),
}

public enum CustomActionType
{
    None,
    LegacySecurityPolicy
}


