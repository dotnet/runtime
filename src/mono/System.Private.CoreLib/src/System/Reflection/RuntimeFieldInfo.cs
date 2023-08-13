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

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.Reflection
{
    // Note that in CoreCLR, RtFieldInfo derives from RuntimeFieldInfo.
    internal abstract class RtFieldInfo : FieldInfo
    {
        internal abstract object UnsafeGetValue(object obj);
        internal abstract void UnsafeSetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture);
        internal abstract void CheckConsistency(object target);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class RuntimeFieldInfo : RtFieldInfo
    {
#pragma warning disable 649
        internal IntPtr klass;
        internal RuntimeFieldHandle fhandle;
        private string? name;
        private Type? type;
        private FieldAttributes attrs;
#pragma warning restore 649

        public override Module Module
        {
            get
            {
                return GetRuntimeModule();
            }
        }

        internal RuntimeType GetDeclaringTypeInternal()
        {
            return (RuntimeType)DeclaringType!;
        }

        internal RuntimeModule GetRuntimeModule()
        {
            return GetDeclaringTypeInternal().GetRuntimeModule();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern override object UnsafeGetValue(object obj);

        internal override void CheckConsistency(object target)
        {
            // only test instance fields
            if ((Attributes & FieldAttributes.Static) != FieldAttributes.Static)
            {
                if (!DeclaringType!.IsInstanceOfType(target))
                {
                    if (target == null)
                    {
                        throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);
                    }
                    else
                    {
                        throw new ArgumentException(
                            SR.Format(SR.Arg_FieldDeclTarget,
                                Name, DeclaringType, target.GetType()));
                    }
                }
            }
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        internal override void UnsafeSetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            bool domainInitialized = false;
            RuntimeFieldHandle.SetValue(this, obj, value, null, Attributes, null, ref domainInitialized);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public override void SetValueDirect(TypedReference obj, object value)
        {
            if (obj.IsNull)
                throw new ArgumentException(SR.Arg_TypedReference_Null);

            unsafe
            {
                // Passing TypedReference by reference is easier to make correct in native code
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('TypedReference')
                RuntimeFieldHandle.SetValueDirect(this, (RuntimeType)FieldType, &obj, value, (RuntimeType?)DeclaringType);
#pragma warning restore CS8500
            }
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public override object GetValueDirect(TypedReference obj)
        {
            if (obj.IsNull)
                throw new ArgumentException(SR.Arg_TypedReference_Null);

            unsafe
            {
                // Passing TypedReference by reference is easier to make correct in native code
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('TypedReference')
                return RuntimeFieldHandle.GetValueDirect(this, (RuntimeType)FieldType, &obj, (RuntimeType?)DeclaringType);
#pragma warning restore CS8500
            }
        }

        public override FieldAttributes Attributes
        {
            get
            {
                return attrs;
            }
        }
        public override RuntimeFieldHandle FieldHandle
        {
            get
            {
                return fhandle;
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern Type ResolveType();

        public override Type FieldType => type ??= ResolveType();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern Type GetParentType(bool declaring);

        public override Type ReflectedType
        {
            get
            {
                return GetParentType(false);
            }
        }
        public override Type? DeclaringType
        {
            get
            {
                Type parentType = GetParentType(true);
                return parentType.Name != "<Module>" ? parentType : null;
            }
        }
        public override string Name
        {
            get
            {
                return name!;
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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern override int GetFieldOffset();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern object? GetValueInternal(object? obj);

        public override object? GetValue(object? obj)
        {
            if (!IsStatic)
            {
                if (obj == null)
                    throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);
                if (!DeclaringType!.IsAssignableFrom(obj.GetType()))
                    throw new ArgumentException(SR.Format(SR.Arg_FieldDeclTarget, Name, DeclaringType, obj.GetType()), nameof(obj));
            }

            if (!IsLiteral)
                CheckGeneric();

            return GetValueInternal(obj);
        }

        public override string ToString()
        {
            return $"{FieldType.FormatTypeName ()} {name}";
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SetValueInternal(FieldInfo fi, object? obj, object? value);

        public override void SetValue(object? obj, object? val, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            if (!IsStatic)
            {
                if (obj == null)
                    throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);
                if (!DeclaringType!.IsAssignableFrom(obj.GetType()))
                    throw new ArgumentException(SR.Format(SR.Arg_FieldDeclTarget, Name, DeclaringType, obj.GetType()), nameof(obj));
            }
            if (IsLiteral)
                throw new FieldAccessException(SR.Acc_ReadOnly);

            binder ??= Type.DefaultBinder;
            CheckGeneric();
            if (val != null)
            {
                RuntimeType fieldType = (RuntimeType)FieldType;

                if (!ReferenceEquals(val.GetType(), fieldType))
                {
                    fieldType.CheckValue(ref val, binder, culture, invokeAttr);
                }
            }

            SetValueInternal(this, obj, val);
        }

        internal RuntimeFieldInfo Clone(string newName)
        {
            RuntimeFieldInfo field = new RuntimeFieldInfo();
            field.name = newName;
            field.type = type;
            field.attrs = attrs;
            field.klass = klass;
            field.fhandle = fhandle;
            return field;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern override object GetRawConstantValue();

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
        }

        private void CheckGeneric()
        {
            Type? declaringType = DeclaringType;
            if (declaringType != null && declaringType.ContainsGenericParameters)
                throw new InvalidOperationException(SR.Arg_UnboundGenField);
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => HasSameMetadataDefinitionAsCore<RuntimeFieldInfo>(other);

        public override int MetadataToken
        {
            get
            {
                return get_metadata_token(this);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int get_metadata_token(RuntimeFieldInfo monoField);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern Type[] GetTypeModifiers(bool optional, int genericArgumentPosition = -1);

        public override Type[] GetOptionalCustomModifiers() => GetCustomModifiers(true);

        public override Type[] GetRequiredCustomModifiers() => GetCustomModifiers(false);

        private Type[] GetCustomModifiers(bool optional) => GetTypeModifiers(optional) ?? Type.EmptyTypes;

        internal Type[] GetCustomModifiersFromModifiedType(bool optional, int genericArgumentPosition) => GetTypeModifiers(optional, genericArgumentPosition) ?? Type.EmptyTypes;

        public override Type GetModifiedFieldType() => ModifiedType.Create(FieldType, this);
    }
}
