// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace SharedLibrary
{
    public class ClassLibrary
    {
        static Thread s_setterThread;
        static int s_primitiveInt;

        [ModuleInitializer]
        public static void CreateThreadInModuleInitializer()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/107699
            // where creating threads in module initializer would lead to a deadlock.
            s_setterThread = new Thread(() => { s_primitiveInt = 10; });
            s_setterThread.Start();
        }

        [UnmanagedCallersOnly(EntryPoint = "ReturnsPrimitiveInt", CallConvs = [typeof(CallConvStdcall)])]
        public static int ReturnsPrimitiveInt()
        {
            s_setterThread.Join();
            return s_primitiveInt;
        }

        [UnmanagedCallersOnly(EntryPoint = "ReturnsPrimitiveBool", CallConvs = [typeof(CallConvStdcall)])]
        public static bool ReturnsPrimitiveBool()
        {
            return true;
        }

        [UnmanagedCallersOnly(EntryPoint = "ReturnsPrimitiveChar", CallConvs = [typeof(CallConvStdcall)])]
        public static char ReturnsPrimitiveChar()
        {
            return 'a';
        }

        [UnmanagedCallersOnly(EntryPoint = "EnsureManagedClassLoaders", CallConvs = [typeof(CallConvStdcall)])]
        public static void EnsureManagedClassLoaders()
        {
            Random random = new Random();
            random.Next();
        }

        [UnmanagedCallersOnly(EntryPoint = "CheckSimpleExceptionHandling", CallConvs = [typeof(CallConvStdcall)])]
        public static int CheckSimpleExceptionHandling()
        {
            int result = 10;

            try
            {
                Console.WriteLine("Throwing exception");
                throw new Exception();
            }
            catch when (result == 10)
            {
                result += 20;
            }
            finally
            {
                result += 70;
            }

            return result;
        }

        private static bool s_collected;

        class ClassWithFinalizer
        {
            ~ClassWithFinalizer() { s_collected = true; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void MakeGarbage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            object[] arr = new object[1024 * 1024];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = new object();

            new ClassWithFinalizer();
        }

        [UnmanagedCallersOnly(EntryPoint = "CheckSimpleGCCollect", CallConvs = [typeof(CallConvStdcall)])]
        public static int CheckSimpleGCCollect()
        {
            string myString = string.Format("Hello {0}", "world");

            MakeGarbage();

            Console.WriteLine("Triggering GC");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return s_collected ? (myString == "Hello world" ? 100 : 1) : 2;
        }
    }
}
