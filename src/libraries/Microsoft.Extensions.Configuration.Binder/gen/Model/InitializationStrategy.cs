// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal enum InitializationStrategy
    {
        None = 0,
        ParameterlessConstructor = 1,
        ParameterizedConstructor = 2,
        ToEnumerableMethod = 3,
        Array = 4,
    }
}
