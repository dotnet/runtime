// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Runtime.Serialization
{
    // SafeSerializationEventArgs are provided to the delegates which do safe serialization.  Each delegate
    // serializes its own state into an IDeserializationCallback instance which must, itself, be serializable.
    // These indivdiual states are then added to the SafeSerializationEventArgs in order to be saved away when
    // the original ISerializable type is serialized.
    public sealed class SafeSerializationEventArgs : EventArgs
    {
        private readonly List<object> _serializedStates = new List<object>();

        internal SafeSerializationEventArgs() { }

        public void AddSerializedState(ISafeSerializationData serializedState)
        {
            if (serializedState == null)
                throw new ArgumentNullException(nameof(serializedState));
            if (!serializedState.GetType().IsSerializable)
                throw new ArgumentException(SR.Format(SR.Serialization_NonSerType, serializedState.GetType(), serializedState.GetType().Assembly.FullName));

            _serializedStates.Add(serializedState);
        }

        public StreamingContext StreamingContext { get; }
    }
}
