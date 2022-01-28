// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Reflection.Runtime.General;

using Internal.TypeSystem;
using Internal.Runtime.Augments;
using Internal.TypeSystem.NativeFormat;
using Internal.NativeFormat;
using System.Runtime.CompilerServices;

namespace Internal.Runtime.TypeLoader
{
    internal class SerializedDebugData
    {
        //
        // SerializedDebugData represents the logical buffer where all debug information is serialized.
        //
        // DBGVISIBLE_serializedDataHeader is the blob of memory that describes the contents of
        // the logical buffer. This blob is allocated on the unmanaged heap using MemoryHelpers.AllocateMemory
        //
        // byte[0-3] in DBGVISIBLE_serializedDataHeader represents the version of serialization format.
        // In our current format, we use multiple physical memory buffers (on unmanaged heap) to hold
        // the actual serialized data and DBGVISIBLE_serializedDataHeader contains a list of pointers
        // to these physical buffers.
        // byte[4-7] in DBGVISIBLE_serializedDataHeader is the number of currently allocated physical
        // buffers.
        // byte[8-...] is a list of pointers to these physical buffers.
        //
        // Note that type-loader must ensure that all debug data serialization is done in a thread-safe manner.
        //

        /// <summary>
        /// Types of records in the serialized debug data. To maintain compatibility with previous
        /// version of the diagnostic stream which used just the two bottom bits for entry type
        /// information, we cannibalize entry #3, StepThroughStubAddress, which has only one bit flag
        /// (IsTailCallStub shared with StepThroughStubSize), to encode additional entry types
        /// in the higher-order bits. When the bits 0-1 contain 1-1 (i.e. the 'old-style' entry type
        /// is StepThroughStubAddress) and bits 3-7 are non-zero, they get split such that
        /// bits 3-4 are shifted right 3 times and increased by 3 to form the final blob kind,
        /// bits 5-7 are shifted right 5 times to form the flags for the new blob kinds.
        /// This creates space for 3 more entry types with 3 bits for flags which should hopefully suffice.
        /// </summary>
        internal enum SerializedDataBlobKind : byte
        {
            SharedType = 0,
            SharedMethod = 1,
            StepThroughStubSize = 2,
            StepThroughStubAddress = 3,
            NativeFormatType = 4,

            Limit,
        };

        [Flags]
        internal enum SharedTypeFlags : byte
        {
            HasGCStaticFieldRegion = 0x01,
            HasNonGCStaticFieldRegion = 0x02,
            HasThreadStaticFieldRegion = 0x04,
            HasStaticFields = 0x08,
            HasInstanceFields = 0x10,
            HasTypeSize = 0x20
        };

        [Flags]
        internal enum SharedMethodFlags : byte
        {
            HasDeclaringTypeHandle = 0x01
        };

        [Flags]
        internal enum StepThroughStubFlags : byte
        {
            IsTailCallStub = 0x01
        };

        private static IntPtr DBGVISIBLE_serializedDataHeader;

        // version of the serialization format
        private const int SerializationFormatVersion = 2;

        // size by which the list of pointers to physical buffers is grown
        private const int HeaderBufferListSize = 100;

        // offset in DBGVISIBLE_serializedDataHeader where the list of pointers to physical buffers starts
        private const int HeaderBufferListOffset = sizeof(int) * 2;

        // size of each physical buffer
        private const int PhysicalBufferSize = 10 * 1024; // 10 KB

        // offset in physical buffer where the actual data blobs start
        private const int PhysicalBufferDataOffset = sizeof(int);

        // the instance of SerializedDebugData via which runtime updates the debug data buffer
        internal static SerializedDebugData Instance = new SerializedDebugData();

        private IntPtr _activePhysicalBuffer;
        private int _activePhysicalBufferIdx = -1;
        private int _activePhysicalBufferOffset;
        private int _activePhysicalBufferAvailableSize;
        private int _serializedDataHeaderSize;

