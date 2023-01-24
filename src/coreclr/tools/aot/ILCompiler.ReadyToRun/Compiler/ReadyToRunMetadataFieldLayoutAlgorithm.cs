// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.TypeSystem.Interop;
using Internal.CorConstants;

namespace ILCompiler
{
    public static class ReadyToRunTypeExtensions
    {
        public static LayoutInt FieldBaseOffset(this MetadataType type)
        {
            return ((ReadyToRunCompilerContext)type.Context).CalculateFieldBaseOffset(type);
        }
    }

    internal class ReadyToRunMetadataFieldLayoutAlgorithm : MetadataFieldLayoutAlgorithm
    {
        /// <summary>
        /// Map from EcmaModule instances to field layouts within the individual modules.
        /// </summary>
        private ModuleFieldLayoutMap _moduleFieldLayoutMap;

        /// <summary>
        /// Compilation module group is used to identify which types extend beyond the current version bubble.
        /// </summary>
        private ReadyToRunCompilationModuleGroupBase _compilationGroup;

        public ReadyToRunMetadataFieldLayoutAlgorithm()
        {
            _moduleFieldLayoutMap = new ModuleFieldLayoutMap();
        }

        /// <summary>
        /// Set up compilation group needed for proper calculation of base class alignment in auto layout.
        /// </summary>
        /// <param name="compilationGroup"></param>
        public void SetCompilationGroup(ReadyToRunCompilationModuleGroupBase compilationGroup)
        {
            _compilationGroup = compilationGroup;
        }

