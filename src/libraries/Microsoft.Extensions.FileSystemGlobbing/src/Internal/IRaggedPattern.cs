// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
