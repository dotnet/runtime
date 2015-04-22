// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Getter and Setter, simple, both should be inlined.
using System;
public class A
{
    private int m_prop;
    public int prop
    {
        get { return m_prop; }
        set { m_prop = value; }
    }
}
class Property
{
    public static int Main()
    {
        A a = new A();
        a.prop = 100;
        int retval = a.prop;
        return retval;
    }
}
