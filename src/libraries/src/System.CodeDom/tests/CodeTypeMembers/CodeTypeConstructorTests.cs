// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeTypeConstructorTests : CodeObjectTestBase<CodeTypeConstructor>
	{
		[Fact]
		public void Ctor_Default()
		{
			var constructor = new CodeTypeConstructor();
			Assert.Equal(".cctor", constructor.Name);
		}
	}
}
