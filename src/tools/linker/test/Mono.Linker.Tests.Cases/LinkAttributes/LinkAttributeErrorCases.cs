using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SetupLinkAttributesFile ("LinkAttributeErrorCases.xml")]
	[IgnoreLinkAttributes (false)]
	[SetupLinkerArgument ("--skip-unresolved", "true")]

	[ExpectedWarning ("IL2007", "NonExistentAssembly2", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2030", "NonExistentAssembly1", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2030", "MalformedAssemblyName, thisiswrong", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2031", "NonExistentAttribute", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2054", "NonExistentEnumValue", "AttributeWithEnumParameterAttribute", "AttributeEnum", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2054", "NotANumber", "AttributeWithIntParameterAttribute", "System.Int32", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2022", "AttributeWithNoParametersAttribute", "1", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2023", "GetTypeMethod", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2024", "methodParameter", "MethodWithParameter", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2029", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2029", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2051", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2052", "NonExistentPropertyName", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2053", "StringValue", "IntProperty", FileName = "LinkAttributeErrorCases.xml")]
	class LinkAttributeErrorCases
	{
		public static void Main ()
		{

		}

		public enum AttributeEnum
		{
			None
		}

		public class AttributeWithEnumParameterAttribute : Attribute
		{
			public AttributeWithEnumParameterAttribute (AttributeEnum enumValue)
			{
			}
		}

		public class AttributeWithIntParameterAttribute : Attribute
		{
			public AttributeWithIntParameterAttribute (int intValue)
			{
			}
		}

		public class AttributeWithNoParametersAttribute : Attribute
		{
			public AttributeWithNoParametersAttribute ()
			{
			}
		}

		public class AttributeWithPropertyAttribute : Attribute
		{
			public AttributeWithPropertyAttribute ()
			{
			}

			int IntProperty { get; }
		}

		public class FirstAttribute : Attribute { }
		public class SecondAttribute : Attribute { }

		public Type GetTypeMethod () => null;

		public void MethodWithParameter (int methodParameter) { }
	}
}
