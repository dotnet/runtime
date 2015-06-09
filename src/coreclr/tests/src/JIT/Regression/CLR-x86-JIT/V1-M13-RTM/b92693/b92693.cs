// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
