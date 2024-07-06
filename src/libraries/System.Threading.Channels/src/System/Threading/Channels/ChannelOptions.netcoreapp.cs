// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Threading.Channels
{
    /// <summary>Provides options that control the behavior of instances created by <see cref="M:Channel.CreateUnboundedPrioritized"/>.</summary>
    public sealed class UnboundedPrioritizedChannelOptions<T> : ChannelOptions
    {
        /// <summary>Gets or sets the comparer used by the channel to prioritize elements.</summary>
        public IComparer<T>? Comparer { get; set; }
    }
}
