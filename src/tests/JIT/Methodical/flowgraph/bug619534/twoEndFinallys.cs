// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * The basic scenario is the test should have two returns (endfinallys) in the finally block.
 * We cannot put a return statement in the finally block for C#
 * In ordinary circumstances, like this, the C# compiler generates one finally with a branch to that finally for the first break statement.
 * Hence the IL manually inserts another endfinally within to reproduce this bug.
 * When optimizing away the if(dummy != 0) block, since it contains a return, the JIT looks at the finally block when removing this basic block.
 * The x86 JIT expected exactly one return from the finally block, and this test breaks that assumption.
 *
 * From notes from the dev:
 * When we are trying to delete a sequence of block because they have become unreachable due to an optimization we can hit this assert when we attempt to delete the BBJ_CALL/BBJ_ALWAYS blocks
 * A BBJ_CALL block is a call to a finally region the assert is expecting that the will be exactly one BBJ_RET branch back from the finally block to the BBJ_ALWAYS block.  Typically this is true, however for finally blocks that use multiple return statements (generally a poor practice IMHO)  we can have more than one BBJ_RET back to the BBJ_ALWAYS block.  Thus hitting the assert.  For a retail build we would not assert and would instead just remove one of the back edges to the BBJ_ALWAYS block, which is wrong but does actually cause any further problems in a retail build.
 * The fix is to loop over all of the back edged into the BBJ_ALWAYS block and remove all of them.
 */

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_twoEndFinallys
{
public class Test
{
    private int _temp;
    private static int s_result = 100;

    private Test()
    {
        _temp = 101;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Test t = TwoEndFinallys(new string[] {});
        if (t._temp == 101)
            return 100;
        else return 101;
    }
    private static Test TwoEndFinallys(string[] args)
    {
        Test t = null;
        Test u = null;
        try
        {
            int dummy = 0;
            Test l = null;
            if (s_result != 1000)
            {
                if (dummy != 0)
                {
                    t = new Test();
                    t._temp = l._temp;
                    return t;
                }
                try
                {
                    return new Test();
                }
                catch (Exception obj1)
                {
                    System.Exception exn = (System.Exception)obj1;
                    return null;
                }
            }
        }
        finally
        {
            switch (args.Length)
            {
                case 0:
                    u = dummyCall(0);
                    if (u != null)
                        u._temp += 100;
                    break;
                default:
                    t = dummyCall(Convert.ToInt32(args[0]));
                    t._temp += 101;
                    break;
            }
        }
        return u;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Test dummyCall(int input)
    {
        try
        {
            if (input != 0)
            {
                return new Test();
            }
            return null;
        }
        finally
        {
        }
    }
}

}
