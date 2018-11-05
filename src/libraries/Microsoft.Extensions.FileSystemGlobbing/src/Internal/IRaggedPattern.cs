// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal
{
    public interface IRaggedPattern : IPattern
    {
        IList<IPathSegment> Segments { get; }

        IList<IPathSegment> StartsWith { get; }

        IList<IList<IPathSegment>> Contains { get; }

        IList<IPathSegment> EndsWith { get; }
    }
}