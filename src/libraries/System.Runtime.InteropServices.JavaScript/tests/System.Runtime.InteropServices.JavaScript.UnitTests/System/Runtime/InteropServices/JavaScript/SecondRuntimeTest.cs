// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public partial class SecondRuntimeTest
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowserDomSupportedOrNodeJS))]
        public static async Task RunSecondRuntimeAndTestStaticState() 
        {
            await JSHost.ImportAsync("SecondRuntimeTest", "./SecondRuntimeTest.js");

            var result = await Interop.RunSecondRuntimeAndTestStaticState();
            Assert.True(result);
        }

        public static partial class Interop
        {
            private static int state = 0;

            [JSExport]
            public static int IncrementState() => ++state;

            [JSImport("runSecondRuntimeAndTestStaticState", "SecondRuntimeTest")]
            [return: JSMarshalAs<JSType.Promise<JSType.Boolean>>]
            internal static partial Task<bool> RunSecondRuntimeAndTestStaticState();
        }
    }
}