// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System
{
    public struct ModuleHandle
    {
        private readonly IntPtr value;

        public static readonly ModuleHandle EmptyHandle = new ModuleHandle(IntPtr.Zero);

        internal ModuleHandle(IntPtr v)
        {
            value = v;
        }

        internal IntPtr Value
        {
            get
            {
                return value;
            }
        }

        public int MDStreamVersion
        {
            get
            {
                if (value == IntPtr.Zero)
                    throw new ArgumentNullException(string.Empty, "Invalid handle");
                return RuntimeModule.GetMDStreamVersion(value);
            }
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeFieldHandle ResolveFieldHandle(int fieldToken)
        {
            return ResolveFieldHandle(fieldToken, null, null);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeMethodHandle ResolveMethodHandle(int methodToken)
        {
            return ResolveMethodHandle(methodToken, null, null);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeTypeHandle ResolveTypeHandle(int typeToken)
        {
            return ResolveTypeHandle(typeToken, null, null);
        }

        private static IntPtr[]? ptrs_from_handles(RuntimeTypeHandle[]? handles)
        {
            if (handles == null)
                return null;

            var res = new IntPtr[handles.Length];
            for (int i = 0; i < handles.Length; ++i)
                res[i] = handles[i].Value;
            return res;
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeTypeHandle ResolveTypeHandle(int typeToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
        {
            ResolveTokenError error;
            if (value == IntPtr.Zero)
                throw new ArgumentNullException(string.Empty, "Invalid handle");
            IntPtr res = RuntimeModule.ResolveTypeToken(value, typeToken, ptrs_from_handles(typeInstantiationContext), ptrs_from_handles(methodInstantiationContext), out error);
            if (res == IntPtr.Zero)
                throw new TypeLoadException(string.Format("Could not load type '0x{0:x}' from assembly '0x{1:x}'", typeToken, value.ToInt64()));
            else
                return new RuntimeTypeHandle(res);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeMethodHandle ResolveMethodHandle(int methodToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
        {
            ResolveTokenError error;
            if (value == IntPtr.Zero)
                throw new ArgumentNullException(string.Empty, "Invalid handle");
            IntPtr res = RuntimeModule.ResolveMethodToken(value, methodToken, ptrs_from_handles(typeInstantiationContext), ptrs_from_handles(methodInstantiationContext), out error);
            if (res == IntPtr.Zero)
                throw new Exception(string.Format("Could not load method '0x{0:x}' from assembly '0x{1:x}'", methodToken, value.ToInt64()));
            else
                return new RuntimeMethodHandle(res);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeFieldHandle ResolveFieldHandle(int fieldToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
        {
            ResolveTokenError error;
            if (value == IntPtr.Zero)
                throw new ArgumentNullException(string.Empty, "Invalid handle");

            IntPtr res = RuntimeModule.ResolveFieldToken(value, fieldToken, ptrs_from_handles(typeInstantiationContext), ptrs_from_handles(methodInstantiationContext), out error);
            if (res == IntPtr.Zero)
                throw new Exception(string.Format("Could not load field '0x{0:x}' from assembly '0x{1:x}'", fieldToken, value.ToInt64()));
            else
                return new RuntimeFieldHandle(res);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeFieldHandle GetRuntimeFieldHandleFromMetadataToken(int fieldToken)
        {
            return ResolveFieldHandle(fieldToken);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeMethodHandle GetRuntimeMethodHandleFromMetadataToken(int methodToken)
        {
            return ResolveMethodHandle(methodToken);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeTypeHandle GetRuntimeTypeHandleFromMetadataToken(int typeToken)
        {
            return ResolveTypeHandle(typeToken);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return value == ((ModuleHandle)obj).Value;
        }

        public bool Equals(ModuleHandle handle)
        {
            return value == handle.Value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static bool operator ==(ModuleHandle left, ModuleHandle right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ModuleHandle left, ModuleHandle right)
        {
            return !Equals(left, right);
        }
    }
}
