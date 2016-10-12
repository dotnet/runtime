// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using Xunit;

namespace System.CodeDom.Tests
{
	public abstract class CodeObjectTestBase<T> : CodeDomTestBase where T : CodeObject, new()
	{
		[Fact]
		public void Ctor_Default_ObjectBase()
		{
			CodeObject codeObject = new T();
			Assert.Empty(codeObject.UserData);
		}

		[Fact]
		public void UserData_AddMultiple_ReturnsExpected()
		{
			CodeObject codeObject = new T();
			codeObject.UserData.Add("key1", "value");
			Assert.Equal(new ListDictionary() { ["key1"] = "value" }, codeObject.UserData);

			codeObject.UserData.Add("key2", "value");
			Assert.Equal(new ListDictionary() { ["key1"] = "value", ["key2"] = "value" }, codeObject.UserData);
		}
	}

	public abstract class CodeDomTestBase
	{
		public static IEnumerable<object[]> CodeExpression_TestData()
		{
			yield return new object[] { null };
			yield return new object[] { new CodePrimitiveExpression("Value") };
		}

		public static IEnumerable<object[]> CodeTypeReference_TestData()
		{
			yield return new object[] { null };
			yield return new object[] { new CodeTypeReference(typeof(int)) };
			yield return new object[] { new CodeTypeReference(typeof(int).MakePointerType()) };
			yield return new object[] { new CodeTypeReference(typeof(int).MakeByRefType()) };
			yield return new object[] { new CodeTypeReference(typeof(List<>)) };
			yield return new object[] { new CodeTypeReference(typeof(List<string>)) };
		}

		public static IEnumerable<object[]> CodeStatement_TestData()
		{
			yield return new object[] { null };
			yield return new object[] { new CodeCommentStatement("Text") };
		}

		public static IEnumerable<object[]> String_TestData()
		{
			yield return new object[] { null };
			yield return new object[] { "" };
			yield return new object[] { "Value" };
		}

		public static IEnumerable<object[]> LinePragma_TestData()
		{
			yield return new object[] { null };
			yield return new object[] { new CodeLinePragma("FileName", 1) };
		}
	}
}
