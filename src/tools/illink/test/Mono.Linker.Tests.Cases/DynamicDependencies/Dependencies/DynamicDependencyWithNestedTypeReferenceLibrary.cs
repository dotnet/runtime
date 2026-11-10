// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies
{
    public class Outer
    {
        public class Nested
        {
        }
    }

    public class GenericOuter<T>
    {
        public class NonGenericNested
        {
        }

        public class GenericMiddle<U>
        {
            public class GenericNested<V>
            {
            }
        }
    }
}
