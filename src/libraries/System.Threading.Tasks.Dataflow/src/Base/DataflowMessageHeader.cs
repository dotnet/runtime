// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// DataflowMessageHeader.cs
//
//
// A container of data attributes passed between dataflow blocks.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Threading.Tasks.Dataflow.Internal;

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>Provides a container of data attributes for passing between dataflow blocks.</summary>
    [DebuggerDisplay("Id = {Id}")]
    public readonly struct DataflowMessageHeader : IEquatable<DataflowMessageHeader>
    {
        /// <summary>The message ID. Needs to be unique within the source.</summary>
        private readonly long _id;

        /// <summary>Initializes the <see cref="DataflowMessageHeader"/> with the specified attributes.</summary>
        /// <param name="id">The ID of the message. Must be unique within the originating source block. Need not be globally unique.</param>
        public DataflowMessageHeader(long id)
        {
            if (id == default(long)) throw new ArgumentException(SR.Argument_InvalidMessageId, nameof(id));

            _id = id;
        }

        /// <summary>Gets the validity of the message.</summary>
        /// <returns>True if the ID of the message is different from 0. False if the ID of the message is 0</returns>
        public bool IsValid { get { return _id != default(long); } }

        /// <summary>Gets the ID of the message within the source.</summary>
        /// <returns>The ID contained in the <see cref="DataflowMessageHeader"/> instance.</returns>
        public long Id { get { return _id; } }

        // These overrides are required by the FX API Guidelines.
        // NOTE: When these overrides are present, the compiler doesn't complain about statements 
        // like 'if (struct == null) ...' which will result in incorrect behavior at runtime.
        // The product code should not use them. Instead, it should compare the Id properties.
        // To verify that, every once in a while, comment out this region and build the product. 
        #region Comparison Operators
        /// <summary>Checks two <see cref="DataflowMessageHeader"/> instances for equality by ID without boxing.</summary>
        /// <param name="other">Another <see cref="DataflowMessageHeader"/> instance.</param>
        /// <returns>True if the instances are equal. False otherwise.</returns>
        public bool Equals(DataflowMessageHeader other)
        {
            return this == other;
        }

        /// <summary>Checks boxed <see cref="DataflowMessageHeader"/> instances for equality by ID.</summary>
        /// <param name="obj">A boxed <see cref="DataflowMessageHeader"/> instance.</param>
        /// <returns>True if the instances are equal. False otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is DataflowMessageHeader && this == (DataflowMessageHeader)obj;
        }

        /// <summary>Generates a hash code for the <see cref="DataflowMessageHeader"/> instance.</summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return (int)Id;
        }

        /// <summary>Checks two <see cref="DataflowMessageHeader"/> instances for equality by ID.</summary>
        /// <param name="left">A <see cref="DataflowMessageHeader"/> instance.</param>
        /// <param name="right">A <see cref="DataflowMessageHeader"/> instance.</param>
        /// <returns>True if the instances are equal. False otherwise.</returns>
        public static bool operator ==(DataflowMessageHeader left, DataflowMessageHeader right)
        {
            return left.Id == right.Id;
        }

        /// <summary>Checks two <see cref="DataflowMessageHeader"/> instances for non-equality by ID.</summary>
        /// <param name="left">A <see cref="DataflowMessageHeader"/> instance.</param>
        /// <param name="right">A <see cref="DataflowMessageHeader"/> instance.</param>
        /// <returns>True if the instances are not equal. False otherwise.</returns>
        public static bool operator !=(DataflowMessageHeader left, DataflowMessageHeader right)
        {
            return left.Id != right.Id;
        }
        #endregion
    }
}
