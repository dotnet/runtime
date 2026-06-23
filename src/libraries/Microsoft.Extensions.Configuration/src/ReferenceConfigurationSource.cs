// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    // Internal carrier for the reference rule set and parser declared through
    // ConfigurationReferenceBuilder. Implements IContextualConfigurationSource because the reference
    // provider must materialise against an upstream provider snapshot, which the standard
    // IConfigurationSource.Build contract does not expose.
    internal sealed class ReferenceConfigurationSource : IContextualConfigurationSource
    {
        internal ReferenceConfigurationSource(
            IReadOnlyDictionary<string, ReferenceRule> concreteRules,
            IReadOnlyList<ReferenceRule> templateRules,
            Func<string, ConfigurationExpansion?> parser)
        {
            ConcreteRules = concreteRules;
            TemplateRules = templateRules;
            Parser = parser;
        }

        internal IReadOnlyDictionary<string, ReferenceRule> ConcreteRules { get; }

        internal IReadOnlyList<ReferenceRule> TemplateRules { get; }

        internal Func<string, ConfigurationExpansion?> Parser { get; }

        // The default IConfigurationSource.Build contract has no view of peer providers, so the
        // reference source can't satisfy it. ConfigurationBuilder and ConfigurationManager detect
        // IContextualConfigurationSource and route to the overload below.
        public IConfigurationProvider Build(IConfigurationBuilder builder)
            => throw new InvalidOperationException(SR.Error_ReferenceSourceContextualBuildOnly);

        public IConfigurationProvider Build(IConfigurationBuilder builder, IReadOnlyList<IConfigurationProvider> previousProviders)
            => new ReferenceConfigurationProvider(this, previousProviders);
    }
}
