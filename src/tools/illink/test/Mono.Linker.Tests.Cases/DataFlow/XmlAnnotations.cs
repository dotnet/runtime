// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SetupLinkAttributesFile ("XmlAnnotations.xml")]
	[ExpectedWarning ("IL2031", "Attribute type 'System.DoesNotExistAttribute' could not be found", FileName = "XmlAnnotations.xml")]
	[LogDoesNotContain ("IL2067: Mono.Linker.Tests.Cases.DataFlow.XmlAnnotations.ReadFromInstanceField():*", true)]
	[ExpectedNoWarnings]
	class XmlAnnotations
	{
		public static void Main ()
		{
			var instance = new XmlAnnotations ();

			instance.ReadFromInstanceField ();
			instance.TwoAnnotatedParameters (typeof (TestType), typeof (TestType));
			instance.SpacesBetweenParametersWrongArgument (typeof (TestType), true);
			instance.GenericMethod<String> ("nonUsed", typeof (TestType));
			instance.ReturnConstructorsFailure (null);
			instance.ReadFromInstanceProperty ();

			var nestedinstance = new NestedType ();

			nestedinstance.ReadFromInstanceField ();
		}

		Type _typeWithPublicParameterlessConstructor;

		Type PropertyWithPublicParameterlessConstructor { get; set; }

		[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		private void ReadFromInstanceField ()
		{
			_typeWithPublicParameterlessConstructor.RequiresPublicParameterlessConstructor ();
			_typeWithPublicParameterlessConstructor.RequiresPublicConstructors ();
			_typeWithPublicParameterlessConstructor.RequiresNonPublicConstructors ();
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		private void TwoAnnotatedParameters (
			Type type,
			Type type2)
		{
			type.RequiresPublicParameterlessConstructor ();
			type2.RequiresPublicParameterlessConstructor ();
			type.RequiresPublicConstructors ();
			type2.RequiresPublicConstructors ();
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicParameterlessConstructor))]
		private void SpacesBetweenParametersWrongArgument (
			Type type,
			bool nonused)
		{
			type.RequiresPublicParameterlessConstructor ();
		}

		private void GenericMethod<T> (
			T input,
			Type type)
		{
			type.RequiresPublicParameterlessConstructor ();
		}

		[ExpectedWarning ("IL2068", nameof (XmlAnnotations) + "." + nameof (ReturnConstructorsFailure))]
		private Type ReturnConstructorsFailure (
			Type publicParameterlessConstructorType)
		{
			return publicParameterlessConstructorType;
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		private void ReadFromInstanceProperty ()
		{
			PropertyWithPublicParameterlessConstructor.RequiresPublicParameterlessConstructor ();
			PropertyWithPublicParameterlessConstructor.RequiresPublicConstructors ();
			PropertyWithPublicParameterlessConstructor.RequiresNonPublicConstructors ();
		}

		class TestType { }

		class NestedType
		{
			Type _typeWithPublicParameterlessConstructor;

			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
			public void ReadFromInstanceField ()
			{
				_typeWithPublicParameterlessConstructor.RequiresPublicParameterlessConstructor ();
				_typeWithPublicParameterlessConstructor.RequiresPublicConstructors ();
				_typeWithPublicParameterlessConstructor.RequiresNonPublicConstructors ();
			}
		}
	}
}
