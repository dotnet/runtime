// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

enum TestEnum { }

struct AA
{
    static short m_shStatic1;
    static TestEnum[] Static2(String[] args)
    {
        return new TestEnum[(long)(m_shStatic1 * 11u - m_shStatic1 * 11u)];
    }
    static int Main() { Static2(null); return 100; }
}
