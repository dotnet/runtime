// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class COMExceptionTests
    {
        [ActiveIssue(12467)]
        [Fact]
        public void SerializationRoundTrip()
        {
            var ex = new COMException("E_BAD_PIZZA", -1337);
            BinaryFormatterHelpers.AssertRoundtrips(ex);
        }
    }
}
