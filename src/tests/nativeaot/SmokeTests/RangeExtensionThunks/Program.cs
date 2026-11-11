// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

// The compiler aligns every method body to a page (--function-alignment) so the 19683
// instantiations of Worker<,,> span more than the 128 MB reach of an arm64 direct branch, and the
// linker places range extension thunks between the code sections the compiler split. Run and Work
// call the rotated instantiation directly and the unboxing stub of Work branches to the method, so
// the direct calls and the stubs whose target is out of range go through thunks, and a stub can
// share the thunk of a direct call to Work.
public interface IWork
{
    int Work(int depth);
}

public struct Worker<T1, T2, T3> : IWork
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Run(int depth) => depth == 0 ? 1 : Worker<T2, T3, T1>.Run(depth - 1) + 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Work(int depth) => depth == 0 ? 1 : default(Worker<T2, T3, T1>).Work(depth - 1) + 1;
}

public struct X00 { }
public struct X01 { }
public struct X02 { }
public struct X03 { }
public struct X04 { }
public struct X05 { }
public struct X06 { }
public struct X07 { }
public struct X08 { }
public struct X09 { }
public struct X10 { }
public struct X11 { }
public struct X12 { }
public struct X13 { }
public struct X14 { }
public struct X15 { }
public struct X16 { }
public struct X17 { }
public struct X18 { }
public struct X19 { }
public struct X20 { }
public struct X21 { }
public struct X22 { }
public struct X23 { }
public struct X24 { }
public struct X25 { }
public struct X26 { }

public static unsafe class Program
{
    private const int Types = 27;
    private const int Count = Types * Types * Types;
    private const long BranchReach = 128 * 1024 * 1024;

    private static readonly nint[] s_run = new nint[Count];
    private static readonly IWork[] s_workers = new IWork[Count];
    private static int s_next;
    private static volatile bool s_stop;

    public static int Main()
    {
        Each();

        // The test needs a direct call and an unboxing stub whose target is out of range.
        int farCalls = 0;
        for (int i = 0; i < Count; i++)
        {
            int rotated = i % (Types * Types) * Types + i / (Types * Types);
            if (Math.Abs((long)(s_run[i] - s_run[rotated])) > BranchReach)
                farCalls++;
        }

        int thunkedStubs = 0;
        for (int i = 0; i < Count; i++)
        {
            Func<int, int> work = s_workers[i].Work;
            if (BranchesThroughThunk(MethodPtr(work)))
                thunkedStubs++;

            DiagnosticMethodInfo info = DiagnosticMethodInfo.Create(work);
            if (info?.Name != "Work" || !info.DeclaringTypeName.EndsWith("Worker`3"))
            {
                Console.WriteLine($"Delegate {i} resolved to {info?.DeclaringTypeName}.{info?.Name}");
                return 1;
            }

            if (work(2) != 3 || ((delegate*<int, int>)s_run[i])(2) != 3)
            {
                Console.WriteLine($"Worker {i} computed the wrong result");
                return 2;
            }
        }

        Console.WriteLine($"{farCalls} calls out of range, {thunkedStubs} unboxing stubs branching through a thunk");
        if (farCalls == 0 || thunkedStubs == 0)
        {
            Console.WriteLine("The code is not laid out as the test expects");
            return 3;
        }

        int collections = SuspendThreadsInThunks();
        Console.WriteLine($"{collections} collections while the threads ran through the thunks");

        if (OperatingSystem.IsLinux() && !DebugInfoLocatesMain())
            return 4;

        return 100;
    }

    private static void Add(delegate*<int, int> run, IWork worker)
    {
        s_run[s_next] = (nint)run;
        s_workers[s_next++] = worker;
    }

    private static void Each()
    {
        Each<X00>();
        Each<X01>();
        Each<X02>();
        Each<X03>();
        Each<X04>();
        Each<X05>();
        Each<X06>();
        Each<X07>();
        Each<X08>();
        Each<X09>();
        Each<X10>();
        Each<X11>();
        Each<X12>();
        Each<X13>();
        Each<X14>();
        Each<X15>();
        Each<X16>();
        Each<X17>();
        Each<X18>();
        Each<X19>();
        Each<X20>();
        Each<X21>();
        Each<X22>();
        Each<X23>();
        Each<X24>();
        Each<X25>();
        Each<X26>();
    }

    private static void Each<T1>()
    {
        Each<T1, X00>();
        Each<T1, X01>();
        Each<T1, X02>();
        Each<T1, X03>();
        Each<T1, X04>();
        Each<T1, X05>();
        Each<T1, X06>();
        Each<T1, X07>();
        Each<T1, X08>();
        Each<T1, X09>();
        Each<T1, X10>();
        Each<T1, X11>();
        Each<T1, X12>();
        Each<T1, X13>();
        Each<T1, X14>();
        Each<T1, X15>();
        Each<T1, X16>();
        Each<T1, X17>();
        Each<T1, X18>();
        Each<T1, X19>();
        Each<T1, X20>();
        Each<T1, X21>();
        Each<T1, X22>();
        Each<T1, X23>();
        Each<T1, X24>();
        Each<T1, X25>();
        Each<T1, X26>();
    }

