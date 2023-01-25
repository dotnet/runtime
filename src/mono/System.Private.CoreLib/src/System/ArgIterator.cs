// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    [StructLayout(LayoutKind.Auto)]
    public ref struct ArgIterator
    {
#pragma warning disable 169, 414
        private IntPtr sig;
        private IntPtr args;
        private int next_arg;
        private int num_args;
#pragma warning restore 169, 414

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void Setup(IntPtr argsp, IntPtr start);

        public ArgIterator(RuntimeArgumentHandle arglist)
        {
            sig = IntPtr.Zero;
            args = IntPtr.Zero;
            next_arg = num_args = 0;
            if (arglist.args == IntPtr.Zero)
                throw new PlatformNotSupportedException();
            Setup(arglist.args, IntPtr.Zero);
        }

        [CLSCompliant(false)]
        public unsafe ArgIterator(RuntimeArgumentHandle arglist, void* ptr)
        {
            sig = IntPtr.Zero;
            args = IntPtr.Zero;
            next_arg = num_args = 0;
            if (arglist.args == IntPtr.Zero)
                throw new PlatformNotSupportedException();
            Setup(arglist.args, (IntPtr)ptr);
        }

        public void End()
        {
            next_arg = num_args;
        }

        public override bool Equals(object? o)
        {
            throw new NotSupportedException(SR.NotSupported_NYI);
        }

        public override int GetHashCode()
        {
            return sig.GetHashCode();
        }

        [CLSCompliant(false)]
        public TypedReference GetNextArg()
        {
            if (num_args == next_arg)
                throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
            TypedReference result = default;
            unsafe
            {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('TypedReference')
                IntGetNextArg(&result);
#pragma warning restore CS8500
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern unsafe void IntGetNextArg(void* res);

        [CLSCompliant(false)]
        public TypedReference GetNextArg(RuntimeTypeHandle rth)
        {
            if (num_args == next_arg)
                throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
            TypedReference result = default;
            unsafe
            {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('TypedReference')
                IntGetNextArgWithType(&result, rth.Value);
#pragma warning restore CS8500
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern unsafe void IntGetNextArgWithType(void* res, IntPtr rth);

        public RuntimeTypeHandle GetNextArgType()
        {
            if (num_args == next_arg)
                throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
            return new RuntimeTypeHandle(IntGetNextArgType());
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern IntPtr IntGetNextArgType();

        public int GetRemainingCount()
        {
            return num_args - next_arg;
        }
    }
}
