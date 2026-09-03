// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Runtime_133120
{
    private static readonly Dictionary<int, Func<CancellationToken, Task>> s_validators = new()
    {
        [0] = static _ => Task.CompletedTask,
    };

    [Fact]
    public static void TestEntryPoint()
    {
        ValidateAsync().GetAwaiter().GetResult();
    }

    private static async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        List<Exception>? exceptions = null;

        foreach (Func<CancellationToken, Task> validator in s_validators.Values)
        {
            try
            {
                await validator(cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
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
