// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime;
using System.Runtime.InteropServices;

using Internal.NativeFormat;
using Internal.Runtime.Augments;
using Internal.Runtime.TypeLoader;

namespace Internal.Runtime.CompilerHelpers
{
    internal static class RuntimeInteropData
    {
        public static uint GetStructFieldOffset(RuntimeTypeHandle structureTypeHandle, string fieldName)
        {
            if (TryGetStructFieldOffset(structureTypeHandle, fieldName, out bool structExists, out uint offset))
            {
                return offset;
            }

            Type structureType = Type.GetTypeFromHandle(structureTypeHandle)!;

            // if we can find the struct but couldn't find its field, throw Argument Exception
            if (structExists)
            {
                throw new ArgumentException(SR.Format(SR.Argument_OffsetOfFieldNotFound, structureType), nameof(fieldName));
            }

            throw new NotSupportedException(SR.Format(SR.StructMarshalling_MissingInteropData, structureType));
        }

        public static unsafe int GetStructUnsafeStructSize(RuntimeTypeHandle structureTypeHandle)
        {
            if (TryGetStructUnsafeStructSize(structureTypeHandle, out int size))
            {
                return size;
            }

            MethodTable* structureMT = structureTypeHandle.ToMethodTable();

            // IsBlittable() checks whether the type contains GC references. It is approximate check with false positives.
            // This fallback path will return incorrect answer for types that do not contain GC references, but that are
            // not actually blittable; e.g. for types with bool fields.
            if (structureTypeHandle.IsBlittable() && structureMT->IsValueType)
            {
                return (int)structureMT->ValueTypeSize;
            }

            // If the type is an interface or a generic type, the reason is likely that.
            Type structureType = Type.GetTypeFromHandle(structureTypeHandle)!;
            if (structureMT->IsInterface || structureMT->IsGeneric)
            {
                throw new ArgumentException(SR.Format(SR.Arg_CannotMarshal, structureType));
            }

            // Otherwise assume we miss interop data for the type.
            throw new NotSupportedException(SR.Format(SR.StructMarshalling_MissingInteropData, structureType));
        }

        public static IntPtr GetStructUnmarshalStub(RuntimeTypeHandle structureTypeHandle)
        {
            if (TryGetStructUnmarshalStub(structureTypeHandle, out IntPtr stub))
            {
                return stub;
            }

            throw new NotSupportedException(SR.Format(SR.StructMarshalling_MissingInteropData, Type.GetTypeFromHandle(structureTypeHandle)));
        }

        public static IntPtr GetStructMarshalStub(RuntimeTypeHandle structureTypeHandle)
        {
            if (TryGetStructMarshalStub(structureTypeHandle, out IntPtr stub))
            {
                return stub;
            }

            throw new NotSupportedException(SR.Format(SR.StructMarshalling_MissingInteropData, Type.GetTypeFromHandle(structureTypeHandle)));
        }

        public static IntPtr GetDestroyStructureStub(RuntimeTypeHandle structureTypeHandle, out bool hasInvalidLayout)
        {
            if (TryGetDestroyStructureStub(structureTypeHandle, out IntPtr stub, out hasInvalidLayout))
            {
                return stub;
            }

            throw new NotSupportedException(SR.Format(SR.StructMarshalling_MissingInteropData, Type.GetTypeFromHandle(structureTypeHandle)));
        }


        public static IntPtr GetForwardDelegateCreationStub(RuntimeTypeHandle delegateTypeHandle)
        {
            GetMarshallersForDelegate(delegateTypeHandle, out _, out _, out IntPtr delegateCreationStub);
            if (delegateCreationStub == IntPtr.Zero)
                throw new NotSupportedException(SR.Format(SR.DelegateMarshalling_MissingInteropData, Type.GetTypeFromHandle(delegateTypeHandle)));
            return delegateCreationStub;
        }

        public static IntPtr GetDelegateMarshallingStub(RuntimeTypeHandle delegateTypeHandle, bool openStaticDelegate)
        {
            GetMarshallersForDelegate(delegateTypeHandle, out IntPtr openStub, out IntPtr closedStub, out _);
            IntPtr pStub = openStaticDelegate ? openStub : closedStub;
            if (pStub == IntPtr.Zero)
                throw new NotSupportedException(SR.Format(SR.DelegateMarshalling_MissingInteropData, Type.GetTypeFromHandle(delegateTypeHandle)));
            return pStub;
        }

        #region "Struct Data"
        public static bool TryGetStructUnmarshalStub(RuntimeTypeHandle structureTypeHandle, out IntPtr unmarshalStub)
            => TryGetMarshallersForStruct(structureTypeHandle, out _, out unmarshalStub, out _, out _, out _);

        public static bool TryGetStructMarshalStub(RuntimeTypeHandle structureTypeHandle, out IntPtr marshalStub)
            => TryGetMarshallersForStruct(structureTypeHandle, out marshalStub, out _, out _, out _, out _);

        public static bool TryGetDestroyStructureStub(RuntimeTypeHandle structureTypeHandle, out IntPtr destroyStub, out bool hasInvalidLayout)
            => TryGetMarshallersForStruct(structureTypeHandle, out _, out _, out destroyStub, out hasInvalidLayout, out _);

        public static bool TryGetStructUnsafeStructSize(RuntimeTypeHandle structureTypeHandle, out int size)
            => TryGetMarshallersForStruct(structureTypeHandle, out _, out _, out _, out _, out size);

        public static bool TryGetStructFieldOffset(RuntimeTypeHandle structureTypeHandle, string fieldName, out bool structExists, out uint offset)
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

        private static unsafe bool TryGetNativeReaderForBlob(TypeManagerHandle module, ReflectionMapBlob blob, out NativeReader reader)
        {
            byte* pBlob;
            uint cbBlob;

            if (RuntimeImports.RhFindBlob(module, (uint)blob, &pBlob, &cbBlob))
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

            foreach (TypeManagerHandle module in RuntimeAugments.GetLoadedModules())
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
            foreach (TypeManagerHandle module in RuntimeAugments.GetLoadedModules())
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
