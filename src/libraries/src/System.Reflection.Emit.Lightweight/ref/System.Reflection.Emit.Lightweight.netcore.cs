// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection.Emit
{
    public sealed partial class DynamicILInfo
    {
        internal DynamicILInfo() { }
        public System.Reflection.Emit.DynamicMethod DynamicMethod { get { throw null; } }
        public int GetTokenFor(byte[] signature) { throw null; }
        public int GetTokenFor(System.Reflection.Emit.DynamicMethod method) { throw null; }
        public int GetTokenFor(System.RuntimeFieldHandle field) { throw null; }
        public int GetTokenFor(System.RuntimeFieldHandle field, System.RuntimeTypeHandle contextType) { throw null; }
        public int GetTokenFor(System.RuntimeMethodHandle method) { throw null; }
        public int GetTokenFor(System.RuntimeMethodHandle method, System.RuntimeTypeHandle contextType) { throw null; }
        public int GetTokenFor(System.RuntimeTypeHandle type) { throw null; }
        public int GetTokenFor(string literal) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe void SetCode(byte* code, int codeSize, int maxStackSize) { }
        public void SetCode(byte[] code, int maxStackSize) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe void SetExceptions(byte* exceptions, int exceptionsSize) { }
        public void SetExceptions(byte[] exceptions) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe void SetLocalSignature(byte* localSignature, int signatureSize) { }
        public void SetLocalSignature(byte[] localSignature) { }
    }
    public sealed partial class DynamicMethod : System.Reflection.MethodInfo
    {
        public System.Reflection.Emit.DynamicILInfo GetDynamicILInfo() { throw null; }
    }
}
