// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.InteropServices.RuntimeInformationTests
{
    public class CheckArchitectureTests
    {
        [Fact]
        public void VerifyArchitecture()
        {
            Architecture osArch = RuntimeInformation.OSArchitecture;
            Architecture processArch = RuntimeInformation.ProcessArchitecture;

            switch (osArch)
            {
                case Architecture.X64:
                    Assert.NotEqual(Architecture.Arm, processArch);
                    break;

                case Architecture.X86:
                    Assert.Equal(Architecture.X86, processArch);
                    break;

                case Architecture.Arm:
                    Assert.Equal(Architecture.Arm, processArch);
                    break;

                default:
                    Assert.False(true, "Unexpected Architecture.");
                    break;
            }
        }
    }
}
