// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection.Tests.Fakes
{
    //    A
    //  / | \
    // B  C  C
    // |
    // C
    public class NoCircularDependencySameTypeMultipleTimesA
    {
        public NoCircularDependencySameTypeMultipleTimesA(
            NoCircularDependencySameTypeMultipleTimesB b,
            NoCircularDependencySameTypeMultipleTimesC c1,
            NoCircularDependencySameTypeMultipleTimesC c2)
        {

        }
    }
}