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
        private readonly MetadataVirtualMethodAlgorithm _virtualMethodAlgorithm = new MetadataVirtualMethodAlgorithm();

        public CompilerTypeSystemContext(TargetDetails details, SharedGenericsMode genericsMode)
            : base(details)
        {
            _genericsMode = genericsMode;
        }

        internal DefType GetClosestDefType(TypeDesc type)
        {
            if (type.IsArray)
            {
                return GetWellKnownType(WellKnownType.Array);
            }

            Debug.Assert(type is DefType);
            return (DefType)type;
        }
    }

    public partial class ReadyToRunCompilerContext : CompilerTypeSystemContext
    {
        // Depth cutoff specifies the number of repetitions of a particular generic type within a type instantiation
        // to trigger marking the type as potentially cyclic. Considering a generic type CyclicType`1<T> marked as
        // cyclic by the initial module analysis, for instance CyclicType`1<CyclicType`1<CyclicType`1<__Canon>>> has "depth 3"
        // so it will be cut off by specifying anything less than or equal to three.
        public const int DefaultGenericCycleDepthCutoff = 4;

        // Breadth cutoff specifies the minimum total number of generic types identified as potentially cyclic
        // that must appear within a type instantiation to mark it as potentially cyclic. Considering generic types
        // CyclicA`1, CyclicB`1 and CyclicC`1 marked as cyclic by the initial module analysis, a hypothetical type
        // SomeType`3<CyclicA`1<__Canon>, List`1<CyclicB`1<__Canon>>, IEnumerable`1<HashSet`1<CyclicC`1<__Canon>>>>
        // will have "breadth 3" and will be cut off by specifying anything less than or equal to three.
        public const int DefaultGenericCycleBreadthCutoff = 2;

        private ReadyToRunMetadataFieldLayoutAlgorithm _r2rFieldLayoutAlgorithm;
        private SystemObjectFieldLayoutAlgorithm _systemObjectFieldLayoutAlgorithm;
        private VectorOfTFieldLayoutAlgorithm _vectorOfTFieldLayoutAlgorithm;
        private VectorFieldLayoutAlgorithm _vectorFieldLayoutAlgorithm;
        private Int128FieldLayoutAlgorithm _int128FieldLayoutAlgorithm;
        private TypeWithRepeatedFieldsFieldLayoutAlgorithm _typeWithRepeatedFieldsFieldLayoutAlgorithm;
        private RuntimeInterfacesAlgorithm _arrayOfTRuntimeInterfacesAlgorithm;

        public ReadyToRunCompilerContext(
            TargetDetails details,
            SharedGenericsMode genericsMode,
            bool bubbleIncludesCoreModule,
            InstructionSetSupport instructionSetSupport,
            CompilerTypeSystemContext oldTypeSystemContext)
            : base(details, genericsMode)
        {
            BubbleIncludesCoreModule = bubbleIncludesCoreModule;
            InstructionSetSupport = instructionSetSupport;
            _r2rFieldLayoutAlgorithm = new ReadyToRunMetadataFieldLayoutAlgorithm();
            _systemObjectFieldLayoutAlgorithm = new SystemObjectFieldLayoutAlgorithm(_r2rFieldLayoutAlgorithm);

            // There are a few types which require special layout algorithms and which could change based on hardware
            // or to better match the underlying native ABI in the future. However, these are also fairly core types
            // which are used in many perf critical functions that exist on startup and which are often tied to an ISA.
            //
            // Given that, we treat them as ABI stable today to ensure that R2R works well. If we end up modifying the
            // ABI handling in the future, we would need to take a major version bump to R2R, which is deemed worthwhile.

            _vectorFieldLayoutAlgorithm = new VectorFieldLayoutAlgorithm(_r2rFieldLayoutAlgorithm);

            ReadOnlySpan<byte> matchingVectorType = "Unknown"u8;
            if (details.MaximumSimdVectorLength == SimdVectorLength.Vector128Bit)
                matchingVectorType = "Vector128`1"u8;
            else if (details.MaximumSimdVectorLength == SimdVectorLength.Vector256Bit)
                matchingVectorType = "Vector256`1"u8;
            else if (details.MaximumSimdVectorLength == SimdVectorLength.Vector512Bit)
                matchingVectorType = "Vector512`1"u8;

            _vectorOfTFieldLayoutAlgorithm = new VectorOfTFieldLayoutAlgorithm(_r2rFieldLayoutAlgorithm, _vectorFieldLayoutAlgorithm, matchingVectorType);
            _int128FieldLayoutAlgorithm = new Int128FieldLayoutAlgorithm(_r2rFieldLayoutAlgorithm);

            _typeWithRepeatedFieldsFieldLayoutAlgorithm = new TypeWithRepeatedFieldsFieldLayoutAlgorithm(_r2rFieldLayoutAlgorithm);

            if (oldTypeSystemContext != null)
            {
                InheritOpenModules(oldTypeSystemContext);
            }
        }

        public bool BubbleIncludesCoreModule { get; }

        public InstructionSetSupport InstructionSetSupport { get; }

        public bool TargetAllowsRuntimeCodeGeneration
        {
            get
            {
                if (Target.OperatingSystem is TargetOS.iOS or TargetOS.iOSSimulator or TargetOS.MacCatalyst or TargetOS.tvOS or TargetOS.tvOSSimulator)
                {
                    return false;
                }

                if (Target.Architecture is TargetArchitecture.Wasm32)
                {
                    return false;
                }

                return true;
            }
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
            else if (Int128FieldLayoutAlgorithm.IsIntegerType(type))
            {
                return _int128FieldLayoutAlgorithm;
            }
            else if (type is TypeWithRepeatedFields)
            {
                return _typeWithRepeatedFieldsFieldLayoutAlgorithm;
            }
            else
            {
                Debug.Assert(_r2rFieldLayoutAlgorithm != null);
                return _r2rFieldLayoutAlgorithm;
            }
        }

        /// <summary>
        /// This is a rough equivalent of the CoreCLR runtime method ReadyToRunInfo::GetFieldBaseOffset.
        /// In contrast to the auto field layout algorithm, this method unconditionally applies alignment
        /// between base and derived class (even when they reside in the same version bubble).
        /// </summary>
        public LayoutInt CalculateFieldBaseOffset(MetadataType type) => _r2rFieldLayoutAlgorithm.CalculateFieldBaseOffset(type, type.RequiresAlign8(), requiresAlignedBase: true);

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
            if (_arrayOfTRuntimeInterfacesAlgorithm == null)
            {
                _arrayOfTRuntimeInterfacesAlgorithm = new SimpleArrayOfTRuntimeInterfacesAlgorithm(SystemModule);
            }
            return _arrayOfTRuntimeInterfacesAlgorithm;
        }

        TypeDesc _asyncStateMachineBox;
        public TypeDesc AsyncStateMachineBoxType
        {
            get
            {
                if (_asyncStateMachineBox == null)
                {
                    _asyncStateMachineBox = SystemModule.GetType("System.Runtime.CompilerServices"u8, "AsyncTaskMethodBuilder`1"u8).GetNestedType("AsyncStateMachineBox`1");
                    if (_asyncStateMachineBox == null)
                        throw new Exception();
                }

                return _asyncStateMachineBox;
            }
        }

        public override bool SupportsTypeEquivalence => Target.IsWindows;
        public override bool SupportsCOMInterop => Target.IsWindows;
    }

    internal class VectorOfTFieldLayoutAlgorithm : FieldLayoutAlgorithm
    {
        private FieldLayoutAlgorithm _fallbackAlgorithm;
        private FieldLayoutAlgorithm _vectorFallbackAlgorithm;
        private byte[] _similarVectorName;
        private DefType _similarVectorOpenType;

        public VectorOfTFieldLayoutAlgorithm(FieldLayoutAlgorithm fallbackAlgorithm, FieldLayoutAlgorithm vectorFallbackAlgorithm, ReadOnlySpan<byte> similarVector)
        {
            _fallbackAlgorithm = fallbackAlgorithm;
            _vectorFallbackAlgorithm = vectorFallbackAlgorithm;
            _similarVectorName = similarVector.ToArray();
        }

        private DefType GetSimilarVector(DefType vectorOfTType)
        {
            if (_similarVectorOpenType == null)
            {
                if (_similarVectorName.SequenceEqual("Unknown"u8))
                    return null;

                _similarVectorOpenType = ((MetadataType)vectorOfTType.GetTypeDefinition()).Module.GetType("System.Runtime.Intrinsics"u8, _similarVectorName);
            }

            return ((MetadataType)_similarVectorOpenType).MakeInstantiatedType(vectorOfTType.Instantiation);
        }

        public override bool ComputeContainsGCPointers(DefType type)
        {
            return false;
        }

        public override bool ComputeContainsByRefs(DefType type)
        {
            return false;
        }

        public override bool ComputeIsUnsafeValueType(DefType type)
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
                    LayoutAbiStable = true,
                    IsVectorTOrHasVectorTFields = true,
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
                    LayoutAbiStable = true,
                    IsVectorTOrHasVectorTFields = true,
                };
#else
                return new ComputedInstanceFieldLayout
                {
                    ByteCountUnaligned = layoutFromSimilarIntrinsicVector.ByteCountUnaligned,
                    ByteCountAlignment = layoutFromMetadata.ByteCountAlignment,
                    FieldAlignment = layoutFromMetadata.FieldAlignment,
                    FieldSize = layoutFromSimilarIntrinsicVector.FieldSize,
                    Offsets = layoutFromMetadata.Offsets,
                    LayoutAbiStable = true,
                    IsVectorTOrHasVectorTFields = true,
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
            if (type.Context.Target.Architecture == TargetArchitecture.ARM64 &&
                type.Instantiation[0].IsPrimitiveNumeric)
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
            return type.IsIntrinsic && type.Namespace.SequenceEqual("System.Numerics"u8) && type.Name.SequenceEqual("Vector`1"u8);
        }
    }
}
