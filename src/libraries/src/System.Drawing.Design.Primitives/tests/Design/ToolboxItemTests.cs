// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Drawing.Design.Tests
{
    public class ToolboxItemTests
    {

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_DefaultsAssignedCorrectly()
        {
            var type = typeof(Bitmap);
            var unitUnderTest = new ToolboxItem(type);

            Assert.Equal(type.FullName, unitUnderTest.TypeName);
            Assert.Equal(type.Name, unitUnderTest.DisplayName);
            Assert.Equal(type.Assembly.GetName(true).ToString(), unitUnderTest.AssemblyName.ToString());
            Assert.NotNull(unitUnderTest.DependentAssemblies);
            Assert.Equal(unitUnderTest.Description, "");
            Assert.NotEqual(unitUnderTest.Filter.Count, 0);
        }


        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void ToString_StateUnderTest_ExpectedBehavior()
        {
            var type = typeof(Bitmap);
            var unitUnderTest = new ToolboxItem(type);
            var result = unitUnderTest.ToString();

            Assert.Equal(type.Name, result);
        }
    }
}
