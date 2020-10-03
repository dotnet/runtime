// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    partial class CompilerTypeSystemContext
    {
        public CompilerTypeSystemContext(TargetDetails details, SharedGenericsMode genericsMode)
            : base(details)
        {
            _genericsMode = genericsMode;
        }

        private object _normalizedLock = new object();
        private HashSet<TypeDesc> _nonNormalizedTypes = new HashSet<TypeDesc>();
        private Dictionary<TypeDesc, TypeFlags> _normalizedTypeCategory = new Dictionary<TypeDesc, TypeFlags>();

        public TypeFlags NormalizedCategoryFor4ByteStructOnX86(TypeDesc type)
        {
            // Fast early out for cases which don't need normalization
            var typeCategory = type.Category;

            if (((typeCategory != TypeFlags.ValueType) && (typeCategory != TypeFlags.Enum)) || (type.GetElementSize().AsInt != 4))
            {
                return typeCategory;
            }

            lock(_normalizedLock)
            {
                if (_nonNormalizedTypes.Contains(type))
                    return typeCategory;
                
                if (_normalizedTypeCategory.TryGetValue(type, out TypeFlags category))
                    return category;

                if (Target.Architecture != TargetArchitecture.X86)
                {
                    throw new NotSupportedException();
                }

                TypeDesc typeOfEmbeddedField = null;
                foreach (var field in type.GetFields())
                {
                    if (field.IsStatic)
                        continue;
                    if (typeOfEmbeddedField != null)
                    {
                        // Type has more than one instance field
                        _nonNormalizedTypes.Add(type);
                        return typeCategory;
                    }

                    typeOfEmbeddedField = field.FieldType;
                }

                if ((typeOfEmbeddedField != null) && ((typeOfEmbeddedField.IsValueType) || (typeOfEmbeddedField.IsPointer)))
                {
                    TypeFlags singleElementFieldType = NormalizedCategoryFor4ByteStructOnX86(typeOfEmbeddedField);
                    if (singleElementFieldType == TypeFlags.Pointer)
                        singleElementFieldType = TypeFlags.UIntPtr;
                    
                    switch (singleElementFieldType)
                    {
                        case TypeFlags.IntPtr:
                        case TypeFlags.UIntPtr:
                        case TypeFlags.Int32:
                        case TypeFlags.UInt32:
                            _normalizedTypeCategory.Add(type, singleElementFieldType);
                            return singleElementFieldType;
                    }
                }

                _nonNormalizedTypes.Add(type);
                return typeCategory;
            }
        }
    }

    public partial class ReadyToRunCompilerContext : CompilerTypeSystemContext
    {
        private ReadyToRunMetadataFieldLayoutAlgorithm _r2rFieldLayoutAlgorithm;
        private SystemObjectFieldLayoutAlgorithm _systemObjectFieldLayoutAlgorithm;
        private VectorOfTFieldLayoutAlgorithm _vectorOfTFieldLayoutAlgorithm;
        private VectorFieldLayoutAlgorithm _vectorFieldLayoutAlgorithm;

        public ReadyToRunCompilerContext(TargetDetails details, SharedGenericsMode genericsMode, bool bubbleIncludesCorelib)
            : base(details, genericsMode)
        {
            _r2rFieldLayoutAlgorithm = new ReadyToRunMetadataFieldLayoutAlgorithm();
            _systemObjectFieldLayoutAlgorithm = new SystemObjectFieldLayoutAlgorithm(_r2rFieldLayoutAlgorithm);

            // Only the Arm64 JIT respects the OS rules for vector type abi currently
            _vectorFieldLayoutAlgorithm = new VectorFieldLayoutAlgorithm(_r2rFieldLayoutAlgorithm, (details.Architecture == TargetArchitecture.ARM64) ? true : bubbleIncludesCorelib);

            string matchingVectorType = "Unknown";
            if (details.MaximumSimdVectorLength == SimdVectorLength.Vector128Bit)
                matchingVectorType = "Vector128`1";
            else if (details.MaximumSimdVectorLength == SimdVectorLength.Vector256Bit)
                matchingVectorType = "Vector256`1";

            // No architecture has completely stable handling of Vector<T> in the abi (Arm64 may change to SVE)
            _vectorOfTFieldLayoutAlgorithm = new VectorOfTFieldLayoutAlgorithm(_r2rFieldLayoutAlgorithm, _vectorFieldLayoutAlgorithm, matchingVectorType, bubbleIncludesCorelib);
        }

        public override FieldLayoutAlgorithm GetLayoutAlgorithmForType(DefType type)
        {
            if (type.IsObject)
                return _systemObjectFieldLayoutAlgorithm;
            else if (type == UniversalCanonType)
                throw new NotImplementedException();
            else if (type.IsRuntimeDeterminedType)
                throw new NotImplementedException();
            else if (VectorOfTFieldLayoutAlgorithm.IsVectorOfTType(type))
            {
                return _vectorOfTFieldLayoutAlgorithm;
            }
            else if (VectorFieldLayoutAlgorithm.IsVectorType(type))
            {
                return _vectorFieldLayoutAlgorithm;
            }
            else
            {
                Debug.Assert(_r2rFieldLayoutAlgorithm != null);
                return _r2rFieldLayoutAlgorithm;
            }
        }

        public void SetCompilationGroup(ReadyToRunCompilationModuleGroupBase compilationModuleGroup)
        {
            _r2rFieldLayoutAlgorithm.SetCompilationGroup(compilationModuleGroup);
        }

        /// <summary>
        /// Prevent any synthetic methods being added to types in the base CompilerTypeSystemContext
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected override IEnumerable<MethodDesc> GetAllMethods(TypeDesc type)
        {
            return type.GetMethods();
        }

        protected override bool ComputeHasGCStaticBase(FieldDesc field)
        {
            Debug.Assert(field.IsStatic);

            TypeDesc fieldType = field.FieldType;
            if (fieldType.IsValueType)
            {
                return !fieldType.IsPrimitive && !fieldType.IsEnum; // In CoreCLR, all structs are implicitly boxed i.e. stored as GC pointers
            }
            else
            {
                return fieldType.IsGCPointer;
            }
        }

        /// <summary>
        /// CoreCLR has no Array`1 type to hang the various generic interfaces off.
        /// Return nothing at compile time so the runtime figures it out.
        /// </summary>
        protected override RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForNonPointerArrayType(ArrayType type)
        {
            return BaseTypeRuntimeInterfacesAlgorithm.Instance;
        }
    }

    internal class VectorOfTFieldLayoutAlgorithm : FieldLayoutAlgorithm
    {
        private FieldLayoutAlgorithm _fallbackAlgorithm;
        private FieldLayoutAlgorithm _vectorFallbackAlgorithm;
        private string _similarVectorName;
        private DefType _similarVectorOpenType;
        private bool _vectorAbiIsStable;

        public VectorOfTFieldLayoutAlgorithm(FieldLayoutAlgorithm fallbackAlgorithm, FieldLayoutAlgorithm vectorFallbackAlgorithm, string similarVector, bool vectorAbiIsStable = true)
        {
            _fallbackAlgorithm = fallbackAlgorithm;
            _vectorFallbackAlgorithm = vectorFallbackAlgorithm;
            _similarVectorName = similarVector;
            _vectorAbiIsStable = vectorAbiIsStable;
        }

        private DefType GetSimilarVector(DefType vectorOfTType)
        {
            if (_similarVectorOpenType == null)
            {
                if (_similarVectorName == "Unknown")
                    return null;

                _similarVectorOpenType = ((MetadataType)vectorOfTType.GetTypeDefinition()).Module.GetType("System.Runtime.Intrinsics", _similarVectorName);
            }

            return ((MetadataType)_similarVectorOpenType).MakeInstantiatedType(vectorOfTType.Instantiation);
        }

        public override bool ComputeContainsGCPointers(DefType type)
        {
            return false;
        }

        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType type, InstanceLayoutKind layoutKind)
        {
            DefType similarSpecifiedVector = GetSimilarVector(type);
            if (similarSpecifiedVector == null)
            {
                List<FieldAndOffset> fieldsAndOffsets = new List<FieldAndOffset>();
                foreach (FieldDesc field in type.GetFields())
                {
                    if (!field.IsStatic)
                    {
                        fieldsAndOffsets.Add(new FieldAndOffset(field, LayoutInt.Indeterminate));
                    }
                }
                ComputedInstanceFieldLayout instanceLayout = new ComputedInstanceFieldLayout()
                {
                    FieldSize = LayoutInt.Indeterminate,
                    FieldAlignment = LayoutInt.Indeterminate,
                    ByteCountUnaligned = LayoutInt.Indeterminate,
                    ByteCountAlignment = LayoutInt.Indeterminate,
                    Offsets = fieldsAndOffsets.ToArray(),
                    LayoutAbiStable = false,
                };
                return instanceLayout;
            }
            else
            {
                ComputedInstanceFieldLayout layoutFromMetadata = _fallbackAlgorithm.ComputeInstanceLayout(type, layoutKind);
                ComputedInstanceFieldLayout layoutFromSimilarIntrinsicVector = _vectorFallbackAlgorithm.ComputeInstanceLayout(similarSpecifiedVector, layoutKind);

                // TODO, enable this code when we switch Vector<T> to follow the same calling convention as its matching similar intrinsic vector
#if MATCHING_HARDWARE_VECTOR
                return new ComputedInstanceFieldLayout
                {
                    ByteCountUnaligned = layoutFromSimilarIntrinsicVector.ByteCountUnaligned,
                    ByteCountAlignment = layoutFromSimilarIntrinsicVector.ByteCountAlignment,
                    FieldAlignment = layoutFromSimilarIntrinsicVector.FieldAlignment,
                    FieldSize = layoutFromSimilarIntrinsicVector.FieldSize,
                    Offsets = layoutFromMetadata.Offsets,
                    LayoutAbiStable = _vectorAbiIsStable,
                };
#else
                return new ComputedInstanceFieldLayout
                {
                    ByteCountUnaligned = layoutFromSimilarIntrinsicVector.ByteCountUnaligned,
                    ByteCountAlignment = layoutFromMetadata.ByteCountAlignment,
                    FieldAlignment = layoutFromMetadata.FieldAlignment,
                    FieldSize = layoutFromSimilarIntrinsicVector.FieldSize,
                    Offsets = layoutFromMetadata.Offsets,
                    LayoutAbiStable = _vectorAbiIsStable,
                };
#endif
            }
        }

        public override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType type, StaticLayoutKind layoutKind)
        {
            return _fallbackAlgorithm.ComputeStaticFieldLayout(type, layoutKind);
        }

        public override ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type)
        {
            if (type.Context.Target.Architecture == TargetArchitecture.ARM64)
            {
                return type.InstanceFieldSize.AsInt switch
                {
                    16 => ValueTypeShapeCharacteristics.Vector128Aggregate,
                    _ => ValueTypeShapeCharacteristics.None
                };
            }
            return ValueTypeShapeCharacteristics.None;
        }

        public static bool IsVectorOfTType(DefType type)
        {
            return type.IsIntrinsic && type.Namespace == "System.Numerics" && type.Name == "Vector`1";
        }
    }
}
