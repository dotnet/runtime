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
                DeconstructRecordManualWithAnnotation(new(typeof(string), null));
                DeconstructRecordManualWithMismatchAnnotation(new(typeof(string), null));
                DeconstructExtensionWithAnnotation(new());
                DeconstructExtensionWithMismatchAnnotation(new());
                DeconstructNestedTuple(((typeof(string), null), null));
                DeconstructTupleLiteral(typeof(string));
                DeconstructTupleSwapSuccess(typeof(string), typeof(string));
                DeconstructTupleSwap(typeof(string), typeof(string));
                DeconstructPropertyTargetSideEffect(typeof(string), typeof(string));
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
