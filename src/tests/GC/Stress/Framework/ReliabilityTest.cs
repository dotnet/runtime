// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Runtime.Loader;
using System.Reflection;
using System.Runtime.CompilerServices;

public class TestAssemblyLoadContext : AssemblyLoadContext
{
    List<string> _privatePaths = new List<string>();
    string _applicationBase;
    public TestAssemblyLoadContext(string name, string applicationBase = null, string[] paths = null) : base(true)
    {
        FriendlyName = name;

        SetPaths(applicationBase, paths);
    }

    public void SetPaths(string applicationBase, string[] paths)
    {
        _applicationBase = applicationBase;
        if (paths != null)
        {
            _privatePaths.AddRange(paths);
        }
    }

    public void AppendPrivatePath(string path)
    {
        _privatePaths.Add(path);
    }

    public int ExecuteAssemblyByName(string name, string[] args)
    {
        return ExecuteAssembly(name + ".dll", args);

    }

    public int ExecuteAssembly(string path, string[] args)
    {
        Assembly assembly = LoadFromAssemblyPath(Path.Combine(_applicationBase, path));
        object[] actualArgs = new object[] { args != null ? args : new string[0] };
        return (int)assembly.EntryPoint.Invoke(null, actualArgs);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        Assembly assembly = null;
        foreach (string path in _privatePaths)
        {
            try
            {
                assembly = LoadFromAssemblyPath(Path.Combine(path, assemblyName.Name + ".dll"));
                break;
            }
            catch (Exception)
            {
            }
        }

        if (assembly == null)
        {
            try
            {
                assembly = LoadFromAssemblyPath(Path.Combine(_applicationBase, assemblyName.Name + ".dll"));
            }
            catch (Exception)
            {
            }
        }

        return assembly;
    }

    public string FriendlyName { get; private set; }
}

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
    private bool _suppressConsoleOutput = false;

    private string _assembly, _debugger, _debuggerOptions;
    private string _basePath;
#if PROJECTK_BUILD
    private MethodInfo _entryPointMethod = null;
#endif    
    private string _refOrID;
    private string _arguments;
    private string _entryPoint;
    private int _successCode = 0;
    private int _runCount = 0;
#if !PROJECTK_BUILD
    private AppDomain appDomain;
#endif
    private TestAssemblyLoadContext _assemblyLoadContext;
    private Object _testObject;
    private object _myLoader;
    private int _concurrentCopies = 1;
    private int _runningCount = 0;
    private int _expectedDuration = -1;
    private bool _requiresSDK = false, _hasFailed = false;
    private Guid _guid = Guid.Empty;
    private TestStartModeEnum _testStartMode = TestStartModeEnum.AppDomainLoader;
    private DateTime _startTime = DateTime.Now;
    private string _testOwner = null;
    private string _resultFilename = null;
    private List<ReliabilityTest> _group;
    private List<string> _preCommands;
    private List<string> _postCommands;
    private int _appDomainIndex = 0;
    private int _assemblyLoadContextIndex = 0;
    private TestAttributes _testAttrs = TestAttributes.None;
    private CustomActionType _customAction = CustomActionType.None;
    private bool _testLoadFailed = false;
    private int _index;

    public ReliabilityTest(bool suppressConsoleOutput)
    {
        SuppressConsoleOutput = suppressConsoleOutput;
    }

    public void TestStarted()
    {
        Interlocked.Increment(ref _runCount);
        Interlocked.Increment(ref _runningCount);
    }

    public void TestStopped()
    {
        Interlocked.Decrement(ref _runningCount);
    }

    public bool SuppressConsoleOutput
    {
        get { return _suppressConsoleOutput; }
        set { _suppressConsoleOutput = value; }
    }

    public DateTime StartTime
    {
        get
        {
            return (_startTime);
        }
        set
        {
            _startTime = value;
        }
    }

    public TestAttributes TestAttrs
    {
        get
        {
            return (_testAttrs);
        }
        set
        {
            _testAttrs = value;
        }
    }

    public int ConcurrentCopies
    {
        get
        {
            return (_concurrentCopies);
        }
        set
        {
            _concurrentCopies = value;
        }
    }

    /// <summary>
    /// RunningCount is the number of instances of this test which are currently running
    /// </summary>
    public int RunningCount
    {
        get
        {
            return (_runningCount);
        }
        set
        {
            _runningCount = value;
        }
    }

    public Object TestObject
    {
        get
        {
            return (_testObject);
        }
        set
        {
            _testObject = value;
        }
    }

    public Object MyLoader
    {
        get
        {
            return (_myLoader);
        }
        set
        {
            _myLoader = value;
        }
    }

    public bool TestLoadFailed
    {
        get
        {
            return (_testLoadFailed);
        }
        set
        {
            _testLoadFailed = value;
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

    public TestAssemblyLoadContext AssemblyLoadContext
    {
        get
        {
            return _assemblyLoadContext;
        }
        set
        {
            _assemblyLoadContext = value;
        }
    }


    public bool HasAssemblyLoadContext
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get { return _assemblyLoadContext != null; }
    }

    public string AssemblyLoadContextName
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get { return _assemblyLoadContext.FriendlyName; }
    }

    public string Assembly
    {
        get
        {
            return (_assembly);
        }
        set
        {
            _assembly = value;
        }
    }
    public string RefOrID
    {
        get
        {
            return (_refOrID);
        }
        set
        {
            _refOrID = value;
        }
    }
    public string Arguments
    {
        get
        {
            return (_arguments);
        }
        set
        {
            _arguments = value;
        }
    }

    public string[] GetSplitArguments()
    {
        if (_arguments == null)
        {
            return (null);
        }
        //TODO: Handle quotes intelligently.
        return (_arguments.Split(' '));
    }

    public string EntryPoint
    {
        get
        {
            return (_entryPoint);
        }
        set
        {
            _entryPoint = value;
        }
    }
    public int SuccessCode
    {
        get
        {
            return (_successCode);
        }
        set
        {
            _successCode = value;
        }
    }

    /// <summary>
    /// RunCount is the total times this test has been run
    /// </summary>
    public int RunCount
    {
        get
        {
            return (_runCount);
        }
        set
        {
            _runCount = value;
        }
    }

    public bool RequiresSDK
    {
        get
        {
            return (_requiresSDK);
        }
        set
        {
            _requiresSDK = value;
        }
    }

