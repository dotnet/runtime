// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Interface used by hosts to validate options during startup.
    /// Options are enabled to be validated during startup by calling <see cref="DependencyInjection.OptionsBuilderExtensions.ValidateOnStart{TOptions}(OptionsBuilder{TOptions})"/>.
    /// </summary>
    public interface IStartupValidator
    {
        /// <summary>
        /// Calls the <see cref="IValidateOptions{TOptions}"/> validators.
        /// </summary>
        /// <exception cref="OptionsValidationException">One or more <see cref="IValidateOptions{TOptions}"/> return failed <see cref="ValidateOptionsResult"/> when validating.</exception>
        void Validate();
    }
}
