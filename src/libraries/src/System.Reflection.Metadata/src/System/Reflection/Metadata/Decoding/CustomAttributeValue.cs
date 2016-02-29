// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

#if SRM
namespace System.Reflection.Metadata.Decoding
#else
namespace Roslyn.Reflection.Metadata.Decoding
#endif
{
#if SRM && FUTURE
    public
#endif
    struct CustomAttributeValue<TType>
    {
        private readonly ImmutableArray<CustomAttributeTypedArgument<TType>> _fixedArguments;
        private readonly ImmutableArray<CustomAttributeNamedArgument<TType>> _namedArguments;

        public CustomAttributeValue(ImmutableArray<CustomAttributeTypedArgument<TType>> fixedArguments, ImmutableArray<CustomAttributeNamedArgument<TType>> namedArguments)
        {
            _fixedArguments = fixedArguments;
            _namedArguments = namedArguments;
        }

        public ImmutableArray<CustomAttributeTypedArgument<TType>> FixedArguments
        {
            get { return _fixedArguments; }
        }

        public ImmutableArray<CustomAttributeNamedArgument<TType>> NamedArguments
        {
            get { return _namedArguments; }
        }
    }
}
