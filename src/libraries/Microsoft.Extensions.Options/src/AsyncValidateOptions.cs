// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of <see cref="IAsyncValidateOptions{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">The options type to validate.</typeparam>
    public class AsyncValidateOptions<TOptions> : IAsyncValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncValidateOptions{TOptions}"/>.
        /// </summary>
        /// <param name="name">Options name.</param>
        /// <param name="validation">Asynchronous validation function.</param>
        /// <param name="failureMessage">Validation failure message.</param>
        public AsyncValidateOptions(string? name, Func<TOptions, CancellationToken, Task<bool>> validation, string failureMessage)
        {
            ArgumentNullException.ThrowIfNull(validation);

            Name = name;
            Validation = validation;
            FailureMessage = failureMessage;
        }

        /// <summary>
        /// Gets the options name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the asynchronous validation function.
        /// </summary>
        public Func<TOptions, CancellationToken, Task<bool>> Validation { get; }

        /// <summary>
        /// Gets the error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <summary>
        /// Validates a specific named options instance (or all when <paramref name="name"/> is null).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            if (Name is null || name == Name)
            {
                return ValidateOptionsResult.Fail(OptionsAsyncValidation.GetSyncValidationUnsupportedFailureMessage<TOptions>(name ?? Options.DefaultName));
            }

            return ValidateOptionsResult.Skip;
        }

        /// <summary>
        /// Asynchronously validates a specific named options instance (or all when <paramref name="name"/> is null).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        public async Task<ValidateOptionsResult> ValidateAsync(string? name, TOptions options, CancellationToken cancellationToken = default)
        {
            // null name is used to configure all named options
            if (Name is null || name == Name)
            {
                if (await Validation(options, cancellationToken).ConfigureAwait(false))
                {
                    return ValidateOptionsResult.Success;
                }

                return ValidateOptionsResult.Fail(FailureMessage);
            }

            // ignored if not validating this instance
            return ValidateOptionsResult.Skip;
        }
    }

    /// <summary>
    /// Implementation of <see cref="IAsyncValidateOptions{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">The options type to validate.</typeparam>
    /// <typeparam name="TDep">Dependency type.</typeparam>
    public class AsyncValidateOptions<TOptions, TDep> : IAsyncValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncValidateOptions{TOptions, TDep}"/>.
        /// </summary>
        /// <param name="name">Options name.</param>
        /// <param name="dependency">The dependency.</param>
        /// <param name="validation">Asynchronous validation function.</param>
        /// <param name="failureMessage">Validation failure message.</param>
        public AsyncValidateOptions(string? name, TDep dependency, Func<TOptions, TDep, CancellationToken, Task<bool>> validation, string failureMessage)
        {
            ArgumentNullException.ThrowIfNull(validation);

            Name = name;
            Dependency = dependency;
            Validation = validation;
            FailureMessage = failureMessage;
        }

        /// <summary>
        /// Gets the options name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the dependency.
        /// </summary>
        public TDep Dependency { get; }

        /// <summary>
        /// Gets the asynchronous validation function.
        /// </summary>
        public Func<TOptions, TDep, CancellationToken, Task<bool>> Validation { get; }

        /// <summary>
        /// Gets the error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            if (Name is null || name == Name)
            {
                return ValidateOptionsResult.Fail(OptionsAsyncValidation.GetSyncValidationUnsupportedFailureMessage<TOptions>(name ?? Options.DefaultName));
            }

            return ValidateOptionsResult.Skip;
        }

        /// <inheritdoc/>
        public async Task<ValidateOptionsResult> ValidateAsync(string? name, TOptions options, CancellationToken cancellationToken = default)
        {
            if (Name is null || name == Name)
            {
                if (await Validation(options, Dependency, cancellationToken).ConfigureAwait(false))
                {
                    return ValidateOptionsResult.Success;
                }

                return ValidateOptionsResult.Fail(FailureMessage);
            }

            return ValidateOptionsResult.Skip;
        }
    }

    /// <summary>
    /// Implementation of <see cref="IAsyncValidateOptions{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">The options type to validate.</typeparam>
    /// <typeparam name="TDep1">First dependency type.</typeparam>
    /// <typeparam name="TDep2">Second dependency type.</typeparam>
    public class AsyncValidateOptions<TOptions, TDep1, TDep2> : IAsyncValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncValidateOptions{TOptions, TDep1, TDep2}"/>.
        /// </summary>
        /// <param name="name">Options name.</param>
        /// <param name="dependency1">The first dependency.</param>
        /// <param name="dependency2">The second dependency.</param>
        /// <param name="validation">Asynchronous validation function.</param>
        /// <param name="failureMessage">Validation failure message.</param>
        public AsyncValidateOptions(string? name, TDep1 dependency1, TDep2 dependency2, Func<TOptions, TDep1, TDep2, CancellationToken, Task<bool>> validation, string failureMessage)
        {
            ArgumentNullException.ThrowIfNull(validation);

            Name = name;
            Dependency1 = dependency1;
            Dependency2 = dependency2;
            Validation = validation;
            FailureMessage = failureMessage;
        }

        /// <summary>
        /// Gets the options name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the first dependency.
        /// </summary>
        public TDep1 Dependency1 { get; }

        /// <summary>
        /// Gets the second dependency.
        /// </summary>
        public TDep2 Dependency2 { get; }

        /// <summary>
        /// Gets the asynchronous validation function.
        /// </summary>
        public Func<TOptions, TDep1, TDep2, CancellationToken, Task<bool>> Validation { get; }

        /// <summary>
        /// Gets the error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            if (Name is null || name == Name)
            {
                return ValidateOptionsResult.Fail(OptionsAsyncValidation.GetSyncValidationUnsupportedFailureMessage<TOptions>(name ?? Options.DefaultName));
            }

            return ValidateOptionsResult.Skip;
        }

        /// <inheritdoc/>
        public async Task<ValidateOptionsResult> ValidateAsync(string? name, TOptions options, CancellationToken cancellationToken = default)
        {
            if (Name is null || name == Name)
            {
                if (await Validation(options, Dependency1, Dependency2, cancellationToken).ConfigureAwait(false))
                {
                    return ValidateOptionsResult.Success;
                }

                return ValidateOptionsResult.Fail(FailureMessage);
            }

            return ValidateOptionsResult.Skip;
        }
    }

    /// <summary>
    /// Implementation of <see cref="IAsyncValidateOptions{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">The options type to validate.</typeparam>
    /// <typeparam name="TDep1">First dependency type.</typeparam>
    /// <typeparam name="TDep2">Second dependency type.</typeparam>
    /// <typeparam name="TDep3">Third dependency type.</typeparam>
    public class AsyncValidateOptions<TOptions, TDep1, TDep2, TDep3> : IAsyncValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncValidateOptions{TOptions, TDep1, TDep2, TDep3}"/>.
        /// </summary>
        /// <param name="name">Options name.</param>
        /// <param name="dependency1">The first dependency.</param>
        /// <param name="dependency2">The second dependency.</param>
        /// <param name="dependency3">The third dependency.</param>
        /// <param name="validation">Asynchronous validation function.</param>
        /// <param name="failureMessage">Validation failure message.</param>
        public AsyncValidateOptions(string? name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, Func<TOptions, TDep1, TDep2, TDep3, CancellationToken, Task<bool>> validation, string failureMessage)
        {
            ArgumentNullException.ThrowIfNull(validation);

            Name = name;
            Dependency1 = dependency1;
            Dependency2 = dependency2;
            Dependency3 = dependency3;
            Validation = validation;
            FailureMessage = failureMessage;
        }

        /// <summary>
        /// Gets the options name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the first dependency.
        /// </summary>
        public TDep1 Dependency1 { get; }

        /// <summary>
        /// Gets the second dependency.
        /// </summary>
        public TDep2 Dependency2 { get; }

        /// <summary>
        /// Gets the third dependency.
        /// </summary>
        public TDep3 Dependency3 { get; }

        /// <summary>
        /// Gets the asynchronous validation function.
        /// </summary>
        public Func<TOptions, TDep1, TDep2, TDep3, CancellationToken, Task<bool>> Validation { get; }

        /// <summary>
        /// Gets the error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            if (Name is null || name == Name)
            {
                return ValidateOptionsResult.Fail(OptionsAsyncValidation.GetSyncValidationUnsupportedFailureMessage<TOptions>(name ?? Options.DefaultName));
            }

            return ValidateOptionsResult.Skip;
        }

        /// <inheritdoc/>
        public async Task<ValidateOptionsResult> ValidateAsync(string? name, TOptions options, CancellationToken cancellationToken = default)
        {
            if (Name is null || name == Name)
            {
                if (await Validation(options, Dependency1, Dependency2, Dependency3, cancellationToken).ConfigureAwait(false))
                {
                    return ValidateOptionsResult.Success;
                }

                return ValidateOptionsResult.Fail(FailureMessage);
            }

            return ValidateOptionsResult.Skip;
        }
    }

    /// <summary>
    /// Implementation of <see cref="IAsyncValidateOptions{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">The options type to validate.</typeparam>
    /// <typeparam name="TDep1">First dependency type.</typeparam>
    /// <typeparam name="TDep2">Second dependency type.</typeparam>
    /// <typeparam name="TDep3">Third dependency type.</typeparam>
    /// <typeparam name="TDep4">Fourth dependency type.</typeparam>
    public class AsyncValidateOptions<TOptions, TDep1, TDep2, TDep3, TDep4> : IAsyncValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncValidateOptions{TOptions, TDep1, TDep2, TDep3, TDep4}"/>.
        /// </summary>
        /// <param name="name">Options name.</param>
        /// <param name="dependency1">The first dependency.</param>
        /// <param name="dependency2">The second dependency.</param>
        /// <param name="dependency3">The third dependency.</param>
        /// <param name="dependency4">The fourth dependency.</param>
        /// <param name="validation">Asynchronous validation function.</param>
        /// <param name="failureMessage">Validation failure message.</param>
        public AsyncValidateOptions(string? name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, Func<TOptions, TDep1, TDep2, TDep3, TDep4, CancellationToken, Task<bool>> validation, string failureMessage)
        {
            ArgumentNullException.ThrowIfNull(validation);

            Name = name;
            Dependency1 = dependency1;
            Dependency2 = dependency2;
            Dependency3 = dependency3;
            Dependency4 = dependency4;
            Validation = validation;
            FailureMessage = failureMessage;
        }

        /// <summary>
        /// Gets the options name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the first dependency.
        /// </summary>
        public TDep1 Dependency1 { get; }

        /// <summary>
        /// Gets the second dependency.
        /// </summary>
        public TDep2 Dependency2 { get; }

        /// <summary>
        /// Gets the third dependency.
        /// </summary>
        public TDep3 Dependency3 { get; }

        /// <summary>
        /// Gets the fourth dependency.
        /// </summary>
        public TDep4 Dependency4 { get; }

        /// <summary>
        /// Gets the asynchronous validation function.
        /// </summary>
        public Func<TOptions, TDep1, TDep2, TDep3, TDep4, CancellationToken, Task<bool>> Validation { get; }

        /// <summary>
        /// Gets the error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            if (Name is null || name == Name)
            {
                return ValidateOptionsResult.Fail(OptionsAsyncValidation.GetSyncValidationUnsupportedFailureMessage<TOptions>(name ?? Options.DefaultName));
            }

            return ValidateOptionsResult.Skip;
        }

        /// <inheritdoc/>
        public async Task<ValidateOptionsResult> ValidateAsync(string? name, TOptions options, CancellationToken cancellationToken = default)
        {
            if (Name is null || name == Name)
            {
                if (await Validation(options, Dependency1, Dependency2, Dependency3, Dependency4, cancellationToken).ConfigureAwait(false))
                {
                    return ValidateOptionsResult.Success;
                }

                return ValidateOptionsResult.Fail(FailureMessage);
            }

            return ValidateOptionsResult.Skip;
        }
    }

    /// <summary>
    /// Implementation of <see cref="IAsyncValidateOptions{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">The options type to validate.</typeparam>
    /// <typeparam name="TDep1">First dependency type.</typeparam>
    /// <typeparam name="TDep2">Second dependency type.</typeparam>
    /// <typeparam name="TDep3">Third dependency type.</typeparam>
    /// <typeparam name="TDep4">Fourth dependency type.</typeparam>
    /// <typeparam name="TDep5">Fifth dependency type.</typeparam>
    public class AsyncValidateOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> : IAsyncValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncValidateOptions{TOptions, TDep1, TDep2, TDep3, TDep4, TDep5}"/>.
        /// </summary>
        /// <param name="name">Options name.</param>
        /// <param name="dependency1">The first dependency.</param>
        /// <param name="dependency2">The second dependency.</param>
        /// <param name="dependency3">The third dependency.</param>
        /// <param name="dependency4">The fourth dependency.</param>
        /// <param name="dependency5">The fifth dependency.</param>
        /// <param name="validation">Asynchronous validation function.</param>
        /// <param name="failureMessage">Validation failure message.</param>
        public AsyncValidateOptions(string? name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, TDep5 dependency5, Func<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, CancellationToken, Task<bool>> validation, string failureMessage)
        {
            ArgumentNullException.ThrowIfNull(validation);

            Name = name;
            Dependency1 = dependency1;
            Dependency2 = dependency2;
            Dependency3 = dependency3;
            Dependency4 = dependency4;
            Dependency5 = dependency5;
            Validation = validation;
            FailureMessage = failureMessage;
        }

        /// <summary>
        /// Gets the options name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the first dependency.
        /// </summary>
        public TDep1 Dependency1 { get; }

        /// <summary>
        /// Gets the second dependency.
        /// </summary>
        public TDep2 Dependency2 { get; }

        /// <summary>
        /// Gets the third dependency.
        /// </summary>
        public TDep3 Dependency3 { get; }

        /// <summary>
        /// Gets the fourth dependency.
        /// </summary>
        public TDep4 Dependency4 { get; }

        /// <summary>
        /// Gets the fifth dependency.
        /// </summary>
        public TDep5 Dependency5 { get; }

        /// <summary>
        /// Gets the asynchronous validation function.
        /// </summary>
        public Func<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, CancellationToken, Task<bool>> Validation { get; }

        /// <summary>
        /// Gets the error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            if (Name is null || name == Name)
            {
                return ValidateOptionsResult.Fail(OptionsAsyncValidation.GetSyncValidationUnsupportedFailureMessage<TOptions>(name ?? Options.DefaultName));
            }

            return ValidateOptionsResult.Skip;
        }

        /// <inheritdoc/>
        public async Task<ValidateOptionsResult> ValidateAsync(string? name, TOptions options, CancellationToken cancellationToken = default)
        {
            if (Name is null || name == Name)
            {
                if (await Validation(options, Dependency1, Dependency2, Dependency3, Dependency4, Dependency5, cancellationToken).ConfigureAwait(false))
                {
                    return ValidateOptionsResult.Success;
                }

                return ValidateOptionsResult.Fail(FailureMessage);
            }

            return ValidateOptionsResult.Skip;
        }
    }
}
