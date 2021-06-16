// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Reflection.Emit/PropertyOnTypeBuilderInst.cs
//
// Author:
//   Rodrigo Kumpera (rkumpera@novell.com)
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

#if MONO_FEATURE_SRE
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    /*
     * This class represents a property of an instantiation of a generic type builder.
     */
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class PropertyOnTypeBuilderInst : PropertyInfo
    {
        private TypeBuilderInstantiation instantiation;
        private PropertyInfo prop;

        internal PropertyOnTypeBuilderInst(TypeBuilderInstantiation instantiation, PropertyInfo prop)
        {
            this.instantiation = instantiation;
            this.prop = prop;
        }

        public override PropertyAttributes Attributes
        {
            get { throw new NotSupportedException(); }
        }

        public override bool CanRead
        {
            get { throw new NotSupportedException(); }
        }

        public override bool CanWrite
        {
            get { throw new NotSupportedException(); }
        }

        public override Type PropertyType
        {
            get { return instantiation.InflateType(prop.PropertyType)!; }
        }

        public override Type? DeclaringType
        {
            get { return instantiation.InflateType(prop.DeclaringType); }
        }

        public override Type ReflectedType
        {
            get { return instantiation; }
        }

        public override string Name
        {
            get { return prop.Name; }
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            MethodInfo? getter = GetGetMethod(nonPublic);
            MethodInfo? setter = GetSetMethod(nonPublic);

            int methods = 0;
            if (getter != null)
                ++methods;
            if (setter != null)
                ++methods;

            MethodInfo[] res = new MethodInfo[methods];

            methods = 0;
            if (getter != null)
                res[methods++] = getter;
            if (setter != null)
                res[methods] = setter;

            return res;
        }


        public override MethodInfo? GetGetMethod(bool nonPublic)
        {
            MethodInfo? mi = prop.GetGetMethod(nonPublic);
            if (mi != null && prop.DeclaringType == instantiation.generic_type)
            {
                mi = TypeBuilder.GetMethod(instantiation, mi);
            }
            return mi;
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            MethodInfo? method = GetGetMethod(true);
            if (method != null)
                return method.GetParameters();

            return Array.Empty<ParameterInfo>();
        }

        public override MethodInfo? GetSetMethod(bool nonPublic)
        {
            MethodInfo? mi = prop.GetSetMethod(nonPublic);
            if (mi != null && prop.DeclaringType == instantiation.generic_type)
            {
                mi = TypeBuilder.GetMethod(instantiation, mi);
            }
            return mi;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", PropertyType, Name);
        }

        public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            throw new NotSupportedException();
        }

        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            throw new NotSupportedException();
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
    }
}

#endif
