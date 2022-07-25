// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmSIMDTests : WasmBuildAppBase
    {
        public WasmSIMDTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public void BuildWithSIMD(BuildArgs buildArgs, RunHost host, string id)
            => TestMain("main_simd_aot",
                @"
                using System;
                using System.Runtime.Intrinsics;

                public class TestClass {
                    public static int Main()
                    {
                        var v1 = Vector128.Create(0x12345678);
                        var v2 = Vector128.Create(0x23456789);
                        var v3 = v1*v2;
                        Console.WriteLine(v3);
                        Console.WriteLine(""Hello, World!"");

                        return 42;
                    }
                }",
                buildArgs, host, id, extraProperties: "<WasmSIMD>true</WasmSIMD>");
    }
}
