// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace JitTest
{
    internal struct Struct1
    {
        private int _m_i1;
        private long _m_l1;
        public struct Struct2
        {
            private int _m_i2;
            private long _m_l2;
            public void Verify()
            {
                if (_m_i2 != 0 || _m_l2 != 0) throw new Exception();
            }
        }
        public Struct2 m_str2;
        public void Verify()
        {
            if (_m_i1 != 0 || _m_l1 != 0) throw new Exception();
            m_str2.Verify();
        }
    }

    internal class Test
    {
        private static int Main()
        {
            Struct1 str1 = new Struct1();
            TypedReference _ref = __makeref(str1);
            str1 = __refvalue(_ref, Struct1);
            str1.Verify();
            _ref = __makeref(str1.m_str2);
            Struct1.Struct2 str2 = __refvalue(_ref, Struct1.Struct2);
            str2.Verify();
            return 100;
        }
    }
}
