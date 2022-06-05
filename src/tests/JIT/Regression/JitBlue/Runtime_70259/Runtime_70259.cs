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
        // This creates an open delegate that goes through shuffle thunk and then VSD stub.
        C c = new C();
        c.Delegate = typeof(IFace).GetMethod("Method").CreateDelegate<Func<IFace, int, int>>();

        // Do a static call to C.Method to get started -- this is needed to get
        // a call site of the form `call [rel32]` which triggers the bug.
        IL.Push(c);
        IL.Push(100_000);
        IL.Emit.Call(new MethodRef(typeof(C), nameof(C.Method)));
        return IL.Return<int>();
    }

    interface IFace
    {
        int Method(int left);
    }

    class C : IFace
    {
        public Func<IFace, int, int> Delegate;

        [SkipLocalsInit]
        public virtual int Method(int left)
        {
            if (left == 0)
                return 100;

            LargeStruct s;
            Consume(s);

            IL.Push(Delegate);
            IL.Push(this);
            IL.Push(left - 1);
            IL.Emit.Tail();
            IL.Emit.Call(new MethodRef(typeof(Func<IFace, int, int>), "Invoke"));
            return IL.Return<int>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Consume(in LargeStruct s) { }

        private unsafe struct LargeStruct
        {
            public fixed byte Bytes[1024];
        }
    }
}

