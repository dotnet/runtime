// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    internal enum PolymorphicSerializationState : byte
    {
        None,

        /// <summary>
        /// Dispatch to a derived converter has been initiated.
        /// </summary>
        PolymorphicReEntryStarted,

        /// <summary>
        /// Current frame is a continuation using a suspended derived converter.
        /// </summary>
        PolymorphicReEntrySuspended,

        /// <summary>
        /// Current frame is a polymorphic converter that couldn't resolve a derived converter.
        /// (E.g. because the runtime type matches the declared type).
        /// </summary>
        PolymorphicReEntryNotFound
    }
}