        private unsafe void InitializeHeader(int physicalBufferListSize)
        {
            int headerSize = HeaderBufferListOffset + IntPtr.Size * physicalBufferListSize;
            IntPtr header = MemoryHelpers.AllocateMemory(headerSize);
            IntPtr oldHeader = IntPtr.Zero;
            MemoryHelpers.Memset(header, headerSize, 0);

            // check if an older header exists and copy all data from it to the newly
            // allocated header.
            if (_serializedDataHeaderSize > 0)
            {
                Debug.Assert(headerSize > _serializedDataHeaderSize);

                Buffer.MemoryCopy((byte*)DBGVISIBLE_serializedDataHeader, (byte*)header, headerSize, _serializedDataHeaderSize);

                // mark the older header for deletion
                oldHeader = DBGVISIBLE_serializedDataHeader;
            }
            else
            {
                // write the serialization format version
                *(int*)header = SerializationFormatVersion;
                // write the total allocated number of physical buffers (0)
                *(int*)(header + sizeof(int)) = 0;
                Debug.Assert(_activePhysicalBufferIdx == 0);
            }

            DBGVISIBLE_serializedDataHeader = header;
            _serializedDataHeaderSize = headerSize;

            // delete the older header if a new one was allocated
            if (oldHeader != IntPtr.Zero)
            {
                MemoryHelpers.FreeMemory(oldHeader);
            }
        }
        private unsafe int GetAllocatedPhysicalBufferCount()
        {
            if (_serializedDataHeaderSize < HeaderBufferListOffset)
                return 0;

            return *(int*)(DBGVISIBLE_serializedDataHeader + sizeof(int));
        }
        private unsafe void AddAllocatedBufferToHeader(IntPtr buffer, int insertIdx)
        {
            Debug.Assert(insertIdx >= 0);

            int currentPhysicalBufferListSize = _serializedDataHeaderSize == 0 ? 0 :
                (_serializedDataHeaderSize - HeaderBufferListOffset) / IntPtr.Size;
            if (currentPhysicalBufferListSize <= insertIdx)
            {
                // not enough space in the header, grow it
                InitializeHeader(currentPhysicalBufferListSize + HeaderBufferListSize);
            }

            Debug.Assert(GetAllocatedPhysicalBufferCount() == insertIdx);

            *(void**)(DBGVISIBLE_serializedDataHeader + HeaderBufferListOffset + IntPtr.Size * insertIdx) = buffer.ToPointer();
            *(int*)(DBGVISIBLE_serializedDataHeader + sizeof(int)) = insertIdx + 1; // update the buffer count
        }

        //
        // Allocates a new physical buffer and returns the first offset where data can be written into buffer
        // First few bytes of the physical buffer are used to describe it.
        //
        // buffer[0-3] = Used buffer size
        //
        private unsafe int AllocatePhysicalBuffer(out IntPtr buffer)
        {
            // Allocate a new physical buffer.
            IntPtr newPhysicalBuffer = MemoryHelpers.AllocateMemory(PhysicalBufferSize);
            *(int*)newPhysicalBuffer = 0; // write the used buffer size, currently 0

            // Add the pointer to new physical buffer to DBGVISIBLE_serializedDataHeader
            AddAllocatedBufferToHeader(newPhysicalBuffer, ++_activePhysicalBufferIdx);

            buffer = newPhysicalBuffer;
            return PhysicalBufferDataOffset;
        }

        //
        // GetPhysicalBuffer returns a physical buffer of a given size.
        //
        // Given a requested buffer size, this method gives back a buffer pointer
        // and available usable size.
        // It is the caller's responsibility to update the used buffer size after writing
        // to the buffer.
        //
        private int GetPhysicalBuffer(int requestedSize, out IntPtr bufferPtr)
        {
            if (_activePhysicalBufferAvailableSize == 0)
            {
                // no space available in active physical buffer
                // allocate a new physical buffer
                _activePhysicalBufferOffset = AllocatePhysicalBuffer(out _activePhysicalBuffer);
                _activePhysicalBufferAvailableSize = PhysicalBufferSize - _activePhysicalBufferOffset;
            }

            int availableSize = (_activePhysicalBufferAvailableSize < requestedSize) ?
                _activePhysicalBufferAvailableSize : requestedSize;

            _activePhysicalBufferAvailableSize -= availableSize;
            bufferPtr = new IntPtr(_activePhysicalBuffer.ToInt64() + _activePhysicalBufferOffset);
            _activePhysicalBufferOffset += availableSize;
            return availableSize;
        }

