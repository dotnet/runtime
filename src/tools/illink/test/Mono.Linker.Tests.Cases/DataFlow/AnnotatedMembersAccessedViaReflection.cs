// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.DataFlow;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SandboxDependency ("Dependencies/TestSystemTypeBase.cs")]
	[ExpectedNoWarnings]
	[UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "These tests are not targeted at AOT scenarios")]
	class AnnotatedMembersAccessedViaReflection
	{
		public static void Main ()
		{
			AnnotatedField.Test ();
			AnnotatedMethodThisParameter.Test ();
			AnnotatedMethodParameters.Test ();
			AnnotatedMethodReturnValue.Test ();
			AnnotatedProperty.Test ();
			AnnotatedGenerics.Test ();
			AnnotationOnGenerics.Test ();
			AnnotationOnInteropMethod.Test ();
			DelegateCreation.Test ();
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

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/runtime/issues/83080
			[ExpectedWarning ("IL2110", nameof (_annotatedField), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicFields, typeof (AnnotatedField))]
			static void DynamicDependency ()
			{
			}

			[RequiresUnreferencedCode ("test")]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicFields, typeof (AnnotatedField))]
			static void DynamicDependencySuppressedByRUC ()
			{
			}

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/runtime/issues/83080
			[ExpectedWarning ("IL2110", nameof (_annotatedField), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

			static void PotentialWriteAccess (ref Type type)
			{
			}

			// https://github.com/dotnet/linker/issues/3172
			[ExpectedWarning ("IL2110", nameof (AnnotatedField._annotatedField), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void LdToken ()
			{
				Expression<Action> a = () => PotentialWriteAccess (ref _annotatedField);
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
				LdToken ();
			}
		}

		public class AnnotatedMethodThisParameter : TestSystemTypeBase
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public void MethodWithAnnotatedThisParameter ()
			{ }

			[ExpectedWarning ("IL2111", nameof (MethodWithAnnotatedThisParameter))]
			static void Reflection ()
			{
				typeof (AnnotatedMethodThisParameter).GetMethod (nameof (MethodWithAnnotatedThisParameter)).Invoke (null, null);
			}

			[ExpectedWarning ("IL2111", nameof (MethodWithAnnotatedThisParameter))]
			void Ldftn ()
			{
				var _ = new Action (MethodWithAnnotatedThisParameter);
			}

			[ExpectedWarning ("IL2111")]
			void LdftnOnLambda ()
			{
				var _ = new Action (
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
					() => MethodWithAnnotatedThisParameter ());
			}

			[ExpectedWarning ("IL2111")]
			void LdftnOnLocalMethod ()
			{
				var _ = new Action (LocalMethod);

				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				void LocalMethod ()
				{
					MethodWithAnnotatedThisParameter ();
				}
			}

			public static void Test ()
			{
				var instance = new AnnotatedMethodThisParameter ();
				Reflection ();
				instance.Ldftn ();
				instance.LdftnOnLambda ();
				instance.LdftnOnLocalMethod ();
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

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/runtime/issues/83080
			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicMethods, typeof (AnnotatedMethodParameters))]
			static void DynamicDependency ()
			{
			}

			[RequiresUnreferencedCode ("test")]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicMethods, typeof (AnnotatedMethodParameters))]
			static void DynamicDependencySuppressedByRUC ()
			{
			}

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/runtime/issues/83080
			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter))]
			static void Ldftn ()
			{
				var _ = new Action<Type> (AnnotatedMethodParameters.MethodWithSingleAnnotatedParameter);
			}

			[RequiresUnreferencedCode ("test")]
			static void LdftnSuppressedByRequiresUnreferencedCode ()
			{
				var _ = new Action<Type> (AnnotatedMethodParameters.MethodWithSingleAnnotatedParameter);
			}

			[ExpectedWarning ("IL2111")]
			static void 
			LdftnOnLambda ()
			{
				var _ = new Action<Type> (
					([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) => { });
			}

			[ExpectedWarning ("IL2111")]
			static void LdftnOnLocalMethod ()
			{
				var _ = new Action<Type> (LocalMethod);

				void LocalMethod (
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{ }
			}

			static void LdftnOnLambdaTriggersLamdaAnalysis ()
			{
				var _ = new Action<Type> (
					[ExpectedWarning ("IL2067", nameof (type), nameof (DataFlowTypeExtensions.RequiresAll))]
					(Type type) => { type.RequiresAll (); });
			}

			static void LdftnOnLocalMethodTriggersLocalMethodAnalysis ()
			{
				var _ = new Action<Type> (LocalMethod);

				[ExpectedWarning ("IL2067", nameof (type), nameof (DataFlowTypeExtensions.RequiresAll))]
				void LocalMethod (Type type)
				{
					type.RequiresAll ();
				}
			}

			interface IWithAnnotatedMethod
			{
				public void AnnotatedMethod ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] Type type);
			}

			[ExpectedWarning ("IL2111", nameof (IWithAnnotatedMethod.AnnotatedMethod))]
			static void Ldvirtftn ()
			{
				IWithAnnotatedMethod instance = null;
				var _ = new Action<Type> (instance.AnnotatedMethod);
			}

			[ExpectedWarning ("IL2026", "ReflectionSuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "DynamicDependencySuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "DynamicallyAccessedMembersSuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "LdftnSuppressedByRequiresUnreferencedCode", "test")]
			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter))]
			[ExpectedWarning ("IL2111", nameof (IWithAnnotatedMethod.AnnotatedMethod))]
			[ExpectedWarning ("IL2111", nameof (IWithAnnotatedMethod.AnnotatedMethod))]
			[ExpectedWarning ("IL2118", nameof (LdftnOnLambdaTriggersLamdaAnalysis), ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LdftnOnLocalMethodTriggersLocalMethodAnalysis), ProducedBy = Tool.Trimmer)]
			static void DynamicallyAccessedMembersAll1 ()
			{
				typeof (AnnotatedMethodParameters).RequiresAll ();
			}

			[ExpectedWarning ("IL2026", "ReflectionSuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "DynamicDependencySuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "DynamicallyAccessedMembersSuppressedByRUC", "test")]
			[ExpectedWarning ("IL2026", "LdftnSuppressedByRequiresUnreferencedCode", "test")]
			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter))]
			[ExpectedWarning ("IL2111", nameof (IWithAnnotatedMethod.AnnotatedMethod))]
			[ExpectedWarning ("IL2111", nameof (IWithAnnotatedMethod.AnnotatedMethod))]
			[ExpectedWarning ("IL2118", nameof (LdftnOnLambdaTriggersLamdaAnalysis), ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LdftnOnLocalMethodTriggersLocalMethodAnalysis), ProducedBy = Tool.Trimmer)]
			static void DynamicallyAccessedMembersAll2 ()
			{
				typeof (AnnotatedMethodParameters).RequiresAll ();
			}

			// https://github.com/dotnet/linker/issues/3172
			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2067", nameof (MethodWithSingleAnnotatedParameter), ProducedBy = Tool.Analyzer)]
			static void LdToken ()
			{
				Expression<Action<Type>> _ = (Type t) => MethodWithSingleAnnotatedParameter (t);
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
				LdftnSuppressedByRequiresUnreferencedCode ();
				LdftnOnLambda ();
				LdftnOnLocalMethod ();
				LdftnOnLambdaTriggersLamdaAnalysis ();
				LdftnOnLocalMethodTriggersLocalMethodAnalysis ();
				Ldvirtftn ();
				DynamicallyAccessedMembersAll1 ();
				DynamicallyAccessedMembersAll2 ();
				LdToken ();
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

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/runtime/issues/83080
			[ExpectedWarning ("IL2111", nameof (VirtualMethodWithAnnotatedReturnValue), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/runtime/issues/83080
			[ExpectedWarning ("IL2111", nameof (VirtualMethodWithAnnotatedReturnValue), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

			[ExpectedWarning ("IL2111", nameof (VirtualMethodWithAnnotatedReturnValue))]
			static void LdftnOnVirtual ()
			{
				var _ = new Func<Type> ((new AnnotatedMethodReturnValue ()).VirtualMethodWithAnnotatedReturnValue);
			}

			static void LdTokenOnStatic ()
			{
				Expression<Action> _ = () => StaticMethodWithAnnotatedReturnValue ();
			}

			// https://github.com/dotnet/linker/issues/3172
			[ExpectedWarning ("IL2111", nameof (VirtualMethodWithAnnotatedReturnValue), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void LdTokenOnVirtual ()
			{
				Expression<Action<AnnotatedMethodReturnValue>> _ = (a) => a.VirtualMethodWithAnnotatedReturnValue ();
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
				LdTokenOnStatic ();
				LdTokenOnVirtual ();
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
				[ExpectedWarning ("IL2078", nameof (Property5WithAnnotationOnMembers) + ".get", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

			// DynamicDependency is not supported yet in the analyzer https://github.com/dotnet/runtime/issues/83080
			[ExpectedWarning ("IL2111", nameof (Property1WithAnnotation) + ".set", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty3WithAnnotationGetterOnly) + ".get", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".get", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty4WithAnnotation) + ".set", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2111", nameof (Property5WithAnnotationOnMembers) + ".set", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".get", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2111", nameof (VirtualProperty6WithAnnotationOnMembers) + ".set", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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
			// Duplicated warnings for trimming and analyzer see bug https://github.com/dotnet/linker/issues/2462
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
			// Duplicated warnings for trimming and analyzer see bug https://github.com/dotnet/linker/issues/2462
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
			[ExpectedWarning ("IL2110", nameof (Property1WithAnnotation), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void DynamicallyAccessedFields ()
			{
				typeof (AnnotatedProperty).RequiresNonPublicFields ();
			}

			// Action delegate is not handled correctly https://github.com/dotnet/runtime/issues/84918
			[ExpectedWarning ("IL2111", nameof (Property1WithAnnotation), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void LdToken ()
			{
				Expression<Func<Type>> _ = () => Property1WithAnnotation;
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
				LdToken ();
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

			[ExpectedWarning ("IL2091", nameof (GenericWithAnnotation))]
			static void LdToken<TUnknown> ()
			{
				Expression<Action> _ = () => GenericWithAnnotation<TUnknown> ();
			}

			public static void Test ()
			{
				ReflectionOnly ();
				DynamicDependency ();
				DynamicallyAccessedMembers ();
				InstantiateGeneric ();
				DynamicallyAccessedMembersAll ();
				LdToken<string> ();
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

			[ExpectedWarning ("IL2111", nameof (GenericWithAnnotatedMethod<TestType>.AnnotatedMethod))]
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

			[ExpectedWarning ("IL2111", nameof (GenericMethodWithAnnotation))]
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

			// https://github.com/dotnet/linker/issues/3172
			[ExpectedWarning ("IL2111", "GenericWithAnnotatedMethod", "AnnotatedMethod", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void LdToken ()
			{
				// Note that this should warn even though the code looks "Correct"
				// That is because under the hood the expression tree create MethodInfo which is accessible by anything
				// which gets the expression tree as input (so some queryable) and that could invoke the method
				// with a different parameter value and thus violate the requirements.
				Expression<Action> _ = () => GenericWithAnnotatedMethod<TestType>.AnnotatedMethod (typeof (TestType));
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
				LdToken ();
			}
		}

		class AnnotationOnInteropMethod
		{
			struct ValueWithAnnotatedField
			{
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public Type _typeField;
			}

			// Analyzer doesn't take into account interop attributes https://github.com/dotnet/linker/issues/2562
			[ExpectedWarning ("IL2110", nameof (ValueWithAnnotatedField._typeField), ProducedBy = Tool.Trimmer)]
			[DllImport ("nonexistent")]
			static extern ValueWithAnnotatedField GetValueWithAnnotatedField ();

			// Analyzer doesn't take into account interop attributes https://github.com/dotnet/linker/issues/2562
			[ExpectedWarning ("IL2110", nameof (ValueWithAnnotatedField._typeField), ProducedBy = Tool.Trimmer)]
			[DllImport ("nonexistent")]
			static extern void AcceptValueWithAnnotatedField (ValueWithAnnotatedField value);

			public static void Test ()
			{
				GetValueWithAnnotatedField ();
				AcceptValueWithAnnotatedField (default (ValueWithAnnotatedField));
			}
		}

		class DelegateCreation
		{
			delegate void UnannotatedDelegate (Type type);

			static Action<Type> field;

			static Action<Type> Property { get; set; }

			static Action<Type> MethodReturnValue () => null;

			static event Action<Type> Event;

			static void TestField ()
			{
				var d = new UnannotatedDelegate (field);
				d(typeof(int));
			}

			static void TestProperty ()
			{
				var d = new UnannotatedDelegate (Property);
				d(typeof(int));
			}

			[ExpectedWarning ("IL2111")]
			static void TestLambda ()
			{
				var d = new UnannotatedDelegate (
					([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t) =>
					{ });
				d(typeof(int));
			}

			[ExpectedWarning ("IL2111", "LocalMethod")]
			static void TestLocalMethod ()
			{
				var d = new UnannotatedDelegate (LocalMethod);
				d(typeof(int));

				void LocalMethod (
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{ }
			}

			static void TestMethodReturnValue ()
			{
				var d = new UnannotatedDelegate (MethodReturnValue ());
				d(typeof(int));
			}


			static void TestEvent ()
			{
				var d = new UnannotatedDelegate (Event);
				d(typeof(int));
			}

			public static void Test ()
			{
				// Check that the analyzer is able to analyze delegate creation
				// with various targets, without hitting an assert.
				TestField ();
				TestProperty ();
				TestLambda ();
				TestLocalMethod ();
				TestMethodReturnValue ();
				TestEvent ();
			}
		}

		class TestType { }

		[RequiresUnreferencedCode ("--RequiresUnreferencedCodeType--")]
		public class RequiresUnreferencedCodeType { }
	}
}
