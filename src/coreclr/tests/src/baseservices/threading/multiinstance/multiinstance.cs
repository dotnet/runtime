// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Threading;
using System.Collections.Generic;
using TestLibrary;

#if WINCORESYS
[assembly: AllowPartiallyTrustedCallers]
#endif

[SecuritySafeCritical]
public class MultiThreading
{
    #region ParamsDefintion
    private struct CommandLineParams
    {
        public string assemblyName;
        public string[] args;
        public int workers;
    }
    #endregion Params

    #region Data
    static private CommandLineParams cmdParams;
    static private MethodInfo methToExec;
    static private List<int> retVals;
    static private object myLock;
    static private ManualResetEvent[] events;
    #endregion Data


    #region Utilities
    private static void Usage()
    {
        Console.WriteLine("Usage: MultipleInstance.exe /workers:#### /exe:#####");
        Console.WriteLine("/workers: run test in n threads.");
    }

    private static bool ParseArguments(string[] args, ref CommandLineParams cmdParams)
    {
        bool ret = true;
        cmdParams.workers = 0;
        try
        {
            int index = 0;
            for (int i = 0; i < args.Length; i++)
            {
                string name = "";
                string value = "";

                if (args[i].Contains(":"))
                {
                    name = args[i].Substring(0, args[i].IndexOf(":")).ToLower();
                    value = args[i].Substring(args[i].IndexOf(":") + 1);
                }

                if (name.StartsWith("/workers"))
                {
                    cmdParams.workers = Convert.ToInt32(value);
                }
                else if (name.StartsWith("/exe"))
                {
                    cmdParams.assemblyName = value;
                    index = i;
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid paramter: {0}", args[i]);
                    ret = false;
                }
            }
            if (index == args.Length - 1)
            {
                cmdParams.args = new string[0];
            }
            else
            {
                cmdParams.args = new string[args.Length - index - 1];
                for (int i = index + 1; i < args.Length; i++)
                {
                    cmdParams.args[i - index - 1] = args[i];
                }
            }


        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            ret = false;
        }
        return ret;
    }

    public static Byte[] ReadFileContents(String path)
    {
        byte[] bytes;
        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            // Do a blocking read
            int index = 0;
            long fileLength = fs.Length;
            if (fileLength > Int32.MaxValue)
                throw new IOException("File size greater then 2Gb");
            int count = (int)fs.Length;
            bytes = new byte[count];
            while (count > 0)
            {
                int n = fs.Read(bytes, index, count);
                if (n == 0)
                    throw new EndOfStreamException();
                index += n;
                count -= n;
            }
        }
        return bytes;
    }

    private static MethodInfo GetEntryPoint(string assemblyName)
    {
        Assembly asm = null;
        string asmName = "";


        asmName = assemblyName;

        try
        {
            asmName = assemblyName;
            if (asmName.LastIndexOf(".") > -1)
            {
                asmName = asmName.Substring(0, asmName.LastIndexOf("."));
            }
            if (asmName.LastIndexOf(@"\") > -1)
            {
                asmName = asmName.Substring(asmName.LastIndexOf(@"\") + 1);
            }
            if (asmName.LastIndexOf(@"/") > -1)
            {
                asmName = asmName.Substring(asmName.LastIndexOf(@"/") + 1);
            }

            asm = Assembly.Load( new AssemblyName(asmName) );
        }
        catch (FileLoadException)
        {
            asm = null;
        }
        catch (FileNotFoundException)
        {
            asm = null;
        }

        if (null == asm)
        {
            MethodInfo assemload = null;
            foreach(MethodInfo m in typeof(Assembly).GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
            {
                ParameterInfo[] mps = m.GetParameters();
                if (m.Name.Equals("Load") && mps.Length == 1 && mps[0].GetType() == typeof(byte[])) assemload = m;
            }
            if (assemload == null) {
                throw new Exception("Failed to get Assembly.Load(int8[]) method");
            }

            asm = (Assembly)assemload.Invoke(null, new object[1] { ReadFileContents( assemblyName ) });
        }

        methToExec = asm.EntryPoint;

        return methToExec;
    }

    private static void Worker()
    {
        if (cmdParams.workers > 1)
            WaitHandle.WaitAny(events);

        RunMain();
    }

    private static Thread NewThread()
    {
        Thread t = null;
        t = new Thread(new ThreadStart(Worker));
        return t;
    }
    #endregion Utilities

    #region Main
    public static int Main(string[] args)
    {
        if (0 == args.Length)
        {
            Usage();
            return 0;
        }

        cmdParams = new CommandLineParams();
        if (!ParseArguments(args, ref cmdParams))
        {
            Usage();
            return 0;
        }

        retVals = new List<int>();
        myLock = new object();

        Run();  //run test...

        int retVal = retVals[0];
        for (int i = 0; i < retVals.Count - 1; i++)
        {
            if (retVals[i] != retVals[i + 1])
            {
                Logging.WriteLine("Failed");
                retVal = 0xff;
                break;
            }
        }
        return retVal;
    }

    private static void Run()
    {
        methToExec = GetEntryPoint(cmdParams.assemblyName);
        int threadNum = cmdParams.workers;
        if (threadNum == 0)
        {
            Logging.WriteLine("Run test in the main thread");
            RunMain();
        }
        else if (threadNum == 1)
        {
            Logging.WriteLine("Run test in a worker thread");
            Thread t = NewThread();
            t.Start();
            t.Join();
        }
        else if (threadNum > 1)
        {
            Logging.WriteLine("Run test in the main thread and spawn {0} threads to run test", threadNum - 1);

            events = new ManualResetEvent[1];
            events[0] = new ManualResetEvent(false);
          
            Thread[] threads = new Thread[threadNum - 1];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = NewThread();
                threads[i].Start();
            }
            events[0].Set();
            RunMain();
            foreach (Thread t in threads)
            {
                t.Join();
            }
        }
    }




    private static void RunMain()
    {
        int numMainParams = methToExec.GetParameters().Length;
        if (numMainParams > 1)
        {
            throw new Exception("Main method must contain 0 or 1 arguments.");
        }

        DateTime time1 = DateTime.Now;
        Object oExitCode = null;
        while (true)
        {
            oExitCode = methToExec.Invoke(null, numMainParams == 1 ? new Object[] { cmdParams.args } : null);
            DateTime time2 = DateTime.Now;
            TimeSpan span = time2 - time1;
            if (span.TotalMilliseconds > 1500)
                break;
            Thread.Sleep(0);
        }


        int iRetCode;
        if (methToExec.ReturnParameter.ParameterType == typeof(void))
        {
            //iRetCode = Environment.ExitCode;
            Console.WriteLine("WARNING: Unable to get the exit code from a void Main method");
            iRetCode = 100;
        }
        else if (methToExec.ReturnParameter.ParameterType == typeof(int))
        {
            iRetCode = (int)oExitCode;
        }
        else if (methToExec.ReturnParameter.ParameterType == typeof(uint))
        {
            iRetCode = (int)(uint)oExitCode;
        }
        else
        {
            throw new Exception("Main method must return void, int or uint not " + methToExec.ReturnParameter.ParameterType.ToString());
        }

        lock (myLock)
        {
            retVals.Add(iRetCode);
        }
    }
    #endregion
}


