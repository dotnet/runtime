// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.DataFlow.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [SkipKeptItemsValidation]
    [SandboxDependency("Dependencies/TestSystemTypeBase.cs")]
    [SetupCompileBefore("skiplibrary.dll", new[] { "Dependencies/Library.cs" })]
    [SetupLinkerAction("skip", "skiplibrary")]

    // Suppress warnings about accessing methods with annotations via reflection - the test below does that a LOT
    // (The test accessed these methods through DynamicallyAccessedMembers annotations which is effectively the same reflection access)
    [UnconditionalSuppressMessage("test", "IL2111")]

    [ExpectedNoWarnings]
    class VirtualMethodHierarchyDataflowAnnotationValidation
    {
        // The code below marks methods which have RUC on them, it's not the point of this test to validate these here
        [UnconditionalSuppressMessage("test", "IL2026")]
        // The code below marks methods which have RDC on them, it's not the point of this test to validate these here
        [UnconditionalSuppressMessage("test", "IL3050")]
        public static void Main()
        {
            // The test uses data flow annotation to mark all public methods on the specified types
            // which in turn will trigger querying the annotations on those methods and thus the validation.

            RequirePublicMethods(typeof(BaseClass));
            RequirePublicMethods(typeof(DerivedClass));
            RequirePublicMethods(typeof(SuperDerivedClass));
            RequirePublicMethods(typeof(DerivedOverNoAnnotations));
            RequirePublicMethods(typeof(DerivedWithNoAnnotations));
            RequirePublicMethods(typeof(IBase));
            RequirePublicMethods(typeof(IDerived));
            RequirePublicMethods(typeof(ImplementationClass));
            RequirePublicMethods(typeof(IBaseImplementedInterface));
            RequirePublicMethods(typeof(BaseImplementsInterfaceViaDerived));
            RequirePublicMethodsAndConstructor(typeof(DerivedWithInterfaceImplementedByBase));
            RequirePublicMethods(typeof(VirtualMethodHierarchyDataflowAnnotationValidationTypeTestBase));
            RequirePublicMethods(typeof(VirtualMethodHierarchyDataflowAnnotationValidationTypeTestDerived));
            RequirePublicMethods(typeof(ITwoInterfacesImplementedByOneMethod_One));
            RequirePublicMethods(typeof(ITwoInterfacesImplementedByOneMethod_Two));
            RequirePublicMethods(typeof(ImplementationOfTwoInterfacesWithOneMethod));
            StaticInterfaceMethods.Test();
            BaseInPreservedScope.Test();
            DirectCall.Test();
            RequiresAndDynamicallyAccessedMembersValidation.Test();
            InstantiatedGeneric.Test();
            AnnotationOnUnsupportedType.Test();
        }

        static void RequirePublicMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
        }

        static void RequirePublicMethodsAndConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
        }

        class BaseClass
        {
            // === Return values ===
            // Other than the basics, the return value also checks all of the inheritance cases - we omit those for the other tests
            public virtual Type ReturnValueBaseWithoutDerivedWithout() => null;
            public virtual Type ReturnValueBaseWithoutDerivedWith() => null;
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            public virtual Type ReturnValueBaseWithDerivedWithout() => null;
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            public virtual Type ReturnValueBaseWithDerivedWith() => null;

            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            public virtual Type ReturnValueBaseWithSuperDerivedWithout() => null;

            // === Method parameters ===
            // This does not check complicated inheritance cases as that is already validated by the return values
            public virtual void SingleParameterBaseWithDerivedWithout(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                Type p)
            { }

            public virtual void SingleParameterBaseWithDerivedWith_(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                Type p)
            { }

            public virtual void SingleParameterBaseWithoutDerivedWith_(Type p) { }

            public virtual void SingleParameterBaseWithoutDerivedWithout(Type p) { }

            public virtual void SingleParameterBaseWithDerivedWithDifferent(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                Type p)
            { }

            public virtual void MultipleParametersBaseWithDerivedWithout(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                Type p1BaseWithDerivedWithout,
                Type p2BaseWithoutDerivedWithout,
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                Type p3BaseWithDerivedWithout)
            { }

            public virtual void MultipleParametersBaseWithoutDerivedWith(
                Type p1BaseWithoutDerivedWith,
                Type p2BaseWithoutDerivedWithout,
                Type p3BaseWithoutDerivedWith)
            { }

            public virtual void MultipleParametersBaseWithDerivedWithMatch(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                Type p1BaseWithDerivedWith,
                Type p2BaseWithoutDerivedWithout,
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                Type p3BaseWithDerivedWith)
            { }

            public virtual void MultipleParametersBaseWithDerivedWithMismatch(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                Type p1BaseWithDerivedWithMismatch,
                Type p2BaseWithoutDerivedWith,
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                Type p3BaseWithDerivedWithMatch,
                Type p4NoAnnotations)
            { }

            // === Generic methods ===
            public virtual void GenericBaseWithDerivedWithout<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() { }
            public virtual void GenericBaseWithoutDerivedWith<T>() { }
            public virtual void GenericBaseWithDerivedWith_<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() { }
            public virtual void GenericBaseWithoutDerivedWithout<T>() { }

            // === Properties ===
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public virtual Type PropertyBaseWithDerivedWithout { get; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public virtual Type PropertyBaseWithDerivedWith_ { get; set; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public virtual Type PropertyBaseWithDerivedOnGetterWith { get; }

            // === RequiresUnreferencedCode ===
            [RequiresUnreferencedCode("")]
            public virtual void RequiresUnreferencedCodeBaseWithDerivedWithout() { }
            public virtual void RequiresUnreferencedCodeBaseWithoutDerivedWith_() { }
            [RequiresUnreferencedCode("")]
            public virtual void RequiresUnreferencedCodeBaseWithDerivedWith_() { }
            public virtual void RequiresUnreferencedCodeBaseWithoutDerivedWithout() { }

            public virtual void RequiresUnreferencedCodeBaseWithoutSuperDerivedWith_() { }
        }

        class DerivedClass : BaseClass
        {
            // === Return values ===
            // Return values are covariant: override can ADD annotations (strengthen postcondition)
            // but cannot REMOVE annotations that the base declares.
            [LogDoesNotContain("DerivedClass.ReturnValueBaseWithoutDerivedWithout")]
            public override Type ReturnValueBaseWithoutDerivedWithout() => null;

            // Adding DAMT to return value is now allowed (covariant - strengthening postcondition)
            [LogDoesNotContain("DerivedClass.ReturnValueBaseWithoutDerivedWith")]
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            public override Type ReturnValueBaseWithoutDerivedWith() => null;

            // Removing DAMT from return value is NOT allowed (covariant - weakening postcondition)
            [ExpectedWarning("IL2093", "DerivedClass.ReturnValueBaseWithDerivedWithout", "BaseClass.ReturnValueBaseWithDerivedWithout")]
            public override Type ReturnValueBaseWithDerivedWithout() => null;

            [LogDoesNotContain("DerivedClass.ReturnValueBaseWithDerivedWitht")]
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            public override Type ReturnValueBaseWithDerivedWith() => null;


            // === Method parameters ===
            // Parameters are contravariant: override can REMOVE annotations (weaken precondition)
            // but cannot ADD annotations that the base doesn't declare.

            // Removing DAMT from parameter is now allowed (contravariant - weakening precondition)
            [LogDoesNotContain("DerivedClass.SingleParameterBaseWithDerivedWithout")]
            public override void SingleParameterBaseWithDerivedWithout(Type p) { }

            [LogDoesNotContain("DerivedClass.SingleParameterBaseWithDerivedWith_")]
            public override void SingleParameterBaseWithDerivedWith_(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                Type p)
            { }

            // Adding DAMT to parameter is NOT allowed (contravariant - strengthening precondition)
            [ExpectedWarning("IL2092",
                "p", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedClass.SingleParameterBaseWithoutDerivedWith_(Type)",
                "p", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseClass.SingleParameterBaseWithoutDerivedWith_(Type)")]
            public override void SingleParameterBaseWithoutDerivedWith_(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                Type p)
            { }

            [LogDoesNotContain("DerivedClass.SingleParameterBaseWithoutDerivedWithout")]
            public override void SingleParameterBaseWithoutDerivedWithout(Type p) { }

            // Changing DAMT on parameter where new value is not subset of base is NOT allowed
            [ExpectedWarning("IL2092",
                "p", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedClass.SingleParameterBaseWithDerivedWithDifferent(Type)",
                "p", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseClass.SingleParameterBaseWithDerivedWithDifferent(Type)")]
            public override void SingleParameterBaseWithDerivedWithDifferent(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                Type p)
            { }


            // Removing DAMT from parameters is now allowed (contravariant)
            [LogDoesNotContain("DerivedClass.MultipleParametersBaseWithDerivedWithout")]
            public override void MultipleParametersBaseWithDerivedWithout(
                Type p1BaseWithDerivedWithout,
                Type p2BaseWithoutDerivedWithout,
                Type p3BaseWithDerivedWithout)
            { }

            // Adding DAMT to parameters is NOT allowed (contravariant)
            [LogContains(".*'p1BaseWithoutDerivedWith'.*DerivedClass.*MultipleParametersBaseWithoutDerivedWith.*", regexMatch: true)]
            [LogDoesNotContain(".*'p2BaseWithoutDerivedWithout'.*DerivedClass.*MultipleParametersBaseWithoutDerivedWith.*", regexMatch: true)]
            [LogContains(".*'p3BaseWithoutDerivedWith'.*DerivedClass.*MultipleParametersBaseWithoutDerivedWith.*", regexMatch: true)]
            public override void MultipleParametersBaseWithoutDerivedWith(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                Type p1BaseWithoutDerivedWith,
                Type p2BaseWithoutDerivedWithout,
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                Type p3BaseWithoutDerivedWith)
            { }

            [LogDoesNotContain("DerivedClass.MultipleParametersBaseWithDerivedWithMatch")]
            public override void MultipleParametersBaseWithDerivedWithMatch(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                Type p1BaseWithDerivedWith,
                Type p2BaseWithoutDerivedWithout,
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                Type p3BaseWithDerivedWith)
            { }

            // p1 changes from PublicMethods to PublicFields - not a subset, should warn
            // p2 adds PublicFields where base has none - should warn
            // p3 matches base - no warning
            // p4 no annotations - no warning
            [LogContains(".*'p1BaseWithDerivedWithMismatch'.*DerivedClass.*MultipleParametersBaseWithDerivedWithMismatch.*", regexMatch: true)]
            [LogContains(".*'p2BaseWithoutDerivedWith'.*DerivedClass.*MultipleParametersBaseWithDerivedWithMismatch.*", regexMatch: true)]
            [LogDoesNotContain(".*'p3BaseWithDerivedWithMatch'.*DerivedClass.*MultipleParametersBaseWithDerivedWithMismatch.*", regexMatch: true)]
            [LogDoesNotContain(".*'p4NoAnnotations'.*DerivedClass.*MultipleParametersBaseWithDerivedWithMismatch.*", regexMatch: true)]
            public override void MultipleParametersBaseWithDerivedWithMismatch(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                Type p1BaseWithDerivedWithMismatch,
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                Type p2BaseWithoutDerivedWith,
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                Type p3BaseWithDerivedWithMatch,
                Type p4NoAnnotations)
            { }

            // === Generic methods ===
            // Generic parameters are contravariant: override can REMOVE annotations but cannot ADD

            // Removing DAMT from generic parameter is now allowed (contravariant)
            [LogDoesNotContain("DerivedClass.GenericBaseWithDerivedWithout")]
            public override void GenericBaseWithDerivedWithout<T>() { }

            // Adding DAMT to generic parameter is NOT allowed (contravariant)
            [ExpectedWarning("IL2095",
                "T", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedClass.GenericBaseWithoutDerivedWith<T>()",
                "T", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseClass.GenericBaseWithoutDerivedWith<T>()")]
            public override void GenericBaseWithoutDerivedWith<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() { }

            [LogDoesNotContain("DerivedClass.GenericBaseWithDerivedWith_")]
            public override void GenericBaseWithDerivedWith_<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() { }

            [LogDoesNotContain("DerivedClass.GenericBaseWithoutDerivedWithout")]
            public override void GenericBaseWithoutDerivedWithout<T>() { }


            // === Properties ===
            // Properties with DAMT on the type affect both getter (return) and setter (parameter)
            // For getter (return value) - covariant rules apply
            // Removing DAMT from property (affects return value) is NOT allowed
            [ExpectedWarning("IL2093",
                "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedClass.PropertyBaseWithDerivedWithout.get",
                "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseClass.PropertyBaseWithDerivedWithout.get")]
            public override Type PropertyBaseWithDerivedWithout { get; }

            [LogDoesNotContain("DerivedClass.PropertyBaseWithDerivedWith_.get")]
            [LogDoesNotContain("DerivedClass.PropertyBaseWithDerivedWith_.set")]
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public override Type PropertyBaseWithDerivedWith_ { get; set; }

            [LogDoesNotContain("PropertyBaseWithDerivedOnGetterWith")]
            [field: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public override Type PropertyBaseWithDerivedOnGetterWith { [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] get; }


            // === RequiresUnreferencedCode ===
            // Variance rules: Override can REMOVE Requires* attributes (weaker precondition is OK)
            // but cannot ADD Requires* attributes that the base doesn't have

            // Removing [RUC] is now allowed (variance - weakening precondition)
            [LogDoesNotContain("DerivedClass.RequiresUnreferencedCodeBaseWithDerivedWithout")]
            public override void RequiresUnreferencedCodeBaseWithDerivedWithout() { }

            // Adding [RUC] is NOT allowed (variance - strengthening precondition)
            [ExpectedWarning("IL2046", "DerivedClass.RequiresUnreferencedCodeBaseWithoutDerivedWith_()",
                "BaseClass.RequiresUnreferencedCodeBaseWithoutDerivedWith_()",
                "'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides")]
            [RequiresUnreferencedCode("")]
            public override void RequiresUnreferencedCodeBaseWithoutDerivedWith_() { }
            [LogDoesNotContain("DerivedClass.RequiresUnreferencedCodeBaseWithDerivedWith_")]
            [RequiresUnreferencedCode("")]
            public override void RequiresUnreferencedCodeBaseWithDerivedWith_() { }
            [LogDoesNotContain("DerivedClass.RequiresUnreferencedCodeBaseWithoutDerivedWithout")]
            public override void RequiresUnreferencedCodeBaseWithoutDerivedWithout() { }
        }

        class InBetweenDerived : DerivedClass
        {
            // This is intentionally left empty to validate that the logic can skip over to deeper base classes correctly
        }

        class SuperDerivedClass : InBetweenDerived
        {
            // === Return values ===
            // Removing DAMT from return value is NOT allowed (covariant - weakening postcondition)
            [ExpectedWarning("IL2093",
                "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.SuperDerivedClass.ReturnValueBaseWithSuperDerivedWithout()",
                "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseClass.ReturnValueBaseWithSuperDerivedWithout()")]
            public override Type ReturnValueBaseWithSuperDerivedWithout() => null;

            // === RequiresUnreferencedCode ===
            // Adding [RUC] is NOT allowed (variance - strengthening precondition)
            [ExpectedWarning("IL2046", "SuperDerivedClass.RequiresUnreferencedCodeBaseWithoutSuperDerivedWith_")]
            [RequiresUnreferencedCode("")]
            public override void RequiresUnreferencedCodeBaseWithoutSuperDerivedWith_() { }
        }


        abstract class BaseWithNoAnnotations
        {
            // This class must not have ANY annotations anywhere on it.
            // It's here to test that the optimization works (as most classes won't have any annotations, so we shortcut that path).

            // === Return values ===
            public abstract Type ReturnValueBaseWithoutDerivedWith();

            public abstract Type ReturnValueBaseWithoutDerivedWithout();

            // === Method parameters ===
            public virtual void SingleParameterBaseWithoutDerivedWith_(Type p) { }

            public virtual void SingleParameterBaseWithoutDerivedWithout(Type p) { }

            // === Generic methods ===
            public virtual void GenericBaseWithoutDerivedWith_<T>() { }

            public virtual void GenericBaseWithoutDerivedWithout<T>() { }

            // === RequiresUnreferencedCode ===
            public virtual void RequiresUnreferencedCodeBaseWithoutDerivedWith_() { }
            public virtual void RequiresUnreferencedCodeBaseWithoutDerivedWithout() { }
        }

        class DerivedOverNoAnnotations : BaseWithNoAnnotations
        {
            // === Return values ===
            // Adding DAMT to return value is now allowed (covariant - strengthening postcondition)
            [LogDoesNotContain("DerivedOverNoAnnotations.ReturnValueBaseWithoutDerivedWith")]
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            public override Type ReturnValueBaseWithoutDerivedWith() => null;

            [LogDoesNotContain("DerivedOverNoAnnotations.ReturnValueBaseWithoutDerivedWithout")]
            public override Type ReturnValueBaseWithoutDerivedWithout() => null;

            // === Method parameters ===
            // Adding DAMT to parameter is NOT allowed (contravariant - strengthening precondition)
            [ExpectedWarning("IL2092",
                "p", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedOverNoAnnotations.SingleParameterBaseWithoutDerivedWith_(Type)",
                "p", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseWithNoAnnotations.SingleParameterBaseWithoutDerivedWith_(Type)")]
            public override void SingleParameterBaseWithoutDerivedWith_(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                Type p)
            { }

            [LogDoesNotContain("DerivedOverNoAnnotations.SingleParameterBaseWithoutDerivedWithout")]
            public override void SingleParameterBaseWithoutDerivedWithout(Type p) { }

            // === Generic methods ===
            // Adding DAMT to generic parameter is NOT allowed (contravariant - strengthening precondition)
            [ExpectedWarning("IL2095",
                "T", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedOverNoAnnotations.GenericBaseWithoutDerivedWith_<T>()",
                "T", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseWithNoAnnotations.GenericBaseWithoutDerivedWith_<T>()")]
            public override void GenericBaseWithoutDerivedWith_<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() { }

            [LogDoesNotContain("DerivedOverNoAnnotations.GenericBaseWithoutDerivedWithout")]
            public override void GenericBaseWithoutDerivedWithout<T>() { }


            // === RequiresUnreferencedCode ===
            // Adding [RUC] is NOT allowed (variance - strengthening precondition)
            [ExpectedWarning("IL2046", "DerivedOverNoAnnotations.RequiresUnreferencedCodeBaseWithoutDerivedWith_")]
            [RequiresUnreferencedCode("")]
            public override void RequiresUnreferencedCodeBaseWithoutDerivedWith_() { }
            [LogDoesNotContain("DerivedOverNoAnnotations.RequiresUnreferencedCodeBaseWithoutDerivedWithout")]
            public override void RequiresUnreferencedCodeBaseWithoutDerivedWithout() { }
        }


        abstract class BaseWithAnnotations
        {
            // === Return values ===
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            public abstract Type ReturnValueBaseWithDerivedWithout();

            public abstract Type ReturnValueBaseWithoutDerivedWithout();

            // === Method parameters ===
            public virtual void SingleParameterBaseWithDerivedWithout(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                Type p)
            { }

            public virtual void SingleParameterBaseWithDerivedWith_(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                Type p)
            { }

            // === Generic methods ===
            public virtual void GenericBaseWithDerivedWithout<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() { }

            public virtual void GenericBaseWithoutDerivedWithout<T>() { }


            // === RequiresUnreferencedCode ===
            [RequiresUnreferencedCode("")]
            public virtual void RequiresUnreferencedCodeBaseWithDerivedWith_() { }
            [RequiresUnreferencedCode("")]
            public virtual void RequiresUnreferencedCodeBaseWithDerivedWithout() { }
        }

        class DerivedWithNoAnnotations : BaseWithAnnotations
        {
            // This class must not have ANY annotations anywhere on it.
            // It's here to test that the optimization works (as most classes won't have any annotations, so we shortcut that path).

            // === Return values ===
            // Removing DAMT from return value is NOT allowed (covariant - weakening postcondition)
            [ExpectedWarning("IL2093",
                "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedWithNoAnnotations.ReturnValueBaseWithDerivedWithout()",
                "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseWithAnnotations.ReturnValueBaseWithDerivedWithout()")]
            public override Type ReturnValueBaseWithDerivedWithout() => null;

            [LogDoesNotContain("DerivedWithNoAnnotations.ReturnValueBaseWithoutDerivedWithout")]
            public override Type ReturnValueBaseWithoutDerivedWithout() => null;

            // === Method parameters ===
            // Removing DAMT from parameter is now allowed (contravariant - weakening precondition)
            [LogDoesNotContain("DerivedWithNoAnnotations.SingleParameterBaseWithDerivedWithout")]
            public override void SingleParameterBaseWithDerivedWithout(Type p) { }

            [LogDoesNotContain("DerivedWithNoAnnotations.SingleParameterBaseWithDerivedWith_")]
            public override void SingleParameterBaseWithDerivedWith_(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                Type p)
            { }

            // === Generic methods ===
            // Removing DAMT from generic parameter is now allowed (contravariant - weakening precondition)
            [LogDoesNotContain("DerivedWithNoAnnotations.GenericBaseWithDerivedWithout")]
            public override void GenericBaseWithDerivedWithout<T>() { }

            [LogDoesNotContain("DerivedWithNoAnnotations.GenericBaseWithoutDerivedWithout")]
            public override void GenericBaseWithoutDerivedWithout<T>() { }


            // === RequiresUnreferencedCode ===
            [LogDoesNotContain("DerivedWithNoAnnotations.RequiresUnreferencedCodeBaseWithDerivedWith_")]
            [RequiresUnreferencedCode("")]
            public override void RequiresUnreferencedCodeBaseWithDerivedWith_() { }
            // Removing [RUC] is now allowed (variance - weakening precondition)
            [LogDoesNotContain("DerivedWithNoAnnotations.RequiresUnreferencedCodeBaseWithDerivedWithout")]
            public override void RequiresUnreferencedCodeBaseWithDerivedWithout() { }
        }


        interface IBase
        {
            // === Return values ===
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type ReturnValueInterfaceBaseWithImplementationWithout();

            Type ReturnValueInterfaceBaseWithoutImplementationWith_();

            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type ReturnValueInterfaceBaseWithImplementationWith_();


            // === Method parameters ===
            void SingleParameterBaseWithImplementationWith_(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                Type p);

            void SingleParameterBaseWithoutImplementationWith_(Type p);

            void SingleParameterBaseWithImplementationWithout(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                Type p);

            void SingleParameterBaseWithoutImplementationWithout(Type p);


            // === Generic methods ===
            void GenericInterfaceBaseWithoutImplementationWith_<T>();

            void GenericInterfaceBaseWithImplementationWithout<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>();

            // === Properties ===
            Type PropertyInterfaceBaseWithoutImplementationWith { get; set; }


            // === RequiresUnreferencedCode ===
            [RequiresUnreferencedCode("")]
            void RequiresUnreferencedCodeInterfaceBaseWithImplementationWith_();
            void RequiresUnreferencedCodeInterfaceBaseWithoutImplementationWith_();
        }

        interface IDerived : IBase
        {
            // === Return values ===
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type ReturnTypeInterfaceDerivedWithImplementationWithout();


            // === Method parameters ===
        }

        abstract class ImplementationClass : IDerived
        {
            // === Return values ===
            // Removing DAMT from return value is NOT allowed (covariant - weakening postcondition)
            [ExpectedWarning("IL2093",
                "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.ImplementationClass.ReturnValueInterfaceBaseWithImplementationWithout()",
                "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.IBase.ReturnValueInterfaceBaseWithImplementationWithout()")]
            public Type ReturnValueInterfaceBaseWithImplementationWithout() => null;

            // Adding DAMT to return value is now allowed (covariant - strengthening postcondition)
            [LogDoesNotContain("ImplementationClass.ReturnValueInterfaceBaseWithoutImplementationWith_")]
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            public Type ReturnValueInterfaceBaseWithoutImplementationWith_() => null;

            [LogDoesNotContain("ImplementationClass.ReturnValueInterfaceBaseWithImplementationWith_")]
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            public Type ReturnValueInterfaceBaseWithImplementationWith_() => null;

            // Removing DAMT from return value is NOT allowed
            [ExpectedWarning("IL2093",
                "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.ImplementationClass.ReturnTypeInterfaceDerivedWithImplementationWithout()",
                "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.IDerived.ReturnTypeInterfaceDerivedWithImplementationWithout()")]
            public Type ReturnTypeInterfaceDerivedWithImplementationWithout() => null;


            // === Method parameters ===
            [LogDoesNotContain("ImplementationClass.SingleParameterBaseWithImplementationWith_")]
            public void SingleParameterBaseWithImplementationWith_(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                Type p)
            { }

            // Removing DAMT from parameter is now allowed (contravariant - weakening precondition)
            [LogDoesNotContain("ImplementationClass.SingleParameterBaseWithImplementationWithout")]
            public void SingleParameterBaseWithImplementationWithout(Type p) { }

            // Adding DAMT to parameter is NOT allowed (contravariant - strengthening precondition)
            [ExpectedWarning("IL2092",
                "p", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.ImplementationClass.SingleParameterBaseWithoutImplementationWith_(Type)",
                "p", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.IBase.SingleParameterBaseWithoutImplementationWith_(Type)")]
            public void SingleParameterBaseWithoutImplementationWith_(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                Type p)
            { }

            [LogDoesNotContain("ImplementationClass.SingleParameterBaseWithoutImplementationWithout")]
            public void SingleParameterBaseWithoutImplementationWithout(Type p) { }


            // === Generic methods ===
            // Adding DAMT to generic parameter is NOT allowed (contravariant - strengthening precondition)
            [ExpectedWarning("IL2095",
                "T", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.ImplementationClass.GenericInterfaceBaseWithoutImplementationWith_<T>()",
                "T", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.IBase.GenericInterfaceBaseWithoutImplementationWith_<T>()")]
            public void GenericInterfaceBaseWithoutImplementationWith_<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() { }

            // Removing DAMT from generic parameter is now allowed (contravariant - weakening precondition)
            [LogDoesNotContain("ImplementationClass.GenericInterfaceBaseWithImplementationWithout")]
            public void GenericInterfaceBaseWithImplementationWithout<T>() { }

            // === Properties ===
            // Property on return affects getter (covariant) and setter parameter (contravariant)
            // Adding DAMT to property that has none: getter is OK (covariant), setter adds to param which is NOT OK (contravariant)
            [ExpectedWarning("IL2092",
                "value", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.ImplementationClass.PropertyInterfaceBaseWithoutImplementationWith.set",
                "value", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.IBase.PropertyInterfaceBaseWithoutImplementationWith.set")]
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            public Type PropertyInterfaceBaseWithoutImplementationWith { get; set; }


            // === RequiresUnreferencedCode ===
            [LogDoesNotContain("ImplementationClass.RequiresUnreferencedCodeInterfaceBaseWithImplementationWith_")]
            [RequiresUnreferencedCode("")]
            public void RequiresUnreferencedCodeInterfaceBaseWithImplementationWith_() { }
            // Adding [RUC] is NOT allowed (variance - strengthening precondition)
            [ExpectedWarning("IL2046", "ImplementationClass.RequiresUnreferencedCodeInterfaceBaseWithoutImplementationWith_")]
            [RequiresUnreferencedCode("")]
            public void RequiresUnreferencedCodeInterfaceBaseWithoutImplementationWith_() { }
        }

        interface IBaseImplementedInterface
        {
            Type ReturnValueBaseWithInterfaceWithout();

            [RequiresUnreferencedCode("")]
            void RequiresUnreferencedCodeBaseWithoutInterfaceWith();
        }

        class BaseImplementsInterfaceViaDerived
        {
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            public virtual Type ReturnValueBaseWithInterfaceWithout() => null;

            public virtual void RequiresUnreferencedCodeBaseWithoutInterfaceWith() { }
        }

        // Interface has no DAMT on return, but class adds DAMT - now allowed (covariant)
        // Interface has [RUC], but class removes it - now allowed (variance)
        class DerivedWithInterfaceImplementedByBase : BaseImplementsInterfaceViaDerived, IBaseImplementedInterface
        {
        }


        interface ITwoInterfacesImplementedByOneMethod_One
        {
            Type ReturnValueInterfaceWithoutImplementationWith();
        }

        interface ITwoInterfacesImplementedByOneMethod_Two
        {
            Type ReturnValueInterfaceWithoutImplementationWith();
        }

        class ImplementationOfTwoInterfacesWithOneMethod : ITwoInterfacesImplementedByOneMethod_One, ITwoInterfacesImplementedByOneMethod_Two
        {
            // Adding DAMT to return value is now allowed (covariant - strengthening postcondition)
            [LogDoesNotContain("ITwoInterfacesImplementedByOneMethod_One.ReturnValueInterfaceWithoutImplementationWith")]
            [LogDoesNotContain("ITwoInterfacesImplementedByOneMethod_Two.ReturnValueInterfaceWithoutImplementationWith")]
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public virtual Type ReturnValueInterfaceWithoutImplementationWith() => null;
        }

        static class StaticInterfaceMethods
        {
            interface IDamOnAll
            {
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                static abstract Type AbstractMethod
                    <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
                T>(
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                    Type type);

                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                static virtual Type VirtualMethod
                    <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
                T>(
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                    Type type)
                { return null; }
            }

            class ImplIDamOnAllMissing : IDamOnAll
            {
                // NativeAOT doesn't validate overrides when accessed through reflection because it's a direct call (non-virtual)
                // So it doesn't matter that the annotations are not in-sync since the access will validate
                // the annotations on the implementation method - it doesn't even see the base method in this case.
                // With variance: removing from return is NOT allowed, but removing from params/generics IS allowed
                [ExpectedWarning("IL2093", Tool.Trimmer | Tool.Analyzer, "")]
                public static Type AbstractMethod<T>(Type type) => null;

                // NativeAOT doesn't validate overrides when accessed through reflection because it's a direct call (non-virtual)
                [ExpectedWarning("IL2093", Tool.Trimmer | Tool.Analyzer, "")]
                public static Type VirtualMethod<T>(Type type) => null;
            }

            class ImplIDamOnAllMismatch : IDamOnAll
            {
                // NativeAOT doesn't validate overrides when accessed through reflection because it's a direct call (non-virtual)
                [ExpectedWarning("IL2092", Tool.Trimmer | Tool.Analyzer, "")]
                [ExpectedWarning("IL2093", Tool.Trimmer | Tool.Analyzer, "")]
                [ExpectedWarning("IL2095", Tool.Trimmer | Tool.Analyzer, "")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                public static Type AbstractMethod
                    <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                T>(
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
                    Type type)
                { return null; }

                // NativeAOT doesn't validate overrides when accessed through reflection because it's a direct call (non-virtual)
                [ExpectedWarning("IL2092", Tool.Trimmer | Tool.Analyzer, "")]
                [ExpectedWarning("IL2093", Tool.Trimmer | Tool.Analyzer, "")]
                [ExpectedWarning("IL2095", Tool.Trimmer | Tool.Analyzer, "")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                public static Type VirtualMethod
                    <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                T>(
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    Type type)
                { return null; }
            }

            class ImplIDamOnAllMatch : IDamOnAll
            {
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public static Type AbstractMethod
                                <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
                T>(
                                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                    Type type)
                { return null; }

                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public static Type VirtualMethod
                    <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
                T>(
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                    Type type)
                { return null; }
            }

            interface IDamOnNone
            {
                static virtual Type VirtualMethod<T>(Type t) { return null; }
                static abstract Type AbstractMethod<T>(Type t);
            }

            class ImplIDamOnNoneMatch : IDamOnNone
            {
                public static Type VirtualMethod<T>(Type t) { return null; }
                public static Type AbstractMethod<T>(Type t) { return null; }
            }

            class ImplIDamOnNoneMismatch : IDamOnNone
            {
                // NativeAOT doesn't validate overrides when accessed through reflection because it's a direct call (non-virtual)
                // With variance: adding to return is OK (covariant), adding to params/generics is NOT (contravariant)
                [ExpectedWarning("IL2092", Tool.Trimmer | Tool.Analyzer, "")]
                [ExpectedWarning("IL2095", Tool.Trimmer | Tool.Analyzer, "")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                public static Type AbstractMethod
                    <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                T>(
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
                    Type type)
                { return null; }

                // NativeAOT doesn't validate overrides when accessed through reflection because it's a direct call (non-virtual)
                [ExpectedWarning("IL2092", Tool.Trimmer | Tool.Analyzer, "")]
                [ExpectedWarning("IL2095", Tool.Trimmer | Tool.Analyzer, "")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                public static Type VirtualMethod
                    <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                T>(
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    Type type)
                { return null; }
            }


            public static void Test()
            {
                typeof(ImplIDamOnAllMatch).RequiresPublicMethods();
                typeof(ImplIDamOnAllMismatch).RequiresPublicMethods();
                typeof(ImplIDamOnAllMissing).RequiresPublicMethods();
                typeof(IDamOnAll).RequiresPublicMethods();
                typeof(ImplIDamOnNoneMatch).RequiresPublicMethods();
                typeof(ImplIDamOnNoneMismatch).RequiresPublicMethods();
                typeof(IDamOnNone).RequiresPublicMethods();

            }
        }

        class BaseInPreservedScope
        {
            class ImplIAnnotatedMethodsMismatch : Library.IAnnotatedMethods
            {
                // NativeAOT doesn't always validate static overrides when accessed through reflection because it's a direct call (non-virtual)
                [ExpectedWarning("IL2095", Tool.Trimmer | Tool.Analyzer, "")]
                public static void GenericWithMethodsStatic<T>() { }

                [ExpectedWarning("IL2092")]
                public static void ParamWithMethodsStatic(Type t) { }

                [ExpectedWarning("IL2093")]
                public static Type ReturnWithMethodsStatic() => typeof(int);

                [ExpectedWarning("IL2095")]
                public void GenericWithMethods<T>() { }

                [ExpectedWarning("IL2092")]
                public void ParamWithMethods(Type t) { }

                [ExpectedWarning("IL2093")]
                public Type ReturnWithMethods() => typeof(int);
            }

            class ImplIUnannotatedMethodsMismatch : Library.IUnannotatedMethods
            {
                // NativeAOT doesn't always validate static overrides when accessed through reflection because it's a direct call (non-virtual)
                [ExpectedWarning("IL2095", Tool.Trimmer | Tool.Analyzer, "")]
                public static void GenericStatic<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>() { }

                [ExpectedWarning("IL2092")]
                public static void ParamStatic([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

                [ExpectedWarning("IL2093")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public static Type ReturnStatic() => typeof(int);

                [ExpectedWarning("IL2095")]
                public void Generic<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>() { }

                [ExpectedWarning("IL2092")]
                public void Param([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

                [ExpectedWarning("IL2093")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public Type Return() => typeof(int);
            }

            class DerivedFromUnannotatedMismatch : Library.UnannotatedMethods
            {
                [ExpectedWarning("IL2095")]
                public override void Generic<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>() { }

                [ExpectedWarning("IL2092")]
                public override void Param([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

                [ExpectedWarning("IL2093")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public override Type Return() => typeof(int);
            }

            class DerivedFromAnnotatedMismatch : Library.AnnotatedMethods
            {
                [ExpectedWarning("IL2095")]
                public override void GenericWithMethods<T>() { }

                [ExpectedWarning("IL2092")]
                public override void ParamWithMethods(Type t) { }

                [ExpectedWarning("IL2093")]
                public override Type ReturnWithMethods() => typeof(int);
            }

            class ImplIUnannotatedMethodsMatch : Library.IUnannotatedMethods
            {
                public static void GenericStatic<T>() { }

                public static void ParamStatic(Type t) { }

                public static Type ReturnStatic() => typeof(int);

                public void Generic<T>() { }

                public void Param(Type t) { }

                public Type Return() => typeof(int);
            }

            class ImplIAnnotatedMethodsMatch : Library.IAnnotatedMethods
            {
                public static void GenericWithMethodsStatic<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>() { }

                public static void ParamWithMethodsStatic([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public static Type ReturnWithMethodsStatic() => typeof(int);

                public void GenericWithMethods<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>() { }

                public void ParamWithMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public Type ReturnWithMethods() => typeof(int);
            }

            class DerivedFromAnnotatedMatch : Library.AnnotatedMethods
            {
                public override void GenericWithMethods<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>() { }

                public override void ParamWithMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public override Type ReturnWithMethods() => typeof(int);
            }

            class DerivedFromUnannotatedMatch : Library.UnannotatedMethods
            {
                public override void Generic<T>() { }

                public override void Param(Type t) { }

                public override Type Return() => typeof(int);
            }

            public static void Test()
            {
                // https://github.com/dotnet/linker/issues/3133
                // Access the interfaces as well - otherwise NativeAOT can decide
                // to not look for overrides since it knows it's making a direct access
                // to a method and it doesn't need to know about the base method
                // which leads to some warnings not being generated.
                // The goal of this test is to validate the generated diagnostics
                // so we're forcing the checks to happen with this.
                typeof(Library.IAnnotatedMethods).RequiresAll();
                typeof(Library.IUnannotatedMethods).RequiresAll();

                typeof(ImplIUnannotatedMethodsMismatch).RequiresPublicMethods();
                typeof(ImplIAnnotatedMethodsMismatch).RequiresPublicMethods();
                typeof(DerivedFromAnnotatedMismatch).RequiresPublicMethods();
                typeof(DerivedFromUnannotatedMismatch).RequiresPublicMethods();
                typeof(ImplIUnannotatedMethodsMatch).RequiresPublicMethods();
                typeof(ImplIAnnotatedMethodsMatch).RequiresPublicMethods();
                typeof(DerivedFromAnnotatedMatch).RequiresPublicMethods();
                typeof(DerivedFromUnannotatedMatch).RequiresPublicMethods();
            }
        }

        // This is mostly for Native AOT - in that compiler it matters how a method
        // is referenced as it will take a different code path to do some of these validations
        // The above tests all rely on reflection marking so this test also uses direct calls
        class DirectCall
        {
            abstract class Base
            {
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public abstract Type NonGenericAbstract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type);

                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public virtual Type NonGenericVirtual([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type) => type;

                public abstract void GenericAbstract<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>();

                public virtual void GenericVirtual<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>() { }

                public abstract Type UnannotatedAbstract(Type type);

                public abstract void UnannotatedGenericAbstract<T>();
            }

            class Derived : Base
            {
                [ExpectedWarning("IL2092")]
                [ExpectedWarning("IL2093")]
                public override Type NonGenericAbstract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type) => null;

                [ExpectedWarning("IL2092")]
                [ExpectedWarning("IL2093")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
                public override Type NonGenericVirtual(Type type) => null;

                [ExpectedWarning("IL2095")]
                public override void GenericAbstract<T>() { }

                [ExpectedWarning("IL2095")]
                public override void GenericVirtual<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() { }

                [ExpectedWarning("IL2092")]
                [ExpectedWarning("IL2093")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public override Type UnannotatedAbstract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type) => null;

                [ExpectedWarning("IL2095")]
                public override void UnannotatedGenericAbstract<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() { }
            }

            interface IBaseWithDefault
            {
                void DefaultMethod(Type type);
            }

            interface IDerivedWithDefault : IBaseWithDefault
            {
                [ExpectedWarning("IL2092")]
                [UnexpectedWarning("IL2092", Tool.Analyzer, "https://github.com/dotnet/linker/issues/3121")]
                void IBaseWithDefault.DefaultMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type) { }
            }

            class ImplDerivedWithDefault : IDerivedWithDefault
            {
            }

            interface IGvmBase
            {
                Type UnannotatedGvm<T>(Type type);
                Type UnannotatedGvmCalledThroughBase<T>(Type type);

                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                static abstract Type AnnotatedStaticGvm<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type);

                static virtual Type UnannotatedStaticGvm<T>(Type type) => null;
            }

            class ImplIGvmBase : IGvmBase
            {
                // NativeAOT doesn't validate overrides when it can resolve them as direct calls
                [ExpectedWarning("IL2092", Tool.Trimmer | Tool.Analyzer, "")]
                [ExpectedWarning("IL2093", Tool.Trimmer | Tool.Analyzer, "")]
                [ExpectedWarning("IL2095", Tool.Trimmer | Tool.Analyzer, "")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public Type UnannotatedGvm<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type) => null;

                [ExpectedWarning("IL2092")]
                [ExpectedWarning("IL2093")]
                [ExpectedWarning("IL2095")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public Type UnannotatedGvmCalledThroughBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type) => null;

                [ExpectedWarning("IL2092")]
                [ExpectedWarning("IL2093")]
                [ExpectedWarning("IL2095")]
                public static Type AnnotatedStaticGvm<T>(Type type) => null;

                [ExpectedWarning("IL2092")]
                [ExpectedWarning("IL2093")]
                [ExpectedWarning("IL2095")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public static Type UnannotatedStaticGvm<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type) => null;
            }

            static void CallStaticGvm<TGvmBase>() where TGvmBase : IGvmBase
            {
                TGvmBase.AnnotatedStaticGvm<string>(typeof(string));
                TGvmBase.UnannotatedStaticGvm<string>(typeof(string));
            }

            public static void Test()
            {
                Base instance = new Derived();
                instance.NonGenericAbstract(typeof(string));
                instance.NonGenericVirtual(typeof(string));
                instance.GenericAbstract<string>();
                instance.GenericVirtual<string>();
                instance.UnannotatedAbstract(typeof(string));
                instance.UnannotatedGenericAbstract<string>();

                ((IBaseWithDefault)(new ImplDerivedWithDefault())).DefaultMethod(typeof(string));

                ImplIGvmBase impl = new ImplIGvmBase();
                impl.UnannotatedGvm<string>(typeof(string));

                IGvmBase ibase = (IGvmBase)impl;
                ibase.UnannotatedGvmCalledThroughBase<string>(typeof(string));

                CallStaticGvm<ImplIGvmBase>();
            }
        }

        class RequiresAndDynamicallyAccessedMembersValidation
        {
            // These tests have both DynamicallyAccessedMembers annotations and Requires annotations.
            // This is to reproduce a bug where the virtual method annotations would be validated due to
            // the presence of DynamicallyAccessedMembers, but the logic for checking Requires annotations
            // was incorrect. The bug didn't manifest with just Requires annotations because the methods wouldn't
            // be validated at all for Requires on type.

            class BaseMethodWithRequires
            {
                [RequiresUnreferencedCode(nameof(MethodWithRequires))]
                [RequiresDynamicCode(nameof(MethodWithRequires))]
                public virtual void MethodWithRequires([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) { }
            }

            [RequiresUnreferencedCode(nameof(DerivedTypeWithRequires_BaseMethodWithRequires))]
            [RequiresDynamicCode(nameof(DerivedTypeWithRequires_BaseMethodWithRequires))]
            class DerivedTypeWithRequires_BaseMethodWithRequires : BaseMethodWithRequires
            {
                public override void MethodWithRequires([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) { }
            }

            [ExpectedWarning("IL2026", nameof(DerivedTypeWithRequires_BaseMethodWithRequires))]
            [ExpectedWarning("IL2026", nameof(DerivedTypeWithRequires_BaseMethodWithRequires.MethodWithRequires))]
            [ExpectedWarning("IL3050", nameof(DerivedTypeWithRequires_BaseMethodWithRequires), Tool.Analyzer | Tool.NativeAot, "NativeAOT-specific warning")]
            [ExpectedWarning("IL3050", nameof(DerivedTypeWithRequires_BaseMethodWithRequires.MethodWithRequires), Tool.Analyzer | Tool.NativeAot, "NativeAOT-specific warning")]
            static void Test_DerivedTypeWithRequires_BaseMethodWithRequires()
            {
                new DerivedTypeWithRequires_BaseMethodWithRequires().MethodWithRequires(typeof(int));
            }

            class BaseMethodWithoutRequires
            {
                public virtual void MethodWithoutRequires([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) { }
            }

            [RequiresUnreferencedCode(nameof(DerivedTypeWithRequires_BaseMethodWithoutRequires))]
            class DerivedTypeWithRequires_BaseMethodWithoutRequires : BaseMethodWithoutRequires
            {
                public override void MethodWithoutRequires([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) { }
            }

            [ExpectedWarning("IL2026", nameof(DerivedTypeWithRequires_BaseMethodWithoutRequires))]
            static void Test_DerivedTypeWithRequires_BaseMethodWithoutRequires()
            {
                new DerivedTypeWithRequires_BaseMethodWithoutRequires().MethodWithoutRequires(typeof(int));
            }

            public static void Test()
            {
                Test_DerivedTypeWithRequires_BaseMethodWithRequires();
                Test_DerivedTypeWithRequires_BaseMethodWithoutRequires();
            }
        }

        class InstantiatedGeneric
        {
            class GenericBase<T>
            {
                [ExpectedWarning("IL2106")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public virtual T ReturnValue() => default;
            }

            class InstantiatedDerived : GenericBase<Type>
            {
                public override Type ReturnValue() => null;
            }

            public static void Test()
            {
                new InstantiatedDerived().ReturnValue();
            }
        }

        class AnnotationOnUnsupportedType
        {
            class UnsupportedType
            {
                [ExpectedWarning("IL2041")]
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                public virtual void UnsupportedAnnotationMismatch() { }
            }

            class DerivedUnsupportedType : UnsupportedType
            {
                [ExpectedWarning("IL2041")]
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                public override void UnsupportedAnnotationMismatch() { }
            }

            public static void Test()
            {
                new DerivedUnsupportedType().UnsupportedAnnotationMismatch();
            }
        }
    }
}

namespace System
{
    // This verifies correct validation of the "this" parameter annotations
    class VirtualMethodHierarchyDataflowAnnotationValidationTypeTestBase : TestSystemTypeBase
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public virtual void ThisBaseWithDerivedWithout() { }
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public virtual void ThisBaseWithDerivedWith_() { }
        public virtual void ThisBaseWithoutDerivedWith() { }
    }

    class VirtualMethodHierarchyDataflowAnnotationValidationTypeTestDerived : VirtualMethodHierarchyDataflowAnnotationValidationTypeTestBase
    {
        [ExpectedWarning("IL2094",
            "System.VirtualMethodHierarchyDataflowAnnotationValidationTypeTestDerived.ThisBaseWithDerivedWithout()",
            "System.VirtualMethodHierarchyDataflowAnnotationValidationTypeTestBase.ThisBaseWithDerivedWithout()")]
        public override void ThisBaseWithDerivedWithout() { }

        [LogDoesNotContain("VirtualMethodHierarchyDataflowAnnotationValidationTypeTestDerived.ThisBaseWithDerivedWith_")]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override void ThisBaseWithDerivedWith_() { }

        [LogContains("VirtualMethodHierarchyDataflowAnnotationValidationTypeTestDerived.ThisBaseWithoutDerivedWith")]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override void ThisBaseWithoutDerivedWith() { }
    }
}
