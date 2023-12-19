// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;
using System.Threading;

using Internal.NativeFormat;
using Internal.Runtime.CompilerServices;
using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    public sealed partial class TypeLoaderEnvironment
    {
        internal class GenericMethodEntry
        {
            private int? _hashCode;
            public bool _isRegisteredSuccessfully;
            public IntPtr _methodDictionary;
            public RuntimeTypeHandle _declaringTypeHandle;
            public MethodNameAndSignature _methodNameAndSignature;
            public RuntimeTypeHandle[] _genericMethodArgumentHandles;

            public override int GetHashCode()
            {
                if (!_hashCode.HasValue)
                {
                    _hashCode = _declaringTypeHandle.GetHashCode() ^ TypeHashingAlgorithms.ComputeGenericInstanceHashCode(TypeHashingAlgorithms.ComputeNameHashCode(_methodNameAndSignature.Name), _genericMethodArgumentHandles);
                }
                return _hashCode.Value;
            }

            public override bool Equals(object obj)
            {
                // There are no scenarios where we call .Equals to check the componentized equality (we explicitly use IsEqualToEntryByComponentsComparison for that),
                // so making sure we revert to the reference equality in case someone calls here.
                return base.Equals(obj);
            }

            public virtual bool IsEqualToEntryByComponentsComparison(GenericMethodEntry other)
            {
                if (!other._declaringTypeHandle.Equals(_declaringTypeHandle))
                    return false;

                if (!other._methodNameAndSignature.Equals(_methodNameAndSignature))
                    return false;

                if (other._genericMethodArgumentHandles == null)
                    return false;

                if (other._genericMethodArgumentHandles.Length != _genericMethodArgumentHandles.Length)
                    return false;

                for (int i = 0; i < _genericMethodArgumentHandles.Length; i++)
                    if (!other._genericMethodArgumentHandles[i].Equals(_genericMethodArgumentHandles[i]))
                        return false;

                return true;
            }
        }

        internal class DynamicGenericMethodsHashtable : LockFreeReaderHashtable<GenericMethodLookupData, GenericMethodEntry>
        {
            protected override int GetKeyHashCode(GenericMethodLookupData key)
            {
                return key.LookupHashCode();
            }
            protected override bool CompareKeyToValue(GenericMethodLookupData key, GenericMethodEntry value)
            {
                return key.MatchGenericMethodEntry(value);
            }

            protected override int GetValueHashCode(GenericMethodEntry value)
            {
                return value.GetHashCode();
            }

            protected override bool CompareValueToValue(GenericMethodEntry value1, GenericMethodEntry value2)
            {
                // Comparisons should *only* be done using the generic method components
                return value1.IsEqualToEntryByComponentsComparison(value2);
            }

            protected override GenericMethodEntry CreateValueFromKey(GenericMethodLookupData key)
            {
                // Feature not used by the TypeBuilder
                throw NotImplemented.ByDesign;
            }
        }

        internal class DynamicGenericMethodComponentsHashtable : LockFreeReaderHashtable<IntPtr, GenericMethodEntry>
        {
            protected override int GetKeyHashCode(IntPtr key)
            {
                return key.GetHashCode();
            }
            protected override bool CompareKeyToValue(IntPtr key, GenericMethodEntry value)
            {
                return key.Equals(value._methodDictionary);
            }

            protected override int GetValueHashCode(GenericMethodEntry value)
            {
                Debug.Assert(value._methodDictionary != IntPtr.Zero);
                return value._methodDictionary.GetHashCode();
            }

            protected override bool CompareValueToValue(GenericMethodEntry value1, GenericMethodEntry value2)
            {
                // Comparisons should *only* be done using the generic method components
                return value1.IsEqualToEntryByComponentsComparison(value2);
            }

            protected override GenericMethodEntry CreateValueFromKey(IntPtr key)
            {
                // Feature not used by the TypeBuilder
                throw NotImplemented.ByDesign;
            }
        }

        internal abstract class GenericMethodLookupData
        {
            internal abstract int LookupHashCode();
            internal abstract bool MatchParsedEntry(ref NativeParser entryParser, ref ExternalReferencesTable externalReferencesLookup, TypeManagerHandle moduleHandle);
            internal abstract bool MatchGenericMethodEntry(GenericMethodEntry entry);
        }
        internal class MethodDescBasedGenericMethodLookup : GenericMethodLookupData
        {
            protected InstantiatedMethod _methodToLookup;

            internal MethodDescBasedGenericMethodLookup(InstantiatedMethod methodToLookup) { _methodToLookup = methodToLookup; }

            internal override int LookupHashCode() { return _methodToLookup.GetHashCode(); }

            internal override bool MatchParsedEntry(ref NativeParser entryParser, ref ExternalReferencesTable externalReferencesLookup, TypeManagerHandle moduleHandle)
            {
                //
                // Entries read from the hashtable are loaded as GenericMethodDescs, and compared to the input.
                // This lookup is slower than the lookups using RuntimeTypeHandles, but can handle cases where we don't have
                // RuntimeTypeHandle values for all of the components of the input GenericMethodDesc, but still need to look it up in case the
                // method dictionary statically really exists
                //
                TypeSystemContext context = _methodToLookup.Context;

                RuntimeTypeHandle parsedDeclaringTypeHandle = externalReferencesLookup.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());

                // Hash table names / sigs are indirected through to the native layout info
                MethodNameAndSignature nameAndSignature = TypeLoaderEnvironment.Instance.GetMethodNameAndSignatureFromNativeLayoutOffset(moduleHandle, entryParser.GetUnsigned());

                RuntimeTypeHandle[] parsedArgsHandles = GetTypeSequence(ref externalReferencesLookup, ref entryParser);

                DefType parsedDeclaringType = context.ResolveRuntimeTypeHandle(parsedDeclaringTypeHandle) as DefType;
                Instantiation parsedArgs = context.ResolveRuntimeTypeHandles(parsedArgsHandles);
                InstantiatedMethod parsedGenericMethod = (InstantiatedMethod)context.ResolveGenericMethodInstantiation(false, parsedDeclaringType, nameAndSignature, parsedArgs, IntPtr.Zero, false);

                return parsedGenericMethod == _methodToLookup;
            }

            internal override bool MatchGenericMethodEntry(GenericMethodEntry entry)
            {
                TypeSystemContext context = _methodToLookup.Context;

                DefType parsedDeclaringType = context.ResolveRuntimeTypeHandle(entry._declaringTypeHandle) as DefType;
                Instantiation parsedArgs = context.ResolveRuntimeTypeHandles(entry._genericMethodArgumentHandles);
                InstantiatedMethod parsedGenericMethod = (InstantiatedMethod)context.ResolveGenericMethodInstantiation(false, parsedDeclaringType, entry._methodNameAndSignature, parsedArgs, IntPtr.Zero, false);

                return parsedGenericMethod == _methodToLookup;
            }
        }

        private DynamicGenericMethodsHashtable _dynamicGenericMethods = new DynamicGenericMethodsHashtable();
        private DynamicGenericMethodComponentsHashtable _dynamicGenericMethodComponents = new DynamicGenericMethodComponentsHashtable();

        internal bool TryLookupGenericMethodDictionary(GenericMethodLookupData lookupData, out IntPtr result)
        {
            if (!TryGetStaticGenericMethodDictionary(lookupData, out result))
                if (!TryGetDynamicGenericMethodDictionary(lookupData, out result))
                    return false;

            return true;
        }

        public bool TryGetGenericMethodComponents(IntPtr methodDictionary, out RuntimeTypeHandle declaringType, out MethodNameAndSignature nameAndSignature, out RuntimeTypeHandle[] genericMethodArgumentHandles)
        {
            if (!TryGetDynamicGenericMethodComponents(methodDictionary, out declaringType, out nameAndSignature, out genericMethodArgumentHandles))
            {
                if (!TryGetStaticGenericMethodComponents(methodDictionary, out declaringType, out TypeManagerHandle typeManager, out uint nameAndSigOffset, out genericMethodArgumentHandles))
                    return false;

                nameAndSignature = TypeLoaderEnvironment.Instance.GetMethodNameAndSignatureFromNativeLayoutOffset(typeManager, nameAndSigOffset);
            }

            return true;
        }

        public static bool TryGetGenericMethodComponents(IntPtr methodDictionary, out RuntimeTypeHandle declaringType, out RuntimeTypeHandle[] genericMethodArgumentHandles)
        {
            TypeLoaderEnvironment instance = TypeLoaderEnvironment.InstanceOrNull;
            if (instance == null || !instance.TryGetDynamicGenericMethodComponents(methodDictionary, out declaringType, out _, out genericMethodArgumentHandles))
                if (!TryGetStaticGenericMethodComponents(methodDictionary, out declaringType, out _, out _, out genericMethodArgumentHandles))
                    return false;

            return true;
        }


        public bool TryLookupExactMethodPointer(InstantiatedMethod method, out IntPtr result)
        {
            int lookupHashcode = method.OwningType.GetHashCode();

            NativeHashtable hashtable;
            ExternalReferencesTable externalReferencesLookup;

            MethodDescBasedGenericMethodLookup lookupData = new MethodDescBasedGenericMethodLookup(method);

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                if (!GetHashtableFromBlob(module, ReflectionMapBlob.ExactMethodInstantiationsHashtable, out hashtable, out externalReferencesLookup))
                    continue;

                var enumerator = hashtable.Lookup(lookupHashcode);

                NativeParser entryParser;
                while (!(entryParser = enumerator.GetNext()).IsNull)
                {
                    if (!lookupData.MatchParsedEntry(ref entryParser, ref externalReferencesLookup, module.Handle))
                        continue;

                    // We found a match
                    result = externalReferencesLookup.GetIntPtrFromIndex(entryParser.GetUnsigned());
                    return true;
                }
            }

            result = IntPtr.Zero;
            return false;
        }

        // This method computes the method pointer and dictionary pointer for a GVM.
        // Inputs:
        //      - method: the GVM whose pointer and dictionary to retrieve
        // Outputs:
        //      - methodPointer: pointer to the GVM's implementation
        //      - dictionaryPointer: (if applicable) pointer to the dictionary to be used with the GVM call
        public bool TryGetGenericVirtualMethodPointer(InstantiatedMethod method, out IntPtr methodPointer, out IntPtr dictionaryPointer)
        {
            if (!method.CanShareNormalGenericCode())
            {
                // First see if we can find an exact method implementation for the GVM (avoid using USG implementations if we can,
                // because USG code is much slower).
                if (TryLookupExactMethodPointer(method, out methodPointer))
                {
                    Debug.Assert(methodPointer != IntPtr.Zero);
                    dictionaryPointer = IntPtr.Zero;
                    return true;
                }
            }

            // If we cannot find an exact method entry point, look for an equivalent template and compute the generic dictionary
            InstantiatedMethod templateMethod = TemplateLocator.TryGetGenericMethodTemplate(method, out _, out _);
            if (templateMethod == null)
            {
                methodPointer = default;
                dictionaryPointer = default;
                return false;
            }

            methodPointer = templateMethod.IsCanonicalMethod(CanonicalFormKind.Universal) ?
                templateMethod.UsgFunctionPointer :
                templateMethod.FunctionPointer;

            if (!TryLookupGenericMethodDictionary(new MethodDescBasedGenericMethodLookup(method), out dictionaryPointer))
            {
                using (_typeLoaderLock.EnterScope())
                {
                    // Now that we hold the lock, we may find that existing types can now find
                    // their associated RuntimeTypeHandle. Flush the type builder states as a way
                    // to force the reresolution of RuntimeTypeHandles which couldn't be found before.
                    method.Context.FlushTypeBuilderStates();

                    if (!TypeBuilder.TryBuildGenericMethod(method, out dictionaryPointer))
                    {
                        return false;
                    }
                }
            }

            Debug.Assert(methodPointer != IntPtr.Zero && dictionaryPointer != IntPtr.Zero);
            return true;
        }

