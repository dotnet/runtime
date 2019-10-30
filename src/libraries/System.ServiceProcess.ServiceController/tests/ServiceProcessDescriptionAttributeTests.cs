// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ServiceProcess.Tests
{
    public class ServiceProcessDescriptionAttributeTests
    {
        public static TheoryData<string> Ctor_Data => new TheoryData<string>
        {
            { string.Empty },
            { null },
            { "hello" }
        };

        [Theory,
            MemberData(nameof(Ctor_Data))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void CtorAndGetDescription_test(string input) => Assert.Equal(input, new ServiceProcessDescriptionAttribute(input).Description);
    }
}
