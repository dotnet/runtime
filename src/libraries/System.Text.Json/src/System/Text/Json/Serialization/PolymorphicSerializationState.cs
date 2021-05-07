// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    internal enum PolymorphicSerializationState : byte
    {
        None,

        /// <summary>
        /// Dispatch to a polymorphic converter has been initiated.
        /// </summary>
        PolymorphicReEntryStarted,

        /// <summary>
        /// Current frame is a continuation using a suspended polymorphic converter.
        /// </summary>
        PolymorphicReEntrySuspended
    }
}