        public override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType defType, StaticLayoutKind layoutKind)
        {
            ComputedStaticFieldLayout layout = new ComputedStaticFieldLayout();
            if (defType.GetTypeDefinition() is EcmaType ecmaType)
            {
                // ECMA types are the only ones that can have statics
                ModuleFieldLayout moduleFieldLayout = _moduleFieldLayoutMap.GetOrCreateValue(ecmaType.EcmaModule);
                layout.GcStatics = moduleFieldLayout.GcStatics;
                layout.NonGcStatics = moduleFieldLayout.NonGcStatics;
                layout.ThreadGcStatics = moduleFieldLayout.ThreadGcStatics;
                layout.ThreadNonGcStatics = moduleFieldLayout.ThreadNonGcStatics;
                if (defType is EcmaType nonGenericType)
                {
                    OffsetsForType offsetsForType;
                    if (moduleFieldLayout.TypeOffsets.TryGetValue(nonGenericType.Handle, out offsetsForType))
                    {
                        layout.Offsets = _moduleFieldLayoutMap.CalculateTypeLayout(defType, moduleFieldLayout.Module, offsetsForType);
                    }
                }
                else if (defType is InstantiatedType instantiatedType)
                {
                    layout.Offsets = _moduleFieldLayoutMap.GetOrAddDynamicLayout(defType, moduleFieldLayout);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return layout;
        }

        /// <summary>
        /// Map from modules to their static field layouts.
        /// </summary>
        private class ModuleFieldLayoutMap : LockFreeReaderHashtable<EcmaModule, ModuleFieldLayout>
        {
            /// <summary>
            /// CoreCLR DomainLocalModule::OffsetOfDataBlob() / sizeof(void *)
            /// </summary>
            private const int DomainLocalModuleDataBlobOffsetAsIntPtrCount = 6;

            /// <summary>
            /// CoreCLR ThreadLocalModule::OffsetOfDataBlob() / sizeof(void *)
            /// </summary>
            private const int ThreadLocalModuleDataBlobOffsetAsIntPtrCount = 3;

            /// <summary>
            /// CoreCLR DomainLocalModule::NormalDynamicEntry::OffsetOfDataBlob for X86
            /// </summary>
            private const int DomainLocalModuleNormalDynamicEntryOffsetOfDataBlobX86 = 4;

            /// <summary>
            /// CoreCLR DomainLocalModule::NormalDynamicEntry::OffsetOfDataBlob for Amd64
            /// </summary>
            private const int DomainLocalModuleNormalDynamicEntryOffsetOfDataBlobAmd64 = 8;

            /// <summary>
            /// CoreCLR DomainLocalModule::NormalDynamicEntry::OffsetOfDataBlob for Arm64
            /// </summary>
            private const int DomainLocalModuleNormalDynamicEntryOffsetOfDataBlobArm64 = 8;

            /// <summary>
            /// CoreCLR DomainLocalModule::NormalDynamicEntry::OffsetOfDataBlob for Arm
            /// </summary>
            private const int DomainLocalModuleNormalDynamicEntryOffsetOfDataBlobArm = 8;

            /// <summary>
            /// CoreCLR DomainLocalModule::NormalDynamicEntry::OffsetOfDataBlob for LoongArch64
            /// </summary>
            private const int DomainLocalModuleNormalDynamicEntryOffsetOfDataBlobLoongArch64 = 8;

            protected override bool CompareKeyToValue(EcmaModule key, ModuleFieldLayout value)
            {
                return key == value.Module;
            }

            protected override bool CompareValueToValue(ModuleFieldLayout value1, ModuleFieldLayout value2)
            {
                return value1.Module == value2.Module;
            }

            protected override ModuleFieldLayout CreateValueFromKey(EcmaModule module)
            {
                int typeCountInModule = module.MetadataReader.GetTableRowCount(TableIndex.TypeDef);
                int pointerSize = module.Context.Target.PointerSize;

                // 0 corresponds to "normal" statics, 1 to thread-local statics
                LayoutInt[] gcStatics = new LayoutInt[StaticIndex.Count]
                {
                    LayoutInt.Zero,
                    LayoutInt.Zero
                };

                LayoutInt[] nonGcStatics = new LayoutInt[StaticIndex.Count]
                {
                    new LayoutInt(DomainLocalModuleDataBlobOffsetAsIntPtrCount * pointerSize + typeCountInModule),
                    new LayoutInt(ThreadLocalModuleDataBlobOffsetAsIntPtrCount * pointerSize + typeCountInModule),
                };

                Dictionary<TypeDefinitionHandle, OffsetsForType> typeOffsets = new Dictionary<TypeDefinitionHandle, OffsetsForType>();

                foreach (TypeDefinitionHandle typeDefHandle in module.MetadataReader.TypeDefinitions)
                {
                    TypeDefinition typeDef = module.MetadataReader.GetTypeDefinition(typeDefHandle);
                    if (typeDef.GetGenericParameters().Count != 0)
                    {
                        // Generic types are exempt from the static field layout algorithm, see
                        // <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/ceeload.cpp#L931">this check</a>.
                        continue;
                    }

                    // 0 corresponds to "normal" statics, 1 to thread-local statics
                    int[] nonGcAlignment = new int[StaticIndex.Count] { 1, 1, };
                    int[] nonGcBytes = new int[StaticIndex.Count] { 0, 0, };
                    int[] gcBytes = new int[StaticIndex.Count] { 0, 0, };

                    foreach (FieldDefinitionHandle fieldDefHandle in typeDef.GetFields())
                    {
                        FieldDefinition fieldDef = module.MetadataReader.GetFieldDefinition(fieldDefHandle);
                        if ((fieldDef.Attributes & (FieldAttributes.Static | FieldAttributes.Literal)) == FieldAttributes.Static)
                        {
                            // Static RVA fields are included when approximating offsets and sizes for the module field layout, see
                            // <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/ceeload.cpp#L939">this loop</a>.

                            int index = (IsFieldThreadStatic(in fieldDef, module.MetadataReader) ? StaticIndex.ThreadLocal : StaticIndex.Regular);
                            int alignment;
                            int size;
                            bool isGcPointerField;
                            bool isGcBoxedField;

                            CorElementType corElementType;
                            EntityHandle valueTypeHandle;

                            GetFieldElementTypeAndValueTypeHandle(in fieldDef, module.MetadataReader, out corElementType, out valueTypeHandle);
                            FieldDesc fieldDesc = module.GetField(fieldDefHandle);

                            GetElementTypeInfo(module, fieldDesc, valueTypeHandle, corElementType, pointerSize, moduleLayout: true,
                                out alignment, out size, out isGcPointerField, out isGcBoxedField);

                            if (size != 0)
                            {
                                nonGcBytes[index] += size;
                                nonGcAlignment[index] = Math.Max(nonGcAlignment[index], alignment);
                            }
                            if (isGcPointerField || isGcBoxedField)
                            {
                                gcBytes[index] += pointerSize;
                            }
                        }
                    }

                    if (nonGcBytes[StaticIndex.Regular] != 0 ||
                        nonGcBytes[StaticIndex.ThreadLocal] != 0 ||
                        gcBytes[StaticIndex.Regular] != 0 ||
                        gcBytes[StaticIndex.ThreadLocal] != 0)
                    {
                        OffsetsForType offsetsForType = new OffsetsForType(LayoutInt.Indeterminate, LayoutInt.Indeterminate, LayoutInt.Indeterminate, LayoutInt.Indeterminate);
                        for (int staticIndex = 0; staticIndex < StaticIndex.Count; staticIndex++)
                        {
                            if (nonGcBytes[staticIndex] != 0)
                            {
                                offsetsForType.NonGcOffsets[staticIndex] = LayoutInt.AlignUp(nonGcStatics[staticIndex], new LayoutInt(nonGcAlignment[staticIndex]), module.Context.Target);
                                nonGcStatics[staticIndex] = offsetsForType.NonGcOffsets[staticIndex] + new LayoutInt(nonGcBytes[staticIndex]);
                            }
                            if (gcBytes[staticIndex] != 0)
                            {
                                offsetsForType.GcOffsets[staticIndex] = gcStatics[staticIndex];
                                gcStatics[staticIndex] += new LayoutInt(gcBytes[staticIndex]);
                            }
                        }

                        typeOffsets.Add(typeDefHandle, offsetsForType);
                    }
                }

                LayoutInt blockAlignment = new LayoutInt(TargetDetails.MaximumPrimitiveSize);

                return new ModuleFieldLayout(
                    module,
                    gcStatics: new StaticsBlock() { Size = gcStatics[StaticIndex.Regular], LargestAlignment = blockAlignment },
                    nonGcStatics: new StaticsBlock() { Size = nonGcStatics[StaticIndex.Regular], LargestAlignment = blockAlignment },
                    threadGcStatics: new StaticsBlock() { Size = gcStatics[StaticIndex.ThreadLocal], LargestAlignment = blockAlignment },
                    threadNonGcStatics: new StaticsBlock() { Size = nonGcStatics[StaticIndex.ThreadLocal], LargestAlignment = blockAlignment },
                    typeOffsets: typeOffsets);
            }

            private void GetElementTypeInfoGeneric(
                EcmaModule module,
                FieldDesc fieldDesc,
                EntityHandle valueTypeHandle,
                bool moduleLayout,
                out int alignment,
                out int size,
                out bool isGcPointerField,
                out bool isGcBoxedField)
            {
                alignment = 1;
                size = 0;
                isGcPointerField = false;
                isGcBoxedField = false;

                TypeDesc fieldType = fieldDesc.FieldType;

                if (fieldType.IsPrimitive || fieldType.IsFunctionPointer || fieldType.IsPointer)
                {
                    size = fieldType.GetElementSize().AsInt;
                    alignment = size;
                }
                else if (fieldType.IsByRef || fieldType.IsByRefLike)
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, fieldDesc.OwningType);
                }
                else if (fieldType.IsValueType)
                {
                    if (IsTypeByRefLike(valueTypeHandle, module.MetadataReader))
                    {
                        ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, fieldDesc.OwningType);
                    }
                    if (moduleLayout && fieldType.GetTypeDefinition() is EcmaType ecmaType && ecmaType.EcmaModule != module)
                    {
                        // Allocate pessimistic non-GC area for cross-module fields as that's what CoreCLR does
                        // <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/ceeload.cpp#L1006">here</a>
                        alignment = TargetDetails.MaximumPrimitiveSize;
                        size = TargetDetails.MaximumPrimitiveSize;
                        isGcBoxedField = true;
                    }
                    else if (fieldType.IsEnum)
                    {
                        size = fieldType.UnderlyingType.GetElementSize().AsInt;
                        alignment = size;
                        isGcBoxedField = false;
                    }
                    else
                    {
                        // All struct statics are boxed in CoreCLR
                        isGcBoxedField = true;
                    }
                }
                else
                {
                    isGcPointerField = true;
                }
            }

