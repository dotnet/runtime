// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.Connections
{
    public abstract partial class Connection : System.Net.Connections.ConnectionBase
    {
        protected Connection() { }
        public System.IO.Pipelines.IDuplexPipe Pipe { get { throw null; } }
        public System.IO.Stream Stream { get { throw null; } }
        protected virtual System.IO.Pipelines.IDuplexPipe CreatePipe() { throw null; }
        protected virtual System.IO.Stream CreateStream() { throw null; }
        public static System.Net.Connections.Connection FromPipe(System.IO.Pipelines.IDuplexPipe pipe, bool leaveOpen = false, System.Net.Connections.IConnectionProperties? properties = null, System.Net.EndPoint? localEndPoint = null, System.Net.EndPoint? remoteEndPoint = null) { throw null; }
        public static System.Net.Connections.Connection FromStream(System.IO.Stream stream, bool leaveOpen = false, System.Net.Connections.IConnectionProperties? properties = null, System.Net.EndPoint? localEndPoint = null, System.Net.EndPoint? remoteEndPoint = null) { throw null; }
    }
    public abstract partial class ConnectionBase : System.IAsyncDisposable, System.IDisposable
    {
        protected ConnectionBase() { }
        public abstract System.Net.Connections.IConnectionProperties ConnectionProperties { get; }
        public abstract System.Net.EndPoint? LocalEndPoint { get; }
        public abstract System.Net.EndPoint? RemoteEndPoint { get; }
        public System.Threading.Tasks.ValueTask CloseAsync(System.Net.Connections.ConnectionCloseMethod method = System.Net.Connections.ConnectionCloseMethod.GracefulShutdown, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected abstract System.Threading.Tasks.ValueTask CloseAsyncCore(System.Net.Connections.ConnectionCloseMethod method, System.Threading.CancellationToken cancellationToken);
        public void Dispose() { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
    }
    public enum ConnectionCloseMethod
    {
        GracefulShutdown = 0,
        Abort = 1,
        Immediate = 2,
    }
    public static partial class ConnectionExtensions
    {
        public static System.Net.Connections.ConnectionFactory Filter(this System.Net.Connections.ConnectionFactory factory, System.Func<System.Net.Connections.Connection, System.Net.Connections.IConnectionProperties?, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Net.Connections.Connection>> filter) { throw null; }
        public static bool TryGet<T>(this System.Net.Connections.IConnectionProperties properties, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out T property) { throw null; }
    }
    public abstract partial class ConnectionFactory : System.IAsyncDisposable, System.IDisposable
    {
        protected ConnectionFactory() { }
        public abstract System.Threading.Tasks.ValueTask<System.Net.Connections.Connection> ConnectAsync(System.Net.EndPoint? endPoint, System.Net.Connections.IConnectionProperties? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        protected virtual System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
    }
    public abstract partial class ConnectionListener : System.IAsyncDisposable, System.IDisposable
    {
        protected ConnectionListener() { }
        public abstract System.Net.Connections.IConnectionProperties ListenerProperties { get; }
        public abstract System.Net.EndPoint? LocalEndPoint { get; }
        public abstract System.Threading.Tasks.ValueTask<System.Net.Connections.Connection> AcceptAsync(System.Net.Connections.IConnectionProperties? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        protected virtual System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
    }
    public abstract partial class ConnectionListenerFactory : System.IAsyncDisposable, System.IDisposable
    {
        protected ConnectionListenerFactory() { }
        public abstract System.Threading.Tasks.ValueTask<System.Net.Connections.ConnectionListener> ListenAsync(System.Net.EndPoint? endPoint, System.Net.Connections.IConnectionProperties? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        protected virtual System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
    }
    public partial interface IConnectionProperties
    {
        bool TryGet(System.Type propertyKey, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out object? property);
    }
}
