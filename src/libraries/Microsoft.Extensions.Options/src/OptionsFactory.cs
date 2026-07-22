// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

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
            TOptions options = CreateAndConfigure(name);

            if (_validations.Length > 0)
            {
                List<string>? failures = null;
                foreach (IValidateOptions<TOptions> validate in _validations)
                {
                    CollectFailures(ref failures, validate.Validate(name, options));
                }
                ThrowIfValidationFailed(name, failures);
            }

            return options;
        }

        /// <summary>
        /// Creates, configures, and asynchronously validates a <typeparamref name="TOptions"/> instance with the given <paramref name="name"/>.
        /// </summary>
        internal async Task<TOptions> CreateAsync(string name, CancellationToken cancellationToken)
        {
            TOptions options = CreateAndConfigure(name);

            if (_validations.Length > 0)
            {
                List<string>? failures = null;
                foreach (IValidateOptions<TOptions> validate in _validations)
                {
                    // Dispatch in registration order depending on capability:
                    // async validators are awaited, all others run synchronously.
                    ValidateOptionsResult result = validate is IAsyncValidateOptions<TOptions> asyncValidate
                        ? await asyncValidate.ValidateAsync(name, options, cancellationToken).ConfigureAwait(false)
                        : validate.Validate(name, options);

                    CollectFailures(ref failures, result);
                }
                ThrowIfValidationFailed(name, failures);
            }

            return options;
        }

        private TOptions CreateAndConfigure(string name)
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
            return options;
        }

        private static void CollectFailures(ref List<string>? failures, ValidateOptionsResult? result)
        {
            if (result is not null && result.Failed)
            {
                (failures ??= new List<string>()).AddRange(result.Failures);
            }
        }

        private static void ThrowIfValidationFailed(string name, List<string>? failures)
        {
            if (failures is { Count: > 0 })
            {
                throw new OptionsValidationException(name, typeof(TOptions), failures);
            }
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
}
