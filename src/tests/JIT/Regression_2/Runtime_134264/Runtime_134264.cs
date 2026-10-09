// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for a WebAssembly R2R (crossgen) codegen bug in finally
// calls. When a callfinally's continuation was the lexically next block,
// codegen fell through to it without a branch. If a try_table ended between
// the two, the fall-through landed on the validation `unreachable` emitted
// after the try_table `end` and trapped (RuntimeError: unreachable).
//
// The shape needs a user try/finally that ends the async method (so the
// continuation is outside the state machine's try/catch) and a finally too
// large to be cloned onto the normal path.
//
// Reproduces only under crossgen wasm R2R (TargetOS=browser). Passes trivially
// on all other targets.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Runtime_134264
{
    private static int s_value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Touch(int value) => s_value += value;

    private static async Task TryCatchFinallyAsync(bool enter)
    {
        bool entered = false;
        try
        {
            if (enter)
            {
                await Task.CompletedTask;
                entered = true;
            }

            Touch(1);
        }
        catch (Exception ex)
        {
            GC.KeepAlive(ex);
            Touch(-100);
        }
        finally
        {
            if (entered)
            {
                Touch(1); Touch(2); Touch(3); Touch(4); Touch(5); Touch(6); Touch(7); Touch(8);
                Touch(9); Touch(10); Touch(11); Touch(12); Touch(13); Touch(14); Touch(15); Touch(16);
            }

            Touch(1000);
        }
    }

    private static async Task NestedTryFinallyAsync(bool enter)
    {
        bool entered = false;
        try
        {
            try
            {
                if (enter)
                {
                    await Task.CompletedTask;
                    entered = true;
                }

                Touch(1);
            }
            finally
            {
                if (entered)
                {
                    Touch(1); Touch(2); Touch(3); Touch(4); Touch(5); Touch(6); Touch(7); Touch(8);
                    Touch(9); Touch(10); Touch(11); Touch(12); Touch(13); Touch(14); Touch(15); Touch(16);
                }

                Touch(1000);
            }
        }
        catch (Exception ex)
        {
            GC.KeepAlive(ex);
            Touch(-100);
        }
    }

    [Theory]
    [InlineData(true, 1137)]
    [InlineData(false, 1001)]
    public static async Task TestEntryPoint(bool enter, int expected)
    {
        s_value = 0;
        await TryCatchFinallyAsync(enter);
        Assert.Equal(expected, s_value);

        s_value = 0;
        await NestedTryFinallyAsync(enter);
        Assert.Equal(expected, s_value);
    }
}
