// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;

namespace System.Transactions.DtcProxyShim;

internal static class OletxHelper
{
    private const int RetryInterval = 50;  // in milliseconds
    private const int MaxRetryCount = 100;

    internal const int S_OK = 0;
    internal const int E_FAIL = -2147467259;  // 0x80004005, -2147467259
    internal const int XACT_S_READONLY = 315394;  // 0x0004D002, 315394
    internal const int XACT_S_SINGLEPHASE = 315401;  // 0x0004D009, 315401
    internal const int XACT_E_ABORTED = -2147168231;  // 0x8004D019, -2147168231
    internal const int XACT_E_NOTRANSACTION = -2147168242;  // 0x8004D00E, -2147168242
    internal const int XACT_E_CONNECTION_DOWN = -2147168228;  // 0x8004D01C, -2147168228
    internal const int XACT_E_REENLISTTIMEOUT = -2147168226;  // 0x8004D01E, -2147168226
    internal const int XACT_E_RECOVERYALREADYDONE = -2147167996;  // 0x8004D104, -2147167996
    internal const int XACT_E_TMNOTAVAILABLE = -2147168229; // 0x8004d01b, -2147168229
    internal const int XACT_E_INDOUBT = -2147168234; // 0x8004d016,
    internal const int XACT_E_ALREADYINPROGRESS = -2147168232; // x08004d018,
    internal const int XACT_E_TOOMANY_ENLISTMENTS = -2147167999; // 0x8004d101
    internal const int XACT_E_PROTOCOL = -2147167995; // 8004d105
    internal const int XACT_E_FIRST = -2147168256; // 0x8004D000
    internal const int XACT_E_LAST = -2147168215; // 0x8004D029
    internal const int XACT_E_NOTSUPPORTED = -2147168241; // 0x8004D00F
    internal const int XACT_E_NETWORK_TX_DISABLED = -2147168220; // 0x8004D024

    internal static void Retry(Action action)
    {
        int nRetries = MaxRetryCount;

        while (true)
        {
            try
            {
                action();
                return;
            }
            catch (COMException e) when (e.ErrorCode == XACT_E_ALREADYINPROGRESS)
            {
                if (--nRetries == 0)
                {
                    throw;
                }

                Thread.Sleep(RetryInterval);
            }
        }
    }
}
