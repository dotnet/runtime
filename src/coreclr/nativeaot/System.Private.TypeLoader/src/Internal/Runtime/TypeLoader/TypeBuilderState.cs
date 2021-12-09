// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.TypeSystem;
using Internal.TypeSystem.NativeFormat;
using Internal.TypeSystem.NoMetadata;

namespace Internal.Runtime.TypeLoader
{
    internal struct NativeLayoutInfo
    {
        public uint Offset;
        public NativeFormatModuleInfo Module;
        public NativeReader Reader;
        public NativeLayoutInfoLoadContext LoadContext;
    }

    //
    // TypeBuilder per-Type state. It is attached to each TypeDesc that gets involved in type building.
    //
    internal class TypeBuilderState
    {
        internal class VTableLayoutInfo
        {
            public uint VTableSlot;
            public RuntimeSignature MethodSignature;
            public bool IsSealedVTableSlot;
        }

        internal class VTableSlotMapper
        {
            private int[] _slotMap;
            private IntPtr[] _dictionarySlots;
            private int _numMappingsAssigned;

            public VTableSlotMapper(int numVtableSlotsInTemplateType)
            {
                _slotMap = new int[numVtableSlotsInTemplateType];
                _dictionarySlots = new IntPtr[numVtableSlotsInTemplateType];
                _numMappingsAssigned = 0;
                for (int i = 0; i < numVtableSlotsInTemplateType; i++)
                {
                    _slotMap[i] = -1;
                    _dictionarySlots[i] = IntPtr.Zero;
                }
            }
            public void AddMapping(int vtableSlotInTemplateType, int vtableSlotInTargetType, IntPtr dictionaryValueInSlot)
            {
                Debug.Assert(_numMappingsAssigned < _slotMap.Length);
                _slotMap[vtableSlotInTemplateType] = vtableSlotInTargetType;
                _dictionarySlots[vtableSlotInTemplateType] = dictionaryValueInSlot;
                _numMappingsAssigned++;
            }
            public int GetVTableSlotInTargetType(int vtableSlotInTemplateType)
            {
                Debug.Assert((uint)vtableSlotInTemplateType < (uint)_slotMap.Length);
                return _slotMap[vtableSlotInTemplateType];
            }
            public bool IsDictionarySlot(int vtableSlotInTemplateType, out IntPtr dictionaryPtrValue)
            {
                Debug.Assert((uint)vtableSlotInTemplateType < (uint)_dictionarySlots.Length);
                dictionaryPtrValue = _dictionarySlots[vtableSlotInTemplateType];
                return _dictionarySlots[vtableSlotInTemplateType] != IntPtr.Zero;
            }
            public int NumSlotMappings
            {
                get { return _numMappingsAssigned; }
            }
        }

        public TypeBuilderState(TypeDesc typeBeingBuilt)
        {
            TypeBeingBuilt = typeBeingBuilt;
        }

        public readonly TypeDesc TypeBeingBuilt;

        //
        // We cache and try to reuse the most recently used TypeSystemContext. The TypeSystemContext is used by not just type builder itself,
        // but also in several random other places in reflection. There can be multiple ResolutionContexts in flight at any given moment.
        // This check ensures that the RuntimeTypeHandle cache in the current resolution context is refreshed in case there were new
        // types built using a different TypeSystemContext in the meantime.
        // NOTE: For correctness, this value must be recomputed every time the context is recycled. This requires flushing the TypeBuilderState
        // from each type that has one if context is recycled.
        //
        public bool AttemptedAndFailedToRetrieveTypeHandle;

        public bool NeedsTypeHandle;
        public bool HasBeenPrepared;

        public RuntimeTypeHandle HalfBakedRuntimeTypeHandle;
        public IntPtr HalfBakedDictionary;
        public IntPtr HalfBakedSealedVTable;

        private bool _templateComputed;
        private bool _nativeLayoutTokenComputed;
        private TypeDesc _templateType;

