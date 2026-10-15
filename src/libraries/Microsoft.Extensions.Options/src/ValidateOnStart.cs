// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Options
{
#pragma warning disable SYSLIB0066 // IStartupValidator is obsolete but retained for compatibility.
    internal sealed class StartupValidator : IStartupValidator, IAsyncStartupValidator
#pragma warning restore SYSLIB0066
    {
        private readonly StartupValidatorOptions _validatorOptions;

        public StartupValidator(IOptions<StartupValidatorOptions> validators)
        {
            _validatorOptions = validators.Value;
        }

        public void Validate()
        {
            List<Exception>? exceptions = null;

            foreach (Action validator in _validatorOptions._validators.Values)
            {
                try
                {
                    // Execute the validation method and catch the validation error
                    validator();
                }
                catch (OptionsValidationException ex)
                {
                    exceptions ??= new();
                    exceptions.Add(ex);
                }
                catch (Exception ex)
                {
                    (exceptions ??= new()).Add(ex);
                    break;
                }
            }

            if (exceptions != null)
            {
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
            }
        }

        public async Task ValidateAsync(CancellationToken cancellationToken = default)
        {
            List<Exception>? exceptions = null;

            foreach (Func<CancellationToken, Task> asyncValidator in _validatorOptions._asyncValidators.Values)
            {
                try
                {
                    await asyncValidator(cancellationToken).ConfigureAwait(false);
                }
                catch (OptionsValidationException ex)
                {
                    exceptions ??= new();
                    exceptions.Add(ex);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    (exceptions ??= new()).Add(ex);
                    break;
                }
            }

            if (exceptions is not null)
            {
                if (exceptions.Count == 1)
                {
                    ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
                }

                if (exceptions.Count > 1)
                {
                    throw new AggregateException(exceptions);
                }
            }
        }
    }
}
