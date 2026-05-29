using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes
{
    [SetupCompileBefore("GenericAttributesDataFlow.dll", new[] { "Dependencies/GenericAttributesDataFlow.il" })]
    class GenericAttributes
    {
        static void Main()
        {
            new WithGenericAttribute_OfString();
            new WithGenericAttribute_OfInt();
            new WithConstrainedGenericAttribute();
            typeof(WithNewConstrainedGenericAttribute).GetCustomAttributes(false);
            ReflectOnGenericAttributeWithUnannotatedTypeParameter<TypeWithPublicMethods>.Test();
            ReflectOnGenericAttributeWithAnnotatedTypeParameter<TypeWithPublicMethods>.Test();
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
            [Kept]
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
        [KeptMember(".ctor()")]
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

        [Kept]
        class ReflectOnGenericAttributeWithUnannotatedTypeParameter<T>
        {
            [Kept]
            [ExpectedWarning("IL2091", Tool.Trimmer | Tool.NativeAot, "")]
            public static void Test()
            {
                GetDynamicallyAccessedMembersGenericClass(typeof(T)).GetCustomAttributes(false);
            }
        }

        [Kept]
        class ReflectOnGenericAttributeWithAnnotatedTypeParameter<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            [KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
            T>()
        {
            [Kept]
            public static void Test()
            {
                GetDynamicallyAccessedMembersGenericClass(typeof(T)).GetCustomAttributes(false);
            }
        }

        [Kept]
        static Type GetDynamicallyAccessedMembersGenericClass(Type typeArgument)
        {
            return Type.GetType("Mono.Linker.Tests.Cases.Attributes.Dependencies.DynamicallyAccessedMembersGenericClass`1, GenericAttributesDataFlow")!
                .MakeGenericType(typeArgument);
        }
    }
}
