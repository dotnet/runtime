// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Reflection/MonoMethod.cs
// The class used to represent methods from the mono runtime.
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
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

#if MONO_FEATURE_SRE
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class MonoArrayMethod : MethodInfo
    {
#pragma warning disable 649
        internal RuntimeMethodHandle mhandle;
        internal Type parent;
        internal Type ret;
        internal Type[] parameters;
        internal string name;
        internal int table_idx;
        internal CallingConventions call_conv;
#pragma warning restore 649

        internal MonoArrayMethod(Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            name = methodName;
            parent = arrayClass;
            ret = returnType;
            parameters = (Type[])parameterTypes.Clone();
            call_conv = callingConvention;
        }

        // FIXME: "Always returns this"
        public override MethodInfo GetBaseDefinition()
        {
            return this; /* FIXME */
        }
        public override Type ReturnType
        {
            get
            {
                return ret;
            }
        }

        // FIXME: "Not implemented.  Always returns null"
        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get { return null!; }
        }

        // FIXME: "Not implemented.  Always returns zero"
        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return (MethodImplAttributes)0;
        }

        // FIXME: "Not implemented.  Always returns an empty array"
        public override ParameterInfo[] GetParameters()
        {
            return GetParametersInternal();
        }

        internal override ParameterInfo[] GetParametersInternal()
        {
            return Array.Empty<ParameterInfo>();
        }

        // FIXME: "Not implemented.  Always returns 0"
        internal override int GetParametersCount()
        {
            return 0;
        }

        // FIXME: "Not implemented"
        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { return mhandle; }
        }

        // FIXME: "Not implemented.  Always returns zero"
        public override MethodAttributes Attributes
        {
            get
            {
                return (MethodAttributes)0;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return parent;
            }
        }
        public override Type DeclaringType
        {
            get
            {
                return parent;
            }
        }
        public override string Name
        {
            get
            {
                return name;
            }
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return CustomAttribute.IsDefined(this, attributeType, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, inherit);
        }
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, attributeType, inherit);
        }

        public override string ToString()
        {
            string parms = string.Empty;
            ParameterInfo[] p = GetParameters();
            for (int i = 0; i < p.Length; ++i)
            {
                if (i > 0)
                    parms = parms + ", ";
                parms = parms + p[i].ParameterType.Name;
            }
            if (ReturnType != null)
                return ReturnType.Name + " " + Name + "(" + parms + ")";
            else
                return "void " + Name + "(" + parms + ")";
        }
    }
}
#endif
