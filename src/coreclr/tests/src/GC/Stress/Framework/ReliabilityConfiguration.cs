// Copyright (c) Microsoft. All rights reserved.
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
    ProcessLoader
}

public enum AppDomainLoaderMode
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

    All = (0x01 | 0x02 | 0x04 | 0x08 | 0x10 | 0x20 | 0x80)
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// <summary>
/// ReliabilityConfig is responsible for parsing the available XML configuration files (both the primary config file & the concurrent
/// test config file.  We do not parse single test config files).  
/// </summary>
public class ReliabilityConfig : IEnumerable, IEnumerator
{
    ArrayList testSet = new ArrayList();					// this is the set of <Test... > tags we find.  There may be multiple test runs in one
    // config file.
    ReliabilityTestSet curTestSet;							// The test set we're currently filling in...
    int index = -1;											// Current test set index

    // Toplevel tags for all config files
    const string configHost = "Host";
    const string configInclude = "Include";
    const string configIncludes = "Includes";

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
    const string concurrentConfigTest = "Test";
    const string concurrentConfigAssembly = "Assembly";
    const string configTestMinimumCPU = "minimumCPU";
    const string configTestMinimumCPUStaggered = "minimumCPUStaggered";
    const string configInstallDetours = "installDetours";
    const string configLoggingLevel = "loggingLevel";
    const string configTestMinimumMem = "minimumMem";
    const string configTestMinimumTests = "minimumTests";
    const string configTestMaximumTests = "maximumTests";    
    const string configTestDisableLogging = "disableLogging";
    const string configEnablePerfCounters = "enablePerfCounters";
    const string configDefaultDebugger = "defaultDebugger";
    const string configDefaultDebuggerOptions = "defaultDebuggerOptions";
    const string configDefaultTestStartMode = "defaultTestLoader";
    const string configResultReporting = "resultReporting";
    const string configResultReportingUrl = "resultReportingUrl";
    const string configResultReportingBvtCategory = "resultBvtCategory";
    const string configULAppDomainUnloadPercent = "ulAppDomainUnloadPercent";
    const string configULGeneralUnloadPercent = "ulGeneralUnloadPercent";
    const string configULAssemblyLoadPercent = "ulAssemblyLoadPercent";
    const string configULWaitTime = "ulWaitTime";
    const string configCcFailMail = "ccFailMail";
    const string configAppDomainLoaderMode = "appDomainLoaderMode";
    const string configRoundRobinAppDomainCount = "numAppDomains";
    // Attributes for the <Assembly ...> tag
    const string configAssemblyName = "id";
    const string configAssemblyFilename = "filename";
    const string configAssemblySuccessCode = "successCode";
    const string configAssemblyEntryPoint = "entryPoint";
    const string configAssemblyArguments = "arguments";
    const string configAssemblyConcurrentCopies = "concurrentCopies";
    const string configAssemblyBasePath = "basePath";
    const string configAssemblyStatus = "status";
    const string configAssemblyStatusDisabled = "disabled";
    const string configAssemblyDebugger = "debugger";                 // "none", "cdb.exe", "windbg.exe", etc...  only w/ Process.Start test starter
    const string configAssemblyDebuggerOptions = "debuggerOptions";   // cmd line options for debugger - only w/ Process.Start test starter
    const string configAssemblySmartNetGuid = "guid";
    const string configAssemblyDuration = "expectedDuration";
    const string configAssemblyRequiresSDK = "requiresSDK";
    const string configAssemblyTestLoader = "testLoader";
    const string configAssemblyTestAttributes = "testAttrs";
    const string configAssemblyTestOwner = "testOwner";
    const string configAssemblyTestGroup = "group";
    const string configAssemblyPreCommand = "precommand";
    const string configAssemblyPostCommand = "postcommand";
    const string configAssemblyCustomAction = "customAction";


    const string configPercentPassIsPass = "percentPassIsPass";

    // Attributes for the <Include ...> tag
    const string configIncludeFilename = "filename";

    // Attributes for the <Discovery ...> tag
    const string configDiscovery = "Discovery";
    const string configDiscoveryPath = "path";

    // Attributes related to debug mode...
    const string debugConfigIncludeInlined = "INLINE";

    // Test start modes
    const string configTestStartModeAppDomainLoader = "appDomainLoader";
    const string configTestStartModeProcessLoader = "processLoader";
    const string configTestStartModeTaskLoader = "taskLoader";

