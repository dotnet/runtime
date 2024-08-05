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
            var runId = Guid.NewGuid().ToString();
            await JSHost.ImportAsync("SecondRuntimeTest", "../SecondRuntimeTest.js?run=" + runId);

            Interop.State = 42;
            var state2 = await Interop.RunSecondRuntimeAndTestStaticState(runId);
            Assert.Equal(44, Interop.State);
            Assert.Equal(3, state2);
        }

        public static partial class Interop
        {
            public static int State { get; set; }

            [JSExport]
            public static int IncrementState() => ++State;

            [JSImport("runSecondRuntimeAndTestStaticState", "SecondRuntimeTest")]
            [return: JSMarshalAs<JSType.Promise<JSType.Number>>]
            internal static partial Task<int> RunSecondRuntimeAndTestStaticState(string guid);
        }
    }
}