// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public class DynamicILInfo
    {
        internal DynamicILInfo()
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
        }

        public DynamicMethod DynamicMethod { get { return default; } }

        public void SetCode(byte[] code, int maxStackSize) { }

        [CLSCompliant(false)]
        public unsafe void SetCode(byte* code, int codeSize, int maxStackSize) { }

        public void SetExceptions(byte[] exceptions) { }

        [CLSCompliant(false)]
        public unsafe void SetExceptions(byte* exceptions, int exceptionsSize) { }

        public void SetLocalSignature(byte[] localSignature) { }

        [CLSCompliant(false)]
        public unsafe void SetLocalSignature(byte* localSignature, int signatureSize) { }

        public int GetTokenFor(RuntimeMethodHandle method)
        {
            return default;
        }
        public int GetTokenFor(DynamicMethod method)
        {
            return default;
        }
        public int GetTokenFor(RuntimeMethodHandle method, RuntimeTypeHandle contextType)
        {
            return default;
        }
        public int GetTokenFor(RuntimeFieldHandle field)
        {
            return default;
        }
        public int GetTokenFor(RuntimeFieldHandle field, RuntimeTypeHandle contextType)
        {
            return default;
        }
        public int GetTokenFor(RuntimeTypeHandle type)
        {
            return default;
        }
        public int GetTokenFor(string literal)
        {
            return default;
        }
        public int GetTokenFor(byte[] signature)
        {
            return default;
        }
    }
}
