// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [ExpectedNoWarnings]
    [SkipKeptItemsValidation]
    class ConstructedTypesDataFlow
    {
        public static void Main()
        {
            DeconstructedVariable.Test();
            ConstructedVariable.Test();
        }

        class DeconstructedVariable
        {
            [ExpectedWarning("IL2077")]
            static void DeconstructVariableNoAnnotation((Type type, object instance) input)
            {
                var (type, instance) = input;
                type.RequiresPublicMethods();
            }

            static (Type type, object instance) GetInput(int unused) => (typeof(string), null);

            [ExpectedWarning("IL2077")]
            static void DeconstructVariableFlowCapture(bool b = true)
            {
                // This creates a control-flow graph where the tuple elements assigned to
                // are flow capture references. This is only the case when the variable types
                // are declared before the deconstruction assignment, and the assignment creates
                // a branch in the control-flow graph.
                Type type;
                object instance;
                (type, instance) = GetInput(b ? 0 : 1);
                type.RequiresPublicMethods();
            }

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            static Type annotatedfield;

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            static ref Type AnnotatedProperty => ref annotatedfield;

            [ExpectedWarning("IL2062", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/linker/issues/2158")]
            [ExpectedWarning("IL2078", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/linker/issues/2158")]
            static void DeconstructVariablePropertyReference((Type type, object instance) input)
            {
                object instance;
                (AnnotatedProperty, instance) = input;
                AnnotatedProperty.RequiresPublicMethods();
            }

            record TypeAndInstance(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                Type type,
                object instance);

            // In IL based tools this is a behavior of the compiler. The attribute on the record declaration parameter
            // is only propagated to the .ctor constructor parameter. The property and field attributes are applied to the
            // generated property and field respectively. But none of the attributes is propagated to the Deconstruct method parameters.
            [ExpectedWarning("IL2067")]
            static void DeconstructRecordWithAnnotation(TypeAndInstance value)
            {
                var (type, instance) = value;
                type.RequiresPublicMethods();
            }

            class TypeAndInstanceManual
            {
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public Type type;
                public object instance;

                public TypeAndInstanceManual([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, object instance)
                    => (this.type, this.instance) = (type, instance);

                public void Deconstruct([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] out Type type, out object instance)
                    => (type, instance) = (this.type, this.instance);
            }

            // This case actually works because the annotation is correctly propagated through the Deconstruct
            static void DeconstructClassWithAnnotation(TypeAndInstanceManual value)
            {
                var (type, instance) = value;
                type.RequiresPublicMethods();
            }

            [RequiresUnreferencedCode(nameof(GetTypeAndInstance))]
            static TypeAndInstance GetTypeAndInstance() => null;

            // The deconstruction source here is itself a method call, unlike the other Deconstruct()
            // test cases above. The source call and the synthesized Deconstruct() call must be
            // tracked as two independent calls, not merged together.
            [ExpectedWarning("IL2026", nameof(GetTypeAndInstance))]
            [ExpectedWarning("IL2067")]
            static void DeconstructMethodCallSource()
            {
                var (type, instance) = GetTypeAndInstance();
                type.RequiresPublicMethods();
            }

            record TypeAndInstanceRecordManual(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                Type type,
                object instance)
            {
                // The generated property getter doesn't have the same attributes???
                // The attributes are only propagated to the generated .ctor - so suppressing the warning the this.type doesn't have the matching annotations
                //[UnconditionalSuppressMessage("", "IL2072")]
                public void Deconstruct([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] out Type type, out object instance)
                    => (type, instance) = (this.type, this.instance);
            }

            static void DeconstructRecordManualWithAnnotation(TypeAndInstanceRecordManual value)
            {
                var (type, instance) = value;
                type.RequiresPublicMethods();
            }

            [ExpectedWarning("IL2067")]
            static void DeconstructRecordManualWithMismatchAnnotation(TypeAndInstanceRecordManual value)
            {
                var (type, instance) = value;
                type.RequiresPublicFields();
            }

            static void DeconstructExtensionWithAnnotation(TypeAndInstanceExtension value)
            {
                var (type, instance) = value;
                type.RequiresPublicMethods();
            }

            [ExpectedWarning("IL2067")]
            static void DeconstructExtensionWithMismatchAnnotation(TypeAndInstanceExtension value)
            {
                var (type, instance) = value;
                type.RequiresPublicFields();
            }

            [ExpectedWarning("IL2077")]
            static void DeconstructNestedTuple(((Type type, object instance) nested, object instance) input)
            {
                var ((type, instance), outerInstance) = input;
                type.RequiresPublicMethods();
            }

            static void DeconstructTupleLiteral(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type input)
            {
                var (type, instance) = (input, new object());
                type.RequiresPublicMethods();
            }

            // The swap correctly propagates the annotation from typeWithMethods to first (via second),
            // so no warning is produced here.
            static void DeconstructTupleSwapSuccess(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type typeWithMethods,
                Type typeWithoutMethods)
            {
                Type first = typeWithoutMethods;
                Type second = typeWithMethods;
                (first, second) = (second, first);
                first.RequiresPublicMethods();
            }

            [ExpectedWarning("IL2067", nameof(DataFlowTypeExtensions.RequiresPublicMethods))]
            static void DeconstructTupleSwap(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type typeWithMethods,
                Type typeWithoutMethods)
            {
                Type first = typeWithMethods;
                Type second = typeWithoutMethods;
                (first, second) = (second, first);
                first.RequiresPublicMethods();
                second.RequiresPublicMethods();
            }

            class PropertyHolder
            {
                public Type AnnotatedProperty;
            }

            [RequiresUnreferencedCode(nameof(GetPropertyHolder))]
            static PropertyHolder GetPropertyHolder() => new();

            // The property target's receiver (GetPropertyHolder()) is a side-effecting expression that
            // must be evaluated exactly once, and (to match left-to-right evaluation order) before the
            // source values are read - not only after, as part of performing the write.
            [ExpectedWarning("IL2026", nameof(GetPropertyHolder))]
            static void DeconstructPropertyTargetSideEffect(Type first, Type second)
            {
                object other;
                (GetPropertyHolder().AnnotatedProperty, other) = (first, second);
            }

            class IndexerHolder
            {
                public Type this[int index]
                {
                    get => null;
                    set { }
                }
            }

            [RequiresUnreferencedCode(nameof(GetIndexerHolder))]
            static IndexerHolder GetIndexerHolder() => new();

            [RequiresUnreferencedCode(nameof(GetIndex))]
            static int GetIndex() => 0;

            // Like DeconstructPropertyTargetSideEffect, but for an explicit indexer target. The
            // receiver (GetIndexerHolder()) and index argument (GetIndex()) are each side-effecting
            // expressions that must be evaluated exactly once, even though they're visited from two
            // different places: the receiver ahead of the source (VisitDeconstructionTargetSideEffects),
            // and the index argument as part of performing the write (ProcessSingleTargetAssignment),
            // matching the same (pre-existing) evaluation order used for an ordinary indexer assignment.
            [ExpectedWarning("IL2026", nameof(GetIndexerHolder))]
            [ExpectedWarning("IL2026", nameof(GetIndex))]
            static void DeconstructIndexerTargetSideEffect(Type first, Type second)
            {
                object other;
                (GetIndexerHolder()[GetIndex()], other) = (first, second);
            }

            static Type GetUnannotatedType() => null;

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            static Type annotatedFieldTarget;

            // A field target (IFieldReferenceOperation) is handled directly by ProcessAssignment/
            // ProcessSingleTargetAssignment (it has no side-effecting sub-expression pre-visit of its
            // own; a static field has no instance to evaluate). This just verifies the assigned value
            // is correctly checked against the field's DynamicallyAccessedMembers requirement.
            [ExpectedWarning("IL2074", nameof(GetUnannotatedType))]
            static void DeconstructFieldTarget()
            {
                object other;
                (annotatedFieldTarget, other) = (GetUnannotatedType(), new object());
            }

            // A parameter target (IParameterReferenceOperation) reassigns an existing parameter
            // directly (no declaration expression), unlike DeconstructVariableFlowCapture's locals.
            [ExpectedWarning("IL2067")]
            static void DeconstructParameterTarget(Type type, object instance)
            {
                (type, instance) = (GetUnannotatedType(), new object());
                type.RequiresPublicMethods();
            }

            // A discard target (IDiscardOperation) drops the corresponding source value entirely -
            // there's nothing to check dataflow-wise, and it must not affect tracking of the other
            // target in the same deconstruction.
            [ExpectedWarning("IL2072")]
            static void DeconstructDiscardTarget()
            {
                (_, Type type) = (new object(), GetUnannotatedType());
                type.RequiresPublicMethods();
            }

            [RequiresUnreferencedCode(nameof(GetArrayForElementTarget))]
            static Type[] GetArrayForElementTarget() => new Type[2];

            [RequiresUnreferencedCode(nameof(GetArrayIndex))]
            static int GetArrayIndex() => 0;

            // Like DeconstructIndexerTargetSideEffect, but for an array element target
            // (IArrayElementReferenceOperation). Unlike the explicit indexer case, both the array
            // reference and the index are pre-visited by VisitDeconstructionTargetSideEffects (there's
            // no equivalent to the indexer-Arguments ordering quirk here), so both are visited twice
            // (once ahead of the source, once again performing the write) - verifying each still
            // produces exactly one warning despite the double-visit.
            [ExpectedWarning("IL2026", nameof(GetArrayForElementTarget))]
            [ExpectedWarning("IL2026", nameof(GetArrayIndex))]
            static void DeconstructArrayElementTargetSideEffect(Type first, Type second)
            {
                object other;
                (GetArrayForElementTarget()[GetArrayIndex()], other) = (first, second);
            }

            [RequiresUnreferencedCode(nameof(GetArrayForImplicitIndexerTarget))]
            static Type[] GetArrayForImplicitIndexerTarget() => new Type[2];

            // An implicit System.Index-based indexer target (IImplicitIndexerReferenceOperation),
            // e.g. 'arr[^1]'. The receiver is a side-effecting expression visited ahead of the source,
            // same as the explicit indexer and array element cases above.
            [ExpectedWarning("IL2026", nameof(GetArrayForImplicitIndexerTarget))]
            static void DeconstructImplicitIndexerTargetSideEffect(Type first, Type second)
            {
                object other;
                (GetArrayForImplicitIndexerTarget()[^1], other) = (first, second);
            }

            struct ConversionTarget
            {
                public static implicit operator ConversionTarget(Type type) => default;
            }

            // Deconstructing an element through a user-defined conversion operator (Type ->
            // ConversionTarget here) can't be modeled by the analyzer - the operator body is opaque -
            // so EvaluateDeconstruction treats the converted value as unknown (top) rather than
            // reusing the source's own tracked value, which would no longer make sense once its type
            // has changed. ConversionTarget isn't itself a dataflow-tracked type, so this mainly
            // verifies the conversion-operator code path in EvaluateDeconstruction runs without
            // hitting UnexpectedOperationHandler or otherwise producing an unexpected warning (see
            // [ExpectedNoWarnings] on the containing class).
            static void DeconstructWithUserDefinedConversion(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type typeWithMethods)
            {
                (ConversionTarget converted, object instance) = (typeWithMethods, new object());
            }

            [ExpectedWarning("IL2077")]
            static void DeconstructForeach((Type type, object instance)[] inputs)
            {
                foreach (var (type, instance) in inputs)
                    type.RequiresPublicMethods();
            }

            public static void Test()
            {
                DeconstructVariableNoAnnotation((typeof(string), null));
                DeconstructVariableFlowCapture();
                DeconstructVariablePropertyReference((typeof(string), null));
                DeconstructRecordWithAnnotation(new(typeof(string), null));
                DeconstructClassWithAnnotation(new(typeof(string), null));
                DeconstructMethodCallSource();
                DeconstructRecordManualWithAnnotation(new(typeof(string), null));
                DeconstructRecordManualWithMismatchAnnotation(new(typeof(string), null));
                DeconstructExtensionWithAnnotation(new());
                DeconstructExtensionWithMismatchAnnotation(new());
                DeconstructNestedTuple(((typeof(string), null), null));
                DeconstructTupleLiteral(typeof(string));
                DeconstructTupleSwapSuccess(typeof(string), typeof(string));
                DeconstructTupleSwap(typeof(string), typeof(string));
                DeconstructPropertyTargetSideEffect(typeof(string), typeof(string));
                DeconstructIndexerTargetSideEffect(typeof(string), typeof(string));
                DeconstructFieldTarget();
                DeconstructParameterTarget(typeof(string), null);
                DeconstructDiscardTarget();
                DeconstructArrayElementTargetSideEffect(typeof(string), typeof(string));
                DeconstructImplicitIndexerTargetSideEffect(typeof(string), typeof(string));
                DeconstructWithUserDefinedConversion(typeof(string));
                DeconstructForeach(new[] { (typeof(string), (object)null) });
            }
        }

        class ConstructedVariable
        {
            [ExpectedWarning("IL2077")]
            static void ConstructedType()
            {
                var ct = (typeof(string), 1);
                ct.Item1.RequiresPublicMethods();
            }

            [ExpectedWarning("IL2077")]
            static void ConstructedTypeNamed()
            {
                (Type Type, int Value) ct = (typeof(string), 1);
                ct.Type.RequiresPublicMethods();
            }

            [ExpectedWarning("IL2077")]
            static void ConstructedTypeWithAnnotations([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
            {
                var ct = (type, 1);
                ct.Item1.RequiresPublicMethods();
            }

            [ExpectedWarning("IL2072")]
            static void AnonymousTypeWithoutAnnotations()
            {
                var ct = new
                {
                    Type = typeof(string),
                    Value = 1
                };

                ct.Type.RequiresPublicMethods();
            }

            [ExpectedWarning("IL2072")]
            static void AnonymousTypeWithExplicitTypesWithoutAnnotations()
            {
                var ct = new
                {
                    Type = typeof(string),
                    Value = 1
                };

                ct.Type.RequiresPublicMethods();
            }

            // Compiler doesn't propagate attributes, only types
            [ExpectedWarning("IL2072")]
            static void AnonymousTypeWithAnnotation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
            {
                var ct = new
                {
                    Type = type,
                    Value = 1
                };

                ct.Type.RequiresPublicMethods();
            }

            record TypeAndValue([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type Type, int Value);

            [ExpectedWarning("IL2067", "typeUnknown")]
            static void RecordConstruction(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type typeWithPublicMethods,
                Type typeUnknown)
            {
                _ = new TypeAndValue(typeof(string), 1);
                _ = new TypeAndValue(typeWithPublicMethods, 2);
                _ = new TypeAndValue(typeUnknown, 3);
            }

            public static void Test()
            {
                ConstructedType();
                ConstructedTypeNamed();
                ConstructedTypeWithAnnotations(typeof(string));

                AnonymousTypeWithoutAnnotations();
                AnonymousTypeWithExplicitTypesWithoutAnnotations();
                AnonymousTypeWithAnnotation(typeof(string));

                RecordConstruction(typeof(string), typeof(string));
            }
        }
    }

    class TypeAndInstanceExtension
    {
    }

    static class TypeAndInstanceExtensions
    {
        public static void Deconstruct(
            this TypeAndInstanceExtension value,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] out Type type,
            out object instance)
        {
            type = typeof(string);
            instance = null;
        }
    }
}
