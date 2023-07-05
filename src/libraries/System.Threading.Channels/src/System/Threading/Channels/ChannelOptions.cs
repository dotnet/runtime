// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Channels
{
    /// <summary>Provides options that control the behavior of channel instances.</summary>
    public abstract class ChannelOptions
    {
        /// <summary>
        /// <code>true</code> if writers to the channel guarantee that there will only ever be at most one write operation
        /// at a time; <code>false</code> if no such constraint is guaranteed.
        /// </summary>
        /// <remarks>
        /// If true, the channel may be able to optimize certain operations based on knowing about the single-writer guarantee.
        /// The default is false.
        /// </remarks>
        public bool SingleWriter { get; set; }

        /// <summary>
        /// <code>true</code> if readers from the channel guarantee that there will only ever be at most one read operation
        /// at a time; <code>false</code> if no such constraint is guaranteed.
        /// </summary>
        /// <remarks>
        /// If true, the channel may be able to optimize certain operations based on knowing about the single-reader guarantee.
        /// The default is false.
        /// </remarks>
        public bool SingleReader { get; set; }

        /// <summary>
        /// <code>true</code> if operations performed on a channel may synchronously invoke continuations subscribed to
        /// notifications of pending async operations; <code>false</code> if all continuations should be invoked asynchronously.
        /// </summary>
        /// <remarks>
        /// Setting this option to <code>true</code> can provide measurable throughput improvements by avoiding
        /// scheduling additional work items. However, it may come at the cost of reduced parallelism, as for example a producer
        /// may then be the one to execute work associated with a consumer, and if not done thoughtfully, this can lead
        /// to unexpected interactions. The default is false.
        /// </remarks>
        public bool AllowSynchronousContinuations { get; set; }
    }

    /// <summary>Provides options that control the behavior of <see cref="BoundedChannel{T}"/> instances.</summary>
    public sealed class BoundedChannelOptions : ChannelOptions
    {
        /// <summary>The maximum number of items the bounded channel may store.</summary>
        private int _capacity;
        /// <summary>The behavior incurred by write operations when the channel is full.</summary>
        private BoundedChannelFullMode _mode = BoundedChannelFullMode.Wait;

        /// <summary>Initializes the options.</summary>
        /// <param name="capacity">The maximum number of items the bounded channel may store.</param>
        public BoundedChannelOptions(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
        }

        /// <summary>Gets or sets the maximum number of items the bounded channel may store.</summary>
        public int Capacity
        {
            get => _capacity;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _capacity = value;
            }
        }

        /// <summary>Gets or sets the behavior incurred by write operations when the channel is full.</summary>
        public BoundedChannelFullMode FullMode
        {
            get => _mode;
            set
            {
                switch (value)
                {
                    case BoundedChannelFullMode.Wait:
                    case BoundedChannelFullMode.DropNewest:
                    case BoundedChannelFullMode.DropOldest:
                    case BoundedChannelFullMode.DropWrite:
                        _mode = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value));
                }
            }
        }
    }

    /// <summary>Provides options that control the behavior of <see cref="UnboundedChannel{T}"/> instances.</summary>
    public sealed class UnboundedChannelOptions : ChannelOptions
    {
    }
}
