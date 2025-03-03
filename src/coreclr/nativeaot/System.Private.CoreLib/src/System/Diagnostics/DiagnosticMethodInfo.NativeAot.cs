// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Runtime.Augments;

namespace System.Diagnostics
{
    public sealed partial class DiagnosticMethodInfo
    {
        // Public for System.Private.StackTraceMetadata sake
        public DiagnosticMethodInfo(string name, string declaringTypeName, string declaringAssemblyName)
            => (Name, DeclaringTypeName, DeclaringAssemblyName) = (name, declaringTypeName, declaringAssemblyName);

        public string Name { get; }

        public string? DeclaringTypeName { get; }

        public string? DeclaringAssemblyName { get; }

        public static DiagnosticMethodInfo? Create(Delegate @delegate)
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            return @delegate.GetDiagnosticMethodInfo();
        }

        public static DiagnosticMethodInfo? Create(StackFrame frame)
        {
            ArgumentNullException.ThrowIfNull(frame);
            return frame.TryGetMethodStartAddress(out IntPtr startAddress)
                ? RuntimeAugments.StackTraceCallbacksIfAvailable?.TryGetDiagnosticMethodInfoFromStartAddress(startAddress)
                : null;
        }
    }
}
