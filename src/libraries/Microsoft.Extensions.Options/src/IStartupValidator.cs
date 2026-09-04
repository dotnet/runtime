// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Provides synchronous options validation during host startup.
    /// </summary>
    /// <remarks>
    /// Options are enabled to be validated during startup by calling <see cref="DependencyInjection.OptionsBuilderExtensions.ValidateOnStart{TOptions}(OptionsBuilder{TOptions})"/>.
    /// This interface is retained for compatibility. New startup validators should implement and register
    /// <see cref="IAsyncStartupValidator"/> instead. When migrating, replace the obsolete registration rather than
    /// registering a custom validator under both startup contracts.
    /// </remarks>
    [Obsolete(Obsoletions.IStartupValidatorMessage, DiagnosticId = Obsoletions.IStartupValidatorDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    public interface IStartupValidator
    {
        /// <summary>
        /// Validates options during host startup.
        /// </summary>
        /// <exception cref="OptionsValidationException">One or more <see cref="IValidateOptions{TOptions}"/> return failed <see cref="ValidateOptionsResult"/> when validating.</exception>
        void Validate();
    }
}
