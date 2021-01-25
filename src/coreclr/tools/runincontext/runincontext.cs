// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

public class ArgInput
{
    public bool Verbose = false;
    public string AsmName = null;
    public string AssemblyPath = null;
    public string[] EntryArgs;
    public StringBuilder EntryArgStr = null;
    public bool StressMode = false;
    public int StressModeCount = 2000;
    public int MaxRestarts = 10;
    public int IterationCount = 1;
    public bool MonitorMode = false;
    public int IterationsToSkip = 500;
    public string ReferencesPath = null;
    public bool BreakBeforeRun;
    public bool BreakAfterRun;
    public bool DelegateLoad;
    public bool BreakOnUnloadFailure;

    public static void DisplayUsage()
    {
        Console.WriteLine("Usage: RunInContext.exe [options ...] <Assembly file name> [assembly command line options]");
        Console.WriteLine("    /v                        Verbose mode");
        Console.WriteLine("    /collectstress:<n>        Emit in collectible assembly n times, checking for memory leaks(def:2000)");
        Console.WriteLine("    /maxstressrestarts:<n>    Maximum allowed stress run restarts when memory usage increases (def:10)");
        Console.WriteLine("    /iterationcount:<n>       Number of iterations in non-stress mode (def:1)");
        Console.WriteLine("    /memorymonitor:<skip>     Monitor memory usage closely. skip: number of iterations to skip before monitoring memory.");
        Console.WriteLine("    /referencespath:<path>    Path to resolve assemblies referenced by the main assembly");
        Console.WriteLine("    /breakbeforerun           Break into debugger before executing the assembly");
        Console.WriteLine("    /breakafterrun            Break into debugger after executing the assembly");
        Console.WriteLine("    /breakonunloadfailure     Break into debugger on unload failure");
        Console.WriteLine("    /delegateload             Delegate the AssemblyLoadContext.Load to a secondary AssemblyLoadContext");
    }

    public ArgInput(String[] args)
    {
        EntryArgStr = new StringBuilder();
        var assemblyArgs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            string option = args[i].ToLower();

            if (option.StartsWith("/v"))
            {
                Verbose = true;
            }
            else if (option.StartsWith("/collectstress"))
            {
                StressMode = true;
                if (option.Length > 14 && option[14] == ':')
                {
                    StressModeCount = int.Parse(option.Substring(15));
                }
            }
            else if (option.StartsWith("/iterationcount:"))
            {
                IterationCount = int.Parse(option.Substring(16));
            }
            else if (option.StartsWith("/breakbeforerun"))
            {
                BreakBeforeRun = true;
            }
            else if (option.StartsWith("/breakafterrun"))
            {
                BreakAfterRun = true;
            }
            else if (option.StartsWith("/breakonunloadfailure"))
            {
                BreakOnUnloadFailure = true;
            }
            else if (option.StartsWith("/delegateload"))
            {
                DelegateLoad = true;
            }
            else if (option.StartsWith("/maxstressrestarts:"))
            {
                MaxRestarts = int.Parse(option.Substring(19));
            }
            else if (option.StartsWith("/memorymonitor:"))
            {
                MonitorMode = true;
                IterationsToSkip = int.Parse(option.Substring(15));
            }
            else if (option.StartsWith("/referencespath:"))
            {
                ReferencesPath = Path.GetFullPath(args[i].Substring(16));
            }
            else
            {
                // The remaining arguments are the assembly name and its parameters
                AsmName = args[i];
                AssemblyPath = Path.GetDirectoryName(Path.GetFullPath(AsmName));

                for (i++; i < args.Length; i++)
                {
                    assemblyArgs.Add(args[i]);
                    if (args[i].Contains(" ") || args[i].Contains("\t"))
                    {
                        EntryArgStr.Append($"\"{args[i]}\"");
                    }
                    else
                    {
                        EntryArgStr.Append(args[i]);
                    }
                    EntryArgStr.Append(" ");
                }
            }
        }
        EntryArgs = assemblyArgs.ToArray();

