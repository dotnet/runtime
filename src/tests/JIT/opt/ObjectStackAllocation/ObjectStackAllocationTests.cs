// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ObjectStackAllocation
{
    class SimpleClassA
    {
        public int f1;
        public int f2;

        public SimpleClassA(int f1, int f2)
        {
            this.f1 = f1;
            this.f2 = f2;
        }
    }

    sealed class SimpleClassB
    {
        public long f1;
        public long f2;

        public SimpleClassB(long f1, long f2)
        {
            this.f1 = f1;
            this.f2 = f2;
        }
    }

    sealed class SimpleClassWithGCField : SimpleClassA
    {
        public object o;

        public SimpleClassWithGCField(int f1, int f2, object o) : base(f1, f2)
        {
            this.o = o;
        }
    }

    class ClassWithGCFieldAndInt
    {
        public object o;
        public int i;

        public ClassWithGCFieldAndInt(int i, object o)
        {
            this.o = o;
            this.i = i;
        }
    }

    class ClassWithNestedStruct
    {
        public ClassWithNestedStruct(int f1, int f2)
        {
            ns.f1 = f1;
            ns.f2 = f2;
            ns.s.f1 = f1;
            ns.s.f2 = f2;
        }

        public NestedStruct ns;
    }

    struct SimpleStruct
    {
        public int f1;
        public int f2;
    }

    struct NestedStruct
    {
        public int f1;
        public int f2;
        public SimpleStruct s;
    }

    enum AllocationKind
    {
        Heap,
        Stack,
        Undefined
    }

    class Tests
    {
        static volatile int f1 = 5;
        static volatile int f2 = 7;
        static SimpleClassA classA;
        static SimpleClassWithGCField classWithGCField;
        static string str0;
        static string str1;
        static string str2;
        static string str3;
        static string str4;

        delegate int Test();

        static int methodResult = 100;

        public static int Main()
        {
            AllocationKind expectedAllocationKind = AllocationKind.Stack;
            if (GCStressEnabled()) {
                Console.WriteLine("GCStress is enabled");
                expectedAllocationKind = AllocationKind.Undefined;
            }
            else if (!SPCOptimizationsEnabled() && !Crossgen2Test()) {
                Console.WriteLine("System.Private.CoreLib.dll optimizations are disabled");
                expectedAllocationKind = AllocationKind.Heap;
            }

            classA = new SimpleClassA(f1, f2);

            classWithGCField = new SimpleClassWithGCField(f1, f2, null);

            str0 = "str_zero";
            str1 = "str_one";
            str2 = "str_two";
            str3 = "str_three";
            str4 = "str_four";

            CallTestAndVerifyAllocation(AllocateSimpleClassAndAddFields, 12, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassesAndEQCompareThem, 0, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassesAndNECompareThem, 1, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassAndGetField, 7, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateClassWithNestedStructAndGetField, 5, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateClassWithNestedStructAndAddFields, 24, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassWithGCFieldAndAddFields, 12, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassAndAssignRefToAField, 12, expectedAllocationKind);

            CallTestAndVerifyAllocation(TestMixOfReportingAndWriteBarriers, 34, expectedAllocationKind);

            // The object is currently allocated on the stack when this method is jitted and on the heap when it's R2R-compiled.
            // The reason is that we always do the type check via helper in R2R mode, which blocks stack allocation.
            // We don't have to use a helper in this case (even for R2R), https://github.com/dotnet/coreclr/issues/22086 tracks fixing that.
            CallTestAndVerifyAllocation(AllocateSimpleClassAndCheckTypeNoHelper, 1, AllocationKind.Undefined);

            CallTestAndVerifyAllocation(AllocateClassWithGcFieldAndInt, 5, expectedAllocationKind);

            // The remaining tests currently never allocate on the stack
            if (expectedAllocationKind == AllocationKind.Stack) {
                expectedAllocationKind = AllocationKind.Heap;
            }

            // This test calls CORINFO_HELP_ISINSTANCEOFCLASS
            CallTestAndVerifyAllocation(AllocateSimpleClassAndCheckTypeHelper, 1, expectedAllocationKind);

            // This test calls CORINFO_HELP_CHKCASTCLASS_SPECIAL
            CallTestAndVerifyAllocation(AllocateSimpleClassAndCast, 7, expectedAllocationKind);

            // Stack allocation of boxed structs is currently disabled
            CallTestAndVerifyAllocation(BoxSimpleStructAndAddFields, 12, expectedAllocationKind);

            return methodResult;
        }

        static bool SPCOptimizationsEnabled()
        {
            Assembly objectAssembly = Assembly.GetAssembly(typeof(object));
            object[] attribs = objectAssembly.GetCustomAttributes(typeof(DebuggableAttribute),
                                                        false);
            DebuggableAttribute debuggableAttribute = attribs[0] as DebuggableAttribute;
            return ((debuggableAttribute == null) || !debuggableAttribute.IsJITOptimizerDisabled);
        }

        static bool GCStressEnabled()
        {
            return Environment.GetEnvironmentVariable("COMPlus_GCStress") != null;
        }

        static bool Crossgen2Test()
        {
            // CrossGen2 doesn't respect the debuggable attribute
            return Environment.GetEnvironmentVariable("RunCrossGen2") != null;
        }

        static void CallTestAndVerifyAllocation(Test test, int expectedResult, AllocationKind expectedAllocationsKind)
        {
            long allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
            int testResult = test();
            long allocatedBytesAfter = GC.GetAllocatedBytesForCurrentThread();
            string methodName = test.Method.Name;

            if (testResult != expectedResult) {
                Console.WriteLine($"FAILURE ({methodName}): expected {expectedResult}, got {testResult}");
                methodResult = -1;
            }
            else if ((expectedAllocationsKind == AllocationKind.Stack) && (allocatedBytesBefore != allocatedBytesAfter)) {
                Console.WriteLine($"FAILURE ({methodName}): unexpected allocation of {allocatedBytesAfter - allocatedBytesBefore} bytes");
                methodResult = -1;
            }
            else if ((expectedAllocationsKind == AllocationKind.Heap) && (allocatedBytesBefore == allocatedBytesAfter)) {
                Console.WriteLine($"FAILURE ({methodName}): unexpected stack allocation");
                methodResult = -1;
            }
            else {
                Console.WriteLine($"SUCCESS ({methodName})");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndAddFields()
        {
            SimpleClassA a = new SimpleClassA(f1, f2);
            GC.Collect();
            return a.f1 + a.f2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassesAndEQCompareThem()
        {
            SimpleClassA a1 = new SimpleClassA(f1, f2);
            SimpleClassA a2 = (f1 == 0) ? a1 : new SimpleClassA(f2, f1);
            GC.Collect();
            return (a1 == a2) ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassesAndNECompareThem()
        {
            SimpleClassA a1 = new SimpleClassA(f1, f2);
            SimpleClassA a2 = (f1 == 0) ? a1 : new SimpleClassA(f2, f1);
            GC.Collect();
            return (a1 != a2) ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndCheckTypeNoHelper()
        {
            object o = (f1 == 0) ? (object)new SimpleClassB(f1, f2) : (object)new SimpleClassA(f1, f2);
            GC.Collect();
            return (o is SimpleClassB) ? 0 : 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndCheckTypeHelper()
        {
            object o = (f1 == 0) ? (object)new SimpleClassB(f1, f2) : (object)new SimpleClassA(f1, f2);
            GC.Collect();
            return !(o is SimpleClassA) ? 0 : 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndCast()
        {
            object o = (f1 == 0) ? (object)new SimpleClassB(f1, f2) : (object)new SimpleClassA(f2, f1);
            GC.Collect();
            return ((SimpleClassA)o).f1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndGetField()
        {
            SimpleClassA a = new SimpleClassA(f1, f2);
            GC.Collect();
            ref int f = ref a.f2;
            return f;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateClassWithNestedStructAndGetField()
        {
            ClassWithNestedStruct c = new ClassWithNestedStruct(f1, f2);
            GC.Collect();
            ref int f = ref c.ns.s.f1;
            return f;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateClassWithNestedStructAndAddFields()
        {
            ClassWithNestedStruct c = new ClassWithNestedStruct(f1, f2);
            GC.Collect();
            return c.ns.f1 + c.ns.f2 + c.ns.s.f1 + c.ns.s.f2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassWithGCFieldAndAddFields()
        {
            SimpleClassWithGCField c = new SimpleClassWithGCField(f1, f2, null);
            GC.Collect();
            return c.f1 + c.f2;
        }

        static int AllocateSimpleClassAndAssignRefToAField()
        {
            SimpleClassWithGCField c = new SimpleClassWithGCField(f1, f2, null);
            GC.Collect();
            c.o = classA;
            return c.f1 + c.f2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int BoxSimpleStructAndAddFields()
        {
            SimpleStruct str;
            str.f1 = f1;
            str.f2 = f2;
            object boxedSimpleStruct = (object)str;
            GC.Collect();
            return ((SimpleStruct)boxedSimpleStruct).f1 + ((SimpleStruct)boxedSimpleStruct).f2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestMixOfReportingAndWriteBarriers()
        {
            // c1 doesn't escape and is allocated on the stack
            SimpleClassWithGCField c1 = new SimpleClassWithGCField(f1, f2, str0);

            // c2 always points to a heap-allocated object
            SimpleClassWithGCField c2 = classWithGCField;

            // c2 and c3 may point to a heap-allocated object or to a stack-allocated object
            SimpleClassWithGCField c3 = (f1 == 0) ? c1 : c2;
            SimpleClassWithGCField c4 = (f2 == 0) ? c2 : c1;

            // c1 doesn't have to be reported to GC (but can be conservatively reported as an interior pointer)
            // c1.o should be reported to GC as a normal pointer (but can be conservatively reported as an interior pointer)
            // c2 should be reported to GC as a normal pointer (but can be conservatively reported as an interior pointer)
            // c3 and c4 must be reported as interior pointers
            GC.Collect();

            // This assignment doesn't need a write barrier but may conservatively use a checked barrier
            c1.o = str1;
            // This assignment should optimally use a normal write barrier but may conservatively use a checked barrier
            c2.o = str2;
            // These assignments require a checked write barrier
            c3.o = str3;
            c4.o = str4;

            return c1.o.ToString().Length + c2.o.ToString().Length + c3.o.ToString().Length + c4.o.ToString().Length;
        }

        static int AllocateClassWithGcFieldAndInt()
        {
            ClassWithGCFieldAndInt c = new ClassWithGCFieldAndInt(f1, null);
            GC.Collect();
            return c.i;
        }
    }
}
