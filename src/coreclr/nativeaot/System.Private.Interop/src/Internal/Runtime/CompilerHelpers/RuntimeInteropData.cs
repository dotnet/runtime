// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Internal.Runtime.Augments;
using Internal.NativeFormat;
using Internal.Runtime.TypeLoader;
using Internal.Reflection.Execution;
using System.Runtime.InteropServices;

namespace Internal.Runtime.CompilerHelpers
{
    internal partial class RuntimeInteropData : InteropCallbacks
    {
        public override uint GetStructFieldOffset(RuntimeTypeHandle structureTypeHandle, string fieldName)
        {
            if (TryGetStructFieldOffset(structureTypeHandle, fieldName, out bool structExists, out uint offset))
            {
                return offset;
            }

            // if we can find the struct but couldn't find its field, throw Argument Exception
            if (structExists)
            {
                throw new ArgumentException(SR.Format(SR.Argument_OffsetOfFieldNotFound, RuntimeAugments.GetLastResortString(structureTypeHandle)), nameof(fieldName));
            }

            throw new MissingInteropDataException(SR.StructMarshalling_MissingInteropData, Type.GetTypeFromHandle(structureTypeHandle));
        }

        public override int GetStructUnsafeStructSize(RuntimeTypeHandle structureTypeHandle)
        {
            if (TryGetStructUnsafeStructSize(structureTypeHandle, out int size))
            {
                return size;
            }

            // IsBlittable() checks whether the type contains GC references. It is approximate check with false positives.
            // This fallback path will return incorrect answer for types that do not contain GC references, but that are
            // not actually blittable; e.g. for types with bool fields.
            if (structureTypeHandle.IsBlittable() && structureTypeHandle.IsValueType())
            {
                return structureTypeHandle.GetValueTypeSize();
            }

            throw new MissingInteropDataException(SR.StructMarshalling_MissingInteropData, Type.GetTypeFromHandle(structureTypeHandle));
        }

        public override IntPtr GetStructUnmarshalStub(RuntimeTypeHandle structureTypeHandle)
        {
            if (TryGetStructUnmarshalStub(structureTypeHandle, out IntPtr stub))
            {
                return stub;
            }

            throw new MissingInteropDataException(SR.StructMarshalling_MissingInteropData, Type.GetTypeFromHandle(structureTypeHandle));
        }

        public override IntPtr GetStructMarshalStub(RuntimeTypeHandle structureTypeHandle)
        {
            if (TryGetStructMarshalStub(structureTypeHandle, out IntPtr stub))
            {
                return stub;
            }

            throw new MissingInteropDataException(SR.StructMarshalling_MissingInteropData, Type.GetTypeFromHandle(structureTypeHandle));
        }

        public override IntPtr GetDestroyStructureStub(RuntimeTypeHandle structureTypeHandle, out bool hasInvalidLayout)
        {
            if (TryGetDestroyStructureStub(structureTypeHandle, out IntPtr stub, out hasInvalidLayout))
            {
                return stub;
            }

            throw new MissingInteropDataException(SR.StructMarshalling_MissingInteropData, Type.GetTypeFromHandle(structureTypeHandle));
        }
    }
}
