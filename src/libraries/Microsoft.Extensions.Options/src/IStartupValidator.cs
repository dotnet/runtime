// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Interface used by hosts to validate options during startup.
    /// </summary>
    /// <seealso cref="DependencyInjection.OptionsBuilderExtensions.ValidateOnStart{TOptions}(OptionsBuilder{TOptions})"/>
    public interface IStartupValidator
    {
        void Validate();
    }
}
