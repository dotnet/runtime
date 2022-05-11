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
    internal partial class RuntimeInteropData
    {
        public override IntPtr GetForwardDelegateCreationStub(RuntimeTypeHandle delegateTypeHandle)
        {
            GetMarshallersForDelegate(delegateTypeHandle, out _, out _, out IntPtr delegateCreationStub);
            if (delegateCreationStub == IntPtr.Zero)
                throw new MissingInteropDataException(SR.DelegateMarshalling_MissingInteropData, Type.GetTypeFromHandle(delegateTypeHandle));
            return delegateCreationStub;
        }

        public override IntPtr GetDelegateMarshallingStub(RuntimeTypeHandle delegateTypeHandle, bool openStaticDelegate)
        {
            GetMarshallersForDelegate(delegateTypeHandle, out IntPtr openStub, out IntPtr closedStub, out _);
            IntPtr pStub = openStaticDelegate ? openStub : closedStub;
            if (pStub == IntPtr.Zero)
                throw new MissingInteropDataException(SR.DelegateMarshalling_MissingInteropData, Type.GetTypeFromHandle(delegateTypeHandle));
            return pStub;
        }

        #region "Struct Data"
        public override bool TryGetStructUnmarshalStub(RuntimeTypeHandle structureTypeHandle, out IntPtr unmarshalStub)
            => TryGetMarshallersForStruct(structureTypeHandle, out _, out unmarshalStub, out _, out _, out _);

        public override bool TryGetStructMarshalStub(RuntimeTypeHandle structureTypeHandle, out IntPtr marshalStub)
            => TryGetMarshallersForStruct(structureTypeHandle, out marshalStub, out _, out _, out _, out _);

        public override bool TryGetDestroyStructureStub(RuntimeTypeHandle structureTypeHandle, out IntPtr destroyStub, out bool hasInvalidLayout)
            => TryGetMarshallersForStruct(structureTypeHandle, out _, out _, out destroyStub, out hasInvalidLayout, out _);

        public override bool TryGetStructUnsafeStructSize(RuntimeTypeHandle structureTypeHandle, out int size)
            => TryGetMarshallersForStruct(structureTypeHandle, out _, out _, out _, out _, out size);

        public override bool TryGetStructFieldOffset(RuntimeTypeHandle structureTypeHandle, string fieldName, out bool structExists, out uint offset)
        {
            NativeParser entryParser;
            structExists = false;
            if (TryGetStructData(structureTypeHandle, out _, out entryParser))
            {
                structExists = true;

                uint mask = entryParser.GetUnsigned();
                if ((mask & InteropDataConstants.HasMarshallers) != 0)
                {
                    // skip the first 4 IntPtrs(3 stubs and size)
                    entryParser.SkipInteger();
                    entryParser.SkipInteger();
                    entryParser.SkipInteger();
                    entryParser.SkipInteger();
                }

                uint fieldCount = mask >> InteropDataConstants.FieldCountShift;
                for (uint index = 0; index < fieldCount; index++)
                {
                    string name = entryParser.GetString();
                    offset = entryParser.GetUnsigned();
                    if (name == fieldName)
                    {
                        return true;
                    }
                }
            }
            offset = 0;
            return false;
        }
        #endregion

        private static unsafe bool TryGetNativeReaderForBlob(NativeFormatModuleInfo module, ReflectionMapBlob blob, out NativeReader reader)
        {
            byte* pBlob;
            uint cbBlob;

            if (module.TryFindBlob((int)blob, out pBlob, out cbBlob))
            {
                reader = new NativeReader(pBlob, cbBlob);
                return true;
            }

            reader = default(NativeReader);
            return false;
        }

        private static unsafe bool GetMarshallersForDelegate(RuntimeTypeHandle delegateTypeHandle, out IntPtr openStub, out IntPtr closedStub, out IntPtr delegateCreationStub)
        {
            int delegateHashcode = delegateTypeHandle.GetHashCode();
            openStub = IntPtr.Zero;
            closedStub = IntPtr.Zero;
            delegateCreationStub = IntPtr.Zero;

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                NativeReader delegateMapReader;
                if (TryGetNativeReaderForBlob(module, ReflectionMapBlob.DelegateMarshallingStubMap, out delegateMapReader))
                {
                    NativeParser delegateMapParser = new NativeParser(delegateMapReader, 0);
                    NativeHashtable delegateHashtable = new NativeHashtable(delegateMapParser);

                    ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                    externalReferences.InitializeCommonFixupsTable(module);

                    var lookup = delegateHashtable.Lookup(delegateHashcode);
                    NativeParser entryParser;
                    while (!(entryParser = lookup.GetNext()).IsNull)
                    {
                        RuntimeTypeHandle foundDelegateType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                        if (foundDelegateType.Equals(delegateTypeHandle))
                        {
                            openStub = externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());
                            closedStub = externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());
                            delegateCreationStub = externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static unsafe bool TryGetStructData(RuntimeTypeHandle structTypeHandle, out ExternalReferencesTable externalReferences, out NativeParser entryParser)
        {
            int structHashcode = structTypeHandle.GetHashCode();
            externalReferences = default(ExternalReferencesTable);
            entryParser = default(NativeParser);
            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                NativeReader structMapReader;
                if (TryGetNativeReaderForBlob(module, ReflectionMapBlob.StructMarshallingStubMap, out structMapReader))
                {
                    NativeParser structMapParser = new NativeParser(structMapReader, 0);
                    NativeHashtable structHashtable = new NativeHashtable(structMapParser);

                    externalReferences.InitializeCommonFixupsTable(module);

                    var lookup = structHashtable.Lookup(structHashcode);
                    while (!(entryParser = lookup.GetNext()).IsNull)
                    {
                        RuntimeTypeHandle foundStructType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                        if (foundStructType.Equals(structTypeHandle))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static unsafe bool TryGetMarshallersForStruct(RuntimeTypeHandle structTypeHandle, out IntPtr marshalStub, out IntPtr unmarshalStub, out IntPtr destroyStub, out bool hasInvalidLayout, out int size)
        {
            marshalStub = IntPtr.Zero;
            unmarshalStub = IntPtr.Zero;
            destroyStub = IntPtr.Zero;
            hasInvalidLayout = true;
            size = 0;

            ExternalReferencesTable externalReferences;
            NativeParser entryParser;
            if (TryGetStructData(structTypeHandle, out externalReferences, out entryParser))
            {
                uint mask = entryParser.GetUnsigned();
                if ((mask & InteropDataConstants.HasMarshallers) != 0)
                {
                    hasInvalidLayout = (mask & InteropDataConstants.HasInvalidLayout) != 0;

                    size = (int)entryParser.GetUnsigned();
                    marshalStub = externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());
                    unmarshalStub = externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());
                    destroyStub = externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());

                    return true;
                }
            }
            return false;
        }
    }
}
