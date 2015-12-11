// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


/* Test case for Dev10 bug #640711
 * -----------------------------------------------------------------------
 Expected output:
 A really long string to get us past the limits of Lib1.dll's string blob
System.Exception: Another really long string just because we can!
   at Repro.Caller(Boolean b) in c:\tests\Dev10\640711\app.cs:line 12
   at Repro.Main() in c:\tests\Dev10\640711\app.cs:line 16
 

Actual Output:
a
System.BadImageFormatException: [C:\tests\Dev10\640711\Lib1.dll] Bad string token.
   at Repro.Caller(Boolean b) in c:\tests\Dev10\640711\app.cs:line 10
   at Repro.Main() in c:\tests\Dev10\640711\app.cs:line 16
 * 
 * ----------------------------------------------------------------------
 * The reader should not cache the embedded module handle if it is not clearing the cache when changing scopes. 
 */

using System;
using System.Runtime.CompilerServices;

static class Repro
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Caller(bool b)
    {
        Throws.M(false);
        if (b)
        {
            Console.WriteLine("A really long string to get us past the limits of Lib1.dll's string blob");
            throw new Exception("Another really long string just because we can!");
        }
    }

    static int Main()
    {
        try
        {
            Caller(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return 100;
    }
}