    // APp domain loader modes
    const string configAppDomainLoaderModeFullIsolation = "fullIsolation";
    const string configAppDomainLoaderModeNormal = "normal";
    const string configAppDomainLoaderModeRoundRobin = "roundRobin";
    const string configAppDomainLoaderModeLazy = "lazy";


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
        throw new Exception(String.Format("Unknown option value for {0}: {1}",configSettingName, value));        
    }


    private int ConvertTimeValueToTestRunTime(string timeValue)
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
    void GetTestsToRun(string testConfig)
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
                                if (curTestSet != null && foundTests != null && foundTests.Count > 0)
                                {
                                    curTestSet.Tests = (ReliabilityTest[])foundTests.ToArray(typeof(ReliabilityTest));
                                    curTestSet.DiscoveryPaths = (string[])discoveryPaths.ToArray(typeof(string));
                                    discoveryPaths.Clear();
                                    foundTests.Clear();
                                }

                                testLevelStack.Push(concurrentConfigTest);

                                curTestSet = new ReliabilityTestSet();

                                while (currentXML.MoveToNextAttribute())
                                {
                                    XmlDebugOut(" " + currentXML.Name + "=\"" + currentXML.Value + "\"");
                                    switch (currentXML.Name)
                                    {
                                        case "maximumTestRuns":
                                            curTestSet.MaximumLoops = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case "maximumExecutionTime":
                                            string timeValue = currentXML.Value;
                                            curTestSet.MaximumTime = ConvertTimeValueToTestRunTime(timeValue);
                                            break;
                                        case "id":
                                            curTestSet.FriendlyName = currentXML.Value;
                                            break;
                                        case "xmlns:xsi":
                                        case "xmlns:xsd":
                                            break;
                                        case configTestMinimumMem:
                                            curTestSet.MinPercentMem = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configLoggingLevel:
                                            curTestSet.LoggingLevel = (LoggingLevels)Convert.ToInt32(currentXML.Value.ToString(), 16);
                                            break;
                                        case configTestMinimumCPUStaggered:
                                            curTestSet.MinPercentCPUStaggered = currentXML.Value;
                                            break;
                                        case configTestMinimumCPU:
                                            curTestSet.MinPercentCPU = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configInstallDetours:
                                            if (currentXML.Value == "true" || currentXML.Value == "1" || currentXML.Value == "yes")
                                            {
                                                curTestSet.InstallDetours = true;
                                            }
                                            else if (currentXML.Value == "false" || currentXML.Value == "0" || currentXML.Value == "no")
                                            {
                                                curTestSet.InstallDetours = false;
                                            }
                                            else
                                            {
                                                throw new Exception("Unknown value for result reporting: " + currentXML.Value);
                                            }
                                            break;
                                        case configTestMinimumTests:
                                            curTestSet.MinTestsRunning = Convert.ToInt32(currentXML.Value);
                                            break;

                                        case RFConfigOptions.RFConfigOptions_Test_MinMaxTestsUseCPUCount:
                                            if (GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_MinMaxTestsUseCPUCount))
                                            {
                                                int CPUCount = Convert.ToInt32(Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS"));
                                                if (CPUCount <= 0)
                                                    throw new Exception("Invalid Value when reading NUMBER_OF_PROCESSORS: {0}" + CPUCount);
                                                curTestSet.MinTestsRunning = CPUCount;
                                                curTestSet.MaxTestsRunning = (int)(CPUCount * 1.5);
                                            }
                                            break;
                                        case RFConfigOptions.RFConfigOptions_Test_SuppressConsoleOutputFromTests:
                                            curTestSet.SuppressConsoleOutputFromTests = GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_SuppressConsoleOutputFromTests);
                                            break;
                                            
                                        case RFConfigOptions.RFConfigOptions_Test_DebugBreakOnHang:
                                            curTestSet.DebugBreakOnTestHang = GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_DebugBreakOnHang);
                                            break;

                                        case RFConfigOptions.RFConfigOptions_Test_DebugBreakOnBadTest:
                                            curTestSet.DebugBreakOnBadTest = GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_DebugBreakOnBadTest);
                                            break;

                                        case RFConfigOptions.RFConfigOptions_Test_DebugBreakOnOutOfMemory:
                                            curTestSet.DebugBreakOnOutOfMemory = GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_DebugBreakOnOutOfMemory);
                                            break;

                                        case RFConfigOptions.RFConfigOptions_Test_DebugBreakOnPathTooLong:
                                            curTestSet.DebugBreakOnPathTooLong = GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_DebugBreakOnPathTooLong);
                                            break;
                                        
                                        case RFConfigOptions.RFConfigOptions_Test_DebugBreakOnMissingTest:
                                            curTestSet.DebugBreakOnMissingTest = GetTrueFalseOptionValue(currentXML.Value, RFConfigOptions.RFConfigOptions_Test_DebugBreakOnMissingTest);
                                            break;

                                        case configResultReporting:
                                            curTestSet.ReportResults = GetTrueFalseOptionValue(currentXML.Value, configResultReporting);
                                            break;

                                        case configResultReportingUrl:
                                            curTestSet.ReportResultsTo = currentXML.Value;
                                            break;
                                        case configResultReportingBvtCategory:
                                            try
                                            {
                                                curTestSet.BvtCategory = new Guid(currentXML.Value);
                                            }
                                            catch (FormatException)
                                            {
                                                throw new Exception(String.Format("BVT Category Guid {0} is not in the correct form", currentXML.Value));
                                            }
                                            break;
                                        case configTestMaximumTests:
                                            curTestSet.MaxTestsRunning = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configTestDisableLogging:
                                            curTestSet.DisableLogging = GetTrueFalseOptionValue(currentXML.Value, configTestDisableLogging);
                                            break;

                                        case configEnablePerfCounters:
                                            curTestSet.EnablePerfCounters = GetTrueFalseOptionValue(currentXML.Value, configEnablePerfCounters);
                                            break;

                                        case configDefaultTestStartMode:
                                            switch (currentXML.Value)
                                            {
                                                case configTestStartModeAppDomainLoader:
                                                    if (null != curTestSet.DefaultDebugger || null != curTestSet.DefaultDebuggerOptions)
                                                    {
                                                        throw new Exception(String.Format("{0} specified with default debugger or debugger options.  If you want a debugger per test please use {1}=\"{2}\" ",
                                                            configTestStartModeAppDomainLoader,
                                                            configDefaultTestStartMode,
                                                            configTestStartModeProcessLoader));
                                                    }
                                                    curTestSet.DefaultTestStartMode = TestStartModeEnum.AppDomainLoader;
                                                    break;
                                                case configTestStartModeProcessLoader:
                                                    curTestSet.DefaultTestStartMode = TestStartModeEnum.ProcessLoader;
                                                    break;
                                                default:
                                                    throw new Exception(String.Format("Unknown test starter {0} specified!", currentXML.Value));
                                            }
                                            break;
                                        case configRoundRobinAppDomainCount:
                                            try
                                            {
                                                curTestSet.NumAppDomains = Convert.ToInt32(currentXML.Value);
                                                if (curTestSet.NumAppDomains <= 0)
                                                {
                                                    throw new Exception("Number of app domains must be greater than zero!");
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
                                                    curTestSet.AppDomainLoaderMode = AppDomainLoaderMode.FullIsolation;
                                                    break;
                                                case configAppDomainLoaderModeNormal:
                                                    curTestSet.AppDomainLoaderMode = AppDomainLoaderMode.Normal;
                                                    break;
                                                case configAppDomainLoaderModeRoundRobin:
                                                    curTestSet.AppDomainLoaderMode = AppDomainLoaderMode.RoundRobin;
                                                    break;
                                                case configAppDomainLoaderModeLazy:
                                                    curTestSet.AppDomainLoaderMode = AppDomainLoaderMode.Lazy;
                                                    break;

                                                default:
                                                    throw new Exception(String.Format("Unknown AD Loader mode {0} specified!", currentXML.Value));
                                            }
                                            break;
                                        case configPercentPassIsPass:
                                            curTestSet.PercentPassIsPass = Convert.ToInt32(currentXML.Value);
                                            break;

                                        case configDefaultDebugger:
                                            if (currentXML.Value.Length >= 7 && currentXML.Value.Substring(currentXML.Value.Length - 7).ToLower() == "cdb.exe")
                                            {
                                                curTestSet.DefaultDebugger = currentXML.Value;
                                            }
                                            else if (currentXML.Value.Length >= 10 && currentXML.Value.Substring(currentXML.Value.Length - 7).ToLower() == "windbg.exe")
                                            {
                                                curTestSet.DefaultDebugger = currentXML.Value;
                                            }
                                            else if (currentXML.Value.ToLower() == "none")
                                            {
                                                curTestSet.DefaultDebugger = String.Empty;
                                            }
                                            else
                                            {
                                                throw new Exception("Unknown default debugger specified (" + currentXML.Value + ")");
                                            }
                                            break;
                                        case configDefaultDebuggerOptions:
                                            curTestSet.DefaultDebuggerOptions = Environment.ExpandEnvironmentVariables(currentXML.Value);
                                            break;
                                        case configULAssemblyLoadPercent:
                                            curTestSet.ULAssemblyLoadPercent = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configULAppDomainUnloadPercent:
                                            curTestSet.ULAppDomainUnloadPercent = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configULGeneralUnloadPercent:
                                            curTestSet.ULGeneralUnloadPercent = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configULWaitTime:
                                            curTestSet.ULWaitTime = Convert.ToInt32(currentXML.Value);
                                            break;
                                        case configCcFailMail:
                                            curTestSet.CCFailMail = currentXML.Value;
                                            break;
                                        default:
                                            throw new Exception("Unknown attribute (" + currentXML.Name + ") on " + concurrentConfigTest + " tag!");
                                    }
                                }

                                // Check to see if any of the test attribute environment variables are set,
                                // If so, then use the environment variables.
                                if ((Environment.GetEnvironmentVariable("TIMELIMIT") != null) && (Environment.GetEnvironmentVariable("TIMELIMIT") != ""))
                                    curTestSet.MaximumTime = ConvertTimeValueToTestRunTime(Environment.GetEnvironmentVariable("TIMELIMIT"));

                                if ((Environment.GetEnvironmentVariable("MINCPU") != null) && (Environment.GetEnvironmentVariable("MINCPU") != ""))
                                    curTestSet.MinPercentCPU = Convert.ToInt32(Environment.GetEnvironmentVariable("MINCPU"));


                                testSet.Add(curTestSet);
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

                                ReliabilityTest rt = new ReliabilityTest(curTestSet.SuppressConsoleOutputFromTests);
                                rt.TestStartMode = curTestSet.DefaultTestStartMode;

                                // first we need to setup any default options which are set globally on
                                // the test start mode.
                                if (null != curTestSet.DefaultDebugger)
                                {
                                    if (curTestSet.DefaultTestStartMode != TestStartModeEnum.ProcessLoader)
                                    {
                                        throw new Exception(String.Format("{0} specified with default debugger or debugger options.  If you want a debugger per test please use {1}=\"{2}\" ",
                                            configTestStartModeAppDomainLoader,
                                            configDefaultTestStartMode,
                                            configTestStartModeProcessLoader));
                                    }
                                    rt.Debugger = curTestSet.DefaultDebugger;
                                }

                                if (null != curTestSet.DefaultDebuggerOptions)
                                {
                                    if (curTestSet.DefaultTestStartMode != TestStartModeEnum.ProcessLoader)
                                    {
                                        throw new Exception(String.Format("{0} specified with default debugger or debugger options.  If you want a debugger per test please use {1}=\"{2}\" ",
                                            configTestStartModeAppDomainLoader,
                                            configDefaultTestStartMode,
                                            configTestStartModeProcessLoader));
                                    }
                                    rt.DebuggerOptions = curTestSet.DefaultDebuggerOptions;
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
                                            if (TestStartModeEnum.ProcessLoader != curTestSet.DefaultTestStartMode)
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
                                            if (TestStartModeEnum.ProcessLoader != curTestSet.DefaultTestStartMode)
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
                                if (curTestSet.AppDomainLoaderMode == AppDomainLoaderMode.FullIsolation)
                                {
                                    // in this mode each copy of the test is ran in it's own app domain,
                                    // fully isolated from all other copies of the test.  If the user
                                    // specified a cloning level we need to duplicate the test.
                                    testCopies = rt.ConcurrentCopies;
                                    rt.ConcurrentCopies = 1;
                                }
                                else if (curTestSet.AppDomainLoaderMode == AppDomainLoaderMode.RoundRobin)
                                {
                                    // In this mode each test is ran in an app domain w/ other tests.
                                    testCopies = rt.ConcurrentCopies;
                                    rt.ConcurrentCopies = 1;
                                }
                                else
                                {
                                    // Normal mode - tests are ran in app domains w/ copies of themselves
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

        if (curTestSet != null && foundTests != null && foundTests.Count > 0)
        {
            curTestSet.Tests = (ReliabilityTest[])foundTests.ToArray(typeof(ReliabilityTest));
            curTestSet.DiscoveryPaths = (string[])discoveryPaths.ToArray(typeof(string));
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


        if (basepath.LastIndexOf("\\") == (basepath.Length - 1))
        {
            return (basepath + trimmedPath);
        }
        else
        {
            return (basepath + "\\" + trimmedPath);
        }
    }

    /// <summary>
    /// given a path with a filename on it we remove the filename (to get just the base path).
    /// </summary>
    string stripFilenameFromPath(string path)
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
            if (index < testSet.Count && index >= 0)
            {
                return (testSet[index]);
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
        index++;
        if (index >= testSet.Count)
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
        index = 0;
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


