// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sample
{
    public partial class FrameApp
    {
        [JSImport("globalThis.frameApp.ReachedCallback")]
        public static partial Task ReachedCallback();

        public static void Main()
        {
            ReachedCallback();
        }
    }
}
