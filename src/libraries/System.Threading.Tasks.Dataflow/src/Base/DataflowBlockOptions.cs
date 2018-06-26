// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// DataflowBlockOptions.cs
//
//
// DataflowBlockOptions types for configuring dataflow blocks
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>
    /// Provides options used to configure the processing performed by dataflow blocks.
    /// </summary>
    /// <remarks>
    /// <see cref="DataflowBlockOptions"/> is mutable and can be configured through its properties.  
    /// When specific configuration options are not set, the following defaults are used:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Options</term>
    ///         <description>Default</description>
    ///     </listheader>
    ///     <item>
    ///         <term>TaskScheduler</term>
    ///         <description><see cref="System.Threading.Tasks.TaskScheduler.Default"/></description>
    ///     </item>
    ///     <item>
    ///         <term>MaxMessagesPerTask</term>
    ///         <description>DataflowBlockOptions.Unbounded (-1)</description>
    ///     </item>
    ///     <item>
    ///         <term>CancellationToken</term>
    ///         <description><see cref="System.Threading.CancellationToken.None"/></description>
    ///     </item>
    ///     <item>
    ///         <term>BoundedCapacity</term>
    ///         <description>DataflowBlockOptions.Unbounded (-1)</description>
    ///     </item>
    ///     <item>
    ///         <term>NameFormat</term>
    ///         <description>"{0} Id={1}"</description>
    ///     </item>
    ///     <item>
    ///         <term>EnsureOrdered</term>
    ///         <description>true</description>
    ///     </item>
    /// </list>
    /// Dataflow blocks capture the state of the options at their construction.  Subsequent changes
    /// to the provided <see cref="DataflowBlockOptions"/> instance should not affect the behavior
    /// of a dataflow block.
    /// </remarks>
    [DebuggerDisplay("TaskScheduler = {TaskScheduler}, MaxMessagesPerTask = {MaxMessagesPerTask}, BoundedCapacity = {BoundedCapacity}")]
    public class DataflowBlockOptions
    {
        /// <summary>
        /// A constant used to specify an unlimited quantity for <see cref="DataflowBlockOptions"/> members 
        /// that provide an upper bound. This field is constant.
        /// </summary>
        public const int Unbounded = -1;

        /// <summary>The scheduler to use for scheduling tasks to process messages.</summary>
        private TaskScheduler _taskScheduler = TaskScheduler.Default;
        /// <summary>The cancellation token to monitor for cancellation requests.</summary>
        private CancellationToken _cancellationToken = CancellationToken.None;
        /// <summary>The maximum number of messages that may be processed per task.</summary>
        private int _maxMessagesPerTask = Unbounded;
        /// <summary>The maximum number of messages that may be buffered by the block.</summary>
        private int _boundedCapacity = Unbounded;
        /// <summary>The name format to use for creating a name for a block.</summary>
        private string _nameFormat = "{0} Id={1}"; // see NameFormat property for a description of format items
        /// <summary>Whether to force ordered processing of messages.</summary>
        private bool _ensureOrdered = true;

        /// <summary>A default instance of <see cref="DataflowBlockOptions"/>.</summary>
        /// <remarks>
        /// Do not change the values of this instance.  It is shared by all of our blocks when no options are provided by the user.
        /// </remarks>
        internal static readonly DataflowBlockOptions Default = new DataflowBlockOptions();

        /// <summary>Returns this <see cref="DataflowBlockOptions"/> instance if it's the default instance or else a cloned instance.</summary>
        /// <returns>An instance of the options that may be cached by the block.</returns>
        internal DataflowBlockOptions DefaultOrClone()
        {
            return (this == Default) ?
                this :
                new DataflowBlockOptions
                {
                    TaskScheduler = this.TaskScheduler,
                    CancellationToken = this.CancellationToken,
                    MaxMessagesPerTask = this.MaxMessagesPerTask,
                    BoundedCapacity = this.BoundedCapacity,
                    NameFormat = this.NameFormat,
                    EnsureOrdered = this.EnsureOrdered
                };
        }

        /// <summary>Initializes the <see cref="DataflowBlockOptions"/>.</summary>
        public DataflowBlockOptions() { }

        /// <summary>Gets or sets the <see cref="System.Threading.Tasks.TaskScheduler"/> to use for scheduling tasks.</summary>
        public TaskScheduler TaskScheduler
        {
            get { return _taskScheduler; }
            set
            {
                Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
                if (value == null) throw new ArgumentNullException(nameof(value));
                _taskScheduler = value;
            }
        }

        /// <summary>Gets or sets the <see cref="System.Threading.CancellationToken"/> to monitor for cancellation requests.</summary>
        public CancellationToken CancellationToken
        {
            get { return _cancellationToken; }
            set
            {
                Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
                _cancellationToken = value;
            }
        }

        /// <summary>Gets or sets the maximum number of messages that may be processed per task.</summary>
        public int MaxMessagesPerTask
        {
            get { return _maxMessagesPerTask; }
            set
            {
                Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
                if (value < 1 && value != Unbounded) throw new ArgumentOutOfRangeException(nameof(value));
                _maxMessagesPerTask = value;
            }
        }

        /// <summary>Gets a MaxMessagesPerTask value that may be used for comparison purposes.</summary>
        /// <returns>The maximum value, usable for comparison purposes.</returns>
        /// <remarks>Unlike MaxMessagesPerTask, this property will always return a positive value.</remarks>
        internal int ActualMaxMessagesPerTask
        {
            get { return (_maxMessagesPerTask == Unbounded) ? int.MaxValue : _maxMessagesPerTask; }
        }

        /// <summary>Gets or sets the maximum number of messages that may be buffered by the block.</summary>
        public int BoundedCapacity
        {
            get { return _boundedCapacity; }
            set
            {
                Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
                if (value < 1 && value != Unbounded) throw new ArgumentOutOfRangeException(nameof(value));
                _boundedCapacity = value;
            }
        }

        /// <summary>
        /// Gets or sets the format string to use when a block is queried for its name.
        /// </summary>
        /// <remarks>
        /// The name format may contain up to two format items. {0} will be substituted 
        /// with the block's name. {1} will be substituted with the block's Id, as is 
        /// returned from the block's Completion.Id property.
        /// </remarks>
        public string NameFormat
        {
            get { return _nameFormat; }
            set
            {
                Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
                if (value == null) throw new ArgumentNullException(nameof(value));
                _nameFormat = value;
            }
        }

        /// <summary>Gets or sets whether ordered processing should be enforced on a block's handling of messages.</summary>
        /// <remarks>
        /// By default, dataflow blocks enforce ordering on the processing of messages. This means that a
        /// block like <see cref="TransformBlock{TInput, TOutput}"/> will ensure that messages are output in the same
        /// order they were input, even if parallelism is employed by the block and the processing of a message N finishes 
        /// after the processing of a subsequent message N+1 (the block will reorder the results to maintain the input
        /// ordering prior to making those results available to a consumer).  Some blocks may allow this to be relaxed,
        /// however.  Setting <see cref="EnsureOrdered"/> to false tells a block that it may relax this ordering if
        /// it's able to do so.  This can be beneficial if the immediacy of a processed result being made available
        /// is more important than the input-to-output ordering being maintained.
        /// </remarks>
        public bool EnsureOrdered
        {
            get { return _ensureOrdered; }
            set { _ensureOrdered = value; }
        }
    }

    /// <summary>
    /// Provides options used to configure the processing performed by dataflow blocks that
    /// process each message through the invocation of a user-provided delegate, blocks such
    /// as <see cref="ActionBlock{T}"/> and <see cref="TransformBlock{TInput,TOutput}"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="ExecutionDataflowBlockOptions"/> is mutable and can be configured through its properties.  
    /// When specific configuration options are not set, the following defaults are used:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Options</term>
    ///         <description>Default</description>
    ///     </listheader>
    ///     <item>
    ///         <term>TaskScheduler</term>
    ///         <description><see cref="System.Threading.Tasks.TaskScheduler.Default"/></description>
    ///     </item>
    ///     <item>
    ///         <term>CancellationToken</term>
    ///         <description><see cref="System.Threading.CancellationToken.None"/></description>
    ///     </item>
    ///     <item>
    ///         <term>MaxMessagesPerTask</term>
    ///         <description>DataflowBlockOptions.Unbounded (-1)</description>
    ///     </item>
    ///     <item>
    ///         <term>BoundedCapacity</term>
    ///         <description>DataflowBlockOptions.Unbounded (-1)</description>
    ///     </item>
    ///     <item>
    ///         <term>NameFormat</term>
    ///         <description>"{0} Id={1}"</description>
    ///     </item>
    ///     <item>
    ///         <term>EnsureOrdered</term>
    ///         <description>true</description>
    ///     </item>
    ///     <item>
    ///         <term>MaxDegreeOfParallelism</term>
    ///         <description>1</description>
    ///     </item>
    ///     <item>
    ///         <term>SingleProducerConstrained</term>
    ///         <description>false</description>
    ///     </item>
    /// </list>
    /// Dataflow block captures the state of the options at their construction.  Subsequent changes
    /// to the provided <see cref="ExecutionDataflowBlockOptions"/> instance should not affect the behavior
    /// of a dataflow block.
    /// </remarks>
    [DebuggerDisplay("TaskScheduler = {TaskScheduler}, MaxMessagesPerTask = {MaxMessagesPerTask}, BoundedCapacity = {BoundedCapacity}, MaxDegreeOfParallelism = {MaxDegreeOfParallelism}")]
    public class ExecutionDataflowBlockOptions : DataflowBlockOptions
    {
        /// <summary>A default instance of <see cref="DataflowBlockOptions"/>.</summary>
        /// <remarks>
        /// Do not change the values of this instance.  It is shared by all of our blocks when no options are provided by the user.
        /// </remarks>
        internal new static readonly ExecutionDataflowBlockOptions Default = new ExecutionDataflowBlockOptions();

        /// <summary>Returns this <see cref="ExecutionDataflowBlockOptions"/> instance if it's the default instance or else a cloned instance.</summary>
        /// <returns>An instance of the options that may be cached by the block.</returns>
        internal new ExecutionDataflowBlockOptions DefaultOrClone()
        {
            return (this == Default) ?
                this :
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = this.TaskScheduler,
                    CancellationToken = this.CancellationToken,
                    MaxMessagesPerTask = this.MaxMessagesPerTask,
                    BoundedCapacity = this.BoundedCapacity,
                    NameFormat = this.NameFormat,
                    EnsureOrdered = this.EnsureOrdered,
                    MaxDegreeOfParallelism = this.MaxDegreeOfParallelism,
                    SingleProducerConstrained = this.SingleProducerConstrained
                };
        }

        /// <summary>The maximum number of tasks that may be used concurrently to process messages.</summary>
        private int _maxDegreeOfParallelism = 1;
        /// <summary>Whether the code using this block will only ever have a single producer accessing the block at any given time.</summary>
        private bool _singleProducerConstrained = false;

        /// <summary>Initializes the <see cref="ExecutionDataflowBlockOptions"/>.</summary>
        public ExecutionDataflowBlockOptions() { }

        /// <summary>Gets the maximum number of messages that may be processed by the block concurrently.</summary>
        public int MaxDegreeOfParallelism
        {
            get { return _maxDegreeOfParallelism; }
            set
            {
                Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
                if (value < 1 && value != Unbounded) throw new ArgumentOutOfRangeException(nameof(value));
                _maxDegreeOfParallelism = value;
            }
        }

        /// <summary>
        /// Gets whether code using the dataflow block is constrained to one producer at a time.
        /// </summary>
        /// <remarks>
        /// This property defaults to false, such that the block may be used by multiple
        /// producers concurrently.  This property should only be set to true if the code
        /// using the block can guarantee that it will only ever be used by one producer
        /// (e.g. a source linked to the block) at a time, meaning that methods like Post, 
        /// Complete, Fault, and OfferMessage will never be called concurrently.  Some blocks 
        /// may choose to capitalize on the knowledge that there will only be one producer at a time
        /// in order to provide better performance.
        /// </remarks>
        public bool SingleProducerConstrained
        {
            get { return _singleProducerConstrained; }
            set
            {
                Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
                _singleProducerConstrained = value;
            }
        }

        /// <summary>Gets a MaxDegreeOfParallelism value that may be used for comparison purposes.</summary>
        /// <returns>The maximum value, usable for comparison purposes.</returns>
        /// <remarks>Unlike MaxDegreeOfParallelism, this property will always return a positive value.</remarks>
        internal int ActualMaxDegreeOfParallelism
        {
            get { return (_maxDegreeOfParallelism == Unbounded) ? int.MaxValue : _maxDegreeOfParallelism; }
        }

        /// <summary>Gets whether these dataflow block options allow for parallel execution.</summary>
        internal bool SupportsParallelExecution { get { return _maxDegreeOfParallelism == Unbounded || _maxDegreeOfParallelism > 1; } }
    }

    /// <summary>
    /// Provides options used to configure the processing performed by dataflow blocks that
    /// group together multiple messages, blocks such as <see cref="JoinBlock{T1,T2}"/> and 
    /// <see cref="BatchBlock{T}"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="GroupingDataflowBlockOptions"/> is mutable and can be configured through its properties.  
    /// When specific configuration options are not set, the following defaults are used:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Options</term>
    ///         <description>Default</description>
    ///     </listheader>
    ///     <item>
    ///         <term>TaskScheduler</term>
    ///         <description><see cref="System.Threading.Tasks.TaskScheduler.Default"/></description>
    ///     </item>
    ///     <item>
    ///         <term>CancellationToken</term>
    ///         <description><see cref="System.Threading.CancellationToken.None"/></description>
    ///     </item>
    ///     <item>
    ///         <term>MaxMessagesPerTask</term>
    ///         <description>DataflowBlockOptions.Unbounded (-1)</description>
    ///     </item>
    ///     <item>
    ///         <term>BoundedCapacity</term>
    ///         <description>DataflowBlockOptions.Unbounded (-1)</description>
    ///     </item>
    ///     <item>
    ///         <term>NameFormat</term>
    ///         <description>"{0} Id={1}"</description>
    ///     </item>
    ///     <item>
    ///         <term>EnsureOrdered</term>
    ///         <description>true</description>
    ///     </item>
    ///     <item>
    ///         <term>MaxNumberOfGroups</term>
    ///         <description>GroupingDataflowBlockOptions.Unbounded (-1)</description>
    ///     </item>
    ///     <item>
    ///         <term>Greedy</term>
    ///         <description>true</description>
    ///     </item>
    /// </list>
    /// Dataflow block capture the state of the options at their construction.  Subsequent changes
    /// to the provided <see cref="GroupingDataflowBlockOptions"/> instance should not affect the behavior
    /// of a dataflow block.
    /// </remarks>
    [DebuggerDisplay("TaskScheduler = {TaskScheduler}, MaxMessagesPerTask = {MaxMessagesPerTask}, BoundedCapacity = {BoundedCapacity}, Greedy = {Greedy}, MaxNumberOfGroups = {MaxNumberOfGroups}")]
    public class GroupingDataflowBlockOptions : DataflowBlockOptions
    {
        /// <summary>A default instance of <see cref="DataflowBlockOptions"/>.</summary>
        /// <remarks>
        /// Do not change the values of this instance.  It is shared by all of our blocks when no options are provided by the user.
        /// </remarks>
        internal new static readonly GroupingDataflowBlockOptions Default = new GroupingDataflowBlockOptions();

        /// <summary>Returns this <see cref="GroupingDataflowBlockOptions"/> instance if it's the default instance or else a cloned instance.</summary>
        /// <returns>An instance of the options that may be cached by the block.</returns>
        internal new GroupingDataflowBlockOptions DefaultOrClone()
        {
            return (this == Default) ?
                this :
                new GroupingDataflowBlockOptions
                {
                    TaskScheduler = this.TaskScheduler,
                    CancellationToken = this.CancellationToken,
                    MaxMessagesPerTask = this.MaxMessagesPerTask,
                    BoundedCapacity = this.BoundedCapacity,
                    NameFormat = this.NameFormat,
                    EnsureOrdered = this.EnsureOrdered,
                    Greedy = this.Greedy,
                    MaxNumberOfGroups = this.MaxNumberOfGroups
                };
        }

        /// <summary>Whether the block should greedily consume offered messages.</summary>
        private bool _greedy = true;
        /// <summary>The maximum number of groups that should be generated by the block.</summary>
        private long _maxNumberOfGroups = Unbounded;

        /// <summary>Initializes the <see cref="GroupingDataflowBlockOptions"/>.</summary>
        public GroupingDataflowBlockOptions() { }

        /// <summary>Gets or sets the Boolean value to use to determine whether to greedily consume offered messages.</summary>
        public bool Greedy
        {
            get { return _greedy; }
            set
            {
                Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
                _greedy = value;
            }
        }

        /// <summary>Gets or sets the maximum number of groups that should be generated by the block.</summary>
        public long MaxNumberOfGroups
        {
            get { return _maxNumberOfGroups; }
            set
            {
                Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
                if (value <= 0 && value != Unbounded) throw new ArgumentOutOfRangeException(nameof(value));
                _maxNumberOfGroups = value;
            }
        }

        /// <summary>Gets a MaxNumberOfGroups value that may be used for comparison purposes.</summary>
        /// <returns>The maximum value, usable for comparison purposes.</returns>
        /// <remarks>Unlike MaxNumberOfGroups, this property will always return a positive value.</remarks>
        internal long ActualMaxNumberOfGroups
        {
            get { return (_maxNumberOfGroups == Unbounded) ? long.MaxValue : _maxNumberOfGroups; }
        }
    }
}
