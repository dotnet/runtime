// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Channels
{
    /// <summary>Exception thrown when a channel is used after it's been closed.</summary>
    public partial class ChannelClosedException : InvalidOperationException
    {
        /// <summary>Initializes a new instance of the <see cref="ChannelClosedException"/> class.</summary>
        public ChannelClosedException() :
            base(SR.ChannelClosedException_DefaultMessage) { }

        /// <summary>Initializes a new instance of the <see cref="ChannelClosedException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        public ChannelClosedException(string? message) : base(message ?? SR.ChannelClosedException_DefaultMessage) { }

        /// <summary>Initializes a new instance of the <see cref="ChannelClosedException"/> class.</summary>
        /// <param name="innerException">The exception that is the cause of this exception.</param>
        public ChannelClosedException(Exception? innerException) :
            base(SR.ChannelClosedException_DefaultMessage, innerException) { }

        /// <summary>Initializes a new instance of the <see cref="ChannelClosedException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of this exception.</param>
        public ChannelClosedException(string? message, Exception? innerException) : base(message ?? SR.ChannelClosedException_DefaultMessage, innerException) { }
    }
}
