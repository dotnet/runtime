// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.DataFlow.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SandboxDependency ("Dependencies/TestSystemTypeBase.cs")]
	[SetupCompileBefore ("skiplibrary.dll", new[] { "Dependencies/Library.cs" })]
	[SetupLinkerAction ("skip", "skiplibrary")]

	// Suppress warnings about accessing methods with annotations via reflection - the test below does that a LOT
	// (The test accessed these methods through DynamicallyAccessedMembers annotations which is effectively the same reflection access)
	[UnconditionalSuppressMessage ("test", "IL2111")]

	[ExpectedNoWarnings]
	class VirtualMethodHierarchyDataflowAnnotationValidation
	{
		// The code below marks methods which have RUC on them, it's not the point of this test to validate these here
		[UnconditionalSuppressMessage ("test", "IL2026")]
		public static void Main ()
		{
			// The test uses data flow annotation to mark all public methods on the specified types
			// which in turn will trigger querying the annotations on those methods and thus the validation.

			RequirePublicMethods (typeof (BaseClass));
			RequirePublicMethods (typeof (DerivedClass));
			RequirePublicMethods (typeof (SuperDerivedClass));
			RequirePublicMethods (typeof (DerivedOverNoAnnotations));
			RequirePublicMethods (typeof (DerivedWithNoAnnotations));
			RequirePublicMethods (typeof (IBase));
			RequirePublicMethods (typeof (IDerived));
			RequirePublicMethods (typeof (ImplementationClass));
			RequirePublicMethods (typeof (IBaseImplementedInterface));
			RequirePublicMethods (typeof (BaseImplementsInterfaceViaDerived));
			RequirePublicMethods (typeof (DerivedWithInterfaceImplementedByBase));
			RequirePublicMethods (typeof (VirtualMethodHierarchyDataflowAnnotationValidationTypeTestBase));
			RequirePublicMethods (typeof (VirtualMethodHierarchyDataflowAnnotationValidationTypeTestDerived));
			RequirePublicMethods (typeof (ITwoInterfacesImplementedByOneMethod_One));
			RequirePublicMethods (typeof (ITwoInterfacesImplementedByOneMethod_Two));
			RequirePublicMethods (typeof (ImplementationOfTwoInterfacesWithOneMethod));
			StaticInterfaceMethods.Test ();
			BaseInPreservedScope.Test ();
			DirectCall.Test ();
		}

		static void RequirePublicMethods ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
		}

		class BaseClass
		{
			// === Return values ===
			// Other than the basics, the return value also checks all of the inheritance cases - we omit those for the other tests
			public virtual Type ReturnValueBaseWithoutDerivedWithout () => null;
			public virtual Type ReturnValueBaseWithoutDerivedWith () => null;
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public virtual Type ReturnValueBaseWithDerivedWithout () => null;
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public virtual Type ReturnValueBaseWithDerivedWith () => null;

			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public virtual Type ReturnValueBaseWithSuperDerivedWithout () => null;

			// === Method parameters ===
			// This does not check complicated inheritance cases as that is already validated by the return values
			public virtual void SingleParameterBaseWithDerivedWithout (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type p)
			{ }

			public virtual void SingleParameterBaseWithDerivedWith_ (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type p)
			{ }

			public virtual void SingleParameterBaseWithoutDerivedWith_ (Type p) { }

			public virtual void SingleParameterBaseWithoutDerivedWithout (Type p) { }

			public virtual void SingleParameterBaseWithDerivedWithDifferent (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type p)
			{ }

			public virtual void MultipleParametersBaseWithDerivedWithout (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type p1BaseWithDerivedWithout,
				Type p2BaseWithoutDerivedWithout,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type p3BaseWithDerivedWithout)
			{ }

			public virtual void MultipleParametersBaseWithoutDerivedWith (
				Type p1BaseWithoutDerivedWith,
				Type p2BaseWithoutDerivedWithout,
				Type p3BaseWithoutDerivedWith)
			{ }

			public virtual void MultipleParametersBaseWithDerivedWithMatch (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type p1BaseWithDerivedWith,
				Type p2BaseWithoutDerivedWithout,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				Type p3BaseWithDerivedWith)
			{ }

			public virtual void MultipleParametersBaseWithDerivedWithMismatch (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type p1BaseWithDerivedWithMismatch,
				Type p2BaseWithoutDerivedWith,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				Type p3BaseWithDerivedWithMatch,
				Type p4NoAnnotations)
			{ }

			// === Generic methods ===
			public virtual void GenericBaseWithDerivedWithout<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] T> () { }
			public virtual void GenericBaseWithoutDerivedWith<T> () { }
			public virtual void GenericBaseWithDerivedWith_<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] T> () { }
			public virtual void GenericBaseWithoutDerivedWithout<T> () { }

			// === Properties ===
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public virtual Type PropertyBaseWithDerivedWithout { get; }
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public virtual Type PropertyBaseWithDerivedWith_ { get; set; }
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public virtual Type PropertyBaseWithDerivedOnGetterWith { get; }

			// === RequiresUnreferencedCode ===
			[RequiresUnreferencedCode ("")]
			public virtual void RequiresUnreferencedCodeBaseWithDerivedWithout () { }
			public virtual void RequiresUnreferencedCodeBaseWithoutDerivedWith_ () { }
			[RequiresUnreferencedCode ("")]
			public virtual void RequiresUnreferencedCodeBaseWithDerivedWith_ () { }
			public virtual void RequiresUnreferencedCodeBaseWithoutDerivedWithout () { }

			public virtual void RequiresUnreferencedCodeBaseWithoutSuperDerivedWith_ () { }
		}

		class DerivedClass : BaseClass
		{
			// === Return values ===
			[LogDoesNotContain ("DerivedClass.ReturnValueBaseWithoutDerivedWithout")]
			public override Type ReturnValueBaseWithoutDerivedWithout () => null;

			[ExpectedWarning ("IL2093", "BaseClass.ReturnValueBaseWithoutDerivedWith", "DerivedClass.ReturnValueBaseWithoutDerivedWith")]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public override Type ReturnValueBaseWithoutDerivedWith () => null;

			[LogContains ("DerivedClass.ReturnValueBaseWithDerivedWithout")]
			public override Type ReturnValueBaseWithDerivedWithout () => null;

			[LogDoesNotContain ("DerivedClass.ReturnValueBaseWithDerivedWitht")]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public override Type ReturnValueBaseWithDerivedWith () => null;


			// === Method parameters ===
			[ExpectedWarning ("IL2092",
				"p", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedClass.SingleParameterBaseWithDerivedWithout(Type)",
				"p", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseClass.SingleParameterBaseWithDerivedWithout(Type)")]
			public override void SingleParameterBaseWithDerivedWithout (Type p) { }

			[LogDoesNotContain ("DerivedClass.SingleParameterBaseWithDerivedWith_")]
			public override void SingleParameterBaseWithDerivedWith_ (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type p)
			{ }

			[LogContains (
				"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 'p' of method 'Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedClass.SingleParameterBaseWithoutDerivedWith_(Type)' " +
				"don't match overridden parameter 'p' of method 'Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseClass.SingleParameterBaseWithoutDerivedWith_(Type)'. " +
				"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.")]
			public override void SingleParameterBaseWithoutDerivedWith_ (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type p)
			{ }

			[LogDoesNotContain ("DerivedClass.SingleParameterBaseWithoutDerivedWithout")]
			public override void SingleParameterBaseWithoutDerivedWithout (Type p) { }

			[LogContains (
				"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 'p' of method 'Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedClass.SingleParameterBaseWithDerivedWithDifferent(Type)' " +
				"don't match overridden parameter 'p' of method 'Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseClass.SingleParameterBaseWithDerivedWithDifferent(Type)'. " +
				"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.")]
			public override void SingleParameterBaseWithDerivedWithDifferent (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				Type p)
			{ }


			[LogContains (".*'p1BaseWithDerivedWithout'.*DerivedClass.*MultipleParametersBaseWithDerivedWithout.*", regexMatch: true)]
			[LogDoesNotContain (".*'p2BaseWithoutDerivedWithout'.*DerivedClass.*MultipleParametersBaseWithDerivedWithout.*", regexMatch: true)]
			[LogContains (".*'p3BaseWithDerivedWithout'.*DerivedClass.*MultipleParametersBaseWithDerivedWithout.*", regexMatch: true)]
			public override void MultipleParametersBaseWithDerivedWithout (
				Type p1BaseWithDerivedWithout,
				Type p2BaseWithoutDerivedWithout,
				Type p3BaseWithDerivedWithout)
			{ }

			[LogContains (".*'p1BaseWithoutDerivedWith'.*DerivedClass.*MultipleParametersBaseWithoutDerivedWith.*", regexMatch: true)]
			[LogDoesNotContain (".*'p2BaseWithoutDerivedWithout'.*DerivedClass.*MultipleParametersBaseWithoutDerivedWith.*", regexMatch: true)]
			[LogContains (".*'p3BaseWithoutDerivedWith'.*DerivedClass.*MultipleParametersBaseWithoutDerivedWith.*", regexMatch: true)]
			public override void MultipleParametersBaseWithoutDerivedWith (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				Type p1BaseWithoutDerivedWith,
				Type p2BaseWithoutDerivedWithout,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				Type p3BaseWithoutDerivedWith)
			{ }

			[LogDoesNotContain ("DerivedClass.MultipleParametersBaseWithDerivedWithMatch")]
			public override void MultipleParametersBaseWithDerivedWithMatch (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type p1BaseWithDerivedWith,
				Type p2BaseWithoutDerivedWithout,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				Type p3BaseWithDerivedWith)
			{ }

			[LogContains (".*'p1BaseWithDerivedWithMismatch'.*DerivedClass.*MultipleParametersBaseWithDerivedWithMismatch.*", regexMatch: true)]
			[LogContains (".*'p2BaseWithoutDerivedWith'.*DerivedClass.*MultipleParametersBaseWithDerivedWithMismatch.*", regexMatch: true)]
			[LogDoesNotContain (".*'p3BaseWithDerivedWithMatch'.*DerivedClass.*MultipleParametersBaseWithDerivedWithMismatch.*", regexMatch: true)]
			[LogDoesNotContain (".*'p4NoAnnotations'.*DerivedClass.*MultipleParametersBaseWithDerivedWithMismatch.*", regexMatch: true)]
			public override void MultipleParametersBaseWithDerivedWithMismatch (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				Type p1BaseWithDerivedWithMismatch,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				Type p2BaseWithoutDerivedWith,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				Type p3BaseWithDerivedWithMatch,
				Type p4NoAnnotations)
			{ }

			// === Generic methods ===
			[ExpectedWarning ("IL2095",
				"T", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedClass.GenericBaseWithDerivedWithout<T>()",
				"T", "Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseClass.GenericBaseWithDerivedWithout<T>()")]
			public override void GenericBaseWithDerivedWithout<T> () { }

			[LogContains (
				"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the generic parameter 'T' of 'Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedClass.GenericBaseWithoutDerivedWith<T>()' " +
				"don't match overridden generic parameter 'T' of 'Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseClass.GenericBaseWithoutDerivedWith<T>()'. " +
				"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.")]
			public override void GenericBaseWithoutDerivedWith<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] T> () { }

			[LogDoesNotContain ("DerivedClass.GenericBaseWithDerivedWith_")]
			public override void GenericBaseWithDerivedWith_<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] T> () { }

			[LogDoesNotContain ("DerivedClass.GenericBaseWithoutDerivedWithout")]
			public override void GenericBaseWithoutDerivedWithout<T> () { }


			// === Properties ===
			// The warning is reported on the getter (or setter), which is not ideal, but it's probably good enough for now (we don't internally track annotations
			// on properties themselves, only on methods).
			[LogContains (
				"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the return value of method 'Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.DerivedClass.PropertyBaseWithDerivedWithout.get' " +
				"don't match overridden return value of method 'Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseClass.PropertyBaseWithDerivedWithout.get'. " +
				"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.")]
			public override Type PropertyBaseWithDerivedWithout { get; }

			[LogDoesNotContain ("DerivedClass.PropertyBaseWithDerivedWith_.get")]
			[LogDoesNotContain ("DerivedClass.PropertyBaseWithDerivedWith_.set")]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public override Type PropertyBaseWithDerivedWith_ { get; set; }

			[LogDoesNotContain ("PropertyBaseWithDerivedOnGetterWith")]
			[field: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public override Type PropertyBaseWithDerivedOnGetterWith { [return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] get; }


			// === RequiresUnreferencedCode ===
			[ExpectedWarning ("IL2046", "DerivedClass.RequiresUnreferencedCodeBaseWithDerivedWithout()",
				"BaseClass.RequiresUnreferencedCodeBaseWithDerivedWithout()",
				"'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides")]
			public override void RequiresUnreferencedCodeBaseWithDerivedWithout () { }
			[ExpectedWarning ("IL2046", "DerivedClass.RequiresUnreferencedCodeBaseWithoutDerivedWith_()",
				"BaseClass.RequiresUnreferencedCodeBaseWithoutDerivedWith_()",
				"'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides")]
			[RequiresUnreferencedCode ("")]
			public override void RequiresUnreferencedCodeBaseWithoutDerivedWith_ () { }
			[LogDoesNotContain ("DerivedClass.RequiresUnreferencedCodeBaseWithDerivedWith_")]
			[RequiresUnreferencedCode ("")]
			public override void RequiresUnreferencedCodeBaseWithDerivedWith_ () { }
			[LogDoesNotContain ("DerivedClass.RequiresUnreferencedCodeBaseWithoutDerivedWithout")]
			public override void RequiresUnreferencedCodeBaseWithoutDerivedWithout () { }
		}

		class InBetweenDerived : DerivedClass
		{
			// This is intentionally left empty to validate that the logic can skip over to deeper base classes correctly
		}

		class SuperDerivedClass : InBetweenDerived
		{
			// === Return values ===
			[LogContains (
				"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the return value of method 'Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.SuperDerivedClass.ReturnValueBaseWithSuperDerivedWithout()' " +
				"don't match overridden return value of method 'Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseClass.ReturnValueBaseWithSuperDerivedWithout()'. " +
				"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.")]
			public override Type ReturnValueBaseWithSuperDerivedWithout () => null;

			// === RequiresUnreferencedCode ===
			[LogContains ("SuperDerivedClass.RequiresUnreferencedCodeBaseWithoutSuperDerivedWith_")]
			[RequiresUnreferencedCode ("")]
			public override void RequiresUnreferencedCodeBaseWithoutSuperDerivedWith_ () { }
		}


		abstract class BaseWithNoAnnotations
		{
			// This class must not have ANY annotations anywhere on it.
			// It's here to test that the optimization works (as most classes won't have any annotations, so we shortcut that path).

			// === Return values ===
			public abstract Type ReturnValueBaseWithoutDerivedWith ();

			public abstract Type ReturnValueBaseWithoutDerivedWithout ();

			// === Method parameters ===
			public virtual void SingleParameterBaseWithoutDerivedWith_ (Type p) { }

			public virtual void SingleParameterBaseWithoutDerivedWithout (Type p) { }

			// === Generic methods ===
			public virtual void GenericBaseWithoutDerivedWith_<T> () { }

			public virtual void GenericBaseWithoutDerivedWithout<T> () { }

			// === RequiresUnreferencedCode ===
			public virtual void RequiresUnreferencedCodeBaseWithoutDerivedWith_ () { }
			public virtual void RequiresUnreferencedCodeBaseWithoutDerivedWithout () { }
		}

		class DerivedOverNoAnnotations : BaseWithNoAnnotations
		{
			// === Return values ===
			[LogContains ("DerivedOverNoAnnotations.ReturnValueBaseWithoutDerivedWith")]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public override Type ReturnValueBaseWithoutDerivedWith () => null;

			[LogDoesNotContain ("DerivedOverNoAnnotations.ReturnValueBaseWithoutDerivedWithout")]
			public override Type ReturnValueBaseWithoutDerivedWithout () => null;

			// === Method parameters ===
			[LogContains ("DerivedOverNoAnnotations.SingleParameterBaseWithoutDerivedWith_")]
			public override void SingleParameterBaseWithoutDerivedWith_ (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type p)
			{ }

			[LogDoesNotContain ("DerivedOverNoAnnotations.SingleParameterBaseWithoutDerivedWithout")]
			public override void SingleParameterBaseWithoutDerivedWithout (Type p) { }

			// === Generic methods ===
			[LogContains ("DerivedOverNoAnnotations.GenericBaseWithoutDerivedWith_")]
			public override void GenericBaseWithoutDerivedWith_<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] T> () { }

			[LogDoesNotContain ("DerivedOverNoAnnotations.GenericBaseWithoutDerivedWithout")]
			public override void GenericBaseWithoutDerivedWithout<T> () { }


			// === RequiresUnreferencedCode ===
			[LogContains ("DerivedOverNoAnnotations.RequiresUnreferencedCodeBaseWithoutDerivedWith_")]
			[RequiresUnreferencedCode ("")]
			public override void RequiresUnreferencedCodeBaseWithoutDerivedWith_ () { }
			[LogDoesNotContain ("DerivedOverNoAnnotations.RequiresUnreferencedCodeBaseWithoutDerivedWithout")]
			public override void RequiresUnreferencedCodeBaseWithoutDerivedWithout () { }
		}


		abstract class BaseWithAnnotations
		{
			// === Return values ===
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public abstract Type ReturnValueBaseWithDerivedWithout ();

			public abstract Type ReturnValueBaseWithoutDerivedWithout ();

			// === Method parameters ===
			public virtual void SingleParameterBaseWithDerivedWithout (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type p)
			{ }

			public virtual void SingleParameterBaseWithDerivedWith_ (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type p)
			{ }

			// === Generic methods ===
			public virtual void GenericBaseWithDerivedWithout<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] T> () { }

			public virtual void GenericBaseWithoutDerivedWithout<T> () { }


			// === RequiresUnreferencedCode ===
			[RequiresUnreferencedCode ("")]
			public virtual void RequiresUnreferencedCodeBaseWithDerivedWith_ () { }
			[RequiresUnreferencedCode ("")]
			public virtual void RequiresUnreferencedCodeBaseWithDerivedWithout () { }
		}

		class DerivedWithNoAnnotations : BaseWithAnnotations
		{
			// This class must not have ANY annotations anywhere on it.
			// It's here to test that the optimization works (as most classes won't have any annotations, so we shortcut that path).

			// === Return values ===
			[LogContains ("DerivedWithNoAnnotations.ReturnValueBaseWithDerivedWithout")]
			public override Type ReturnValueBaseWithDerivedWithout () => null;

			[LogDoesNotContain ("DerivedWithNoAnnotations.ReturnValueBaseWithoutDerivedWithout")]
			public override Type ReturnValueBaseWithoutDerivedWithout () => null;

			// === Method parameters ===
			[LogContains ("DerivedWithNoAnnotations.SingleParameterBaseWithDerivedWithout")]
			public override void SingleParameterBaseWithDerivedWithout (Type p) { }

			[LogDoesNotContain ("DerivedWithNoAnnotations.SingleParameterBaseWithDerivedWith_")]
			public override void SingleParameterBaseWithDerivedWith_ (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type p)
			{ }

			// === Generic methods ===
			[LogContains ("DerivedWithNoAnnotations.GenericBaseWithDerivedWithout")]
			public override void GenericBaseWithDerivedWithout<T> () { }

			[LogDoesNotContain ("DerivedWithNoAnnotations.GenericBaseWithoutDerivedWithout")]
			public override void GenericBaseWithoutDerivedWithout<T> () { }


			// === RequiresUnreferencedCode ===
			[LogDoesNotContain ("DerivedWithNoAnnotations.RequiresUnreferencedCodeBaseWithDerivedWith_")]
			[RequiresUnreferencedCode ("")]
			public override void RequiresUnreferencedCodeBaseWithDerivedWith_ () { }
			[LogContains ("DerivedWithNoAnnotations.RequiresUnreferencedCodeBaseWithDerivedWithout")]
			public override void RequiresUnreferencedCodeBaseWithDerivedWithout () { }
		}


		interface IBase
		{
			// === Return values ===
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type ReturnValueInterfaceBaseWithImplementationWithout ();

			Type ReturnValueInterfaceBaseWithoutImplementationWith_ ();

			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type ReturnValueInterfaceBaseWithImplementationWith_ ();


			// === Method parameters ===
			void SingleParameterBaseWithImplementationWith_ (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type p);

			void SingleParameterBaseWithoutImplementationWith_ (Type p);

			void SingleParameterBaseWithImplementationWithout (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type p);

			void SingleParameterBaseWithoutImplementationWithout (Type p);


			// === Generic methods ===
			void GenericInterfaceBaseWithoutImplementationWith_<T> ();

			void GenericInterfaceBaseWithImplementationWithout<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] T> ();

			// === Properties ===
			Type PropertyInterfaceBaseWithoutImplementationWith { get; set; }


			// === RequiresUnreferencedCode ===
			[RequiresUnreferencedCode ("")]
			void RequiresUnreferencedCodeInterfaceBaseWithImplementationWith_ ();
			void RequiresUnreferencedCodeInterfaceBaseWithoutImplementationWith_ ();
		}

		interface IDerived : IBase
		{
			// === Return values ===
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type ReturnTypeInterfaceDerivedWithImplementationWithout ();


			// === Method parameters ===
		}

		abstract class ImplementationClass : IDerived
		{
			// === Return values ===
			[LogContains ("ImplementationClass.ReturnValueInterfaceBaseWithImplementationWithout")]
			public Type ReturnValueInterfaceBaseWithImplementationWithout () => null;

			[LogContains ("ImplementationClass.ReturnValueInterfaceBaseWithoutImplementationWith_")]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public Type ReturnValueInterfaceBaseWithoutImplementationWith_ () => null;

			[LogDoesNotContain ("ImplementationClass.ReturnValueInterfaceBaseWithImplementationWith_")]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public Type ReturnValueInterfaceBaseWithImplementationWith_ () => null;

			[LogContains ("ImplementationClass.ReturnTypeInterfaceDerivedWithImplementationWithout")]
			public Type ReturnTypeInterfaceDerivedWithImplementationWithout () => null;


			// === Method parameters ===
			[LogDoesNotContain ("ImplementationClass.SingleParameterBaseWithImplementationWith_")]
			public void SingleParameterBaseWithImplementationWith_ (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type p)
			{ }

			[LogContains ("ImplementationClass.SingleParameterBaseWithImplementationWithout")]
			public void SingleParameterBaseWithImplementationWithout (Type p) { }

			[LogContains ("ImplementationClass.SingleParameterBaseWithoutImplementationWith_")]
			public void SingleParameterBaseWithoutImplementationWith_ (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type p)
			{ }

			[LogDoesNotContain ("ImplementationClass.SingleParameterBaseWithoutImplementationWithout")]
			public void SingleParameterBaseWithoutImplementationWithout (Type p) { }


			// === Generic methods ===
			[LogContains ("ImplementationClass.GenericInterfaceBaseWithoutImplementationWith_")]
			public void GenericInterfaceBaseWithoutImplementationWith_<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] T> () { }

			[LogContains ("ImplementationClass.GenericInterfaceBaseWithImplementationWithout")]
			public void GenericInterfaceBaseWithImplementationWithout<T> () { }

			// === Properties ===
			[LogContains ("ImplementationClass.PropertyInterfaceBaseWithoutImplementationWith.get")]
			[LogContains ("ImplementationClass.PropertyInterfaceBaseWithoutImplementationWith.set")]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public Type PropertyInterfaceBaseWithoutImplementationWith { get; set; }


			// === RequiresUnreferencedCode ===
			[LogDoesNotContain ("ImplementationClass.RequiresUnreferencedCodeInterfaceBaseWithImplementationWith_")]
			[RequiresUnreferencedCode ("")]
			public void RequiresUnreferencedCodeInterfaceBaseWithImplementationWith_ () { }
			[ExpectedWarning ("IL2046", "ImplementationClass.RequiresUnreferencedCodeInterfaceBaseWithoutImplementationWith_")]
			[RequiresUnreferencedCode ("")]
			public void RequiresUnreferencedCodeInterfaceBaseWithoutImplementationWith_ () { }
		}

		interface IBaseImplementedInterface
		{
			Type ReturnValueBaseWithInterfaceWithout ();

			[RequiresUnreferencedCode ("")]
			void RequiresUnreferencedCodeBaseWithoutInterfaceWith ();
		}

		class BaseImplementsInterfaceViaDerived
		{
			[LogContains (
				"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the return value of method 'Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.BaseImplementsInterfaceViaDerived.ReturnValueBaseWithInterfaceWithout()' " +
				"don't match overridden return value of method 'Mono.Linker.Tests.Cases.DataFlow.VirtualMethodHierarchyDataflowAnnotationValidation.IBaseImplementedInterface.ReturnValueBaseWithInterfaceWithout()'. " +
				"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.")]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public virtual Type ReturnValueBaseWithInterfaceWithout () => null;

			[ExpectedWarning ("IL2046", "BaseImplementsInterfaceViaDerived.RequiresUnreferencedCodeBaseWithoutInterfaceWith")]
			public virtual void RequiresUnreferencedCodeBaseWithoutInterfaceWith () { }
		}

		class DerivedWithInterfaceImplementedByBase : BaseImplementsInterfaceViaDerived, IBaseImplementedInterface
		{
		}


		interface ITwoInterfacesImplementedByOneMethod_One
		{
			Type ReturnValueInterfaceWithoutImplementationWith ();
		}

		interface ITwoInterfacesImplementedByOneMethod_Two
		{
			Type ReturnValueInterfaceWithoutImplementationWith ();
		}

		class ImplementationOfTwoInterfacesWithOneMethod : ITwoInterfacesImplementedByOneMethod_One, ITwoInterfacesImplementedByOneMethod_Two
		{
			[LogContains ("ITwoInterfacesImplementedByOneMethod_One.ReturnValueInterfaceWithoutImplementationWith")]
			[LogContains ("ITwoInterfacesImplementedByOneMethod_Two.ReturnValueInterfaceWithoutImplementationWith")]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public virtual Type ReturnValueInterfaceWithoutImplementationWith () => null;
		}

		static class StaticInterfaceMethods
		{
			interface IDamOnAll
			{
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				static abstract Type AbstractMethod
					<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
				T> (
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
					Type type);

				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				static virtual Type VirtualMethod
					<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
				T> (
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
					Type type)
				{ return null; }
			}

			class ImplIDamOnAllMissing : IDamOnAll
			{
				// NativeAOT doesn't validate overrides when accessed through reflection because it's a direct call (non-virtual)
				// So it doesn't matter that the annotations are not in-sync since the access will validate
				// the annotations on the implementation method - it doesn't even see the base method in this case.
				[ExpectedWarning ("IL2092", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2093", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2095", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				public static Type AbstractMethod<T> (Type type) => null;

				// NativeAOT doesn't validate overrides when accessed through reflection because it's a direct call (non-virtual)
				[ExpectedWarning ("IL2092", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2093", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2095", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				public static Type VirtualMethod<T> (Type type) => null;
			}

			class ImplIDamOnAllMismatch : IDamOnAll
			{
				// NativeAOT doesn't validate overrides when accessed through reflection because it's a direct call (non-virtual)
				[ExpectedWarning ("IL2092", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2093", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2095", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				public static Type AbstractMethod
					<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				T> (
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
					Type type)
				{ return null; }

				// NativeAOT doesn't validate overrides when accessed through reflection because it's a direct call (non-virtual)
				[ExpectedWarning ("IL2092", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2093", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2095", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				public static Type VirtualMethod
					<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				T> (
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
					Type type)
				{ return null; }
			}

			class ImplIDamOnAllMatch : IDamOnAll
			{
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public static Type AbstractMethod
								<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
				T> (
								[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
					Type type)
				{ return null; }

				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public static Type VirtualMethod
					<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
				T> (
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
					Type type)
				{ return null; }
			}

			interface IDamOnNone
			{
				static virtual Type VirtualMethod<T> (Type t) { return null; }
				static abstract Type AbstractMethod<T> (Type t);
			}

			class ImplIDamOnNoneMatch : IDamOnNone
			{
				public static Type VirtualMethod<T> (Type t) { return null; }
				public static Type AbstractMethod<T> (Type t) { return null; }
			}

			class ImplIDamOnNoneMismatch : IDamOnNone
			{
				// NativeAOT doesn't validate overrides when accessed through reflection because it's a direct call (non-virtual)
				[ExpectedWarning ("IL2092", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2093", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2095", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				public static Type AbstractMethod
					<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				T> (
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
					Type type)
				{ return null; }

				// NativeAOT doesn't validate overrides when accessed through reflection because it's a direct call (non-virtual)
				[ExpectedWarning ("IL2092", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2093", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2095", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				public static Type VirtualMethod
					<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				T> (
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
					Type type)
				{ return null; }
			}


			public static void Test ()
			{
				typeof (ImplIDamOnAllMatch).RequiresPublicMethods ();
				typeof (ImplIDamOnAllMismatch).RequiresPublicMethods ();
				typeof (ImplIDamOnAllMissing).RequiresPublicMethods ();
				typeof (IDamOnAll).RequiresPublicMethods ();
				typeof (ImplIDamOnNoneMatch).RequiresPublicMethods ();
				typeof (ImplIDamOnNoneMismatch).RequiresPublicMethods ();
				typeof (IDamOnNone).RequiresPublicMethods ();

			}
		}

		class BaseInPreservedScope
		{
			class ImplIAnnotatedMethodsMismatch : Library.IAnnotatedMethods
			{
				// NativeAOT doesn't always validate static overrides when accessed through reflection because it's a direct call (non-virtual)
				[ExpectedWarning ("IL2095", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				public static void GenericWithMethodsStatic<T> () { }

				[ExpectedWarning ("IL2092")]
				public static void ParamWithMethodsStatic (Type t) { }

				[ExpectedWarning ("IL2093")]
				public static Type ReturnWithMethodsStatic () => typeof (int);

				[ExpectedWarning ("IL2095")]
				public void GenericWithMethods<T> () { }

				[ExpectedWarning ("IL2092")]
				public void ParamWithMethods (Type t) { }

				[ExpectedWarning ("IL2093")]
				public Type ReturnWithMethods () => typeof (int);
			}

			class ImplIUnannotatedMethodsMismatch : Library.IUnannotatedMethods
			{
				// NativeAOT doesn't always validate static overrides when accessed through reflection because it's a direct call (non-virtual)
				[ExpectedWarning ("IL2095", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				public static void GenericStatic<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> () { }

				[ExpectedWarning ("IL2092")]
				public static void ParamStatic ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

				[ExpectedWarning ("IL2093")]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public static Type ReturnStatic () => typeof (int);

				[ExpectedWarning ("IL2095")]
				public void Generic<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> () { }

				[ExpectedWarning ("IL2092")]
				public void Param ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

				[ExpectedWarning ("IL2093")]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public Type Return () => typeof (int);
			}

			class DerivedFromUnannotatedMismatch : Library.UnannotatedMethods
			{
				[ExpectedWarning ("IL2095")]
				public override void Generic<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> () { }

				[ExpectedWarning ("IL2092")]
				public override void Param ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

				[ExpectedWarning ("IL2093")]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public override Type Return () => typeof (int);
			}

			class DerivedFromAnnotatedMismatch : Library.AnnotatedMethods
			{
				[ExpectedWarning ("IL2095")]
				public override void GenericWithMethods<T> () { }

				[ExpectedWarning ("IL2092")]
				public override void ParamWithMethods (Type t) { }

				[ExpectedWarning ("IL2093")]
				public override Type ReturnWithMethods () => typeof (int);
			}

			class ImplIUnannotatedMethodsMatch : Library.IUnannotatedMethods
			{
				public static void GenericStatic<T> () { }

				public static void ParamStatic (Type t) { }

				public static Type ReturnStatic () => typeof (int);

				public void Generic<T> () { }

				public void Param (Type t) { }

				public Type Return () => typeof (int);
			}

			class ImplIAnnotatedMethodsMatch : Library.IAnnotatedMethods
			{
				public static void GenericWithMethodsStatic<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> () { }

				public static void ParamWithMethodsStatic ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public static Type ReturnWithMethodsStatic () => typeof (int);

				public void GenericWithMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> () { }

				public void ParamWithMethods ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public Type ReturnWithMethods () => typeof (int);
			}

			class DerivedFromAnnotatedMatch : Library.AnnotatedMethods
			{
				public override void GenericWithMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> () { }

				public override void ParamWithMethods ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public override Type ReturnWithMethods () => typeof (int);
			}

			class DerivedFromUnannotatedMatch : Library.UnannotatedMethods
			{
				public override void Generic<T> () { }

				public override void Param (Type t) { }

				public override Type Return () => typeof (int);
			}

			public static void Test ()
			{
				// Access the interfaces as well - otherwise NativeAOT can decide
				// to not look for overrides since it knows it's making a direct access
				// to a method and it doesn't need to know about the base method
				// which leads to some warnings not being generated.
				typeof (Library.IAnnotatedMethods).RequiresAll ();
				typeof (Library.IUnannotatedMethods).RequiresAll ();

				typeof (ImplIUnannotatedMethodsMismatch).RequiresPublicMethods ();
				typeof (ImplIAnnotatedMethodsMismatch).RequiresPublicMethods ();
				typeof (DerivedFromAnnotatedMismatch).RequiresPublicMethods ();
				typeof (DerivedFromUnannotatedMismatch).RequiresPublicMethods ();
				typeof (ImplIUnannotatedMethodsMatch).RequiresPublicMethods ();
				typeof (ImplIAnnotatedMethodsMatch).RequiresPublicMethods ();
				typeof (DerivedFromAnnotatedMatch).RequiresPublicMethods ();
				typeof (DerivedFromUnannotatedMatch).RequiresPublicMethods ();
			}
		}

		// This is mostly for Native AOT - in that compiler it matters how a method
		// is referenced as it will take a different code path to do some of these validations
		// The above tests all rely on reflection marking so this test also uses direct calls
		class DirectCall
		{
			abstract class Base
			{
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public abstract Type NonGenericAbstract ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type);

				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public virtual Type NonGenericVirtual ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) => type;

				public abstract void GenericAbstract<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ();

				public virtual void GenericVirtual<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> () { }

				public abstract Type UnannotatedAbstract (Type type);

				public abstract void UnannotatedGenericAbstract<T> ();
			}

			class Derived : Base
			{
				[ExpectedWarning ("IL2092")]
				[ExpectedWarning ("IL2093")]
				public override Type NonGenericAbstract ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] Type type) => null;

				[ExpectedWarning ("IL2092")]
				[ExpectedWarning ("IL2093")]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
				public override Type NonGenericVirtual (Type type) => null;

				[ExpectedWarning ("IL2095")]
				public override void GenericAbstract<T> () { }

				[ExpectedWarning ("IL2095")]
				public override void GenericVirtual<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> () { }

				[ExpectedWarning ("IL2092")]
				[ExpectedWarning ("IL2093")]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public override Type UnannotatedAbstract ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] Type type) => null;

				[ExpectedWarning ("IL2095")]
				public override void UnannotatedGenericAbstract<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> () { }
			}

			interface IBaseWithDefault
			{
				void DefaultMethod (Type type);
			}

			interface IDerivedWithDefault : IBaseWithDefault
			{
				[ExpectedWarning ("IL2092")]
				[ExpectedWarning ("IL2092", ProducedBy = ProducedBy.Analyzer)] // https://github.com/dotnet/linker/issues/3121
				void IBaseWithDefault.DefaultMethod ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) { }
			}

			class ImplDerivedWithDefault : IDerivedWithDefault
			{
			}

			interface IGvmBase
			{
				Type UnannotatedGvm<T> (Type type);
				Type UnannotatedGvmCalledThroughBase<T> (Type type);

				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				static abstract Type AnnotatedStaticGvm<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] Type type);

				static virtual Type UnannotatedStaticGvm<T> (Type type) => null;
			}

			class ImplIGvmBase : IGvmBase
			{
				// NativeAOT doesn't validate overrides when it can resolve them as direct calls
				[ExpectedWarning ("IL2092", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2093", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2095", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public Type UnannotatedGvm<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) => null;

				[ExpectedWarning ("IL2092")]
				[ExpectedWarning ("IL2093")]
				[ExpectedWarning ("IL2095")]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public Type UnannotatedGvmCalledThroughBase<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) => null;

				[ExpectedWarning ("IL2092")]
				[ExpectedWarning ("IL2093")]
				[ExpectedWarning ("IL2095")]
				public static Type AnnotatedStaticGvm<T> (Type type) => null;

				[ExpectedWarning ("IL2092")]
				[ExpectedWarning ("IL2093")]
				[ExpectedWarning ("IL2095")]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public static Type UnannotatedStaticGvm<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] Type type) => null;
			}

			static void CallStaticGvm<TGvmBase> () where TGvmBase : IGvmBase
			{
				TGvmBase.AnnotatedStaticGvm<string> (typeof (string));
				TGvmBase.UnannotatedStaticGvm<string> (typeof (string));
			}

			public static void Test ()
			{
				Base instance = new Derived ();
				instance.NonGenericAbstract (typeof (string));
				instance.NonGenericVirtual (typeof (string));
				instance.GenericAbstract<string> ();
				instance.GenericVirtual<string> ();
				instance.UnannotatedAbstract (typeof (string));
				instance.UnannotatedGenericAbstract<string> ();

				((IBaseWithDefault) (new ImplDerivedWithDefault ())).DefaultMethod (typeof (string));

				ImplIGvmBase impl = new ImplIGvmBase ();
				impl.UnannotatedGvm<string> (typeof (string));

				IGvmBase ibase = (IGvmBase) impl;
				ibase.UnannotatedGvmCalledThroughBase<string> (typeof (string));

				CallStaticGvm<ImplIGvmBase> ();
			}
		}
	}
}

namespace System
{
	// This verifies correct validation of the "this" parameter annotations
	class VirtualMethodHierarchyDataflowAnnotationValidationTypeTestBase : TestSystemTypeBase
	{
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		public virtual void ThisBaseWithDerivedWithout () { }
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		public virtual void ThisBaseWithDerivedWith_ () { }
		public virtual void ThisBaseWithoutDerivedWith () { }
	}

	class VirtualMethodHierarchyDataflowAnnotationValidationTypeTestDerived : VirtualMethodHierarchyDataflowAnnotationValidationTypeTestBase
	{
		[ExpectedWarning ("IL2094",
			"System.VirtualMethodHierarchyDataflowAnnotationValidationTypeTestDerived.ThisBaseWithDerivedWithout()",
			"System.VirtualMethodHierarchyDataflowAnnotationValidationTypeTestBase.ThisBaseWithDerivedWithout()")]
		public override void ThisBaseWithDerivedWithout () { }

		[LogDoesNotContain ("VirtualMethodHierarchyDataflowAnnotationValidationTypeTestDerived.ThisBaseWithDerivedWith_")]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		public override void ThisBaseWithDerivedWith_ () { }

		[LogContains ("VirtualMethodHierarchyDataflowAnnotationValidationTypeTestDerived.ThisBaseWithoutDerivedWith")]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		public override void ThisBaseWithoutDerivedWith () { }
	}
}
