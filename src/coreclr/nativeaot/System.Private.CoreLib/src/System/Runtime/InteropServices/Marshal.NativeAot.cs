// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Internal.Runtime.Augments;
using Internal.Runtime.CompilerHelpers;

namespace System.Runtime.InteropServices
{
    public static partial class Marshal
    {
        internal static int SizeOfHelper(Type t, bool throwIfNotMarshalable)
        {
            Debug.Assert(throwIfNotMarshalable);

            if (t is not RuntimeType)
                throw new ArgumentException(SR.Argument_MustBeRuntimeType);

            if (t.IsPointer /* or IsFunctionPointer */)
                return IntPtr.Size;

            if (t.IsByRef || t.IsArray || t.ContainsGenericParameters)
                throw new ArgumentException(SR.Format(SR.Arg_CannotMarshal, t));

            return RuntimeInteropData.GetStructUnsafeStructSize(t.TypeHandle);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr OffsetOf(Type t, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(t);

            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentNullException(nameof(fieldName));

            // COMPAT: CoreCLR would allow a non-runtime type as long as has a runtime field.
            // We need a runtime type because we don't reflection-locate the field.
            if (t is not RuntimeType)
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeFieldInfo, nameof(fieldName));
            }

            if (t.TypeHandle.IsGenericTypeDefinition())
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(t));

