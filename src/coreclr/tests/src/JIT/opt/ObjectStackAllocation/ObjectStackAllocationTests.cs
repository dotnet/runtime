// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    class SimpleClassWithGCField : SimpleClassA
    {
        public object o;

        public SimpleClassWithGCField(int f1, int f2, object o) : base(f1, f2)
        {
            this.o = o;
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

        delegate int Test();

        static int methodResult = 100;

        public static int Main()
        {
            AllocationKind expectedAllocationKind = AllocationKind.Stack;
            if (GCStressEnabled()) {
                expectedAllocationKind = AllocationKind.Undefined;
            }
            else if (!SPCOptimizationsEnabled()) {
                expectedAllocationKind = AllocationKind.Heap;
            }

            CallTestAndVerifyAllocation(AllocateSimpleClassAndAddFields, 12, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassesAndEQCompareThem, 0, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassesAndNECompareThem, 1, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassAndGetField, 7, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateClassWithNestedStructAndGetField, 5, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateClassWithNestedStructAndAddFields, 24, expectedAllocationKind);

            // The remaining tests currently never allocate on the stack
            if (expectedAllocationKind == AllocationKind.Stack) {
                expectedAllocationKind = AllocationKind.Heap;
            }

            // This test calls CORINFO_HELP_ISINSTANCEOFCLASS
            CallTestAndVerifyAllocation(AllocateSimpleClassAndCheckType, 1, expectedAllocationKind);

            // This test calls CORINFO_HELP_CHKCASTCLASS_SPECIAL
            CallTestAndVerifyAllocation(AllocateSimpleClassAndCast, 7, expectedAllocationKind);

            // Stack allocation of classes with GC fields is currently disabled
            CallTestAndVerifyAllocation(AllocateSimpleClassWithGCFieldAndAddFields, 12, expectedAllocationKind);

            // Assigning class ref to a field of another object currently always disables stack allocation
            CallTestAndVerifyAllocation(AllocateSimpleClassAndAssignRefToAField, 12, expectedAllocationKind);

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

        static void CallTestAndVerifyAllocation(Test test, int expectedResult, AllocationKind expectedAllocationsKind)
        {
            // Run the test once to exclude any allocations during jitting, etc.
            //test();
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
            return (a1 == a2) ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassesAndNECompareThem()
        {
            SimpleClassA a1 = new SimpleClassA(f1, f2);
            SimpleClassA a2 = (f1 == 0) ? a1 : new SimpleClassA(f2, f1);
            return (a1 != a2) ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndCheckType()
        {
            object o = (f1 == 0) ? (object)new SimpleClassB(f1, f2) : (object)new SimpleClassA(f1, f2);
            return (o is SimpleClassB) || !(o is SimpleClassA) ? 0 : 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndCast()
        {
            object o = (f1 == 0) ? (object)new SimpleClassB(f1, f2) : (object)new SimpleClassA(f2, f1);
            return ((SimpleClassA)o).f1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndGetField()
        {
            SimpleClassA a = new SimpleClassA(f1, f2);
            ref int f = ref a.f2;
            return f;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateClassWithNestedStructAndGetField()
        {
            ClassWithNestedStruct c = new ClassWithNestedStruct(f1, f2);
            ref int f = ref c.ns.s.f1;
            return f;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateClassWithNestedStructAndAddFields()
        {
            ClassWithNestedStruct c = new ClassWithNestedStruct(f1, f2);
            return c.ns.f1 + c.ns.f2 + c.ns.s.f1 + c.ns.s.f2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassWithGCFieldAndAddFields()
        {
            SimpleClassWithGCField c = new SimpleClassWithGCField(f1, f2, null);
            return c.f1 + c.f2;
        }

        static int AllocateSimpleClassAndAssignRefToAField()
        {
            SimpleClassWithGCField c = new SimpleClassWithGCField(f1, f2, null);
            SimpleClassA a = new SimpleClassA(f1, f2);
            c.o = a;
            return c.f1 + c.f2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int BoxSimpleStructAndAddFields()
        {
            SimpleStruct str;
            str.f1 = f1;
            str.f2 = f2;
            object boxedSimpleStruct = (object)str;
            return ((SimpleStruct)boxedSimpleStruct).f1 + ((SimpleStruct)boxedSimpleStruct).f2;
        }
    }
}
