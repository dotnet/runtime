// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Reflection;

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// <summary>
/// The reliability class is the place where we keep track of information for each individual test.  ReliabilityConfiguration
/// first builds a hashtable of ReliabilityTest's (index by their name attribute) available in the primary configuration file,
/// and then uses this hashtable to pull out the actual tests which we want to run (from the test config file) by the ID attribute
/// on each test specified.
/// </summary>
public class ReliabilityTest 
#if !PROJECTK_BUILD
    : ICloneable
#endif
{
    private bool suppressConsoleOutput = false;

    private string assembly, debugger, debuggerOptions;
    private string basePath;
#if PROJECTK_BUILD
    private MethodInfo entryPointMethod = null;
#endif    
    private string refOrID;
    private string arguments;
    private string entryPoint;
    private int successCode = 0;
    private int runCount = 0;
#if !PROJECTK_BUILD
    private AppDomain appDomain;
#endif
    private Object testObject;
    private int concurrentCopies = 1;
    private int runningCount = 0;
    private int expectedDuration = -1;
    private bool requiresSDK = false, hasFailed = false;
    private Guid guid = Guid.Empty;
    TestStartModeEnum testStartMode = TestStartModeEnum.AppDomainLoader;
    DateTime startTime = DateTime.Now;
    string testOwner = null;
    private string resultFilename = null;
    private List<ReliabilityTest> group;
    private List<string> preCommands;
    private List<string> postCommands;
    private int appDomainIndex = 0;
    private TestAttributes testAttrs = TestAttributes.None;
    private CustomActionType customAction = CustomActionType.None;
    bool testLoadFailed = false;
    int index;

    public ReliabilityTest(bool suppressConsoleOutput)
    {
        SuppressConsoleOutput = suppressConsoleOutput;
    }

    public void TestStarted()
    {
        Interlocked.Increment(ref runCount);
        Interlocked.Increment(ref runningCount);        
    }

    public void TestStopped()
    {
        Interlocked.Decrement(ref runningCount);
    }

    public bool SuppressConsoleOutput
    {
        get { return suppressConsoleOutput; }
        set { suppressConsoleOutput = value; }
    }

    public DateTime StartTime
    {
        get
        {
            return (startTime);
        }
        set
        {
            startTime = value;
        }
    }

    public TestAttributes TestAttrs
    {
        get
        {
            return (testAttrs);
        }
        set
        {
            testAttrs = value;
        }
    }

    public int ConcurrentCopies
    {
        get
        {
            return (concurrentCopies);
        }
        set
        {
            concurrentCopies = value;
        }
    }

    /// <summary>
    /// RunningCount is the number of instances of this test which are currently running
    /// </summary>
    public int RunningCount
    {
        get
        {
            return (runningCount);
        }
        set
        {
            runningCount = value;
        }
    }

    public Object TestObject
    {
        get
        {
            return (testObject);
        }
        set
        {
            testObject = value;
        }
    }

    public bool TestLoadFailed
    {
        get
        {
            return (testLoadFailed);
        }
        set
        {
            testLoadFailed = value;
        }
    }

#if !PROJECTK_BUILD
    public AppDomain AppDomain
    {
        get
        {
            return (appDomain);
        }
        set
        {
            appDomain = value;
        }
    }
#endif
    public string Assembly
    {
        get
        {
            return (assembly);
        }
        set
        {
            assembly = value;
        }
    }
    public string RefOrID
    {
        get
        {
            return (refOrID);
        }
        set
        {
            refOrID = value;
        }
    }
    public string Arguments
    {
        get
        {
            return (arguments);
        }
        set
        {
            arguments = value;
        }
    }

    public string[] GetSplitArguments()
    {
        if (arguments == null)
        {
            return (null);
        }
        //TODO: Handle quotes intelligently.
        return (arguments.Split(' '));
    }

    public string EntryPoint
    {
        get
        {
            return (entryPoint);
        }
        set
        {
            entryPoint = value;
        }
    }
    public int SuccessCode
    {
        get
        {
            return (successCode);
        }
        set
        {
            successCode = value;
        }
    }

    /// <summary>
    /// RunCount is the total times this test has been run
    /// </summary>
    public int RunCount
    {
        get
        {
            return (runCount);
        }
        set
        {
            runCount = value;
        }
    }

    public bool RequiresSDK
    {
        get
        {
            return (requiresSDK);
        }
        set
        {
            requiresSDK = value;
        }
    }

#if PROJECTK_BUILD
    public MethodInfo EntryPointMethod
    {
        get
        {
            return entryPointMethod;
        }
        set
        {
            entryPointMethod = value;
        }
    }
#endif 

    public string BasePath
    {
        get
        {
            if (basePath == null)
            {
#if PROJECTK_BUILD
                string strBVTRoot = Environment.GetEnvironmentVariable("BVT_ROOT");
                if (String.IsNullOrEmpty(strBVTRoot))
                    return (Directory.GetCurrentDirectory() + "\\Tests");
                else
                    return strBVTRoot;
#else
                return (String.Empty);
#endif
            }

            if (basePath.Length > 0)
            {
                if (basePath[basePath.Length - 1] != '\\')
                {
                    basePath = basePath + "\\";
                }
            }
            return (basePath);
        }
        set
        {
            basePath = value;
        }
    }

    /// <summary>
    /// returns the debugger, with full path.  On assignment it should just be a simple filename (eg, "cdb", "windbg", or "ntsd")
    /// </summary>
    public string Debugger
    {
        get
        {
            if (debugger == null || debugger == String.Empty)
            {
                return (debugger);
            }

            // first, check the current directory
            string curDir = Directory.GetCurrentDirectory();
            string theAnswer;
            if (File.Exists(theAnswer = String.Format("{0}\\{1}", curDir, debugger)))
            {
                return (theAnswer);
            }

            // now check the path.
            string path = Environment.GetEnvironmentVariable("PATH");
            if (path == null)
            {
                return (debugger);
            }

            string[] splitPath = path.Split(new char[] { ';' });
            foreach (string curPath in splitPath)
            {
                if (File.Exists(theAnswer = String.Format("{0}\\{1}", curPath, debugger)))
                {
                    return (theAnswer);
                }
            }
            return (debugger);
        }
        set
        {
            debugger = value;
        }
    }

    public string DebuggerOptions
    {
        get
        {
            return (debuggerOptions);
        }
        set
        {
            debuggerOptions = value;
        }
    }

    // Expected duration of the test, in minutes
    public int ExpectedDuration
    {
        get
        {
            return (expectedDuration);
        }
        set
        {
            expectedDuration = value;
        }
    }

    public TestStartModeEnum TestStartMode
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

    public Guid Guid
    {
        get
        {
            return (guid);
        }
        set
        {
            guid = value;
        }
    }

    public string TestOwner
    {
        get
        {
            return (testOwner);
        }
        set
        {
            testOwner = value;
        }
    }

    /// <summary>
    /// This stores the filename used to get the result back from batch files on Win9x systems (exitcode.exe stores it in this file, we read it back)
    /// </summary>
    public string ResultFilename
    {
        get
        {
            return (resultFilename);
        }
        set
        {
            resultFilename = value;
        }
    }
    
    public List<ReliabilityTest> Group
    {
        get
        {
            return (group);
        }
        set
        {
            group = value;
        }
    }
     

    public List<string> PreCommands
    {
        get
        {
            return (preCommands);
        }
        set
        {
            preCommands = value;
        }
    }

    public List<string> PostCommands
    {
        get
        {
            return (postCommands);
        }
        set
        {
            postCommands = value;
        }
    }
    
    public bool HasFailed
    {
        get
        {
            return (hasFailed);
        }
        set
        {
            hasFailed = value;
        }
    }

    public int AppDomainIndex
    {
        get
        {
            return (this.appDomainIndex);
        }
        set
        {
            appDomainIndex = value;
        }
    }

    public int Index
    {
        get
        {
            return (index);
        }
        set
        {
            index = value;
        }
    }

    public CustomActionType CustomAction
    {
        get
        {
            return (customAction);
        }
        set
        {
            customAction = value;
        }
    }

    public override string ToString()
    {
        return ("Ref/ID: " + refOrID + " Assembly: " + assembly + " Arguments: " + arguments + " SuccessCode: " + successCode.ToString());
    }

    public object Clone()
    {
        return (this.MemberwiseClone());
    }
}

