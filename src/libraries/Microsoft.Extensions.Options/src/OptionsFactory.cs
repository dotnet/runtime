// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of <see cref="IOptionsFactory{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">The type of options being requested.</typeparam>
    public class OptionsFactory<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IOptionsFactory<TOptions>
        where TOptions : class
    {
        private readonly IConfigureOptions<TOptions>[] _setups;
        private readonly IPostConfigureOptions<TOptions>[] _postConfigures;
        private readonly IValidateOptions<TOptions>[] _validations;

        /// <summary>
        /// Initializes a new instance with the specified options configurations.
        /// </summary>
        /// <param name="setups">The configuration actions to run.</param>
        /// <param name="postConfigures">The initialization actions to run.</param>
        public OptionsFactory(IEnumerable<IConfigureOptions<TOptions>> setups, IEnumerable<IPostConfigureOptions<TOptions>> postConfigures) : this(setups, postConfigures, validations: Array.Empty<IValidateOptions<TOptions>>())
        { }

        /// <summary>
        /// Initializes a new instance with the specified options configurations.
        /// </summary>
        /// <param name="setups">The configuration actions to run.</param>
        /// <param name="postConfigures">The initialization actions to run.</param>
        /// <param name="validations">The validations to run.</param>
        public OptionsFactory(IEnumerable<IConfigureOptions<TOptions>> setups, IEnumerable<IPostConfigureOptions<TOptions>> postConfigures, IEnumerable<IValidateOptions<TOptions>> validations)
        {
            // The default DI container uses arrays under the covers. Take advantage of this knowledge
            // by checking for an array and enumerate over that, so we don't need to allocate an enumerator.
            // When it isn't already an array, convert it to one, but don't use System.Linq to avoid pulling Linq in to
            // small trimmed applications.

            _setups = setups as IConfigureOptions<TOptions>[] ?? new List<IConfigureOptions<TOptions>>(setups).ToArray();
            _postConfigures = postConfigures as IPostConfigureOptions<TOptions>[] ?? new List<IPostConfigureOptions<TOptions>>(postConfigures).ToArray();
            _validations = validations as IValidateOptions<TOptions>[] ?? new List<IValidateOptions<TOptions>>(validations).ToArray();
        }

        /// <summary>
        /// Returns a configured <typeparamref name="TOptions"/> instance with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <typeparamref name="TOptions"/> instance to create.</param>
        /// <returns>The created <typeparamref name="TOptions"/> instance with the given <paramref name="name"/>.</returns>
        /// <exception cref="OptionsValidationException">One or more <see cref="IValidateOptions{TOptions}"/> return failed <see cref="ValidateOptionsResult"/> when validating the <typeparamref name="TOptions"/> instance created.</exception>
        /// <exception cref="MissingMethodException">The <typeparamref name="TOptions"/> does not have a public parameterless constructor or <typeparamref name="TOptions"/> is <see langword="abstract"/>.</exception>
        public TOptions Create(string name)
        {
            TOptions options = CreateInstance(name);
            foreach (IConfigureOptions<TOptions> setup in _setups)
            {
                if (setup is IConfigureNamedOptions<TOptions> namedSetup)
                {
                    namedSetup.Configure(name, options);
                }
                else if (name == Options.DefaultName)
                {
                    setup.Configure(options);
                }
            }
            foreach (IPostConfigureOptions<TOptions> post in _postConfigures)
            {
                post.PostConfigure(name, options);
            }

            if (_validations.Length > 0)
            {
                var failures = new List<string>();
                foreach (IValidateOptions<TOptions> validate in _validations)
                {
                    ValidateOptionsResult result = validate.Validate(name, options);
                    if (result is not null && result.Failed)
                    {
                        failures.AddRange(result.Failures);
                    }
                }
                if (failures.Count > 0)
                {
                    throw new OptionsValidationException(name, typeof(TOptions), failures);
                }
            }

            return options;
        }

        /// <summary>
        /// Creates a new instance of type <typeparamref name="TOptions"/>.
        /// </summary>
        /// <param name="name">The name of the <typeparamref name="TOptions"/> instance to create.</param>
        /// <returns>The created <typeparamref name="TOptions"/> instance.</returns>
        /// <exception cref="MissingMethodException">The <typeparamref name="TOptions"/> does not have a public parameterless constructor or <typeparamref name="TOptions"/> is <see langword="abstract"/>.</exception>
        protected virtual TOptions CreateInstance(string name)
        {
            return Activator.CreateInstance<TOptions>();
        }
    }

    internal sealed class OptionsFactoryWithAsyncValidation<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IOptionsFactory<TOptions>
        where TOptions : class
    {
        private readonly OptionsFactory<TOptions> _factory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScopeFactory? _scopeFactory;
        private readonly IValidateOptions<TOptions>[] _validations;
        private readonly bool _isRootProvider;

        public OptionsFactoryWithAsyncValidation(
            IEnumerable<IConfigureOptions<TOptions>> setups,
            IEnumerable<IPostConfigureOptions<TOptions>> postConfigures,
            IEnumerable<IValidateOptions<TOptions>> validations,
            IServiceProvider serviceProvider,
            OptionsRootServiceProvider rootServiceProvider)
        {
            _validations = validations as IValidateOptions<TOptions>[] ?? new List<IValidateOptions<TOptions>>(validations).ToArray();
            _factory = new OptionsFactory<TOptions>(setups, postConfigures, _validations);
            _serviceProvider = serviceProvider;
            _scopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
            _isRootProvider = ReferenceEquals(serviceProvider, rootServiceProvider.ServiceProvider);
        }

        public TOptions Create(string name)
        {
            string localName = name ?? Options.DefaultName;
            TOptions options = _factory.Create(localName);
            if (!OptionsAsyncValidation.IsSyncGuardSuppressed<TOptions>(localName))
            {
                ValidateAsyncValidators(localName, options);
            }

            return options;
        }

        private void ValidateAsyncValidators(string name, TOptions options)
        {
            IServiceScopeFactory? scopeFactory = _scopeFactory;
            if (scopeFactory is null ||
                !OptionsAsyncValidation.MayHaveAsyncValidators<TOptions>(_serviceProvider))
            {
                return;
            }

            if (_isRootProvider)
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                ValidateAsyncValidators(name, options, scope.ServiceProvider);

                return;
            }

            ValidateAsyncValidators(name, options, _serviceProvider);
        }

        private void ValidateAsyncValidators(string name, TOptions options, IServiceProvider serviceProvider)
        {
            List<string>? failures = null;
            IEnumerable<IAsyncValidateOptions<TOptions>> asyncValidations = serviceProvider.GetServices<IAsyncValidateOptions<TOptions>>();
            foreach (IAsyncValidateOptions<TOptions> validate in asyncValidations)
            {
                if (IsAlreadyValidated(validate))
                {
                    continue;
                }

                ValidateOptionsResult result = validate.Validate(name, options);
                if (result is not null && result.Failed)
                {
                    failures ??= new List<string>();
                    failures.AddRange(result.Failures);
                }
            }

            if (failures is not null && failures.Count > 0)
            {
                throw new OptionsValidationException(name, typeof(TOptions), failures);
            }
        }

        private bool IsAlreadyValidated(IAsyncValidateOptions<TOptions> validate)
        {
            foreach (IValidateOptions<TOptions> syncValidation in _validations)
            {
                if (ReferenceEquals(validate, syncValidation))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class OptionsRootServiceProvider
    {
        public OptionsRootServiceProvider(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }
    }
}
