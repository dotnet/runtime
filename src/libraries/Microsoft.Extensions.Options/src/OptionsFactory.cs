// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
            _setups = setups as IConfigureOptions<TOptions>[] ?? setups.ToArray();
            _postConfigures = postConfigures as IPostConfigureOptions<TOptions>[] ?? postConfigures.ToArray();
            _validations = validations as IValidateOptions<TOptions>[] ?? validations.ToArray();
        }

        /// <summary>
        /// Returns a configured <typeparamref name="TOptions"/> instance with the given <paramref name="name"/>.
        /// </summary>
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

            if (_validations != null)
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
        /// Creates a new instance of options type
        /// </summary>
        protected virtual TOptions CreateInstance(string name)
        {
            return Activator.CreateInstance<TOptions>();
        }
    }
}