        if (StressModeCount < 50)
        {
            Console.WriteLine("The number of stress runs is less that the minimum (50). Defaulting to 50");
            StressModeCount = 50;
        }
        if (!MonitorMode && (MaxRestarts < 5))
        {
            Console.WriteLine("The number of stress run restarts is less that the minimum (5). Defaulting to 5");
            MaxRestarts = 5;
        }
    }
}

abstract class TestAssemblyLoadContextBase : AssemblyLoadContext
{
    public TestAssemblyLoadContextBase() : base(true)
    {

    }
    public virtual void Cleanup()
    {

    }
}

class TestAssemblyLoadContext : TestAssemblyLoadContextBase
{
    public List<WeakReference> _assemblyReferences;
    string _assemblyDirectory;
    string _referencesDirectory;

    public TestAssemblyLoadContext(string assemblyDirectory, string referencesDirectory, List<WeakReference> assemblyReferences)
    {
        _assemblyDirectory = assemblyDirectory;
        _referencesDirectory = referencesDirectory;
        _assemblyReferences = assemblyReferences;
    }

    protected override Assembly Load(AssemblyName name)
    {
        Assembly assembly = null;
        try
        {
            assembly = LoadFromAssemblyPath(Path.Combine(_referencesDirectory, name.Name + ".dll"));
        }
        catch (Exception)
        {
            try
            {
                assembly = LoadFromAssemblyPath(Path.Combine(_assemblyDirectory, name.Name + ".dll"));
            }
            catch (Exception)
            {
                assembly = LoadFromAssemblyPath(Path.Combine(_assemblyDirectory, name.Name + ".exe"));
            }
        }

        lock(_assemblyReferences)
        {
            _assemblyReferences.Add(new WeakReference(assembly));
        }
        return assembly;
    }
}

class TestAssemblyLoadContextDelegating : TestAssemblyLoadContextBase
{
    public TestAssemblyLoadContextBase _delegateContext;

    public TestAssemblyLoadContextDelegating(TestAssemblyLoadContextBase delegateContext)
    {
        _delegateContext = delegateContext;
    }

    public override void Cleanup()
    {
        _delegateContext.Cleanup();
        _delegateContext = null;
    }

    protected override Assembly Load(AssemblyName name)
    {
        Assembly asm = _delegateContext.LoadFromAssemblyName(name);
        return asm;
    }
}

public class UnloadFailedException : Exception
{

}

public class TestRunner
{
    ArgInput _input;