            private void GetElementTypeInfo(
                EcmaModule module,
                FieldDesc fieldDesc,
                EntityHandle valueTypeHandle,
                CorElementType elementType,
                int pointerSize,
                bool moduleLayout,
                out int alignment,
                out int size,
                out bool isGcPointerField,
                out bool isGcBoxedField)
            {
                alignment = 1;
                size = 0;
                isGcPointerField = false;
                isGcBoxedField = false;

                switch (elementType)
                {
                    case CorElementType.ELEMENT_TYPE_I1:
                    case CorElementType.ELEMENT_TYPE_U1:
                    case CorElementType.ELEMENT_TYPE_BOOLEAN:
                        size = 1;
                        break;

                    case CorElementType.ELEMENT_TYPE_I2:
                    case CorElementType.ELEMENT_TYPE_U2:
                    case CorElementType.ELEMENT_TYPE_CHAR:
                        alignment = 2;
                        size = 2;
                        break;

                    case CorElementType.ELEMENT_TYPE_I4:
                    case CorElementType.ELEMENT_TYPE_U4:
                    case CorElementType.ELEMENT_TYPE_R4:
                        alignment = 4;
                        size = 4;
                        break;

                    case CorElementType.ELEMENT_TYPE_FNPTR:
                    case CorElementType.ELEMENT_TYPE_PTR:
                    case CorElementType.ELEMENT_TYPE_I:
                    case CorElementType.ELEMENT_TYPE_U:
                        alignment = pointerSize;
                        size = pointerSize;
                        break;

                    case CorElementType.ELEMENT_TYPE_I8:
                    case CorElementType.ELEMENT_TYPE_U8:
                    case CorElementType.ELEMENT_TYPE_R8:
                        alignment = 8;
                        size = 8;
                        break;

                    case CorElementType.ELEMENT_TYPE_VAR:
                    case CorElementType.ELEMENT_TYPE_MVAR:
                    case CorElementType.ELEMENT_TYPE_STRING:
                    case CorElementType.ELEMENT_TYPE_SZARRAY:
                    case CorElementType.ELEMENT_TYPE_ARRAY:
                    case CorElementType.ELEMENT_TYPE_CLASS:
                    case CorElementType.ELEMENT_TYPE_OBJECT:
                        isGcPointerField = true;
                        break;

                    case CorElementType.ELEMENT_TYPE_BYREF:
                        ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, fieldDesc.OwningType);
                        break;

                    // Statics for valuetypes where the valuetype is defined in this module are handled here.
                    // Other valuetype statics utilize the pessimistic model below.
                    case CorElementType.ELEMENT_TYPE_VALUETYPE:
                        if (IsTypeByRefLike(valueTypeHandle, module.MetadataReader))
                        {
                            ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, fieldDesc.OwningType);
                        }
                        if (moduleLayout && fieldDesc.FieldType.GetTypeDefinition() is EcmaType ecmaType && ecmaType.EcmaModule != module)
                        {
                            // Allocate pessimistic non-GC area for cross-module fields as that's what CoreCLR does
                            // <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/ceeload.cpp#L1006">here</a>
                            alignment = TargetDetails.MaximumPrimitiveSize;
                            size = TargetDetails.MaximumPrimitiveSize;
                            isGcBoxedField = true;
                        }
                        else if (fieldDesc.FieldType.IsEnum)
                        {
                            size = fieldDesc.FieldType.UnderlyingType.GetElementSize().AsInt;
                            alignment = size;
                        }
                        else
                        {
                            // All struct statics are boxed in CoreCLR
                            isGcBoxedField = true;
                        }
                        break;

