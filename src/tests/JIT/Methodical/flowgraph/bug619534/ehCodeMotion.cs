// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Actually it was a case of the JIT incorrectly move the 'ret=true' down to a point where logically it was reachable via the artificial edges we add to simulate the EH flow, but in reality it was totally unreachable.
 * The fix is to recognize such situations and not place code there.
 *
 * Notes from the bug:
 * Compile the following with no additional command line args.
 * If you run the resulting assembly on the x86 runtime, it will correctly print "Main: True".
 * However, if you run it on the amd64 runtime, then it prints "Main: False".
 * However, if you put something else in the catch block (such as the commented out line) then it works again.
 * It seems like the 64bit runtime is doing something wrong here.
 */


using System;
using Xunit;

namespace Test_ehCodeMotion_cs
{
public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        //Console.WriteLine("Main: " + new C().M());
        if (new C().M()) return 100; else return 101;
    }
}

internal class C
{
    private object _o;

    public bool M()
    {
        bool ret = true;

        try
        {
            _o.ToString();
        }
        catch (NullReferenceException)
        {
            //Console.WriteLine(ret); // Uncommenting this line "fixes" the problem
            goto Label;
        }
        ret = false;

    Label:

        return ret;
    }
}
}
