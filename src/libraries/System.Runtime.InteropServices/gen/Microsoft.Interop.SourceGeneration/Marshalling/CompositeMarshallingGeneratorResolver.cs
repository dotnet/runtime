// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Interop
{
    /// <summary>
    /// A resolver that will try each of the supplied resolvers in order until one returns a resolved generator.
    /// This resolver will return the first resolved generator, even if the generator is resolved with errors.
    /// </summary>
    /// <param name="resolvers">The list of resolvers to try in order.</param>
    public sealed class CompositeMarshallingGeneratorResolver(IEnumerable<IMarshallingGeneratorResolver> resolvers) : IMarshallingGeneratorResolver
    {
        /// <summary>
        /// A resolver that will try each of the supplied resolvers in order until one returns a resolved generator.
        /// This resolver will return the first resolved generator, even if the generator is resolved with errors.
        /// </summary>
        /// <param name="resolvers">The list of resolvers to try in order.</param>
        public CompositeMarshallingGeneratorResolver(params IMarshallingGeneratorResolver[] resolvers)
            : this((IEnumerable<IMarshallingGeneratorResolver>)resolvers)
        {
        }

        /// <inheritdoc />
        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            foreach (IMarshallingGeneratorResolver resolver in resolvers)
            {
                ResolvedGenerator generator = resolver.Create(info, context);
                if (generator.IsResolved)
                {
                    return generator;
                }
            }
            return ResolvedGenerator.UnresolvedGenerator;
        }
    }
}
