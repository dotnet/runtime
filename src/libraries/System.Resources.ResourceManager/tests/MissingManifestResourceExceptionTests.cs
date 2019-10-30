// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Resources.Tests
{
    public partial class MissingManifestResourceExceptionTests
    {
        [Fact]
        public static void ConstructorSimple()
        {
            MissingManifestResourceException mmre = new MissingManifestResourceException();
            Assert.NotNull(mmre.Message);
        }

        [Fact]
        public static void ConstructorWithMessage()
        {
            string message = "message";
            MissingManifestResourceException mmre = new MissingManifestResourceException(message);
            Assert.Equal(message, mmre.Message);
        }

        [Fact]
        public static void ConstructorWithMessageAndInnerException()
        {
            string message = "message";
            Exception innerException = new Exception();
            MissingManifestResourceException mmre = new MissingManifestResourceException(message, innerException);
            Assert.Equal(message, mmre.Message);
            Assert.Same(innerException, mmre.InnerException);
        }
    }
}
