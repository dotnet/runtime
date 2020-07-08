// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public class CB
{
    public Int32 I;
    public CB_A C;
    public CB(int i)
    {
        I = i;
        C = new CB_A(i);

    }
    public class CB_A
    {
        public Int32 I;
        public CB_A(int i)
        {
            I = i;
        }
    }
}
public struct VT_E
{
    public int I;
    public CB C;
    public VT_E(int i)
    {
        I = i;
        C = new CB(i);
    }
}