        // Helper used to update the used buffer size in buffer[0-3]
        private unsafe void UpdatePhysicalBufferUsedSize()
        {
            Debug.Assert(_activePhysicalBufferOffset >= PhysicalBufferDataOffset);
            *(int*)_activePhysicalBuffer = _activePhysicalBufferOffset;
        }

        // Write the given byte array to the logical buffer in a thread-safe manner
        private unsafe void ThreadSafeWriteBytes(byte[] src)
        {
            lock (Instance)
            {
                IntPtr dst;
                int requiredSize = src.Length;
                int availableSize = GetPhysicalBuffer(requiredSize, out dst);
                if (availableSize < requiredSize)
                {
                    // if current physical buffer doesn't have enough space, try
                    // and allocate a new one
                    availableSize = GetPhysicalBuffer(requiredSize, out dst);
                    if (availableSize < requiredSize)
                        throw new OutOfMemoryException();
                }
                src.AsSpan().CopyTo(new Span<byte>((void*)dst, src.Length));
                UpdatePhysicalBufferUsedSize();    // make sure that used physical buffer size is updated
            }
        }

        // Helper method to serialize the data-blob type and flags
        public static void SerializeDataBlobTypeAndFlags(ref NativePrimitiveEncoder encoder, SerializedDataBlobKind blobType, byte flags)
        {
            // make sure that blobType fits in 2 bits and flags fits in 6 bits
            Debug.Assert(blobType < SerializedDataBlobKind.Limit);
            Debug.Assert((byte)blobType <= 2 && flags <= 0x3F ||
                (byte)blobType == 3 && flags <= 1 ||
                (byte)blobType > 3 && flags <= 7);
            byte encodedKindAndFlags;
            if (blobType <= (SerializedDataBlobKind)3)
            {
                encodedKindAndFlags = (byte)((byte)blobType | (flags << 2));
            }
            else
            {
                encodedKindAndFlags = (byte)(3 | (((byte)blobType - 3) << 3) | (flags << 5));
            }
            encoder.WriteByte(encodedKindAndFlags);
        }