            return new IntPtr(RuntimeInteropData.GetStructFieldOffset(t.TypeHandle, fieldName));
        }

        private static void PtrToStructureHelper(IntPtr ptr, object structure, bool allowValueClasses)
        {
            ArgumentNullException.ThrowIfNull(ptr);
            ArgumentNullException.ThrowIfNull(structure);

            if (!allowValueClasses && structure.GetEETypePtr().IsValueType)
            {
                throw new ArgumentException(SR.Argument_StructMustNotBeValueClass, nameof(structure));
            }

            PtrToStructureImpl(ptr, structure);
        }

        internal static unsafe void PtrToStructureImpl(IntPtr ptr, object structure)
        {
            RuntimeTypeHandle structureTypeHandle = structure.GetType().TypeHandle;

            IntPtr unmarshalStub;
            if (structureTypeHandle.IsBlittable())
            {
                if (!RuntimeInteropData.TryGetStructUnmarshalStub(structureTypeHandle, out unmarshalStub))
                {
                    unmarshalStub = IntPtr.Zero;
                }
            }
            else
            {
                unmarshalStub = RuntimeInteropData.GetStructUnmarshalStub(structureTypeHandle);
            }

            if (unmarshalStub != IntPtr.Zero)
            {
                if (structureTypeHandle.IsValueType())
                {
                    ((delegate*<ref byte, ref byte, void>)unmarshalStub)(ref *(byte*)ptr, ref structure.GetRawData());
                }
                else
                {
                    ((delegate*<ref byte, object, void>)unmarshalStub)(ref *(byte*)ptr, structure);
                }
            }
            else
            {
                nuint size = (nuint)RuntimeInteropData.GetStructUnsafeStructSize(structureTypeHandle);

                Buffer.Memmove(ref structure.GetRawData(), ref *(byte*)ptr, size);
            }
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available. Use the DestroyStructure<T> overload instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static unsafe void DestroyStructure(IntPtr ptr, Type structuretype)
        {
            ArgumentNullException.ThrowIfNull(ptr);
            ArgumentNullException.ThrowIfNull(structuretype, "structureType");

            RuntimeTypeHandle structureTypeHandle = structuretype.TypeHandle;

            if (structureTypeHandle.IsGenericType() || structureTypeHandle.IsGenericTypeDefinition())
                throw new ArgumentException(SR.Argument_NeedNonGenericType, "structure");

            if (structureTypeHandle.IsEnum() ||
                structureTypeHandle.IsInterface() ||
                InteropExtensions.AreTypesAssignable(typeof(Delegate).TypeHandle, structureTypeHandle))
            {
                throw new ArgumentException(SR.Format(SR.Argument_MustHaveLayoutOrBeBlittable, structuretype));
            }

            if (structureTypeHandle.IsBlittable())
            {
                // ok to call with blittable structure, but no work to do in this case.
                return;
            }

            IntPtr destroyStructureStub = RuntimeInteropData.GetDestroyStructureStub(structureTypeHandle, out bool hasInvalidLayout);
            if (hasInvalidLayout)
                throw new ArgumentException(SR.Format(SR.Argument_MustHaveLayoutOrBeBlittable, structuretype));
            // DestroyStructureStub == IntPtr.Zero means its fields don't need to be destroyed
            if (destroyStructureStub != IntPtr.Zero)
            {
                ((delegate*<ref byte, void>)destroyStructureStub)(ref *(byte*)ptr);
            }
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available. Use the StructureToPtr<T> overload instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static unsafe void StructureToPtr(object structure, IntPtr ptr, bool fDeleteOld)
        {
            ArgumentNullException.ThrowIfNull(structure);
            ArgumentNullException.ThrowIfNull(ptr);

            if (fDeleteOld)
            {
                DestroyStructure(ptr, structure.GetType());
            }

            RuntimeTypeHandle structureTypeHandle = structure.GetType().TypeHandle;

            if (structureTypeHandle.IsGenericType() || structureTypeHandle.IsGenericTypeDefinition())
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericObject, nameof(structure));
            }

            IntPtr marshalStub;
            if (structureTypeHandle.IsBlittable())
            {
                if (!RuntimeInteropData.TryGetStructMarshalStub(structureTypeHandle, out marshalStub))
                {
                    marshalStub = IntPtr.Zero;
                }
            }
            else
            {
                marshalStub = RuntimeInteropData.GetStructMarshalStub(structureTypeHandle);
            }

            if (marshalStub != IntPtr.Zero)
            {
                if (structureTypeHandle.IsValueType())
                {
                    ((delegate*<ref byte, ref byte, void>)marshalStub)(ref structure.GetRawData(), ref *(byte*)ptr);
                }
                else
                {
                    ((delegate*<object, ref byte, void>)marshalStub)(structure, ref *(byte*)ptr);
                }
            }
            else
            {
                nuint size = (nuint)RuntimeInteropData.GetStructUnsafeStructSize(structureTypeHandle);

                Buffer.Memmove(ref *(byte*)ptr, ref structure.GetRawData(), size);
            }
        }

        // This method is effectively a no-op for NativeAOT, everything pre-generated.
        static partial void PrelinkCore(MethodInfo m);

        internal static Delegate GetDelegateForFunctionPointerInternal(IntPtr ptr, Type t)
        {
            return PInvokeMarshal.GetDelegateForFunctionPointer(ptr, t.TypeHandle);
        }

        internal static IntPtr GetFunctionPointerForDelegateInternal(Delegate d)
        {
            return PInvokeMarshal.GetFunctionPointerForDelegate(d);
        }

        public static int GetLastPInvokeError()
        {
            return PInvokeMarshal.t_lastError;
        }

        public static void SetLastPInvokeError(int error)
        {
            PInvokeMarshal.t_lastError = error;
        }

        internal static bool IsPinnable(object o)
        {
            return (o == null) || !o.GetEETypePtr().ContainsGCPointers;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("GetExceptionCode() may be unavailable in future releases.")]
        public static int GetExceptionCode()
        {
            // Obsolete
            throw new PlatformNotSupportedException();
        }

        public static IntPtr GetExceptionPointers()
        {
            throw new PlatformNotSupportedException();
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadByte(Object, Int32) may be unavailable in future releases.")]
        public static unsafe byte ReadByte(object ptr, int ofs)
        {
            return ReadValueSlow<byte>(ptr, ofs, &ReadByte);
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadInt16(Object, Int32) may be unavailable in future releases.")]
        public static unsafe short ReadInt16(object ptr, int ofs)
        {
            return ReadValueSlow<short>(ptr, ofs, &ReadInt16);
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadInt32(Object, Int32) may be unavailable in future releases.")]
        public static unsafe int ReadInt32(object ptr, int ofs)
        {
            return ReadValueSlow<int>(ptr, ofs, &ReadInt32);
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadInt64(Object, Int32) may be unavailable in future releases.")]
        public static unsafe long ReadInt64(object ptr, int ofs)
        {
            return ReadValueSlow<long>(ptr, ofs, &ReadInt64);
        }

        //====================================================================
        // Read value from marshaled object (marshaled using AsAny)
        // It's quite slow and can return back dangling pointers
        // It's only there for backcompact
        // People should instead use the IntPtr overloads
        //====================================================================
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        private static unsafe T ReadValueSlow<T>(object ptr, int ofs, delegate*<IntPtr, int, T> readValueHelper)
        {
            // Consumers of this method are documented to throw AccessViolationException on any AV
            if (ptr is null)
            {
                throw new AccessViolationException();
            }

            if (ptr.GetEETypePtr().IsArray ||
                ptr is string ||
                ptr is StringBuilder)
            {
                // We could implement these if really needed.
                throw new PlatformNotSupportedException();
            }

            // We are going to assume this is a Sequential or Explicit layout type because
            // we don't want to touch reflection metadata for this.
            // If we're wrong, this will throw the exception we get for missing interop data
            // instead of an ArgumentException.
            // That's quite acceptable for an obsoleted API.

            Type structType = ptr.GetType();

            int size = SizeOf(structType);

            // Compat note: CLR wouldn't bother with a range check. If someone does this,
            // they're likely taking dependency on some CLR implementation detail quirk.
#pragma warning disable 8500 // sizeof of managed types
            ArgumentOutOfRangeException.ThrowIfGreaterThan(checked(ofs + sizeof(T)), size, nameof(ofs));
#pragma warning restore 8500

            IntPtr nativeBytes = AllocCoTaskMem(size);
            NativeMemory.Clear((void*)nativeBytes, (nuint)size);

            try
            {
                StructureToPtr(ptr, nativeBytes, false);
                return readValueHelper(nativeBytes, ofs);
            }
            finally
            {
                DestroyStructure(nativeBytes, structType);
                FreeCoTaskMem(nativeBytes);
            }
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteByte(Object, Int32, Byte) may be unavailable in future releases.")]
        public static unsafe void WriteByte(object ptr, int ofs, byte val)
        {
            WriteValueSlow(ptr, ofs, val, &WriteByte);
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt16(Object, Int32, Int16) may be unavailable in future releases.")]
        public static unsafe void WriteInt16(object ptr, int ofs, short val)
        {
            WriteValueSlow(ptr, ofs, val, &WriteInt16);
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt32(Object, Int32, Int32) may be unavailable in future releases.")]
        public static unsafe void WriteInt32(object ptr, int ofs, int val)
        {
            WriteValueSlow(ptr, ofs, val, &WriteInt32);
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt64(Object, Int32, Int64) may be unavailable in future releases.")]
        public static unsafe void WriteInt64(object ptr, int ofs, long val)
        {
            WriteValueSlow(ptr, ofs, val, &WriteInt64);
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        private static unsafe void WriteValueSlow<T>(object ptr, int ofs, T val, delegate*<IntPtr, int, T, void> writeValueHelper)
        {
            // Consumers of this method are documented to throw AccessViolationException on any AV
            if (ptr is null)
            {
                throw new AccessViolationException();
            }

            if (ptr.GetEETypePtr().IsArray ||
                ptr is string ||
                ptr is StringBuilder)
            {
                // We could implement these if really needed.
                throw new PlatformNotSupportedException();
            }

            // We are going to assume this is a Sequential or Explicit layout type because
            // we don't want to touch reflection metadata for this.
            // If we're wrong, this will throw the exception we get for missing interop data
            // instead of an ArgumentException.
            // That's quite acceptable for an obsoleted API.

            Type structType = ptr.GetType();

            int size = SizeOf(structType);

            // Compat note: CLR wouldn't bother with a range check. If someone does this,
            // they're likely taking dependency on some CLR implementation detail quirk.
#pragma warning disable 8500 // sizeof of managed types
            ArgumentOutOfRangeException.ThrowIfGreaterThan(checked(ofs + sizeof(T)), size, nameof(ofs));
#pragma warning restore 8500

            IntPtr nativeBytes = AllocCoTaskMem(size);
            NativeMemory.Clear((void*)nativeBytes, (nuint)size);

            try
            {
                StructureToPtr(ptr, nativeBytes, false);
                writeValueHelper(nativeBytes, ofs, val);
                PtrToStructureImpl(nativeBytes, ptr);
            }
            finally
            {
                DestroyStructure(nativeBytes, structType);
                FreeCoTaskMem(nativeBytes);
            }
        }
    }
}
