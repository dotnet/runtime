// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Note: This test file is the source of the Runtime_70259.il file. It requires
// InlineIL.Fody to compile. It is not used as anything but a reference of that
// IL file.

using InlineIL;
using System;
using System.Runtime.CompilerServices;

class Runtime_70259
{
    private static int Main()
    {
        // This creates an open delegate that goes through shuffle thunk and
        // then VSD stub. On arm32 it also goes through wrapper delegate.
        Func<IFace, int> del = typeof(IFace).GetMethod("Method").CreateDelegate<Func<IFace, int>>();

        // We need a normal call here to get a call site of the form `call
        // [rel32]` which triggers the bug.
        return CallMe(del);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallMe(Func<IFace, int> del)
    {
        IL.Push(del);
        IL.Push(new C());
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Func<IFace, int>), "Invoke"));
        return IL.Return<int>();
    }

    interface IFace
    {
        int Method();
    }

    class C : IFace
    {
        public virtual int Method()
        {
            return 100;
        }
    }
}

