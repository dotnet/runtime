// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Reflection.Emit.TypeBuilderInstantiation
//
// Sean MacIsaac (macisaac@ximian.com)
// Paolo Molaro (lupus@ximian.com)
// Patrik Torstensson (patrik.torstensson@labs2.com)
//
// (C) 2001 Ximian, Inc.
//

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial class TypeBuilderInstantiation
    {
        // generic_type and type_arguments fields defined in shared TypeBuilderInstantiation should kept in sync with MonoReflectionGenericClass in object-internals.h

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "Reflection.Emit is not subject to trimming")]
        internal override Type InternalResolve()
        {
            Type gtd = generic_type.InternalResolve();
            Type[] args = new Type[type_arguments.Length];
            for (int i = 0; i < type_arguments.Length; ++i)
                args[i] = type_arguments[i].InternalResolve();
            return gtd.MakeGenericType(args);
        }

        // Called from the runtime to return the corresponding finished Type object
        internal override Type RuntimeResolve()
        {
            if (generic_type is TypeBuilder tb && !tb.IsCreated())
                throw new NotImplementedException();
            for (int i = 0; i < type_arguments.Length; ++i)
            {
                Type t = type_arguments[i];
                if (t is TypeBuilder ttb && !ttb.IsCreated())
                    throw new NotImplementedException();
            }
            return InternalResolve();
        }

        internal override bool IsUserType
        {
            get
            {
                foreach (Type t in type_arguments)
                {
                    if (t.IsUserType)
                        return true;
                }
                return false;
            }
        }

        internal override MethodInfo GetMethod(MethodInfo fromNonInstantiated)
        {
            return new MethodOnTypeBuilderInstantiation(fromNonInstantiated, this);
        }

        internal override ConstructorInfo GetConstructor(ConstructorInfo fromNoninstanciated)
        {
            return new ConstructorOnTypeBuilderInstantiation(fromNoninstanciated, this);
        }

        internal override FieldInfo GetField(FieldInfo fromNoninstanciated)
        {
            return FieldOnTypeBuilderInstantiation.GetField(fromNoninstanciated, this);
        }
    }
}
