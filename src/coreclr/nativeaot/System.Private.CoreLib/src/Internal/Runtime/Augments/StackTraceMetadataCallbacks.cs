// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Runtime.CompilerServices;

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
        /// <summary>
        /// Helper function to format a given method address using the stack trace metadata.
        /// Return null if stack trace information is not available.
        /// </summary>
        /// <param name="methodStartAddress">Memory address representing the start of a method</param>
        /// <param name="isStackTraceHidden">Returns a value indicating whether the method should be hidden in stack traces</param>
        /// <returns>Formatted method name or null if metadata for the method is not available</returns>
        public abstract string TryGetMethodNameFromStartAddress(IntPtr methodStartAddress, out bool isStackTraceHidden);
    }
}
