// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Internal.NativeFormat;
using Internal.Runtime.Augments;

namespace Internal.Runtime.TypeLoader
{
    public sealed partial class TypeLoaderEnvironment
    {
        // To keep the synchronization simple, we execute all TLS registration/lookups under a global lock
        private Lock _threadStaticsLock = new Lock(useTrivialWaits: true);

        // Counter to keep track of generated offsets for TLS cells of dynamic types;
        private LowLevelDictionary<IntPtr, uint> _maxThreadLocalIndex = new LowLevelDictionary<IntPtr, uint>();
        private LowLevelDictionary<IntPtr, LowLevelDictionary<uint, IntPtr>> _dynamicGenericsThreadStaticDescs = new LowLevelDictionary<IntPtr, LowLevelDictionary<uint, IntPtr>>();

        // Various functions in static access need to create permanent pointers for use by thread static lookup.
        #region GC/Non-GC Statics
        /// <summary>
        /// Get a pointer to the nongc static field data of a type. This function works for dynamic
        /// types, reflectable types, and for all generic types
        /// </summary>
        public IntPtr TryGetNonGcStaticFieldData(RuntimeTypeHandle runtimeTypeHandle)
        {
            unsafe
            {
                // Non-generic, non-dynamic static data is found via the FieldAccessMap
                MethodTable* typeAsEEType = runtimeTypeHandle.ToEETypePtr();
                // Non-generic, non-dynamic types need special handling.
                Debug.Assert(typeAsEEType->IsDynamicType || typeAsEEType->IsGeneric);
            }

            // Search hashtable for static entry
            ExternalReferencesTable staticInfoLookup;
            var parser = GetStaticInfo(runtimeTypeHandle, out staticInfoLookup);
            if (!parser.IsNull)
            {
                var index = parser.GetUnsignedForBagElementKind(BagElementKind.NonGcStaticData);

                return index.HasValue ? staticInfoLookup.GetIntPtrFromIndex(index.Value) : IntPtr.Zero;
            }

            // Not found in hashtable... must be a dynamically created type
            Debug.Assert(runtimeTypeHandle.IsDynamicType());
            unsafe
            {
                MethodTable* typeAsEEType = runtimeTypeHandle.ToEETypePtr();
                if ((typeAsEEType->RareFlags & EETypeRareFlags.IsDynamicTypeWithNonGcStatics) != 0)
                {
                    return typeAsEEType->DynamicNonGcStaticsData;
                }
            }

            // Type has no non-GC statics
            return IntPtr.Zero;
        }

        /// <summary>
        /// Get a pointer to the gc static field data of a type. This function works for dynamic
        /// types, reflectable types, and for all generic types
        /// </summary>
        public IntPtr TryGetGcStaticFieldData(RuntimeTypeHandle runtimeTypeHandle)
        {
            unsafe
            {
                // Non-generic, non-dynamic static data is found via the FieldAccessMap
                MethodTable* typeAsEEType = runtimeTypeHandle.ToEETypePtr();
                // Non-generic, non-dynamic types need special handling.
                Debug.Assert(typeAsEEType->IsDynamicType || typeAsEEType->IsGeneric);
            }

            // Search hashtable for static entry
            ExternalReferencesTable staticInfoLookup;
            var parser = GetStaticInfo(runtimeTypeHandle, out staticInfoLookup);
            if (!parser.IsNull)
            {
                var index = parser.GetUnsignedForBagElementKind(BagElementKind.GcStaticData);

                return index.HasValue ? staticInfoLookup.GetIntPtrFromIndex(index.Value) : IntPtr.Zero;
            }

            // Not found in hashtable... must be a dynamically created type
            Debug.Assert(runtimeTypeHandle.IsDynamicType());
            unsafe
            {
                MethodTable* typeAsEEType = runtimeTypeHandle.ToEETypePtr();
                if ((typeAsEEType->RareFlags & EETypeRareFlags.IsDynamicTypeWithGcStatics) != 0)
                {
                    return typeAsEEType->DynamicGcStaticsData;
                }
            }

            // Type has no GC statics
            return IntPtr.Zero;
        }
        #endregion


        #region Thread Statics
        /// <summary>
        /// Get a pointer to a pointer to the thread static field data of a type. This function works for all generic types
        /// </summary>
        public IntPtr TryGetThreadStaticFieldData(RuntimeTypeHandle runtimeTypeHandle)
        {
            unsafe
            {
                // Non-generic, non-dynamic static data is found via the FieldAccessMap
                MethodTable* typeAsEEType = runtimeTypeHandle.ToEETypePtr();
                // Non-generic, non-dynamic types need special handling.
                Debug.Assert(typeAsEEType->IsDynamicType || typeAsEEType->IsGeneric);
            }

            // Search hashtable for static entry
            ExternalReferencesTable staticInfoLookup;
            var parser = GetStaticInfo(runtimeTypeHandle, out staticInfoLookup);
            if (!parser.IsNull)
            {
                var index = parser.GetUnsignedForBagElementKind(BagElementKind.ThreadStaticIndex);

                return index.HasValue ? staticInfoLookup.GetIntPtrFromIndex(index.Value) : IntPtr.Zero;
            }

            // Not found in hashtable... might be a dynamically created type
            if (runtimeTypeHandle.IsDynamicType())
            {
                unsafe
                {
                    MethodTable* typeAsEEType = runtimeTypeHandle.ToEETypePtr();
                    if (typeAsEEType->DynamicThreadStaticsIndex != IntPtr.Zero)
                        return typeAsEEType->DynamicThreadStaticsIndex;
                }
            }

            // Type has no GC statics
            return IntPtr.Zero;
        }

