// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace MS_jumper_cs
{
    public struct VT
    {
        private int _m_n;
        private VT[] _m_dummyGCRef;
        private VT(int n) { _m_n = n; _m_dummyGCRef = new VT[10]; }

        private VT add(VT what) { _m_n += what._m_n; return this; }
        private VT sub(VT what) { _m_n -= what._m_n; return this; }   //this will be implemented via NEG+JMP in IL

        [Fact]
        public static int TestEntryPoint()
        {
            VT vt = new VT(100);
            VT what = new VT(99);
            vt = vt.add(what);
            vt = vt.sub(what);
            if (vt._m_n != 100)
                return vt._m_n;
            VT what2 = new VT(96);
            vt.add(what2);
            vt.sub(what2);
            return vt._m_n;
        }
    }
}
