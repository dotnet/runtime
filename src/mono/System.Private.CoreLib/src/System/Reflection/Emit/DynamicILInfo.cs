// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Reflection.Emit/DynamicILInfo.cs
//
// Author:
//   Zoltan Varga (vargaz@gmail.com)
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#if MONO_FEATURE_SRE
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{

    [ComVisible(true)]
    public class DynamicILInfo
    {

        private DynamicMethod method = null!;

        internal DynamicILInfo()
        {
        }

        internal DynamicILInfo(DynamicMethod method)
        {
            this.method = method;
        }

        public DynamicMethod DynamicMethod
        {
            get
            {
                return method;
            }
        }

        // FIXME:
        public int GetTokenFor(byte[] signature)
        {
            throw new NotImplementedException();
        }

        public int GetTokenFor(DynamicMethod method)
        {
            return this.method.GetILGenerator().TokenGenerator.GetToken(method, false);
        }

        public int GetTokenFor(RuntimeFieldHandle field)
        {
            return this.method.GetILGenerator().TokenGenerator.GetToken(FieldInfo.GetFieldFromHandle(field), false);
        }

        public int GetTokenFor(RuntimeMethodHandle method)
        {
            MethodBase mi = MethodBase.GetMethodFromHandle(method)!;
            return this.method.GetILGenerator().TokenGenerator.GetToken(mi, false);
        }

        public int GetTokenFor(RuntimeTypeHandle type)
        {
            Type t = Type.GetTypeFromHandle(type)!;
            return this.method.GetILGenerator().TokenGenerator.GetToken(t, false);
        }

        public int GetTokenFor(string literal)
        {
            return method.GetILGenerator().TokenGenerator.GetToken(literal);
        }

        // FIXME:
        public int GetTokenFor(RuntimeMethodHandle method, RuntimeTypeHandle contextType)
        {
            throw new NotImplementedException();
        }

        // FIXME:
        public int GetTokenFor(RuntimeFieldHandle field, RuntimeTypeHandle contextType)
        {
            throw new NotImplementedException();
        }

        public void SetCode(byte[]? code, int maxStackSize)
        {
            method.GetILGenerator().SetCode(code, maxStackSize);
        }

        [CLSCompliantAttribute(false)]
        public unsafe void SetCode(byte* code, int codeSize, int maxStackSize)
        {
            if (codeSize < 0)
                throw new ArgumentOutOfRangeException(nameof(codeSize), SR.ArgumentOutOfRange_GenericPositive);
            if (codeSize > 0 && code == null)
                throw new ArgumentNullException(nameof(code));

            method.GetILGenerator().SetCode(code, codeSize, maxStackSize);
        }

        // FIXME:
        public void SetExceptions(byte[]? exceptions)
        {
            throw new NotImplementedException();
        }

        // FIXME:
        [CLSCompliantAttribute(false)]
        public unsafe void SetExceptions(byte* exceptions, int exceptionsSize)
        {
            throw new NotImplementedException();
        }

        // FIXME:
        public void SetLocalSignature(byte[]? localSignature)
        {
            throw new NotImplementedException();
        }

        [CLSCompliantAttribute(false)]
        public unsafe void SetLocalSignature(byte* localSignature, int signatureSize)
        {
            byte[] b = new byte[signatureSize];
            for (int i = 0; i < signatureSize; ++i)
                b[i] = localSignature[i];
        }
    }
}

#endif
