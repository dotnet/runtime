// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CompilerErrorTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var error = new CompilerError();
            Assert.Equal(0, error.Column);
            Assert.Empty(error.ErrorNumber);
            Assert.Empty(error.ErrorText);
            Assert.Empty(error.FileName);
            Assert.False(error.IsWarning);
            Assert.Equal(0, error.Line);
        }

        [Theory]
        [InlineData(null, 0, 0, null, null)]
        [InlineData("", -1, -1, "", "")]
        [InlineData("fileName", 1, 1, "errorNumber", "errorText")]
        public void Ctor_String_Int_Int_String_String(string fileName, int line, int column, string errorNumber, string errorText)
        {
            var error = new CompilerError(fileName, line, column, errorNumber, errorText);
            Assert.Equal(column, error.Column);
            Assert.Equal(errorNumber, error.ErrorNumber);
            Assert.Equal(errorText, error.ErrorText);
            Assert.Equal(fileName, error.FileName);
            Assert.False(error.IsWarning);
            Assert.Equal(line, error.Line);
        }

        [Theory]
        [InlineData(true, "warning : ")]
        [InlineData(false, "error : ")]
        public void ToString_Invoke_ReturnsExpected(bool isWarning, string expected)
        {
            var error = new CompilerError { IsWarning = isWarning };
            Assert.Equal(expected, error.ToString());
        }
    }
}
