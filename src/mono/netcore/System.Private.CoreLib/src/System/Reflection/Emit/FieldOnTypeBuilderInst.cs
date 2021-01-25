// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Reflection.Emit/FieldOnTypeBuilderInst.cs
//
// Author:
//   Zoltan Varga (vargaz@gmail.com)
//
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    /*
     * This class represents a field of an instantiation of a generic type builder.
     */
    internal sealed class FieldOnTypeBuilderInst : FieldInfo
    {
        internal TypeBuilderInstantiation instantiation;
        internal FieldInfo fb;

        public FieldOnTypeBuilderInst(TypeBuilderInstantiation instantiation, FieldInfo fb)
        {
            this.instantiation = instantiation;
            this.fb = fb;
        }

        //
        // MemberInfo members
        //

        public override Type DeclaringType
        {
            get
            {
                return instantiation;
            }
        }

        public override string Name
        {
            get
            {
                return fb.Name;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return instantiation;
            }
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override string ToString()
        {
            return fb.FieldType.ToString() + " " + Name;
        }
        //
        // FieldInfo members
        //

        public override FieldAttributes Attributes
        {
            get
            {
                return fb.Attributes;
            }
        }

        public override RuntimeFieldHandle FieldHandle
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override int MetadataToken
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        public override Type FieldType
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override object? GetValue(object? obj)
        {
            throw new NotSupportedException();
        }

        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            throw new NotSupportedException();
        }

        // Called from the runtime to return the corresponding finished FieldInfo object
        internal FieldInfo RuntimeResolve()
        {
            Type type = instantiation.RuntimeResolve();
            return type.GetField(fb);
        }
    }
}
#endif