        public IntPtr GetThreadStaticGCDescForDynamicType(TypeManagerHandle typeManagerHandle, uint index)
        {
            using (_threadStaticsLock.EnterScope())
            {
                return _dynamicGenericsThreadStaticDescs[typeManagerHandle.GetIntPtrUNSAFE()][index];
            }
        }

        public uint GetNextThreadStaticsOffsetValue(TypeManagerHandle typeManagerHandle)
        {
            if (!_maxThreadLocalIndex.TryGetValue(typeManagerHandle.GetIntPtrUNSAFE(), out uint result))
                result = (uint)RuntimeAugments.GetHighestStaticThreadStaticIndex(typeManagerHandle);

            _maxThreadLocalIndex[typeManagerHandle.GetIntPtrUNSAFE()] = checked(++result);

            return result;
        }

        public void RegisterDynamicThreadStaticsInfo(RuntimeTypeHandle runtimeTypeHandle, uint offsetValue, IntPtr gcDesc)
        {
            bool registered = false;
            Debug.Assert(offsetValue != 0 && runtimeTypeHandle.IsDynamicType());

            IntPtr typeManager = runtimeTypeHandle.GetTypeManager().GetIntPtrUNSAFE();

            _threadStaticsLock.Enter();
            try
            {
                if (!_dynamicGenericsThreadStaticDescs.TryGetValue(typeManager, out LowLevelDictionary<uint, IntPtr> gcDescs))
                {
                    _dynamicGenericsThreadStaticDescs.Add(typeManager, gcDescs = new LowLevelDictionary<uint, IntPtr>());
                }
                gcDescs.Add(offsetValue, gcDesc);
                registered = true;
            }
            finally
            {
                if (!registered)
                {
                    if (_dynamicGenericsThreadStaticDescs.TryGetValue(typeManager, out LowLevelDictionary<uint, IntPtr> gcDescs))
                    {
                        gcDescs.Remove(offsetValue);
                    }
                }

                _threadStaticsLock.Exit();
            }
        }
        #endregion


        #region Privates
        // get the statics hash table, external references, and static info table for a module
        // TODO multi-file: consider whether we want to cache this info
        private static unsafe bool GetStaticsInfoHashtable(NativeFormatModuleInfo module, out NativeHashtable staticsInfoHashtable, out ExternalReferencesTable externalReferencesLookup, out ExternalReferencesTable staticInfoLookup)
        {
            byte* pBlob;
            uint cbBlob;

            staticsInfoHashtable = default(NativeHashtable);
            externalReferencesLookup = default(ExternalReferencesTable);
            staticInfoLookup = default(ExternalReferencesTable);

            // Load statics info hashtable
            if (!module.TryFindBlob(ReflectionMapBlob.StaticsInfoHashtable, out pBlob, out cbBlob))
                return false;
            NativeReader reader = new NativeReader(pBlob, cbBlob);
            NativeParser parser = new NativeParser(reader, 0);

            if (!externalReferencesLookup.InitializeNativeReferences(module))
                return false;

            if (!staticInfoLookup.InitializeNativeStatics(module))
                return false;

            staticsInfoHashtable = new NativeHashtable(parser);

            return true;
        }

        private static NativeParser GetStaticInfo(RuntimeTypeHandle instantiatedType, out ExternalReferencesTable staticsInfoLookup)
        {
            TypeManagerHandle moduleHandle = RuntimeAugments.GetModuleFromTypeHandle(instantiatedType);
            NativeFormatModuleInfo module = ModuleList.Instance.GetModuleInfoByHandle(moduleHandle);
            NativeHashtable staticsInfoHashtable;
            ExternalReferencesTable externalReferencesLookup;
            if (!GetStaticsInfoHashtable(module, out staticsInfoHashtable, out externalReferencesLookup, out staticsInfoLookup))
                return default(NativeParser);

            int lookupHashcode = instantiatedType.GetHashCode();
            var enumerator = staticsInfoHashtable.Lookup(lookupHashcode);

            NativeParser entryParser;
            while (!(entryParser = enumerator.GetNext()).IsNull)
            {
                RuntimeTypeHandle parsedInstantiatedType = externalReferencesLookup.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());

                if (!parsedInstantiatedType.Equals(instantiatedType))
                    continue;

                return entryParser;
            }

            return default(NativeParser);
        }
        #endregion
    }
}
