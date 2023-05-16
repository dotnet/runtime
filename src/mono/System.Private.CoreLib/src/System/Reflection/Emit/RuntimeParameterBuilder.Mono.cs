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
// System.Reflection.Emit/ParameterBuilder.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
//

#if MONO_FEATURE_SRE
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class RuntimeParameterBuilder : ParameterBuilder
    {
#region Sync with MonoReflectionParamBuilder in object-internals.h
        private MethodBase methodb; /* MethodBuilder, ConstructorBuilder or DynamicMethod */
        private string? name;
        private CustomAttributeBuilder[]? cattrs;
        private UnmanagedMarshal? marshal_info;
        private ParameterAttributes attrs;
        private int position;
        private int table_idx;
        private object? def_value;
#endregion

        [DynamicDependency(nameof(def_value))]  // Automatically keeps all previous fields too due to StructLayout
        internal RuntimeParameterBuilder(MethodBase mb, int pos, ParameterAttributes attributes, string? strParamName)
        {
            name = strParamName;
            position = pos;
            attrs = attributes;
            methodb = mb;
            if (mb is DynamicMethod)
                table_idx = 0;
            else
                table_idx = mb.get_next_table_index(0x08, 1);
        }

        public override int Attributes
        {
            get { return (int)attrs; }
        }
        public override string? Name
        {
            get { return name; }
        }
        public override int Position
        {
            get { return position; }
        }

        public override void SetConstant(object? defaultValue)
        {
            if (position > 0)
            {
                RuntimeTypeBuilder.SetConstantValue(methodb.GetParameterType(position - 1),
                                  defaultValue, ref defaultValue);
            }

            def_value = defaultValue;
            attrs |= ParameterAttributes.HasDefault;
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            CustomAttributeBuilder customBuilder = new CustomAttributeBuilder(con, binaryAttribute);
            string? attrname = con.ReflectedType!.FullName;
            if (attrname == "System.Runtime.InteropServices.InAttribute")
            {
                attrs |= ParameterAttributes.In;
                return;
            }
            else if (attrname == "System.Runtime.InteropServices.OutAttribute")
            {
                attrs |= ParameterAttributes.Out;
                return;
            }
            else if (attrname == "System.Runtime.InteropServices.OptionalAttribute")
            {
                attrs |= ParameterAttributes.Optional;
                return;
            }
            else if (attrname == "System.Runtime.InteropServices.MarshalAsAttribute")
            {
                attrs |= ParameterAttributes.HasFieldMarshal;
                marshal_info = CustomAttributeBuilder.get_umarshal(customBuilder, false);
                /* FIXME: check for errors */
                return;
            }
            else if (attrname == "System.Runtime.InteropServices.DefaultParameterValueAttribute")
            {
                /* MS.NET doesn't handle this attribute but we handle it for consistency */
                CustomAttributeBuilder.CustomAttributeInfo cinfo = CustomAttributeBuilder.decode_cattr(customBuilder);
                /* FIXME: check for type compatibility */
                SetConstant(cinfo.ctorArgs[0]);
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
    }
}

#endif
