// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// DataflowMessageStatus.cs
//
//
// Status about the propagation of a message.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>Represents the status of a <see cref="DataflowMessageHeader"/> when passed between dataflow blocks.</summary>
    public enum DataflowMessageStatus
    {
        /// <summary>
        /// Indicates that the <see cref="ITargetBlock{TInput}"/> accepted the message.  Once a target has accepted a message, 
        /// it is wholly owned by the target.
        /// </summary>
        Accepted = 0x0,

        /// <summary>
        /// Indicates that the <see cref="ITargetBlock{TInput}"/> declined the message.  The <see cref="ISourceBlock{TOutput}"/> still owns the message.
        /// </summary>
        Declined = 0x1,

        /// <summary>
        /// Indicates that the <see cref="ITargetBlock{TInput}"/> postponed the message for potential consumption at a later time.  
        /// The <see cref="ISourceBlock{TOutput}"/> still owns the message.
        /// </summary>
        Postponed = 0x2,

        /// <summary>
        /// Indicates that the <see cref="ITargetBlock{TInput}"/> tried to accept the message from the <see cref="ISourceBlock{TOutput}"/>, but the 
        /// message was no longer available.
        /// </summary>
        NotAvailable = 0x3,

        /// <summary>
        /// Indicates that the <see cref="ITargetBlock{TInput}"/> declined the message.  The <see cref="ISourceBlock{TOutput}"/> still owns the message.  
        /// Additionally, the <see cref="ITargetBlock{TInput}"/> will decline all future messages sent by the source.
        /// </summary>
        DecliningPermanently = 0x4
    }
}
