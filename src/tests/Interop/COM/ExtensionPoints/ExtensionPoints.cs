// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using COM;

using TestLibrary;
using Xunit;

public class ExtensionPoints
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static unsafe void ConcurrentVtablePublication(bool classLayout)
    {
        const int MethodCount = 16;
        const int WorkerCount = 16;
        Guid unsupportedIID = new Guid("9970D486-61A2-4BF9-8395-0C6D8BFA3D2E");

        for (int iteration = 0; iteration < 8; iteration++)
        {
            // Fresh types keep both shared vtables cold on every iteration.
            ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName($"ConcurrentCCW{iteration}"), AssemblyBuilderAccess.Run).DefineDynamicModule("Main");
            var visible = new CustomAttributeBuilder(
                typeof(ComVisibleAttribute).GetConstructor(new[] { typeof(bool) }), new object[] { true });
            TypeBuilder interfaceBuilder = module.DefineType("ITest",
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            interfaceBuilder.SetCustomAttribute(visible);
            interfaceBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(InterfaceTypeAttribute).GetConstructor(new[] { typeof(ComInterfaceType) }),
                new object[] { ComInterfaceType.InterfaceIsIUnknown }));
            for (int slot = 0; slot < MethodCount; slot++)
            {
                interfaceBuilder.DefineMethod($"Method{slot}",
                    MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual,
                    typeof(int), Type.EmptyTypes);
            }
            Type interfaceType = interfaceBuilder.CreateType();

            TypeBuilder classBuilder = module.DefineType("Test", TypeAttributes.Public);
            classBuilder.SetCustomAttribute(visible);
            classBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(ClassInterfaceAttribute).GetConstructor(new[] { typeof(ClassInterfaceType) }),
                new object[] { ClassInterfaceType.AutoDual }));
            classBuilder.AddInterfaceImplementation(interfaceType);
            for (int slot = 0; slot < MethodCount; slot++)
            {
                MethodBuilder method = classBuilder.DefineMethod($"Method{slot}",
                    MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), Type.EmptyTypes);
                ILGenerator il = method.GetILGenerator();
                il.Emit(OpCodes.Ldc_I4, slot);
                il.Emit(OpCodes.Ret);
            }
            Type classType = classBuilder.CreateType();

            var instances = new object[WorkerCount];
            var interfaces = new nint[WorkerCount];
            var workers = new Task[WorkerCount];
            using var start = new Barrier(WorkerCount);
            try
            {
                for (int worker = 0; worker < WorkerCount; worker++)
                {
                    instances[worker] = Activator.CreateInstance(classType);
                    if (classLayout)
                    {
                        // An explicit interface does not lay out the class interface.
                        interfaces[worker] = Marshal.GetComInterfaceForObject(instances[worker], interfaceType);
                    }
                }

                for (int worker = 0; worker < WorkerCount; worker++)
                {
                    int index = worker;
                    workers[worker] = Task.Factory.StartNew(() =>
                    {
                        nint pointer = 0;
                        try
                        {
                            start.SignalAndWait();
                            Guid iid = unsupportedIID;
                            if (classLayout && (index & 1) != 0)
                            {
                                // Failed class-IID lookup generates the lazy IID without doing class layout.
                                Assert.Equal(unchecked((int)0x80004002),
                                    Marshal.QueryInterface(interfaces[index], in iid, out nint result));
                                Assert.Equal((nint)0, result);
                            }
                            pointer = classLayout
                                ? Marshal.GetIUnknownForObject(instances[index])
                                : Marshal.GetComInterfaceForObject(instances[index], interfaceType);

                            iid = new Guid("00000000-0000-0000-C000-000000000046");
                            Assert.Equal(0, Marshal.QueryInterface(pointer, in iid, out nint unknown));
                            Marshal.Release(unknown);

                            nint methodInterface = classLayout ? interfaces[index] : pointer;
                            nint* vtable = *(nint**)methodInterface;
                            for (int slot = 0; slot < MethodCount; slot++)
                            {
                                int value = -1;
                                var invoke = (delegate* unmanaged[Stdcall]<nint, int*, int>)vtable[3 + slot];
                                Assert.Equal(0, invoke(methodInterface, &value));
                                Assert.Equal(slot, value);
                            }
                        }
                        finally
                        {
                            if (pointer != 0)
                                Marshal.Release(pointer);
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
                Task.WaitAll(workers);
            }
            finally
            {
                foreach (nint pointer in interfaces)
                {
                    if (pointer != 0)
                        Marshal.Release(pointer);
                }
                GC.KeepAlive(instances);
            }
        }
    }

    unsafe class MallocSpy : IMallocSpy
    {
        private int _called = 0;
        public int Called => _called;

        public virtual nuint PreAlloc(nuint cbRequest)
        {
            _called++;
            return cbRequest;
        }

        public virtual unsafe void* PostAlloc(void* pActual) => pActual;
        public virtual unsafe void* PreFree(void* pRequest, [MarshalAs(UnmanagedType.Bool)] bool fSpyed)
        {
            _called++;
            return pRequest;
        }

        public virtual void PostFree([MarshalAs(UnmanagedType.Bool)] bool fSpyed) { }
        public virtual unsafe nuint PreRealloc(void* pRequest, nuint cbRequest, void** ppNewRequest, [MarshalAs(UnmanagedType.Bool)] bool fSpyed) => cbRequest;
        public virtual unsafe void* PostRealloc(void* pActual, [MarshalAs(UnmanagedType.Bool)] bool fSpyed) => pActual;
        public virtual unsafe void* PreGetSize(void* pRequest, [MarshalAs(UnmanagedType.Bool)] bool fSpyed) => pRequest;
        public virtual nuint PostGetSize(nuint cbActual, [MarshalAs(UnmanagedType.Bool)] bool fSpyed) => cbActual;
        public virtual unsafe void* PreDidAlloc(void* pRequest, [MarshalAs(UnmanagedType.Bool)] bool fSpyed) => pRequest;
        public virtual unsafe int PostDidAlloc(void* pRequest, [MarshalAs(UnmanagedType.Bool)] bool fSpyed, int fActual) => fActual;
        public virtual void PreHeapMinimize() { }
        public virtual void PostHeapMinimize() { }
    }

    [Fact]
    [Xunit.SkipOnCoreClrAttribute("Depends on marshalled calli", RuntimeTestModes.InterpreterActive)]
    public static unsafe void Validate_Managed_IMallocSpy()
    {
        Console.WriteLine($"Running {nameof(Validate_Managed_IMallocSpy)}...");
        var mallocSpy = new MallocSpy();
        int result = Ole32.CoRegisterMallocSpy(mallocSpy);
        Assert.Equal(0, result);
        try
        {
            var arr = new [] { "", "", "", "", null };

            // The goal of this test is to trigger paths in which CoTaskMemAlloc
            // will be implicitly used and validate that the registered managed
            // IMallocSpy can be called successful. The validation is for confirming
            // the transition to Preemptive mode was performed.
            //
            // Casting the function pointer to one in which an IL stub will be
            // used to marshal the string[].
            var fptr = (delegate*unmanaged<string[], int>)(delegate*unmanaged<char**, int>)&ArrayLen;
            int len = fptr(arr);
            Assert.Equal(arr.Length - 1, len);

            // Allocate 1 for the array, 1 for each non-null element, then double it for Free.
            Assert.Equal((1 + (arr.Length - 1)) * 2, mallocSpy.Called);
        }
        finally
        {
            Ole32.CoRevokeMallocSpy();
        }

        [UnmanagedCallersOnly]
        static int ArrayLen(char** ptr)
        {
            char** begin = ptr;
            while (*ptr != null)
                ptr++;
            return (int)(ptr - begin);
        }
    }
}
