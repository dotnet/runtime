// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Authors:
//   Miguel de Icaza (miguel@ximian.com)
//   Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//
// (C) Ximian, Inc.  http://www.ximian.com
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

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System
{
    public struct RuntimeTypeHandle : IEquatable<RuntimeTypeHandle>, ISerializable
    {
        private readonly IntPtr value;

        internal RuntimeTypeHandle(IntPtr val)
        {
            value = val;
        }

        internal RuntimeTypeHandle(RuntimeType type)
            : this(type._impl.value)
        {
        }

        public IntPtr Value
        {
            get
            {
                return value;
            }
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return value == ((RuntimeTypeHandle)obj).Value;
        }

        public bool Equals(RuntimeTypeHandle handle)
        {
            return value == handle.Value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static RuntimeTypeHandle FromIntPtr(IntPtr value) => new RuntimeTypeHandle(value);

        public static IntPtr ToIntPtr(RuntimeTypeHandle value) => value.Value;

        public static bool operator ==(RuntimeTypeHandle left, object right)
        {
            return (right != null) && (right is RuntimeTypeHandle) && left.Equals((RuntimeTypeHandle)right);
        }

        public static bool operator !=(RuntimeTypeHandle left, object right)
        {
            return (right == null) || !(right is RuntimeTypeHandle) || !left.Equals((RuntimeTypeHandle)right);
        }

        public static bool operator ==(object left, RuntimeTypeHandle right)
        {
            return (left != null) && (left is RuntimeTypeHandle) && ((RuntimeTypeHandle)left).Equals(right);
        }

        public static bool operator !=(object left, RuntimeTypeHandle right)
        {
            return (left == null) || !(left is RuntimeTypeHandle) || !((RuntimeTypeHandle)left).Equals(right);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern TypeAttributes GetAttributes(QCallTypeHandle type);

        internal static TypeAttributes GetAttributes(RuntimeType type)
        {
            return type.GetAttributes();
        }

        public ModuleHandle GetModuleHandle()
        {
            // The check is needed because Type.GetTypeFromHandle returns null
            // for zero handles.
            if (value == IntPtr.Zero)
                throw new ArgumentException(SR.Arg_InvalidHandle);

            return Type.GetTypeFromHandle(this)!.Module.ModuleHandle;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetMetadataToken(QCallTypeHandle type);

        internal static int GetToken(RuntimeType type)
        {
            return GetMetadataToken(new QCallTypeHandle(ref type));
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetGenericTypeDefinition_impl(QCallTypeHandle type, ObjectHandleOnStack res);

        internal static Type GetGenericTypeDefinition(RuntimeType type)
        {
            Type? res = null;
            GetGenericTypeDefinition_impl(new QCallTypeHandle(ref type), ObjectHandleOnStack.Create(ref res));
            if (res == null)
                // The icall returns null if TYPE is a gtd
                return type;
            return res!;
        }

        internal static bool IsPrimitive(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return (corElemType >= CorElementType.ELEMENT_TYPE_BOOLEAN && corElemType <= CorElementType.ELEMENT_TYPE_R8) ||
                corElemType == CorElementType.ELEMENT_TYPE_I ||
                corElemType == CorElementType.ELEMENT_TYPE_U;
        }

        internal static bool IsByRef(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return corElemType == CorElementType.ELEMENT_TYPE_BYREF;
        }

        internal static bool IsPointer(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return corElemType == CorElementType.ELEMENT_TYPE_PTR;
        }

        internal static bool IsFunctionPointer(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return corElemType == CorElementType.ELEMENT_TYPE_FNPTR;
        }

        internal static bool IsArray(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return corElemType == CorElementType.ELEMENT_TYPE_ARRAY || corElemType == CorElementType.ELEMENT_TYPE_SZARRAY;
        }

        internal static bool IsSzArray(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return corElemType == CorElementType.ELEMENT_TYPE_SZARRAY;
        }

        internal static bool IsValueType(RuntimeType type) => type.IsValueType;

        internal static bool HasElementType(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);

            return ((corElemType == CorElementType.ELEMENT_TYPE_ARRAY || corElemType == CorElementType.ELEMENT_TYPE_SZARRAY) // IsArray
                   || (corElemType == CorElementType.ELEMENT_TYPE_PTR)                                          // IsPointer
                   || (corElemType == CorElementType.ELEMENT_TYPE_BYREF));                                      // IsByRef
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern CorElementType GetCorElementType(QCallTypeHandle type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool HasInstantiation(QCallTypeHandle type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsComObject(QCallTypeHandle type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsInstanceOfType(QCallTypeHandle type, [NotNullWhen(true)] object? o);

        internal static bool IsInstanceOfType(RuntimeType type, [NotNullWhen(true)] object? o)
        {
            return IsInstanceOfType(new QCallTypeHandle(ref type), o);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool HasReferences(QCallTypeHandle type);

        internal static bool HasReferences(RuntimeType type)
        {
            return HasReferences(new QCallTypeHandle(ref type));
        }

        internal static CorElementType GetCorElementType(RuntimeType type)
        {
            return type.GetCorElementType();
        }

        internal static bool HasInstantiation(RuntimeType type)
        {
            return HasInstantiation(new QCallTypeHandle(ref type));
        }

        internal static bool IsComObject(RuntimeType type, bool isGenericCOM)
        {
            return isGenericCOM ? false : IsComObject(new QCallTypeHandle(ref type));
        }

#pragma warning disable IDE0060
        internal static bool IsEquivalentTo(RuntimeType rtType1, RuntimeType rtType2)
        {
            // reference check is done earlier and we don't recognize anything else
            return false;
        }
#pragma warning restore IDE0060

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int GetArrayRank(QCallTypeHandle type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void GetAssembly(QCallTypeHandle type, ObjectHandleOnStack res);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void GetElementType(QCallTypeHandle type, ObjectHandleOnStack res);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void GetModule(QCallTypeHandle type, ObjectHandleOnStack res);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void GetBaseType(QCallTypeHandle type, ObjectHandleOnStack res);

        internal static int GetArrayRank(RuntimeType type)
        {
            return GetArrayRank(new QCallTypeHandle(ref type));
        }

        internal static RuntimeAssembly GetAssembly(RuntimeType type)
        {
            RuntimeAssembly? res = null;
            GetAssembly(new QCallTypeHandle(ref type), ObjectHandleOnStack.Create(ref res));
            return res!;
        }

        internal static RuntimeModule GetModule(RuntimeType type)
        {
            RuntimeModule? res = null;
            GetModule(new QCallTypeHandle(ref type), ObjectHandleOnStack.Create(ref res));
            return res!;
        }

        internal static RuntimeType GetElementType(RuntimeType type)
        {
            RuntimeType? res = null;
            GetElementType(new QCallTypeHandle(ref type), ObjectHandleOnStack.Create(ref res));
            return res!;
        }

        internal static RuntimeType GetBaseType(RuntimeType type)
        {
            RuntimeType? res = null;
            GetBaseType(new QCallTypeHandle(ref type), ObjectHandleOnStack.Create(ref res));
            return res!;
        }

        internal static bool CanCastTo(RuntimeType type, RuntimeType target)
        {
            return type_is_assignable_from(new QCallTypeHandle(ref target), new QCallTypeHandle(ref type));
        }

        internal static bool IsGenericVariable(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return corElemType == CorElementType.ELEMENT_TYPE_VAR || corElemType == CorElementType.ELEMENT_TYPE_MVAR;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool type_is_assignable_from(QCallTypeHandle a, QCallTypeHandle b);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsGenericTypeDefinition(QCallTypeHandle type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetGenericParameterInfo(QCallTypeHandle type);

        internal static bool IsGenericTypeDefinition(RuntimeType type)
        {
            return IsGenericTypeDefinition(new QCallTypeHandle(ref type));
        }

        internal static IntPtr GetGenericParameterInfo(RuntimeType type)
        {
            return GetGenericParameterInfo(new QCallTypeHandle(ref type));
        }

        internal static bool IsSubclassOf(RuntimeType childType, RuntimeType baseType)
        {
            return is_subclass_of(new QCallTypeHandle(ref childType), new QCallTypeHandle(ref baseType));
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool is_subclass_of(QCallTypeHandle childType, QCallTypeHandle baseType);

        [DynamicDependency("#ctor()", typeof(IsByRefLikeAttribute))]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsByRefLike(QCallTypeHandle type);

        internal static bool IsByRefLike(RuntimeType type)
        {
            return IsByRefLike(new QCallTypeHandle(ref type));
        }

        internal static bool IsTypeDefinition(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            if (!((corElemType >= CorElementType.ELEMENT_TYPE_VOID && corElemType < CorElementType.ELEMENT_TYPE_PTR) ||
                    corElemType == CorElementType.ELEMENT_TYPE_VALUETYPE ||
                    corElemType == CorElementType.ELEMENT_TYPE_CLASS ||
                    corElemType == CorElementType.ELEMENT_TYPE_TYPEDBYREF ||
                    corElemType == CorElementType.ELEMENT_TYPE_I ||
                    corElemType == CorElementType.ELEMENT_TYPE_U ||
                    corElemType == CorElementType.ELEMENT_TYPE_OBJECT))
                return false;

            if (HasInstantiation(type) && !IsGenericTypeDefinition(type))
                return false;

            return true;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void internal_from_name(IntPtr name, ref StackCrawlMark stackMark, ObjectHandleOnStack res, bool throwOnError, bool ignoreCase);

        [RequiresUnreferencedCode("Types might be removed")]
        internal static RuntimeType? GetTypeByName(string typeName, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark)
        {
            ArgumentNullException.ThrowIfNull(typeName);

            if (typeName.Length == 0)
                if (throwOnError)
                    throw new TypeLoadException(SR.Arg_TypeLoadNullStr);
                else
                    return null;

            RuntimeType? t = null;
            using (var namePtr = new Mono.SafeStringMarshal(typeName)) {
                internal_from_name(
                                   namePtr.Value,
                                   ref stackMark,
                                   ObjectHandleOnStack.Create(ref t), throwOnError, ignoreCase);
                if (throwOnError && t == null)
                    throw new TypeLoadException(SR.Arg_TypeLoadException);
            }
            return t;
        }

        internal static IntPtr[]? CopyRuntimeTypeHandles(RuntimeTypeHandle[]? inHandles, out int length)
        {
            if (inHandles == null || inHandles.Length == 0)
            {
                length = 0;
                return null;
            }

            IntPtr[] outHandles = new IntPtr[inHandles.Length];
            for (int i = 0; i < inHandles.Length; i++)
                outHandles[i] = inHandles[i].Value;
            length = outHandles.Length;
            return outHandles;
        }
    }
}
