// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class SseTests
{
    private static bool RunTests => Sse.IsSupported;
    private static bool Run32BitTests => RunTests && PlatformDetection.Is32BitProcess;
    private static bool Run64BitTests => RunTests && PlatformDetection.Is64BitProcess;
}
