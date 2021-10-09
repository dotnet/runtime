// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class ValidationHostedService : IHostedService
    {
        private readonly IDictionary<(Type, string), Action> _validators;

        public ValidationHostedService(IOptions<ValidatorOptions> validatorOptions)
        {
            _validators = validatorOptions?.Value?.Validators ?? throw new ArgumentNullException(nameof(validatorOptions));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var exceptions = new List<Exception>();

            foreach (var validate in _validators.Values)
            {
                try
                {
                    // Execute the validation method and catch the validation error
                    validate();
                }
                catch (OptionsValidationException ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count == 1)
            {
                // Rethrow if it's a single error
                ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
            }

            if (exceptions.Count > 1)
            {
                // Aggregate if we have many errors
                throw new AggregateException(exceptions);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}