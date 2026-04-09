// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Options
{
    internal sealed class StartupValidator : IStartupValidator
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
    }
}
