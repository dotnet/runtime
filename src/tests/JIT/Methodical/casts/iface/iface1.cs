// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_iface1_cs
{
    internal interface Iface1
    {
        int Method1a();
    }

    internal interface Iface2 : Iface1
    {
        int Method2a();
    }

    internal interface Iface3 : Iface1
    {
        int Method3a();
    }

    public class BaseClass : Iface2
    {
        public int Method1a() { return 1; }
        public int Method2a() { return 10; }
    }

    public class CoClass : BaseClass, Iface3
    {
        public int Method3a() { return 100; }

        private static int Static0(BaseClass co)
        {
            int s = 0;
            s += ((Iface1)co).Method1a();
            s += (co as Iface1).Method1a();
            s += ((Iface2)co).Method2a();
            s += (co as Iface2).Method2a();
            if (co is Iface3)
            {
                s += ((Iface3)co).Method3a();
                s += (co as Iface3).Method3a();
            }
            else
            {
                try
                {
                    return ((Iface3)co).Method3a() +
                        (co as Iface3).Method3a();
                }
                catch { s += 1000; }
            }
            return s;
        }

        private static int Static1(Iface1 i)
        {
            return
                Static0((CoClass)i) +
                Static0(i as CoClass)
                ;
        }

        private static int Static2(Iface2 i)
        {
            return
                Static0((CoClass)i) +
                Static0(i as CoClass)
                ;
        }

        private static int Static3(Iface3 i)
        {
            return
                Static0((CoClass)i) +
                Static0(i as CoClass)
                ;
        }

        private static int Static4(Iface1 i)
        {
            return
                Static0((BaseClass)i) +
                Static0(i as BaseClass)
                ;
        }

        private static int Static5(Iface2 i)
        {
            return
                Static0((BaseClass)i) +
                Static0(i as BaseClass)
                ;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            CoClass co = new CoClass();
            if (Static1(co) != 444)
            {
                Console.WriteLine("Test 1 failed.");
                return 101;
            }
            if (Static2(co) != 444)
            {
                Console.WriteLine("Test 2 failed.");
                return 102;
            }
            if (Static3(co) != 444)
            {
                Console.WriteLine("Test 3 failed.");
                return 103;
            }
            BaseClass bs = new BaseClass();
            if (Static4(bs) != 2044)
            {
                Console.WriteLine("Test 4 failed.");
                return 104;
            }
            if (Static5(bs) != 2044)
            {
                Console.WriteLine("Test 5 failed.");
                return 105;
            }
            Console.WriteLine("Passed");
            return 100;
        }
    }
}
