// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// Activity may be linked to zero or more other <see cref="ActivityContext"/> that are causally related.
    /// Links can point to ActivityContexts inside a single Trace or across different Traces.
    /// Links can be used to represent batched operations where a Activity was initiated by multiple initiating Activities,
    /// each representing a single incoming item being processed in the batch.
    /// </summary>
    public readonly partial struct ActivityLink : IEquatable<ActivityLink>
    {
        public override int GetHashCode()
        {
            HashCode hashCode = default;
            hashCode.Add(Context);
            if (Tags != null)
            {
                foreach (KeyValuePair<string, object?> kvp in Tags)
                {
                    hashCode.Add(kvp.Key);
                    hashCode.Add(kvp.Value);
                }
            }
            return hashCode.ToHashCode();
        }
    }
}
