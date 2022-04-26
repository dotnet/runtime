// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

// This test is testing control flow guard.
//
// Since the only observable behavior of control flow guard is that it terminates the process,
// to test various scenarios the test re-runs itself. The main executable finishes with
// exit code 100 on success. The subprocesses are all expected to be killed by control
// flow guard and exit with exit code C0000409.
//
// The "corrupted" indirect call target is located in a piece of memory we VirtualAlloc'd.
// By default VirtualAlloc'd RWX memory is considered valid call target.
// The s_armed static variable controls whether we ask the OS to consider the VirtualAlloc'd
// memory an invalid call target.
unsafe class ControlFlowGuardTests
{
    static Func<int>[] s_scenarios =
        {
            TestFunctionPointer.Run,
            TestDelegate.Run,
            TestCorruptingVTable.Run,
        };

    static bool s_armed;

    static int Main(string[] args)
    {
        // Are we running the control program?
        if (args.Length == 0)
        {
            // Dry run - execute all scenarios while s_armed is false.
            //
            // The replaced call target will not be considered invalid by CFG and none of this
            // should crash. This is a safeguard to make sure the only reason why the subordinate
            // programs could exit with a FailFast is CFG, and not some other bug in the test logic.
            Console.WriteLine("*** Dry run ***");
            foreach (Func<int> scenario in s_scenarios)
                scenario();

            // Now launch subordinate processes and check they FailFast
            for (int i = 0; i < s_scenarios.Length; i++)
            {
                Console.WriteLine($"*** Scenario {i} ***");
                Process p = Process.Start(new ProcessStartInfo(Environment.ProcessPath, i.ToString()));
                p.WaitForExit();
                if ((p.ExitCode != -1073740791) && (p.ExitCode != 57005))
                {
                    Console.WriteLine($"FAIL: Scenario exited with exit code {p.ExitCode}");
                    return 1;
                }
                else
                {
                    Console.WriteLine($"Crashed as expected.");
                }
            }

            return 100;
        }

        [DllImport("kernel32", ExactSpelling = true)]
        static extern uint GetErrorMode();

        [DllImport("kernel32", ExactSpelling = true)]
        static extern uint SetErrorMode(uint uMode);

        // Don't pop the WER dialog box that blocks the process until someone clicks Close.
        SetErrorMode(GetErrorMode() | 0x0002 /* NOGPFAULTERRORBOX */);

        // VirtualAlloc should specify TARGETS_INVALID
        s_armed = true;

        // Run specified subordinate program
        if (int.TryParse(args[0], out int index) && ((uint)index) < (uint)s_scenarios.Length)
            return s_scenarios[index]();

        // Subordinate program unknown
        return 10;
    }

    class TestFunctionPointer
    {
        public static int Run()
        {
            var target = (delegate*<void>)CreateNewMethod();
            target();
            Console.WriteLine("Was able to call the pointer");
            return 1;
        }
    }

    class TestDelegate
    {
        class RawData
        {
            public IntPtr FirstField;
        }

        public static int Run()
        {
            Func<int> del = Run;

            // Replace the delegate destination
            Span<IntPtr> delegateMemory = MemoryMarshal.CreateSpan(ref Unsafe.As<RawData>(del).FirstField, 4);
            int slotIndex = delegateMemory.IndexOf((IntPtr)(delegate*<int>)&Run);
            if (slotIndex < 0)
            {
                Console.WriteLine("Target not found in the delegate?");
                return 1;
            }
            delegateMemory[slotIndex] = CreateNewMethod();

            del();

            Console.WriteLine("Was able to call the modified delegate");
            return 2;
        }
    }

    class TestCorruptingVTable
    {
        class Test<T>
        {
            public override string ToString() => "TotallyUniqueString";
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:UnrecognizedReflectionPattern",
            Justification = "Hiding the parameter to Type.GetType on purpose")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:UnrecognizedReflectionPattern",
            Justification = "MakeGenericType is over a reference type")]
        public static int Run()
        {
            // Obscure `typeof(string)` so that dataflow analysis can't see it and the MakeGenericType
            // call produces a freshly allocated vtable (not a vtable in the readonly data segment of
            // the executable that we wouldn't be able to overwrite).
            Type stringType = Type.GetType(new StringBuilder("System.").Append("String").ToString());
            Type testOfString = typeof(Test<>).MakeGenericType(stringType);

            // Patch the MethodTable of Test<string>: find the vtable slot with the ToString method
            // and replace it with a new value that is not in the control flow guard bitmask.
            IntPtr toStringMethod = testOfString.GetMethod("ToString").MethodHandle.GetFunctionPointer();
            var methodTableMemory = new Span<IntPtr>((void*)testOfString.TypeHandle.Value, 64);
            int slotIndex = methodTableMemory.IndexOf(toStringMethod);
            if (slotIndex < 0)
            {
                Console.WriteLine("ToString method not found in the MethodTable?");
                return 1;
            }
            methodTableMemory[slotIndex] = CreateNewMethod();

            // Allocate the type and call the corrupted virtual slot
            object o = Activator.CreateInstance(testOfString);
            o.ToString();

            // CFG should have stopped the party
            Console.WriteLine("Was able to call the modified slot");
            return 2;
        }
    }

    static IntPtr CreateNewMethod()
    {
        [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
        static extern IntPtr VirtualAlloc(IntPtr lpAddress, nuint dwSize, int flAllocationType, int flProtect);

        int flProtect = 0x40 /* EXEC_READWRITE */;

        if (s_armed)
            flProtect |= 0x40000000 /* TARGETS_INVALID */;

        IntPtr address = VirtualAlloc(
            lpAddress: IntPtr.Zero,
            dwSize: 4096,
            flAllocationType: 0x00001000 | 0x00002000 /* COMMIT+RESERVE*/,
            flProtect: flProtect);

        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
            case Architecture.X86:
                *((byte*)address) = 0xC3; // ret
                break;
            case Architecture.Arm64:
                *((uint*)address) = 0xD65F03C0; // ret
                break;
            case Architecture.Arm:
                *((ushort*)address) = 0x46F7; // mov pc, lr
                break;
            default:
                throw new NotSupportedException();
        }

        return address;
    }
}
