using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes
{
    // A generic attribute whose type parameter is annotated with DynamicallyAccessedMembers can only be
    // applied with a generic-parameter argument in IL (C# forbids type parameters as generic attribute
    // arguments - CS8968), so the data-flow scenario is provided by a compiled-before IL assembly.
    // The warning originates in that dependency assembly, so it is asserted with LogContains
    // (ExpectedWarning only matches origins in the test assembly itself).
    [SetupCompileBefore("GenericAttributesDataFlow.dll", new[] { "Dependencies/GenericAttributesDataFlow.il" })]
    [LogContains("IL2091.*ClassWithUnannotatedTypeParameter", regexMatch: true)]
    class GenericAttributes
    {
        static void Main()
        {
            new WithGenericAttribute_OfString();
            new WithGenericAttribute_OfInt();
            new WithConstrainedGenericAttribute();
            typeof(WithNewConstrainedGenericAttribute).GetCustomAttributes(false);
            ReflectOnGenericAttributeWithUnannotatedTypeParameter();
        }

        [Kept]
        [KeptAttributeAttribute(typeof(GenericAttribute<string>))]
        [KeptMember(".ctor()")]
        [GenericAttribute<string>("t", F = "f", P = "p")]
        class WithGenericAttribute_OfString
        {
        }

        [Kept]
        [KeptAttributeAttribute(typeof(GenericAttribute<int>))]
        [KeptMember(".ctor()")]
        [GenericAttribute<int>(1, F = 2, P = 3)]
        class WithGenericAttribute_OfInt
        {
        }

        [Kept]
        [KeptAttributeAttribute(typeof(ConstrainedGenericAttribute<DerivedFromConstraintType>))]
        [KeptMember(".ctor()")]
        [ConstrainedGenericAttribute<DerivedFromConstraintType>()]
        class WithConstrainedGenericAttribute
        {
        }

        [KeptBaseType(typeof(Attribute))]
        class GenericAttribute<T> : Attribute
        {
            [Kept]
            public GenericAttribute(T t) { }

            [Kept]
            public T F;

            [Kept]
            [KeptBackingField]
            public T P
            {
                get;
                [Kept]
                set;
            }
        }

        [Kept]
        class ConstraintType
        {
        }

        [Kept]
        class TypeWithPublicMethods
        {
            public void Method() { }
        }

        [KeptBaseType(typeof(ConstraintType))]
        class DerivedFromConstraintType : ConstraintType
        {
        }

        [KeptBaseType(typeof(Attribute))]
        class ConstrainedGenericAttribute<T> : Attribute
            where T : ConstraintType
        {
            [Kept]
            public ConstrainedGenericAttribute() { }
        }

        [Kept]
        class Handler
        {
            [Kept]
            public Handler() { }
        }

        [Kept]
        [KeptAttributeAttribute(typeof(NewConstrainedGenericAttribute<Handler>))]
        [NewConstrainedGenericAttribute<Handler>]
        class WithNewConstrainedGenericAttribute
        {
        }

        [Kept]
        [KeptBaseType(typeof(Attribute))]
        class NewConstrainedGenericAttribute<[KeptGenericParamAttributes(GenericParameterAttributes.DefaultConstructorConstraint)] T> : Attribute
            where T : new()
        {
            [Kept]
            public NewConstrainedGenericAttribute() { }
        }

        // Reflecting over the generic instantiation keeps the dependency type and forces the trimmer
        // to analyze the generic attribute applied to it.
        [Kept]
        static void ReflectOnGenericAttributeWithUnannotatedTypeParameter()
        {
            // ClassWithUnannotatedTypeParameter<T> applies a DynamicallyAccessedMembers-annotated generic
            // attribute using its own (unverifiable) generic parameter as the argument, so the trimmer
            // must analyze the generic-argument data flow without crashing and warn (IL2091).
            Type.GetType("Mono.Linker.Tests.Cases.Attributes.Dependencies.ClassWithUnannotatedTypeParameter`1, GenericAttributesDataFlow")!
                .MakeGenericType(typeof(TypeWithPublicMethods)).GetCustomAttributes(false);
        }
    }
}
