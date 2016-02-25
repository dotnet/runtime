// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
