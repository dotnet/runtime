// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.DataFlow;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

[assembly: ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeType--")]
[assembly: AnnotatedMembersAccessedViaReflection.AnnotatedAttributeConstructorAttribute (typeof (AnnotatedMembersAccessedViaReflection.RequiresUnreferencedCodeType))]

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	[UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "These tests are not targetted at AOT scenarios")]
	class AnnotatedMembersAccessedViaReflection
	{
		public static void Main ()
		{
			AnnotatedField.Test ();
			AnnotatedMethodParameters.Test ();
			AnnotatedMethodReturnValue.Test ();
			AnnotatedProperty.Test ();
			AnnotatedGenerics.Test ();
			AnnotationOnGenerics.Test ();
			AnnotationOnInteropMethod.Test ();
			AccessThroughLdToken.Test ();
		}

		class AnnotatedField
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static Type _annotatedField;

			[ExpectedWarning ("IL2110", nameof (_annotatedField))]
			static void Reflection ()
			{
				typeof (AnnotatedField).GetField ("_annotatedField").SetValue (null, typeof (TestType));
			}

			[RequiresUnreferencedCode ("test")]
			static void ReflectionSuppressedByRUC ()
			{
				typeof (AnnotatedField).GetField ("_annotatedField").SetValue (null, typeof (TestType));
			}

			[ExpectedWarning ("IL2110", nameof (_annotatedField))]
			static void ReflectionReadOnly ()
			{
				typeof (AnnotatedField).GetField ("_annotatedField").GetValue (null);
			}

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/linker/issues/2560
			[ExpectedWarning ("IL2110", nameof (_annotatedField), ProducedBy = ProducedBy.Trimmer)]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicFields, typeof (AnnotatedField))]
			static void DynamicDependency ()
			{
			}

			[RequiresUnreferencedCode ("test")]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicFields, typeof (AnnotatedField))]
			static void DynamicDependencySuppressedByRUC ()
			{
			}

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/linker/issues/2560
			[ExpectedWarning ("IL2110", nameof (_annotatedField), ProducedBy = ProducedBy.Trimmer)]
			[DynamicDependency (nameof (_annotatedField), typeof (AnnotatedField))]
			static void DynamicDependencyByName ()
			{
			}

			[ExpectedWarning ("IL2110", nameof (_annotatedField))]
			static void DynamicallyAccessedMembers ()
			{
				typeof (AnnotatedField).RequiresPublicFields ();
			}

			[RequiresUnreferencedCode ("test")]
			static void DynamicallyAccessedMembersSuppressedByRUC ()
			{
				typeof (AnnotatedField).RequiresPublicFields ();
			}

			class NestedType
			{
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public static Type _annotatedField;
			}

			[ExpectedWarning ("IL2110", nameof (AnnotatedField._annotatedField))]
			[ExpectedWarning ("IL2110", nameof (NestedType._annotatedField))]
			[ExpectedWarning ("IL2026", "ReflectionSuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "DynamicDependencySuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "DynamicallyAccessedMembersSuppressedByRUC", "test")]
			static void DynamicallyAccessedMembersAll1 ()
			{
				typeof (AnnotatedField).RequiresAll ();
			}

			[ExpectedWarning ("IL2110", nameof (AnnotatedField._annotatedField))]
			[ExpectedWarning ("IL2110", nameof (NestedType._annotatedField))]
			[ExpectedWarning ("IL2026", "ReflectionSuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "DynamicDependencySuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "DynamicallyAccessedMembersSuppressedByRUC", "test")]
			static void DynamicallyAccessedMembersAll2 ()
			{
				typeof (AnnotatedField).RequiresAll ();
			}

			[ExpectedWarning ("IL2110", nameof (NestedType), nameof (NestedType._annotatedField))]
			static void DynamicallyAccessedMembersNestedTypes1 ()
			{
				typeof (AnnotatedField).RequiresNonPublicNestedTypes ();
			}

			[ExpectedWarning ("IL2110", nameof (NestedType), nameof (NestedType._annotatedField))]
			static void DynamicallyAccessedMembersNestedTypes2 ()
			{
				typeof (AnnotatedField).RequiresNonPublicNestedTypes ();
			}

			[UnconditionalSuppressMessage ("test", "IL2026")]
			public static void Test ()
			{
				Reflection ();
				ReflectionSuppressedByRUC ();
				ReflectionReadOnly ();
				DynamicDependency ();
				DynamicDependencySuppressedByRUC ();
				DynamicDependencyByName ();
				DynamicallyAccessedMembers ();
				DynamicallyAccessedMembersSuppressedByRUC ();
				DynamicallyAccessedMembersAll1 ();
				DynamicallyAccessedMembersAll2 ();
				DynamicallyAccessedMembersNestedTypes1 ();
				DynamicallyAccessedMembersNestedTypes2 ();
			}
		}

		class AnnotatedMethodParameters
		{
			public static void MethodWithSingleAnnotatedParameter (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{ }

			class AttributeWithConstructorWithAnnotation : Attribute
			{
				public AttributeWithConstructorWithAnnotation (
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{ }
			}

			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter))]
			static void Reflection ()
			{
				typeof (AnnotatedMethodParameters).GetMethod (nameof (MethodWithSingleAnnotatedParameter)).Invoke (null, null);
			}

			[RequiresUnreferencedCode ("test")]
			static void ReflectionSuppressedByRUC ()
			{
				typeof (AnnotatedMethodParameters).GetMethod (nameof (MethodWithSingleAnnotatedParameter)).Invoke (null, null);
			}

			// Should not warn, there's nothing wrong about this
			[AttributeWithConstructorWithAnnotation (typeof (TestType))]
			static void AnnotatedAttributeConstructor ()
			{
			}

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/linker/issues/2560
			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter), ProducedBy = ProducedBy.Trimmer)]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicMethods, typeof (AnnotatedMethodParameters))]
			static void DynamicDependency ()
			{
			}

			[RequiresUnreferencedCode ("test")]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicMethods, typeof (AnnotatedMethodParameters))]
			static void DynamicDependencySuppressedByRUC ()
			{
			}

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/linker/issues/2560
			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter), ProducedBy = ProducedBy.Trimmer)]
			[DynamicDependency (nameof (MethodWithSingleAnnotatedParameter), typeof (AnnotatedMethodParameters))]
			static void DynamicDependencyByName ()
			{
			}

			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter))]
			static void DynamicallyAccessedMembers ()
			{
				typeof (AnnotatedMethodParameters).RequiresPublicMethods ();
			}

			[RequiresUnreferencedCode ("test")]
			static void DynamicallyAccessedMembersSuppressedByRUC ()
			{
				typeof (AnnotatedMethodParameters).RequiresPublicMethods ();
			}

			// Action delegate is not handled correctly https://github.com/dotnet/linker/issues/2561
			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter), ProducedBy = ProducedBy.Trimmer)]
			static void Ldftn ()
			{
				var _ = new Action<Type> (AnnotatedMethodParameters.MethodWithSingleAnnotatedParameter);
			}

			interface IWithAnnotatedMethod
			{
				public void AnnotatedMethod ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] Type type);
			}

			// Action delegate is not handled correctly https://github.com/dotnet/linker/issues/2561
			[ExpectedWarning ("IL2111", nameof (IWithAnnotatedMethod.AnnotatedMethod), ProducedBy = ProducedBy.Trimmer)]
			static void Ldvirtftn ()
			{
				IWithAnnotatedMethod instance = null;
				var _ = new Action<Type> (instance.AnnotatedMethod);
			}

			[ExpectedWarning ("IL2026", "ReflectionSuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "DynamicDependencySuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "DynamicallyAccessedMembersSuppressedByRUC", "test")]
			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter))]
			[ExpectedWarning ("IL2111", nameof (IWithAnnotatedMethod.AnnotatedMethod))]
			[ExpectedWarning ("IL2111", nameof (IWithAnnotatedMethod.AnnotatedMethod))]
			static void DynamicallyAccessedMembersAll1 ()
			{
				typeof (AnnotatedMethodParameters).RequiresAll ();
			}

			[ExpectedWarning ("IL2026", "ReflectionSuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "DynamicDependencySuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "DynamicallyAccessedMembersSuppressedByRUC", "test")]
			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter))]
			[ExpectedWarning ("IL2111", nameof (IWithAnnotatedMethod.AnnotatedMethod))]
			[ExpectedWarning ("IL2111", nameof (IWithAnnotatedMethod.AnnotatedMethod))]
			static void DynamicallyAccessedMembersAll2 ()
			{
				typeof (AnnotatedMethodParameters).RequiresAll ();
			}

			[UnconditionalSuppressMessage ("test", "IL2026")]
			public static void Test ()
			{
				Reflection ();
				ReflectionSuppressedByRUC ();
				DynamicDependency ();
				DynamicDependencySuppressedByRUC ();
				DynamicDependencyByName ();
				DynamicallyAccessedMembers ();
				DynamicallyAccessedMembersSuppressedByRUC ();
				Ldftn ();
				Ldvirtftn ();
				DynamicallyAccessedMembersAll1 ();
				DynamicallyAccessedMembersAll2 ();
			}
		}

		class AnnotatedMethodReturnValue
		{
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static Type StaticMethodWithAnnotatedReturnValue () => null;

			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public Type InstanceMethodWithAnnotatedReturnValue () => null;

			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public virtual Type VirtualMethodWithAnnotatedReturnValue () => null;

			// Only virtual methods should warn - the problem is only possible if something overrides a virtual method.
			// Getting an annotated value in itself is not dangerous in any way.

			static void ReflectionOnStatic ()
			{
				typeof (AnnotatedMethodReturnValue).GetMethod (nameof (StaticMethodWithAnnotatedReturnValue)).Invoke (null, null);
			}

			static void ReflectionOnInstance ()
			{
				typeof (AnnotatedMethodReturnValue).GetMethod (nameof (InstanceMethodWithAnnotatedReturnValue)).Invoke (null, null);
			}

			[ExpectedWarning ("IL2111", nameof (VirtualMethodWithAnnotatedReturnValue))]
			static void ReflectionOnVirtual ()
			{
				typeof (AnnotatedMethodReturnValue).GetMethod (nameof (VirtualMethodWithAnnotatedReturnValue)).Invoke (null, null);
			}

			[RequiresUnreferencedCode ("test")]
			static void ReflectionOnVirtualSuppressedByRUC ()
			{
				typeof (AnnotatedMethodReturnValue).GetMethod (nameof (VirtualMethodWithAnnotatedReturnValue)).Invoke (null, null);
			}

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/linker/issues/2560
			[ExpectedWarning ("IL2111", nameof (VirtualMethodWithAnnotatedReturnValue), ProducedBy = ProducedBy.Trimmer)]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicMethods, typeof (AnnotatedMethodReturnValue))]
			static void DynamicDependency ()
			{
			}

			[RequiresUnreferencedCode ("test")]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicMethods, typeof (AnnotatedMethodReturnValue))]
			static void DynamicDependencySuppressedByRUC ()
			{
			}

			[DynamicDependency (nameof (StaticMethodWithAnnotatedReturnValue), typeof (AnnotatedMethodReturnValue))]
			static void DynamicDependencyByNameOnStatic ()
			{
			}

			[DynamicDependency (nameof (InstanceMethodWithAnnotatedReturnValue), typeof (AnnotatedMethodReturnValue))]
			static void DynamicDependencyByNameOnInstance ()
			{
			}

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/linker/issues/2560
			[ExpectedWarning ("IL2111", nameof (VirtualMethodWithAnnotatedReturnValue), ProducedBy = ProducedBy.Trimmer)]
			[DynamicDependency (nameof (VirtualMethodWithAnnotatedReturnValue), typeof (AnnotatedMethodReturnValue))]
			static void DynamicDependencyByNameOnVirtual ()
			{
			}

			[ExpectedWarning ("IL2111", nameof (VirtualMethodWithAnnotatedReturnValue))]
			static void DynamicallyAccessedMembers ()
			{
				typeof (AnnotatedMethodReturnValue).RequiresPublicMethods ();
			}

			[RequiresUnreferencedCode ("test")]
			static void DynamicallyAccessedMembersSuppressedByRUC ()
			{
				typeof (AnnotatedMethodReturnValue).RequiresPublicMethods ();
			}

			static void LdftnOnStatic ()
			{
				var _ = new Func<Type> (AnnotatedMethodReturnValue.StaticMethodWithAnnotatedReturnValue);
			}

			static void LdftnOnInstance ()
			{
				var _ = new Func<Type> ((new AnnotatedMethodReturnValue ()).InstanceMethodWithAnnotatedReturnValue);
			}

			// Action delegate is not handled correctly https://github.com/dotnet/linker/issues/2561
			[ExpectedWarning ("IL2111", nameof (VirtualMethodWithAnnotatedReturnValue), ProducedBy = ProducedBy.Trimmer)]
			static void LdftnOnVirtual ()
			{
				var _ = new Func<Type> ((new AnnotatedMethodReturnValue ()).VirtualMethodWithAnnotatedReturnValue);
			}

			[UnconditionalSuppressMessage ("test", "IL2026")]
			public static void Test ()
			{
				ReflectionOnStatic ();
				ReflectionOnInstance ();
				ReflectionOnVirtual ();
				ReflectionOnVirtualSuppressedByRUC ();
				DynamicDependency ();
				DynamicDependencySuppressedByRUC ();
				DynamicDependencyByNameOnStatic ();
				DynamicDependencyByNameOnInstance ();
				DynamicDependencyByNameOnVirtual ();
				DynamicallyAccessedMembers ();
				DynamicallyAccessedMembersSuppressedByRUC ();
				LdftnOnStatic ();
				LdftnOnInstance ();
				LdftnOnVirtual ();
			}
		}

		class AnnotatedProperty
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)]
			public static Type Property1WithAnnotation { get; set; }

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
			public static Type Property2WithAnnotationGetterOnly { get => null; }

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
			public virtual Type VirtualProperty3WithAnnotationGetterOnly { get => null; }

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
			public virtual Type VirtualProperty4WithAnnotation { get => null; set { value.ToString (); } }

			public static Type Property5WithAnnotationOnMembers {
				[ExpectedWarning ("IL2078", nameof (Property5WithAnnotationOnMembers) + ".get", ProducedBy = ProducedBy.Trimmer)]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
				get;
				[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
				set;
			}

			public virtual Type VirtualProperty6WithAnnotationOnMembers {
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
				get => null;
				[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
				set { value.ToString (); }
			}

			class AttributeWithPropertyWithAnnotation : Attribute
			{
				public AttributeWithPropertyWithAnnotation () { }

				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
				public Type PropertyWithAnnotation { get; set; }
			}

			[ExpectedWarning ("IL2111", nameof (Property1WithAnnotation) + ".set")]
			static void ReflectionOnPropertyItself ()
			{
				typeof (AnnotatedProperty).GetProperty (nameof (Property1WithAnnotation));
			}

			[RequiresUnreferencedCode ("test")]
			static void ReflectionOnPropertyItselfSuppressedByRUC ()
			{
				typeof (AnnotatedProperty).GetProperty (nameof (Property1WithAnnotation));
			}

			static void ReflectionOnPropertyWithGetterOnly ()
			{
				typeof (AnnotatedProperty).GetProperty (nameof (Property2WithAnnotationGetterOnly));
			}

			[ExpectedWarning ("IL2111", nameof (VirtualProperty3WithAnnotationGetterOnly))]
			static void ReflectionOnPropertyWithGetterOnlyOnVirtual ()
			{
				typeof (AnnotatedProperty).GetProperty (nameof (VirtualProperty3WithAnnotationGetterOnly));
			}

			static void ReflectionOnGetter ()
			{
				typeof (AnnotatedProperty).GetMethod ("get_" + nameof (Property1WithAnnotation));
			}

			[ExpectedWarning ("IL2111", nameof (Property1WithAnnotation) + ".set")]
			static void ReflectionOnSetter ()
			{
				typeof (AnnotatedProperty).GetMethod ("set_" + nameof (Property1WithAnnotation));
			}

			[ExpectedWarning ("IL2111", nameof (VirtualProperty3WithAnnotationGetterOnly) + ".get")]
			static void ReflectionOnVirtualGetter ()
			{
				typeof (AnnotatedProperty).GetMethod ("get_" + nameof (VirtualProperty3WithAnnotationGetterOnly));
			}

			// Should not warn - there's nothing wrong with this
			[AttributeWithPropertyWithAnnotation (PropertyWithAnnotation = typeof (TestType))]
			static void AnnotatedAttributeProperty ()
			{
			}

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/linker/issues/2560
			[ExpectedWarning ("IL2111", nameof (Property1WithAnnotation) + ".set", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty3WithAnnotationGetterOnly) + ".get", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".get", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".set", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2111", nameof (Property5WithAnnotationOnMembers) + ".set", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".get", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".set", ProducedBy = ProducedBy.Trimmer)]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicProperties, typeof (AnnotatedProperty))]
			static void DynamicDependency ()
			{
			}

			[RequiresUnreferencedCode ("test")]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicProperties, typeof (AnnotatedProperty))]
			static void DynamicDependencySuppressedByRUC ()
			{
			}

			[ExpectedWarning ("IL2111", nameof (Property1WithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty3WithAnnotationGetterOnly) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (Property5WithAnnotationOnMembers) + ".set")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".set")]

			static void DynamicallyAccessedMembers ()
			{
				typeof (AnnotatedProperty).RequiresPublicProperties ();
			}

			[RequiresUnreferencedCode ("test")]
			static void DynamicallyAccessedMembersSuppressedByRUC ()
			{
				typeof (AnnotatedProperty).RequiresPublicProperties ();
			}

			[ExpectedWarning ("IL2026", nameof (DynamicDependencySuppressedByRUC), "test")]
			[ExpectedWarning ("IL2026", nameof (DynamicallyAccessedMembersSuppressedByRUC), "test")]
			[ExpectedWarning ("IL2026", nameof (ReflectionOnPropertyItselfSuppressedByRUC), "test")]
			// Duplicated warnings for linker and analyzer see bug https://github.com/dotnet/linker/issues/2462
			[ExpectedWarning ("IL2111", nameof (AnnotatedProperty.Property1WithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (AnnotatedProperty.Property1WithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (AttributeWithPropertyWithAnnotation.PropertyWithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (AttributeWithPropertyWithAnnotation.PropertyWithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty3WithAnnotationGetterOnly) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty3WithAnnotationGetterOnly) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (Property5WithAnnotationOnMembers) + ".set")]
			[ExpectedWarning ("IL2111", nameof (Property5WithAnnotationOnMembers) + ".set")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".set")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".set")]
			[UnconditionalSuppressMessage ("Test", "IL2110", Justification = "Suppress warning about backing field of PropertyWithAnnotation")]
			static void DynamicallyAccessedMembersAll1 ()
			{
				typeof (AnnotatedProperty).RequiresAll ();
			}

			[ExpectedWarning ("IL2026", nameof (DynamicDependencySuppressedByRUC), "test")]
			[ExpectedWarning ("IL2026", nameof (DynamicallyAccessedMembersSuppressedByRUC), "test")]
			[ExpectedWarning ("IL2026", nameof (ReflectionOnPropertyItselfSuppressedByRUC), "test")]
			// Duplicated warnings for linker and analyzer see bug https://github.com/dotnet/linker/issues/2462
			[ExpectedWarning ("IL2111", nameof (AnnotatedProperty.Property1WithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (AnnotatedProperty.Property1WithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (AttributeWithPropertyWithAnnotation.PropertyWithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (AttributeWithPropertyWithAnnotation.PropertyWithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty3WithAnnotationGetterOnly) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty3WithAnnotationGetterOnly) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (Property5WithAnnotationOnMembers) + ".set")]
			[ExpectedWarning ("IL2111", nameof (Property5WithAnnotationOnMembers) + ".set")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".get")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".set")]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".set")]
			[UnconditionalSuppressMessage ("Test", "IL2110", Justification = "Suppress warning about backing field of PropertyWithAnnotation")]
			static void DynamicallyAccessedMembersAll2 ()
			{
				typeof (AnnotatedProperty).RequiresAll ();
			}

			// Analyzer doesn't produce this warning https://github.com/dotnet/linker/issues/2628
			[ExpectedWarning ("IL2110", nameof (Property1WithAnnotation), ProducedBy = ProducedBy.Trimmer)]
			static void DynamicallyAccessedFields ()
			{
				typeof (AnnotatedProperty).RequiresNonPublicFields ();
			}

			[UnconditionalSuppressMessage ("test", "IL2026")]
			public static void Test ()
			{
				ReflectionOnPropertyItself ();
				ReflectionOnPropertyItselfSuppressedByRUC ();
				ReflectionOnPropertyWithGetterOnly ();
				ReflectionOnPropertyWithGetterOnlyOnVirtual ();
				ReflectionOnGetter ();
				ReflectionOnSetter ();
				ReflectionOnVirtualGetter ();
				AnnotatedAttributeProperty ();
				DynamicDependency ();
				DynamicDependencySuppressedByRUC ();
				DynamicallyAccessedMembers ();
				DynamicallyAccessedMembersSuppressedByRUC ();
				DynamicallyAccessedMembersAll1 ();
				DynamicallyAccessedMembersAll2 ();
				DynamicallyAccessedFields ();
			}
		}

		// Annotation on generic parameter
		class AnnotatedGenerics
		{
			public static void GenericWithAnnotation<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)] T> ()
			{ }

			static void ReflectionOnly ()
			{
				// Should not warn - there's nothing wrong with asking for MethodInfo alone
				typeof (AnnotatedGenerics).GetMethod (nameof (GenericWithAnnotation));
			}

			// Similarly to direct reflection - no warning expected
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicMethods, typeof (AnnotatedGenerics))]
			static void DynamicDependency ()
			{
			}

			// Similarly to direct reflection - no warning expected
			static void DynamicallyAccessedMembers ()
			{
				typeof (AnnotatedGenerics).RequiresPublicMethods ();
			}

			// This should produce IL2071 https://github.com/dotnet/linker/issues/2144
			[ExpectedWarning ("IL2070", "MakeGenericMethod")]
			static void InstantiateGeneric (Type type = null)
			{
				// This should warn due to MakeGenericMethod - in this case the generic parameter is unannotated type
				typeof (AnnotatedGenerics).GetMethod (nameof (GenericWithAnnotation)).MakeGenericMethod (type);
			}

			// Like above, no warning expected
			static void DynamicallyAccessedMembersAll ()
			{
				typeof (AnnotatedGenerics).RequiresAll ();
			}

			public static void Test ()
			{
				ReflectionOnly ();
				DynamicDependency ();
				DynamicallyAccessedMembers ();
				InstantiateGeneric ();
				DynamicallyAccessedMembersAll ();
			}
		}

		// Annotation on non-generic parameter but on generic methods
		class AnnotationOnGenerics
		{
			class GenericWithAnnotatedMethod<T>
			{
				public static void AnnotatedMethod (
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{ }
			}

			public static void GenericMethodWithAnnotation<T> (
			   [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{ }

			// Action delegate is not handled correctly https://github.com/dotnet/linker/issues/2561
			[ExpectedWarning ("IL2111", nameof (GenericWithAnnotatedMethod<TestType>.AnnotatedMethod), ProducedBy = ProducedBy.Trimmer)]
			public static void GenericTypeWithStaticMethodViaLdftn ()
			{
				var _ = new Action<Type> (GenericWithAnnotatedMethod<TestType>.AnnotatedMethod);
			}

			[ExpectedWarning ("IL2111", nameof (GenericMethodWithAnnotation))]
			public static void GenericMethodWithAnnotationReflection ()
			{
				typeof (AnnotationOnGenerics).GetMethod (nameof (GenericMethodWithAnnotation));
			}

			public static void GenericMethodWithAnnotationDirectCall ()
			{
				// Should not warn, nothing wrong about this
				GenericMethodWithAnnotation<TestType> (typeof (TestType));
			}

			// Action delegate is not handled correctly https://github.com/dotnet/linker/issues/2561
			[ExpectedWarning ("IL2111", nameof (GenericMethodWithAnnotation), ProducedBy = ProducedBy.Trimmer)]
			public static void GenericMethodWithAnnotationViaLdftn ()
			{
				var _ = new Action<Type> (GenericMethodWithAnnotation<TestType>);
			}

			[ExpectedWarning ("IL2111", nameof (GenericMethodWithAnnotation))]
			public static void GenericMethodDynamicallyAccessedMembers ()
			{
				typeof (AnnotationOnGenerics).RequiresPublicMethods ();
			}

			[ExpectedWarning ("IL2111", nameof (GenericMethodWithAnnotation))]
			[ExpectedWarning ("IL2111", "GenericWithAnnotatedMethod", "AnnotatedMethod")]
			static void DynamicallyAccessedMembersAll1 ()
			{
				typeof (AnnotationOnGenerics).RequiresAll ();
			}

			[ExpectedWarning ("IL2111", nameof (GenericMethodWithAnnotation))]
			[ExpectedWarning ("IL2111", "GenericWithAnnotatedMethod", "AnnotatedMethod")]
			static void DynamicallyAccessedMembersAll2 ()
			{
				typeof (AnnotationOnGenerics).RequiresAll ();
			}

			public static void Test ()
			{
				GenericTypeWithStaticMethodViaLdftn ();
				GenericMethodWithAnnotationReflection ();
				GenericMethodWithAnnotationDirectCall ();
				GenericMethodWithAnnotationViaLdftn ();
				GenericMethodDynamicallyAccessedMembers ();
				DynamicallyAccessedMembersAll1 ();
				DynamicallyAccessedMembersAll2 ();
			}
		}

		class AnnotationOnInteropMethod
		{
			struct ValueWithAnnotatedField
			{
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public Type _typeField;
			}

			// Analyzer doesnt take into account interop attributes https://github.com/dotnet/linker/issues/2562
			[ExpectedWarning ("IL2110", nameof (ValueWithAnnotatedField._typeField), ProducedBy = ProducedBy.Trimmer)]
			[DllImport ("nonexistent")]
			static extern ValueWithAnnotatedField GetValueWithAnnotatedField ();

			// Analyzer doesnt take into account interop attributes https://github.com/dotnet/linker/issues/2562
			[ExpectedWarning ("IL2110", nameof (ValueWithAnnotatedField._typeField), ProducedBy = ProducedBy.Trimmer)]
			[DllImport ("nonexistent")]
			static extern void AcceptValueWithAnnotatedField (ValueWithAnnotatedField value);

			public static void Test ()
			{
				GetValueWithAnnotatedField ();
				AcceptValueWithAnnotatedField (default (ValueWithAnnotatedField));
			}
		}

		class AccessThroughLdToken
		{
			public virtual Type PropertyWithLdToken {
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				get {
					return null;
				}
			}

			// Action delegate is not handled correctly https://github.com/dotnet/linker/issues/2561
			[ExpectedWarning ("IL2111", nameof (PropertyWithLdToken), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2111", nameof (PropertyWithLdToken), ProducedBy = ProducedBy.Trimmer)]
			public static void Test ()
			{
				Expression<Func<Type>> getter = () => (new AccessThroughLdToken ()).PropertyWithLdToken;
			}
		}

		public class AnnotatedAttributeConstructorAttribute : Attribute
		{
			public AnnotatedAttributeConstructorAttribute ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
			{ }
		}

		class TestType { }

		[RequiresUnreferencedCode ("--RequiresUnreferencedCodeType--")]
		public class RequiresUnreferencedCodeType { }
	}
}
