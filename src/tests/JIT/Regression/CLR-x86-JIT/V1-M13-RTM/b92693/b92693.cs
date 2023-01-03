// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

enum TestEnum { }

public struct AA
{
    static short m_shStatic1;
    static TestEnum[] Static2(String[] args)
    {
        return new TestEnum[(long)(m_shStatic1 * 11u - m_shStatic1 * 11u)];
    }
    public static int Main() { Static2(null); return 100; }
}
