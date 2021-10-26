// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Threading.Tasks;

namespace SimpleConsole
{
    public class Test
    {
        public static int Main(string[] args)
        {
            Console.WriteLine ($"from pinvoke: {SimpleConsole.Test.print_line(100)}");
            return 0;
        }

        class Helper {
            public Helper () {
                int t = 0;
                unsafe {
                    delegate *unmanaged<int,int> fn = &nested_helper;
                    t += native_intint_callback_acceptor ((IntPtr)fn, 1);

                    fn = &member_helper;

                    t += native_intint_callback_acceptor ((IntPtr)fn, 2);
                }

                Console.WriteLine ("total in helper: {t}");

                // local function inside a nested class ctor. Mangled name will be something like
                //   int32 SimpleConsole.Test/Helper::'<.ctor>g__Helper|1_0'
                [UnmanagedCallersOnly]
                static int nested_helper (int j) => j + 20;

            }
        }

        [UnmanagedCallersOnly]
        private static int member_helper(int x) => x + 30;


        [DllImport("native-lib")]
        public static extern int print_line(int x);

        // FIXME: support function pointers in pinvoke arguments
        [DllImport("native-lib")]
        public static unsafe extern int native_intint_callback_acceptor(/*delegate *unmanaged<int,int>*/ IntPtr fn, int i);
    }
}
