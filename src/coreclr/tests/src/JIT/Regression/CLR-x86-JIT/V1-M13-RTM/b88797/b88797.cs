// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