        public static void RegisterDebugDataForType(TypeBuilder typeBuilder, DefType defType, TypeBuilderState state)
        {
            if (!defType.IsGeneric())
            {
                RegisterDebugDataForNativeFormatType(typeBuilder, defType, state);
                return;
            }

            if (defType.IsGenericDefinition)
            {
                // We don't yet have an encoding for open generic types
                // TODO! fill this in
                return;
            }

            NativePrimitiveEncoder encoder = new NativePrimitiveEncoder();
            encoder.Init();

            IntPtr gcStaticFieldData = TypeLoaderEnvironment.Instance.TryGetGcStaticFieldData(typeBuilder.GetRuntimeTypeHandle(defType));
            IntPtr nonGcStaticFieldData = TypeLoaderEnvironment.Instance.TryGetNonGcStaticFieldData(typeBuilder.GetRuntimeTypeHandle(defType));

            bool isUniversalGenericType = state.TemplateType != null && state.TemplateType.IsCanonicalSubtype(CanonicalFormKind.Universal);
            bool embeddedTypeSizeAndFieldOffsets = isUniversalGenericType || (state.TemplateType == null);
            uint instanceFieldCount = 0;
            uint staticFieldCount = 0;

            // GetDiagnosticFields only returns the fields that are of interest for diagnostic reporting. So it doesn't
            // return a meaningful list for non-universal canonical templates
            IEnumerable<FieldDesc> diagnosticFields = defType.GetDiagnosticFields();
            foreach (var f in diagnosticFields)
            {
                if (f.IsLiteral)
                    continue;

                if (f.IsStatic)
                {
                    ++staticFieldCount;
                }
                else
                {
                    ++instanceFieldCount;
                }
            }

            SharedTypeFlags sharedTypeFlags = 0;
            if (gcStaticFieldData != IntPtr.Zero) sharedTypeFlags |= SharedTypeFlags.HasGCStaticFieldRegion;
            if (nonGcStaticFieldData != IntPtr.Zero) sharedTypeFlags |= SharedTypeFlags.HasNonGCStaticFieldRegion;
            if (state.ThreadDataSize != 0) sharedTypeFlags |= SharedTypeFlags.HasThreadStaticFieldRegion;
            if (embeddedTypeSizeAndFieldOffsets)
            {
                sharedTypeFlags |= SerializedDebugData.SharedTypeFlags.HasTypeSize;

                if (instanceFieldCount > 0)
                    sharedTypeFlags |= SerializedDebugData.SharedTypeFlags.HasInstanceFields;

                if (staticFieldCount > 0)
                    sharedTypeFlags |= SerializedDebugData.SharedTypeFlags.HasStaticFields;
            }

            SerializeDataBlobTypeAndFlags(ref encoder, SerializedDataBlobKind.SharedType, (byte)sharedTypeFlags);

            //
            // The order of these writes is a contract shared between the runtime and debugger engine.
            // Changes here must also be updated in the debugger reader code
            //
            encoder.WriteUnsignedLong((ulong)typeBuilder.GetRuntimeTypeHandle(defType).ToIntPtr().ToInt64());
            encoder.WriteUnsigned((uint)defType.Instantiation.Length);

            foreach (var instParam in defType.Instantiation)
            {
                encoder.WriteUnsignedLong((ulong)typeBuilder.GetRuntimeTypeHandle(instParam).ToIntPtr().ToInt64());
            }

            if (gcStaticFieldData != IntPtr.Zero)
            {
                encoder.WriteUnsignedLong((ulong)gcStaticFieldData.ToInt64());
            }

            if (nonGcStaticFieldData != IntPtr.Zero)
            {
                encoder.WriteUnsignedLong((ulong)nonGcStaticFieldData.ToInt64());
            }

            // Write the TLS offset into the native thread's TLS buffer. That index de-referenced is the thread static
            // data region for this type
            if (state.ThreadDataSize != 0)
            {
                encoder.WriteUnsigned(state.ThreadStaticOffset);
            }

            // Collect information debugger only requires for universal generics and dynamically loaded types
            if (embeddedTypeSizeAndFieldOffsets)
            {
                Debug.Assert(state.TypeSize != null);
                encoder.WriteUnsigned((uint)state.TypeSize);

                if (instanceFieldCount > 0)
                {
                    encoder.WriteUnsigned(instanceFieldCount);

                    uint i = 0;
                    foreach (FieldDesc f in diagnosticFields)
                    {
                        if (f.IsLiteral)
                            continue;
                        if (f.IsStatic)
                            continue;

                        encoder.WriteUnsigned(i);
                        encoder.WriteUnsigned((uint)f.Offset.AsInt);
                        i++;
                    }
                }

                if (staticFieldCount > 0)
                {
                    encoder.WriteUnsigned(staticFieldCount);

                    uint i = 0;
                    foreach (FieldDesc f in diagnosticFields)
                    {
                        if (f.IsLiteral)
                            continue;
                        if (!f.IsStatic)
                            continue;

                        NativeLayoutFieldDesc nlfd = f as NativeLayoutFieldDesc;
                        FieldStorage fieldStorage;
                        if (nlfd != null)
                        {
                            // NativeLayoutFieldDesc's have the field storage information directly embedded in them
                            fieldStorage = nlfd.FieldStorage;
                        }
                        else
                        {
                            // Metadata based types do not, but the api's to get the info are available
                            if (f.IsThreadStatic)
                            {
                                fieldStorage = FieldStorage.TLSStatic;
                            }
                            else if (f.HasGCStaticBase)
                            {
                                fieldStorage = FieldStorage.GCStatic;
                            }
                            else
                            {
                                fieldStorage = FieldStorage.NonGCStatic;
                            }
                        }

                        encoder.WriteUnsigned(i);
                        encoder.WriteUnsigned((uint)fieldStorage);
                        encoder.WriteUnsigned((uint)f.Offset.AsInt);
                        i++;
                    }
                }
            }

            Instance.ThreadSafeWriteBytes(encoder.GetBytes());
        }

