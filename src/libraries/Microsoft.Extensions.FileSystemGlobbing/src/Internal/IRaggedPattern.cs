// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public interface IRaggedPattern : IPattern
    {
        IList<IPathSegment> Segments { get; }

        IList<IPathSegment> StartsWith { get; }

        IList<IList<IPathSegment>> Contains { get; }

        IList<IPathSegment> EndsWith { get; }
    }
}
