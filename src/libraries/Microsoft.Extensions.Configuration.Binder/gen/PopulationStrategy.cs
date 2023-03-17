// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal enum PopulationStrategy
    {
        NotApplicable = 0,
        Indexer = 1,
        Add = 2,
        Push = 3,
        Enqueue = 4,
    }
}