        public TypeDesc TemplateType
        {
            get
            {
                if (!_templateComputed)
                {
                    // Multidimensional arrays and szarrays of pointers don't implement generic interfaces and are special cases. They use
                    // typeof(object[,]) as their template.
                    if (TypeBeingBuilt.IsMdArray || (TypeBeingBuilt.IsSzArray && ((ArrayType)TypeBeingBuilt).ElementType.IsPointer))
                    {
                        _templateType = TypeBeingBuilt.Context.ResolveRuntimeTypeHandle(typeof(object[,]).TypeHandle);
                        _templateTypeLoaderNativeLayout = false;
                        _nativeLayoutComputed = _nativeLayoutTokenComputed = _templateComputed = true;

                        return _templateType;
                    }

                    // Locate the template type and native layout info
                    _templateType = TypeBeingBuilt.Context.TemplateLookup.TryGetTypeTemplate(TypeBeingBuilt, ref _nativeLayoutInfo);
                    Debug.Assert(_templateType == null || !_templateType.RuntimeTypeHandle.IsNull());

                    _templateTypeLoaderNativeLayout = true;
                    _templateComputed = true;
                    if ((_templateType != null) && !_templateType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                        _nativeLayoutTokenComputed = true;
                }

                return _templateType;
            }
        }

        private bool _nativeLayoutComputed;
        private bool _templateTypeLoaderNativeLayout;
        private bool _readyToRunNativeLayout;

        private NativeLayoutInfo _nativeLayoutInfo;
        private NativeLayoutInfo _r2rnativeLayoutInfo;

        private void EnsureNativeLayoutInfoComputed()
        {
            if (!_nativeLayoutComputed)
            {
                if (!_nativeLayoutTokenComputed)
                {
                    if (!_templateComputed)
                    {
                        // Attempt to compute native layout through as a non-ReadyToRun template
                        object temp = this.TemplateType;
                    }
                    if (!_nativeLayoutTokenComputed)
                    {
                        TypeBeingBuilt.Context.TemplateLookup.TryGetMetadataNativeLayout(TypeBeingBuilt, out _r2rnativeLayoutInfo.Module, out _r2rnativeLayoutInfo.Offset);

                        if (_r2rnativeLayoutInfo.Module != null)
                            _readyToRunNativeLayout = true;
                    }
                    _nativeLayoutTokenComputed = true;
                }

                if (_nativeLayoutInfo.Module != null)
                {
                    FinishInitNativeLayoutInfo(TypeBeingBuilt, ref _nativeLayoutInfo);
                }

                if (_r2rnativeLayoutInfo.Module != null)
                {
                    FinishInitNativeLayoutInfo(TypeBeingBuilt, ref _r2rnativeLayoutInfo);
                }

                _nativeLayoutComputed = true;
            }
        }

        /// <summary>
        /// Initialize the Reader and LoadContext fields of the native layout info
        /// </summary>
        /// <param name="type"></param>
        /// <param name="nativeLayoutInfo"></param>
        private static void FinishInitNativeLayoutInfo(TypeDesc type, ref NativeLayoutInfo nativeLayoutInfo)
        {
            var nativeLayoutInfoLoadContext = new NativeLayoutInfoLoadContext();

            nativeLayoutInfoLoadContext._typeSystemContext = type.Context;
            nativeLayoutInfoLoadContext._module = nativeLayoutInfo.Module;

            if (type is DefType)
            {
                nativeLayoutInfoLoadContext._typeArgumentHandles = ((DefType)type).Instantiation;
            }
            else if (type is ArrayType)
            {
                nativeLayoutInfoLoadContext._typeArgumentHandles = new Instantiation(new TypeDesc[] { ((ArrayType)type).ElementType });
            }
            else
            {
                Debug.Assert(false);
            }

            nativeLayoutInfoLoadContext._methodArgumentHandles = new Instantiation(null);

            nativeLayoutInfo.Reader = TypeLoaderEnvironment.Instance.GetNativeLayoutInfoReader(nativeLayoutInfo.Module.Handle);
            nativeLayoutInfo.LoadContext = nativeLayoutInfoLoadContext;
        }

        public NativeLayoutInfo NativeLayoutInfo
        {
            get
            {
                EnsureNativeLayoutInfoComputed();
                return _nativeLayoutInfo;
            }
        }

        public NativeLayoutInfo R2RNativeLayoutInfo
        {
            get
            {
                EnsureNativeLayoutInfoComputed();
                return _r2rnativeLayoutInfo;
            }
        }

        public NativeParser GetParserForNativeLayoutInfo()
        {
            EnsureNativeLayoutInfoComputed();
            if (_templateTypeLoaderNativeLayout)
                return new NativeParser(_nativeLayoutInfo.Reader, _nativeLayoutInfo.Offset);
            else
                return default(NativeParser);
        }

        public NativeParser GetParserForReadyToRunNativeLayoutInfo()
        {
            EnsureNativeLayoutInfoComputed();
            if (_readyToRunNativeLayout)
                return new NativeParser(_r2rnativeLayoutInfo.Reader, _r2rnativeLayoutInfo.Offset);
            else
                return default(NativeParser);
        }

        public NativeParser GetParserForUniversalNativeLayoutInfo(out NativeLayoutInfoLoadContext universalLayoutLoadContext, out NativeLayoutInfo universalLayoutInfo)
        {
            universalLayoutInfo = new NativeLayoutInfo();
            universalLayoutLoadContext = null;
            TypeDesc universalTemplate = TypeBeingBuilt.Context.TemplateLookup.TryGetUniversalTypeTemplate(TypeBeingBuilt, ref universalLayoutInfo);
            if (universalTemplate == null)
                return new NativeParser();

            FinishInitNativeLayoutInfo(TypeBeingBuilt, ref universalLayoutInfo);
            universalLayoutLoadContext = universalLayoutInfo.LoadContext;
            return new NativeParser(universalLayoutInfo.Reader, universalLayoutInfo.Offset);
        }

        // RuntimeInterfaces is the full list of interfaces that the type implements. It can include private internal implementation
        // detail interfaces that nothing is known about.
        public DefType[] RuntimeInterfaces
        {
            get
            {
                // Generic Type Definitions have no runtime interfaces
                if (TypeBeingBuilt.IsGenericDefinition)
                    return null;

                return TypeBeingBuilt.RuntimeInterfaces;
            }
        }

        private bool? _hasDictionarySlotInVTable;
        private bool ComputeHasDictionarySlotInVTable()
        {
            if (!TypeBeingBuilt.IsGeneric() && !(TypeBeingBuilt is ArrayType))
                return false;

            // Generic interfaces always have a dictionary slot
            if (TypeBeingBuilt.IsInterface)
                return true;

            return TypeBeingBuilt.CanShareNormalGenericCode();
        }

        public bool HasDictionarySlotInVTable
        {
            get
            {
                if (_hasDictionarySlotInVTable == null)
                {
                    _hasDictionarySlotInVTable = ComputeHasDictionarySlotInVTable();
                }
                return _hasDictionarySlotInVTable.Value;
            }
        }

        private bool? _hasDictionaryInVTable;
        private bool ComputeHasDictionaryInVTable()
        {
            if (!HasDictionarySlotInVTable)
                return false;

            if (TypeBeingBuilt.RetrieveRuntimeTypeHandleIfPossible())
            {
                // Type was already constructed
                return TypeBeingBuilt.RuntimeTypeHandle.GetDictionary() != IntPtr.Zero;
            }
            else
            {
                // Type is being newly constructed
                if (TemplateType != null)
                {
                    NativeParser parser = GetParserForNativeLayoutInfo();
                    // Template type loader case
#if GENERICS_FORCE_USG
                    bool isTemplateUniversalCanon = state.TemplateType.IsCanonicalSubtype(CanonicalFormKind.UniversalCanonLookup);
                    if (isTemplateUniversalCanon && type.CanShareNormalGenericCode())
                    {
                        TypeBuilderState tempState = new TypeBuilderState();
                        tempState.NativeLayoutInfo = new NativeLayoutInfo();
                        tempState.TemplateType = type.Context.TemplateLookup.TryGetNonUniversalTypeTemplate(type, ref tempState.NativeLayoutInfo);
                        if (tempState.TemplateType != null)
                        {
                            Debug.Assert(!tempState.TemplateType.IsCanonicalSubtype(CanonicalFormKind.UniversalCanonLookup));
                            parser = GetNativeLayoutInfoParser(type, ref tempState.NativeLayoutInfo);
                        }
                    }
#endif
                    var dictionaryLayoutParser = parser.GetParserForBagElementKind(BagElementKind.DictionaryLayout);

                    return !dictionaryLayoutParser.IsNull;
                }
                else
                {
                    NativeParser parser = GetParserForReadyToRunNativeLayoutInfo();
                    // ReadyToRun case
                    // Dictionary is directly encoded instead of the NativeLayout being a collection of bags
                    if (parser.IsNull)
                        return false;

                    // First unsigned value in the native layout is the number of dictionary entries
                    return parser.GetUnsigned() != 0;
                }
            }
        }

        public bool HasDictionaryInVTable
        {
            get
            {
                if (_hasDictionaryInVTable == null)
                    _hasDictionaryInVTable = ComputeHasDictionaryInVTable();
                return _hasDictionaryInVTable.Value;
            }
        }

        private ushort? _numVTableSlots;
        private ushort ComputeNumVTableSlots()
        {
            if (TypeBeingBuilt.RetrieveRuntimeTypeHandleIfPossible())
            {
                unsafe
                {
                    return TypeBeingBuilt.RuntimeTypeHandle.ToEETypePtr()->NumVtableSlots;
                }
            }
            else
            {
                TypeDesc templateType = TypeBeingBuilt.ComputeTemplate(false);
                if (templateType != null)
                {
                    // Template type loader case
                    if (VTableSlotsMapping != null)
                    {
                        return checked((ushort)VTableSlotsMapping.NumSlotMappings);
                    }
                    else
                    {
                        unsafe
                        {
                            if (TypeBeingBuilt.IsMdArray || (TypeBeingBuilt.IsSzArray && ((ArrayType)TypeBeingBuilt).ElementType.IsPointer))
                            {
                                // MDArray types and pointer arrays have the same vtable as the System.Array type they "derive" from.
                                // They do not implement the generic interfaces that make this interesting for normal arrays.
                                return TypeBeingBuilt.BaseType.GetRuntimeTypeHandle().ToEETypePtr()->NumVtableSlots;
                            }
                            else
                            {
                                // This should only happen for non-universal templates
                                Debug.Assert(TypeBeingBuilt.IsTemplateCanonical());

                                // Canonical template type loader case
                                return templateType.GetRuntimeTypeHandle().ToEETypePtr()->NumVtableSlots;
                            }
                        }
                    }
                }
                else
                {
                    // Metadata based type loading.

                    // Generic Type Definitions have no actual vtable entries
                    if (TypeBeingBuilt.IsGenericDefinition)
                        return 0;

                    // We have at least as many slots as exist on the base type.

                    ushort numVTableSlots = 0;
                    checked
                    {
                        if (TypeBeingBuilt.BaseType != null)
                        {
                            numVTableSlots = TypeBeingBuilt.BaseType.GetOrCreateTypeBuilderState().NumVTableSlots;
                        }
                        else
                        {
                            // Generic interfaces have a dictionary slot
                            if (TypeBeingBuilt.IsInterface && TypeBeingBuilt.HasInstantiation)
                                numVTableSlots = 1;
                        }

                        // Interfaces have actual vtable slots
                        if (TypeBeingBuilt.IsInterface)
                            return numVTableSlots;

                        foreach (MethodDesc method in TypeBeingBuilt.GetMethods())
                        {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                            if (LazyVTableResolver.MethodDefinesVTableSlot(method))
                                numVTableSlots++;
#else
                            Environment.FailFast("metadata type loader required");
#endif
                        }

                        if (HasDictionarySlotInVTable)
                            numVTableSlots++;
                    }

                    return numVTableSlots;
                }
            }
        }

        public ushort NumVTableSlots
        {
            get
            {
                if (_numVTableSlots == null)
                    _numVTableSlots = ComputeNumVTableSlots();

                return _numVTableSlots.Value;
            }
        }


        public GenericTypeDictionary Dictionary;

        public int NonGcDataSize
        {
            get
            {
                DefType defType = TypeBeingBuilt as DefType;

                // The NonGCStatic fields hold the class constructor data if it exists in the negative space
                // of the memory region. The ClassConstructorOffset is negative, so it must be negated to
                // determine the extra space that is used.

                if (defType != null)
                {
                    return defType.NonGCStaticFieldSize.AsInt - (HasStaticConstructor ? TypeBuilder.ClassConstructorOffset : 0);
                }
                else
                {
                    return -(HasStaticConstructor ? TypeBuilder.ClassConstructorOffset : 0);
                }
            }
        }

        public int GcDataSize
        {
            get
            {
                DefType defType = TypeBeingBuilt as DefType;
                if (defType != null)
                {
                    return defType.GCStaticFieldSize.AsInt;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int ThreadDataSize
        {
            get
            {
                DefType defType = TypeBeingBuilt as DefType;
                if (defType != null && !defType.IsGenericDefinition)
                {
                    return defType.ThreadGcStaticFieldSize.AsInt;
                }
                else
                {
                    // Non-DefType's and GenericEETypeDefinitions do not have static fields of any form
                    return 0;
                }
            }
        }

        public bool HasStaticConstructor
        {
            get { return TypeBeingBuilt.HasStaticConstructor; }
        }

        public IntPtr? ClassConstructorPointer;
        public IntPtr GcStaticDesc;
        public IntPtr GcStaticEEType;
        public IntPtr ThreadStaticDesc;
        public bool AllocatedStaticGCDesc;
        public bool AllocatedThreadStaticGCDesc;
        public uint ThreadStaticOffset;
        public uint NumSealedVTableEntries;
        public GenericVariance[] GenericVarianceFlags;

        // Sentinel static to allow us to initialize _instanceLayout to something
        // and then detect that InstanceGCLayout should return null
        private static LowLevelList<bool> s_emptyLayout = new LowLevelList<bool>();

        private LowLevelList<bool> _instanceGCLayout;

        /// <summary>
        /// The instance gc layout of a dynamically laid out type.
        /// null if one of the following is true
        ///     1) For an array type:
        ///         - the type is a reference array
        ///     2) For a generic type:
        ///         - the type has no GC instance fields
        ///         - the type already has a type handle
        ///         - the type has a non-universal canonical template
        ///         - the type has already been constructed
        ///
        /// If the type is a valuetype array, this is the layout of the valuetype held in the array if the type has GC reference fields
        /// Otherwise, it is the layout of the fields in the type.
        /// </summary>
        public LowLevelList<bool> InstanceGCLayout
        {
            get
            {
                if (_instanceGCLayout == null)
                {
                    LowLevelList<bool> instanceGCLayout = null;

                    if (TypeBeingBuilt is ArrayType)
                    {
                        if (!IsArrayOfReferenceTypes)
                        {
                            ArrayType arrayType = (ArrayType)TypeBeingBuilt;
                            TypeBuilder.GCLayout elementGcLayout = GetFieldGCLayout(arrayType.ElementType);
                            if (!elementGcLayout.IsNone)
                            {
                                instanceGCLayout = new LowLevelList<bool>();
                                elementGcLayout.WriteToBitfield(instanceGCLayout, 0);
                                _instanceGCLayout = instanceGCLayout;
                            }
                        }
                        else
                        {
                            // Array of reference type returns null
                            _instanceGCLayout = s_emptyLayout;
                        }
                    }
                    else if (TypeBeingBuilt.RetrieveRuntimeTypeHandleIfPossible() ||
                             TypeBeingBuilt.IsTemplateCanonical() ||
                             (TypeBeingBuilt is PointerType) ||
                             (TypeBeingBuilt is ByRefType))
                    {
                        _instanceGCLayout = s_emptyLayout;
                    }
                    else
                    {
                        // Generic Type Definitions have no gc layout
                        if (!(TypeBeingBuilt.IsGenericDefinition))
                        {
                            // Copy in from base type
                            if (!TypeBeingBuilt.IsValueType && (TypeBeingBuilt.BaseType != null))
                            {
                                DefType baseType = TypeBeingBuilt.BaseType;

                                // Capture the gc layout from the base type
                                TypeBuilder.GCLayout baseTypeLayout = GetInstanceGCLayout(baseType);
                                if (!baseTypeLayout.IsNone)
                                {
                                    instanceGCLayout = new LowLevelList<bool>();
                                    baseTypeLayout.WriteToBitfield(instanceGCLayout, IntPtr.Size /* account for the MethodTable pointer */);
                                }
                            }

                            foreach (FieldDesc field in GetFieldsForGCLayout())
                            {
                                if (field.IsStatic)
                                    continue;

                                if (field.IsLiteral)
                                    continue;

                                TypeBuilder.GCLayout fieldGcLayout = GetFieldGCLayout(field.FieldType);
                                if (!fieldGcLayout.IsNone)
                                {
                                    if (instanceGCLayout == null)
                                        instanceGCLayout = new LowLevelList<bool>();

                                    fieldGcLayout.WriteToBitfield(instanceGCLayout, field.Offset.AsInt);
                                }
                            }

                            if ((instanceGCLayout != null) && instanceGCLayout.HasSetBits())
                            {
                                // When bits are set in the instance GC layout, it implies that the type contains GC refs,
                                // which implies that the type size is pointer-aligned.  In this case consumers assume that
                                // the type size can be computed by multiplying the bitfield size by the pointer size.  If
                                // necessary, expand the bitfield to ensure that this invariant holds.

                                // Valuetypes with gc fields must be aligned on at least pointer boundaries
                                Debug.Assert(!TypeBeingBuilt.IsValueType || (FieldAlignment.Value >= TypeBeingBuilt.Context.Target.PointerSize));
                                // Valuetypes with gc fields must have a type size which is aligned on an IntPtr boundary.
                                Debug.Assert(!TypeBeingBuilt.IsValueType || ((TypeSize.Value & (IntPtr.Size - 1)) == 0));

                                int impliedBitCount = (TypeSize.Value + IntPtr.Size - 1) / IntPtr.Size;
                                Debug.Assert(instanceGCLayout.Count <= impliedBitCount);
                                instanceGCLayout.Expand(impliedBitCount);
                                Debug.Assert(instanceGCLayout.Count == impliedBitCount);
                            }
                        }

                        if (instanceGCLayout == null)
                            _instanceGCLayout = s_emptyLayout;
                        else
                            _instanceGCLayout = instanceGCLayout;
                    }
                }

                if (_instanceGCLayout == s_emptyLayout)
                    return null;
                else
                    return _instanceGCLayout;
            }
        }


        public LowLevelList<bool> StaticGCLayout;
        public LowLevelList<bool> ThreadStaticGCLayout;

        private bool _staticGCLayoutPrepared;

        /// <summary>
        /// Prepare the StaticGCLayout/ThreadStaticGCLayout/GcStaticDesc/ThreadStaticDesc fields by
        /// reading native layout or metadata as appropriate. This method should only be called for types which
        /// are actually to be created.
        /// </summary>
        public void PrepareStaticGCLayout()
        {
            if (!_staticGCLayoutPrepared)
            {
                _staticGCLayoutPrepared = true;
                DefType defType = TypeBeingBuilt as DefType;

                if (defType == null)
                {
                    // Array/pointer types do not have static fields
                }
                else if (defType.IsTemplateCanonical())
                {
                    // Canonical templates get their layout directly from the NativeLayoutInfo.
                    // Parse it and pull that info out here.

                    NativeParser typeInfoParser = GetParserForNativeLayoutInfo();

                    BagElementKind kind;
                    while ((kind = typeInfoParser.GetBagElementKind()) != BagElementKind.End)
                    {
                        switch (kind)
                        {
                            case BagElementKind.GcStaticDesc:
                                GcStaticDesc = NativeLayoutInfo.LoadContext.GetGCStaticInfo(typeInfoParser.GetUnsigned());
                                break;

                            case BagElementKind.ThreadStaticDesc:
                                ThreadStaticDesc = NativeLayoutInfo.LoadContext.GetGCStaticInfo(typeInfoParser.GetUnsigned());
                                break;

                            case BagElementKind.GcStaticEEType:
                                GcStaticEEType = NativeLayoutInfo.LoadContext.GetGCStaticInfo(typeInfoParser.GetUnsigned());
                                break;

                            default:
                                typeInfoParser.SkipInteger();
                                break;
                        }
                    }
                }
                else
                {
                    // Compute GC layout boolean array from field information.
                    IEnumerable<FieldDesc> fields = GetFieldsForGCLayout();
                    LowLevelList<bool> threadStaticLayout = null;
                    LowLevelList<bool> gcStaticLayout = null;

                    foreach (FieldDesc field in fields)
                    {
                        if (!field.IsStatic)
                            continue;

                        if (field.IsLiteral)
                            continue;

                        LowLevelList<bool> gcLayoutInfo = null;
                        if (field.IsThreadStatic)
                        {
                            if (threadStaticLayout == null)
                                threadStaticLayout = new LowLevelList<bool>();
                            gcLayoutInfo = threadStaticLayout;
                        }
                        else if (field.HasGCStaticBase)
                        {
                            if (gcStaticLayout == null)
                                gcStaticLayout = new LowLevelList<bool>();
                            gcLayoutInfo = gcStaticLayout;
                        }
                        else
                        {
                            // Non-GC static  no need to record information
                            continue;
                        }

                        TypeBuilder.GCLayout fieldGcLayout = GetFieldGCLayout(field.FieldType);
                        fieldGcLayout.WriteToBitfield(gcLayoutInfo, field.Offset.AsInt);
                    }

                    if (gcStaticLayout != null && gcStaticLayout.Count > 0)
                        StaticGCLayout = gcStaticLayout;

                    if (threadStaticLayout != null && threadStaticLayout.Count > 0)
                        ThreadStaticGCLayout = threadStaticLayout;
                }
            }
        }

        /// <summary>
        /// Get an enumerable list of the fields used for dynamic gc layout calculation.
        /// </summary>
        private IEnumerable<FieldDesc> GetFieldsForGCLayout()
        {
            DefType defType = (DefType)TypeBeingBuilt;

            IEnumerable<FieldDesc> fields;

            if (defType.ComputeTemplate(false) != null)
            {
                // we have native layout and a template. Use the NativeLayoutFields as that is the only complete
                // description of the fields available. (There may be metadata fields, but those aren't guaranteed
                // to be a complete set of fields due to reflection reduction.
                NativeLayoutFieldAlgorithm.EnsureFieldLayoutLoadedForGenericType(defType);
                fields = defType.NativeLayoutFields;
            }
            else
            {
                // The metadata case. We're loading the type from regular metadata, so use the regular metadata fields
                fields = defType.GetFields();
            }

            return fields;
        }

        // Get the GC layout of a type. Handles pre-created, universal template, and non-universal template cases
        // Only to be used for getting the instance layout of non-valuetypes.
        /// <summary>
        /// Get the GC layout of a type. Handles pre-created, universal template, and non-universal template cases
        /// Only to be used for getting the instance layout of non-valuetypes that are used as base types
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private unsafe TypeBuilder.GCLayout GetInstanceGCLayout(TypeDesc type)
        {
            Debug.Assert(!type.IsCanonicalSubtype(CanonicalFormKind.Any));
            Debug.Assert(!type.IsValueType);

            if (type.RetrieveRuntimeTypeHandleIfPossible())
            {
                return new TypeBuilder.GCLayout(type.RuntimeTypeHandle);
            }

            if (type.IsTemplateCanonical())
            {
                var templateType = type.ComputeTemplate();
                bool success = templateType.RetrieveRuntimeTypeHandleIfPossible();
                Debug.Assert(success && !templateType.RuntimeTypeHandle.IsNull());

                return new TypeBuilder.GCLayout(templateType.RuntimeTypeHandle);
            }
            else
            {
                TypeBuilderState state = type.GetOrCreateTypeBuilderState();
                if (state.InstanceGCLayout == null)
                    return TypeBuilder.GCLayout.None;
                else
                    return new TypeBuilder.GCLayout(state.InstanceGCLayout, true);
            }
        }

        /// <summary>
        /// Get the GC layout of a type when used as a field.
        /// NOTE: if the fieldtype is a reference type, this function will return GCLayout.None
        ///       Consumers of the api must handle that special case.
        /// </summary>
        private unsafe TypeBuilder.GCLayout GetFieldGCLayout(TypeDesc fieldType)
        {
            if (!fieldType.IsValueType)
            {
                if (fieldType.IsPointer)
                    return TypeBuilder.GCLayout.None;
                else
                    return TypeBuilder.GCLayout.SingleReference;
            }

            // Is this a type that already exists? If so, get its gclayout from the MethodTable directly
            if (fieldType.RetrieveRuntimeTypeHandleIfPossible())
            {
                return new TypeBuilder.GCLayout(fieldType.RuntimeTypeHandle);
            }

            // The type of the field must be a valuetype that is dynamically being constructed

            if (fieldType.IsTemplateCanonical())
            {
                // Pull the GC Desc from the canonical instantiation
                TypeDesc templateType = fieldType.ComputeTemplate();
                bool success = templateType.RetrieveRuntimeTypeHandleIfPossible();
                Debug.Assert(success);
                return new TypeBuilder.GCLayout(templateType.RuntimeTypeHandle);
            }
            else
            {
                // Use the type builder state's computed InstanceGCLayout
                var instanceGCLayout = fieldType.GetOrCreateTypeBuilderState().InstanceGCLayout;
                if (instanceGCLayout == null)
                    return TypeBuilder.GCLayout.None;

                return new TypeBuilder.GCLayout(instanceGCLayout, false /* Always represents a valuetype as the reference type case
                                                                           is handled above with the GCLayout.SingleReference return */);
            }
        }


        public bool IsArrayOfReferenceTypes
        {
            get
            {
                ArrayType typeAsArrayType = TypeBeingBuilt as ArrayType;
                if (typeAsArrayType != null)
                    return !typeAsArrayType.ParameterType.IsValueType && !typeAsArrayType.ParameterType.IsPointer;
                else
                    return false;
            }
        }

        // Rank for arrays, -1 is used for an SzArray, and a positive number for a multidimensional array.
        public int? ArrayRank
        {
            get
            {
                if (!TypeBeingBuilt.IsArray)
                    return null;
                else if (TypeBeingBuilt.IsSzArray)
                    return -1;
                else
                {
                    Debug.Assert(TypeBeingBuilt.IsMdArray);
                    return ((ArrayType)TypeBeingBuilt).Rank;
                }
            }
        }

        public int? BaseTypeSize
        {
            get
            {
                if (TypeBeingBuilt.BaseType == null)
                {
                    return null;
                }
                else
                {
                    return TypeBeingBuilt.BaseType.InstanceByteCountUnaligned.AsInt;
                }
            }
        }

        public int? TypeSize
        {
            get
            {
                DefType defType = TypeBeingBuilt as DefType;
                if (defType != null)
                {
                    // Generic Type Definition EETypes do not have size
                    if (defType.IsGenericDefinition)
                        return null;

                    if (defType.IsValueType)
                    {
                        return defType.InstanceFieldSize.AsInt;
                    }
                    else
                    {
                        if (defType.IsInterface)
                            return IntPtr.Size;

                        return defType.InstanceByteCountUnaligned.AsInt;
                    }
                }
                else if (TypeBeingBuilt is ArrayType)
                {
                    int basicArraySize = 2 * IntPtr.Size; // EETypePtr + Length
                    if (TypeBeingBuilt.IsMdArray)
                    {
                        // MD Arrays are arranged like normal arrays, but they also have 2 int's per rank for the individual dimension loBounds and range.
                        basicArraySize += ((ArrayType)TypeBeingBuilt).Rank * sizeof(int) * 2;
                    }
                    return basicArraySize;
                }
                else
                {
                    return null;
                }
            }
        }

        public int? UnalignedTypeSize
        {
            get
            {
                DefType defType = TypeBeingBuilt as DefType;
                if (defType != null)
                {
                    return defType.InstanceByteCountUnaligned.AsInt;
                }
                else if (TypeBeingBuilt is ArrayType)
                {
                    // Arrays use the same algorithm for TypeSize as for UnalignedTypeSize
                    return TypeSize;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int? FieldAlignment
        {
            get
            {
                if (TypeBeingBuilt is DefType)
                {
                    return checked((ushort)((DefType)TypeBeingBuilt).InstanceFieldAlignment.AsInt);
                }
                else if (TypeBeingBuilt is ArrayType)
                {
                    ArrayType arrayType = (ArrayType)TypeBeingBuilt;

                    if (arrayType.ElementType is DefType)
                    {
                        return checked((ushort)((DefType)arrayType.ElementType).InstanceFieldAlignment.AsInt);
                    }
                    else
                    {
                        return (ushort)arrayType.Context.Target.PointerSize;
                    }
                }
                else if (TypeBeingBuilt is PointerType || TypeBeingBuilt is ByRefType)
                {
                    return (ushort)TypeBeingBuilt.Context.Target.PointerSize;
                }
                else
                {
                    return null;
                }
            }
        }

        public ushort? ComponentSize
        {
            get
            {
                ArrayType arrayType = TypeBeingBuilt as ArrayType;
                if (arrayType != null)
                {
                    if (arrayType.ElementType is DefType)
                    {
                        uint size = (uint)((DefType)arrayType.ElementType).InstanceFieldSize.AsInt;

                        if (size > ArrayTypesConstants.MaxSizeForValueClassInArray && arrayType.ElementType.IsValueType)
                            ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadValueClassTooLarge, arrayType.ElementType);

                        return checked((ushort)size);
                    }
                    else
                    {
                        return (ushort)arrayType.Context.Target.PointerSize;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public uint NullableValueOffset
        {
            get
            {
                if (!TypeBeingBuilt.IsNullable)
                    return 0;

                if (TypeBeingBuilt.IsTemplateCanonical())
                {
                    // Pull the GC Desc from the canonical instantiation
                    TypeDesc templateType = TypeBeingBuilt.ComputeTemplate();
                    bool success = templateType.RetrieveRuntimeTypeHandleIfPossible();
                    Debug.Assert(success);
                    unsafe
                    {
                        return templateType.RuntimeTypeHandle.ToEETypePtr()->NullableValueOffset;
                    }
                }
                else
                {
                    int fieldCount = 0;
                    uint nullableValueOffset = 0;

                    foreach (FieldDesc f in GetFieldsForGCLayout())
                    {
                        if (fieldCount == 1)
                        {
                            nullableValueOffset = checked((uint)f.Offset.AsInt);
                        }
                        fieldCount++;
                    }

                    // Nullable<T> only has two fields. HasValue and Value
                    Debug.Assert(fieldCount == 2);
                    return nullableValueOffset;
                }
            }
        }

        public bool IsHFA
        {
            get
            {
#if TARGET_ARM
                if (TypeBeingBuilt.IsValueType && TypeBeingBuilt is DefType)
                {
                    return ((DefType)TypeBeingBuilt).IsHomogeneousAggregate;
                }
                else
                {
                    return false;
                }
#else
                // On Non-ARM platforms, HFA'ness is not encoded in the MethodTable as it doesn't effect ABI
                return false;
#endif
            }
        }

        public VTableLayoutInfo[] VTableMethodSignatures;
        public int NumSealedVTableMethodSignatures;

        public VTableSlotMapper VTableSlotsMapping;

#if GENERICS_FORCE_USG
        public TypeDesc NonUniversalTemplateType;
        public int NonUniversalInstanceGCDescSize;
        public IntPtr NonUniversalInstanceGCDesc;
        public IntPtr NonUniversalStaticGCDesc;
        public IntPtr NonUniversalThreadStaticGCDesc;
#endif
    }
}
