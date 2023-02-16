// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Reflection.Emit.DerivedTypes.cs
//
// Authors:
//  Rodrigo Kumpera <rkumpera@novell.com>
//
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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

using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial class SymbolType
    {
        // element_type, type_kind and rank fields defined in shared SymbolType should kept in sync with MonoReflectionSymbolType in object-internals.h

        internal override Type InternalResolve()
        {
            switch (type_kind)
            {
                case TypeKind.IsArray:
                    {
                        Type et = element_type.InternalResolve();
                        if (rank == 1)
                            return et.MakeArrayType();
                        return et.MakeArrayType(rank);
                    }
                case TypeKind.IsByRef: return element_type.InternalResolve().MakeByRefType();
                case TypeKind.IsPointer: return element_type.InternalResolve().MakePointerType();
            }

            throw new NotSupportedException();
        }

        // Called from the runtime to return the corresponding finished Type object
        internal override Type RuntimeResolve()
        {
            if (type_kind == TypeKind.IsArray)
            {
                Type et = element_type.RuntimeResolve();
                if (rank == 1)
                {
                    return et.MakeArrayType();
                }

                return et.MakeArrayType(rank);
            }

            return InternalResolve();
        }
    }
}
