// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
class AA
{
    static void f(ref Array param)
    {
        try
        {

        }
        finally
        {
            for (int i = 0; i < 3; i++)
            {
            }
#pragma warning disable 1718
            while ((param != param))
#pragma warning restore 1718
            {
            }
        }
    }

    static int Main()
    {
        f(ref m_arr);
        Console.WriteLine("Passed.");
        return 100;
    }

    static Array m_arr;

}
