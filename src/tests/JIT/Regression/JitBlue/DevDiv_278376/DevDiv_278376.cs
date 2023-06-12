using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

// NOTE: the bug for this test was an assertion in RyuJIT/x86 when generating code for a double-returning call that
//       was spilled by the RA and subsequently used. The call in question is the call to `C.GetDouble` in `C.Test`.
//       To ensure that its return value is spilled, `C.GetDouble` is implemented as a P/Invoke method: the return
//       value ends up spilled because there is a call to `TrapReturningThreads` between the call and the use of the
//       return value by the cast. Because the bug is a simple assert, there is no need for the problematic code to
//       actually run, so the implementation of `GetDouble` does not need to actually exist.

public sealed class C
{
    [DllImport("nonexistent.dll")]
    extern static double GetDouble();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void UseDouble(double d)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test(bool condition)
    {
        if (condition)
        {
            UseDouble((double)GetDouble());
        }

        return 100;
    }
    
    [Fact]
    public static int TestEntryPoint()
    {
        return Test(false);
    }
}
