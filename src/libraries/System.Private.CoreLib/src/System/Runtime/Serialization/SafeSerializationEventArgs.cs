// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Runtime.Serialization
{
    // This type exists for public surface compatibility only.
    public sealed class SafeSerializationEventArgs : EventArgs
    {
        private SafeSerializationEventArgs() { }

        public void AddSerializedState(ISafeSerializationData serializedState)
        {
        }

        public StreamingContext StreamingContext { get; }
    }
}
