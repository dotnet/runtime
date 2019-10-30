// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

                case Architecture.Arm64:
                    Assert.Equal(IntPtr.Size == 4 ? Architecture.Arm : Architecture.Arm64, processArch);
                    break;

                default:
                    Assert.False(true, "Unexpected Architecture.");
                    break;
            }

            Assert.Equal(osArch, RuntimeInformation.OSArchitecture);
            Assert.Equal(processArch, RuntimeInformation.ProcessArchitecture);
        }
    }
}
