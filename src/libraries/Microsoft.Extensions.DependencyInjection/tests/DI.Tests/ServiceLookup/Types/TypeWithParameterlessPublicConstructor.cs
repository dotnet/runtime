// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    public class TypeWithParameterlessPublicConstructor
    {
        public TypeWithParameterlessPublicConstructor()
            : this("some name")
        {
        }

        protected TypeWithParameterlessPublicConstructor(string name)
        {
        }
    }
}
