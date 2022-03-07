// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal.Runtime.Augments
{
    [CLSCompliant(false)]
    [System.Runtime.CompilerServices.ReflectionBlocked]
    public abstract class InteropCallbacks
    {
        public abstract IntPtr GetForwardDelegateCreationStub(RuntimeTypeHandle delegateTypeHandle);

        public abstract IntPtr GetDelegateMarshallingStub(RuntimeTypeHandle delegateTypeHandle, bool openStaticDelegate);

        public abstract bool TryGetStructUnmarshalStub(RuntimeTypeHandle structureTypeHandle, out IntPtr unmarshalStub);

        public abstract IntPtr GetStructUnmarshalStub(RuntimeTypeHandle structureTypeHandle);

        public abstract bool TryGetStructMarshalStub(RuntimeTypeHandle structureTypeHandle, out IntPtr marshalStub);

        public abstract IntPtr GetStructMarshalStub(RuntimeTypeHandle structureTypeHandle);

        public abstract bool TryGetDestroyStructureStub(RuntimeTypeHandle structureTypeHandle, out IntPtr destroyStructureStub, out bool hasInvalidLayout);

        public abstract IntPtr GetDestroyStructureStub(RuntimeTypeHandle structureTypeHandle, out bool hasInvalidLayout);

        public abstract bool TryGetStructFieldOffset(RuntimeTypeHandle structureTypeHandle, string fieldName, out bool structExists, out uint offset);

        public abstract uint GetStructFieldOffset(RuntimeTypeHandle structureTypeHandle, string fieldName);

        public abstract bool TryGetStructUnsafeStructSize(RuntimeTypeHandle structureTypeHandle, out int size);

        public abstract int GetStructUnsafeStructSize(RuntimeTypeHandle structureTypeHandle);
    }
}
