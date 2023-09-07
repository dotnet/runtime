// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing
{
    // This is part of the NativeRuntimeEventsource, which is the managed version of the Microsoft-Windows-DotNETRuntime provider.
    // It contains the runtime specific interop to native event sinks.
    //
    // Currently the Binder events are only used by managed binder of CoreCLR.
    internal sealed unsafe partial class NativeRuntimeEventSource : EventSource
    {
        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_GetClrInstanceId")]
        private static partial ushort GetClrInstanceId();

        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ResolutionAttempted(
            string AssemblyName,
            ushort Stage,
            string AssemblyLoadContext,
            ushort Result,
            string ResultAssemblyName,
            string ResultAssemblyPath,
            string ErrorMessage)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.AssemblyLoaderKeyword))
            {
                ResolutionAttempted(GetClrInstanceId(), AssemblyName, Stage, AssemblyLoadContext, Result, ResultAssemblyName, ResultAssemblyPath, ErrorMessage, null, null);
            }
        }

        [Event(292, Level = EventLevel.Informational, Message = "", Task = default, Opcode = default, Version = 0, Keywords = Keywords.AssemblyLoaderKeyword)]
        private unsafe void ResolutionAttempted(
            ushort ClrInstanceId,
            string AssemblyName,
            ushort Stage,
            string AssemblyLoadContext,
            ushort Result,
            string ResultAssemblyName,
            string ResultAssemblyPath,
            string ErrorMessage,
            Guid* ActivityId = null,
            Guid* RelatedActivityId = null)
        {
            LogResolutionAttempted(ClrInstanceId, AssemblyName, Stage, AssemblyLoadContext, Result, ResultAssemblyName, ResultAssemblyPath, ErrorMessage, ActivityId, RelatedActivityId);
        }

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, StringMarshalling = StringMarshalling.Utf16)]
        private static partial void LogResolutionAttempted(
            ushort ClrInstanceId,
            string AssemblyName,
            ushort Stage,
            string AssemblyLoadContext,
            ushort Result,
            string ResultAssemblyName,
            string ResultAssemblyPath,
            string ErrorMessage,
            Guid* ActivityId,
            Guid* RelatedActivityId);

        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void KnownPathProbed(string FilePath, ushort Source, int Result)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.AssemblyLoaderKeyword))
            {
                KnownPathProbed(GetClrInstanceId(), FilePath, Source, Result);
            }
        }

        [Event(296, Level = EventLevel.Informational, Message = "", Task = default, Opcode = default, Version = 0, Keywords = Keywords.AssemblyLoaderKeyword)]
        private unsafe void KnownPathProbed(
            ushort ClrInstanceID,
            string FilePath,
            ushort Source,
            int Result,
            Guid* ActivityId = null,
            Guid* RelatedActivityId = null)
        {
            LogKnownPathProbed(ClrInstanceID, FilePath, Source, Result, ActivityId, RelatedActivityId);
        }

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, StringMarshalling = StringMarshalling.Utf16)]
        private static partial void LogKnownPathProbed(
            ushort ClrInstanceId,
            string FilePath,
            ushort Source,
            int Result,
            Guid* ActivityId,
            Guid* RelatedActivityId);
    }
}
