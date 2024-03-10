// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Runtime.General;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.TypeLoader;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using ReflectionExecution = Internal.Reflection.Execution.ReflectionExecution;

namespace Internal.StackTraceMetadata
{
    /// <summary>
    /// This helper class is used to resolve non-reflectable method names using a special
    /// compiler-generated metadata blob to enhance quality of exception call stacks
    /// in situations where symbol information is not available.
    /// </summary>
    internal static class StackTraceMetadata
    {
        /// <summary>
        /// Module address-keyed map of per-module method name resolvers.
        /// </summary>
        private static PerModuleMethodNameResolverHashtable _perModuleMethodNameResolverHashtable;

        /// <summary>
        /// Eager startup initialization of stack trace metadata support creates
        /// the per-module method name resolver hashtable and registers the runtime augment
        /// for metadata-based stack trace resolution.
        /// </summary>
        internal static void Initialize()
        {
            _perModuleMethodNameResolverHashtable = new PerModuleMethodNameResolverHashtable();
            RuntimeAugments.InitializeStackTraceMetadataSupport(new StackTraceMetadataCallbacksImpl());
        }

        /// <summary>
        /// Locate the containing module for a method and try to resolve its name based on start address.
        /// </summary>
        public static unsafe string GetMethodNameFromStartAddressIfAvailable(IntPtr methodStartAddress, out bool isStackTraceHidden)
        {
            IntPtr moduleStartAddress = RuntimeAugments.GetOSModuleFromPointer(methodStartAddress);
            int rva = (int)((byte*)methodStartAddress - (byte*)moduleStartAddress);
            foreach (NativeFormatModuleInfo moduleInfo in ModuleList.EnumerateModules())
            {
                if (moduleInfo.Handle.OsModuleBase == moduleStartAddress)
                {
                    string name = _perModuleMethodNameResolverHashtable.GetOrCreateValue(moduleInfo.Handle.GetIntPtrUNSAFE()).GetMethodNameFromRvaIfAvailable(rva, out isStackTraceHidden);
                    if (name != null || isStackTraceHidden)
                        return name;
                }
            }

            isStackTraceHidden = false;

            // We haven't found information in the stack trace metadata tables, but maybe reflection will have this
            if (IsReflectionExecutionAvailable() && ReflectionExecution.TryGetMethodMetadataFromStartAddress(methodStartAddress,
                out MetadataReader reader,
                out TypeDefinitionHandle typeHandle,
                out MethodHandle methodHandle))
            {
                foreach (CustomAttributeHandle cah in reader.GetTypeDefinition(typeHandle).CustomAttributes)
                {
                    if (cah.IsCustomAttributeOfType(reader, ["System", "Diagnostics"], "StackTraceHiddenAttribute"))
                    {
                        isStackTraceHidden = true;
                        break;
                    }
                }

                foreach (CustomAttributeHandle cah in reader.GetMethod(methodHandle).CustomAttributes)
                {
                    if (cah.IsCustomAttributeOfType(reader, ["System", "Diagnostics"], "StackTraceHiddenAttribute"))
                    {
                        isStackTraceHidden = true;
                        break;
                    }
                }

                return MethodNameFormatter.FormatMethodName(reader, typeHandle, methodHandle);
            }

            return null;
        }

        // Can be rewritten to false through a feature switch.
        private static bool IsReflectionExecutionAvailable() => true;

        /// <summary>
        /// This hashtable supports mapping from module start addresses to per-module method name resolvers.
        /// </summary>
        private sealed class PerModuleMethodNameResolverHashtable : LockFreeReaderHashtable<IntPtr, PerModuleMethodNameResolver>
        {
            /// <summary>
            /// Given a key, compute a hash code. This function must be thread safe.
            /// </summary>
            protected override int GetKeyHashCode(IntPtr key)
            {
                return key.GetHashCode();
            }

            /// <summary>
            /// Given a value, compute a hash code which would be identical to the hash code
            /// for a key which should look up this value. This function must be thread safe.
            /// This function must also not cause additional hashtable adds.
            /// </summary>
            protected override int GetValueHashCode(PerModuleMethodNameResolver value)
            {
                return GetKeyHashCode(value.ModuleAddress);
            }

            /// <summary>
            /// Compare a key and value. If the key refers to this value, return true.
            /// This function must be thread safe.
            /// </summary>
            protected override bool CompareKeyToValue(IntPtr key, PerModuleMethodNameResolver value)
            {
                return key == value.ModuleAddress;
            }

