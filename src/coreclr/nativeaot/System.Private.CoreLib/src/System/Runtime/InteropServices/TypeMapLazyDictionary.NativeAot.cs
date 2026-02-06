// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Internal.NativeFormat;
using Internal.Reflection.Core.Execution;
using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.TypeLoader;

namespace System.Runtime.InteropServices
{
    internal static class TypeMapLazyDictionary
    {
        public static IReadOnlyDictionary<string, Type> CreateExternalTypeMap(RuntimeType typeMapGroup)
        {
            RuntimeTypeHandle typeMapGroupHandle = typeMapGroup.TypeHandle;
            foreach (TypeManagerHandle module in RuntimeAugments.GetLoadedModules())
            {
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.ExternalTypeMap, out NativeReader externalTypeMapReader))
                {
                    continue;
                }
                NativeParser externalTypeMapParser = new NativeParser(externalTypeMapReader, 0);
                NativeHashtable externalTypeMapTable = new NativeHashtable(externalTypeMapParser);

                ExternalReferencesTable externalReferences = default;
                externalReferences.InitializeCommonFixupsTable(module);

                var lookup = externalTypeMapTable.Lookup(typeMapGroupHandle.GetHashCode());
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    RuntimeTypeHandle foundTypeMapGroup = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (!foundTypeMapGroup.Equals(typeMapGroupHandle))
                    {
                        continue;
                    }
                    bool isValid = entryParser.GetUnsigned() == 1;
                    if (!isValid)
                    {
                        unsafe
                        {
                            delegate*<void> exceptionStub = (delegate*<void>)externalReferences.GetFunctionPointerFromIndex(entryParser.GetUnsigned());
                            exceptionStub();
                            Debug.Fail("Expected exception stub to throw an exception.");
                            return null; // Should never reach here, as the exception stub should throw an exception.
                        }
                    }

                    return new ExternalTypeMapDictionary(new NativeHashtable(entryParser), externalReferences);
                }
            }

            throw ReflectionCoreExecution.ExecutionEnvironment.CreateMissingMetadataException(typeMapGroup);
        }

        public static IReadOnlyDictionary<Type, Type> CreateProxyTypeMap(RuntimeType typeMapGroup)
        {
            RuntimeTypeHandle typeMapGroupHandle = typeMapGroup.TypeHandle;
            foreach (TypeManagerHandle module in RuntimeAugments.GetLoadedModules())
            {
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.ProxyTypeMap, out NativeReader externalTypeMapReader))
                {
                    continue;
                }
                NativeParser externalTypeMapParser = new NativeParser(externalTypeMapReader, 0);
                NativeHashtable externalTypeMapTable = new NativeHashtable(externalTypeMapParser);

                ExternalReferencesTable externalReferences = default;
                externalReferences.InitializeCommonFixupsTable(module);

                var lookup = externalTypeMapTable.Lookup(typeMapGroupHandle.GetHashCode());
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    RuntimeTypeHandle foundTypeMapGroup = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (!foundTypeMapGroup.Equals(typeMapGroupHandle))
                    {
                        continue;
                    }
                    bool isValid = entryParser.GetUnsigned() == 1;
                    if (!isValid)
                    {
                        unsafe
                        {
                            delegate*<void> exceptionStub = (delegate*<void>)externalReferences.GetFunctionPointerFromIndex(entryParser.GetUnsigned());
                            exceptionStub();
                            Debug.Fail("Expected exception stub to throw an exception.");
                            return null; // Should never reach here, as the exception stub should throw an exception.
                        }
                    }

                    return new AssociatedTypeMapDictionary(new NativeHashtable(entryParser), externalReferences);
                }
            }

            throw ReflectionCoreExecution.ExecutionEnvironment.CreateMissingMetadataException(typeMapGroup);
        }

        private static unsafe bool TryGetNativeReaderForBlob(TypeManagerHandle module, ReflectionMapBlob blob, out NativeReader reader)
        {
            byte* pBlob;
            uint cbBlob;

            if (RuntimeImports.RhFindBlob(module, (uint)blob, &pBlob, &cbBlob))
            {
                reader = new NativeReader(pBlob, cbBlob);
                return true;
            }

            reader = default;
            return false;
        }

        private abstract class TypeMapDictionaryBase<TKey> : IReadOnlyDictionary<TKey, Type>
        {
            public abstract Type this[TKey key] { get; }
            public abstract bool TryGetValue(TKey key, [MaybeNullWhen(false)] out Type value);
            // Not supported to avoid exposing TypeMap entries in a manner that
            // would violate invariants the Trimmer is attempting to enforce.
            public IEnumerable<TKey> Keys => throw new NotSupportedException();
            public IEnumerable<Type> Values => throw new NotSupportedException();
            public int Count => throw new NotSupportedException();
            public bool ContainsKey(TKey key) => throw new NotSupportedException();
            public IEnumerator<KeyValuePair<TKey, Type>> GetEnumerator() => throw new NotSupportedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ExternalTypeMapDictionary(NativeHashtable table, ExternalReferencesTable externalReferences) : TypeMapDictionaryBase<string>
        {
            public override Type this[string key]
            {
                get
                {
                    if (!TryGetValue(key, out Type? value))
                    {
                        ThrowHelper.ThrowKeyNotFoundException(key);
                    }
                    return value;
                }
            }

            public override  bool TryGetValue(string key, [MaybeNullWhen(false)] out Type value)
            {
                var lookup = table.Lookup(TypeHashingAlgorithms.ComputeNameHashCode(key));
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    if (entryParser.StringEquals(key))
                    {
                        entryParser.SkipString();
                        RuntimeTypeHandle typeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                        value = Type.GetTypeFromHandle(typeHandle)!;
                        return true;
                    }
                }
                value = null;
                return false;
            }
        }

        private sealed class AssociatedTypeMapDictionary(NativeHashtable table, ExternalReferencesTable externalReferences) : TypeMapDictionaryBase<Type>
        {
            public override Type this[Type key]
            {
                get
                {
                    if (!TryGetValue(key, out Type? value))
                    {
                        ThrowHelper.ThrowKeyNotFoundException(key);
                    }
                    return value;
                }
            }

            public override bool TryGetValue(Type key, [MaybeNullWhen(false)] out Type value)
            {
                RuntimeTypeHandle handle = key.TypeHandle;
                if (handle.IsNull)
                {
                    value = null;
                    return false;
                }

                var lookup = table.Lookup(handle.GetHashCode());
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    RuntimeTypeHandle foundHandle = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (foundHandle.Equals(handle))
                    {
                        RuntimeTypeHandle targetHandle = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                        value = Type.GetTypeFromHandle(targetHandle)!;
                        return true;
                    }
                }
                value = null;
                return false;
            }
        }
    }
}
