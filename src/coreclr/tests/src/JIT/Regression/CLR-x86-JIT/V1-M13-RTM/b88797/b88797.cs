// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

public class CC
{
    public static bool Method2() { return true; }
    public static int Main()
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
    public static void Main1()
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
