// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Collections.Generic;

namespace System.Threading.Channels
{
    public partial class Channel
    {
        public static System.Threading.Channels.Channel<T> CreateUnboundedPrioritized<T>() { throw null; }
        public static System.Threading.Channels.Channel<T> CreateUnboundedPrioritized<T>(System.Threading.Channels.UnboundedPrioritizedChannelOptions<T> options) { throw null; }
    }
    public sealed partial class UnboundedPrioritizedChannelOptions<T> : System.Threading.Channels.ChannelOptions
    {
        public System.Collections.Generic.IComparer<T>? Comparer { get; set; }
    }
}
