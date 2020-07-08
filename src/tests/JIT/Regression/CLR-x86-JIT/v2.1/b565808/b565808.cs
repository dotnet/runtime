// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.CompilerServices;

public class ContentType
{
    public static int errorCount = 0;
#pragma warning disable 0414
    private static readonly ContentType _instance = new ContentType();
#pragma warning restore 0414
    private static readonly char _semicolonSeparator = ';';

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public ContentType()
    {
        if (_semicolonSeparator != ';') Console.WriteLine("Error {0}", errorCount++);
    }
}


class My
{

    static int Main()
    {

        new ContentType();
        new ContentType();

        if (ContentType.errorCount == 1)
        {
            Console.WriteLine("TEsT SUCCESS");
            return 100;
        }
        else
        {
            Console.WriteLine("TEsT FAILED");
            return 666;
        }

    }

}
