// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the BuiltInCOM contract.
/// Creates STA-context RCW entries in the global RCW cleanup list so that
/// TraverseRCWCleanupList has non-trivial data to enumerate.
///
/// Strategy:
/// 1. An STA thread disables eager COM cleanup and creates several Shell.Link
///    COM objects (CLSID_ShellLink), then releases all references.
/// 2. The main thread forces GC finalization, which adds the RCWs to
///    g_pRCWCleanupList.  Because eager cleanup is disabled, they remain there.
/// 3. The STA thread's message queue is never pumped, so the cleanup list is
///    never processed before the process crashes.
/// </summary>
internal static class Program
{
    // CLSID_ShellLink — universally available on Windows Desktop/Server editions.
    private static readonly Guid s_clsidShellLink = new Guid("00021401-0000-0000-C000-000000000046");

    private static readonly ManualResetEventSlim s_comReady = new ManualResetEventSlim(false);

    private static void RunSTA()
    {
        // Disable eager cleanup so STA-context RCWs remain in g_pRCWCleanupList
        // after GC finalization rather than being released immediately.
        Thread.CurrentThread.DisableComObjectEagerCleanup();

        try
        {
            Type? shellLinkType = Type.GetTypeFromCLSID(s_clsidShellLink);
            if (shellLinkType is not null)
            {
                // Create and immediately discard several COM objects.
                // Each becomes an RCW added to the cleanup list by the GC finalizer.
                for (int i = 0; i < 3; i++)
                    _ = Activator.CreateInstance(shellLinkType);
            }
        }
        catch
        {
            // COM object unavailable (e.g., Windows Nano Server); continue without it.
        }

        s_comReady.Set();

        // Keep the STA thread alive but idle — intentionally not pumping the
        // message queue so the cleanup list is never processed before the crash.
        Thread.Sleep(Timeout.Infinite);
    }

    private static void Main()
    {
        Thread sta = new Thread(RunSTA) { IsBackground = true };
        sta.SetApartmentState(ApartmentState.STA);
        sta.Start();

        // Wait until the STA thread has created and released the COM objects.
        s_comReady.Wait();

        // Force GC finalization so RCW finalizers run and populate g_pRCWCleanupList.
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Environment.FailFast("cDAC dump test: BuiltInCOM debuggee intentional crash");
    }
}
