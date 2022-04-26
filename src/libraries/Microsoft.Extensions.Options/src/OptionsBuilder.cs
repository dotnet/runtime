// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Used to configure <typeparamref name="TOptions"/> instances.
    /// </summary>
    /// <typeparam name="TOptions">The type of options being requested.</typeparam>
    public class OptionsBuilder<TOptions> where TOptions : class
    {
        private const string DefaultValidationFailureMessage = "A validation error has occurred.";

        /// <summary>
        /// The default name of the <typeparamref name="TOptions"/> instance.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The <see cref="IServiceCollection"/> for the options being configured.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for the options being configured.</param>
        /// <param name="name">The default name of the <typeparamref name="TOptions"/> instance, if null <see cref="Options.DefaultName"/> is used.</param>
        public OptionsBuilder(IServiceCollection services, string? name)
        {
            ThrowHelper.ThrowIfNull(services);

            Services = services;
            Name = name ?? Options.DefaultName;
        }

        /// <summary>
        /// Registers an action used to configure a particular type of options.
        /// Note: These are run before all <seealso cref="PostConfigure(Action{TOptions})"/>.
        /// </summary>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Configure(Action<TOptions> configureOptions)
        {
            ThrowHelper.ThrowIfNull(configureOptions);

            Services.AddSingleton<IConfigureOptions<TOptions>>(new ConfigureNamedOptions<TOptions>(Name, configureOptions));
            return this;
        }

        /// <summary>
        /// Registers an action used to configure a particular type of options.
        /// Note: These are run before all <seealso cref="PostConfigure(Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TDep">A dependency used by the action.</typeparam>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Configure<TDep>(Action<TOptions, TDep> configureOptions)
            where TDep : class
        {
            ThrowHelper.ThrowIfNull(configureOptions);

            Services.AddTransient<IConfigureOptions<TOptions>>(sp =>
                new ConfigureNamedOptions<TOptions, TDep>(Name, sp.GetRequiredService<TDep>(), configureOptions));
            return this;
        }

        /// <summary>
        /// Registers an action used to configure a particular type of options.
        /// Note: These are run before all <seealso cref="PostConfigure(Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the action.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the action.</typeparam>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Configure<TDep1, TDep2>(Action<TOptions, TDep1, TDep2> configureOptions)
            where TDep1 : class
            where TDep2 : class
        {
            ThrowHelper.ThrowIfNull(configureOptions);

            Services.AddTransient<IConfigureOptions<TOptions>>(sp =>
                new ConfigureNamedOptions<TOptions, TDep1, TDep2>(Name, sp.GetRequiredService<TDep1>(), sp.GetRequiredService<TDep2>(), configureOptions));
            return this;
        }

        /// <summary>
        /// Registers an action used to configure a particular type of options.
        /// Note: These are run before all <seealso cref="PostConfigure(Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the action.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the action.</typeparam>
        /// <typeparam name="TDep3">The third dependency used by the action.</typeparam>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Configure<TDep1, TDep2, TDep3>(Action<TOptions, TDep1, TDep2, TDep3> configureOptions)
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
        {
            ThrowHelper.ThrowIfNull(configureOptions);

            Services.AddTransient<IConfigureOptions<TOptions>>(
                sp => new ConfigureNamedOptions<TOptions, TDep1, TDep2, TDep3>(
                    Name,
                    sp.GetRequiredService<TDep1>(),
                    sp.GetRequiredService<TDep2>(),
                    sp.GetRequiredService<TDep3>(),
                    configureOptions));
            return this;
        }

        /// <summary>
        /// Registers an action used to configure a particular type of options.
        /// Note: These are run before all <seealso cref="PostConfigure(Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the action.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the action.</typeparam>
        /// <typeparam name="TDep3">The third dependency used by the action.</typeparam>
        /// <typeparam name="TDep4">The fourth dependency used by the action.</typeparam>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Configure<TDep1, TDep2, TDep3, TDep4>(Action<TOptions, TDep1, TDep2, TDep3, TDep4> configureOptions)
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
            where TDep4 : class
        {
            ThrowHelper.ThrowIfNull(configureOptions);

            Services.AddTransient<IConfigureOptions<TOptions>>(
                sp => new ConfigureNamedOptions<TOptions, TDep1, TDep2, TDep3, TDep4>(
                    Name,
                    sp.GetRequiredService<TDep1>(),
                    sp.GetRequiredService<TDep2>(),
                    sp.GetRequiredService<TDep3>(),
                    sp.GetRequiredService<TDep4>(),
                    configureOptions));
            return this;
        }

        /// <summary>
        /// Registers an action used to configure a particular type of options.
        /// Note: These are run before all <seealso cref="PostConfigure(Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the action.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the action.</typeparam>
        /// <typeparam name="TDep3">The third dependency used by the action.</typeparam>
        /// <typeparam name="TDep4">The fourth dependency used by the action.</typeparam>
        /// <typeparam name="TDep5">The fifth dependency used by the action.</typeparam>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Configure<TDep1, TDep2, TDep3, TDep4, TDep5>(Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> configureOptions)
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
            where TDep4 : class
            where TDep5 : class
        {
            ThrowHelper.ThrowIfNull(configureOptions);

            Services.AddTransient<IConfigureOptions<TOptions>>(
                sp => new ConfigureNamedOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5>(
                    Name,
                    sp.GetRequiredService<TDep1>(),
                    sp.GetRequiredService<TDep2>(),
                    sp.GetRequiredService<TDep3>(),
                    sp.GetRequiredService<TDep4>(),
                    sp.GetRequiredService<TDep5>(),
                    configureOptions));
            return this;
        }

        /// <summary>
        /// Registers an action used to configure a particular type of options.
        /// Note: These are run after all <seealso cref="Configure(Action{TOptions})"/>.
        /// </summary>
        /// <param name="configureOptions">The action used to configure the options.</param>
        public virtual OptionsBuilder<TOptions> PostConfigure(Action<TOptions> configureOptions)
        {
            ThrowHelper.ThrowIfNull(configureOptions);

            Services.AddSingleton<IPostConfigureOptions<TOptions>>(new PostConfigureOptions<TOptions>(Name, configureOptions));
            return this;
        }

        /// <summary>
        /// Registers an action used to post configure a particular type of options.
        /// Note: These are run after all <seealso cref="Configure(Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TDep">The dependency used by the action.</typeparam>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> PostConfigure<TDep>(Action<TOptions, TDep> configureOptions)
            where TDep : class
        {
            ThrowHelper.ThrowIfNull(configureOptions);

            Services.AddTransient<IPostConfigureOptions<TOptions>>(sp =>
                new PostConfigureOptions<TOptions, TDep>(Name, sp.GetRequiredService<TDep>(), configureOptions));
            return this;
        }

        /// <summary>
        /// Registers an action used to post configure a particular type of options.
        /// Note: These are run after all <seealso cref="Configure(Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the action.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the action.</typeparam>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> PostConfigure<TDep1, TDep2>(Action<TOptions, TDep1, TDep2> configureOptions)
            where TDep1 : class
            where TDep2 : class
        {
            ThrowHelper.ThrowIfNull(configureOptions);

            Services.AddTransient<IPostConfigureOptions<TOptions>>(sp =>
                new PostConfigureOptions<TOptions, TDep1, TDep2>(Name, sp.GetRequiredService<TDep1>(), sp.GetRequiredService<TDep2>(), configureOptions));
            return this;
        }

        /// <summary>
        /// Registers an action used to post configure a particular type of options.
        /// Note: These are run after all <seealso cref="Configure(Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the action.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the action.</typeparam>
        /// <typeparam name="TDep3">The third dependency used by the action.</typeparam>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> PostConfigure<TDep1, TDep2, TDep3>(Action<TOptions, TDep1, TDep2, TDep3> configureOptions)
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
        {
            ThrowHelper.ThrowIfNull(configureOptions);

            Services.AddTransient<IPostConfigureOptions<TOptions>>(
                sp => new PostConfigureOptions<TOptions, TDep1, TDep2, TDep3>(
                    Name,
                    sp.GetRequiredService<TDep1>(),
                    sp.GetRequiredService<TDep2>(),
                    sp.GetRequiredService<TDep3>(),
                    configureOptions));
            return this;
        }

        /// <summary>
        /// Registers an action used to post configure a particular type of options.
        /// Note: These are run after all <seealso cref="Configure(Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the action.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the action.</typeparam>
        /// <typeparam name="TDep3">The third dependency used by the action.</typeparam>
        /// <typeparam name="TDep4">The fourth dependency used by the action.</typeparam>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> PostConfigure<TDep1, TDep2, TDep3, TDep4>(Action<TOptions, TDep1, TDep2, TDep3, TDep4> configureOptions)
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
            where TDep4 : class
        {
            ThrowHelper.ThrowIfNull(configureOptions);

            Services.AddTransient<IPostConfigureOptions<TOptions>>(
                sp => new PostConfigureOptions<TOptions, TDep1, TDep2, TDep3, TDep4>(
                    Name,
                    sp.GetRequiredService<TDep1>(),
                    sp.GetRequiredService<TDep2>(),
                    sp.GetRequiredService<TDep3>(),
                    sp.GetRequiredService<TDep4>(),
                    configureOptions));
            return this;
        }

        /// <summary>
        /// Registers an action used to post configure a particular type of options.
        /// Note: These are run after all <seealso cref="Configure(Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the action.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the action.</typeparam>
        /// <typeparam name="TDep3">The third dependency used by the action.</typeparam>
        /// <typeparam name="TDep4">The fourth dependency used by the action.</typeparam>
        /// <typeparam name="TDep5">The fifth dependency used by the action.</typeparam>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> PostConfigure<TDep1, TDep2, TDep3, TDep4, TDep5>(Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> configureOptions)
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
            where TDep4 : class
            where TDep5 : class
        {
            ThrowHelper.ThrowIfNull(configureOptions);

            Services.AddTransient<IPostConfigureOptions<TOptions>>(
                sp => new PostConfigureOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5>(
                    Name,
                    sp.GetRequiredService<TDep1>(),
                    sp.GetRequiredService<TDep2>(),
                    sp.GetRequiredService<TDep3>(),
                    sp.GetRequiredService<TDep4>(),
                    sp.GetRequiredService<TDep5>(),
                    configureOptions));
            return this;
        }

        /// <summary>
        /// Register a validation action for an options type using a default failure message.
        /// </summary>
        /// <param name="validation">The validation function.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Validate(Func<TOptions, bool> validation)
            => Validate(validation: validation, failureMessage: DefaultValidationFailureMessage);

        /// <summary>
        /// Register a validation action for an options type.
        /// </summary>
        /// <param name="validation">The validation function.</param>
        /// <param name="failureMessage">The failure message to use when validation fails.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Validate(Func<TOptions, bool> validation, string failureMessage)
        {
            ThrowHelper.ThrowIfNull(validation);

            Services.AddSingleton<IValidateOptions<TOptions>>(new ValidateOptions<TOptions>(Name, validation, failureMessage));
            return this;
        }

        /// <summary>
        /// Register a validation action for an options type using a default failure message.
        /// </summary>
        /// <typeparam name="TDep">The dependency used by the validation function.</typeparam>
        /// <param name="validation">The validation function.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Validate<TDep>(Func<TOptions, TDep, bool> validation) where TDep : notnull
            => Validate(validation: validation, failureMessage: DefaultValidationFailureMessage);

        /// <summary>
        /// Register a validation action for an options type.
        /// </summary>
        /// <typeparam name="TDep">The dependency used by the validation function.</typeparam>
        /// <param name="validation">The validation function.</param>
        /// <param name="failureMessage">The failure message to use when validation fails.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Validate<TDep>(Func<TOptions, TDep, bool> validation, string failureMessage) where TDep : notnull
        {
            ThrowHelper.ThrowIfNull(validation);

            Services.AddTransient<IValidateOptions<TOptions>>(sp =>
                new ValidateOptions<TOptions, TDep>(Name, sp.GetRequiredService<TDep>(), validation, failureMessage));
            return this;
        }

        /// <summary>
        /// Register a validation action for an options type using a default failure message.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the validation function.</typeparam>
        /// <param name="validation">The validation function.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2>(Func<TOptions, TDep1, TDep2, bool> validation)
            where TDep1 : notnull
            where TDep2 : notnull
            => Validate(validation: validation, failureMessage: DefaultValidationFailureMessage);

        /// <summary>
        /// Register a validation action for an options type.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the validation function.</typeparam>
        /// <param name="validation">The validation function.</param>
        /// <param name="failureMessage">The failure message to use when validation fails.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2>(Func<TOptions, TDep1, TDep2, bool> validation, string failureMessage)
            where TDep1 : notnull
            where TDep2 : notnull
        {
            ThrowHelper.ThrowIfNull(validation);

            Services.AddTransient<IValidateOptions<TOptions>>(sp =>
                new ValidateOptions<TOptions, TDep1, TDep2>(Name,
                    sp.GetRequiredService<TDep1>(),
                    sp.GetRequiredService<TDep2>(),
                    validation,
                    failureMessage));
            return this;
        }

        /// <summary>
        /// Register a validation action for an options type using a default failure message.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep3">The third dependency used by the validation function.</typeparam>
        /// <param name="validation">The validation function.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2, TDep3>(Func<TOptions, TDep1, TDep2, TDep3, bool> validation)
            where TDep1 : notnull
            where TDep2 : notnull
            where TDep3 : notnull
            => Validate(validation: validation, failureMessage: DefaultValidationFailureMessage);

        /// <summary>
        /// Register a validation action for an options type.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep3">The third dependency used by the validation function.</typeparam>
        /// <param name="validation">The validation function.</param>
        /// <param name="failureMessage">The failure message to use when validation fails.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2, TDep3>(Func<TOptions, TDep1, TDep2, TDep3, bool> validation, string failureMessage)
            where TDep1 : notnull
            where TDep2 : notnull
            where TDep3 : notnull
        {
            ThrowHelper.ThrowIfNull(validation);

            Services.AddTransient<IValidateOptions<TOptions>>(sp =>
                new ValidateOptions<TOptions, TDep1, TDep2, TDep3>(Name,
                    sp.GetRequiredService<TDep1>(),
                    sp.GetRequiredService<TDep2>(),
                    sp.GetRequiredService<TDep3>(),
                    validation,
                    failureMessage));
            return this;
        }

        /// <summary>
        /// Register a validation action for an options type using a default failure message.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep3">The third dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep4">The fourth dependency used by the validation function.</typeparam>
        /// <param name="validation">The validation function.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2, TDep3, TDep4>(Func<TOptions, TDep1, TDep2, TDep3, TDep4, bool> validation)
            where TDep1 : notnull
            where TDep2 : notnull
            where TDep3 : notnull
            where TDep4 : notnull
            => Validate(validation: validation, failureMessage: DefaultValidationFailureMessage);

        /// <summary>
        /// Register a validation action for an options type.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep3">The third dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep4">The fourth dependency used by the validation function.</typeparam>
        /// <param name="validation">The validation function.</param>
        /// <param name="failureMessage">The failure message to use when validation fails.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2, TDep3, TDep4>(Func<TOptions, TDep1, TDep2, TDep3, TDep4, bool> validation, string failureMessage)
            where TDep1 : notnull
            where TDep2 : notnull
            where TDep3 : notnull
            where TDep4 : notnull
        {
            ThrowHelper.ThrowIfNull(validation);

            Services.AddTransient<IValidateOptions<TOptions>>(sp =>
                new ValidateOptions<TOptions, TDep1, TDep2, TDep3, TDep4>(Name,
                    sp.GetRequiredService<TDep1>(),
                    sp.GetRequiredService<TDep2>(),
                    sp.GetRequiredService<TDep3>(),
                    sp.GetRequiredService<TDep4>(),
                    validation,
                    failureMessage));
            return this;
        }

        /// <summary>
        /// Register a validation action for an options type using a default failure message.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep3">The third dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep4">The fourth dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep5">The fifth dependency used by the validation function.</typeparam>
        /// <param name="validation">The validation function.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2, TDep3, TDep4, TDep5>(Func<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, bool> validation)
            where TDep1 : notnull
            where TDep2 : notnull
            where TDep3 : notnull
            where TDep4 : notnull
            where TDep5 : notnull
            => Validate(validation: validation, failureMessage: DefaultValidationFailureMessage);

        /// <summary>
        /// Register a validation action for an options type.
        /// </summary>
        /// <typeparam name="TDep1">The first dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep2">The second dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep3">The third dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep4">The fourth dependency used by the validation function.</typeparam>
        /// <typeparam name="TDep5">The fifth dependency used by the validation function.</typeparam>
        /// <param name="validation">The validation function.</param>
        /// <param name="failureMessage">The failure message to use when validation fails.</param>
        /// <returns>The current <see cref="OptionsBuilder{TOptions}"/>.</returns>
        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2, TDep3, TDep4, TDep5>(Func<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, bool> validation, string failureMessage)
            where TDep1 : notnull
            where TDep2 : notnull
            where TDep3 : notnull
            where TDep4 : notnull
            where TDep5 : notnull
        {
            ThrowHelper.ThrowIfNull(validation);

            Services.AddTransient<IValidateOptions<TOptions>>(sp =>
                new ValidateOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5>(Name,
                    sp.GetRequiredService<TDep1>(),
                    sp.GetRequiredService<TDep2>(),
                    sp.GetRequiredService<TDep3>(),
                    sp.GetRequiredService<TDep4>(),
                    sp.GetRequiredService<TDep5>(),
                    validation,
                    failureMessage));
            return this;
        }
    }
}
