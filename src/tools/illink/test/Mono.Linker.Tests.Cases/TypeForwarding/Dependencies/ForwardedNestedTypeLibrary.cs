// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


#if INCLUDE_FORWARDERS
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedGenericNestedTypeLibrary<>))]
#endif

#if INCLUDE_REFERENCE_IMPL
namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

public class ForwardedGenericNestedTypeLibrary<T>
{
    public class Nested
    {
    }
}

public class ForwardedNestedTypeLibrary
{
    public class NestedOne
    {
        public class NestedTwo
        {
            public class NestedThree
            {
            }
        }
    }
}
#endif
