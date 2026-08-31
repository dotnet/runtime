// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace StackCommitTest
{
    public unsafe class WinApi
    {
#pragma warning disable 618
        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo([MarshalAs(UnmanagedType.Struct)] ref SYSTEM_INFO lpSystemInfo);
#pragma warning restore 618

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            internal PROCESSOR_INFO_UNION uProcessorInfo;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public IntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort dwProcessorLevel;
            public ushort dwProcessorRevision;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct PROCESSOR_INFO_UNION
        {
            [FieldOffset(0)]
            internal uint dwOemId;
            [FieldOffset(0)]
            internal ushort wProcessorArchitecture;
            [FieldOffset(2)]
            internal ushort wReserved;
        }

        [DllImport("kernel32")]
        public static extern IntPtr VirtualQuery(void* address, ref MEMORY_BASIC_INFORMATION buffer, IntPtr length);

        public struct MEMORY_BASIC_INFORMATION
        {
            public byte* BaseAddress;
            public byte* AllocationBase;
            public int AllocationProtect;
            public IntPtr RegionSize;
            public MemState State;
            public int Protect;
            public int Type;
        }

        [Flags]
        public enum MemState
        {
            MEM_COMMIT = 0x1000,
            MEM_RESERVE = 0x2000,
            MEM_FREE = 0x10000,
        }


        public const int PAGE_GUARD = 0x100;

    }

    unsafe public static class Utility
    {
        public static Int64 PageSize { get; private set; }

        static Utility()
        {
            WinApi.SYSTEM_INFO sysInfo = new WinApi.SYSTEM_INFO();

            WinApi.GetSystemInfo(ref sysInfo);

            PageSize = (Int64)sysInfo.dwPageSize;
        }

        public static void GetStackExtents(out byte* stackBase, out long stackSize)
        {
            WinApi.MEMORY_BASIC_INFORMATION info = new WinApi.MEMORY_BASIC_INFORMATION();
            WinApi.VirtualQuery(&info, ref info, new IntPtr(sizeof(WinApi.MEMORY_BASIC_INFORMATION)));
            stackBase = info.AllocationBase;
            stackSize = (info.BaseAddress - info.AllocationBase) + info.RegionSize.ToInt64();
        }

        public static List<WinApi.MEMORY_BASIC_INFORMATION> GetRegionsOfStack()
        {
            byte* stackBase;
            long stackSize;
            GetStackExtents(out stackBase, out stackSize);

            List<WinApi.MEMORY_BASIC_INFORMATION> result = new List<WinApi.MEMORY_BASIC_INFORMATION>();

            byte* current = stackBase;
            while (current < stackBase + stackSize)
            {
                WinApi.MEMORY_BASIC_INFORMATION info = new WinApi.MEMORY_BASIC_INFORMATION();
                WinApi.VirtualQuery(current, ref info, new IntPtr(sizeof(WinApi.MEMORY_BASIC_INFORMATION)));
                result.Add(info);
                current = info.BaseAddress + info.RegionSize.ToInt64();
            }

            result.Reverse();
            return result;
        }


        public static bool ValidateStack(string threadName, bool shouldBePreCommitted, Int32 expectedStackSize)
        {
            bool result = true;

            byte* stackBase;
            long stackSize;
            GetStackExtents(out stackBase, out stackSize);

            Console.WriteLine("{2} -- Base: {0:x}, Size: {1}kb", new IntPtr(stackBase).ToInt64(), stackSize / 1024, threadName);

            //
            // Start at the highest addresses, which should be committed (because that's where we're currently running).
            // The next region should be committed, but marked as a guard page.
            // After that, we'll either find committed pages, or reserved pages, depending on whether the runtime
            // is pre-committing stacks.
            //
            bool foundGuardRegion = false;

            foreach (var info in GetRegionsOfStack())
            {
                string regionType = string.Empty;

                if (!foundGuardRegion)
                {
                    if ((info.Protect & WinApi.PAGE_GUARD) != 0)
                    {
                        foundGuardRegion = true;
                        regionType = "guard region";
                    }
                    else
                    {
                        regionType = "active region";
                    }
                }
                else
                {
                    if (shouldBePreCommitted)
                    {
                        if (!info.State.HasFlag(WinApi.MemState.MEM_COMMIT))
                        {
                            // If we pre-commit the stack, the last 1 or 2 pages are left "reserved" (they are the "hard guard region")
                            // ??? How to decide whether it is 1 or 2 pages?
                            if ((info.BaseAddress != stackBase || info.RegionSize.ToInt64() > PageSize))
                            {
                                result = false;
                                regionType = "<---- should be pre-committed";
                            }
                        }
                    }
                    else
                    {
                        if (info.State.HasFlag(WinApi.MemState.MEM_COMMIT))
                        {
                            result = false;
                            regionType = "<---- should not be pre-committed";
                        }
                    }
                }

                Console.WriteLine(
                    "{0:x8}-{1:x8} {2,5:g}kb {3,-11:g} {4}",
                    new IntPtr(info.BaseAddress).ToInt64(),
                    new IntPtr(info.BaseAddress + info.RegionSize.ToInt64()).ToInt64(),
                    info.RegionSize.ToInt64() / 1024,
                    info.State,
                    regionType);
            }

            if (!foundGuardRegion)
            {
                result = false;

                Console.WriteLine("Did not find GuardRegion for the whole stack");
            }

            if (expectedStackSize != -1 && stackSize != expectedStackSize)
            {
                result = false;

                Console.WriteLine("Stack size is not as expected: actual -- {0}, expected -- {1}", stackSize, expectedStackSize);
            }

            Console.WriteLine();
            return result;
        }

        static private bool RunTestItem(string threadName, bool shouldBePreCommitted, Int32 expectedThreadSize, Action<Action> runOnThread)
        {
            bool result = false;
            ManualResetEventSlim mre = new ManualResetEventSlim();

            runOnThread(() =>
            {
                result = Utility.ValidateStack(threadName, shouldBePreCommitted, expectedThreadSize);
                mre.Set();
            });

            mre.Wait();
            return result;
        }

        static public bool RunTest(bool shouldBePreCommitted)
        {
            if (RunTestItem("Main", shouldBePreCommitted, -1, action => action()) &
                RunTestItem("ThreadPool", shouldBePreCommitted, -1, action => ThreadPool.QueueUserWorkItem(state => action())) &
                RunTestItem("new Thread()", shouldBePreCommitted, -1, action => new Thread(() => action()).Start()) &
                //RunTestItem("new Thread(512kb)", true, 512 * 1024, action => new Thread(() => action(), 512 * 1024).Start()) &
                RunTestItem("Finalizer", shouldBePreCommitted, -1, action => Finalizer.Run(action)))
            {
                return true;
            }

            return false;
        }
    }

    public class Finalizer
    {
        Action m_action;
        private Finalizer(Action action) { m_action = action; }
        ~Finalizer() { m_action(); }

        public static void Run(Action action)
        {
            //We need to allocate the object inside of a separate method to ensure that
            //the reference will be eliminated before GC.Collect is called. Technically
            //even across methods we probably don't make any formal guarantees but this
            //is sufficient for current runtime implementations.
            CreateUnreferencedObject(action);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void CreateUnreferencedObject(Action action)
        {
            new Finalizer(action);
        }
    }
}