        /// <summary>
        /// Add information about dynamically created non-generic native format type
        /// to the diagnostic stream in form of a NativeFormatType blob.
        /// </summary>
        /// <param name="typeBuilder">TypeBuilder is used to query runtime type handle for the type</param>
        /// <param name="defType">Type to emit to the diagnostic stream</param>
        /// <param name="state"></param>
        public static void RegisterDebugDataForNativeFormatType(TypeBuilder typeBuilder, DefType defType, TypeBuilderState state)
        {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            NativeFormatType nativeFormatType = defType as NativeFormatType;
            if (nativeFormatType == null)
            {
                return;
            }

            NativePrimitiveEncoder encoder = new NativePrimitiveEncoder();
            encoder.Init();

            byte nativeFormatTypeFlags = 0;

            SerializeDataBlobTypeAndFlags(
                ref encoder,
                SerializedDataBlobKind.NativeFormatType,
                nativeFormatTypeFlags);

            TypeManagerHandle moduleHandle = ModuleList.Instance.GetModuleForMetadataReader(nativeFormatType.MetadataReader);

            encoder.WriteUnsignedLong(unchecked((ulong)typeBuilder.GetRuntimeTypeHandle(defType).ToIntPtr().ToInt64()));
            encoder.WriteUnsigned(nativeFormatType.Handle.ToHandle(nativeFormatType.MetadataReader).AsUInt());
            encoder.WriteUnsignedLong(unchecked((ulong)moduleHandle.GetIntPtrUNSAFE().ToInt64()));

            Instance.ThreadSafeWriteBytes(encoder.GetBytes());
#else
            return;
#endif
        }

        public static void RegisterDebugDataForMethod(TypeBuilder typeBuilder, InstantiatedMethod method)
        {
            NativePrimitiveEncoder encoder = new NativePrimitiveEncoder();
            encoder.Init();

            byte sharedMethodFlags = 0;
            sharedMethodFlags |= (byte)(method.OwningType.IsGeneric() ? SharedMethodFlags.HasDeclaringTypeHandle : 0);

            SerializeDataBlobTypeAndFlags(ref encoder, SerializedDataBlobKind.SharedMethod, sharedMethodFlags);
            encoder.WriteUnsignedLong((ulong)method.RuntimeMethodDictionary.ToInt64());
            encoder.WriteUnsigned((uint)method.Instantiation.Length);

            foreach (var instParam in method.Instantiation)
            {
                encoder.WriteUnsignedLong((ulong)typeBuilder.GetRuntimeTypeHandle(instParam).ToIntPtr().ToInt64());
            }

            if (method.OwningType.IsGeneric())
            {
                encoder.WriteUnsignedLong((ulong)typeBuilder.GetRuntimeTypeHandle(method.OwningType).ToIntPtr().ToInt64());
            }

            Instance.ThreadSafeWriteBytes(encoder.GetBytes());
        }

        // This method is called whenever a new thunk is allocated, to capture the thunk's code address
        // in the serialized stream.
        // This information is used by the debugger to detect thunk frames on the callstack.
        private static bool s_tailCallThunkSizeRegistered;
        public static void RegisterTailCallThunk(IntPtr thunk)
        {
            NativePrimitiveEncoder encoder = new NativePrimitiveEncoder();

            if (!s_tailCallThunkSizeRegistered)
            {
                lock (Instance)
                {
                    if (!s_tailCallThunkSizeRegistered)
                    {
                        // Write out the size of thunks used by the calling convention converter
                        // Make sure that this is called only once
                        encoder.Init();
                        SerializeDataBlobTypeAndFlags(ref encoder,
                            SerializedDataBlobKind.StepThroughStubSize,
                            (byte)StepThroughStubFlags.IsTailCallStub);
                        encoder.WriteUnsigned((uint)RuntimeAugments.GetThunkSize());
                        Instance.ThreadSafeWriteBytes(encoder.GetBytes());
                        s_tailCallThunkSizeRegistered = true;
                    }
                }
            }

            encoder.Init();
            SerializeDataBlobTypeAndFlags(ref encoder,
                SerializedDataBlobKind.StepThroughStubAddress,
                (byte)StepThroughStubFlags.IsTailCallStub);
            encoder.WriteUnsignedLong((ulong)thunk.ToInt64());
            Instance.ThreadSafeWriteBytes(encoder.GetBytes());
        }
    }
}
