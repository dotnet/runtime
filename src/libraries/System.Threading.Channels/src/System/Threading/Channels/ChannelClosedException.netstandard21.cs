// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Threading.Channels
{
    /// <summary>Exception thrown when a channel is used after it's been closed.</summary>
    [Serializable]
    public partial class ChannelClosedException : InvalidOperationException
    {
        /// <summary>Initializes a new instance of the <see cref="ChannelClosedException"/> class with serialized data.</summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected ChannelClosedException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
