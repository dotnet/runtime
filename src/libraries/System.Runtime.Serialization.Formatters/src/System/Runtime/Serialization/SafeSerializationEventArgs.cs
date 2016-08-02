// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Runtime.Serialization
{
    public sealed class SafeSerializationEventArgs : EventArgs
    {
        private readonly StreamingContext _streamingContext;

        internal SafeSerializationEventArgs(StreamingContext streamingContext)
        {
            _streamingContext = streamingContext;
        }

        public void AddSerializedState(ISafeSerializationData serializedState)
        {
            if (serializedState == null)
            {
                throw new ArgumentNullException(nameof(serializedState));
            }
            if (!serializedState.GetType().GetTypeInfo().IsSerializable)
            {
                throw new ArgumentException(SR.Format(SR.Serialization_NonSerType, serializedState.GetType(), serializedState.GetType().GetTypeInfo().Assembly.FullName));
            }

            // nop
        }

        public StreamingContext StreamingContext => _streamingContext;
    }

    public interface ISafeSerializationData
    {
        // CompleteDeserialization is called when the object to which the extra serialized data was attached
        // has completed its deserialization, and now needs to be populated with the extra data stored in
        // this object.
        void CompleteDeserialization(object deserialized);
    }
}
