// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using System.Reflection.Runtime.General;

using Internal.Runtime;
using Internal.Runtime.Augments;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    public sealed partial class TypeLoaderEnvironment
    {
        private class NamedTypeLookupResult
        {
            public int RuntimeTypeHandleHashcode;
            public RuntimeTypeHandle RuntimeTypeHandle;
            public QTypeDefinition QualifiedTypeDefinition;
            public IntPtr GcStaticFields;
            public IntPtr NonGcStaticFields;
            public volatile int VersionNumber;
        }

        private volatile int _namedTypeLookupLiveVersion;

        private NamedTypeRuntimeTypeHandleToMetadataHashtable _runtimeTypeHandleToMetadataHashtable = new NamedTypeRuntimeTypeHandleToMetadataHashtable();

        public static IntPtr NoStaticsData { get; } = (IntPtr)1;

        private class NamedTypeRuntimeTypeHandleToMetadataHashtable : LockFreeReaderHashtable<RuntimeTypeHandle, NamedTypeLookupResult>
        {
            protected override unsafe int GetKeyHashCode(RuntimeTypeHandle key)
            {
                return (int)key.ToEETypePtr()->HashCode;
            }
            protected override bool CompareKeyToValue(RuntimeTypeHandle key, NamedTypeLookupResult value)
            {
                return key.Equals(value.RuntimeTypeHandle);
            }

            protected override unsafe int GetValueHashCode(NamedTypeLookupResult value)
            {
                return value.RuntimeTypeHandleHashcode;
            }

            protected override bool CompareValueToValue(NamedTypeLookupResult value1, NamedTypeLookupResult value2)
            {
                if (value1.RuntimeTypeHandle.IsNull() || value2.RuntimeTypeHandle.IsNull())
                {
                    return value1.QualifiedTypeDefinition.Token.Equals(value2.QualifiedTypeDefinition.Token) &&
                           value1.QualifiedTypeDefinition.Reader.Equals(value2.QualifiedTypeDefinition.Reader);
                }
                return value1.RuntimeTypeHandle.Equals(value2.RuntimeTypeHandle);
            }

            protected override NamedTypeLookupResult CreateValueFromKey(RuntimeTypeHandle key)
            {
                int hashCode = GetKeyHashCode(key);

                // Iterate over all modules, starting with the module that defines the MethodTable
                foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules(RuntimeAugments.GetModuleFromTypeHandle(key)))
                {
                    NativeReader typeMapReader;
                    if (TryGetNativeReaderForBlob(module, ReflectionMapBlob.TypeMap, out typeMapReader))
                    {
                        NativeParser typeMapParser = new NativeParser(typeMapReader, 0);
                        NativeHashtable typeHashtable = new NativeHashtable(typeMapParser);

                        ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                        externalReferences.InitializeCommonFixupsTable(module);

                        var lookup = typeHashtable.Lookup(hashCode);
                        NativeParser entryParser;
                        while (!(entryParser = lookup.GetNext()).IsNull)
                        {
                            RuntimeTypeHandle foundType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                            if (foundType.Equals(key))
                            {
                                Handle entryMetadataHandle = entryParser.GetUnsigned().AsHandle();
                                if (entryMetadataHandle.HandleType == HandleType.TypeDefinition)
                                {
                                    MetadataReader metadataReader = module.MetadataReader;
                                    return new NamedTypeLookupResult()
                                    {
                                        QualifiedTypeDefinition = new QTypeDefinition(metadataReader, entryMetadataHandle.ToTypeDefinitionHandle(metadataReader)),
                                        RuntimeTypeHandle = key,
                                        RuntimeTypeHandleHashcode = hashCode
                                    };
                                }
                            }
                        }
                    }
                }

                return new NamedTypeLookupResult()
                {
                    RuntimeTypeHandle = key,
                    RuntimeTypeHandleHashcode = hashCode
                };
            }
        }

        private QTypeDefinitionToRuntimeTypeHandleHashtable _metadataToRuntimeTypeHandleHashtable = new QTypeDefinitionToRuntimeTypeHandleHashtable();

        private class QTypeDefinitionToRuntimeTypeHandleHashtable : LockFreeReaderHashtable<QTypeDefinition, NamedTypeLookupResult>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int _rotl(int value, int shift)
            {
                return (int)(((uint)value << shift) | ((uint)value >> (32 - shift)));
            }

            protected override unsafe int GetKeyHashCode(QTypeDefinition key)
            {
                return key.Token.GetHashCode() ^ _rotl(key.Reader.GetHashCode(), 8);
            }
            protected override bool CompareKeyToValue(QTypeDefinition key, NamedTypeLookupResult value)
            {
                return key.Token.Equals(value.QualifiedTypeDefinition.Token) &&
                       key.Reader.Equals(value.QualifiedTypeDefinition.Reader);
            }

            protected override unsafe int GetValueHashCode(NamedTypeLookupResult value)
            {
                return value.QualifiedTypeDefinition.Token.GetHashCode() ^ _rotl(value.QualifiedTypeDefinition.Reader.GetHashCode(), 8);
            }

            protected override bool CompareValueToValue(NamedTypeLookupResult value1, NamedTypeLookupResult value2)
            {
                return value1.QualifiedTypeDefinition.Token.Equals(value2.QualifiedTypeDefinition.Token) &&
                        value1.QualifiedTypeDefinition.Reader.Equals(value2.QualifiedTypeDefinition.Reader);
            }

            protected override NamedTypeLookupResult CreateValueFromKey(QTypeDefinition key)
            {
                RuntimeTypeHandle foundRuntimeTypeHandle = default(RuntimeTypeHandle);

                if (key.IsNativeFormatMetadataBased)
                {
                    MetadataReader metadataReader = key.NativeFormatReader;
                    TypeDefinitionHandle typeDefHandle = key.NativeFormatHandle;
                    int hashCode = typeDefHandle.ComputeHashCode(metadataReader);

                    NativeFormatModuleInfo module = ModuleList.Instance.GetModuleInfoForMetadataReader(metadataReader);

                    NativeReader typeMapReader;
                    if (TryGetNativeReaderForBlob(module, ReflectionMapBlob.TypeMap, out typeMapReader))
                    {
                        NativeParser typeMapParser = new NativeParser(typeMapReader, 0);
                        NativeHashtable typeHashtable = new NativeHashtable(typeMapParser);

                        ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                        externalReferences.InitializeCommonFixupsTable(module);

                        var lookup = typeHashtable.Lookup(hashCode);
                        NativeParser entryParser;
                        while (!(entryParser = lookup.GetNext()).IsNull)
                        {
                            var foundTypeIndex = entryParser.GetUnsigned();
                            if (entryParser.GetUnsigned().AsHandle().Equals(typeDefHandle))
                            {
                                foundRuntimeTypeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(foundTypeIndex);
                                break;
                            }
                        }
                    }
                }

                return new NamedTypeLookupResult()
                {
                    QualifiedTypeDefinition = key,
                    RuntimeTypeHandle = foundRuntimeTypeHandle,
                    VersionNumber = TypeLoaderEnvironment.Instance._namedTypeLookupLiveVersion
                };
            }
        }

        /// <summary>
        /// Return the metadata handle for a TypeDef if the pay-for-policy enabled this type as browsable. This is used to obtain name and other information for types
        /// obtained via typeof() or Object.GetType(). This can include generic types (not to be confused with generic instances).
        ///
        /// Preconditions:
        ///    runtimeTypeHandle is a typedef (not a constructed type such as an array or generic instance.)
        /// </summary>
        /// <param name="runtimeTypeHandle">Runtime handle of the type in question</param>
        /// <param name="qTypeDefinition">TypeDef handle for the type</param>
        public unsafe bool TryGetMetadataForNamedType(RuntimeTypeHandle runtimeTypeHandle, out QTypeDefinition qTypeDefinition)
        {
            NamedTypeLookupResult result = _runtimeTypeHandleToMetadataHashtable.GetOrCreateValue(runtimeTypeHandle);
            qTypeDefinition = result.QualifiedTypeDefinition;
            return qTypeDefinition.Reader != null;
        }

        /// <summary>
        /// Get the static addresses of a type if it is in the table
        /// </summary>
        /// <param name="runtimeTypeHandle">Runtime handle of the type in question</param>
        /// <param name="nonGcStaticsData">non-gc static field address</param>
        /// <param name="gcStaticsData">gc static field address</param>
        /// <returns>true if nonGcStaticsData/gcStaticsData are valid, false if not</returns>
        public unsafe bool TryGetStaticsInfoForNamedType(RuntimeTypeHandle runtimeTypeHandle, out IntPtr nonGcStaticsData, out IntPtr gcStaticsData)
        {
            NamedTypeLookupResult result;

            if (!_runtimeTypeHandleToMetadataHashtable.TryGetValue(runtimeTypeHandle, out result))
            {
                gcStaticsData = IntPtr.Zero;
                nonGcStaticsData = IntPtr.Zero;
                return false;
            }

            gcStaticsData = result.GcStaticFields;
            nonGcStaticsData = result.NonGcStaticFields;

            bool noResults = gcStaticsData == IntPtr.Zero || gcStaticsData == IntPtr.Zero;

            if (gcStaticsData == (IntPtr)1)
                gcStaticsData = IntPtr.Zero;

            if (nonGcStaticsData == (IntPtr)1)
                nonGcStaticsData = IntPtr.Zero;

            return result.QualifiedTypeDefinition.Reader != null && !noResults;
        }

        /// <summary>
        /// Return the RuntimeTypeHandle for the named type described in metadata. This is used to implement the Create and Invoke
        /// apis for types.
        ///
        /// Preconditions:
        ///    metadataReader + typeDefHandle  - a valid metadata reader + typeDefinitionHandle where "metadataReader" is one
        ///                                      of the metadata readers returned by ExecutionEnvironment.MetadataReaders.
        ///
        /// Note: Although this method has a "bool" return value like the other mapping table accessors, the pay-for-play design
        /// guarantees that any type enabled for metadata also has a RuntimeTypeHandle underneath.
        /// </summary>
        /// <param name="qTypeDefinition">TypeDef handle for the type to look up</param>
        /// <param name="runtimeTypeHandle">Runtime type handle (MethodTable) for the given type</param>
        public unsafe bool TryGetNamedTypeForMetadata(QTypeDefinition qTypeDefinition, out RuntimeTypeHandle runtimeTypeHandle)
        {
            runtimeTypeHandle = default(RuntimeTypeHandle);
            NamedTypeLookupResult result = _metadataToRuntimeTypeHandleHashtable.GetOrCreateValue(qTypeDefinition);

            if (result.VersionNumber <= _namedTypeLookupLiveVersion)
                runtimeTypeHandle = result.RuntimeTypeHandle;

            return !runtimeTypeHandle.IsNull();
        }

        public void RegisterNewNamedTypeRuntimeTypeHandle(QTypeDefinition qTypeDefinition, RuntimeTypeHandle runtimeTypeHandle, IntPtr nonGcStaticFields, IntPtr gcStaticFields)
        {
            TypeLoaderLogger.WriteLine("Register new type with MethodTable = " + runtimeTypeHandle.ToIntPtr().LowLevelToString() + " nonGcStaticFields " + nonGcStaticFields.LowLevelToString() + " gcStaticFields " + gcStaticFields.LowLevelToString());
            NamedTypeLookupResult result = _metadataToRuntimeTypeHandleHashtable.GetOrCreateValue(qTypeDefinition);

            result.VersionNumber = _namedTypeLookupLiveVersion + 1;
            result.RuntimeTypeHandle = runtimeTypeHandle;
            result.GcStaticFields = gcStaticFields;
            result.NonGcStaticFields = nonGcStaticFields;
            unsafe
            {
                result.RuntimeTypeHandleHashcode = (int)runtimeTypeHandle.ToEETypePtr()->HashCode;
            }

            NamedTypeLookupResult rthToMetadataResult = _runtimeTypeHandleToMetadataHashtable.AddOrGetExisting(result);

            if (!object.ReferenceEquals(rthToMetadataResult, result))
            {
                rthToMetadataResult.QualifiedTypeDefinition = qTypeDefinition;
                rthToMetadataResult.GcStaticFields = gcStaticFields;
                rthToMetadataResult.NonGcStaticFields = nonGcStaticFields;
            }
        }

        public void UnregisterNewNamedTypeRuntimeTypeHandle(QTypeDefinition qTypeDefinition, RuntimeTypeHandle runtimeTypeHandle)
        {
            NamedTypeLookupResult metadataLookupResult;
            if (_metadataToRuntimeTypeHandleHashtable.TryGetValue(qTypeDefinition, out metadataLookupResult))
            {
                metadataLookupResult.RuntimeTypeHandle = default(RuntimeTypeHandle);
                metadataLookupResult.VersionNumber = -1;
            }

            if (_runtimeTypeHandleToMetadataHashtable.TryGetValue(runtimeTypeHandle, out _))
            {
                metadataLookupResult.GcStaticFields = IntPtr.Zero;
                metadataLookupResult.NonGcStaticFields = IntPtr.Zero;
                metadataLookupResult.RuntimeTypeHandle = default(RuntimeTypeHandle);
            }
        }

        public void FinishAddingNewNamedTypes()
        {
            _namedTypeLookupLiveVersion++;
            if (_namedTypeLookupLiveVersion == int.MaxValue)
                Environment.FailFast("Too many types loaded");
        }
    }
}
