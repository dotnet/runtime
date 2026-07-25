// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Internal.Runtime.Augments
{
    /// <summary>
    /// This helper class is used to access metadata-based resolution of call stack addresses.
    /// To activate the stack trace resolution support, set up an instance of a class
    /// derived from this one using the method
    ///
    /// Internal.Runtime.Augments.RuntimeAugments.InitializeStackTraceMetadataSupport(StackTraceMetadataCallbacks callbacks);
    ///
    /// </summary>
    [CLSCompliant(false)]
    public abstract class StackTraceMetadataCallbacks
    {
        public abstract string TryGetMethodStackFrameInfo(IntPtr methodStartAddress, int offset, bool needsFileInfo, out string owningType, out string genericArgs, out string methodSignature, out bool isStackTraceHidden, out string fileName, out int lineNumber);

        public abstract DiagnosticMethodInfo TryGetDiagnosticMethodInfoFromStartAddress(IntPtr methodStartAddress);
    }
}
