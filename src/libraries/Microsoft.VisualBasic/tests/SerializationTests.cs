// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization.Formatters.Tests;
using Microsoft.VisualBasic.CompilerServices;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class SerializationTests
    {
        [Fact]
        public void SerializeDeserialize_Roundtrip_Success()
        {
            BinaryFormatterHelpers.AssertRoundtrips(new IncompleteInitialization());

            var initFlag = new StaticLocalInitFlag { State = 42 };
            var clonedInitFlag = BinaryFormatterHelpers.Clone(initFlag);
            Assert.Equal(42, clonedInitFlag.State);
        }
    }
}