    private static void Each<T1, T2>()
    {
        Add(&Worker<T1, T2, X00>.Run, new Worker<T1, T2, X00>());
        Add(&Worker<T1, T2, X01>.Run, new Worker<T1, T2, X01>());
        Add(&Worker<T1, T2, X02>.Run, new Worker<T1, T2, X02>());
        Add(&Worker<T1, T2, X03>.Run, new Worker<T1, T2, X03>());
        Add(&Worker<T1, T2, X04>.Run, new Worker<T1, T2, X04>());
        Add(&Worker<T1, T2, X05>.Run, new Worker<T1, T2, X05>());
        Add(&Worker<T1, T2, X06>.Run, new Worker<T1, T2, X06>());
        Add(&Worker<T1, T2, X07>.Run, new Worker<T1, T2, X07>());
        Add(&Worker<T1, T2, X08>.Run, new Worker<T1, T2, X08>());
        Add(&Worker<T1, T2, X09>.Run, new Worker<T1, T2, X09>());
        Add(&Worker<T1, T2, X10>.Run, new Worker<T1, T2, X10>());
        Add(&Worker<T1, T2, X11>.Run, new Worker<T1, T2, X11>());
        Add(&Worker<T1, T2, X12>.Run, new Worker<T1, T2, X12>());
        Add(&Worker<T1, T2, X13>.Run, new Worker<T1, T2, X13>());
        Add(&Worker<T1, T2, X14>.Run, new Worker<T1, T2, X14>());
        Add(&Worker<T1, T2, X15>.Run, new Worker<T1, T2, X15>());
        Add(&Worker<T1, T2, X16>.Run, new Worker<T1, T2, X16>());
        Add(&Worker<T1, T2, X17>.Run, new Worker<T1, T2, X17>());
        Add(&Worker<T1, T2, X18>.Run, new Worker<T1, T2, X18>());
        Add(&Worker<T1, T2, X19>.Run, new Worker<T1, T2, X19>());
        Add(&Worker<T1, T2, X20>.Run, new Worker<T1, T2, X20>());
        Add(&Worker<T1, T2, X21>.Run, new Worker<T1, T2, X21>());
        Add(&Worker<T1, T2, X22>.Run, new Worker<T1, T2, X22>());
        Add(&Worker<T1, T2, X23>.Run, new Worker<T1, T2, X23>());
        Add(&Worker<T1, T2, X24>.Run, new Worker<T1, T2, X24>());
        Add(&Worker<T1, T2, X25>.Run, new Worker<T1, T2, X25>());
        Add(&Worker<T1, T2, X26>.Run, new Worker<T1, T2, X26>());
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_methodPtr")]
    private static extern ref IntPtr MethodPtr(Delegate d);

    // Decodes the unboxing stub "add x0, x0, #8; b target" and tells whether the branch lands on
    // a range extension thunk "adrp x16, page; add x16, x16, #offset; br x16".
    private static bool BranchesThroughThunk(nint stub)
    {
        uint* code = (uint*)stub;
        if (code[0] != 0x91002000 || (code[1] & 0xfc000000) != 0x14000000)
            throw new Exception($"No unboxing stub at {stub:x}");

        uint* target = (uint*)(stub + 4 + ((int)(code[1] << 6) >> 4));
        return (target[0] & 0x9f00001f) == 0x90000010
            && (target[1] & 0xffc003ff) == 0x91000210
            && target[2] == 0xd61f0200;
    }

    // The threads spend much of their time in the thunks, where the runtime finds no method and
    // must let the thread run on when it suspends them for a collection.
    private static int SuspendThreadsInThunks()
    {
        Thread[] threads = new Thread[Math.Max(2, Environment.ProcessorCount - 1)];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                while (!s_stop)
                {
                    foreach (IWork worker in s_workers)
                        worker.Work(2);
                }
            });
            threads[i].Start();
        }

        int collections = 0;
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 2000)
        {
            GC.Collect();
            collections++;
        }

        s_stop = true;
        foreach (Thread thread in threads)
            thread.Join();
        return collections;
    }

    // The order file places Main in the first code section. Its debug information relocates
    // against that section's own symbol, which each section of a split code section needs.
    private static bool DebugInfoLocatesMain()
    {
        string llvmDwarfDump = Path.Combine(
            Environment.GetEnvironmentVariable("CORE_ROOT"),
            "SuperFileCheck",
            "runtimes",
            RuntimeInformation.RuntimeIdentifier,
            "native",
            "llvm-dwarfdump");

        if (!File.Exists(llvmDwarfDump))
        {
            Console.WriteLine("Could not find llvm-dwarfdump at path: " + llvmDwarfDump);
            return false;
        }

        nint loadAddress = 0;
        foreach (string mapping in File.ReadLines("/proc/self/maps"))
        {
            if (mapping.EndsWith(Environment.ProcessPath))
            {
                loadAddress = (nint)Convert.ToInt64(mapping.Substring(0, mapping.IndexOf('-')), 16);
                break;
            }
        }

        nint linkAddress = (nint)(delegate*<int>)&Main - loadAddress;
        ProcessTextOutput result = Process.RunAndCaptureText(new ProcessStartInfo(llvmDwarfDump, [$"--lookup=0x{linkAddress:x}", Environment.ProcessPath])
        {
            InheritedHandles = [],
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });

        if (!result.StandardOutput.Contains("RangeExtensionThunks_Program__Main"))
        {
            Console.WriteLine($"llvm-dwarfdump found no Main at 0x{linkAddress:x}:");
            Console.WriteLine(result.StandardOutput);
            return false;
        }

        return true;
    }
}
