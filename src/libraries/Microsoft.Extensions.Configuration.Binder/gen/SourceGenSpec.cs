// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record SourceGenSpec
    {
        public ConfigBinderMethodSpec ConfigBinderSpec { get; }
        public OptionsBuilderMethodSpec OptionsBuilderSpec { get; }
        public ServiceCollectionMethodSpec ServiceCollectionSpec { get; }
        public CoreBindingHelperMethodSpec CoreBindingHelperSpec { get; }

        public SourceGenSpec()
        {
            ConfigBinderSpec = new ConfigBinderMethodSpec(this);
            OptionsBuilderSpec = new OptionsBuilderMethodSpec(this);
            ServiceCollectionSpec = new ServiceCollectionMethodSpec(this);
            CoreBindingHelperSpec = new CoreBindingHelperMethodSpec(this);
        }

        public bool HasRootMethods() => ConfigBinderSpec.Any() || OptionsBuilderSpec.Any() || ServiceCollectionSpec.Any();
    }
}
