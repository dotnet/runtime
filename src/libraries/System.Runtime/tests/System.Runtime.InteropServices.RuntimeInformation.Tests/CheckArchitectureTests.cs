// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                    if (IntPtr.Size == 8)
                    {
                        Assert.Equal(Architecture.Arm64, processArch);
                    }
                    else
                    {
                        // armv7/armv6 process running on arm64 host
                        Assert.True(processArch == Architecture.Arm || processArch == Architecture.Armv6, $"Unexpected process architecture: {processArch}");
                    }
                    break;

                case Architecture.Wasm:
                    Assert.Equal(Architecture.Wasm, processArch);
                    break;

                case Architecture.S390x:
                    Assert.Equal(Architecture.S390x, processArch);
                    break;

                case Architecture.LoongArch64:
                    Assert.Equal(Architecture.LoongArch64, processArch);
                    break;

                case Architecture.Armv6:
                    Assert.Equal(Architecture.Armv6, processArch);
                    break;

                case Architecture.Ppc64le:
                    Assert.Equal(Architecture.Ppc64le, processArch);
                    break;

                case Architecture.RiscV64:
                    Assert.Equal(Architecture.RiscV64, processArch);
                    break;

                default:
                    Assert.Fail("Unexpected Architecture.");
                    break;
            }

            Assert.Equal(osArch, RuntimeInformation.OSArchitecture);
            Assert.Equal(processArch, RuntimeInformation.ProcessArchitecture);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Browser)]
        public void VerifyBrowserArchitecture()
        {
            Assert.Equal(Architecture.Wasm, RuntimeInformation.OSArchitecture);
            Assert.Equal(Architecture.Wasm, RuntimeInformation.ProcessArchitecture);
        }
    }
}
