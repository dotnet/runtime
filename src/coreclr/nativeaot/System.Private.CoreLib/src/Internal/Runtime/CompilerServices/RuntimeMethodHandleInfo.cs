// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

using Internal.Runtime.Augments;

namespace Internal.Runtime.CompilerServices
{
    public class MethodNameAndSignature
    {
        public string Name { get; private set; }
        public RuntimeSignature Signature { get; private set; }

        public MethodNameAndSignature(string name, RuntimeSignature signature)
        {
            Name = name;
            Signature = signature;
        }

        public override bool Equals(object? compare)
        {
            if (compare == null)
                return false;

            MethodNameAndSignature? other = compare as MethodNameAndSignature;
            if (other == null)
                return false;

            if (Name != other.Name)
                return false;

            return Signature.Equals(other.Signature);
        }

        public override int GetHashCode()
        {
            int hash = Name.GetHashCode();

            return hash;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public unsafe struct RuntimeMethodHandleInfo
    {
        public IntPtr NativeLayoutInfoSignature;
    }
}
