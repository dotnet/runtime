// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Resources.Tests
{
    public partial class MissingSatelliteAssemblyExceptionTests
    {
        [Fact]
        public void ConstructorSimple()
        {
            MissingSatelliteAssemblyException msae = new MissingSatelliteAssemblyException();
            Assert.NotNull(msae.Message);
        }

        [Fact]
        public void ConstructorWithMessage()
        {
            const string message = "message";
            MissingSatelliteAssemblyException msae = new MissingSatelliteAssemblyException(message);
            Assert.Equal(message, msae.Message);
        }

        [Fact]
        public void ConstructorWithMessageAndCultureName()
        {
            const string message = "message";
            const string cultureName = "fr-FR";
            MissingSatelliteAssemblyException msae = new MissingSatelliteAssemblyException(message, cultureName);
            Assert.Equal(message, msae.Message);
            Assert.Equal(cultureName, msae.CultureName);
        }

        [Fact]
        public void ConstructorWithMessageAndInnerException()
        {
            string message = "message";
            Exception innerException = new Exception();
            MissingSatelliteAssemblyException msae = new MissingSatelliteAssemblyException(message, innerException);
            Assert.Equal(message, msae.Message);
            Assert.Same(innerException, msae.InnerException);
        }

        [Fact]
        public void Serialization()
        {
            const string cultureName = "fr-FR";
            MissingSatelliteAssemblyException msae = new MissingSatelliteAssemblyException("message", cultureName);
            BinaryFormatterHelpers.AssertRoundtrips(msae);
        }
    }
}
