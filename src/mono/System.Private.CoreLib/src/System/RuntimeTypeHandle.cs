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

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System
{
    [Serializable]
    public struct RuntimeTypeHandle : ISerializable
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

        private RuntimeTypeHandle(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public IntPtr Value
        {
            get
            {
                return value;
            }
        }

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
        internal static extern TypeAttributes GetAttributes(RuntimeType type);

        public ModuleHandle GetModuleHandle()
        {
            // Although MS' runtime is crashing here, we prefer throwing an exception.
            // The check is needed because Type.GetTypeFromHandle returns null
            // for zero handles.
            if (value == IntPtr.Zero)
                throw new InvalidOperationException("Object fields may not be properly initialized");

            return Type.GetTypeFromHandle(this).Module.ModuleHandle;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetMetadataToken(RuntimeType type);

        internal static int GetToken(RuntimeType type)
        {
            return GetMetadataToken(type);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Type GetGenericTypeDefinition_impl(RuntimeType type);

        internal static Type GetGenericTypeDefinition(RuntimeType type)
        {
            return GetGenericTypeDefinition_impl(type);
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

        internal static bool HasElementType(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);

            return ((corElemType == CorElementType.ELEMENT_TYPE_ARRAY || corElemType == CorElementType.ELEMENT_TYPE_SZARRAY) // IsArray
                   || (corElemType == CorElementType.ELEMENT_TYPE_PTR)                                          // IsPointer
                   || (corElemType == CorElementType.ELEMENT_TYPE_BYREF));                                      // IsByRef
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern CorElementType GetCorElementType(RuntimeType type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool HasInstantiation(RuntimeType type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsComObject(RuntimeType type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsInstanceOfType(RuntimeType type, [NotNullWhen(true)] object? o);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool HasReferences(RuntimeType? type);

        internal static bool IsComObject(RuntimeType type, bool isGenericCOM)
        {
            return isGenericCOM ? false : IsComObject(type);
        }

        internal static bool IsContextful(RuntimeType type)
        {
            return false;
        }

        internal static bool IsEquivalentTo(RuntimeType rtType1, RuntimeType rtType2)
        {
            // refence check is done earlier and we don't recognize anything else
            return false;
        }

        internal static bool IsInterface(RuntimeType type)
        {
            return (type.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int GetArrayRank(RuntimeType type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern RuntimeAssembly GetAssembly(RuntimeType type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType GetElementType(RuntimeType type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern RuntimeModule GetModule(RuntimeType type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsGenericVariable(RuntimeType type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType GetBaseType(RuntimeType type);

        internal static bool CanCastTo(RuntimeType type, RuntimeType target)
        {
            return type_is_assignable_from(target, type);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool type_is_assignable_from(Type a, Type b);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsGenericTypeDefinition(RuntimeType type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetGenericParameterInfo(RuntimeType type);

        internal static bool IsSubclassOf(RuntimeType childType, RuntimeType baseType)
        {
            return is_subclass_of(childType._impl.Value, baseType._impl.Value);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool is_subclass_of(IntPtr childType, IntPtr baseType);

        [DynamicDependency("#ctor()", typeof(IsByRefLikeAttribute))]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsByRefLike(RuntimeType type);

        internal static bool IsTypeDefinition(RuntimeType type)
        {
            // That's how it has been done on CoreFX but we have no GetCorElementType method implementation
            // see https://github.com/dotnet/coreclr/pull/11355

            // CorElementType corElemType = GetCorElementType (type);
            // if (!((corElemType >= CorElementType.Void && corElemType < CorElementType.Ptr) ||
            //      corElemType == CorElementType.ValueType ||
            //      corElemType == CorElementType.Class ||
            //      corElemType == CorElementType.TypedByRef ||
            //      corElemType == CorElementType.I ||
            //      corElemType == CorElementType.U ||
            //      corElemType == CorElementType.Object))
            //  return false;
            // if (HasInstantiation (type) && !IsGenericTypeDefinition (type))
            //  return false;
            // return true;

            // It's like a workaround mentioned in https://github.com/dotnet/runtime/issues/20711
            return !type.HasElementType && !type.IsConstructedGenericType && !type.IsGenericParameter;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern RuntimeType internal_from_name(string name, ref StackCrawlMark stackMark, Assembly? callerAssembly, bool throwOnError, bool ignoreCase);

        [RequiresUnreferencedCode("Types might be removed")]
        internal static RuntimeType? GetTypeByName(string typeName, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark,
                                                  bool loadTypeFromPartialName)
        {
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName));

            if (typeName.Length == 0)
                if (throwOnError)
                    throw new TypeLoadException("A null or zero length string does not represent a valid Type.");
                else
                    return null;

            RuntimeType? t = internal_from_name(typeName, ref stackMark, null, throwOnError, ignoreCase);
            if (throwOnError && t == null)
                throw new TypeLoadException("Error loading '" + typeName + "'");
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
