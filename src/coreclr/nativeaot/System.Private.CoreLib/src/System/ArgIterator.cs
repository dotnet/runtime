// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System
{
    [StructLayout(LayoutKind.Sequential)]
    public ref struct ArgIterator
    {
        public ArgIterator(RuntimeArgumentHandle arglist)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ArgIterator);
        }

        [CLSCompliant(false)]
        public unsafe ArgIterator(RuntimeArgumentHandle arglist, void* ptr)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ArgIterator);
        }

        public void End()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ArgIterator);
        }

        public override bool Equals(object? o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ArgIterator);
        }

        public override int GetHashCode()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ArgIterator);
        }

        [CLSCompliant(false)]
        public System.TypedReference GetNextArg()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ArgIterator);
        }

        [CLSCompliant(false)]
        public System.TypedReference GetNextArg(System.RuntimeTypeHandle rth)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ArgIterator);
        }

        public unsafe System.RuntimeTypeHandle GetNextArgType()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ArgIterator);
        }

        public int GetRemainingCount()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ArgIterator);
        }
    }
}
