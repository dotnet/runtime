// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Text;

using System.Reflection.Runtime.General;

using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.TypeSystem;
using Internal.TypeSystem.NativeFormat;
using Internal.TypeSystem.NoMetadata;

namespace Internal.Runtime.TypeLoader
{
    using DynamicGenericsRegistrationData = TypeLoaderEnvironment.DynamicGenericsRegistrationData;
    using GenericTypeEntry = TypeLoaderEnvironment.GenericTypeEntry;
    using TypeEntryToRegister = TypeLoaderEnvironment.TypeEntryToRegister;
    using GenericMethodEntry = TypeLoaderEnvironment.GenericMethodEntry;
    using HandleBasedGenericTypeLookup = TypeLoaderEnvironment.HandleBasedGenericTypeLookup;
    using DefTypeBasedGenericTypeLookup = TypeLoaderEnvironment.DefTypeBasedGenericTypeLookup;
    using HandleBasedGenericMethodLookup = TypeLoaderEnvironment.HandleBasedGenericMethodLookup;
    using MethodDescBasedGenericMethodLookup = TypeLoaderEnvironment.MethodDescBasedGenericMethodLookup;
#if FEATURE_UNIVERSAL_GENERICS
    using ThunkKind = CallConverterThunk.ThunkKind;
#endif
    using VTableSlotMapper = TypeBuilderState.VTableSlotMapper;

    internal static class LowLevelListExtensions
    {
        public static void Expand<T>(this LowLevelList<T> list, int count)
        {
            if (list.Capacity < count)
                list.Capacity = count;

            while (list.Count < count)
                list.Add(default(T));
        }

        public static bool HasSetBits(this LowLevelList<bool> list)
        {
            for (int index = 0; index < list.Count; index++)
            {
                if (list[index])
                    return true;
            }

            return false;
        }
    }

    [Flags]
    internal enum FieldLoadState
    {
        None = 0,
        Instance = 1,
        Statics = 2,
    }

    public static class TypeBuilderApi
    {
        public static void ResolveMultipleCells(GenericDictionaryCell [] cells, out IntPtr[] fixups)
        {
            TypeBuilder.ResolveMultipleCells(cells, out fixups);
        }
    }


    internal class TypeBuilder
    {
        public TypeBuilder()
        {
            TypeLoaderEnvironment.Instance.VerifyTypeLoaderLockHeld();
        }

        private const int MinimumValueTypeSize = 0x1;

        /// <summary>
        /// The StaticClassConstructionContext for a type is encoded in the negative space
        /// of the NonGCStatic fields of a type.
        /// </summary>
        public static readonly unsafe int ClassConstructorOffset = -sizeof(System.Runtime.CompilerServices.StaticClassConstructionContext);

        private LowLevelList<TypeDesc> _typesThatNeedTypeHandles = new LowLevelList<TypeDesc>();

        private LowLevelList<InstantiatedMethod> _methodsThatNeedDictionaries = new LowLevelList<InstantiatedMethod>();

        private LowLevelList<TypeDesc> _typesThatNeedPreparation;

        private object _epoch = new object();

#if DEBUG
        private bool _finalTypeBuilding;
#endif

        // Helper exception to abort type building if we do not find the generic type template
        internal class MissingTemplateException : Exception
        {
            public MissingTemplateException()
                // Cannot afford calling into resource manager from here, even to get the default message for System.Exception.
                // This exception is always caught and rethrown as something more user friendly.
                : base("Template is missing") { }
        }


        private static bool CheckAllHandlesValidForMethod(MethodDesc method)
        {
            if (!method.OwningType.RetrieveRuntimeTypeHandleIfPossible())
                return false;

            for (int i = 0; i < method.Instantiation.Length; i++)
                if (!method.Instantiation[i].RetrieveRuntimeTypeHandleIfPossible())
                    return false;

            return true;
        }

        internal static bool RetrieveExactFunctionPointerIfPossible(MethodDesc method, out IntPtr result)
        {
            result = IntPtr.Zero;

            if (!method.IsNonSharableMethod || !CheckAllHandlesValidForMethod(method))
                return false;

            RuntimeTypeHandle[] genMethodArgs = method.Instantiation.Length > 0 ? new RuntimeTypeHandle[method.Instantiation.Length] : Empty<RuntimeTypeHandle>.Array;
            for (int i = 0; i < method.Instantiation.Length; i++)
                genMethodArgs[i] = method.Instantiation[i].RuntimeTypeHandle;

            return TypeLoaderEnvironment.Instance.TryLookupExactMethodPointerForComponents(method.OwningType.RuntimeTypeHandle, method.NameAndSignature, genMethodArgs, out result);
        }