                    default:
                        // Field has an unexpected type
                        throw new InvalidProgramException();
                }
            }

            public FieldAndOffset[] GetOrAddDynamicLayout(DefType defType, ModuleFieldLayout moduleFieldLayout)
            {
                FieldAndOffset[] fieldsForType;
                if (!moduleFieldLayout.TryGetDynamicLayout(defType, out fieldsForType))
                {
                    int nonGcOffset;
                    switch (moduleFieldLayout.Module.Context.Target.Architecture)
                    {
                        case TargetArchitecture.X86:
                            nonGcOffset = DomainLocalModuleNormalDynamicEntryOffsetOfDataBlobX86;
                            break;

                        case TargetArchitecture.X64:
                            nonGcOffset = DomainLocalModuleNormalDynamicEntryOffsetOfDataBlobAmd64;
                            break;

                        case TargetArchitecture.ARM64:
                            nonGcOffset = DomainLocalModuleNormalDynamicEntryOffsetOfDataBlobArm64;
                            break;

                        case TargetArchitecture.ARM:
                            nonGcOffset = DomainLocalModuleNormalDynamicEntryOffsetOfDataBlobArm;
                            break;

                        case TargetArchitecture.LoongArch64:
                            nonGcOffset = DomainLocalModuleNormalDynamicEntryOffsetOfDataBlobLoongArch64;
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    OffsetsForType offsetsForType = new OffsetsForType(
                        nonGcOffset: new LayoutInt(nonGcOffset),
                        tlsNonGcOffset: new LayoutInt(nonGcOffset),
                        gcOffset: LayoutInt.Zero,
                        tlsGcOffset: LayoutInt.Zero);

                    fieldsForType = moduleFieldLayout.GetOrAddDynamicLayout(
                        defType,
                        CalculateTypeLayout(defType, moduleFieldLayout.Module, offsetsForType));
                }

                return fieldsForType;
            }

            public FieldAndOffset[] CalculateTypeLayout(DefType defType, EcmaModule module, in OffsetsForType offsetsForType)
            {
                List<FieldAndOffset> fieldsForType = null;
                int pointerSize = module.Context.Target.PointerSize;

                // In accordance with CoreCLR runtime conventions,
                // index 0 corresponds to regular statics, index 1 to thread-local statics.
                int[][] nonGcStaticsCount = new int[StaticIndex.Count][]
                {
                    new int[TargetDetails.MaximumLog2PrimitiveSize + 1],
                    new int[TargetDetails.MaximumLog2PrimitiveSize + 1],
                };

                int[] gcPointerCount = new int[StaticIndex.Count];
                int[] gcBoxedCount = new int[StaticIndex.Count];

                foreach (FieldDesc field in defType.GetFields())
                {
                    FieldDefinition fieldDef = module.MetadataReader.GetFieldDefinition(((EcmaField)field.GetTypicalFieldDefinition()).Handle);
                    if ((fieldDef.Attributes & (FieldAttributes.Static | FieldAttributes.Literal)) == FieldAttributes.Static)
                    {
                        if ((fieldDef.Attributes & FieldAttributes.HasFieldRVA) != 0)
                            continue;

                        int index = (IsFieldThreadStatic(in fieldDef, module.MetadataReader) ? StaticIndex.ThreadLocal : StaticIndex.Regular);
                        int alignment;
                        int size;
                        bool isGcPointerField;
                        bool isGcBoxedField;

                        CorElementType corElementType;
                        EntityHandle valueTypeHandle;

                        GetFieldElementTypeAndValueTypeHandle(in fieldDef, module.MetadataReader, out corElementType, out valueTypeHandle);

                        if (defType.HasInstantiation)
                        {
                            GetElementTypeInfoGeneric(module, field, valueTypeHandle, moduleLayout: false,
                                out alignment, out size, out isGcPointerField, out isGcBoxedField);
                        }
                        else
                        {
                            GetElementTypeInfo(module, field, valueTypeHandle, corElementType, pointerSize, moduleLayout: false,
                                out alignment, out size, out isGcPointerField, out isGcBoxedField);
                        }
                        if (isGcPointerField)
                        {
                            gcPointerCount[index]++;
                        }
                        else if (isGcBoxedField)
                        {
                            gcBoxedCount[index]++;
                        }
                        else if (size != 0)
                        {
                            int log2Size = GetLog2Size(size);
                            nonGcStaticsCount[index][log2Size]++;
                        }
                    }
                }

                LayoutInt[] nonGcStaticFieldOffsets = new LayoutInt[StaticIndex.Count]
                {
                    offsetsForType.NonGcOffsets[StaticIndex.Regular],
                    offsetsForType.NonGcOffsets[StaticIndex.ThreadLocal],
                };

                LayoutInt[][] nonGcStatics = new LayoutInt[StaticIndex.Count][]
                {
                    new LayoutInt[TargetDetails.MaximumLog2PrimitiveSize + 1],
                    new LayoutInt[TargetDetails.MaximumLog2PrimitiveSize + 1],
                };

                for (int log2Size = TargetDetails.MaximumLog2PrimitiveSize; log2Size >= 0; log2Size--)
                {
                    for (int index = 0; index < StaticIndex.Count; index++)
                    {
                        LayoutInt offset = nonGcStaticFieldOffsets[index];
                        nonGcStatics[index][log2Size] = offset;
                        offset += new LayoutInt(nonGcStaticsCount[index][log2Size] << log2Size);
                        nonGcStaticFieldOffsets[index] = offset;
                    }
                }

                LayoutInt[] gcBoxedFieldOffsets = new LayoutInt[StaticIndex.Count]
                {
                    offsetsForType.GcOffsets[StaticIndex.Regular],
                    offsetsForType.GcOffsets[StaticIndex.ThreadLocal],
                };

                LayoutInt[] gcPointerFieldOffsets = new LayoutInt[StaticIndex.Count]
                {
                    offsetsForType.GcOffsets[StaticIndex.Regular] + new LayoutInt(gcBoxedCount[StaticIndex.Regular] * pointerSize),
                    offsetsForType.GcOffsets[StaticIndex.ThreadLocal] + new LayoutInt(gcBoxedCount[StaticIndex.ThreadLocal] * pointerSize)
                };

                foreach (FieldDesc field in defType.GetFields())
                {
                    FieldDefinitionHandle fieldDefHandle = ((EcmaField)field.GetTypicalFieldDefinition()).Handle;
                    FieldDefinition fieldDef = module.MetadataReader.GetFieldDefinition(fieldDefHandle);
                    if ((fieldDef.Attributes & (FieldAttributes.Static | FieldAttributes.Literal)) == FieldAttributes.Static)
                    {
                        int index = (IsFieldThreadStatic(in fieldDef, module.MetadataReader) ? StaticIndex.ThreadLocal : StaticIndex.Regular);
                        int alignment;
                        int size;
                        bool isGcPointerField;
                        bool isGcBoxedField;

                        CorElementType corElementType;
                        EntityHandle valueTypeHandle;

                        GetFieldElementTypeAndValueTypeHandle(in fieldDef, module.MetadataReader, out corElementType, out valueTypeHandle);

                        if (defType.HasInstantiation)
                        {
                            GetElementTypeInfoGeneric(module, field, valueTypeHandle, moduleLayout: false,
                                out alignment, out size, out isGcPointerField, out isGcBoxedField);
                        }
                        else
                        {
                            GetElementTypeInfo(module, field, valueTypeHandle, corElementType, pointerSize, moduleLayout: false,
                                out alignment, out size, out isGcPointerField, out isGcBoxedField);
                        }

                        LayoutInt offset = LayoutInt.Zero;

                        if ((fieldDef.Attributes & FieldAttributes.HasFieldRVA) != 0)
                        {
                            offset = new LayoutInt(fieldDef.GetRelativeVirtualAddress());
                        }
                        else if (isGcPointerField)
                        {
                            offset = gcPointerFieldOffsets[index];
                            gcPointerFieldOffsets[index] += new LayoutInt(pointerSize);
                        }
                        else if (isGcBoxedField)
                        {
                            offset = gcBoxedFieldOffsets[index];
                            gcBoxedFieldOffsets[index] += new LayoutInt(pointerSize);
                        }
                        else if (size != 0)
                        {
                            int log2Size = GetLog2Size(size);
                            offset = nonGcStatics[index][log2Size];
                            nonGcStatics[index][log2Size] += new LayoutInt(1 << log2Size);
                        }

                        if (fieldsForType == null)
                        {
                            fieldsForType = new List<FieldAndOffset>();
                        }
                        fieldsForType.Add(new FieldAndOffset(field, offset));
                    }
                }

                return fieldsForType == null ? null : fieldsForType.ToArray();
            }


            protected override int GetKeyHashCode(EcmaModule key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(ModuleFieldLayout value)
            {
                return value.Module.GetHashCode();
            }

            /// <summary>
            /// Try to locate the ThreadStatic custom attribute on the field (much like EcmaField.cs does in the method InitializeFieldFlags).
            /// </summary>
            /// <param name="fieldDef">Field definition</param>
            /// <param name="metadataReader">Metadata reader for the module</param>
            /// <returns>true when the field is marked with the ThreadStatic custom attribute</returns>
            private static bool IsFieldThreadStatic(in FieldDefinition fieldDef, MetadataReader metadataReader)
            {
                return !metadataReader.GetCustomAttributeHandle(fieldDef.GetCustomAttributes(), "System", "ThreadStaticAttribute").IsNil;
            }

            /// <summary>
            /// Try to locate the IsByRefLike attribute on the type (much like EcmaType does in ComputeTypeFlags).
            /// </summary>
            /// <param name="typeDefHandle">Handle to the field type to analyze</param>
            /// <param name="metadataReader">Metadata reader for the active module</param>
            /// <returns></returns>
            private static bool IsTypeByRefLike(EntityHandle typeDefHandle, MetadataReader metadataReader)
            {
                return typeDefHandle.Kind == HandleKind.TypeDefinition &&
                    !metadataReader.GetCustomAttributeHandle(
                        metadataReader.GetTypeDefinition((TypeDefinitionHandle)typeDefHandle).GetCustomAttributes(),
                        "System.Runtime.CompilerServices",
                        "IsByRefLikeAttribute").IsNil;
            }

            /// <summary>
            /// Partially decode field signature to obtain CorElementType and optionally the type handle for VALUETYPE fields.
            /// </summary>
            /// <param name="fieldDef">Metadata field definition</param>
            /// <param name="metadataReader">Metadata reader for the active module</param>
            /// <param name="corElementType">Output element type decoded from the signature</param>
            /// <param name="valueTypeHandle">Value type handle decoded from the signature</param>
            private static void GetFieldElementTypeAndValueTypeHandle(
                in FieldDefinition fieldDef,
                MetadataReader metadataReader,
                out CorElementType corElementType,
                out EntityHandle valueTypeHandle)
            {
                BlobReader signature = metadataReader.GetBlobReader(fieldDef.Signature);
                SignatureHeader signatureHeader = signature.ReadSignatureHeader();
                if (signatureHeader.Kind != SignatureKind.Field)
                {
                    throw new InvalidProgramException();
                }

                corElementType = ReadElementType(ref signature);
                valueTypeHandle = default(EntityHandle);
                if (corElementType == CorElementType.ELEMENT_TYPE_GENERICINST)
                {
                    corElementType = ReadElementType(ref signature);
                }

                if (corElementType == CorElementType.ELEMENT_TYPE_VALUETYPE)
                {
                    valueTypeHandle = signature.ReadTypeHandle();
                }
            }

            /// <summary>
            /// Extract element type from a field signature after skipping various modifiers.
            /// </summary>
            /// <param name="signature">Signature byte array</param>
            /// <param name="index">On input, index into the signature array. Gets modified to point after the element type on return.</param>
            /// <returns></returns>
            private static CorElementType ReadElementType(ref BlobReader signature)
            {
                // SigParser::PeekElemType
                byte signatureByte = signature.ReadByte();
                if (signatureByte < (byte)CorElementType.ELEMENT_TYPE_CMOD_REQD)
                {
                    // Fast path
                    return (CorElementType)signatureByte;
                }

                // SigParser::SkipCustomModifiers -> SkipAnyVASentinel
                if (signatureByte == (byte)CorElementType.ELEMENT_TYPE_SENTINEL)
                {
                    signatureByte = signature.ReadByte();
                }

                // SigParser::SkipCustomModifiers - modifier loop
                while (signatureByte == (byte)CorElementType.ELEMENT_TYPE_CMOD_REQD ||
                    signatureByte == (byte)CorElementType.ELEMENT_TYPE_CMOD_OPT)
                {
                    signature.ReadCompressedInteger();
                    signatureByte = signature.ReadByte();
                }
                return (CorElementType)signatureByte;
            }


            /// <summary>
            /// Return the integral value of dyadic logarithm of given size
            /// up to MaximumLog2PrimitiveSize.
            /// </summary>
            /// <param name="size">Size to calculate base 2 logarithm for</param>
            /// <returns></returns>
            private static int GetLog2Size(int size)
            {
                switch (size)
                {
                    case 0:
                    case 1:
                        return 0;
                    case 2:
                        return 1;
                    case 3:
                    case 4:
                        return 2;
                    default:
                        Debug.Assert(TargetDetails.MaximumLog2PrimitiveSize == 3);
                        return TargetDetails.MaximumLog2PrimitiveSize;
                }
            }
        }

        public static class StaticIndex
        {
            public const int Regular = 0;
            public const int ThreadLocal = 1;

            public const int Count = 2;
        }

        /// <summary>
        /// Starting offsets of static allocations for a given type.
        /// </summary>
        private struct OffsetsForType
        {
            /// <summary>
            /// Starting offset for non-GC statics in DomainLocalModule / ThreadLocalModule
            /// </summary>
            public readonly LayoutInt[] NonGcOffsets;

            /// <summary>
            /// Starting offset for GC statics in DomainLocalModule / ThreadLocalModule
            /// </summary>
            public readonly LayoutInt[] GcOffsets;

            public OffsetsForType(LayoutInt nonGcOffset, LayoutInt tlsNonGcOffset, LayoutInt gcOffset, LayoutInt tlsGcOffset)
            {
                NonGcOffsets = new LayoutInt[StaticIndex.Count] { nonGcOffset, tlsNonGcOffset };
                GcOffsets = new LayoutInt[StaticIndex.Count] { gcOffset, tlsGcOffset };
            }
        }

        /// <summary>
        /// Field layouts for a given EcmaModule.
        /// </summary>
        private class ModuleFieldLayout
        {
            public EcmaModule Module { get; }

            public StaticsBlock GcStatics { get; }

            public StaticsBlock NonGcStatics { get;  }

            public StaticsBlock ThreadGcStatics { get;  }

            public StaticsBlock ThreadNonGcStatics { get;  }

            public IReadOnlyDictionary<TypeDefinitionHandle, OffsetsForType> TypeOffsets { get; }

            private ConcurrentDictionary<DefType, FieldAndOffset[]> _genericTypeToFieldMap;

            public ModuleFieldLayout(
                EcmaModule module,
                StaticsBlock gcStatics,
                StaticsBlock nonGcStatics,
                StaticsBlock threadGcStatics,
                StaticsBlock threadNonGcStatics,
                IReadOnlyDictionary<TypeDefinitionHandle, OffsetsForType> typeOffsets)
            {
                Module = module;
                GcStatics = gcStatics;
                NonGcStatics = nonGcStatics;
                ThreadGcStatics = threadGcStatics;
                ThreadNonGcStatics = threadNonGcStatics;
                TypeOffsets = typeOffsets;

                _genericTypeToFieldMap = new ConcurrentDictionary<DefType, FieldAndOffset[]>();
            }

            public bool TryGetDynamicLayout(DefType instantiatedType, out FieldAndOffset[] fieldMap)
            {
                return _genericTypeToFieldMap.TryGetValue(instantiatedType, out fieldMap);
            }

            public FieldAndOffset[] GetOrAddDynamicLayout(DefType instantiatedType, FieldAndOffset[] fieldMap)
            {
                return _genericTypeToFieldMap.GetOrAdd(instantiatedType, fieldMap);
            }
        }

        protected override ComputedInstanceFieldLayout ComputeInstanceFieldLayout(MetadataType type, int numInstanceFields)
        {
            if (type.IsExplicitLayout)
            {
                return ComputeExplicitFieldLayout(type, numInstanceFields);
            }
            else if (type.IsSequentialLayout && !type.ContainsGCPointers)
            {
                return ComputeSequentialFieldLayout(type, numInstanceFields);
            }
            else
            {
                return ComputeAutoFieldLayout(type, numInstanceFields);
            }
        }

        /// <summary>
        /// This method decides whether the type needs aligned base offset in order to have layout resilient to
        /// base class layout changes.
        /// </summary>
        protected override void AlignBaseOffsetIfNecessary(MetadataType type, ref LayoutInt baseOffset, bool requiresAlign8, bool requiresAlignedBase)
        {
            if (requiresAlignedBase || _compilationGroup.NeedsAlignmentBetweenBaseTypeAndDerived(baseType: (MetadataType)type.BaseType, derivedType: type))
            {
                bool use8Align = (requiresAlign8 || type.BaseType.RequiresAlign8()) && type.Context.Target.Architecture != TargetArchitecture.X86;
                LayoutInt alignment = new LayoutInt(use8Align ? 8 : type.Context.Target.PointerSize);
                baseOffset = LayoutInt.AlignUp(baseOffset, alignment, type.Context.Target);
            }
        }
    }
}
