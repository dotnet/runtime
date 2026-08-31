// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.FileSystemGlobbing.Internal
{
    internal struct IncludeOrExcludeValue<TValue>
    {
        internal TValue Value;
        internal bool IsInclude;
    }
}
