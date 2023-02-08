// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class AA
{
    static AA m_xStatic3;
    static long m_lFwd5;
    void Method1(ref long param1) { }
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Main1();
            return 101;
        }
        catch (NullReferenceException)
        {
            return 100;
        }
    }
    static void Main1()
    {
        long local12 = m_lFwd5;
        m_xStatic3.Method1(ref local12);
        try
        {
            throw new IndexOutOfRangeException();
        }
        catch (NullReferenceException) { }
        try
        {
            throw new NullReferenceException();
        }
        finally
        {
            bool local19 = true;
            while (local19) { }
        }
    }
}
