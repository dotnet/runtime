// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace GCTest
{
    internal class Test
    {
        private int _indx;
        public bool m_die = false;
        private static Test[] s_arr = new Test[50];

        public Test(int indx) { _indx = indx; }

        public virtual void CheckValid()
        {
            if (s_arr[_indx] != this)
                throw new Exception();
        }

        ~Test()
        {
            if (!m_die)
            {
                if (s_arr[_indx] != null)
                {
                    throw new Exception("arr[" + _indx.ToString() + "] != null");
                }
                s_arr[_indx] = this;
                GC.ReRegisterForFinalize(this);
            }
        }

        private static int Main()
        {
            Test1();
            Test2();
            Test3();
            Test4();
            Test5();
            Test6();
            Console.WriteLine("Test passed.");
            return 100;
        }
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void Test1()
        {
            for (int i = 0; i < 50; i++)
                s_arr[i] = new Test(i);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void Test2()
        {
            for (int i = 0; i < 50; i++)
            {
                if (s_arr[i] == null) throw new Exception();
                s_arr[i].CheckValid();
                s_arr[i] = null;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void Test3()
        {
            for (int i = 0; i < 50; i++)
            {
                if (s_arr[i] == null) throw new Exception();
                s_arr[i].CheckValid();
                s_arr[i] = null;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void Test4()
        {
            for (int i = 0; i < 50; i++)
            {
                if (s_arr[i] == null) throw new Exception();
                s_arr[i].CheckValid();
                s_arr[i] = null;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void Test5()
        {
            for (int i = 0; i < 50; i++)
            {
                if (s_arr[i] == null) throw new Exception();
                s_arr[i].CheckValid();
                s_arr[i] = null;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void Test6()
        {
            for (int i = 0; i < 50; i++)
            {
                if (s_arr[i] == null) throw new Exception();
                s_arr[i].CheckValid();
                s_arr[i].m_die = true;
                s_arr[i] = null;
            }
        }
    }
}
