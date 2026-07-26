// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal static class LegacyFallbackHelper
{
    internal static bool CanFallback() => Environment.GetEnvironmentVariable("CDAC_NO_FALLBACK") != "1";
}
