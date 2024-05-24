// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class RequiresViaDataflow
	{
		public static void Main ()
		{
			AnnotatedParameter.Test ();
			AnnotatedGenericParameter.Test ();
			DynamicDependency.Test ();
		}

		class AnnotatedParameter
		{
			static void MethodWithAnnotatedParameter (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{
			}

			public class DynamicallyAccessedTypeWithRequires
			{
				[RequiresUnreferencedCode ("Message for --DynamicallyAccessedTypeWithRequires.MethodWithRequires--")]
				public void MethodWithRequires ()
				{
				}
			}

			[ExpectedWarning ("IL2026", "--DynamicallyAccessedTypeWithRequires.MethodWithRequires--")]
			static void TestNonVirtualMethod ()
			{
				MethodWithAnnotatedParameter (typeof (DynamicallyAccessedTypeWithRequires));
			}

			class BaseType
			{
				[RequiresUnreferencedCode ("Message for --BaseType.VirtualMethodRequires--")]
				[RequiresAssemblyFiles ("Message for --BaseType.VirtualMethodRequires--")]
				[RequiresDynamicCode ("Message for --BaseType.VirtualMethodRequires--")]
				public virtual void VirtualMethodRequires ()
				{
				}
			}

			class TypeWhichOverridesMethod : BaseType
			{
				[RequiresUnreferencedCode ("Message for --TypeWhichOverridesMethod.VirtualMethodRequires--")]
				[RequiresAssemblyFiles ("Message for --TypeWhichOverridesMethod.VirtualMethodRequires--")]
				[RequiresDynamicCode ("Message for --TypeWhichOverridesMethod.VirtualMethodRequires--")]
				public override void VirtualMethodRequires ()
				{
				}
			}

			[ExpectedWarning ("IL2026", "TypeWhichOverridesMethod.VirtualMethodRequires()", "--TypeWhichOverridesMethod.VirtualMethodRequires--")]
			[ExpectedWarning ("IL3002", "TypeWhichOverridesMethod.VirtualMethodRequires()", "--TypeWhichOverridesMethod.VirtualMethodRequires--", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3050", "TypeWhichOverridesMethod.VirtualMethodRequires()", "--TypeWhichOverridesMethod.VirtualMethodRequires--", Tool.NativeAot, "")]
			[ExpectedWarning ("IL2026", "BaseType.VirtualMethodRequires()", "--BaseType.VirtualMethodRequires--")]
			[ExpectedWarning ("IL3002", "BaseType.VirtualMethodRequires()", "--BaseType.VirtualMethodRequires--", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3050", "BaseType.VirtualMethodRequires()", "--BaseType.VirtualMethodRequires--", Tool.NativeAot, "")]
			static void TestOverriddenVirtualMethod ()
			{
				MethodWithAnnotatedParameter (typeof (TypeWhichOverridesMethod));
			}

			public static void Test ()
			{
				TestNonVirtualMethod ();
				TestOverriddenVirtualMethod ();
			}
		}

		class AnnotatedGenericParameter
		{
			class TypeWithRequiresMethod
			{
				[RequiresUnreferencedCode ("--AccessedThroughGenericParameterAnnotation.TypeWithRequiresMethod.MethodWhichRequires--")]
				[RequiresDynamicCode ("--AccessedThroughGenericParameterAnnotation.TypeWithRequiresMethod.MethodWhichRequires--")]
				[RequiresAssemblyFiles ("--AccessedThroughGenericParameterAnnotation.TypeWithRequiresMethod.MethodWhichRequires--")]
				public static void MethodWhichRequires () { }
			}

			class TypeWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T>
			{
				public TypeWithPublicMethods () { }
			}

			[ExpectedWarning ("IL2026", "--AccessedThroughGenericParameterAnnotation.TypeWithRequiresMethod.MethodWhichRequires--")]
			[ExpectedWarning ("IL3002", "--AccessedThroughGenericParameterAnnotation.TypeWithRequiresMethod.MethodWhichRequires--", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3050", "--AccessedThroughGenericParameterAnnotation.TypeWithRequiresMethod.MethodWhichRequires--", Tool.NativeAot, "")]
			static void TestAccessOnGenericType ()
			{
				new TypeWithPublicMethods<TypeWithRequiresMethod> ();
			}

			static void MethodWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> () { }

			[ExpectedWarning ("IL2026", "--AccessedThroughGenericParameterAnnotation.TypeWithRequiresMethod.MethodWhichRequires--")]
			[ExpectedWarning ("IL3002", "--AccessedThroughGenericParameterAnnotation.TypeWithRequiresMethod.MethodWhichRequires--", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3050", "--AccessedThroughGenericParameterAnnotation.TypeWithRequiresMethod.MethodWhichRequires--", Tool.NativeAot, "")]
			static void TestAccessOnGenericMethod ()
			{
				MethodWithPublicMethods<TypeWithRequiresMethod> ();
			}

			static void MethodWithPublicMethodsInference<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> (T instance) { }

			[ExpectedWarning ("IL2026", "--AccessedThroughGenericParameterAnnotation.TypeWithRequiresMethod.MethodWhichRequires--")]
			[ExpectedWarning ("IL3002", "--AccessedThroughGenericParameterAnnotation.TypeWithRequiresMethod.MethodWhichRequires--", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3050", "--AccessedThroughGenericParameterAnnotation.TypeWithRequiresMethod.MethodWhichRequires--", Tool.NativeAot, "")]
			static void TestAccessOnGenericMethodWithInferenceOnMethod ()
			{
				MethodWithPublicMethodsInference (new TypeWithRequiresMethod ());
			}

			public static void Test ()
			{
				TestAccessOnGenericType ();
				TestAccessOnGenericMethod ();
				TestAccessOnGenericMethodWithInferenceOnMethod ();
			}
		}

		class DynamicDependency
		{
			[RequiresUnreferencedCode ("Message for --RequiresInDynamicDependency--")]
			[RequiresAssemblyFiles ("Message for --RequiresInDynamicDependency--")]
			[RequiresDynamicCode ("Message for --RequiresInDynamicDependency--")]
			static void RequiresInDynamicDependency ()
			{
			}

			// Analyzer doesn't recognize DynamicDependency in any way
			[ExpectedWarning ("IL2026", "--RequiresInDynamicDependency--", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/83080")]
			[ExpectedWarning ("IL3002", "--RequiresInDynamicDependency--", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/83080")]
			[ExpectedWarning ("IL3050", "--RequiresInDynamicDependency--", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/83080")]
			[DynamicDependency ("RequiresInDynamicDependency")]
			public static void Test ()
			{
			}
		}
	}
}