    public TestRunner(ArgInput input)
    {
        _input = input;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int DoWorkNonStress()
    {
        int retVal = 0;

        for (int i = 0; i < _input.IterationCount; i++)
        {
            retVal = ExecuteAssembly();
            if (retVal != RunInContext.SuccessExitCode)
            {
                break;
            }
        }

        return retVal;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int DoWorkStress()
    {
        //Stress mode

        Process p = Process.GetCurrentProcess();
        long startMemory = p.PrivateMemorySize64;
        long currentMemory = startMemory;
        long monitorStartMem = 0;
        long startSpeed = 0;
        long currentSpeed = 0;
        int restarts = 0;
        long leak = 0;
        int i;
        int lastProgressReport = 0;
        int retVal = 0;

        for (i = 1; i <= _input.StressModeCount; i++)
        {
            if (!_input.MonitorMode && (((i * 1.0) / _input.StressModeCount) * 100 > lastProgressReport))
            {
                Console.WriteLine("Completed: {0}%...", lastProgressReport);
                lastProgressReport += 10;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            retVal = ExecuteAssembly();
            sw.Stop();

            if (retVal != RunInContext.SuccessExitCode)
            {
                break;
            }

            currentSpeed = sw.ElapsedMilliseconds;

            p = Process.GetCurrentProcess();
            currentMemory = p.PrivateMemorySize64;
            if (_input.MonitorMode)
            {
                if (i == _input.IterationsToSkip)
                {
                    startMemory = monitorStartMem = currentMemory;
                }

                if (currentMemory > startMemory)
                {
                    Console.WriteLine($"\n\n +++ Memory usage increased by {currentMemory - startMemory} bytes at iteration {i}!!                                                 ");
                    if (i > _input.IterationsToSkip)
                    {
                        leak = (long)((currentMemory - monitorStartMem) / ((i - _input.IterationsToSkip) * 1.0));
                    }
                    startMemory = currentMemory;
                }
                else if (currentMemory < startMemory)
                {
                    Console.WriteLine($"\n\n --- Memory usage decreased by {startMemory - currentMemory} bytes at iteration {i}                                                 ");
                    startMemory = currentMemory;
                }

                Console.Write($"Private Memory Size = {currentMemory / 1024}K after {i + 1} iterations.");

                if (i > _input.IterationsToSkip)
                {
                    Console.Write($" Average leak: {leak} bytes/iteration. speed: {(int)currentSpeed} ms/type.");
                }

                Console.WriteLine();
            }
            else
            {
                if (currentMemory > startMemory)
                {
                    leak = (currentMemory - startMemory) / i;
                    Console.WriteLine($"LOOP #{i}: Memory usage increased by {_input.MaxRestarts - restarts - 1} bytes! Restarting test... ({currentMemory - startMemory} restarts left)");
                    Console.WriteLine($"    + Average leak over the last {i} iterations: {leak} bytes\n");

                    restarts++;
                    if (restarts == _input.MaxRestarts)
                    {
                        break;
                    }
                    i = 0;
                    startMemory = currentMemory;
                    leak = 0;
                    lastProgressReport = 0;
                    continue;
                }
            }

            if ((i == 2) && (startSpeed == 0))
            {
                startSpeed = currentSpeed;
            }
        }

        //sometimes this happens (no real reason, but it's not a failure, so let's not write a "negative" leak to the output)
        if (currentMemory < startMemory)
        {
            startMemory = currentMemory;
            leak = 0;
        }
        if (_input.MonitorMode)
        {
            leak = (long)((currentMemory - monitorStartMem) / ((_input.StressModeCount * 1.0) - _input.IterationsToSkip));
            startMemory = monitorStartMem;
        }

        Console.WriteLine("\n==================================================");
        Console.WriteLine($"Starting memory size          : {startMemory / 1024} KB");
        Console.WriteLine($"Ending memory size            : {currentMemory / 1024} KB");
        Console.WriteLine();
        Console.WriteLine($"Starting emission speed       : {startSpeed} milliseconds");
        Console.WriteLine($"Ending emission speed         : {currentSpeed} milliseconds");
        Console.WriteLine();
        Console.WriteLine($"Memory leak                   : {currentMemory - startMemory} bytes ({leak} bytes per iteration).");
        if (currentMemory > startMemory)
        {
            throw new Exception("Memory leaked");
        }

        return retVal;
    }

    public int ExecuteAssemblyEntryPoint(MethodInfo entryPoint)
    {
        int result = 0;

        object res;
        object[] args = (entryPoint.GetParameters().Length != 0) ? new object[] { _input.EntryArgs } : null;
        string argsStr = (args == null) ? "" : _input.EntryArgStr.ToString();

        if (_input.Verbose)
        {
            Console.WriteLine($"Invoking Main({argsStr})\n");
        }

        res = entryPoint.Invoke(null, args);

        result = (entryPoint.ReturnType == typeof(void)) ? Environment.ExitCode : Convert.ToInt32(res);

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ExecuteAndUnload(List<WeakReference> assemblyReferences, out WeakReference testAlcWeakRef, out WeakReference testAlcWeakRefInner)
    {
        int result;
        TestAssemblyLoadContextBase testAlc = new TestAssemblyLoadContext(_input.AssemblyPath, _input.ReferencesPath, assemblyReferences);

        if (_input.DelegateLoad)
        {
            testAlcWeakRefInner = new WeakReference(testAlc, trackResurrection: true);
            testAlc = new TestAssemblyLoadContextDelegating(testAlc);
        }
        else
        {
            testAlcWeakRefInner = new WeakReference(null);
        }

        testAlcWeakRef = new WeakReference(testAlc, trackResurrection: true);

        Assembly inputAssembly = null;
        try
        {
            inputAssembly = testAlc.LoadFromAssemblyPath(_input.AsmName);
        }
        catch (Exception LoadEx)
        {
            Console.WriteLine($"Failed to load assembly <{_input.AsmName}>!");
            Console.WriteLine($"Exception: {LoadEx.ToString()}");
            throw;
        }

        assemblyReferences.Add(new WeakReference(inputAssembly));

        Stopwatch sw = new Stopwatch();
        sw.Start();
        result = ExecuteAssemblyEntryPoint(inputAssembly.EntryPoint);
        sw.Stop();

        if (_input.Verbose)
        {
            Console.WriteLine($"Execution time: {sw.Elapsed}");

            foreach (WeakReference wr in assemblyReferences)
            {
                if (wr.Target != null)
                {
                    Console.WriteLine("Unloading Assembly [" + wr.Target + "]");
                }
            }
        }

        testAlc.Cleanup();
        testAlc.Unload();

        testAlc = null;
        inputAssembly = null;

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    bool VerifyAssembliesUnloaded(List<WeakReference> assemblyReferences)
    {
        bool unloadSucceeded = true;

        foreach (WeakReference wr in assemblyReferences)
        {
            if (wr.Target != null)
            {
                if (_input.Verbose)
                {
                    Console.WriteLine("FAILURE: Assembly [" + wr.Target + "] was not unloaded!");
                }
                unloadSucceeded = false;
            }
        }

        return unloadSucceeded;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    int ExecuteAssembly()
    {
        List<WeakReference> assemblyReferences = new List<WeakReference>();
        WeakReference testAlcWeakRef;
        WeakReference testAlcWeakRefInner;

        if (_input.BreakBeforeRun)
        {
            Debugger.Break();
        }

        int result = ExecuteAndUnload(assemblyReferences, out testAlcWeakRef, out testAlcWeakRefInner);

        for (int i = 0; (testAlcWeakRef.IsAlive || testAlcWeakRefInner.IsAlive) && (i < 100); i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(10);
        }

        if (_input.BreakAfterRun)
        {
            Debugger.Break();
        }

        bool unloadSucceeded = VerifyAssembliesUnloaded(assemblyReferences);

        if (!unloadSucceeded)
        {
            if (_input.BreakOnUnloadFailure)
            {
                Debugger.Break();
            }

            throw new UnloadFailedException();
        }

        return result;
    }
}

public class RunInContext
{
    public static int FailureExitCode = 213;
    public static int SuccessExitCode = 100;

    public static int Main(String[] args)
    {
        if (args.Length == 0)
        {
            ArgInput.DisplayUsage();
            return FailureExitCode;
        }

        ArgInput input = new ArgInput(args);
        TestRunner runner = new TestRunner(input);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        int retVal = FailureExitCode;
        try
        {
            if (!input.StressMode)
            {
                retVal = runner.DoWorkNonStress();
            }
            else
            {
                retVal = runner.DoWorkStress();
            }
        }
        catch (UnloadFailedException)
        {
            Console.WriteLine($"FAILURE: Unload failed");
        }
        catch (Exception ex)
        {
            if (input.Verbose)
            {
                Console.WriteLine($"FAILURE: Exception: {ex.ToString()}");
            }
            else
            {
                Console.WriteLine($"FAILURE: Exception: {ex.Message}");
            }
        }

        string status = (retVal == FailureExitCode) ? "FAIL" : "PASS";

        Console.WriteLine();
        Console.WriteLine($"RunInContext {status}! Exiting with code {retVal}");

        return retVal;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Console.WriteLine($"RunInContext FAIL! Exiting due to unhandled exception in the test: {e.ExceptionObject}");
        Environment.Exit(FailureExitCode);
    }

}
