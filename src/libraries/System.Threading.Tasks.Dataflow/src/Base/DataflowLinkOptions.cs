// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// DataflowLinkOptions.cs
//
//
// DataflowLinkOptions type for configuring links between dataflow blocks
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>
    /// Provides options used to configure a link between dataflow blocks.
    /// </summary>
    /// <remarks>
    /// <see cref="DataflowLinkOptions"/> is mutable and can be configured through its properties.  
    /// When specific configuration options are not set, the following defaults are used:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Options</term>
    ///         <description>Default</description>
    ///     </listheader>
    ///     <item>
    ///         <term>PropagateCompletion</term>
    ///         <description>False</description>
    ///     </item>
    ///     <item>
    ///         <term>MaxMessages</term>
    ///         <description>DataflowBlockOptions.Unbounded (-1)</description>
    ///     </item>
    ///     <item>
    ///         <term>Append</term>
    ///         <description>True</description>
    ///     </item>
    /// </list>
    /// Dataflow blocks capture the state of the options at linking. Subsequent changes to the provided
    /// <see cref="DataflowLinkOptions"/> instance should not affect the behavior of a link.
    /// </remarks>
    [DebuggerDisplay("PropagateCompletion = {PropagateCompletion}, MaxMessages = {MaxMessages}, Append = {Append}")]
    public class DataflowLinkOptions
    {
        /// <summary>
        /// A constant used to specify an unlimited quantity for <see cref="DataflowLinkOptions"/> members 
        /// that provide an upper bound. This field is a constant tied to <see cref="DataflowLinkOptions.Unbounded"/>.
        /// </summary>
        internal const int Unbounded = DataflowBlockOptions.Unbounded;

        /// <summary>Whether the linked target will have completion and faulting notification propagated to it automatically.</summary>
        private bool _propagateCompletion = false;
        /// <summary>The maximum number of messages that may be consumed across the link.</summary>
        private int _maxNumberOfMessages = Unbounded;
        /// <summary>Whether the link should be appended to the source?s list of links, or whether it should be prepended.</summary>
        private bool _append = true;

        /// <summary>A default instance of <see cref="DataflowLinkOptions"/>.</summary>
        /// <remarks>
        /// Do not change the values of this instance.  It is shared by all of our blocks when no options are provided by the user.
        /// </remarks>
        internal static readonly DataflowLinkOptions Default = new DataflowLinkOptions();

        /// <summary>A cached instance of <see cref="DataflowLinkOptions"/>.</summary>
        /// <remarks>
        /// Do not change the values of this instance.  It is shared by all of our blocks that need to unlink after one message has been consumed.
        /// </remarks>
        internal static readonly DataflowLinkOptions UnlinkAfterOneAndPropagateCompletion = new DataflowLinkOptions() { MaxMessages = 1, PropagateCompletion = true };

        /// <summary>Initializes the <see cref="DataflowLinkOptions"/>.</summary>
        public DataflowLinkOptions()
        {
        }

        /// <summary>Gets or sets whether the linked target will have completion and faulting notification propagated to it automatically.</summary>
        public bool PropagateCompletion
        {
            get { return _propagateCompletion; }
            set
            {
                Debug.Assert(this != Default && this != UnlinkAfterOneAndPropagateCompletion, "Default and UnlinkAfterOneAndPropagateCompletion instances are supposed to be immutable.");
                _propagateCompletion = value;
            }
        }

        /// <summary>Gets or sets the maximum number of messages that may be consumed across the link.</summary>
        public int MaxMessages
        {
            get { return _maxNumberOfMessages; }
            set
            {
                Debug.Assert(this != Default && this != UnlinkAfterOneAndPropagateCompletion, "Default and UnlinkAfterOneAndPropagateCompletion instances are supposed to be immutable.");
                if (value < 1 && value != Unbounded) throw new ArgumentOutOfRangeException(nameof(value));
                _maxNumberOfMessages = value;
            }
        }

        /// <summary>Gets or sets whether the link should be appended to the source?s list of links, or whether it should be prepended.</summary>
        public bool Append
        {
            get { return _append; }
            set
            {
                Debug.Assert(this != Default && this != UnlinkAfterOneAndPropagateCompletion, "Default and UnlinkAfterOneAndPropagateCompletion instances are supposed to be immutable.");
                _append = value;
            }
        }
    }
}