            /// <summary>
            /// Compare a value with another value. Return true if values are equal.
            /// This function must be thread safe.
            /// </summary>
            protected override bool CompareValueToValue(PerModuleMethodNameResolver value1, PerModuleMethodNameResolver value2)
            {
                return value1.ModuleAddress == value2.ModuleAddress;
            }

            /// <summary>
            /// Create a new value from a key. Must be threadsafe. Value may or may not be added
            /// to collection. Return value must not be null.
            /// </summary>
            protected override PerModuleMethodNameResolver CreateValueFromKey(IntPtr key)
            {
                return new PerModuleMethodNameResolver(key);
            }
        }

        /// <summary>
        /// Implementation of stack trace metadata callbacks.
        /// </summary>
        private sealed class StackTraceMetadataCallbacksImpl : StackTraceMetadataCallbacks
        {
            public override string TryGetMethodNameFromStartAddress(IntPtr methodStartAddress, out bool isStackTraceHidden)
            {
                return GetMethodNameFromStartAddressIfAvailable(methodStartAddress, out isStackTraceHidden);
            }
        }

        /// <summary>
        /// Method name resolver for a single binary module
        /// </summary>
        private sealed class PerModuleMethodNameResolver
        {
            /// <summary>
            /// Start address of the module in question.
            /// </summary>
            private readonly IntPtr _moduleAddress;

            /// <summary>
            /// Dictionary mapping method RVA's to tokens within the metadata blob.
            /// </summary>
            private readonly StackTraceData[] _stacktraceDatas;

            /// <summary>
            /// Metadata reader for the stack trace metadata.
            /// </summary>
            private readonly MetadataReader _metadataReader;

            /// <summary>
            /// Publicly exposed module address property.
            /// </summary>
            public IntPtr ModuleAddress { get { return _moduleAddress; } }

            /// <summary>
            /// Construct the per-module resolver by looking up the necessary blobs.
            /// </summary>
            public unsafe PerModuleMethodNameResolver(IntPtr moduleAddress)
            {
                _moduleAddress = moduleAddress;

                TypeManagerHandle handle = new TypeManagerHandle(moduleAddress);
                ModuleInfo moduleInfo;
                if (!ModuleList.Instance.TryGetModuleInfoByHandle(handle, out moduleInfo))
                {
                    // Module not found
                    return;
                }

                NativeFormatModuleInfo nativeFormatModuleInfo = moduleInfo as NativeFormatModuleInfo;
                if (nativeFormatModuleInfo == null)
                {
                    // It is not a native format module
                    return;
                }

                byte *metadataBlob;
                uint metadataBlobSize;

                byte *rvaToTokenMapBlob;
                uint rvaToTokenMapBlobSize;

                if (nativeFormatModuleInfo.TryFindBlob(
                        (int)ReflectionMapBlob.EmbeddedMetadata,
                        out metadataBlob,
                        out metadataBlobSize) &&
                    nativeFormatModuleInfo.TryFindBlob(
                        (int)ReflectionMapBlob.BlobIdStackTraceMethodRvaToTokenMapping,
                        out rvaToTokenMapBlob,
                        out rvaToTokenMapBlobSize))
                {
                    _metadataReader = new MetadataReader(new IntPtr(metadataBlob), (int)metadataBlobSize);

                    int entryCount = *(int*)rvaToTokenMapBlob;
                    _stacktraceDatas = new StackTraceData[entryCount];

                    PopulateRvaToTokenMap(handle, rvaToTokenMapBlob + sizeof(int), rvaToTokenMapBlobSize - sizeof(int));
                }
            }

