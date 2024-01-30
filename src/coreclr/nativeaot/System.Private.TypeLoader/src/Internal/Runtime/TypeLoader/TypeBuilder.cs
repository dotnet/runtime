// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.NativeFormat;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;
using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    internal static class LowLevelListExtensions
    {
        public static void Expand<T>(this LowLevelList<T> list, int count)
        {
            if (list.Capacity < count)
                list.Capacity = count;

            while (list.Count < count)
                list.Add(default(T));
        }
    }

    internal class TypeBuilder
    {
        public TypeBuilder()
        {
            TypeLoaderEnvironment.Instance.VerifyTypeLoaderLockHeld();
        }

        /// <summary>
        /// The StaticClassConstructionContext for a type is encoded in the negative space
        /// of the NonGCStatic fields of a type.
        /// </summary>
        public static unsafe int ClassConstructorOffset => -sizeof(System.Runtime.CompilerServices.StaticClassConstructionContext);

        private LowLevelList<TypeDesc> _typesThatNeedTypeHandles = new LowLevelList<TypeDesc>();

        private LowLevelList<InstantiatedMethod> _methodsThatNeedDictionaries = new LowLevelList<InstantiatedMethod>();

        private LowLevelList<TypeDesc> _typesThatNeedPreparation;

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


        internal static bool RetrieveMethodDictionaryIfPossible(InstantiatedMethod method)
        {
            if (method.RuntimeMethodDictionary != IntPtr.Zero)
                return true;

            TypeLoaderLogger.WriteLine("Looking for method dictionary for method " + method.ToString() + " ... ");

            IntPtr methodDictionary;

            if (TypeLoaderEnvironment.Instance.TryLookupGenericMethodDictionary(new TypeLoaderEnvironment.MethodDescBasedGenericMethodLookup(method), out methodDictionary))
            {
                TypeLoaderLogger.WriteLine("Found DICT = " + methodDictionary.LowLevelToString() + " for method " + method.ToString());
                method.AssociateWithRuntimeMethodDictionary(methodDictionary);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Register the type for preparation. The preparation will be done once the current type is prepared.
        /// This is the preferred way to get a dependent type prepared because of it avoids issues with cycles and recursion.
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

            _typesThatNeedPreparation ??= new LowLevelList<TypeDesc>();

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

        private void InsertIntoNeedsTypeHandleList(TypeDesc type)
        {
            if ((type is DefType) || (type is ArrayType) || (type is PointerType) || (type is ByRefType) || (type is FunctionPointerType))
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

            state ??= type.GetOrCreateTypeBuilderState();

            // If this type was already prepared, do nothing unless we are re-preparing it for the purpose of loading the field layout
            if (state.HasBeenPrepared)
            {
                return;
            }

            state.HasBeenPrepared = true;
            state.NeedsTypeHandle = true;

            if (!hasTypeHandle)
            {
                InsertIntoNeedsTypeHandleList(type);
            }

            bool noExtraPreparation = false; // Set this to true for types which don't need other types to be prepared. I.e GenericTypeDefinitions

            if (type is DefType typeAsDefType)
            {
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

                        ParseNativeLayoutInfo(state, type);
                    }
                }

                if (!noExtraPreparation)
                    state.PrepareStaticGCLayout();
            }
            else if (type is ParameterizedType)
            {
                PrepareType(((ParameterizedType)type).ParameterType);

                if (type is ArrayType typeAsArrayType)
                {
                    if (typeAsArrayType.IsSzArray && !typeAsArrayType.ElementType.IsPointer && !typeAsArrayType.ElementType.IsFunctionPointer)
                    {
                        TypeDesc.ComputeTemplate(state);
                        Debug.Assert(state.TemplateType != null && state.TemplateType is ArrayType && !state.TemplateType.RuntimeTypeHandle.IsNull());

                        ParseNativeLayoutInfo(state, type);
                    }
                    else
                    {
                        Debug.Assert(typeAsArrayType.IsMdArray || typeAsArrayType.ElementType.IsPointer || typeAsArrayType.ElementType.IsFunctionPointer);
                    }
                }
            }
            else if (type is FunctionPointerType functionPointerType)
            {
                RegisterForPreparation(functionPointerType.Signature.ReturnType);
                foreach (TypeDesc paramType in functionPointerType.Signature)
                    RegisterForPreparation(paramType);
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

                TypeLoaderLogger.WriteLine("Layout for type " + type.ToString() + " complete.");
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
            InstantiatedMethod templateMethod = TemplateLocator.TryGetGenericMethodTemplate(nonTemplateMethod, out nativeLayoutModule, out nativeLayoutInfoToken);
            if (templateMethod == null)
            {
                throw new MissingTemplateException();
            }

            if (templateMethod.FunctionPointer != IntPtr.Zero)
            {
                nonTemplateMethod.SetFunctionPointer(templateMethod.FunctionPointer, isFunctionPointerUSG: false);
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

            if (state.TemplateType == null)
            {
                throw new MissingTemplateException();
            }

            NativeParser typeInfoParser = state.GetParserForNativeLayoutInfo();
            NativeLayoutInfoLoadContext context = state.NativeLayoutInfo.LoadContext;

            NativeParser baseTypeParser = default;

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

                    case BagElementKind.ImplementedInterfaces:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.ImplementedInterfaces");
                        // Interface handling is done entirely in NativeLayoutInterfacesAlgorithm
                        typeInfoParser.GetUnsigned();
                        break;

                    case BagElementKind.ClassConstructorPointer:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.ClassConstructorPointer");
                        state.ClassConstructorPointer = context.GetGCStaticInfo(typeInfoParser.GetUnsigned());
                        break;

                    case BagElementKind.NonGcStaticDataSize:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.NonGcStaticDataSize");
                        // Use checked typecast to int to ensure there aren't any overflows/truncations (size value used in allocation of memory later)
                        state.NonGcDataSize = checked((int)typeInfoParser.GetUnsigned());
                        break;

                    case BagElementKind.GcStaticDataSize:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.GcStaticDataSize");
                        // Use checked typecast to int to ensure there aren't any overflows/truncations (size value used in allocation of memory later)
                        state.GcDataSize = checked((int)typeInfoParser.GetUnsigned());
                        break;

                    case BagElementKind.ThreadStaticDataSize:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.ThreadStaticDataSize");
                        // Use checked typecast to int to ensure there aren't any overflows/truncations (size value used in allocation of memory later)
                        state.ThreadDataSize = checked((int)typeInfoParser.GetUnsigned());
                        break;

                    case BagElementKind.GcStaticDesc:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.GcStaticDesc");
                        state.GcStaticDesc = context.GetGCStaticInfo(typeInfoParser.GetUnsigned());
                        break;

                    case BagElementKind.ThreadStaticDesc:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.ThreadStaticDesc");
                        state.ThreadStaticDesc = context.GetGCStaticInfo(typeInfoParser.GetUnsigned());
                        break;

                    case BagElementKind.FieldLayout:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.FieldLayout");
                        typeInfoParser.SkipInteger(); // Handled in type layout algorithm
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

            type.ParseBaseType(context, baseTypeParser);
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

            public static GCLayout None { get { return default(GCLayout); } }
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
                _gcdesc = MethodTable->ContainsGCPointers ? (void**)MethodTable - 1 : null;
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
                ArgumentNullException.ThrowIfNull(bitfield);

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

        private unsafe void AllocateRuntimeType(TypeDesc type)
        {
            TypeBuilderState state = type.GetTypeBuilderState();

            Debug.Assert(type is DefType || type is ArrayType || type is PointerType || type is ByRefType || type is FunctionPointerType);

            RuntimeTypeHandle rtt = EETypeCreator.CreateEEType(type, state);

            if (state.ThreadDataSize != 0)
                TypeLoaderEnvironment.Instance.RegisterDynamicThreadStaticsInfo(state.HalfBakedRuntimeTypeHandle, state.ThreadStaticOffset, state.ThreadStaticDesc);

            TypeLoaderLogger.WriteLine("Allocated new type " + type.ToString() + " with hashcode value = 0x" + type.GetHashCode().LowLevelToString() + " with MethodTable = " + rtt.ToIntPtr().LowLevelToString() + " of size " + rtt.ToEETypePtr()->RawBaseSize.LowLevelToString());
        }

        private static void AllocateRuntimeMethodDictionary(InstantiatedMethod method)
        {
            Debug.Assert(method.RuntimeMethodDictionary == IntPtr.Zero && method.Dictionary != null);

            IntPtr rmd = method.Dictionary.Allocate();
            method.AssociateWithRuntimeMethodDictionary(rmd);

            TypeLoaderLogger.WriteLine("Allocated new method dictionary for method " + method.ToString() + " @ " + rmd.LowLevelToString());
        }

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

        private void FinishInterfaces(TypeBuilderState state)
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

        private unsafe void FinishTypeDictionary(TypeDesc type)
        {
            TypeBuilderState state = type.GetTypeBuilderState();

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

            IntPtr canonicalClassConstructorFunctionPointer = state.ClassConstructorPointer.Value;

            IntPtr generatedTypeStaticData = GetRuntimeTypeHandle(type).ToEETypePtr()->DynamicNonGcStaticsData;
            IntPtr* generatedTypeClassConstructorSlotPointer = (IntPtr*)((byte*)generatedTypeStaticData + ClassConstructorOffset);

            // Use the template type's class constructor method pointer and this type's generic type dictionary to generate a new fat pointer,
            // and save that fat pointer back to this type's class constructor context offset within the non-GC static data.
            IntPtr instantiationArgument = GetRuntimeTypeHandle(type).ToIntPtr();
            IntPtr generatedTypeClassConstructorFatFunctionPointer = FunctionPointerOps.GetGenericMethodFunctionPointer(canonicalClassConstructorFunctionPointer, instantiationArgument);
            *generatedTypeClassConstructorSlotPointer = generatedTypeClassConstructorFatFunctionPointer;
        }

        private void CopyDictionaryFromTypeToAppropriateSlotInDerivedType(DefType baseType, TypeBuilderState derivedTypeState)
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

            if (type is DefType typeAsDefType)
            {
                if (type.HasInstantiation)
                {
                    // Type definitions don't need any further finishing once created by the EETypeCreator
                    if (type.IsTypeDefinition)
                        return;

                    state.HalfBakedRuntimeTypeHandle.SetGenericDefinition(GetRuntimeTypeHandle(typeAsDefType.GetTypeDefinition()));
                    Instantiation instantiation = typeAsDefType.Instantiation;
                    for (int argIndex = 0; argIndex < instantiation.Length; argIndex++)
                    {
                        state.HalfBakedRuntimeTypeHandle.SetGenericArgument(argIndex, GetRuntimeTypeHandle(instantiation[argIndex]));
                    }
                }

                FinishBaseTypeAndDictionaries(type, state);

                FinishInterfaces(state);

                FinishClassConstructor(type, state);
            }
            else if (type is ParameterizedType)
            {
                if (type is ArrayType typeAsSzArrayType)
                {
                    RuntimeTypeHandle elementTypeHandle = GetRuntimeTypeHandle(typeAsSzArrayType.ElementType);
                    state.HalfBakedRuntimeTypeHandle.SetRelatedParameterType(elementTypeHandle);

                    ushort componentSize = (ushort)IntPtr.Size;
                    unsafe
                    {
                        if (typeAsSzArrayType.ElementType.IsValueType)
                            componentSize = checked((ushort)elementTypeHandle.ToEETypePtr()->ValueTypeSize);
                    }
                    state.HalfBakedRuntimeTypeHandle.SetComponentSize(componentSize);

                    FinishInterfaces(state);
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
                        state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->ElementType = EETypeElementType.ByRef;
                    }
                }
            }
            else if (type is FunctionPointerType)
            {
                MethodSignature sig = ((FunctionPointerType)type).Signature;
                unsafe
                {
                    MethodTable* halfBakedMethodTable = state.HalfBakedRuntimeTypeHandle.ToEETypePtr();
                    halfBakedMethodTable->FunctionPointerReturnType = GetRuntimeTypeHandle(sig.ReturnType).ToEETypePtr();
                    Debug.Assert(halfBakedMethodTable->NumFunctionPointerParameters == sig.Length);
                    MethodTableList paramList = halfBakedMethodTable->FunctionPointerParameters;
                    for (int i = 0; i < sig.Length; i++)
                        paramList[i] = GetRuntimeTypeHandle(sig[i]).ToEETypePtr();
                }
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private IEnumerable<TypeLoaderEnvironment.GenericTypeEntry> TypesToRegister()
        {
            for (int i = 0; i < _typesThatNeedTypeHandles.Count; i++)
            {
                DefType typeAsDefType = _typesThatNeedTypeHandles[i] as DefType;
                if (typeAsDefType == null)
                    continue;

                yield return new TypeLoaderEnvironment.GenericTypeEntry
                {
                    _genericTypeDefinitionHandle = GetRuntimeTypeHandle(typeAsDefType.GetTypeDefinition()),
                    _genericTypeArgumentHandles = GetRuntimeTypeHandles(typeAsDefType.Instantiation),
                    _instantiatedTypeHandle = typeAsDefType.GetTypeBuilderState().HalfBakedRuntimeTypeHandle
                };
            }
        }

        private IEnumerable<TypeLoaderEnvironment.GenericMethodEntry> MethodsToRegister()
        {
            for (int i = 0; i < _methodsThatNeedDictionaries.Count; i++)
            {
                InstantiatedMethod method = _methodsThatNeedDictionaries[i];
                yield return new TypeLoaderEnvironment.GenericMethodEntry
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

            var registrationData = new TypeLoaderEnvironment.DynamicGenericsRegistrationData
            {
                TypesToRegisterCount = typesToRegisterCount,
                TypesToRegister = (typesToRegisterCount != 0) ? TypesToRegister() : null,
                MethodsToRegisterCount = _methodsThatNeedDictionaries.Count,
                MethodsToRegister = (_methodsThatNeedDictionaries.Count != 0) ? MethodsToRegister() : null,
            };
            TypeLoaderEnvironment.Instance.RegisterDynamicGenericTypesAndMethods(registrationData);
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

            for (int i = 0; i < _typesThatNeedTypeHandles.Count; i++)
            {
                FinishTypeDictionary(_typesThatNeedTypeHandles[i]);
            }

            for (int i = 0; i < _methodsThatNeedDictionaries.Count; i++)
            {
                FinishMethodDictionary(_methodsThatNeedDictionaries[i]);
            }

            int newArrayTypesCount = 0;
            int newPointerTypesCount = 0;
            int newByRefTypesCount = 0;
            int newFunctionPointerTypesCount = 0;
            int[] mdArrayNewTypesCount = null;

            for (int i = 0; i < _typesThatNeedTypeHandles.Count; i++)
            {
                TypeDesc type = _typesThatNeedTypeHandles[i];

                if (type.IsSzArray)
                    newArrayTypesCount++;
                else if (type.IsPointer)
                    newPointerTypesCount++;
                else if (type.IsFunctionPointer)
                    newFunctionPointerTypesCount++;
                else if (type.IsByRef)
                    newByRefTypesCount++;
                else if (type.IsMdArray)
                {
                    mdArrayNewTypesCount ??= new int[MDArray.MaxRank + 1];
                    mdArrayNewTypesCount[((ArrayType)type).Rank]++;
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
            TypeSystemContext.FunctionPointerTypesCache.Reserve(TypeSystemContext.FunctionPointerTypesCache.Count + newFunctionPointerTypesCount);

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
                {
                    if (_typesThatNeedTypeHandles[i] is FunctionPointerType typeAsFunctionPointerType)
                    {
                        Debug.Assert(!typeAsFunctionPointerType.RuntimeTypeHandle.IsNull());
                        TypeSystemContext.FunctionPointerTypesCache.AddOrGetExisting(typeAsFunctionPointerType.RuntimeTypeHandle);
                    }
                    continue;
                }

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
                        Debug.Assert(typeAsParameterizedType.RuntimeTypeHandle.ToEETypePtr()->IsByRef);
                    }
                    TypeSystemContext.ByRefTypesCache.AddOrGetExisting(typeAsParameterizedType.RuntimeTypeHandle);
                }
                else
                {
                    Debug.Assert(typeAsParameterizedType is PointerType);
                    unsafe
                    {
                        Debug.Assert(typeAsParameterizedType.RuntimeTypeHandle.ToEETypePtr()->IsPointer);
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

            if ((contextKind & GenericContextKind.FromMethodHiddenArg) != 0)
            {
                RuntimeTypeHandle declaringTypeHandle;
                RuntimeTypeHandle[] genericMethodArgHandles;
                bool success = TypeLoaderEnvironment.TryGetGenericMethodComponents(context, out declaringTypeHandle, out genericMethodArgHandles);
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
                    TypeDesc declaringType = nlilContext.GetType(ref parser);
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

                GenericTypeDictionary ucgDict = new GenericTypeDictionary(GenericDictionaryCell.BuildDictionary(this, nlilContext, parser));
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
                GenericDictionaryCell cell = GenericDictionaryCell.ParseAndCreateCell(nlilContext, ref parser);
                cell.Prepare(this);

                // Process the pending types
                ProcessTypesNeedingPreparation();

                FinishTypeAndMethodBuilding();

                IntPtr dictionaryCell = cell.CreateLazyLookupCell(this, out auxResult);

                return dictionaryCell;
            }
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

                // Recycle the context only if we successfully built the type. The state may be partially initialized otherwise.
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

                // Recycle the context only if we successfully built the type. The state may be partially initialized otherwise.
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
                PointerType pointerType = context.GetPointerType(context.ResolveRuntimeTypeHandle(pointeeTypeHandle));
                pointerTypeHandle = EETypeCreator.CreatePointerEEType((uint)pointerType.GetHashCode(), pointeeTypeHandle, pointerType);
                unsafe
                {
                    Debug.Assert(pointerTypeHandle.ToEETypePtr()->IsPointer);
                }
                TypeSystemContext.PointerTypesCache.AddOrGetExisting(pointerTypeHandle);

                // Recycle the context only if we successfully built the type. The state may be partially initialized otherwise.
                TypeSystemContextFactory.Recycle(context);
            }

            return true;
        }

        public static bool TryBuildByRefType(RuntimeTypeHandle pointeeTypeHandle, out RuntimeTypeHandle byRefTypeHandle)
        {
            if (!TypeSystemContext.ByRefTypesCache.TryGetValue(pointeeTypeHandle, out byRefTypeHandle))
            {
                TypeSystemContext context = TypeSystemContextFactory.Create();
                ByRefType byRefType = context.GetByRefType(context.ResolveRuntimeTypeHandle(pointeeTypeHandle));
                byRefTypeHandle = EETypeCreator.CreateByRefEEType((uint)byRefType.GetHashCode(), pointeeTypeHandle, byRefType);
                unsafe
                {
                    Debug.Assert(byRefTypeHandle.ToEETypePtr()->IsByRef);
                }
                TypeSystemContext.ByRefTypesCache.AddOrGetExisting(byRefTypeHandle);

                // Recycle the context only if we successfully built the type. The state may be partially initialized otherwise.
                TypeSystemContextFactory.Recycle(context);
            }

            return true;
        }

        public static bool TryBuildFunctionPointerType(RuntimeTypeHandle returnTypeHandle, RuntimeTypeHandle[] parameterHandles, bool isUnmanaged, out RuntimeTypeHandle runtimeTypeHandle)
        {
            var key = new TypeSystemContext.FunctionPointerTypeKey(returnTypeHandle, parameterHandles, isUnmanaged);
            if (!TypeSystemContext.FunctionPointerTypesCache.TryGetValue(key, out runtimeTypeHandle))
            {
                TypeSystemContext context = TypeSystemContextFactory.Create();
                FunctionPointerType functionPointerType = context.GetFunctionPointerType(new MethodSignature(
                    isUnmanaged ? MethodSignatureFlags.UnmanagedCallingConvention : 0,
                    genericParameterCount: 0,
                    context.ResolveRuntimeTypeHandle(returnTypeHandle),
                    context.ResolveRuntimeTypeHandlesInternal(parameterHandles)));
                runtimeTypeHandle = EETypeCreator.CreateFunctionPointerEEType((uint)functionPointerType.GetHashCode(), returnTypeHandle, parameterHandles, functionPointerType);
                unsafe
                {
                    Debug.Assert(runtimeTypeHandle.ToEETypePtr()->IsFunctionPointer);
                }
                TypeSystemContext.FunctionPointerTypesCache.AddOrGetExisting(runtimeTypeHandle);

                // Recycle the context only if we successfully built the type. The state may be partially initialized otherwise.
                TypeSystemContextFactory.Recycle(context);
            }
            return true;
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
    }
}
