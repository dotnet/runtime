// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal static class FallbackHelper
{
    private const string CDAC_DISABLE_FALLBACK_ENV_VAR = "CDAC_DISABLE_FALLBACK";
    public static int Fallback(Func<int> legacyImpl)
    {
        if (IsFallbackDisabled())
        {
            return HResults.E_NOTIMPL;
        }

        return legacyImpl();
    }

    private static bool IsFallbackDisabled()
    => Environment.GetEnvironmentVariable(CDAC_DISABLE_FALLBACK_ENV_VAR) == "1";
}
