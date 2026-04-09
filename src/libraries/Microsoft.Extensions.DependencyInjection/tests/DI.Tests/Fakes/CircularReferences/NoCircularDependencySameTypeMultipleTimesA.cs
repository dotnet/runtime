// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