            /// <summary>
            /// Construct the dictionary mapping method RVAs to stack trace metadata tokens
            /// within a single binary module.
            /// </summary>
            /// <param name="handle">Module to use to construct the mapping</param>
            /// <param name="pMap">List of RVA - token pairs</param>
            /// <param name="length">Length of the blob</param>
            private unsafe void PopulateRvaToTokenMap(TypeManagerHandle handle, byte* pMap, uint length)
            {
                Handle currentOwningType = default;
                MethodSignatureHandle currentSignature = default;
                ConstantStringValueHandle currentName = default;
                ConstantStringArrayHandle currentMethodInst = default;

                int current = 0;
                byte* pCurrent = pMap;
                while (pCurrent < pMap + length)
                {
                    byte command = *pCurrent++;

                    if ((command & StackTraceDataCommand.UpdateOwningType) != 0)
                    {
                        currentOwningType = Handle.FromIntToken((int)NativePrimitiveDecoder.ReadUInt32(ref pCurrent));
                        Debug.Assert(currentOwningType.HandleType is HandleType.TypeDefinition or HandleType.TypeReference or HandleType.TypeSpecification);
                    }

                    if ((command & StackTraceDataCommand.UpdateName) != 0)
                    {
                        currentName = new Handle(HandleType.ConstantStringValue, (int)NativePrimitiveDecoder.DecodeUnsigned(ref pCurrent)).ToConstantStringValueHandle(_metadataReader);
                    }

                    if ((command & StackTraceDataCommand.UpdateSignature) != 0)
                    {
                        currentSignature = new Handle(HandleType.MethodSignature, (int)NativePrimitiveDecoder.DecodeUnsigned(ref pCurrent)).ToMethodSignatureHandle(_metadataReader);
                        currentMethodInst = default;
                    }

                    if ((command & StackTraceDataCommand.UpdateGenericSignature) != 0)
                    {
                        currentSignature = new Handle(HandleType.MethodSignature, (int)NativePrimitiveDecoder.DecodeUnsigned(ref pCurrent)).ToMethodSignatureHandle(_metadataReader);
                        currentMethodInst = new Handle(HandleType.ConstantStringArray, (int)NativePrimitiveDecoder.DecodeUnsigned(ref pCurrent)).ToConstantStringArrayHandle(_metadataReader);
                    }

                    void* pMethod = ReadRelPtr32(pCurrent);
                    pCurrent += sizeof(int);

                    Debug.Assert((nint)pMethod > handle.OsModuleBase);
                    int methodRva = (int)((nint)pMethod - handle.OsModuleBase);

                    _stacktraceDatas[current++] = new StackTraceData
                    {
                        Rva = methodRva,
                        IsHidden = (command & StackTraceDataCommand.IsStackTraceHidden) != 0,
                        OwningType = currentOwningType,
                        Name = currentName,
                        Signature = currentSignature,
                        GenericArguments = currentMethodInst,
                    };

                    static void* ReadRelPtr32(byte* address)
                        => address + *(int*)address;
                }

                Debug.Assert(current == _stacktraceDatas.Length);

                Array.Sort(_stacktraceDatas);
            }

            /// <summary>
            /// Try to resolve method name based on its address using the stack trace metadata
            /// </summary>
            public string GetMethodNameFromRvaIfAvailable(int rva, out bool isStackTraceHidden)
            {
                isStackTraceHidden = false;

                if (_stacktraceDatas == null)
                {
                    // No stack trace metadata for this module
                    return null;
                }

                int index = Array.BinarySearch(_stacktraceDatas, new StackTraceData() { Rva = rva });
                if (index < 0)
                {
                    // Method RVA not found in the map
                    return null;
                }

                StackTraceData data = _stacktraceDatas[index];

                isStackTraceHidden = data.IsHidden;

                if (data.OwningType.IsNull(_metadataReader))
                {
                    Debug.Assert(data.Name.IsNull(_metadataReader) && data.Signature.IsNull(_metadataReader));
                    Debug.Assert(isStackTraceHidden);
                    return null;
                }

                return MethodNameFormatter.FormatMethodName(_metadataReader, data.OwningType, data.Name, data.Signature, data.GenericArguments);
            }

            private struct StackTraceData : IComparable<StackTraceData>
            {
                private const int IsHiddenFlag = 0x2;

                private readonly int _rvaAndIsHiddenBit;

                public int Rva
                {
                    get => _rvaAndIsHiddenBit & ~IsHiddenFlag;
                    init
                    {
                        Debug.Assert((value & IsHiddenFlag) == 0);
                        _rvaAndIsHiddenBit = value | (_rvaAndIsHiddenBit & IsHiddenFlag);
                    }
                }
                public bool IsHidden
                {
                    get => (_rvaAndIsHiddenBit & IsHiddenFlag) != 0;
                    init
                    {
                        if (value)
                            _rvaAndIsHiddenBit |= IsHiddenFlag;
                    }
                }
                public Handle OwningType { get; init; }
                public ConstantStringValueHandle Name { get; init; }
                public MethodSignatureHandle Signature { get; init; }
                public ConstantStringArrayHandle GenericArguments { get; init; }

                public int CompareTo(StackTraceData other) => Rva.CompareTo(other.Rva);
            }
        }
    }
}
