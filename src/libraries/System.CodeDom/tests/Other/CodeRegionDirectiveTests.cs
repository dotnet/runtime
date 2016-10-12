// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeRegionDirectiveTests : CodeDomTestBase
	{
		[Fact]
		public void Ctor_Default()
		{
			var region = new CodeRegionDirective();
			Assert.Equal(CodeRegionMode.None, region.RegionMode);
			Assert.Empty(region.RegionText);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { CodeRegionMode.None - 1, null };
			yield return new object[] { CodeRegionMode.None, "" };
			yield return new object[] { CodeRegionMode.End + 1, "RegionText" };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor_CodeRegionMode_String(CodeRegionMode regionMode, string regionText)
		{
			var region = new CodeRegionDirective(regionMode, regionText);
			Assert.Equal(regionMode, region.RegionMode);
			Assert.Equal(regionText ?? string.Empty, region.RegionText);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void RegionText_Set_Get_ReturnsExpected(string value)
		{
			var region = new CodeRegionDirective();
			region.RegionText = value;
			Assert.Equal(value ?? string.Empty, region.RegionText);
		}

		[Theory]
		[InlineData(CodeRegionMode.None - 1)]
		[InlineData(CodeRegionMode.None)]
		[InlineData(CodeRegionMode.Start | CodeRegionMode.End)]
		[InlineData(CodeRegionMode.End + 1)]
		public void Value_Set_Get_ReturnsExpected(CodeRegionMode value)
		{
			var region = new CodeRegionDirective();
			region.RegionMode = value;
			Assert.Equal(value, region.RegionMode);
		}
	}
}
