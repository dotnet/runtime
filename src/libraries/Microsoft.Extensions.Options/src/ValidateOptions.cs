// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of <see cref="IValidateOptions{TOptions}"/>
    /// </summary>
    /// <typeparam name="TOptions">The options type to validate.</typeparam>
    public class ValidateOptions<TOptions> : IValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Options name.</param>
        /// <param name="validation">Validation function.</param>
        /// <param name="failureMessage">Validation failure message.</param>
        public ValidateOptions(string name, Func<TOptions, bool> validation, string failureMessage)
        {
            Name = name;
            Validation = validation;
            FailureMessage = failureMessage;
        }

        /// <summary>
        /// The options name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The validation function.
        /// </summary>
        public Func<TOptions, bool> Validation { get; }

        /// <summary>
        /// The error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <summary>
        /// Validates a specific named options instance (or all when <paramref name="name"/> is null).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            // null name is used to configure all named options
            if (Name == null || name == Name)
            {
                if ((Validation?.Invoke(options)).Value)
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
    /// Implementation of <see cref="IValidateOptions{TOptions}"/>
    /// </summary>
        /// <typeparam name="TOptions">The options type to validate.</typeparam>
    /// <typeparam name="TDep">Dependency type.</typeparam>
    public class ValidateOptions<TOptions, TDep> : IValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Options name.</param>
        /// <param name="dependency">The dependency.</param>
        /// <param name="validation">Validation function.</param>
        /// <param name="failureMessage">Validation failure message.</param>
        public ValidateOptions(string name, TDep dependency, Func<TOptions, TDep, bool> validation, string failureMessage)
        {
            Name = name;
            Validation = validation;
            FailureMessage = failureMessage;
            Dependency = dependency;
        }

        /// <summary>
        /// The options name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The validation function.
        /// </summary>
        public Func<TOptions, TDep, bool> Validation { get; }

        /// <summary>
        /// The error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <summary>
        /// The dependency.
        /// </summary>
        public TDep Dependency { get; }

        /// <summary>
        /// Validates a specific named options instance (or all when <paramref name="name"/> is null).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            // null name is used to configure all named options
            if (Name == null || name == Name)
            {
                if ((Validation?.Invoke(options, Dependency)).Value)
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
    /// Implementation of <see cref="IValidateOptions{TOptions}"/>
    /// </summary>
        /// <typeparam name="TOptions">The options type to validate.</typeparam>
    /// <typeparam name="TDep1">First dependency type.</typeparam>
    /// <typeparam name="TDep2">Second dependency type.</typeparam>
    public class ValidateOptions<TOptions, TDep1, TDep2> : IValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Options name.</param>
        /// <param name="dependency1">The first dependency.</param>
        /// <param name="dependency2">The second dependency.</param>
        /// <param name="validation">Validation function.</param>
        /// <param name="failureMessage">Validation failure message.</param>
        public ValidateOptions(string name, TDep1 dependency1, TDep2 dependency2, Func<TOptions, TDep1, TDep2, bool> validation, string failureMessage)
        {
            Name = name;
            Validation = validation;
            FailureMessage = failureMessage;
            Dependency1 = dependency1;
            Dependency2 = dependency2;
        }

        /// <summary>
        /// The options name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The validation function.
        /// </summary>
        public Func<TOptions, TDep1, TDep2, bool> Validation { get; }

        /// <summary>
        /// The error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <summary>
        /// The first dependency.
        /// </summary>
        public TDep1 Dependency1 { get; }

        /// <summary>
        /// The second dependency.
        /// </summary>
        public TDep2 Dependency2 { get; }

        /// <summary>
        /// Validates a specific named options instance (or all when <paramref name="name"/> is null).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            // null name is used to configure all named options
            if (Name == null || name == Name)
            {
                if ((Validation?.Invoke(options, Dependency1, Dependency2)).Value)
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
    /// Implementation of <see cref="IValidateOptions{TOptions}"/>
    /// </summary>
        /// <typeparam name="TOptions">The options type to validate.</typeparam>
    /// <typeparam name="TDep1">First dependency type.</typeparam>
    /// <typeparam name="TDep2">Second dependency type.</typeparam>
    /// <typeparam name="TDep3">Third dependency type.</typeparam>
    public class ValidateOptions<TOptions, TDep1, TDep2, TDep3> : IValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Options name.</param>
        /// <param name="dependency1">The first dependency.</param>
        /// <param name="dependency2">The second dependency.</param>
        /// <param name="dependency3">The third dependency.</param>
        /// <param name="validation">Validation function.</param>
        /// <param name="failureMessage">Validation failure message.</param>
        public ValidateOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, Func<TOptions, TDep1, TDep2, TDep3, bool> validation, string failureMessage)
        {
            Name = name;
            Validation = validation;
            FailureMessage = failureMessage;
            Dependency1 = dependency1;
            Dependency2 = dependency2;
            Dependency3 = dependency3;
        }

        /// <summary>
        /// The options name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The validation function.
        /// </summary>
        public Func<TOptions, TDep1, TDep2, TDep3, bool> Validation { get; }

        /// <summary>
        /// The error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <summary>
        /// The first dependency.
        /// </summary>
        public TDep1 Dependency1 { get; }

        /// <summary>
        /// The second dependency.
        /// </summary>
        public TDep2 Dependency2 { get; }

        /// <summary>
        /// The third dependency.
        /// </summary>
        public TDep3 Dependency3 { get; }

        /// <summary>
        /// Validates a specific named options instance (or all when <paramref name="name"/> is null).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            // null name is used to configure all named options
            if (Name == null || name == Name)
            {
                if ((Validation?.Invoke(options, Dependency1, Dependency2, Dependency3)).Value)
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
    /// Implementation of <see cref="IValidateOptions{TOptions}"/>
    /// </summary>
        /// <typeparam name="TOptions">The options type to validate.</typeparam>
    /// <typeparam name="TDep1">First dependency type.</typeparam>
    /// <typeparam name="TDep2">Second dependency type.</typeparam>
    /// <typeparam name="TDep3">Third dependency type.</typeparam>
    /// <typeparam name="TDep4">Fourth dependency type.</typeparam>
    public class ValidateOptions<TOptions, TDep1, TDep2, TDep3, TDep4> : IValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Options name.</param>
        /// <param name="dependency1">The first dependency.</param>
        /// <param name="dependency2">The second dependency.</param>
        /// <param name="dependency3">The third dependency.</param>
        /// <param name="dependency4">The fourth dependency.</param>
        /// <param name="validation">Validation function.</param>
        /// <param name="failureMessage">Validation failure message.</param>
        public ValidateOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, Func<TOptions, TDep1, TDep2, TDep3, TDep4, bool> validation, string failureMessage)
        {
            Name = name;
            Validation = validation;
            FailureMessage = failureMessage;
            Dependency1 = dependency1;
            Dependency2 = dependency2;
            Dependency3 = dependency3;
            Dependency4 = dependency4;
        }

        /// <summary>
        /// The options name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The validation function.
        /// </summary>
        public Func<TOptions, TDep1, TDep2, TDep3, TDep4, bool> Validation { get; }

        /// <summary>
        /// The error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <summary>
        /// The first dependency.
        /// </summary>
        public TDep1 Dependency1 { get; }

        /// <summary>
        /// The second dependency.
        /// </summary>
        public TDep2 Dependency2 { get; }

        /// <summary>
        /// The third dependency.
        /// </summary>
        public TDep3 Dependency3 { get; }

        /// <summary>
        /// The fourth dependency.
        /// </summary>
        public TDep4 Dependency4 { get; }

        /// <summary>
        /// Validates a specific named options instance (or all when <paramref name="name"/> is null).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            // null name is used to configure all named options
            if (Name == null || name == Name)
            {
                if ((Validation?.Invoke(options, Dependency1, Dependency2, Dependency3, Dependency4)).Value)
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
    /// Implementation of <see cref="IValidateOptions{TOptions}"/>
    /// </summary>
        /// <typeparam name="TOptions">The options type to validate.</typeparam>
    /// <typeparam name="TDep1">First dependency type.</typeparam>
    /// <typeparam name="TDep2">Second dependency type.</typeparam>
    /// <typeparam name="TDep3">Third dependency type.</typeparam>
    /// <typeparam name="TDep4">Fourth dependency type.</typeparam>
    /// <typeparam name="TDep5">Fifth dependency type.</typeparam>
    public class ValidateOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> : IValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Options name.</param>
        /// <param name="dependency1">The first dependency.</param>
        /// <param name="dependency2">The second dependency.</param>
        /// <param name="dependency3">The third dependency.</param>
        /// <param name="dependency4">The fourth dependency.</param>
        /// <param name="dependency5">The fifth dependency.</param>
        /// <param name="validation">Validation function.</param>
        /// <param name="failureMessage">Validation failure message.</param>
        public ValidateOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, TDep5 dependency5, Func<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, bool> validation, string failureMessage)
        {
            Name = name;
            Validation = validation;
            FailureMessage = failureMessage;
            Dependency1 = dependency1;
            Dependency2 = dependency2;
            Dependency3 = dependency3;
            Dependency4 = dependency4;
            Dependency5 = dependency5;
        }

        /// <summary>
        /// The options name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The validation function.
        /// </summary>
        public Func<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, bool> Validation { get; }

        /// <summary>
        /// The error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <summary>
        /// The first dependency.
        /// </summary>
        public TDep1 Dependency1 { get; }

        /// <summary>
        /// The second dependency.
        /// </summary>
        public TDep2 Dependency2 { get; }

        /// <summary>
        /// The third dependency.
        /// </summary>
        public TDep3 Dependency3 { get; }

        /// <summary>
        /// The fourth dependency.
        /// </summary>
        public TDep4 Dependency4 { get; }

        /// <summary>
        /// The fifth dependency.
        /// </summary>
        public TDep5 Dependency5 { get; }

        /// <summary>
        /// Validates a specific named options instance (or all when <paramref name="name"/> is null).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            // null name is used to configure all named options
            if (Name == null || name == Name)
            {
                if ((Validation?.Invoke(options, Dependency1, Dependency2, Dependency3, Dependency4, Dependency5)).Value)
                {
                    return ValidateOptionsResult.Success;
                }
                return ValidateOptionsResult.Fail(FailureMessage);
            }

            // ignored if not validating this instance
            return ValidateOptionsResult.Skip;
        }
    }
}
