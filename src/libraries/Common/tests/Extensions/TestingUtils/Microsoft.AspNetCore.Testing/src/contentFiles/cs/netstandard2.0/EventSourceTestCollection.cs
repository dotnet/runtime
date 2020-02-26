// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.AspNetCore.Testing.Tracing
{
    // This file comes from Microsoft.AspNetCore.Testing and has to be defined in the test assembly.
    // It enables EventSourceTestBase's parallel isolation functionality.

    [Xunit.CollectionDefinition(EventSourceTestBase.CollectionName, DisableParallelization = true)]
    public class EventSourceTestCollection
    {
    }
}