#if PROJECTK_BUILD
    public MethodInfo EntryPointMethod
    {
        get
        {
            return _entryPointMethod;
        }
        set
        {
            _entryPointMethod = value;
        }
    }
#endif 

    public string BasePath
    {
        get
        {
            if (_basePath == null)
            {
#if PROJECTK_BUILD
                string strBVTRoot = Environment.GetEnvironmentVariable("BVT_ROOT");
                if (String.IsNullOrEmpty(strBVTRoot))
                    return Path.Combine(Directory.GetCurrentDirectory(), "Tests");
                else
                    return strBVTRoot;
#else
                return (String.Empty);
#endif
            }

            if (_basePath.Length > 0)
            {
                if (_basePath[_basePath.Length - 1] != Path.PathSeparator)
                {
                    _basePath = _basePath + Path.PathSeparator;
                }
            }
            return (_basePath);
        }
        set
        {
            _basePath = value;
        }
    }

    /// <summary>
    /// returns the debugger, with full path.  On assignment it should just be a simple filename (eg, "cdb", "windbg", or "ntsd")
    /// </summary>
    public string Debugger
    {
        get
        {
            if (_debugger == null || _debugger == String.Empty)
            {
                return (_debugger);
            }

            // first, check the current directory
            string curDir = Directory.GetCurrentDirectory();
            string theAnswer;
            if (File.Exists(theAnswer = Path.Combine(curDir, _debugger)))
            {
                return (theAnswer);
            }

            // now check the path.
            string path = Environment.GetEnvironmentVariable("PATH");
            if (path == null)
            {
                return (_debugger);
            }

            string[] splitPath = path.Split(new char[] { ';' });
            foreach (string curPath in splitPath)
            {
                if (File.Exists(theAnswer = Path.Combine(curPath, _debugger)))
                {
                    return (theAnswer);
                }
            }
            return (_debugger);
        }
        set
        {
            _debugger = value;
        }
    }

    public string DebuggerOptions
    {
        get
        {
            return (_debuggerOptions);
        }
        set
        {
            _debuggerOptions = value;
        }
    }

    // Expected duration of the test, in minutes
    public int ExpectedDuration
    {
        get
        {
            return (_expectedDuration);
        }
        set
        {
            _expectedDuration = value;
        }
    }

    public TestStartModeEnum TestStartMode
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

    public Guid Guid
    {
        get
        {
            return (_guid);
        }
        set
        {
            _guid = value;
        }
    }

    public string TestOwner
    {
        get
        {
            return (_testOwner);
        }
        set
        {
            _testOwner = value;
        }
    }

    /// <summary>
    /// This stores the filename used to get the result back from batch files on Win9x systems (exitcode.exe stores it in this file, we read it back)
    /// </summary>
    public string ResultFilename
    {
        get
        {
            return (_resultFilename);
        }
        set
        {
            _resultFilename = value;
        }
    }

    public List<ReliabilityTest> Group
    {
        get
        {
            return (_group);
        }
        set
        {
            _group = value;
        }
    }


    public List<string> PreCommands
    {
        get
        {
            return (_preCommands);
        }
        set
        {
            _preCommands = value;
        }
    }

    public List<string> PostCommands
    {
        get
        {
            return (_postCommands);
        }
        set
        {
            _postCommands = value;
        }
    }

    public bool HasFailed
    {
        get
        {
            return (_hasFailed);
        }
        set
        {
            _hasFailed = value;
        }
    }

    public int AppDomainIndex
    {
        get
        {
            return (_appDomainIndex);
        }
        set
        {
            _appDomainIndex = value;
        }
    }

    public int AssemblyLoadContextIndex
    {
        get
        {
            return (_assemblyLoadContextIndex);
        }
        set
        {
            _assemblyLoadContextIndex = value;
        }
    }

    public int Index
    {
        get
        {
            return (_index);
        }
        set
        {
            _index = value;
        }
    }

    public CustomActionType CustomAction
    {
        get
        {
            return (_customAction);
        }
        set
        {
            _customAction = value;
        }
    }

    public override string ToString()
    {
        return ("Ref/ID: " + _refOrID + " Assembly: " + _assembly + " Arguments: " + _arguments + " SuccessCode: " + _successCode.ToString());
    }

    public object Clone()
    {
        return (this.MemberwiseClone());
    }

    public int ExecuteInAssemblyLoadContext()
    {
        int exitCode;

        AssemblyLoadContext.AppendPrivatePath(BasePath);

        // Execute the test.
        if (Assembly.ToLower().IndexOf(".exe") == -1 && Assembly.ToLower().IndexOf(".dll") == -1) // must be a simple name or fullname...
        {
            exitCode = AssemblyLoadContext.ExecuteAssemblyByName(Assembly, GetSplitArguments());
        }
        else
        {
            exitCode = AssemblyLoadContext.ExecuteAssembly(Assembly, GetSplitArguments());
        }

        return exitCode;
    }
}

