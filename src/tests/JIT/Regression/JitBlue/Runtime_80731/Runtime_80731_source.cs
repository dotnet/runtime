using InlineIL;
using System;
using System.Runtime.CompilerServices;

// Tests that we properly account for local uses of GT_JMP nodes when omitting
// copies for implicit byrefs and when forward substituting.
class Runtime_80731
{
    static int Main()
    {
        int code = 100;

        int implicitByrefResult = ImplicitByref(new S16 { A = 5678 });
        if (implicitByrefResult != 5678)
        {
            Console.WriteLine("FAIL: ImplicitByref returned {0}", implicitByrefResult);
            code |= 1;
        }

        int forwardSubResult = ForwardSub(1234);
        if (forwardSubResult != 5678)
        {
            Console.WriteLine("FAIL: ForwardSub returned {0}", forwardSubResult);
            code |= 2;
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ImplicitByref(S16 s)
    {
        Modify(s);
        IL.Emit.Jmp(new MethodRef(typeof(Runtime_80731), nameof(ImplicitByrefCallee)));
        throw IL.Unreachable();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Modify(S16 s)
    {
        s.A = 1234;
        Consume(s);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T val)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ImplicitByrefCallee(S16 s)
    {
        return s.A;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ForwardSub(int a)
    {
        a = 5678;
        Consume(a);
        IL.Emit.Jmp(new MethodRef(typeof(Runtime_80731), nameof(ForwardSubCallee)));
        throw IL.Unreachable();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ForwardSubCallee(int a)
    {
        return a;
    }

    private struct S16
    {
        public int A, B, C, D;
    }
}
