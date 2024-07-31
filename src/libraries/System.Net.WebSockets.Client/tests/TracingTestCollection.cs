// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using TestUtilities;
using Xunit;

namespace System.Net.WebSockets.Client.Tests;

[CollectionDefinition(nameof(TracingTestCollection), DisableParallelization = true)]
public class TracingTestCollection : ICollectionFixture<TracingTestCollection>, IDisposable
{
    private static readonly Dictionary<string, int> s_unobservedExceptions = new Dictionary<string, int>();
    private static readonly EventHandler<UnobservedTaskExceptionEventArgs> eventHandler;
    private readonly TestEventListener s_listener;

    static TracingTestCollection()
    {
        eventHandler = (_, e) =>
        {
            lock (s_unobservedExceptions)
            {
                string text = e.Exception.ToString();
                s_unobservedExceptions[text] = s_unobservedExceptions.GetValueOrDefault(text) + 1;
            }
        };
    }

    public TracingTestCollection()
    {
        Console.WriteLine(Environment.NewLine + "===== Running TracingTestCollection =====" + Environment.NewLine);

        s_listener = new TestEventListener(Console.Out, "Private.InternalDiagnostics.System.Net.WebSockets");

        TaskScheduler.UnobservedTaskException += eventHandler;
    }

    public void Dispose()
    {
        s_listener.Dispose();

        Console.WriteLine(Environment.NewLine + "===== Disposing TracingTestCollection =====" + Environment.NewLine);

        TaskScheduler.UnobservedTaskException -= eventHandler;
        Console.WriteLine($"Unobserved exceptions of {s_unobservedExceptions.Count} different types: {Environment.NewLine}{string.Join(Environment.NewLine + new string('=', 120) + Environment.NewLine, s_unobservedExceptions.Select(pair => $"Count {pair.Value}: {pair.Key}"))}");
    }
}
