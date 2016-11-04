// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Resources.Tests
{
    public partial class MissingManifestResourceExceptionTests
    {
        [Fact]
        public void Serialization()
        {
            const string message = "FATAL ERROR: The pizza could not be found.";
            var ex = new MissingManifestResourceException(message);
            BinaryFormatterHelpers.AssertRoundtrips(ex);
        }
    }
}
