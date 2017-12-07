// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Runtime.ConstrainedExecution.Tests
{
    public class PrePrepareMethodAttributeTests
    {
        public sealed class ConstrainedType
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), PrePrepareMethod]
            public ConstrainedType()
            {
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), PrePrepareMethod]
            public bool SomeMethod() => false;
        }

        [Fact]
        public void SettableOnMethods()
        {
            Assert.NotNull(
                typeof(ConstrainedType).GetMethod(nameof(ConstrainedType.SomeMethod))
                    .GetCustomAttribute<PrePrepareMethodAttribute>());
        }

        [Fact]
        public void SettableOnConstructors()
        {
            Assert.NotNull(
                typeof(ConstrainedType).GetConstructors()[0].GetCustomAttribute<PrePrepareMethodAttribute>());
        }
    }
}
