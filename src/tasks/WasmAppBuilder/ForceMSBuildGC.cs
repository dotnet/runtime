// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks
{
    public class ForceMSBuildGC : Task
    {
        public override bool Execute()
        {
            Process process = Process.GetCurrentProcess();

            // print current free memory
            Log.LogMessage(MessageImportance.High, $"Before GC {process.Id} - PeakWorkingSet64: {process.PeakWorkingSet64} bytes");
            Log.LogMessage(MessageImportance.High, $"Before GC {process.Id} - PrivateMemorySize64: {process.PrivateMemorySize64} bytes");
            Log.LogMessage(MessageImportance.High, $"Before GC {process.Id} - WorkingSet64: {process.WorkingSet64} bytes");
#if NET
            var info = GC.GetGCMemoryInfo(GCKind.Any);
            Log.LogMessage(MessageImportance.High, $"Before GC {process.Id} - FragmentedBytes: {info.FragmentedBytes} bytes");
            Log.LogMessage(MessageImportance.High, $"Before GC {process.Id} - Compacted: {info.Compacted} bytes");
            Log.LogMessage(MessageImportance.High, $"Before GC {process.Id} - HighMemoryLoadThresholdBytes: {info.HighMemoryLoadThresholdBytes} bytes");
            Log.LogMessage(MessageImportance.High, $"Before GC {process.Id} - TotalAvailableMemoryBytes: {info.TotalAvailableMemoryBytes} bytes");
            Log.LogMessage(MessageImportance.High, $"Before GC {process.Id} - TotalCommittedBytes: {info.TotalCommittedBytes} bytes");
#endif

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Log.LogMessage(MessageImportance.High, $"After GC {process.Id} - PeakWorkingSet64: {process.PeakWorkingSet64} bytes");
            Log.LogMessage(MessageImportance.High, $"After GC {process.Id} - PrivateMemorySize64: {process.PrivateMemorySize64} bytes");
            Log.LogMessage(MessageImportance.High, $"After GC {process.Id} - WorkingSet64: {process.WorkingSet64} bytes");
#if NET
            info = GC.GetGCMemoryInfo(GCKind.Any);
            Log.LogMessage(MessageImportance.High, $"After GC {process.Id} - FragmentedBytes: {info.FragmentedBytes} bytes");
            Log.LogMessage(MessageImportance.High, $"After GC {process.Id} - Compacted: {info.Compacted} bytes");
            Log.LogMessage(MessageImportance.High, $"After GC {process.Id} - HighMemoryLoadThresholdBytes: {info.HighMemoryLoadThresholdBytes} bytes");
            Log.LogMessage(MessageImportance.High, $"After GC {process.Id} - TotalAvailableMemoryBytes: {info.TotalAvailableMemoryBytes} bytes");
            Log.LogMessage(MessageImportance.High, $"After GC {process.Id} - TotalCommittedBytes: {info.TotalCommittedBytes} bytes");
#endif
            return true;
        }
    }
}
