// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestUnhandledException
{
    public delegate void MyCallback();

    unsafe class Program
    {
        [DllImport("foreignunhandled")]
        public static extern void InvokeCallbackOnNewThread(delegate*unmanaged<void> callBack);

        private const string INTERNAL_CALL = "__internal";

        [SuppressGCTransition]
        [DllImport(INTERNAL_CALL, EntryPoint = "HelloCpp")]
        private static extern void Test();

        [UnmanagedCallersOnly]
        static void ThrowException()
        {
            SetDllResolver();
            Test();
        }

        private static void SetDllResolver()
        {
            NativeLibrary.SetDllImportResolver(
                Assembly.GetExecutingAssembly(),
                static (library, _, _) =>
                    library == INTERNAL_CALL ? NativeLibrary.GetMainProgramHandle() : IntPtr.Zero
            );
        }

        static void Main(string[] args)
        {
            if (args[0] == "main")
            {
                throw new Exception("Test");
            }
            else if (args[0] == "foreign")
            {
                InvokeCallbackOnNewThread(&ThrowException);
            }
        }
    }
}