        internal static bool RetrieveMethodDictionaryIfPossible(InstantiatedMethod method)
        {
            if (method.RuntimeMethodDictionary != IntPtr.Zero)
                return true;

            bool allHandlesValid = CheckAllHandlesValidForMethod(method);

            TypeLoaderLogger.WriteLine("Looking for method dictionary for method " + method.ToString() + " ... " + (allHandlesValid ? "(All type arg handles valid)" : ""));

            IntPtr methodDictionary;

            if ((allHandlesValid && TypeLoaderEnvironment.Instance.TryLookupGenericMethodDictionaryForComponents(new HandleBasedGenericMethodLookup(method), out methodDictionary)) ||
                 (!allHandlesValid && TypeLoaderEnvironment.Instance.TryLookupGenericMethodDictionaryForComponents(new MethodDescBasedGenericMethodLookup(method), out methodDictionary)))
            {
                TypeLoaderLogger.WriteLine("Found DICT = " + methodDictionary.LowLevelToString() + " for method " + method.ToString());
                method.AssociateWithRuntimeMethodDictionary(methodDictionary);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Register the type for preparation. The preparation will be done once the current type is prepared.
        /// This is the prefered way to get a dependent type prepared because of it avoids issues with cycles and recursion.
        /// </summary>
        public void RegisterForPreparation(TypeDesc type)
        {
            TypeLoaderLogger.WriteLine("Register for preparation " + type.ToString() + " ...");

            // If this type has type handle, do nothing and return
            if (type.RetrieveRuntimeTypeHandleIfPossible())
                return;

            var state = type.GetOrCreateTypeBuilderState();

            // If this type was already inspected, do nothing and return.
            if (state.NeedsTypeHandle)
                return;

            state.NeedsTypeHandle = true;

            if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
                return;

            if (_typesThatNeedPreparation == null)
                _typesThatNeedPreparation = new LowLevelList<TypeDesc>();

            _typesThatNeedPreparation.Add(type);
        }

        /// <summary>
        /// Collects all dependencies that need to be created in order to create
        /// the method that was passed in.
        /// </summary>
        public void PrepareMethod(MethodDesc method)
        {
            TypeLoaderLogger.WriteLine("Preparing method " + method.ToString() + " ...");

            RegisterForPreparation(method.OwningType);

            if (method.Instantiation.Length == 0)
                return;

            InstantiatedMethod genericMethod = (InstantiatedMethod)method;

            if (RetrieveMethodDictionaryIfPossible(genericMethod))
                return;

            // If this method was already inspected, do nothing and return
            if (genericMethod.NeedsDictionary)
                return;

            genericMethod.NeedsDictionary = true;

            if (genericMethod.IsCanonicalMethod(CanonicalFormKind.Any))
                return;

            _methodsThatNeedDictionaries.Add(genericMethod);

            foreach (var type in genericMethod.Instantiation)
                RegisterForPreparation(type);

            ParseNativeLayoutInfo(genericMethod);
        }

        private void InsertIntoNeedsTypeHandleList(TypeBuilderState state, TypeDesc type)
        {
            if ((type is DefType) || (type is ArrayType) || (type is PointerType) || (type is ByRefType))
            {
                _typesThatNeedTypeHandles.Add(type);
            }
        }

        /// <summary>
        /// Collects all dependencies that need to be created in order to create
        /// the type that was passed in.
        /// </summary>
        internal void PrepareType(TypeDesc type)
        {
            TypeLoaderLogger.WriteLine("Preparing type " + type.ToString() + " ...");

            TypeBuilderState state = type.GetTypeBuilderStateIfExist();
            bool hasTypeHandle = type.RetrieveRuntimeTypeHandleIfPossible();

            // If this type has type handle, do nothing and return unless we should prepare even in the presence of a type handle
            if (hasTypeHandle)
                return;

            if (state == null)
                state = type.GetOrCreateTypeBuilderState();

            // If this type was already prepared, do nothing unless we are re-preparing it for the purpose of loading the field layout
            if (state.HasBeenPrepared)
            {
                return;
            }

            state.HasBeenPrepared = true;
            state.NeedsTypeHandle = true;

            if (!hasTypeHandle)
            {
                InsertIntoNeedsTypeHandleList(state, type);
            }

            bool noExtraPreparation = false; // Set this to true for types which don't need other types to be prepared. I.e GenericTypeDefinitions

            if (type is DefType)
            {
                DefType typeAsDefType = (DefType)type;

                if (typeAsDefType.HasInstantiation)
                {
                    if (typeAsDefType.IsTypeDefinition)
                    {
                        noExtraPreparation = true;
                    }
                    else
                    {
                        // This call to ComputeTemplate will find the native layout info for the type, and the template
                        // For metadata loaded types, a template will not exist, but we may find the NativeLayout describing the generic dictionary
                        TypeDesc.ComputeTemplate(state, false);

                        Debug.Assert(state.TemplateType == null || (state.TemplateType is DefType && !state.TemplateType.RuntimeTypeHandle.IsNull()));

                        // Collect dependencies

                        // We need the instantiation arguments to register a generic type
                        foreach (var instArg in typeAsDefType.Instantiation)
                            RegisterForPreparation(instArg);

                        // We need the type definition to register a generic type
                        if (type.GetTypeDefinition() is MetadataType)
                            RegisterForPreparation(type.GetTypeDefinition());

                        ParseNativeLayoutInfo(state, type);
                    }
                }

                if (!noExtraPreparation)
                    state.PrepareStaticGCLayout();
            }
            else if (type is ParameterizedType)
            {
                PrepareType(((ParameterizedType)type).ParameterType);

                if (type is ArrayType)
                {
                    ArrayType typeAsArrayType = (ArrayType)type;

                    if (typeAsArrayType.IsSzArray && !typeAsArrayType.ElementType.IsPointer)
                    {
                        TypeDesc.ComputeTemplate(state);
                        Debug.Assert(state.TemplateType != null && state.TemplateType is ArrayType && !state.TemplateType.RuntimeTypeHandle.IsNull());

                        ParseNativeLayoutInfo(state, type);
                    }
                    else
                    {
                        Debug.Assert(typeAsArrayType.IsMdArray || typeAsArrayType.ElementType.IsPointer);
                    }

                    // Assert that non-valuetypes are considered to have pointer size
                    Debug.Assert(typeAsArrayType.ParameterType.IsValueType || state.ComponentSize == IntPtr.Size);
                }
            }
            else
            {
                Debug.Assert(false);
            }

            // Need to prepare the base type first since it is used to compute interfaces
            if (!noExtraPreparation)
            {
                PrepareBaseTypeAndDictionaries(type);
                PrepareRuntimeInterfaces(type);

                TypeLoaderLogger.WriteLine("Layout for type " + type.ToString() + " complete." +
                    " IsHFA = " + (state.IsHFA ? "true" : "false") +
                    " Type size = " + (state.TypeSize.HasValue ? state.TypeSize.Value.LowLevelToString() : "UNDEF") +
                    " Fields size = " + (state.UnalignedTypeSize.HasValue ? state.UnalignedTypeSize.Value.LowLevelToString() : "UNDEF") +
                    " Type alignment = " + (state.FieldAlignment.HasValue ? state.FieldAlignment.Value.LowLevelToString() : "UNDEF"));

#if FEATURE_UNIVERSAL_GENERICS
                if (state.TemplateType != null && state.TemplateType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                {
                    state.VTableSlotsMapping = new VTableSlotMapper(state.TemplateType.RuntimeTypeHandle.GetNumVtableSlots());
                    ComputeVTableLayout(type, state.TemplateType, state);
                }
#endif
            }
        }

        /// <summary>
        /// Recursively triggers preparation for a type's runtime interfaces
        /// </summary>
        private void PrepareRuntimeInterfaces(TypeDesc type)
        {
            // Prepare all the interfaces that might be used. (This can be a superset of the
            // interfaces explicitly in the NativeLayout.)
            foreach (DefType interfaceType in type.RuntimeInterfaces)
            {
                PrepareType(interfaceType);
            }
        }

        /// <summary>
        /// Triggers preparation for a type's base types
        /// </summary>
        private void PrepareBaseTypeAndDictionaries(TypeDesc type)
        {
            DefType baseType = type.BaseType;
            if (baseType == null)
                return;

            PrepareType(baseType);
        }

        private void ProcessTypesNeedingPreparation()
        {
            // Process the pending types
            while (_typesThatNeedPreparation != null)
            {
                var pendingTypes = _typesThatNeedPreparation;
                _typesThatNeedPreparation = null;

                for (int i = 0; i < pendingTypes.Count; i++)
                    PrepareType(pendingTypes[i]);
            }
        }

        private static GenericDictionaryCell[] GetGenericMethodDictionaryCellsForMetadataBasedLoad(InstantiatedMethod method, InstantiatedMethod nonTemplateMethod)
        {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            uint r2rNativeLayoutInfoToken;
            GenericDictionaryCell[] cells = null;
            NativeFormatModuleInfo r2rNativeLayoutModuleInfo;

            if ((new TemplateLocator()).TryGetMetadataNativeLayout(nonTemplateMethod, out r2rNativeLayoutModuleInfo, out r2rNativeLayoutInfoToken))
            {
                // ReadyToRun dictionary parsing
                NativeReader readyToRunReader = TypeLoaderEnvironment.Instance.GetNativeLayoutInfoReader(r2rNativeLayoutModuleInfo.Handle);
                var readyToRunInfoParser = new NativeParser(readyToRunReader, r2rNativeLayoutInfoToken);

                // A null readyToRunInfoParser is a valid situation to end up in
                // This can happen if either we have exact code for a method, or if
                // we are going to use the universal generic implementation.
                // In both of those cases, we do not have any generic dictionary cells
                // to put into the dictionary
                if (!readyToRunInfoParser.IsNull)
                {
                    NativeFormatMetadataUnit nativeMetadataUnit = method.Context.ResolveMetadataUnit(r2rNativeLayoutModuleInfo);
                    FixupCellMetadataResolver resolver = new FixupCellMetadataResolver(nativeMetadataUnit, nonTemplateMethod);
                    cells = GenericDictionaryCell.BuildDictionaryFromMetadataTokensAndContext(this, readyToRunInfoParser, nativeMetadataUnit, resolver);
                }
            }

            return cells;
#else
            return null;
#endif
        }

        internal void ParseNativeLayoutInfo(InstantiatedMethod method)
        {
            TypeLoaderLogger.WriteLine("Parsing NativeLayoutInfo for method " + method.ToString() + " ...");

            Debug.Assert(method.Dictionary == null);

            InstantiatedMethod nonTemplateMethod = method;

            // Templates are always non-unboxing stubs
            if (method.UnboxingStub)
            {
                // Strip unboxing stub, note the first parameter which is false
                nonTemplateMethod = (InstantiatedMethod)method.Context.ResolveGenericMethodInstantiation(false, (DefType)method.OwningType, method.NameAndSignature, method.Instantiation, IntPtr.Zero, false);
            }

            uint nativeLayoutInfoToken;
            NativeFormatModuleInfo nativeLayoutModule;
            MethodDesc templateMethod = TemplateLocator.TryGetGenericMethodTemplate(nonTemplateMethod, out nativeLayoutModule, out nativeLayoutInfoToken);

            // If the templateMethod found in the static image is missing or universal, see if the R2R layout
            // can provide something more specific.
            if ((templateMethod == null) || templateMethod.IsCanonicalMethod(CanonicalFormKind.Universal))
            {
                GenericDictionaryCell[] cells = GetGenericMethodDictionaryCellsForMetadataBasedLoad(method, nonTemplateMethod);

                if (cells != null)
                {
                    method.SetGenericDictionary(new GenericMethodDictionary(cells));
                    return;
                }

                if (templateMethod == null)
                {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                    // In this case we were looking for the r2r template to create the dictionary, but
                    // there isn't one. This implies that we don't need a Canon specific dictionary
                    // so just generate something empty
                    method.SetGenericDictionary(new GenericMethodDictionary(Array.Empty<GenericDictionaryCell>()));
                    return;
#else
                    throw new TypeBuilder.MissingTemplateException();
#endif
                }
            }

            // Ensure that if this method is non-shareable from a normal canonical perspective, then
            // its template MUST be a universal canonical template method
            Debug.Assert(!method.IsNonSharableMethod || (method.IsNonSharableMethod && templateMethod.IsCanonicalMethod(CanonicalFormKind.Universal)));

            NativeReader nativeLayoutInfoReader = TypeLoaderEnvironment.GetNativeLayoutInfoReader(nativeLayoutModule.Handle);

            var methodInfoParser = new NativeParser(nativeLayoutInfoReader, nativeLayoutInfoToken);
            var context = new NativeLayoutInfoLoadContext
            {
                _typeSystemContext = method.Context,
                _typeArgumentHandles = method.OwningType.Instantiation,
                _methodArgumentHandles = method.Instantiation,
                _module = nativeLayoutModule
            };

            BagElementKind kind;
            while ((kind = methodInfoParser.GetBagElementKind()) != BagElementKind.End)
            {
                switch (kind)
                {
                    case BagElementKind.DictionaryLayout:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.DictionaryLayout");
                        method.SetGenericDictionary(new GenericMethodDictionary(GenericDictionaryCell.BuildDictionary(this, context, methodInfoParser.GetParserFromRelativeOffset())));
                        break;

                    default:
                        Debug.Fail("Unexpected BagElementKind for generic method with name " + method.NameAndSignature.Name + "! Only BagElementKind.DictionaryLayout should appear.");
                        throw new BadImageFormatException();
                }
            }

            if (method.Dictionary == null)
                method.SetGenericDictionary(new GenericMethodDictionary(Array.Empty<GenericDictionaryCell>()));
        }

        internal void ParseNativeLayoutInfo(TypeBuilderState state, TypeDesc type)
        {
            TypeLoaderLogger.WriteLine("Parsing NativeLayoutInfo for type " + type.ToString() + " ...");

            bool isTemplateUniversalCanon = false;
            if (state.TemplateType != null)
            {
                isTemplateUniversalCanon = state.TemplateType.IsCanonicalSubtype(CanonicalFormKind.Universal);
            }

            // If we found the universal template, see if there is a ReadyToRun dictionary description available.
            // If so, use that, otherwise, run down the template type loader path with the universal template
            if ((state.TemplateType == null) || isTemplateUniversalCanon)
            {
                // ReadyToRun case - Native Layout is just the dictionary
                NativeParser readyToRunInfoParser = state.GetParserForReadyToRunNativeLayoutInfo();
                GenericDictionaryCell[] cells = null;

                // A null readyToRunInfoParser is a valid situation to end up in
                // This can happen if either we have exact code for the method on a type, or if
                // we are going to use the universal generic implementation.
                // In both of those cases, we do not have any generic dictionary cells
                // to put into the dictionary
                if (!readyToRunInfoParser.IsNull)
                {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                    NativeFormatMetadataUnit nativeMetadataUnit = type.Context.ResolveMetadataUnit(state.R2RNativeLayoutInfo.Module);
                    FixupCellMetadataResolver resolver = new FixupCellMetadataResolver(nativeMetadataUnit, type);
                    cells = GenericDictionaryCell.BuildDictionaryFromMetadataTokensAndContext(this, readyToRunInfoParser, nativeMetadataUnit, resolver);
#endif
                }
                state.Dictionary = cells != null ? new GenericTypeDictionary(cells) : null;

                if (state.TemplateType == null)
                    return;
            }

            NativeParser typeInfoParser = state.GetParserForNativeLayoutInfo();
            NativeLayoutInfoLoadContext context = state.NativeLayoutInfo.LoadContext;

            NativeParser baseTypeParser = new NativeParser();

            int nonGcDataSize = 0;
            int gcDataSize = 0;
            int threadDataSize = 0;
            bool staticSizesMeaningful = (type is DefType) // Is type permitted to have static fields
                                    && !isTemplateUniversalCanon; // Non-universal templates always specify their statics sizes
                                                                  // if the size can be greater than 0

            int baseTypeSize = 0;
            bool checkBaseTypeSize = false;

            BagElementKind kind;
            while ((kind = typeInfoParser.GetBagElementKind()) != BagElementKind.End)
            {
                switch (kind)
                {
                    case BagElementKind.BaseType:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.BaseType");
                        Debug.Assert(baseTypeParser.IsNull);
                        baseTypeParser = typeInfoParser.GetParserFromRelativeOffset();
                        break;

                    case BagElementKind.BaseTypeSize:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.BaseTypeSize");
                        Debug.Assert(state.TemplateType.IsCanonicalSubtype(CanonicalFormKind.Universal));
                        baseTypeSize = checked((int)typeInfoParser.GetUnsigned());
                        break;

                    case BagElementKind.ImplementedInterfaces:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.ImplementedInterfaces");
                        // Interface handling is done entirely in NativeLayoutInterfacesAlgorithm
                        typeInfoParser.GetUnsigned();
                        break;

                    case BagElementKind.TypeFlags:
                        {
                            TypeLoaderLogger.WriteLine("Found BagElementKind.TypeFlags");
                            Internal.NativeFormat.TypeFlags flags = (Internal.NativeFormat.TypeFlags)typeInfoParser.GetUnsigned();
                            Debug.Assert(state.HasStaticConstructor == ((flags & Internal.NativeFormat.TypeFlags.HasClassConstructor) != 0));
                        }
                        break;

                    case BagElementKind.ClassConstructorPointer:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.ClassConstructorPointer");
                        state.ClassConstructorPointer = context.GetGCStaticInfo(typeInfoParser.GetUnsigned());
                        break;

                    case BagElementKind.NonGcStaticDataSize:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.NonGcStaticDataSize");
                        // Use checked typecast to int to ensure there aren't any overflows/truncations (size value used in allocation of memory later)
                        nonGcDataSize = checked((int)typeInfoParser.GetUnsigned());
                        Debug.Assert(staticSizesMeaningful);
                        break;

                    case BagElementKind.GcStaticDataSize:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.GcStaticDataSize");
                        // Use checked typecast to int to ensure there aren't any overflows/truncations (size value used in allocation of memory later)
                        gcDataSize = checked((int)typeInfoParser.GetUnsigned());
                        Debug.Assert(staticSizesMeaningful);
                        break;

                    case BagElementKind.ThreadStaticDataSize:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.ThreadStaticDataSize");
                        // Use checked typecast to int to ensure there aren't any overflows/truncations (size value used in allocation of memory later)
                        threadDataSize = checked((int)typeInfoParser.GetUnsigned());
                        Debug.Assert(staticSizesMeaningful);
                        break;

                    case BagElementKind.GcStaticDesc:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.GcStaticDesc");
                        state.GcStaticDesc = context.GetGCStaticInfo(typeInfoParser.GetUnsigned());
                        break;

                    case BagElementKind.ThreadStaticDesc:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.ThreadStaticDesc");
                        state.ThreadStaticDesc = context.GetGCStaticInfo(typeInfoParser.GetUnsigned());
                        break;

                    case BagElementKind.GenericVarianceInfo:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.GenericVarianceInfo");
                        NativeParser varianceInfoParser = typeInfoParser.GetParserFromRelativeOffset();
                        state.GenericVarianceFlags = new GenericVariance[varianceInfoParser.GetSequenceCount()];
                        for (int i = 0; i < state.GenericVarianceFlags.Length; i++)
                            state.GenericVarianceFlags[i] = checked((GenericVariance)varianceInfoParser.GetUnsigned());
                        break;

                    case BagElementKind.FieldLayout:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.FieldLayout");
                        typeInfoParser.SkipInteger(); // Handled in type layout algorithm
                        break;

#if FEATURE_UNIVERSAL_GENERICS
                    case BagElementKind.VTableMethodSignatures:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.VTableMethodSignatures");
                        ParseVTableMethodSignatures(state, context, typeInfoParser.GetParserFromRelativeOffset());
                        break;
#endif

                    case BagElementKind.SealedVTableEntries:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.SealedVTableEntries");
                        state.NumSealedVTableEntries = typeInfoParser.GetUnsigned();
                        break;

                    case BagElementKind.DictionaryLayout:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.DictionaryLayout");
                        Debug.Assert(!isTemplateUniversalCanon, "Universal template nativelayout do not have DictionaryLayout");

                        Debug.Assert(state.Dictionary == null);
                        if (!state.TemplateType.RetrieveRuntimeTypeHandleIfPossible())
                        {
                            TypeLoaderLogger.WriteLine("ERROR: failed to get type handle for template type " + state.TemplateType.ToString());
                            throw new TypeBuilder.MissingTemplateException();
                        }
                        state.Dictionary = new GenericTypeDictionary(GenericDictionaryCell.BuildDictionary(this, context, typeInfoParser.GetParserFromRelativeOffset()));
                        break;

                    default:
                        TypeLoaderLogger.WriteLine("Found unknown BagElementKind: " + ((int)kind).LowLevelToString());
                        typeInfoParser.SkipInteger();
                        break;
                }
            }

            if (staticSizesMeaningful)
            {
                Debug.Assert((state.NonGcDataSize + (state.HasStaticConstructor ? TypeBuilder.ClassConstructorOffset : 0)) == nonGcDataSize);
                Debug.Assert(state.GcDataSize == gcDataSize);
                Debug.Assert(state.ThreadDataSize == threadDataSize);
            }

#if GENERICS_FORCE_USG
            if (isTemplateUniversalCanon && type.CanShareNormalGenericCode())
            {
                // Even in the GENERICS_FORCE_USG stress mode today, codegen will generate calls to normal-canonical target methods whenever possible.
                // Given that we use universal template types to build the dynamic EETypes, these dynamic types will end up with NULL dictionary
                // entries, causing the normal-canonical code sharing to fail.
                // To fix this problem, we will load the generic dictionary from the non-universal template type, and build a generic dictionary out of
                // it for the dynamic type, and store that dictionary pointer in the dynamic MethodTable's structure.
                TypeBuilderState tempState = new TypeBuilderState();
                tempState.NativeLayoutInfo = new NativeLayoutInfo();
                state.NonUniversalTemplateType = tempState.TemplateType = type.Context.TemplateLookup.TryGetNonUniversalTypeTemplate(type, ref tempState.NativeLayoutInfo);
                if (tempState.TemplateType != null)
                {
                    Debug.Assert(!tempState.TemplateType.IsCanonicalSubtype(CanonicalFormKind.UniversalCanonLookup));
                    NativeParser nonUniversalTypeInfoParser = GetNativeLayoutInfoParser(type, ref tempState.NativeLayoutInfo);
                    NativeParser dictionaryLayoutParser = nonUniversalTypeInfoParser.GetParserForBagElementKind(BagElementKind.DictionaryLayout);
                    if (!dictionaryLayoutParser.IsNull)
                        state.Dictionary = new GenericTypeDictionary(GenericDictionaryCell.BuildDictionary(this, context, dictionaryLayoutParser));

                    // Get the non-universal GCDesc pointers, so we can compare them the ones we will dynamically construct for the type
                    // and verify they are equal (This is an easy and predictable way of validation for the GCDescs creation logic in the stress mode)
                    GetNonUniversalGCDescPointers(type, state, tempState);
                }
            }
#endif
            type.ParseBaseType(context, baseTypeParser);

            // Assert that parsed base type size matches the BaseTypeSize that we calculated.
            Debug.Assert(!checkBaseTypeSize || state.BaseTypeSize == baseTypeSize);
        }

#if FEATURE_UNIVERSAL_GENERICS
        private void ParseVTableMethodSignatures(TypeBuilderState state, NativeLayoutInfoLoadContext nativeLayoutInfoLoadContext, NativeParser methodSignaturesParser)
        {
            TypeDesc type = state.TypeBeingBuilt;
            if (methodSignaturesParser.IsNull)
                return;

            // Processing vtable method signatures is only meaningful in the context of universal generics only
            Debug.Assert(state.TemplateType != null && state.TemplateType.IsCanonicalSubtype(CanonicalFormKind.Universal));

            uint numSignatures = methodSignaturesParser.GetUnsigned();

            state.VTableMethodSignatures = new TypeBuilderState.VTableLayoutInfo[numSignatures];

            for (int i = 0; i < numSignatures; i++)
            {
                state.VTableMethodSignatures[i] = new TypeBuilderState.VTableLayoutInfo();

                uint slot = methodSignaturesParser.GetUnsigned();
                state.VTableMethodSignatures[i].VTableSlot = (slot >> 1);
                if ((slot & 1) == 1)
                {
                    state.VTableMethodSignatures[i].IsSealedVTableSlot = true;
                    state.NumSealedVTableMethodSignatures++;
                }

                NativeParser sigParser = methodSignaturesParser.GetParserFromRelativeOffset();
                state.VTableMethodSignatures[i].MethodSignature = RuntimeSignature.CreateFromNativeLayoutSignature(nativeLayoutInfoLoadContext._module.Handle, sigParser.Offset);
            }
        }
#endif

        private unsafe void ComputeVTableLayout(TypeDesc currentType, TypeDesc currentTemplateType, TypeBuilderState targetTypeState)
        {
            TypeDesc baseType = GetBaseTypeThatIsCorrectForMDArrays(currentType);
            TypeDesc baseTemplateType = GetBaseTypeUsingRuntimeTypeHandle(currentTemplateType);

            Debug.Assert((baseType == null && baseTemplateType == null) || (baseType != null && baseTemplateType != null));

            // Compute the vtable layout for the current type starting with base types first
            if (baseType != null)
                ComputeVTableLayout(baseType, baseTemplateType, targetTypeState);

            currentTemplateType.RetrieveRuntimeTypeHandleIfPossible();
            Debug.Assert(!currentTemplateType.RuntimeTypeHandle.IsNull());
            Debug.Assert(baseTemplateType == null || !baseTemplateType.RuntimeTypeHandle.IsNull());

            // The m_usNumVtableSlots field on EETypes includes the count of vtable slots of the base type,
            // so make sure we don't count that twice!
            int currentVtableIndex = baseTemplateType == null ? 0 : baseTemplateType.RuntimeTypeHandle.GetNumVtableSlots();

            IntPtr dictionarySlotInVtable = IntPtr.Zero;

            if (currentType.IsGeneric())
            {
                if (!currentType.CanShareNormalGenericCode() && currentTemplateType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                {
                    // We are building a type that cannot share code with normal canonical types, so the type has to have
                    // the same vtable layout as non-shared generics, meaning no dictionary pointer in the vtable.
                    // We use universal canonical template types to build such types. Universal canonical types have 'NULL'
                    // dictionary pointers in their vtables, so we'll start copying the vtable entries right after that
                    // dictionary slot (dictionaries are accessed/used at runtime in a different way, not through the vtable
                    // dictionary pointer for such types).
                    currentVtableIndex++;
                }
                else if (currentType.CanShareNormalGenericCode())
                {
                    // In the case of a normal canonical type in their base class hierarchy,
                    // we need to keep track of its dictionary slot in the vtable mapping, and try to
                    // copy its value values directly from its template type vtable.
                    // Two possible cases:
                    //      1)  The template type is a normal canonical type. In this case, the dictionary value
                    //          in the vtable slot of the template is NULL, but that's ok because this case is
                    //          correctly handled anyways by the FinishBaseTypeAndDictionaries() API.
                    //      2)  The template type is NOT a canonical type. In this case, the dictionary value
                    //          in the vtable slot of the template is not null, and we keep track of it in the
                    //          VTableSlotsMapping so we can copy it to the dynamic type after creation.
                    //          This corner case is not handled by FinishBaseTypeAndDictionaries(), so we track it
                    //          here.
                    // Examples:
                    //      1) Derived<T,U> : Base<U>, instantiated over [int,string]
                    //      2) Derived<__Universal> : BaseClass, and BaseClass : BaseBaseClass<object>
                    //      3) Derived<__Universal> : BaseClass<object>
                    Debug.Assert(currentTemplateType != null && !currentTemplateType.RuntimeTypeHandle.IsNull());

                    IntPtr* pTemplateVtable = (IntPtr*)((byte*)(currentTemplateType.RuntimeTypeHandle.ToEETypePtr()) + sizeof(MethodTable));
                    dictionarySlotInVtable = pTemplateVtable[currentVtableIndex];
                }
            }
            else if (currentType is ArrayType)
            {
                if (currentTemplateType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                {
                    TypeDesc canonicalElementType = currentType.Context.ConvertToCanon(((ArrayType)currentType).ElementType, CanonicalFormKind.Specific);
                    bool quickIsNotCanonical = canonicalElementType == ((ArrayType)currentType).ElementType;

                    Debug.Assert(quickIsNotCanonical == !canonicalElementType.IsCanonicalSubtype(CanonicalFormKind.Any));

                    if (quickIsNotCanonical)
                    {
                        // We are building a type that cannot share code with normal canonical types, so the type has to have
                        // the same vtable layout as non-shared generics, meaning no dictionary pointer in the vtable.
                        // We use universal canonical template types to build such types. Universal canonical types have 'NULL'
                        // dictionary pointers in their vtables, so we'll start copying the vtable entries right after that
                        // dictionary slot (dictionaries are accessed/used at runtime in a different way, not through the vtable
                        // dictionary pointer for such types).
                        currentVtableIndex++;
                    }
                }
            }

            // Map vtable entries from target type's template type
            int numVtableSlotsOnCurrentTemplateType = currentTemplateType.RuntimeTypeHandle.GetNumVtableSlots();
            for (; currentVtableIndex < numVtableSlotsOnCurrentTemplateType; currentVtableIndex++)
            {
                targetTypeState.VTableSlotsMapping.AddMapping(
                    currentVtableIndex,
                    targetTypeState.VTableSlotsMapping.NumSlotMappings,
                    dictionarySlotInVtable);

                // Reset dictionarySlotInVtable (only one dictionary slot in vtable per type)
                dictionarySlotInVtable = IntPtr.Zero;
            }

            // Sanity check: vtable of the dynamic type should be equal or smaller than the vtable of the template type
            Debug.Assert(targetTypeState.VTableSlotsMapping.NumSlotMappings <= numVtableSlotsOnCurrentTemplateType);
        }

        /// <summary>
        /// Wraps information about how a type is laid out into one package.  Types may have been laid out by
        /// TypeBuilder (which means they have a gc bitfield), or they could be types that were laid out by NUTC
        /// (which means we only have a GCDesc for them).  This struct wraps both of those possibilities into
        /// one package to be able to write that layout to another bitfield we are constructing.  (This is for
        /// struct fields.)
        /// </summary>
        internal unsafe struct GCLayout
        {
            private LowLevelList<bool> _bitfield;
            private unsafe void* _gcdesc;
            private int _size;
            private bool _isReferenceTypeGCLayout;

            public static GCLayout None { get { return new GCLayout(); } }
            public static GCLayout SingleReference { get; } = new GCLayout(new LowLevelList<bool>(new bool[1] { true }), false);

            public bool IsNone { get { return _bitfield == null && _gcdesc == null; } }

            public GCLayout(LowLevelList<bool> bitfield, bool isReferenceTypeGCLayout)
            {
                Debug.Assert(bitfield != null);

                _bitfield = bitfield;
                _gcdesc = null;
                _size = 0;
                _isReferenceTypeGCLayout = isReferenceTypeGCLayout;
            }

            public GCLayout(RuntimeTypeHandle rtth)
            {
                MethodTable* MethodTable = rtth.ToEETypePtr();
                Debug.Assert(MethodTable != null);

                _bitfield = null;
                _isReferenceTypeGCLayout = false; // This field is only used for the LowLevelList<bool> path
                _gcdesc = MethodTable->HasGCPointers ? (void**)MethodTable - 1 : null;
                _size = (int)MethodTable->BaseSize;
            }

            /// <summary>
            /// Writes this layout to the given bitfield.
            /// </summary>
            /// <param name="bitfield">The bitfield to write a layout to (may be null, at which
            /// point it will be created and assigned).</param>
            /// <param name="offset">The offset at which we need to write the bitfield.</param>
            public void WriteToBitfield(LowLevelList<bool> bitfield, int offset)
            {
                if (bitfield == null)
                    throw new ArgumentNullException(nameof(bitfield));

                if (IsNone)
                    return;

                // Ensure exactly one of these two are set.
                Debug.Assert(_gcdesc != null ^ _bitfield != null);

                if (_bitfield != null)
                    MergeBitfields(bitfield, offset);
                else
                    WriteGCDescToBitfield(bitfield, offset);
            }

            private unsafe void WriteGCDescToBitfield(LowLevelList<bool> bitfield, int offset)
            {
                int startIndex = offset / IntPtr.Size;

                void** ptr = (void**)_gcdesc;
                Debug.Assert(_gcdesc != null);

                // Number of series
                int count = (int)*ptr-- - 1;
                Debug.Assert(count >= 0);

                // Ensure capacity for the values we are about to write
                int capacity = startIndex + _size / IntPtr.Size - 2;
                bitfield.Expand(capacity);

                while (count-- >= 0)
                {
                    int offs = (int)*ptr-- / IntPtr.Size - 1;
                    int len = ((int)*ptr-- + _size) / IntPtr.Size;

                    Debug.Assert(len > 0);
                    Debug.Assert(offs >= 0);

                    for (int i = 0; i < len; i++)
                        bitfield[startIndex + offs + i] = true;
                }
            }

            private void MergeBitfields(LowLevelList<bool> outputBitfield, int offset)
            {
                int startIndex = offset / IntPtr.Size;

                // These routines represent the GC layout after the MethodTable pointer
                // in an object, but the LowLevelList<bool> bitfield logically contains
                // the EETypepointer if it is describing a reference type. So, skip the
                // first value.
                int itemsToSkip = _isReferenceTypeGCLayout ? 1 : 0;

                // Assert that we only skip a non-reported pointer.
                Debug.Assert(itemsToSkip == 0 || _bitfield[0] == false);

                // Ensure capacity for the values we are about to write
                int capacity = startIndex + _bitfield.Count - itemsToSkip;
                outputBitfield.Expand(capacity);


                for (int i = itemsToSkip; i < _bitfield.Count; i++)
                {
                    // We should never overwrite a TRUE value in the table.
                    Debug.Assert(!outputBitfield[startIndex + i - itemsToSkip] || _bitfield[i]);

                    outputBitfield[startIndex + i - itemsToSkip] = _bitfield[i];
                }
            }
        }

#if GENERICS_FORCE_USG
        private unsafe void GetNonUniversalGCDescPointers(TypeDesc type, TypeBuilderState state, TypeBuilderState tempNonUniversalState)
        {
            NativeParser nonUniversalTypeInfoParser = GetNativeLayoutInfoParser(type, ref tempNonUniversalState.NativeLayoutInfo);
            NativeLayoutInfoLoadContext context = tempNonUniversalState.NativeLayoutInfo.LoadContext;

            uint beginOffset = nonUniversalTypeInfoParser.Offset;
            uint? staticGCDescId = nonUniversalTypeInfoParser.GetUnsignedForBagElementKind(BagElementKind.GcStaticDesc);

            nonUniversalTypeInfoParser.Offset = beginOffset;
            uint? threadStaticGCDescId = nonUniversalTypeInfoParser.GetUnsignedForBagElementKind(BagElementKind.ThreadStaticDesc);

            if(staticGCDescId.HasValue)
                state.NonUniversalStaticGCDesc = context.GetStaticInfo(staticGCDescId.Value);

            if (threadStaticGCDescId.HasValue)
                state.NonUniversalThreadStaticGCDesc = context.GetStaticInfo(threadStaticGCDescId.Value);

            state.NonUniversalInstanceGCDescSize = RuntimeAugments.GetGCDescSize(tempNonUniversalState.TemplateType.RuntimeTypeHandle);
            if (state.NonUniversalInstanceGCDescSize > 0)
                state.NonUniversalInstanceGCDesc = new IntPtr(((byte*)tempNonUniversalState.TemplateType.RuntimeTypeHandle.ToIntPtr().ToPointer()) - 1);
        }
#endif

        private unsafe void AllocateRuntimeType(TypeDesc type)
        {
            TypeBuilderState state = type.GetTypeBuilderState();

            Debug.Assert(type is DefType || type is ArrayType || type is PointerType || type is ByRefType);

            if (state.ThreadDataSize != 0)
                state.ThreadStaticOffset = TypeLoaderEnvironment.Instance.GetNextThreadStaticsOffsetValue();

            RuntimeTypeHandle rtt = EETypeCreator.CreateEEType(type, state);

            if (state.ThreadDataSize != 0)
                TypeLoaderEnvironment.Instance.RegisterDynamicThreadStaticsInfo(state.HalfBakedRuntimeTypeHandle, state.ThreadStaticOffset, state.ThreadDataSize);

            TypeLoaderLogger.WriteLine("Allocated new type " + type.ToString() + " with hashcode value = 0x" + type.GetHashCode().LowLevelToString() + " with MethodTable = " + rtt.ToIntPtr().LowLevelToString() + " of size " + rtt.ToEETypePtr()->BaseSize.LowLevelToString());
        }

        private static void AllocateRuntimeMethodDictionary(InstantiatedMethod method)
        {
            Debug.Assert(method.RuntimeMethodDictionary == IntPtr.Zero && method.Dictionary != null);

            IntPtr rmd = method.Dictionary.Allocate();
            method.AssociateWithRuntimeMethodDictionary(rmd);

            TypeLoaderLogger.WriteLine("Allocated new method dictionary for method " + method.ToString() + " @ " + rmd.LowLevelToString());
        }

        private RuntimeTypeHandle[] GetGenericContextOfBaseType(DefType type, int vtableMethodSlot)
        {
            DefType baseType = type.BaseType;
            Debug.Assert(baseType == null || !GetRuntimeTypeHandle(baseType).IsNull());
            Debug.Assert(vtableMethodSlot < GetRuntimeTypeHandle(type).GetNumVtableSlots());

            int numBaseTypeVtableSlots = baseType == null ? 0 : GetRuntimeTypeHandle(baseType).GetNumVtableSlots();

            if (vtableMethodSlot < numBaseTypeVtableSlots)
                return GetGenericContextOfBaseType(baseType, vtableMethodSlot);
            else
                return GetRuntimeTypeHandles(type.Instantiation);
        }

#if FEATURE_UNIVERSAL_GENERICS
        private unsafe void FinishVTableCallingConverterThunks(TypeDesc type, TypeBuilderState state)
        {
            Debug.Assert(state.TemplateType.IsCanonicalSubtype(CanonicalFormKind.Universal));

            if (state.VTableMethodSignatures == null || state.VTableMethodSignatures.Length == 0)
                return;

            int numVtableSlots = GetRuntimeTypeHandle(type).GetNumVtableSlots();
            IntPtr* vtableCells = (IntPtr*)((byte*)GetRuntimeTypeHandle(type).ToIntPtr() + sizeof(MethodTable));
            Debug.Assert((state.VTableMethodSignatures.Length - state.NumSealedVTableMethodSignatures) <= numVtableSlots);

            TypeDesc baseType = type.BaseType;
            int numBaseTypeVtableSlots = GetRuntimeTypeHandle(baseType).GetNumVtableSlots();

            // Generic context
            RuntimeTypeHandle[] typeArgs = Empty<RuntimeTypeHandle>.Array;

            if (type is DefType)
                typeArgs = GetRuntimeTypeHandles(((DefType)type).Instantiation);
            else if (type is ArrayType)
                typeArgs = GetRuntimeTypeHandles(new Instantiation(new TypeDesc[] { ((ArrayType)type).ElementType }));

            for (int i = 0; i < state.VTableMethodSignatures.Length; i++)
            {
                RuntimeTypeHandle[] typeArgsToUse = typeArgs;

                int vtableSlotInDynamicType = -1;
                if (!state.VTableMethodSignatures[i].IsSealedVTableSlot)
                {
                    vtableSlotInDynamicType = state.VTableSlotsMapping.GetVTableSlotInTargetType((int)state.VTableMethodSignatures[i].VTableSlot);
                    Debug.Assert(vtableSlotInDynamicType != -1);

                    if (vtableSlotInDynamicType < numBaseTypeVtableSlots)
                    {
                        // Vtable method  from the vtable portion of a base type. Use generic context of the basetype defining the vtable slot.
                        // We should never reach here for array types (the vtable entries of the System.Array basetype should never need a converter).
                        Debug.Assert(type is DefType);
                        typeArgsToUse = GetGenericContextOfBaseType((DefType)type, vtableSlotInDynamicType);
                    }
                }

                IntPtr originalFunctionPointerFromVTable = state.VTableMethodSignatures[i].IsSealedVTableSlot ?
                    ((IntPtr*)state.HalfBakedSealedVTable)[state.VTableMethodSignatures[i].VTableSlot] :
                    vtableCells[vtableSlotInDynamicType];

                IntPtr thunkPtr = CallConverterThunk.MakeThunk(
                    ThunkKind.StandardToGeneric,
                    originalFunctionPointerFromVTable,
                    state.VTableMethodSignatures[i].MethodSignature,
                    IntPtr.Zero,                                        // No instantiating arg for non-generic instance methods
                    typeArgsToUse,
                    Empty<RuntimeTypeHandle>.Array);                    // No GVMs in vtables, no no method args

                if (state.VTableMethodSignatures[i].IsSealedVTableSlot)
                {
                    // Patch the sealed vtable entry to point to the calling converter thunk
                    Debug.Assert(state.VTableMethodSignatures[i].VTableSlot < state.NumSealedVTableEntries && state.HalfBakedSealedVTable != IntPtr.Zero);
                    ((IntPtr*)state.HalfBakedSealedVTable)[state.VTableMethodSignatures[i].VTableSlot] = thunkPtr;
                }
                else
                {
                    // Patch the vtable entry to point to the calling converter thunk
                    Debug.Assert(vtableSlotInDynamicType < numVtableSlots && vtableCells != null);
                    vtableCells[vtableSlotInDynamicType] = thunkPtr;
                }
            }
        }
#endif

        //
        // Returns either the registered type handle or half-baked type handle. This method should be only called
        // during final phase of type building.
        //
#pragma warning disable CA1822
        public RuntimeTypeHandle GetRuntimeTypeHandle(TypeDesc type)
        {
#if DEBUG
            Debug.Assert(_finalTypeBuilding);
#endif

            var rtth = type.RuntimeTypeHandle;
            if (!rtth.IsNull())
                return rtth;

            rtth = type.GetTypeBuilderState().HalfBakedRuntimeTypeHandle;
            Debug.Assert(!rtth.IsNull());
            return rtth;
        }
#pragma warning restore CA1822

        public RuntimeTypeHandle[] GetRuntimeTypeHandles(Instantiation types)
        {
            if (types.Length == 0)
                return Array.Empty<RuntimeTypeHandle>();

            RuntimeTypeHandle[] result = new RuntimeTypeHandle[types.Length];
            for (int i = 0; i < types.Length; i++)
                result[i] = GetRuntimeTypeHandle(types[i]);
            return result;
        }

        public static DefType GetBaseTypeUsingRuntimeTypeHandle(TypeDesc type)
        {
            type.RetrieveRuntimeTypeHandleIfPossible();
            unsafe
            {
                RuntimeTypeHandle thBaseTypeTemplate = type.RuntimeTypeHandle.ToEETypePtr()->BaseType->ToRuntimeTypeHandle();
                if (thBaseTypeTemplate.IsNull())
                    return null;

                return (DefType)type.Context.ResolveRuntimeTypeHandle(thBaseTypeTemplate);
            }
        }

        public static DefType GetBaseTypeThatIsCorrectForMDArrays(TypeDesc type)
        {
            if (type.BaseType == type.Context.GetWellKnownType(WellKnownType.Array))
            {
                // Use the type from the template, the metadata we have will be inaccurate for multidimensional
                // arrays, as we hide the MDArray infrastructure from the metadata.
                TypeDesc template = type.ComputeTemplate(false);
                return GetBaseTypeUsingRuntimeTypeHandle(template ?? type);
            }

            return type.BaseType;
        }

        private void FinishInterfaces(TypeDesc type, TypeBuilderState state)
        {
            DefType[] interfaces = state.RuntimeInterfaces;
            if (interfaces != null)
            {
                for (int i = 0; i < interfaces.Length; i++)
                {
                    state.HalfBakedRuntimeTypeHandle.SetInterface(i, GetRuntimeTypeHandle(interfaces[i]));
                }
            }
        }

        private unsafe void FinishTypeDictionary(TypeDesc type, TypeBuilderState state)
        {
            if (state.Dictionary != null)
            {
                // First, update the dictionary slot in the type's vtable to point to the created dictionary when applicable
                Debug.Assert(state.HalfBakedDictionary != IntPtr.Zero);

                int dictionarySlot = EETypeCreator.GetDictionarySlotInVTable(type);
                if (dictionarySlot >= 0)
                {
                    state.HalfBakedRuntimeTypeHandle.SetDictionary(dictionarySlot, state.HalfBakedDictionary);
                }
                else
                {
                    // Dictionary shouldn't be in the vtable of the type
                    Debug.Assert(!type.CanShareNormalGenericCode());
                }

                TypeLoaderLogger.WriteLine("Setting dictionary entries for type " + type.ToString() + " @ " + state.HalfBakedDictionary.LowLevelToString());
                state.Dictionary.Finish(this);
            }
        }

        private unsafe void FinishMethodDictionary(InstantiatedMethod method)
        {
            Debug.Assert(method.Dictionary != null);

            TypeLoaderLogger.WriteLine("Setting dictionary entries for method " + method.ToString() + " @ " + method.RuntimeMethodDictionary.LowLevelToString());
            method.Dictionary.Finish(this);
        }

        private unsafe void FinishClassConstructor(TypeDesc type, TypeBuilderState state)
        {
            if (!state.HasStaticConstructor)
                return;

            IntPtr canonicalClassConstructorFunctionPointer = IntPtr.Zero; // Pointer to canonical static method to serve as cctor
            IntPtr exactClassConstructorFunctionPointer = IntPtr.Zero; // Exact pointer. Takes priority over canonical pointer

            if (state.TemplateType == null)
            {
                if (!type.HasInstantiation)
                {
                    // Non-Generic ReadyToRun types in their current state already have their static field region setup
                    // with the class constructor initialized.
                    return;
                }
                else
                {
                    // For generic types, we need to do the metadata lookup and then resolve to a function pointer.
                    MethodDesc staticConstructor = type.GetStaticConstructor();
                    IntPtr staticCctor;
                    IntPtr unused1;
                    TypeLoaderEnvironment.MethodAddressType addressType;
                    if (!TypeLoaderEnvironment.TryGetMethodAddressFromMethodDesc(staticConstructor, out staticCctor, out unused1, out addressType))
                    {
                        Environment.FailFast("Unable to find class constructor method address for type:" + type.ToString());
                    }
                    Debug.Assert(unused1 == IntPtr.Zero);

                    switch (addressType)
                    {
                        case TypeLoaderEnvironment.MethodAddressType.Exact:
                            // If we have an exact match, put it in the slot directly
                            // and return as we don't want to make this into a fat function pointer
                            exactClassConstructorFunctionPointer = staticCctor;
                            break;

                        case TypeLoaderEnvironment.MethodAddressType.Canonical:
                        case TypeLoaderEnvironment.MethodAddressType.UniversalCanonical:
                            // If we have a canonical method, setup for generating a fat function pointer
                            canonicalClassConstructorFunctionPointer = staticCctor;
                            break;

                        default:
                            Environment.FailFast("Invalid MethodAddressType during ClassConstructor discovery");
                            return;
                    }
                }
            }
            else if (state.ClassConstructorPointer.HasValue)
            {
                canonicalClassConstructorFunctionPointer = state.ClassConstructorPointer.Value;
            }
            else
            {
                // Lookup the non-GC static data for the template type, and use the class constructor context offset to locate the class constructor's
                // fat pointer within the non-GC static data.
                IntPtr templateTypeStaticData = TypeLoaderEnvironment.Instance.TryGetNonGcStaticFieldData(GetRuntimeTypeHandle(state.TemplateType));
                Debug.Assert(templateTypeStaticData != IntPtr.Zero);
                IntPtr* templateTypeClassConstructorSlotPointer = (IntPtr*)((byte*)templateTypeStaticData + ClassConstructorOffset);
                IntPtr templateTypeClassConstructorFatFunctionPointer = templateTypeClassConstructorFatFunctionPointer = *templateTypeClassConstructorSlotPointer;

                // Crack the fat function pointer into the raw class constructor method pointer and the generic type dictionary.
                Debug.Assert(FunctionPointerOps.IsGenericMethodPointer(templateTypeClassConstructorFatFunctionPointer));
                GenericMethodDescriptor* templateTypeGenericMethodDescriptor = FunctionPointerOps.ConvertToGenericDescriptor(templateTypeClassConstructorFatFunctionPointer);
                Debug.Assert(templateTypeGenericMethodDescriptor != null);
                canonicalClassConstructorFunctionPointer = templateTypeGenericMethodDescriptor->MethodFunctionPointer;
            }

            IntPtr generatedTypeStaticData = GetRuntimeTypeHandle(type).ToEETypePtr()->DynamicNonGcStaticsData;
            IntPtr* generatedTypeClassConstructorSlotPointer = (IntPtr*)((byte*)generatedTypeStaticData + ClassConstructorOffset);

            if (exactClassConstructorFunctionPointer != IntPtr.Zero)
            {
                // We have an exact pointer, not a canonical match
                // Just set the pointer and return. No need for a fat pointer
                *generatedTypeClassConstructorSlotPointer = exactClassConstructorFunctionPointer;
                return;
            }

            // If we reach here, classConstructorFunctionPointer points at a canonical method, that needs to be converted into
            // a fat function pointer so that the calli in the ClassConstructorRunner will work properly
            Debug.Assert(canonicalClassConstructorFunctionPointer != IntPtr.Zero);

            // Use the template type's class constructor method pointer and this type's generic type dictionary to generate a new fat pointer,
            // and save that fat pointer back to this type's class constructor context offset within the non-GC static data.
            IntPtr instantiationArgument = GetRuntimeTypeHandle(type).ToIntPtr();
            IntPtr generatedTypeClassConstructorFatFunctionPointer = FunctionPointerOps.GetGenericMethodFunctionPointer(canonicalClassConstructorFunctionPointer, instantiationArgument);
            *generatedTypeClassConstructorSlotPointer = generatedTypeClassConstructorFatFunctionPointer;
        }

        private void CopyDictionaryFromTypeToAppropriateSlotInDerivedType(TypeDesc baseType, TypeBuilderState derivedTypeState)
        {
            var baseTypeState = baseType.GetOrCreateTypeBuilderState();

            if (baseTypeState.HasDictionaryInVTable)
            {
                RuntimeTypeHandle baseTypeHandle = GetRuntimeTypeHandle(baseType);

                // If the basetype is currently being created by the TypeBuilder, we need to get its dictionary pointer from the
                // TypeBuilder state (at this point, the dictionary has not yet been set on the baseTypeHandle). If
                // the basetype is not a dynamic type, or has previously been dynamically allocated in the past, the TypeBuilder
                // state will have a null dictionary pointer, in which case we need to read it directly from the basetype's vtable
                IntPtr dictionaryEntry = baseTypeState.HalfBakedDictionary;
                if (dictionaryEntry == IntPtr.Zero)
                    dictionaryEntry = baseTypeHandle.GetDictionary();
                Debug.Assert(dictionaryEntry != IntPtr.Zero);

                // Compute the vtable slot for the dictionary entry to set
                int dictionarySlot = EETypeCreator.GetDictionarySlotInVTable(baseType);
                Debug.Assert(dictionarySlot >= 0);

                derivedTypeState.HalfBakedRuntimeTypeHandle.SetDictionary(dictionarySlot, dictionaryEntry);
                TypeLoaderLogger.WriteLine("Setting basetype " + baseType.ToString() + " dictionary on type " + derivedTypeState.TypeBeingBuilt.ToString());
            }
        }

        private void FinishBaseTypeAndDictionaries(TypeDesc type, TypeBuilderState state)
        {
            DefType baseType = GetBaseTypeThatIsCorrectForMDArrays(type);
            state.HalfBakedRuntimeTypeHandle.SetBaseType(baseType == null ? default(RuntimeTypeHandle) : GetRuntimeTypeHandle(baseType));

            if (baseType == null)
                return;

            // Update every dictionary in type hierarchy with copy from base type
            while (baseType != null)
            {
                CopyDictionaryFromTypeToAppropriateSlotInDerivedType(baseType, state);
                baseType = baseType.BaseType;
            }
        }

        private void FinishRuntimeType(TypeDesc type)
        {
            TypeLoaderLogger.WriteLine("Finishing type " + type.ToString() + " ...");

            var state = type.GetTypeBuilderState();

            if (type is DefType)
            {
                DefType typeAsDefType = (DefType)type;

                if (type.HasInstantiation)
                {
                    // Type definitions don't need any further finishing once created by the EETypeCreator
                    if (type.IsTypeDefinition)
                        return;

                    state.HalfBakedRuntimeTypeHandle.SetGenericDefinition(GetRuntimeTypeHandle(typeAsDefType.GetTypeDefinition()));
                    Instantiation instantiation = typeAsDefType.Instantiation;
                    state.HalfBakedRuntimeTypeHandle.SetGenericArity((uint)instantiation.Length);
                    for (int argIndex = 0; argIndex < instantiation.Length; argIndex++)
                    {
                        state.HalfBakedRuntimeTypeHandle.SetGenericArgument(argIndex, GetRuntimeTypeHandle(instantiation[argIndex]));
                        if (state.GenericVarianceFlags != null)
                        {
                            Debug.Assert(state.GenericVarianceFlags.Length == instantiation.Length);
                            state.HalfBakedRuntimeTypeHandle.SetGenericVariance(argIndex, state.GenericVarianceFlags[argIndex]);
                        }
                    }
                }

                FinishBaseTypeAndDictionaries(type, state);

                FinishInterfaces(type, state);

                FinishTypeDictionary(type, state);

                FinishClassConstructor(type, state);

#if FEATURE_UNIVERSAL_GENERICS
                // For types that were allocated from universal canonical templates, patch their vtables with
                // pointers to calling convention conversion thunks
                if (state.TemplateType != null && state.TemplateType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                    FinishVTableCallingConverterThunks(type, state);
#endif
            }
            else if (type is ParameterizedType)
            {
                if (type is ArrayType)
                {
                    ArrayType typeAsSzArrayType = (ArrayType)type;

                    state.HalfBakedRuntimeTypeHandle.SetRelatedParameterType(GetRuntimeTypeHandle(typeAsSzArrayType.ElementType));

                    state.HalfBakedRuntimeTypeHandle.SetComponentSize(state.ComponentSize.Value);

                    FinishInterfaces(type, state);

                    if (typeAsSzArrayType.IsSzArray && !typeAsSzArrayType.ElementType.IsPointer)
                    {
                        FinishTypeDictionary(type, state);

#if FEATURE_UNIVERSAL_GENERICS
                        // For types that were allocated from universal canonical templates, patch their vtables with
                        // pointers to calling convention conversion thunks
                        if (state.TemplateType != null && state.TemplateType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                            FinishVTableCallingConverterThunks(type, state);
#endif
                    }
                }
                else if (type is PointerType)
                {
                    state.HalfBakedRuntimeTypeHandle.SetRelatedParameterType(GetRuntimeTypeHandle(((PointerType)type).ParameterType));

                    // Nothing else to do for pointer types
                }
                else if (type is ByRefType)
                {
                    state.HalfBakedRuntimeTypeHandle.SetRelatedParameterType(GetRuntimeTypeHandle(((ByRefType)type).ParameterType));

                    // We used a pointer type for the template because they're similar enough. Adjust this to be a ByRef.
                    unsafe
                    {
                        Debug.Assert(state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->ParameterizedTypeShape == ParameterizedTypeShapeConstants.Pointer);
                        state.HalfBakedRuntimeTypeHandle.SetParameterizedTypeShape(ParameterizedTypeShapeConstants.ByRef);
                        Debug.Assert(state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->ElementType == EETypeElementType.Pointer);
                        state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->Flags = EETypeBuilderHelpers.ComputeFlags(type);
                        Debug.Assert(state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->ElementType == EETypeElementType.ByRef);
                    }
                }
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private IEnumerable<TypeEntryToRegister> TypesToRegister()
        {
            for (int i = 0; i < _typesThatNeedTypeHandles.Count; i++)
            {
                DefType typeAsDefType = _typesThatNeedTypeHandles[i] as DefType;
                if (typeAsDefType == null)
                    continue;

                if (typeAsDefType.HasInstantiation && !typeAsDefType.IsTypeDefinition)
                {
                    yield return new TypeEntryToRegister
                    {
                        GenericTypeEntry = new GenericTypeEntry
                        {
                            _genericTypeDefinitionHandle = GetRuntimeTypeHandle(typeAsDefType.GetTypeDefinition()),
                            _genericTypeArgumentHandles = GetRuntimeTypeHandles(typeAsDefType.Instantiation),
                            _instantiatedTypeHandle = typeAsDefType.GetTypeBuilderState().HalfBakedRuntimeTypeHandle
                        }
                    };
                }
                else
                {
                    yield return new TypeEntryToRegister
                    {
                        MetadataDefinitionType = (MetadataType)typeAsDefType
                    };
                }
            }
        }

        private IEnumerable<GenericMethodEntry> MethodsToRegister()
        {
            for (int i = 0; i < _methodsThatNeedDictionaries.Count; i++)
            {
                InstantiatedMethod method = _methodsThatNeedDictionaries[i];
                yield return new GenericMethodEntry
                {
                    _declaringTypeHandle = GetRuntimeTypeHandle(method.OwningType),
                    _genericMethodArgumentHandles = GetRuntimeTypeHandles(method.Instantiation),
                    _methodNameAndSignature = method.NameAndSignature,
                    _methodDictionary = method.RuntimeMethodDictionary
                };
            }
        }

        private void RegisterGenericTypesAndMethods()
        {
            int typesToRegisterCount = 0;
            for (int i = 0; i < _typesThatNeedTypeHandles.Count; i++)
            {
                if (_typesThatNeedTypeHandles[i] is DefType)
                    typesToRegisterCount++;
            }

            DynamicGenericsRegistrationData registrationData = new DynamicGenericsRegistrationData
            {
                TypesToRegisterCount = typesToRegisterCount,
                TypesToRegister = (typesToRegisterCount != 0) ? TypesToRegister() : null,
                MethodsToRegisterCount = _methodsThatNeedDictionaries.Count,
                MethodsToRegister = (_methodsThatNeedDictionaries.Count != 0) ? MethodsToRegister() : null,
            };
            TypeLoaderEnvironment.Instance.RegisterDynamicGenericTypesAndMethods(registrationData);
        }

        /// <summary>
        /// Publish generic type / method information to the data buffer read by the debugger. This supports
        /// debugging dynamically created types / methods
        /// </summary>
        private void RegisterDebugDataForTypesAndMethods()
        {
            for (int i = 0; i < _typesThatNeedTypeHandles.Count; i++)
            {
                DefType typeAsDefType;
                if ((typeAsDefType = _typesThatNeedTypeHandles[i] as DefType) != null)
                {
                    SerializedDebugData.RegisterDebugDataForType(this, typeAsDefType, typeAsDefType.GetTypeBuilderState());
                }
            }

            for (int i = 0; i < _methodsThatNeedDictionaries.Count; i++)
            {
                SerializedDebugData.RegisterDebugDataForMethod(this, _methodsThatNeedDictionaries[i]);
            }
        }

        private void FinishTypeAndMethodBuilding()
        {
            // Once we start allocating EETypes and dictionaries, the only accepted failure is OOM.
            // TODO: Error handling - on retry, restart where we failed last time? The current implementation is leaking on OOM.

#if DEBUG
            _finalTypeBuilding = true;
#endif

            // At this point we know all types that need EETypes. Allocate all EETypes so that we can start building
            // their contents.
            for (int i = 0; i < _typesThatNeedTypeHandles.Count; i++)
            {
                AllocateRuntimeType(_typesThatNeedTypeHandles[i]);
            }

            for (int i = 0; i < _methodsThatNeedDictionaries.Count; i++)
            {
                AllocateRuntimeMethodDictionary(_methodsThatNeedDictionaries[i]);
            }

            // Do not add more type phases here. Instead, read the required information from the TypeDesc or TypeBuilderState.

            // Fill in content of all EETypes
            for (int i = 0; i < _typesThatNeedTypeHandles.Count; i++)
            {
                FinishRuntimeType(_typesThatNeedTypeHandles[i]);
            }

            for (int i = 0; i < _methodsThatNeedDictionaries.Count; i++)
            {
                FinishMethodDictionary(_methodsThatNeedDictionaries[i]);
            }

            RegisterDebugDataForTypesAndMethods();

            int newArrayTypesCount = 0;
            int newPointerTypesCount = 0;
            int newByRefTypesCount = 0;
            int[] mdArrayNewTypesCount = null;

            for (int i = 0; i < _typesThatNeedTypeHandles.Count; i++)
            {
                ParameterizedType typeAsParameterizedType = _typesThatNeedTypeHandles[i] as ParameterizedType;
                if (typeAsParameterizedType == null)
                    continue;

                if (typeAsParameterizedType.IsSzArray)
                    newArrayTypesCount++;
                else if (typeAsParameterizedType.IsPointer)
                    newPointerTypesCount++;
                else if (typeAsParameterizedType.IsByRef)
                    newByRefTypesCount++;
                else if (typeAsParameterizedType.IsMdArray)
                {
                    if (mdArrayNewTypesCount == null)
                        mdArrayNewTypesCount = new int[MDArray.MaxRank + 1];
                    mdArrayNewTypesCount[((ArrayType)typeAsParameterizedType).Rank]++;
                }
            }
            // Reserve space in array/pointer cache's so that the actual adding can be fault-free.
            var szArrayCache = TypeSystemContext.GetArrayTypesCache(false, -1);
            szArrayCache.Reserve(szArrayCache.Count + newArrayTypesCount);

            //
            if (mdArrayNewTypesCount != null)
            {
                for (int i = 0; i < mdArrayNewTypesCount.Length; i++)
                {
                    if (mdArrayNewTypesCount[i] == 0)
                        continue;

                    var mdArrayCache = TypeSystemContext.GetArrayTypesCache(true, i);
                    mdArrayCache.Reserve(mdArrayCache.Count + mdArrayNewTypesCount[i]);
                }
            }

            TypeSystemContext.PointerTypesCache.Reserve(TypeSystemContext.PointerTypesCache.Count + newPointerTypesCount);
            TypeSystemContext.ByRefTypesCache.Reserve(TypeSystemContext.ByRefTypesCache.Count + newByRefTypesCount);

            // Finally, register all generic types and methods atomically with the runtime
            RegisterGenericTypesAndMethods();


            for (int i = 0; i < _typesThatNeedTypeHandles.Count; i++)
            {
                _typesThatNeedTypeHandles[i].SetRuntimeTypeHandleUnsafe(_typesThatNeedTypeHandles[i].GetTypeBuilderState().HalfBakedRuntimeTypeHandle);

                TypeLoaderLogger.WriteLine("Successfully Registered type " + _typesThatNeedTypeHandles[i].ToString() + ".");
            }

            // Save all constructed array and pointer types to the types cache
            for (int i = 0; i < _typesThatNeedTypeHandles.Count; i++)
            {
                ParameterizedType typeAsParameterizedType = _typesThatNeedTypeHandles[i] as ParameterizedType;
                if (typeAsParameterizedType == null)
                    continue;

                Debug.Assert(!typeAsParameterizedType.RuntimeTypeHandle.IsNull());
                Debug.Assert(!typeAsParameterizedType.ParameterType.RuntimeTypeHandle.IsNull());

                if (typeAsParameterizedType.IsMdArray)
                    TypeSystemContext.GetArrayTypesCache(true, ((ArrayType)typeAsParameterizedType).Rank).AddOrGetExisting(typeAsParameterizedType.RuntimeTypeHandle);
                else if (typeAsParameterizedType.IsSzArray)
                    TypeSystemContext.GetArrayTypesCache(false, -1).AddOrGetExisting(typeAsParameterizedType.RuntimeTypeHandle);
                else if (typeAsParameterizedType.IsByRef)
                {
                    unsafe
                    {
                        Debug.Assert(typeAsParameterizedType.RuntimeTypeHandle.ToEETypePtr()->IsByRefType);
                    }
                    TypeSystemContext.ByRefTypesCache.AddOrGetExisting(typeAsParameterizedType.RuntimeTypeHandle);
                }
                else
                {
                    Debug.Assert(typeAsParameterizedType is PointerType);
                    unsafe
                    {
                        Debug.Assert(typeAsParameterizedType.RuntimeTypeHandle.ToEETypePtr()->IsPointerType);
                    }
                    TypeSystemContext.PointerTypesCache.AddOrGetExisting(typeAsParameterizedType.RuntimeTypeHandle);
                }
            }
        }

        internal void BuildType(TypeDesc type)
        {
            TypeLoaderLogger.WriteLine("Dynamically allocating new type for " + type.ToString());

            // Construct a new type along with all the dependencies that are needed to create interface lists,
            // generic dictionaries, etc.

            // Start by collecting all dependencies we need to create in order to create this type.
            PrepareType(type);

            // Process the pending types
            ProcessTypesNeedingPreparation();

            FinishTypeAndMethodBuilding();
        }

        internal static bool TryComputeFieldOffset(DefType declaringType, uint fieldOrdinal, out int fieldOffset)
        {
            TypeLoaderLogger.WriteLine("Computing offset of field #" + fieldOrdinal.LowLevelToString() + " on type " + declaringType.ToString());

            // Get the computed field offset result
            LayoutInt layoutFieldOffset = declaringType.GetFieldByNativeLayoutOrdinal(fieldOrdinal).Offset;
            if (layoutFieldOffset.IsIndeterminate)
            {
                fieldOffset = 0;
                return false;
            }
            fieldOffset = layoutFieldOffset.AsInt;
            return true;
        }

        private void BuildMethod(InstantiatedMethod method)
        {
            TypeLoaderLogger.WriteLine("Dynamically allocating new method instantiation for " + method.ToString());

            // Start by collecting all dependencies we need to create in order to create this method.
            PrepareMethod(method);

            // Process the pending types
            ProcessTypesNeedingPreparation();

            FinishTypeAndMethodBuilding();
        }

        private static DefType GetExactDeclaringType(DefType srcDefType, DefType dstDefType)
        {
            while (srcDefType != null)
            {
                if (srcDefType.HasSameTypeDefinition(dstDefType))
                    return srcDefType;

                srcDefType = srcDefType.BaseType;
            }

            Debug.Assert(false);
            return null;
        }

        //
        // This method is used by the lazy generic lookup. It resolves the signature of the runtime artifact in the given instantiation context.
        //
        private unsafe IntPtr BuildGenericLookupTarget(TypeSystemContext typeSystemContext, IntPtr context, IntPtr signature, out IntPtr auxResult)
        {
            TypeLoaderLogger.WriteLine("BuildGenericLookupTarget for " + context.LowLevelToString() + "/" + signature.LowLevelToString());

            TypeManagerHandle typeManager;
            NativeReader reader;
            uint offset;

            // The first is a pointer that points to the TypeManager indirection cell.
            // The second is the offset into the native layout info blob in that TypeManager, where the native signature is encoded.
            IntPtr** lazySignature = (IntPtr**)signature.ToPointer();
            typeManager = new TypeManagerHandle(lazySignature[0][0]);
            offset = checked((uint)new IntPtr(lazySignature[1]).ToInt32());
            reader = TypeLoaderEnvironment.GetNativeLayoutInfoReader(typeManager);

            NativeParser parser = new NativeParser(reader, offset);

            GenericContextKind contextKind = (GenericContextKind)parser.GetUnsigned();

            NativeFormatModuleInfo moduleInfo = ModuleList.Instance.GetModuleInfoByHandle(typeManager);

            NativeLayoutInfoLoadContext nlilContext = new NativeLayoutInfoLoadContext();
            nlilContext._module = moduleInfo;
            nlilContext._typeSystemContext = typeSystemContext;

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            NativeFormatMetadataUnit metadataUnit = null;

            if (moduleInfo.ModuleType == ModuleType.ReadyToRun)
                metadataUnit = typeSystemContext.ResolveMetadataUnit(moduleInfo);
#endif

            if ((contextKind & GenericContextKind.FromMethodHiddenArg) != 0)
            {
                RuntimeTypeHandle declaringTypeHandle;
                MethodNameAndSignature nameAndSignature;
                RuntimeTypeHandle[] genericMethodArgHandles;
                bool success = TypeLoaderEnvironment.Instance.TryGetGenericMethodComponents(context, out declaringTypeHandle, out nameAndSignature, out genericMethodArgHandles);
                Debug.Assert(success);

                if (RuntimeAugments.IsGenericType(declaringTypeHandle))
                {
                    DefType declaringType = (DefType)typeSystemContext.ResolveRuntimeTypeHandle(declaringTypeHandle);
                    nlilContext._typeArgumentHandles = declaringType.Instantiation;
                }

                nlilContext._methodArgumentHandles = typeSystemContext.ResolveRuntimeTypeHandles(genericMethodArgHandles);
            }
            else
            {
                TypeDesc typeContext = typeSystemContext.ResolveRuntimeTypeHandle(RuntimeAugments.CreateRuntimeTypeHandle(context));

                if (typeContext is DefType)
                {
                    nlilContext._typeArgumentHandles = ((DefType)typeContext).Instantiation;
                }
                else if (typeContext is ArrayType)
                {
                    nlilContext._typeArgumentHandles = new Instantiation(new TypeDesc[] { ((ArrayType)typeContext).ElementType });
                }
                else
                {
                    Debug.Assert(false);
                }

                if ((contextKind & GenericContextKind.HasDeclaringType) != 0)
                {
                    // No need to deal with arrays - arrays can't have declaring type

                    TypeDesc declaringType;

                    if (moduleInfo.ModuleType == ModuleType.Eager)
                    {
                        declaringType = nlilContext.GetType(ref parser);
                    }
                    else
                    {
                        Debug.Assert(moduleInfo.ModuleType == ModuleType.ReadyToRun);
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                        uint typeToken = parser.GetUnsigned();
                        declaringType = metadataUnit.GetType(((int)typeToken).AsHandle());
#else
                        Environment.FailFast("Ready to Run module type?");
                        declaringType = null;
#endif
                    }

                    DefType actualContext = GetExactDeclaringType((DefType)typeContext, (DefType)declaringType);

                    nlilContext._typeArgumentHandles = actualContext.Instantiation;
                }
            }

            if ((contextKind & GenericContextKind.NeedsUSGContext) != 0)
            {
                IntPtr genericDictionary;
                auxResult = IntPtr.Zero;

                // There is a cache in place so that this function doesn't get called much, but we still need a registration store,
                // so we don't leak allocated contexts
                if (TypeLoaderEnvironment.Instance.TryLookupConstructedLazyDictionaryForContext(context, signature, out genericDictionary))
                {
                    return genericDictionary;
                }

                GenericTypeDictionary ucgDict;

                if (moduleInfo.ModuleType == ModuleType.Eager)
                {
                    ucgDict = new GenericTypeDictionary(GenericDictionaryCell.BuildDictionary(this, nlilContext, parser));
                }
                else
                {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                    Debug.Assert(moduleInfo.ModuleType == ModuleType.ReadyToRun);
                    FixupCellMetadataResolver metadataResolver = new FixupCellMetadataResolver(metadataUnit, nlilContext);
                    ucgDict = new GenericTypeDictionary(GenericDictionaryCell.BuildDictionaryFromMetadataTokensAndContext(this, parser, metadataUnit, metadataResolver));
#else
                    Environment.FailFast("Ready to Run module type?");
                    ucgDict = null;
#endif
                }
                genericDictionary = ucgDict.Allocate();

                // Process the pending types
                ProcessTypesNeedingPreparation();

                FinishTypeAndMethodBuilding();

                ucgDict.Finish(this);

                TypeLoaderEnvironment.Instance.RegisterConstructedLazyDictionaryForContext(context, signature, genericDictionary);
                return genericDictionary;
            }
            else
            {
                GenericDictionaryCell cell;

                if (moduleInfo.ModuleType == ModuleType.Eager)
                {
                    cell = GenericDictionaryCell.ParseAndCreateCell(
                        nlilContext,
                        ref parser);
                }
                else
                {
                    Debug.Assert(moduleInfo.ModuleType == ModuleType.ReadyToRun);
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                    MetadataFixupKind fixupKind = (MetadataFixupKind)parser.GetUInt8();
                    Internal.Metadata.NativeFormat.Handle token = parser.GetUnsigned().AsHandle();
                    Internal.Metadata.NativeFormat.Handle token2 = default(Internal.Metadata.NativeFormat.Handle);

                    switch (fixupKind)
                    {
                        case MetadataFixupKind.GenericConstrainedMethod:
                        case MetadataFixupKind.NonGenericConstrainedMethod:
                        case MetadataFixupKind.NonGenericDirectConstrainedMethod:
                            token2 = parser.GetUnsigned().AsHandle();
                            break;
                    }

                    FixupCellMetadataResolver resolver = new FixupCellMetadataResolver(metadataUnit, nlilContext);
                    cell = GenericDictionaryCell.CreateCellFromFixupKindAndToken(fixupKind, resolver, token, token2);
#else
                    Environment.FailFast("Ready to Run module type?");
                    cell = null;
#endif
                }

                cell.Prepare(this);

                // Process the pending types
                ProcessTypesNeedingPreparation();

                FinishTypeAndMethodBuilding();

                IntPtr dictionaryCell = cell.CreateLazyLookupCell(this, out auxResult);

                return dictionaryCell;
            }
        }

        //
        // This method is used to build the floating portion of a generic dictionary.
        //
        private unsafe IntPtr BuildFloatingDictionary(TypeSystemContext typeSystemContext, IntPtr context, bool isTypeContext, IntPtr fixedDictionary, out bool isNewlyAllocatedDictionary)
        {
            isNewlyAllocatedDictionary = true;

            NativeParser nativeLayoutParser;
            NativeLayoutInfoLoadContext nlilContext;

            if (isTypeContext)
            {
                TypeDesc typeContext = typeSystemContext.ResolveRuntimeTypeHandle(*(RuntimeTypeHandle*)&context);

                TypeLoaderLogger.WriteLine("Building floating dictionary layout for type " + typeContext.ToString() + "...");

                // We should only perform updates to floating dictionaries for types that share normal canonical code
                Debug.Assert(typeContext.CanShareNormalGenericCode());

                // Computing the template will throw if no template is found.
                typeContext.ComputeTemplate();

                TypeBuilderState state = typeContext.GetOrCreateTypeBuilderState();
                nativeLayoutParser = state.GetParserForNativeLayoutInfo();
                nlilContext = state.NativeLayoutInfo.LoadContext;
            }
            else
            {
                RuntimeTypeHandle declaringTypeHandle;
                MethodNameAndSignature nameAndSignature;
                RuntimeTypeHandle[] genericMethodArgHandles;
                bool success = TypeLoaderEnvironment.Instance.TryGetGenericMethodComponents(context, out declaringTypeHandle, out nameAndSignature, out genericMethodArgHandles);
                Debug.Assert(success);

                DefType declaringType = (DefType)typeSystemContext.ResolveRuntimeTypeHandle(declaringTypeHandle);
                InstantiatedMethod methodContext = (InstantiatedMethod)typeSystemContext.ResolveGenericMethodInstantiation(
                    false,
                    declaringType,
                    nameAndSignature,
                    typeSystemContext.ResolveRuntimeTypeHandles(genericMethodArgHandles),
                    IntPtr.Zero,
                    false);

                TypeLoaderLogger.WriteLine("Building floating dictionary layout for method " + methodContext.ToString() + "...");

                // We should only perform updates to floating dictionaries for gemeric methods that share normal canonical code
                Debug.Assert(!methodContext.IsNonSharableMethod);

                uint nativeLayoutInfoToken;
                NativeFormatModuleInfo nativeLayoutModule;
                MethodDesc templateMethod = TemplateLocator.TryGetGenericMethodTemplate(methodContext, out nativeLayoutModule, out nativeLayoutInfoToken);
                if (templateMethod == null)
                    throw new TypeBuilder.MissingTemplateException();

                NativeReader nativeLayoutInfoReader = TypeLoaderEnvironment.GetNativeLayoutInfoReader(nativeLayoutModule.Handle);

                nativeLayoutParser = new NativeParser(nativeLayoutInfoReader, nativeLayoutInfoToken);
                nlilContext = new NativeLayoutInfoLoadContext
                {
                    _typeSystemContext = methodContext.Context,
                    _typeArgumentHandles = methodContext.OwningType.Instantiation,
                    _methodArgumentHandles = methodContext.Instantiation,
                    _module = nativeLayoutModule
                };
            }

            NativeParser dictionaryLayoutParser = nativeLayoutParser.GetParserForBagElementKind(BagElementKind.DictionaryLayout);
            if (dictionaryLayoutParser.IsNull)
                return IntPtr.Zero;

            int floatingVersionCellIndex, floatingVersionInLayout;
            GenericDictionaryCell[] floatingCells = GenericDictionaryCell.BuildFloatingDictionary(this, nlilContext, dictionaryLayoutParser, out floatingVersionCellIndex, out floatingVersionInLayout);
            if (floatingCells == null)
                return IntPtr.Zero;

            // If the floating section is already constructed, then return. This means we are beaten by another thread.
            if (*((IntPtr*)fixedDictionary) != IntPtr.Zero)
            {
                isNewlyAllocatedDictionary = false;
                return *((IntPtr*)fixedDictionary);
            }

            GenericTypeDictionary floatingDict = new GenericTypeDictionary(floatingCells);

            IntPtr result = floatingDict.Allocate();

            ProcessTypesNeedingPreparation();

            FinishTypeAndMethodBuilding();

            floatingDict.Finish(this);

            return result;
        }

        public static bool TryBuildGenericType(RuntimeTypeHandle genericTypeDefinitionHandle, RuntimeTypeHandle[] genericTypeArgumentHandles, out RuntimeTypeHandle runtimeTypeHandle)
        {
            Debug.Assert(!genericTypeDefinitionHandle.IsNull() && genericTypeArgumentHandles != null && genericTypeArgumentHandles.Length > 0);

            try
            {
                TypeSystemContext context = TypeSystemContextFactory.Create();

                DefType genericDef = (DefType)context.ResolveRuntimeTypeHandle(genericTypeDefinitionHandle);
                Instantiation genericArgs = context.ResolveRuntimeTypeHandles(genericTypeArgumentHandles);
                DefType typeBeingLoaded = context.ResolveGenericInstantiation(genericDef, genericArgs);

                new TypeBuilder().BuildType(typeBeingLoaded);

                runtimeTypeHandle = typeBeingLoaded.RuntimeTypeHandle;
                Debug.Assert(!runtimeTypeHandle.IsNull());

                // Recycle the context only if we succesfully built the type. The state may be partially initialized otherwise.
                TypeSystemContextFactory.Recycle(context);

                return true;
            }
            catch (MissingTemplateException)
            {
                runtimeTypeHandle = default(RuntimeTypeHandle);
                return false;
            }
        }

        public static bool TryBuildArrayType(RuntimeTypeHandle elementTypeHandle, bool isMdArray, int rank, out RuntimeTypeHandle arrayTypeHandle)
        {
            try
            {
                TypeSystemContext context = TypeSystemContextFactory.Create();

                TypeDesc elementType = context.ResolveRuntimeTypeHandle(elementTypeHandle);
                ArrayType arrayType = (ArrayType)context.GetArrayType(elementType, !isMdArray ? -1 : rank);

                new TypeBuilder().BuildType(arrayType);

                arrayTypeHandle = arrayType.RuntimeTypeHandle;
                Debug.Assert(!arrayTypeHandle.IsNull());

                // Recycle the context only if we succesfully built the type. The state may be partially initialized otherwise.
                TypeSystemContextFactory.Recycle(context);

                return true;
            }
            catch (MissingTemplateException)
            {
                arrayTypeHandle = default(RuntimeTypeHandle);
                return false;
            }
        }

        public static bool TryBuildPointerType(RuntimeTypeHandle pointeeTypeHandle, out RuntimeTypeHandle pointerTypeHandle)
        {
            if (!TypeSystemContext.PointerTypesCache.TryGetValue(pointeeTypeHandle, out pointerTypeHandle))
            {
                TypeSystemContext context = TypeSystemContextFactory.Create();
                TypeDesc pointerType = context.GetPointerType(context.ResolveRuntimeTypeHandle(pointeeTypeHandle));
                pointerTypeHandle = EETypeCreator.CreatePointerEEType((uint)pointerType.GetHashCode(), pointeeTypeHandle, pointerType);
                unsafe
                {
                    Debug.Assert(pointerTypeHandle.ToEETypePtr()->IsPointerType);
                }
                TypeSystemContext.PointerTypesCache.AddOrGetExisting(pointerTypeHandle);

                // Recycle the context only if we succesfully built the type. The state may be partially initialized otherwise.
                TypeSystemContextFactory.Recycle(context);
            }

            return true;
        }

        public static bool TryBuildByRefType(RuntimeTypeHandle pointeeTypeHandle, out RuntimeTypeHandle byRefTypeHandle)
        {
            if (!TypeSystemContext.ByRefTypesCache.TryGetValue(pointeeTypeHandle, out byRefTypeHandle))
            {
                TypeSystemContext context = TypeSystemContextFactory.Create();
                TypeDesc byRefType = context.GetByRefType(context.ResolveRuntimeTypeHandle(pointeeTypeHandle));
                byRefTypeHandle = EETypeCreator.CreateByRefEEType((uint)byRefType.GetHashCode(), pointeeTypeHandle, byRefType);
                unsafe
                {
                    Debug.Assert(byRefTypeHandle.ToEETypePtr()->IsByRefType);
                }
                TypeSystemContext.ByRefTypesCache.AddOrGetExisting(byRefTypeHandle);

                // Recycle the context only if we succesfully built the type. The state may be partially initialized otherwise.
                TypeSystemContextFactory.Recycle(context);
            }

            return true;
        }

        public static bool TryBuildGenericMethod(RuntimeTypeHandle declaringTypeHandle, RuntimeTypeHandle[] genericMethodArgHandles, MethodNameAndSignature methodNameAndSignature, out IntPtr methodDictionary)
        {
            TypeSystemContext context = TypeSystemContextFactory.Create();

            DefType declaringType = (DefType)context.ResolveRuntimeTypeHandle(declaringTypeHandle);
            InstantiatedMethod methodBeingLoaded = (InstantiatedMethod)context.ResolveGenericMethodInstantiation(false, declaringType, methodNameAndSignature, context.ResolveRuntimeTypeHandles(genericMethodArgHandles), IntPtr.Zero, false);

            bool success = TryBuildGenericMethod(methodBeingLoaded, out methodDictionary);

            // Recycle the context only if we succesfully built the method. The state may be partially initialized otherwise.
            if (success)
                TypeSystemContextFactory.Recycle(context);

            return success;
        }

        internal static bool TryBuildGenericMethod(InstantiatedMethod methodBeingLoaded, out IntPtr methodDictionary)
        {
            try
            {
                new TypeBuilder().BuildMethod(methodBeingLoaded);

                methodDictionary = methodBeingLoaded.RuntimeMethodDictionary;
                Debug.Assert(methodDictionary != IntPtr.Zero);

                return true;
            }
            catch (MissingTemplateException)
            {
                methodDictionary = IntPtr.Zero;
                return false;
            }
        }

        private void ResolveSingleCell_Worker(GenericDictionaryCell cell, out IntPtr fixupResolution)
        {
            cell.Prepare(this);

            // Process the pending types
            ProcessTypesNeedingPreparation();
            FinishTypeAndMethodBuilding();

            // At this stage the pointer we need is accessible via a call to Create on the prepared cell
            fixupResolution = cell.Create(this);
        }

        private void ResolveMultipleCells_Worker(GenericDictionaryCell[] cells, out IntPtr[] fixups)
        {
            foreach (var cell in cells)
            {
                cell.Prepare(this);
            }

            // Process the pending types
            ProcessTypesNeedingPreparation();
            FinishTypeAndMethodBuilding();

            // At this stage the pointer we need is accessible via a call to Create on the prepared cell
            fixups = new IntPtr[cells.Length];
            for (int i = 0; i < fixups.Length; i++)
                fixups[i] = cells[i].Create(this);
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        private void ResolveSingleMetadataFixup(NativeFormatMetadataUnit module, Handle token, MetadataFixupKind fixupKind, out IntPtr fixupResolution)
        {
            FixupCellMetadataResolver metadata = new FixupCellMetadataResolver(module);

            // Allocate a cell object to represent the fixup, and prepare it
            GenericDictionaryCell cell = GenericDictionaryCell.CreateCellFromFixupKindAndToken(fixupKind, metadata, token, default(Handle));
            ResolveSingleCell_Worker(cell, out fixupResolution);
        }

        public static bool TryResolveSingleMetadataFixup(NativeFormatModuleInfo module, int metadataToken, MetadataFixupKind fixupKind, out IntPtr fixupResolution)
        {
            TypeSystemContext context = TypeSystemContextFactory.Create();

            NativeFormatMetadataUnit metadataUnit = context.ResolveMetadataUnit(module);
            new TypeBuilder().ResolveSingleMetadataFixup(metadataUnit, metadataToken.AsHandle(), fixupKind, out fixupResolution);

            TypeSystemContextFactory.Recycle(context);

            return true;
        }

        public static void ResolveSingleTypeDefinition(QTypeDefinition qTypeDefinition, out IntPtr typeHandle)
        {
            TypeSystemContext context = TypeSystemContextFactory.Create();

            TypeDesc type = context.GetTypeDescFromQHandle(qTypeDefinition);
            GenericDictionaryCell cell = GenericDictionaryCell.CreateTypeHandleCell(type);

            new TypeBuilder().ResolveSingleCell_Worker(cell, out typeHandle);

            TypeSystemContextFactory.Recycle(context);
        }
#endif

        internal static void ResolveSingleCell(GenericDictionaryCell cell, out IntPtr fixupResolution)
        {
            new TypeBuilder().ResolveSingleCell_Worker(cell, out fixupResolution);
        }

        public static void ResolveMultipleCells(GenericDictionaryCell [] cells, out IntPtr[] fixups)
        {
            new TypeBuilder().ResolveMultipleCells_Worker(cells, out fixups);
        }

        public static IntPtr BuildGenericLookupTarget(IntPtr typeContext, IntPtr signature, out IntPtr auxResult)
        {
            try
            {
                TypeSystemContext context = TypeSystemContextFactory.Create();

                IntPtr ret = new TypeBuilder().BuildGenericLookupTarget(context, typeContext, signature, out auxResult);

                TypeSystemContextFactory.Recycle(context);

                return ret;
            }
            catch (MissingTemplateException e)
            {
                // This should not ever happen. The static compiler should ensure that the templates are always
                // available for types and methods referenced by lazy dictionary lookups
                Environment.FailFast("MissingTemplateException thrown during lazy generic lookup", e);

                auxResult = IntPtr.Zero;
                return IntPtr.Zero;
            }
        }

        public static bool TryGetFieldOffset(RuntimeTypeHandle declaringTypeHandle, uint fieldOrdinal, out int fieldOffset)
        {
            try
            {
                TypeSystemContext context = TypeSystemContextFactory.Create();

                DefType declaringType = (DefType)context.ResolveRuntimeTypeHandle(declaringTypeHandle);
                Debug.Assert(declaringType.HasInstantiation);

                bool success = TypeBuilder.TryComputeFieldOffset(declaringType, fieldOrdinal, out fieldOffset);

                TypeSystemContextFactory.Recycle(context);

                return success;
            }
            catch (MissingTemplateException)
            {
                fieldOffset = int.MinValue;
                return false;
            }
        }

        internal static bool TryGetDelegateInvokeMethodSignature(RuntimeTypeHandle delegateTypeHandle, out RuntimeSignature signature)
        {
            signature = default(RuntimeSignature);
            bool success = false;

            TypeSystemContext context = TypeSystemContextFactory.Create();

            DefType delegateType = (DefType)context.ResolveRuntimeTypeHandle(delegateTypeHandle);
            Debug.Assert(delegateType.HasInstantiation);

            NativeLayoutInfo universalLayoutInfo;
            NativeParser parser = delegateType.GetOrCreateTypeBuilderState().GetParserForUniversalNativeLayoutInfo(out _, out universalLayoutInfo);
            if (!parser.IsNull)
            {
                NativeParser sigParser = parser.GetParserForBagElementKind(BagElementKind.DelegateInvokeSignature);
                if (!sigParser.IsNull)
                {
                    signature = RuntimeSignature.CreateFromNativeLayoutSignature(universalLayoutInfo.Module.Handle, sigParser.Offset);
                    success = true;
                }
            }

            TypeSystemContextFactory.Recycle(context);

            return success;
        }

        //
        // This method is used to build the floating portion of a generic dictionary.
        //
        internal static IntPtr TryBuildFloatingDictionary(IntPtr context, bool isTypeContext, IntPtr fixedDictionary, out bool isNewlyAllocatedDictionary)
        {
            isNewlyAllocatedDictionary = true;

            try
            {
                TypeSystemContext typeSystemContext = TypeSystemContextFactory.Create();

                IntPtr ret = new TypeBuilder().BuildFloatingDictionary(typeSystemContext, context, isTypeContext, fixedDictionary, out isNewlyAllocatedDictionary);

                TypeSystemContextFactory.Recycle(typeSystemContext);

                return ret;
            }
            catch (MissingTemplateException e)
            {
                // This should not ever happen. The static compiler should ensure that the templates are always
                // available for types and methods that have floating dictionaries
                Environment.FailFast("MissingTemplateException thrown during dictionary update", e);

                return IntPtr.Zero;
            }
        }
    }
}
