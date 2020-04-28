// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// Activity may be linked to zero or more other <see cref="ActivityContext"/> that are causally related.
    /// Links can point to ActivityContexts inside a single Trace or across different Traces.
    /// Links can be used to represent batched operations where a Activity was initiated by multiple initiating Activities,
    /// each representing a single incoming item being processed in the batch.
    /// </summary>
    public readonly struct ActivityLink  : IEquatable<ActivityLink>
    {
        /// <summary>
        /// Construct a new <see cref="ActivityLink"/> object which can be linked to an Activity object.
        /// </summary>
        /// <param name="context">The trace Activity context<see cref="ActivityContext"/></param>
        public ActivityLink(ActivityContext context) : this(context, null) {}

        /// <summary>
        /// Construct a new <see cref="ActivityLink"/> object which can be linked to an Activity object.
        /// </summary>
        /// <param name="context">The trace Activity context<see cref="ActivityContext"/></param>
        /// <param name="attributes">The key-value pair list of attributes which associated to the <see cref="ActivityContext"/></param>
        public ActivityLink(ActivityContext context, IEnumerable<KeyValuePair<string, object>>? attributes)
        {
            Context = context;
            Attributes = attributes;
        }

        /// <summary>
        /// Retrieve the <see cref="ActivityContext"/> object inside this <see cref="ActivityLink"/> object.
        /// </summary>
        public ActivityContext Context { get; }

        /// <summary>
        /// Retrieve the key-value pair list of attributes attached with the <see cref="ActivityContext"/>.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>>? Attributes { get; }

        public override bool Equals(object? obj) => (obj is ActivityLink link) && this.Equals(link);

        public bool Equals(ActivityLink value) => Context == value.Context && value.Attributes == Attributes;
        public static bool operator ==(ActivityLink left, ActivityLink right) => left.Equals(right);
        public static bool operator !=(ActivityLink left, ActivityLink right) => !left.Equals(right);

        public override int GetHashCode()
        {
            if (this == default)
                return 0;

            // HashCode.Combine would be the best but we need to compile for the full framework which require adding dependency
            // on the extensions package. Considering this simple type and hashing is not expected to be used, we are implementing
            // the hashing manually.
            int hash = 5381;
            hash = ((hash << 5) + hash) + this.Context.GetHashCode();
            if (Attributes != null)
            {
                foreach (KeyValuePair<string, object> kvp in Attributes)
                {
                    hash = ((hash << 5) + hash) + kvp.Key.GetHashCode();
                    if (kvp.Value != null)
                    {
                        hash = ((hash << 5) + hash) + kvp.Value.GetHashCode();
                    }
                }
            }

            return hash;
        }
    }
}
