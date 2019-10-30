// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MS
{
    internal struct VT
    {
        private int _m_n;
        private VT[] _m_dummyGCRef;
        private VT(int n) { _m_n = n; _m_dummyGCRef = new VT[10]; }

        private static VT add(VT me, VT what) { me._m_n += what._m_n; return me; }
        private static VT sub(VT me, VT what) { me._m_n -= what._m_n; return me; }

        private static int Main()
        {
            VT vt = new VT(100);
            VT what = new VT(99);
            vt = VT.add(vt, what);
            vt = VT.sub(vt, what);
            return vt._m_n;
        }
    }
}
