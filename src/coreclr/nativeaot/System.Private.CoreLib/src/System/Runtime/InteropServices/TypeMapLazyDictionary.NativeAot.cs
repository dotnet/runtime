// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Internal.NativeFormat;
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

            return new Dictionary<string, Type>(); // Return an empty dictionary if no valid type map is found.
        }

        public static IReadOnlyDictionary<Type, Type> CreateProxyTypeMap(RuntimeType typeMapGroup)
        {
            RuntimeTypeHandle typeMapGroupHandle = typeMapGroup.TypeHandle;
            foreach (TypeManagerHandle module in RuntimeAugments.GetLoadedModules())
            {
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.AssociatedTypeMap, out NativeReader externalTypeMapReader))
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

            return new Dictionary<Type, Type>(); // Return an empty dictionary if no valid type map is found.
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

        private class ExternalTypeMapDictionary(NativeHashtable table, ExternalReferencesTable externalReferences) : IReadOnlyDictionary<string, Type>
        {
            public Type this[string key]
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

            public bool TryGetValue(string key, [MaybeNullWhen(false)] out Type value)
            {
                var lookup = table.Lookup(TypeHashingAlgorithms.ComputeNameHashCode(key));
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    string foundName = entryParser.GetString();
                    if (foundName == key)
                    {
                        RuntimeTypeHandle typeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                        value = Type.GetTypeFromHandle(typeHandle)!;
                        return true;
                    }
                }
                value = null;
                return false;
            }

            // Not supported to avoid exposing TypeMap entries in a manner that
            // would violate invariants the Trimmer is attempting to enforce.
            public IEnumerable<string> Keys => throw new NotSupportedException();

            public IEnumerable<Type> Values => throw new NotSupportedException();

            public int Count => throw new NotSupportedException();

            public bool ContainsKey(string key) => throw new NotSupportedException();
            public IEnumerator<KeyValuePair<string, Type>> GetEnumerator() => throw new NotSupportedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class AssociatedTypeMapDictionary(NativeHashtable table, ExternalReferencesTable externalReferences) : IReadOnlyDictionary<Type, Type>
        {
            public Type this[Type key]
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

            public bool TryGetValue(Type key, [MaybeNullWhen(false)] out Type value)
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

            // Not supported to avoid exposing TypeMap entries in a manner that
            // would violate invariants the Trimmer is attempting to enforce.
            public IEnumerable<Type> Keys => throw new NotSupportedException();

            public IEnumerable<Type> Values => throw new NotSupportedException();

            public int Count => throw new NotSupportedException();

            public bool ContainsKey(Type key) => throw new NotSupportedException();
            public IEnumerator<KeyValuePair<Type, Type>> GetEnumerator() => throw new NotSupportedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
