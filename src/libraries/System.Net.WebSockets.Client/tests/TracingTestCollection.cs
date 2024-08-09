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

    private static readonly EventHandler<UnobservedTaskExceptionEventArgs> s_eventHandler = (_, e) =>
        {
            lock (s_unobservedExceptions)
            {
                string text = e.Exception.ToString();
                s_unobservedExceptions[text] = s_unobservedExceptions.GetValueOrDefault(text) + 1;
            }
        };

    private static readonly FieldInfo s_ClientWebSocket_innerWebSocketField =
        typeof(ClientWebSocket).GetField("_innerWebSocket", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new Exception("Could not find ClientWebSocket._innerWebSocket field");
    private static readonly PropertyInfo s_WebSocketHandle_WebSocketProperty =
        typeof(ClientWebSocket).Assembly.GetType("System.Net.WebSockets.WebSocketHandle", throwOnError: true)!
            .GetProperty("WebSocket", BindingFlags.Instance | BindingFlags.Public)
        ?? throw new Exception("Could not find WebSocketHandle.WebSocket property");

    private static WebSocket GetUnderlyingWebSocket(ClientWebSocket clientWebSocket)
    {
        object? innerWebSocket = s_ClientWebSocket_innerWebSocketField.GetValue(clientWebSocket);
        if (innerWebSocket == null)
        {
            throw new Exception("ClientWebSocket._innerWebSocket is null");
        }

        return (WebSocket)s_WebSocketHandle_WebSocketProperty.GetValue(innerWebSocket);
    }


    public TracingTestCollection()
    {
        Console.WriteLine(Environment.NewLine + "===== Running TracingTestCollection =====" + Environment.NewLine);

        TaskScheduler.UnobservedTaskException += s_eventHandler;
    }

    public void Dispose()
    {
        Console.WriteLine(Environment.NewLine + "===== Disposing TracingTestCollection =====" + Environment.NewLine);

        TaskScheduler.UnobservedTaskException -= s_eventHandler;
        Console.WriteLine($"Unobserved exceptions of {s_unobservedExceptions.Count} different types: {Environment.NewLine}{string.Join(Environment.NewLine + new string('=', 120) + Environment.NewLine, s_unobservedExceptions.Select(pair => $"Count {pair.Value}: {pair.Key}"))}");
    }

    public static void TraceUnderlyingWebSocket(object obj, ClientWebSocket clientWebSocket)
    {
        var ws = GetUnderlyingWebSocket(clientWebSocket);
        Trace(obj, $"Underlying WebSocket: {ws.GetType().Name}#{ws.GetHashCode()}");
    }

    public static void Trace(object obj, string message) => Trace(obj.GetType().Name, message);

    public static void Trace(string objName, string message)
    {
        lock (Console.Out)
        {
            Console.WriteLine($"{objName} {DateTime.UtcNow:ss.fff} {message}");
        }
    }

    public static TestEventListener CreateTestEventListener(object testObject)
        => new TestEventListener(str => Trace(testObject.GetType().Name, str), enableActivityId: true , "Private.InternalDiagnostics.System.Net.WebSockets");
}
