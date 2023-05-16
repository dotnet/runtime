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
// System.Reflection.Emit/FieldBuilder.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//
// (C) 2001-2002 Ximian, Inc.  http://www.ximian.com
//

#if MONO_FEATURE_SRE
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Buffers.Binary;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    internal sealed partial class RuntimeFieldBuilder : FieldBuilder
    {
#region Sync with MonoReflectionFieldBuilder in object-internals.h
        private FieldAttributes attrs;
        private Type type;
        private string name;
        private object? def_value;
        private int offset;
        internal RuntimeTypeBuilder typeb;
        private byte[]? rva_data;
        private CustomAttributeBuilder[]? cattrs;
        private UnmanagedMarshal? marshal_info;
        private RuntimeFieldHandle handle;
        private Type[]? modReq;
        private Type[]? modOpt;
#endregion

        [DynamicDependency(nameof(modOpt))]  // Automatically keeps all previous fields too due to StructLayout
        internal RuntimeFieldBuilder(RuntimeTypeBuilder tb, string fieldName, Type type, FieldAttributes attributes, Type[]? modReq, Type[]? modOpt)
        {
            ArgumentNullException.ThrowIfNull(type);

            attrs = attributes & ~FieldAttributes.ReservedMask;
            name = fieldName;
            this.type = type;
            this.modReq = modReq;
            this.modOpt = modOpt;
            offset = -1;
            typeb = tb;

            ((RuntimeModuleBuilder)tb.Module).RegisterToken(this, MetadataToken);
        }

        public override FieldAttributes Attributes
        {
            get { return attrs; }
        }

        public override Type? DeclaringType
        {
            get
            {
                if (typeb.is_hidden_global_type)
                    return null;
                return typeb;
            }
        }

        public override RuntimeFieldHandle FieldHandle
        {
            get
            {
                throw CreateNotSupportedException();
            }
        }

        public override Type FieldType
        {
            get { return type; }
        }

        public override string Name
        {
            get { return name; }
        }

        public override Type? ReflectedType
        {
            get { return typeb; }
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            /*
             * On MS.NET, this always returns not_supported, but we can't do this
             * since there would be no way to obtain custom attributes of
             * dynamically created ctors.
             */
            if (typeb.is_created)
                return CustomAttribute.GetCustomAttributes(this, inherit);
            else
                throw CreateNotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (typeb.is_created)
                return CustomAttribute.GetCustomAttributes(this, attributeType, inherit);
            else
                throw CreateNotSupportedException();
        }

        public override int MetadataToken { get { return ((RuntimeModuleBuilder)typeb.Module).GetToken(this); } }

        public override object? GetValue(object? obj)
        {
            throw CreateNotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw CreateNotSupportedException();
        }

        internal override int GetFieldOffset()
        {
            /* FIXME: */
            return 0;
        }

        internal void SetRVAData(byte[] data)
        {
            attrs |= FieldAttributes.HasFieldRVA;
            rva_data = (byte[])data.Clone();
        }

        internal static PackingSize RVADataPackingSize(int size)
        {
            if ((size % 8) == 0) return PackingSize.Size8;
            if ((size % 4) == 0) return PackingSize.Size4;
            if ((size % 2) == 0) return PackingSize.Size2;
            return PackingSize.Size1;
        }

        protected override void SetConstantCore(object? defaultValue)
        {
            RejectIfCreated();

            /*if (defaultValue.GetType() != type)
                throw new ArgumentException(SR.Argument_ConstantDoesntMatch);*/
            def_value = defaultValue;
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            RejectIfCreated();
            CustomAttributeBuilder customBuilder = new CustomAttributeBuilder(con, binaryAttribute);
            string? attrname = con.ReflectedType!.FullName;
            if (attrname == "System.Runtime.InteropServices.FieldOffsetAttribute")
            {
                offset = BinaryPrimitives.ReadInt32LittleEndian(binaryAttribute.Slice(2));
                return;
            }
#pragma warning disable SYSLIB0050 // FieldAttributes.NotSerialized is obsolete
            else if (attrname == "System.NonSerializedAttribute")
            {
                attrs |= FieldAttributes.NotSerialized;
                return;
            }
#pragma warning restore SYSLIB0050
            else if (attrname == "System.Runtime.CompilerServices.SpecialNameAttribute")
            {
                attrs |= FieldAttributes.SpecialName;
                return;
            }
            else if (attrname == "System.Runtime.InteropServices.MarshalAsAttribute")
            {
                attrs |= FieldAttributes.HasFieldMarshal;
                marshal_info = CustomAttributeBuilder.get_umarshal(customBuilder, true);
                /* FIXME: check for errors */
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

        protected override void SetOffsetCore(int iOffset)
        {
            RejectIfCreated();
            if (iOffset < 0)
                throw new ArgumentException(SR.Argument_NegativeFieldOffsetNotPermitted);
            offset = iOffset;
        }

        public override void SetValue(object? obj, object? val, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            throw CreateNotSupportedException();
        }

        private static NotSupportedException CreateNotSupportedException()
        {
            return new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        private void RejectIfCreated()
        {
            if (typeb.is_created)
                throw new InvalidOperationException(SR.InvalidOperation_TypeHasBeenCreated);
        }

        internal void ResolveUserTypes()
        {
            type = RuntimeTypeBuilder.ResolveUserType(type);
            RuntimeTypeBuilder.ResolveUserTypes(modReq);
            RuntimeTypeBuilder.ResolveUserTypes(modOpt);
            if (marshal_info != null)
                marshal_info.marshaltyperef = RuntimeTypeBuilder.ResolveUserType(marshal_info.marshaltyperef);
        }

        internal FieldInfo RuntimeResolve()
        {
            // typeb.CreateType() populates this.handle
            var type_handle = new RuntimeTypeHandle((typeb.CreateType() as RuntimeType)!);
            return GetFieldFromHandle(handle, type_handle);
        }

        public override Module Module
        {
            get
            {
                return base.Module;
            }
        }
    }
}

#endif
