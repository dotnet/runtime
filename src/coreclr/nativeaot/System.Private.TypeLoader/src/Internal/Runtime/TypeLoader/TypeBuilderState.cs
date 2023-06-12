// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.NativeFormat;
using Internal.TypeSystem;

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

        private bool _templateComputed;
        private bool _nativeLayoutTokenComputed;
        private TypeDesc _templateType;

        public TypeDesc TemplateType
        {
            get
            {
                if (!_templateComputed)
                {
                    // Multidimensional arrays don't implement generic interfaces and are special cases. They use
                    // typeof(object[,]) as their template.
                    if (TypeBeingBuilt.IsMdArray)
                    {
                        _templateType = TypeBeingBuilt.Context.ResolveRuntimeTypeHandle(typeof(object[,]).TypeHandle);
                        _templateTypeLoaderNativeLayout = false;
                        _nativeLayoutComputed = _nativeLayoutTokenComputed = _templateComputed = true;

                        return _templateType;
                    }

                    // Arrays of pointers don't implement generic interfaces and are special cases. They use
                    // typeof(char*[]) as their template.
                    if (TypeBeingBuilt.IsSzArray && ((ArrayType)TypeBeingBuilt).ElementType is TypeDesc elementType &&
                        (elementType.IsPointer || elementType.IsFunctionPointer))
                    {
                        _templateType = TypeBeingBuilt.Context.ResolveRuntimeTypeHandle(typeof(char*[]).TypeHandle);
                        _templateTypeLoaderNativeLayout = false;
                        _nativeLayoutComputed = _nativeLayoutTokenComputed = _templateComputed = true;

                        return _templateType;
                    }

                    // Locate the template type and native layout info
                    _templateType = TemplateLocator.TryGetTypeTemplate(TypeBeingBuilt, ref _nativeLayoutInfo);
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

        private NativeLayoutInfo _nativeLayoutInfo;

        private void EnsureNativeLayoutInfoComputed()
        {
            if (!_nativeLayoutComputed)
            {
                if (!_nativeLayoutTokenComputed)
                {
                    if (!_templateComputed)
                    {
                        // Attempt to compute native layout through as a non-ReadyToRun template
                        object _ = this.TemplateType;
                    }
                    _nativeLayoutTokenComputed = true;
                }

                if (_nativeLayoutInfo.Module != null)
                {
                    FinishInitNativeLayoutInfo(TypeBeingBuilt, ref _nativeLayoutInfo);
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

            nativeLayoutInfo.Reader = TypeLoaderEnvironment.GetNativeLayoutInfoReader(nativeLayoutInfo.Module.Handle);
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

        public NativeParser GetParserForNativeLayoutInfo()
        {
            EnsureNativeLayoutInfoComputed();
            if (_templateTypeLoaderNativeLayout)
                return new NativeParser(_nativeLayoutInfo.Reader, _nativeLayoutInfo.Offset);
            else
                return default(NativeParser);
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
                _hasDictionarySlotInVTable ??= ComputeHasDictionarySlotInVTable();
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
                Debug.Assert(TemplateType != null);
                NativeParser parser = GetParserForNativeLayoutInfo();
                // Template type loader case
                var dictionaryLayoutParser = parser.GetParserForBagElementKind(BagElementKind.DictionaryLayout);

                return !dictionaryLayoutParser.IsNull;
            }
        }

        public bool HasDictionaryInVTable
        {
            get
            {
                _hasDictionaryInVTable ??= ComputeHasDictionaryInVTable();
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
                // Template type loader case
                unsafe
                {
                    if (TypeBeingBuilt.IsPointer || TypeBeingBuilt.IsByRef || TypeBeingBuilt.IsFunctionPointer)
                    {
                        // Pointers and byrefs don't have vtable slots
                        return 0;
                    }
                    if (TypeBeingBuilt.IsMdArray || (TypeBeingBuilt.IsSzArray && ((ArrayType)TypeBeingBuilt).ElementType is TypeDesc elementType
                        && (elementType.IsPointer || elementType.IsFunctionPointer)))
                    {
                        // MDArray types and pointer arrays have the same vtable as the System.Array type they "derive" from.
                        // They do not implement the generic interfaces that make this interesting for normal arrays.
                        return TypeBeingBuilt.BaseType.GetRuntimeTypeHandle().ToEETypePtr()->NumVtableSlots;
                    }
                    else
                    {
                        // This should only happen for non-universal templates
                        Debug.Assert(TypeBeingBuilt.IsTemplateCanonical());

                        TypeDesc templateType = TypeBeingBuilt.ComputeTemplate(false);
                        Debug.Assert(templateType != null);

                        // Canonical template type loader case
                        return templateType.GetRuntimeTypeHandle().ToEETypePtr()->NumVtableSlots;
                    }
                }
            }
        }

        public ushort NumVTableSlots
        {
            get
            {
                _numVTableSlots ??= ComputeNumVTableSlots();

                return _numVTableSlots.Value;
            }
        }


        public GenericTypeDictionary Dictionary;

        public int NonGcDataSize;
        public int GcDataSize;
        public int ThreadDataSize;

        public bool HasStaticConstructor => ClassConstructorPointer.HasValue;

        public IntPtr? ClassConstructorPointer;
        public IntPtr GcStaticDesc;
        public IntPtr ThreadStaticDesc;
        public uint ThreadStaticOffset;
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
                    LowLevelList<bool> instanceGCLayout;

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
                    else
                    {
                        Debug.Assert(TypeBeingBuilt.RetrieveRuntimeTypeHandleIfPossible() ||
                             TypeBeingBuilt.IsTemplateCanonical() ||
                             (TypeBeingBuilt is PointerType) ||
                             (TypeBeingBuilt is ByRefType) ||
                             (TypeBeingBuilt is FunctionPointerType));
                        _instanceGCLayout = s_emptyLayout;
                    }
                }

                if (_instanceGCLayout == s_emptyLayout)
                    return null;
                else
                    return _instanceGCLayout;
            }
        }

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

                            default:
                                typeInfoParser.SkipInteger();
                                break;
                        }
                    }
                }
                else
                {
                    // SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                    // We land here instead of throwing MissingTemplateException. We really should throw.
                }
            }
        }

        /// <summary>
        /// Get the GC layout of a type when used as a field.
        /// NOTE: if the fieldtype is a reference type, this function will return GCLayout.None
        ///       Consumers of the api must handle that special case.
        /// </summary>
        private static unsafe TypeBuilder.GCLayout GetFieldGCLayout(TypeDesc fieldType)
        {
            if (!fieldType.IsValueType)
            {
                Debug.Assert(!fieldType.IsByRef);
                if (fieldType.IsPointer || fieldType.IsFunctionPointer)
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

            Debug.Assert(fieldType.IsTemplateCanonical());
            // Pull the GC Desc from the canonical instantiation
            TypeDesc templateType = fieldType.ComputeTemplate();
            bool success = templateType.RetrieveRuntimeTypeHandleIfPossible();
            Debug.Assert(success);
            return new TypeBuilder.GCLayout(templateType.RuntimeTypeHandle);
        }


        public bool IsArrayOfReferenceTypes
        {
            get
            {
                ArrayType typeAsArrayType = TypeBeingBuilt as ArrayType;
                if (typeAsArrayType != null)
                    return !typeAsArrayType.ParameterType.IsValueType && !typeAsArrayType.ParameterType.IsPointer && !typeAsArrayType.ParameterType.IsFunctionPointer;
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
    }
}