#region Privates
        private bool TryGetDynamicGenericMethodDictionary(GenericMethodLookupData lookupData, out IntPtr result)
        {
            result = IntPtr.Zero;

            using (_dynamicGenericsLock.EnterScope())
            {
                GenericMethodEntry entry;
                if (!_dynamicGenericMethods.TryGetValue(lookupData, out entry))
                    return false;

                if (!entry._isRegisteredSuccessfully)
                    return false;

                result = entry._methodDictionary;
                return true;
            }
        }
        private static bool TryGetStaticGenericMethodDictionary(GenericMethodLookupData lookupData, out IntPtr result)
        {
            // Search the hashtable for a generic instantiation match

            ExternalReferencesTable externalReferencesLookup;
            NativeHashtable genericMethodsHashtable;

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                if (!GetHashtableFromBlob(module, ReflectionMapBlob.GenericMethodsHashtable, out genericMethodsHashtable, out externalReferencesLookup))
                    continue;

                int lookupHashcode = lookupData.LookupHashCode();
                var enumerator = genericMethodsHashtable.Lookup(lookupHashcode);

                NativeParser entryParser;
                while (!(entryParser = enumerator.GetNext()).IsNull)
                {
                    uint dictionaryIndex = entryParser.GetUnsigned();

                    if (!lookupData.MatchParsedEntry(ref entryParser, ref externalReferencesLookup, module.Handle))
                        continue;

                    // Current entry matched all inputs, return success
                    result = externalReferencesLookup.GetIntPtrFromIndex(dictionaryIndex);
                    return true;
                }
            }

            result = IntPtr.Zero;
            return false;
        }

        private bool TryGetDynamicGenericMethodComponents(IntPtr methodDictionary, out RuntimeTypeHandle declaringType, out MethodNameAndSignature methodNameAndSignature, out RuntimeTypeHandle[] genericMethodArgumentHandles)
        {
            declaringType = default(RuntimeTypeHandle);
            methodNameAndSignature = null;
            genericMethodArgumentHandles = null;

            using (_dynamicGenericsLock.EnterScope())
            {
                GenericMethodEntry entry;
                if (!_dynamicGenericMethodComponents.TryGetValue(methodDictionary, out entry))
                    return false;

                if (!entry._isRegisteredSuccessfully)
                    return false;

                declaringType = entry._declaringTypeHandle;
                methodNameAndSignature = entry._methodNameAndSignature;
                genericMethodArgumentHandles = entry._genericMethodArgumentHandles;
                return true;
            }
        }
        private static unsafe bool TryGetStaticGenericMethodComponents(IntPtr methodDictionary, out RuntimeTypeHandle declaringType, out TypeManagerHandle typeManager, out uint nameAndSigOffset, out RuntimeTypeHandle[] genericMethodArgumentHandles)
        {
            // Generic method dictionaries have a header that has the hash code in it. Locate the header
            IntPtr dictionaryHeader = IntPtr.Subtract(methodDictionary, IntPtr.Size);
            int lookupHashcode = *(int*)dictionaryHeader;

            ExternalReferencesTable externalReferencesLookup;
            NativeHashtable genericMethodsHashtable;

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                if (!GetHashtableFromBlob(module, ReflectionMapBlob.GenericMethodsHashtable, out genericMethodsHashtable, out externalReferencesLookup))
                    continue;

                var enumerator = genericMethodsHashtable.Lookup(lookupHashcode);

                NativeParser entryParser;
                while (!(entryParser = enumerator.GetNext()).IsNull)
                {
                    // Is this entry the dictionary we are looking for?
                    uint dictionaryIndex = entryParser.GetUnsigned();
                    IntPtr parsedMethodDictionary = externalReferencesLookup.GetIntPtrFromIndex(dictionaryIndex);
                    if (parsedMethodDictionary != methodDictionary)
                        continue;

                    // We have a match - fill in the results
                    declaringType = externalReferencesLookup.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());

                    typeManager = module.Handle;
                    nameAndSigOffset = entryParser.GetUnsigned();

                    uint arity = entryParser.GetSequenceCount();
                    genericMethodArgumentHandles = new RuntimeTypeHandle[arity];

                    for (int i = 0; i < arity; i++)
                    {
                        genericMethodArgumentHandles[i] = externalReferencesLookup.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    }

                    return true;
                }
            }

            declaringType = default(RuntimeTypeHandle);
            typeManager = default;
            nameAndSigOffset = 0;
            genericMethodArgumentHandles = null;
            return false;
        }

#endregion
    }
}
