// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class CC
{
    public static bool Method2() { return true; }
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Main1();
            return 101;
        }
        catch (IndexOutOfRangeException)
        {
            return 100;
        }
    }
    internal static void Main1()
    {
        bool a = false;
        try
        {
            while (Method2())
                return;
            do
            {
                while (a) { }
            } while (a);
        }
        finally
        {
            try
            {
                throw new IndexOutOfRangeException();
            }
            catch (NullReferenceException)
            {
                do { } while (Method2());
            }
        }
    }
}
