// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_call_cs
{
    internal interface ISomething
    {
        void DoSomething(VT vt1, ref VT vt2);
        VT RetSomething(VT vt);
    }

    public struct VT : ISomething
    {
        private int _m_vn;
        private String _m_vs;

        private VT(int vn, String vs)
        {
            _m_vn = vn;
            _m_vs = vs;
        }

        public void DoSomething(VT vt1, ref VT vt2)
        {
            Console.WriteLine("ISomething::DoSomething - " + vt1.ToString() + " " + vt2.ToString());
            Console.WriteLine("this is {" + ToString() + "}");
        }

        public VT RetSomething(VT vt)
        {
            Console.WriteLine("ISomething::RetSomething - " + vt.ToString());
            Console.WriteLine("this is {" + ToString() + "}");
            return new VT(vt._m_vn, vt._m_vs);
        }

        public void _DoSomething(VT vt1, ref VT vt2)
        {
            Console.WriteLine("ISomething::DoSomething - " + vt1.ToString() + " " + vt2.ToString());
            Console.WriteLine("this is {" + ToString() + "}");
        }

        public VT _RetSomething(VT vt)
        {
            Console.WriteLine("ISomething::RetSomething - " + vt.ToString());
            Console.WriteLine("this is {" + ToString() + "}");
            return new VT(vt._m_vn, vt._m_vs);
        }

        public override String ToString()
        {
            return _m_vn.ToString() + "/" + _m_vs;
        }

        private static void TryInterface(ISomething smth)
        {
            VT vt = new VT(11, "11");
            smth.DoSomething(new VT(0, "-"), ref vt);
            vt = smth.RetSomething(vt);
            Console.WriteLine("Got : " + vt.ToString());
        }

        [Fact]
        public static int TestEntryPoint()
        {
            VT vt = new VT(10, "10");
            vt._DoSomething(new VT(0, "-"), ref vt);
            vt = vt._RetSomething(vt);
            Console.WriteLine("Got : " + vt.ToString());
            if (vt._m_vn != 10 || vt._m_vs != "10")
            {
                Console.WriteLine("Failed");
                return 101;
            }

            vt.DoSomething(new VT(0, "-"), ref vt);
            vt = vt.RetSomething(vt);
            Console.WriteLine("Got : " + vt.ToString());
            if (vt._m_vn != 10 || vt._m_vs != "10")
            {
                Console.WriteLine("Failed");
                return 101;
            }

            TryInterface(vt);

            Console.WriteLine("Passed");
            return 100;
        }
    }
}
