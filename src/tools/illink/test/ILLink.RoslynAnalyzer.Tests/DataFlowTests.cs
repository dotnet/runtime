// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class DataFlowTests : LinkerTestBase
	{
		protected override string TestSuiteName => "DataFlow";

		[Fact]
		public Task AnnotatedMembersAccessedViaReflection ()
		{
			return RunTest (nameof (AnnotatedMembersAccessedViaReflection));
		}

		[Fact]
		public Task AnnotatedMembersAccessedViaUnsafeAccessor ()
		{
			return RunTest ();
		}

		[Fact]
		public Task ApplyTypeAnnotations ()
		{
			return RunTest ();
		}

		[Fact]
		public Task AssemblyQualifiedNameDataflow ()
		{
			return RunTest (nameof (AssemblyQualifiedNameDataflow));
		}

		[Fact]
		public Task ArrayDataFlow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task AttributeConstructorDataflow ()
		{
			return RunTest (nameof (AttributeConstructorDataflow));
		}

		[Fact]
		public Task AttributeFieldDataflow ()
		{
			return RunTest (nameof (AttributeFieldDataflow));
		}

		[Fact]
		public Task AttributePropertyDataflow ()
		{
			return RunTest (nameof (AttributePropertyDataflow));
		}

		[Fact]
		public Task ByRefDataflow ()
		{
			return RunTest (nameof (ByRefDataflow));
		}

		[Fact]
		public Task CompilerGeneratedCodeDataflow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task CompilerGeneratedCodeInPreservedAssembly ()
		{
			return RunTest ();
		}

		[Fact]
		public Task CompilerGeneratedCodeInPreservedAssemblyWithWarning ()
		{
			return RunTest ();
		}

		[Fact]
		public Task CompilerGeneratedTypes ()
		{
			return RunTest ();
		}

		[Fact]
		public Task CompilerGeneratedTypesRelease ()
		{
			return RunTest ();
		}

		[Fact]
		public Task ComplexTypeHandling ()
		{
			return RunTest ();
		}

		[Fact]
		public Task CompilerGeneratedCodeAccessedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task ConstructedTypesDataFlow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task ConstructorDataFlow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task DynamicDependencyDataflow ()
		{
			return RunTest (nameof (DynamicDependencyDataflow));
		}

		[Fact]
		public Task DynamicObjects ()
		{
			return RunTest ();
		}

		[Fact]
		public Task EmptyArrayIntrinsicsDataFlow ()
		{
			// https://github.com/dotnet/linker/issues/2273
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EventDataFlow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task FeatureCheckDataFlow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task FieldDataFlow ()
		{
			return RunTest (nameof (FieldDataFlow));
		}

		[Fact]
		public Task FileScopedClasses ()
		{
			return RunTest ();
		}

		[Fact]
		public Task GenericParameterDataFlow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task GenericParameterWarningLocation ()
		{
			return RunTest ();
		}

		[Fact]
		public Task InlineArrayDataflow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task InterpolatedStringHandlerDataFlow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task MakeGenericDataFlow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task MethodByRefReturnDataFlow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task GetInterfaceDataFlow ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GetNestedTypeOnAllAnnotatedType ()
		{
			// https://github.com/dotnet/linker/issues/2273
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GetTypeDataFlow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task GetTypeInfoDataFlow ()
		{
			return RunTest (nameof (GetTypeInfoDataFlow));
		}

		[Fact]
		public Task TypeInfoAsTypeDataFlow ()
		{
			return RunTest (nameof (TypeInfoAsTypeDataFlow));
		}

		[Fact]
		public Task TypeHandleDataFlow ()
		{
			return RunTest (nameof (TypeHandleDataFlow));
		}

		[Fact]
		public Task IReflectDataflow ()
		{
			return RunTest (nameof (IReflectDataflow));
		}

		[Fact]
		public Task LocalDataFlow ()
		{
			return RunTest (nameof (LocalDataFlow));
		}


		[Fact]
		public Task ExceptionalDataFlow ()
		{
			return RunTest (nameof (ExceptionalDataFlow));
		}

		[Fact]
		public Task LocalDataFlowKeptMembers ()
		{
			return RunTest (nameof (LocalDataFlowKeptMembers));
		}

		[Fact]
		public Task MemberTypes ()
		{
			return RunTest (nameof (MemberTypes));
		}

		[Fact]
		public Task MemberTypesAllOnCopyAssembly ()
		{
			return RunTest (nameof (MemberTypesAllOnCopyAssembly));
		}

		[Fact]
		public Task MemberTypesRelationships ()
		{
			return RunTest (nameof (MemberTypesRelationships));
		}

		[Fact]
		public Task MethodParametersDataFlow ()
		{
			return RunTest (nameof (MethodParametersDataFlow));
		}

		[Fact]
		public Task MethodReturnParameterDataFlow ()
		{
			return RunTest (nameof (MethodReturnParameterDataFlow));
		}

		[Fact]
		public Task MethodThisDataFlow ()
		{
			return RunTest (nameof (MethodThisDataFlow));
		}

		[Fact]
		public Task NullableAnnotations ()
		{
			return RunTest ();
		}

		[Fact]
		public Task PropertyDataFlow ()
		{
			return RunTest (nameof (PropertyDataFlow));
		}

		[Fact]
		public Task RefFieldDataFlow ()
		{
			return RunTest (nameof (RefFieldDataFlow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task SuppressWarningWithLinkAttributes ()
		{
			return RunTest (nameof (SuppressWarningWithLinkAttributes));
		}

		[Fact]
		public Task TypeBaseTypeDataFlow ()
		{
			return RunTest ();
		}

		[Fact]
		public Task UnresolvedMembers ()
		{
			// https://github.com/dotnet/linker/issues/2273
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task VirtualMethodHierarchyDataflowAnnotationValidation ()
		{
			return RunTest (nameof (VirtualMethodHierarchyDataflowAnnotationValidation));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task XmlAnnotations ()
		{
			return RunTest (nameof (XmlAnnotations));
		}
	}
}
