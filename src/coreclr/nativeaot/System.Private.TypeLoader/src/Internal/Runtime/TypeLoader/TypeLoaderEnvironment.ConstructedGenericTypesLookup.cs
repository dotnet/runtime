// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime;
using Internal.Runtime.Augments;

using Internal.NativeFormat;
using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    public sealed partial class TypeLoaderEnvironment
    {
        internal struct TypeEntryToRegister
        {
            public GenericTypeEntry GenericTypeEntry;
            public MetadataType MetadataDefinitionType;
        }

        internal class GenericTypeEntry
        {
            private int? _hashCode;
            public bool _isRegisteredSuccessfully;
            public RuntimeTypeHandle _instantiatedTypeHandle;
            public RuntimeTypeHandle _genericTypeDefinitionHandle;
            public RuntimeTypeHandle[] _genericTypeArgumentHandles;

            public override int GetHashCode()
            {
                if (!_hashCode.HasValue)
                {
                    _hashCode = TypeHashingAlgorithms.ComputeGenericInstanceHashCode(_genericTypeDefinitionHandle.GetHashCode(), _genericTypeArgumentHandles);
                }
                return _hashCode.Value;
            }

            public override bool Equals(object obj)
            {
                // There are no scenarios where we call .Equals to check the componentized equality (we explicitly use IsEqualToEntryByComponentsComparison for that),
                // so making sure we revert to the reference equality in case someone calls here.
                return base.Equals(obj);
            }

            public virtual bool IsEqualToEntryByComponentsComparison(GenericTypeEntry other)
            {
                if (!other._genericTypeDefinitionHandle.Equals(_genericTypeDefinitionHandle))
                    return false;

                if (other._genericTypeArgumentHandles == null)
                    return false;

                if (other._genericTypeArgumentHandles.Length != _genericTypeArgumentHandles.Length)
                    return false;

                for (int i = 0; i < _genericTypeArgumentHandles.Length; i++)
                    if (!other._genericTypeArgumentHandles[i].Equals(_genericTypeArgumentHandles[i]))
                        return false;

                return true;
            }
        }

        internal class DynamicGenericTypesHashtable : LockFreeReaderHashtable<GenericTypeLookupData, GenericTypeEntry>
        {
            protected override int GetKeyHashCode(GenericTypeLookupData key)
            {
                return key.LookupHashCode();
            }
            protected override bool CompareKeyToValue(GenericTypeLookupData key, GenericTypeEntry value)
            {
                return key.MatchGenericTypeEntry(value);
            }

            protected override int GetValueHashCode(GenericTypeEntry value)
            {
                return value.GetHashCode();
            }

            protected override bool CompareValueToValue(GenericTypeEntry value1, GenericTypeEntry value2)
            {
                // Comparisons should *only* be done using the generic type components
                return value1.IsEqualToEntryByComponentsComparison(value2);
            }

            protected override GenericTypeEntry CreateValueFromKey(GenericTypeLookupData key)
            {
                // Feature not used by the TypeBuilder
                throw NotImplemented.ByDesign;
            }
        }

        internal abstract class GenericTypeLookupData
        {
            internal abstract int LookupHashCode();
            internal abstract bool MatchParsedEntry(RuntimeTypeHandle tentativeType);
            internal abstract bool MatchGenericTypeEntry(GenericTypeEntry entry);
        }
        internal class DefTypeBasedGenericTypeLookup : GenericTypeLookupData
        {
            protected DefType _typeToLookup;

            internal DefTypeBasedGenericTypeLookup(DefType typeToLookup) { _typeToLookup = typeToLookup; }

            internal override int LookupHashCode() { return _typeToLookup.GetHashCode(); }

            internal override bool MatchParsedEntry(RuntimeTypeHandle tentativeType)
            {
                //
                // Entries read from the hashtable are loaded as DefTypes, and compared to the input.
                // This lookup is slower than the lookups using RuntimeTypeHandles, but can handle cases where we don't have
                // RuntimeTypeHandle values for all of the components of the input DefType, but still need to look it up in case the type
                // statically exists and has an existing RuntimeTypeHandle value.
                //
                TypeSystemContext context = _typeToLookup.Context;

                RuntimeTypeHandle[] parsedArgsHandles;
                RuntimeTypeHandle parsedTypeDefinitionHandle = RuntimeAugments.GetGenericInstantiation(tentativeType, out parsedArgsHandles);

                DefType parsedTypeDefinition = (DefType)context.ResolveRuntimeTypeHandle(parsedTypeDefinitionHandle);
                Instantiation parsedArgs = context.ResolveRuntimeTypeHandles(parsedArgsHandles);
                DefType parsedGenericType = context.ResolveGenericInstantiation(parsedTypeDefinition, parsedArgs);

                return parsedGenericType == _typeToLookup;
            }

            internal override bool MatchGenericTypeEntry(GenericTypeEntry entry)
            {
                TypeSystemContext context = _typeToLookup.Context;

                DefType parsedTypeDefinition = (DefType)context.ResolveRuntimeTypeHandle(entry._genericTypeDefinitionHandle);
                Instantiation parsedArgs = context.ResolveRuntimeTypeHandles(entry._genericTypeArgumentHandles);
                DefType parsedGenericType = context.ResolveGenericInstantiation(parsedTypeDefinition, parsedArgs);

                return parsedGenericType == _typeToLookup;
            }
        }
        internal class HandleBasedGenericTypeLookup : DefTypeBasedGenericTypeLookup
        {
            private RuntimeTypeHandle _genericTypeDefinitionHandle;
            private RuntimeTypeHandle[] _genericTypeArgumentHandles;

            internal HandleBasedGenericTypeLookup(DefType typeToLookup) : base(typeToLookup)
            {
                Debug.Assert(typeToLookup != null);
                _genericTypeDefinitionHandle = _typeToLookup.GetTypeDefinition().RuntimeTypeHandle;
                // _genericTypeArgumentHandles not initialized here to avoid allocation of new array (and it's not used if we initialize _typeToLookup).
            }

            internal HandleBasedGenericTypeLookup(RuntimeTypeHandle genericTypeDefinitionHandle, RuntimeTypeHandle[] genericTypeArgumentHandles) : base(null)
            {
                Debug.Assert(genericTypeArgumentHandles != null);
                _genericTypeDefinitionHandle = genericTypeDefinitionHandle;
                _genericTypeArgumentHandles = genericTypeArgumentHandles;
            }

            internal override int LookupHashCode()
            {
                return _typeToLookup != null ? _typeToLookup.GetHashCode() : TypeHashingAlgorithms.ComputeGenericInstanceHashCode(_genericTypeDefinitionHandle.GetHashCode(), _genericTypeArgumentHandles);
            }

            internal override bool MatchParsedEntry(RuntimeTypeHandle tentativeType)
            {
                RuntimeTypeHandle parsedTypeDefinitionHandle = RuntimeAugments.GetGenericDefinition(tentativeType);
                if (!parsedTypeDefinitionHandle.Equals(_genericTypeDefinitionHandle))
                    return false;

                int lookupArity = (_typeToLookup != null ? _typeToLookup.Instantiation.Length : _genericTypeArgumentHandles.Length);

                for (int i = 0; i < lookupArity; i++)
                {
                    RuntimeTypeHandle parsedArg = RuntimeAugments.GetGenericArgument(tentativeType, i);
                    RuntimeTypeHandle lookupArg = (_typeToLookup != null ? _typeToLookup.Instantiation[i].RuntimeTypeHandle : _genericTypeArgumentHandles[i]);
                    if (!parsedArg.Equals(lookupArg))
                        return false;
                }

                return true;
            }

            internal override bool MatchGenericTypeEntry(GenericTypeEntry entry)
            {
                if (!entry._genericTypeDefinitionHandle.Equals(_genericTypeDefinitionHandle))
                    return false;

                if (entry._genericTypeArgumentHandles == null)
                    return false;

                if (_typeToLookup != null)
                {
                    int expectedArity = _typeToLookup.Instantiation.Length;

                    if (entry._genericTypeArgumentHandles.Length != expectedArity)
                        return false;

                    for (int i = 0; i < expectedArity; i++)
                        if (!entry._genericTypeArgumentHandles[i].Equals(_typeToLookup.Instantiation[i].RuntimeTypeHandle))
                            return false;
                }
                else
                {
                    if (entry._genericTypeArgumentHandles.Length != _genericTypeArgumentHandles.Length)
                        return false;

                    for (int i = 0; i < _genericTypeArgumentHandles.Length; i++)
                        if (!entry._genericTypeArgumentHandles[i].Equals(_genericTypeArgumentHandles[i]))
                            return false;
                }

                return true;
            }
        }

        internal struct LazyDictionaryContext : IEquatable<LazyDictionaryContext>
        {
            public IntPtr _context;
            public IntPtr _signature;

            public override bool Equals(object obj)
            {
                if (!(obj is LazyDictionaryContext))
                    return false;
                return Equals((LazyDictionaryContext)obj);
            }

            public bool Equals(LazyDictionaryContext other)
            {
                return _context == other._context && _signature == other._signature;
            }

            public override int GetHashCode()
            {
                return _context.GetHashCode() ^ _signature.GetHashCode();
            }
        }

        private DynamicGenericTypesHashtable _dynamicGenericTypes = new DynamicGenericTypesHashtable();
        private LowLevelDictionary<LazyDictionaryContext, IntPtr> _lazyGenericDictionaries = new LowLevelDictionary<LazyDictionaryContext, IntPtr>();


        //
        // Return a generic type instantiation using the runtime type system. If the underlying runtime type system does not support
        // this operation, return false.
        //
        internal bool TryLookupConstructedGenericTypeForComponents(GenericTypeLookupData lookupData, out RuntimeTypeHandle runtimeTypeHandle)
        {
            if (!TryGetStaticGenericTypeForComponents(lookupData, out runtimeTypeHandle))
                if (!TryGetDynamicGenericTypeForComponents(lookupData, out runtimeTypeHandle))
                    return false;

            return true;
        }

        public bool TryLookupConstructedGenericTypeForComponents(RuntimeTypeHandle genericTypeDefinitionHandle, RuntimeTypeHandle[] genericTypeArgumentHandles, out RuntimeTypeHandle runtimeTypeHandle)
        {
            return TryLookupConstructedGenericTypeForComponents(new HandleBasedGenericTypeLookup(genericTypeDefinitionHandle, genericTypeArgumentHandles), out runtimeTypeHandle);
        }

        public bool TryLookupConstructedLazyDictionaryForContext(IntPtr context, IntPtr signature, out IntPtr dictionary)
        {
            Debug.Assert(_typeLoaderLock.IsAcquired);
            return _lazyGenericDictionaries.TryGetValue(new LazyDictionaryContext { _context = context, _signature = signature }, out dictionary);
        }

        #region Privates
        private unsafe bool TryGetDynamicGenericTypeForComponents(GenericTypeLookupData lookupData, out RuntimeTypeHandle runtimeTypeHandle)
        {
            runtimeTypeHandle = default(RuntimeTypeHandle);

            using (LockHolder.Hold(_dynamicGenericsLock))
            {
                GenericTypeEntry entry;
                if (!_dynamicGenericTypes.TryGetValue(lookupData, out entry))
                    return false;

                if (!entry._isRegisteredSuccessfully)
                    return false;

                runtimeTypeHandle = entry._instantiatedTypeHandle;
                return true;
            }
        }

        internal static unsafe bool TryGetStaticGenericTypeForComponents(GenericTypeLookupData lookupData, out RuntimeTypeHandle runtimeTypeHandle)
        {
            // Search the hashtable for a generic instantiation match
            // TODO multi-file: consider whether we can limit the search somehow,
            // i.e. not look at all the modules

            runtimeTypeHandle = default(RuntimeTypeHandle);

            NativeHashtable genericsHashtable;
            ExternalReferencesTable externalReferencesLookup;

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                if (!GetHashtableFromBlob(module, ReflectionMapBlob.GenericsHashtable, out genericsHashtable, out externalReferencesLookup))
                    continue;

                int lookupHashcode = lookupData.LookupHashCode();
                var enumerator = genericsHashtable.Lookup(lookupHashcode);

                NativeParser entryParser;
                while (!(entryParser = enumerator.GetNext()).IsNull)
                {
                    RuntimeTypeHandle tentativeType = externalReferencesLookup.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());

                    if (!lookupData.MatchParsedEntry(tentativeType))
                        continue;

                    runtimeTypeHandle = tentativeType;
                    Debug.Assert(RuntimeAugments.IsGenericType(runtimeTypeHandle));

                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
