// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using VectorIntrinsicFieldLayoutAlgorithm = ILCompiler.VectorFieldLayoutAlgorithm;

namespace ILCompiler
{
    partial class CompilerTypeSystemContext
    {
        public CompilerTypeSystemContext(TargetDetails details, SharedGenericsMode genericsMode)
            : base(details)
        {
            _genericsMode = genericsMode;
        }
    }

    public partial class ReadyToRunCompilerContext : CompilerTypeSystemContext
    {
        private ReadyToRunMetadataFieldLayoutAlgorithm _r2rFieldLayoutAlgorithm;
        private SystemObjectFieldLayoutAlgorithm _systemObjectFieldLayoutAlgorithm;
        private VectorFieldLayoutAlgorithm _vectorFieldLayoutAlgorithm;
        VectorIntrinsicFieldLayoutAlgorithm _vectorIntrinsicFieldLayoutAlgorithm;

        public ReadyToRunCompilerContext(TargetDetails details, SharedGenericsMode genericsMode)
            : base(details, genericsMode)
        {
            _r2rFieldLayoutAlgorithm = new ReadyToRunMetadataFieldLayoutAlgorithm();
            _systemObjectFieldLayoutAlgorithm = new SystemObjectFieldLayoutAlgorithm(_r2rFieldLayoutAlgorithm);
            _vectorFieldLayoutAlgorithm = new VectorFieldLayoutAlgorithm(_r2rFieldLayoutAlgorithm);
            _vectorIntrinsicFieldLayoutAlgorithm = new VectorIntrinsicFieldLayoutAlgorithm(_r2rFieldLayoutAlgorithm);
        }

        public override FieldLayoutAlgorithm GetLayoutAlgorithmForType(DefType type)
        {
            if (type.IsObject)
                return _systemObjectFieldLayoutAlgorithm;
            else if (type == UniversalCanonType)
                throw new NotImplementedException();
            else if (type.IsRuntimeDeterminedType)
                throw new NotImplementedException();
            else if (type.IsIntrinsic && (type.Name == "Vector`1") && (type.Namespace == "System.Numerics"))
            {
                return _vectorFieldLayoutAlgorithm;
            }
            else if (VectorIntrinsicFieldLayoutAlgorithm.IsVectorType(type))
            {
                return _vectorIntrinsicFieldLayoutAlgorithm;
            }
            else
            {
                Debug.Assert(_r2rFieldLayoutAlgorithm != null);
                return _r2rFieldLayoutAlgorithm;
            }
        }

        public void SetCompilationGroup(CompilationModuleGroup compilationModuleGroup)
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

        private class VectorFieldLayoutAlgorithm : FieldLayoutAlgorithm
        {
            private FieldLayoutAlgorithm _fallbackAlgorithm;

            public VectorFieldLayoutAlgorithm(FieldLayoutAlgorithm fallbackAlgorithm)
            {
                _fallbackAlgorithm = fallbackAlgorithm;
            }

            public override bool ComputeContainsGCPointers(DefType type)
            {
                return _fallbackAlgorithm.ComputeContainsGCPointers(type);
            }

            public override DefType ComputeHomogeneousFloatAggregateElementType(DefType type)
            {
                return _fallbackAlgorithm.ComputeHomogeneousFloatAggregateElementType(type);
            }

            public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType type, InstanceLayoutKind layoutKind)
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
                };
                return instanceLayout;
            }

            public override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType type, StaticLayoutKind layoutKind)
            {
                return _fallbackAlgorithm.ComputeStaticFieldLayout(type, layoutKind);
            }

            public override ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type)
            {
                return _fallbackAlgorithm.ComputeValueTypeShapeCharacteristics(type);
            }
        }
    }
}
