// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace System.Reflection.Metadata
{
    public readonly struct CustomAttributeValue<TType>
    {
        public ImmutableArray<CustomAttributeTypedArgument<TType>> FixedArguments { get; }
        public ImmutableArray<CustomAttributeNamedArgument<TType>> NamedArguments { get; }

        public CustomAttributeValue(ImmutableArray<CustomAttributeTypedArgument<TType>> fixedArguments, ImmutableArray<CustomAttributeNamedArgument<TType>> namedArguments)
        {
            FixedArguments = fixedArguments;
            NamedArguments = namedArguments;
        }
    }
}
