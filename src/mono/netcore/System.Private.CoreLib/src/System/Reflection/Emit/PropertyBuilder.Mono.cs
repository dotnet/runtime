// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

//
// System.Reflection.Emit/PropertyBuilder.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
//

#if MONO_FEATURE_SRE
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed partial class PropertyBuilder : PropertyInfo
    {
#region Sync with MonoReflectionPropertyBuilder in object-internals.h
        private PropertyAttributes attrs;
        private string name;
        private Type type;
        private Type[]? parameters;
        private CustomAttributeBuilder[]? cattrs;
        private object? def_value;
        private MethodBuilder? set_method;
        private MethodBuilder? get_method;
        private int table_idx;
        internal TypeBuilder typeb;
        private Type[]? returnModReq;
        private Type[]? returnModOpt;
        private Type[][]? paramModReq;
        private Type[][]? paramModOpt;
        private CallingConventions callingConvention;
#endregion

        internal PropertyBuilder(TypeBuilder tb, string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[]? returnModReq, Type[]? returnModOpt, Type[]? parameterTypes, Type[][]? paramModReq, Type[][]? paramModOpt)
        {
            this.name = name;
            this.attrs = attributes;
            this.callingConvention = callingConvention;
            this.type = returnType;
            this.returnModReq = returnModReq;
            this.returnModOpt = returnModOpt;
            this.paramModReq = paramModReq;
            this.paramModOpt = paramModOpt;
            if (parameterTypes != null)
            {
                this.parameters = new Type[parameterTypes.Length];
                Array.Copy(parameterTypes, this.parameters, this.parameters.Length);
            }
            typeb = tb;
            table_idx = tb.get_next_table_index(this, 0x17, 1);
        }

        public override PropertyAttributes Attributes
        {
            get { return attrs; }
        }
        public override bool CanRead
        {
            get { return get_method != null; }
        }
        public override bool CanWrite
        {
            get { return set_method != null; }
        }
        public override Type DeclaringType
        {
            get { return typeb; }
        }
        public override string Name
        {
            get { return name; }
        }
        public override Type PropertyType
        {
            get { return type; }
        }
        public override Type ReflectedType
        {
            get { return typeb; }
        }

        public void AddOtherMethod(MethodBuilder mdBuilder)
        {
            if (mdBuilder == null)
                throw new ArgumentNullException(nameof(mdBuilder));
            typeb.check_not_created();
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            return null!; // FIXME: coreclr throws
        }
        public override object[] GetCustomAttributes(bool inherit)
        {
            throw not_supported();
        }
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw not_supported();
        }
        public override MethodInfo? GetGetMethod(bool nonPublic)
        {
            return get_method;
        }
        public override ParameterInfo[] GetIndexParameters()
        {
            throw not_supported();
        }
        public override MethodInfo? GetSetMethod(bool nonPublic)
        {
            return set_method;
        }

        public override object? GetValue(object? obj, object?[]? index)
        {
            throw not_supported();
        }

        public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            throw not_supported();
        }
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw not_supported();
        }
        public void SetConstant(object? defaultValue)
        {
            typeb.check_not_created();
            def_value = defaultValue;
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
                throw new ArgumentNullException(nameof(customBuilder));
            typeb.check_not_created();
            string? attrname = customBuilder.Ctor.ReflectedType!.FullName;
            if (attrname == "System.Runtime.CompilerServices.SpecialNameAttribute")
            {
                attrs |= PropertyAttributes.SpecialName;
                return;
            }

            if (cattrs != null)
            {
                CustomAttributeBuilder[] new_array = new CustomAttributeBuilder[cattrs.Length + 1];
                cattrs.CopyTo(new_array, 0);
                new_array[cattrs.Length] = customBuilder;
                cattrs = new_array;
            }
            else
            {
                cattrs = new CustomAttributeBuilder[1];
                cattrs[0] = customBuilder;
            }
        }

        [ComVisible(true)]
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            SetCustomAttribute(new CustomAttributeBuilder(con, binaryAttribute));
        }

        public void SetGetMethod(MethodBuilder mdBuilder)
        {
            typeb.check_not_created();
            if (mdBuilder == null)
                throw new ArgumentNullException(nameof(mdBuilder));
            get_method = mdBuilder;
        }

        public void SetSetMethod(MethodBuilder mdBuilder)
        {
            if (mdBuilder == null)
                throw new ArgumentNullException(nameof(mdBuilder));
            set_method = mdBuilder;
        }

        public override void SetValue(object? obj, object? value, object?[]? index)
        {
            throw not_supported();
        }

        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            throw not_supported();
        }

        public override Module Module
        {
            get
            {
                return base.Module;
            }
        }

        private static Exception not_supported()
        {
            return new NotSupportedException("The invoked member is not supported in a dynamic module.");
        }
    }
}

#endif
