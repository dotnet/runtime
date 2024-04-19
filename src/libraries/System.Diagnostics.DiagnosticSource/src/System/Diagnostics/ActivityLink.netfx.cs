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
            if (this == default)
                return 0;

            // HashCode.Combine would be the best but we need to compile for the full framework which require adding dependency
            // on the extensions package. Considering this simple type and hashing is not expected to be used, we are implementing
            // the hashing manually.
            int hash = 5381;
            hash = ((hash << 5) + hash) + this.Context.GetHashCode();
            if (Tags != null)
            {
                foreach (KeyValuePair<string, object> kvp in Tags)
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
