// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics;

internal sealed class RunLoop : IDisposable
{
    public event EventHandler<RunLoopExitState>? RunLoopStopped;
    public bool IsRunning => StoppedState is null;
    public RunLoopExitState? StoppedState { get; private set; }

    private TaskCompletionSource<Exception> _failRequested { get; } = new();
    private TaskCompletionSource _shutdownRequested { get; } = new();
    private readonly ChannelWriter<Task> _channelWriter;
    private readonly ChannelReader<Task> _channelReader;
    private readonly DevToolsQueue[] _queues;
    private readonly ILogger _logger;

    public RunLoop(DevToolsQueue[] queues, ILogger logger)
    {
        if (queues.Length == 0)
            throw new ArgumentException($"Minimum of one queue need to run", nameof(queues));

        foreach (DevToolsQueue q in queues)
        {
            if (q.Connection.OnReadAsync is null)
                throw new ArgumentException($"Queue's({q.Id}) connection doesn't have a OnReadAsync handler set");
        }

        _logger = logger;
        _queues = queues;

        var channel = Channel.CreateUnbounded<Task>(new UnboundedChannelOptions { SingleReader = true });
        _channelWriter = channel.Writer;
        _channelReader = channel.Reader;
    }

    public Task RunAsync(CancellationTokenSource cts)
        => Task.Run(async () =>
        {
            RunLoopExitState exitState;

            try
            {
                exitState = await RunActualAsync(cts);
                StoppedState = exitState;
            }
            catch (Exception ex)
            {
                _channelWriter.Complete(ex);
                _logger.LogDebug($"RunLoop threw an exception: {ex}");
                StoppedState = new(RunLoopStopReason.Exception, ex);
                RunLoopStopped?.Invoke(this, StoppedState);
                return;
            }
            finally
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
            }

            try
            {
                _logger.LogDebug($"RunLoop stopped, reason: {exitState}");
                RunLoopStopped?.Invoke(this, exitState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Invoking RunLoopStopped event ({exitState}) failed with {ex}");
            }
        });

    private async Task<RunLoopExitState> RunActualAsync(CancellationTokenSource x)
    {
        List<Task> pending_ops;
        List<Task> tmp_ops = new();
        int numFixed;

        // Fixed index tasks
        {
            pending_ops = new();

            for (int i = 0; i < _queues.Length; i++)
                pending_ops.Add(_queues[i].Connection.ReadOneAsync(x.Token));
            pending_ops.Add(_failRequested.Task);
            pending_ops.Add(_shutdownRequested.Task);

            numFixed = pending_ops.Count;
        }

        Task<bool> readerTask = _channelReader.WaitToReadAsync(x.Token).AsTask();
        pending_ops.Add(readerTask);

        int numQueues = _queues.Length;
        while (!x.IsCancellationRequested)
        {
            Task completedTask = await Task.WhenAny(pending_ops.ToArray()).ConfigureAwait(false);

            if (_shutdownRequested.Task.IsCompleted)
                return new(RunLoopStopReason.Shutdown, null);
            if (_failRequested.Task.IsCompleted)
                return new(RunLoopStopReason.Exception, await _failRequested.Task);

            int completedIdx = pending_ops.IndexOf(completedTask);
            if (completedTask.IsFaulted)
            {
                return (completedIdx < numQueues && !_queues[completedIdx].Connection.IsConnected)
                            ? new(RunLoopStopReason.ConnectionClosed, new Exception($"Connection id: {_queues[completedIdx].Id}", completedTask.Exception))
                            : new(RunLoopStopReason.Exception, completedTask.Exception);
            }

            if (x.IsCancellationRequested)
                return new(RunLoopStopReason.Cancelled, null);

            // Ensure the fixed slots are filled
            for (int i = 0; i < numFixed; i++)
                tmp_ops.Add(pending_ops[i]);

            for (int queueIdx = 0; queueIdx < numQueues; queueIdx++)
            {
                DevToolsQueue curQueue = _queues[queueIdx];
                if (curQueue.TryPumpIfCurrentCompleted(x.Token, out Task? tsk))
                    tmp_ops.Add(tsk);

                Task queueReadTask = pending_ops[queueIdx];
                if (!queueReadTask.IsCompleted)
                    continue;

                string msg = await (Task<string>)queueReadTask;
                tmp_ops[queueIdx] = curQueue.Connection.ReadOneAsync(x.Token);
                if (msg != null)
                {
                    Task? readHandlerTask = curQueue.Connection.OnReadAsync?.Invoke(msg, x.Token);
                    if (readHandlerTask != null)
                        tmp_ops.Add(readHandlerTask);
                }
            }

            // Remaining tasks *after* the fixed ones
            for (int pendingOpsIdx = numFixed; pendingOpsIdx < pending_ops.Count; pendingOpsIdx++)
            {
                Task t = pending_ops[pendingOpsIdx];
                if (t.IsFaulted)
                    return new(RunLoopStopReason.Exception, t.Exception);
                if (t.IsCanceled)
                    return new(RunLoopStopReason.Cancelled, null);

                if (!t.IsCompleted)
                {
                    tmp_ops.Add(t);
                    continue;
                }
            }

            // Add any tasks that were received over the channel
            if (readerTask.IsCompleted)
            {
                while (_channelReader.TryRead(out Task? newTask))
                    tmp_ops.Add(newTask);

                readerTask = _channelReader.WaitToReadAsync(x.Token).AsTask();
                tmp_ops.Add(readerTask);
            }

            pending_ops = tmp_ops;
            tmp_ops = new(capacity: pending_ops.Count + 10);
        }

        _channelWriter.Complete();
        if (_shutdownRequested.Task.IsCompleted)
            return new(RunLoopStopReason.Shutdown, null);
        return x.IsCancellationRequested
                    ? new(RunLoopStopReason.Cancelled, null)
                    : new(RunLoopStopReason.Exception,
                                new InvalidOperationException($"This shouldn't ever get thrown. Unsure why the loop stopped"));
    }

    public Task Send(byte[] payload, CancellationToken token, DevToolsQueue? queue = null)
    {
        queue ??= _queues[0];
        Task? task = queue.Send(payload, token);
        return task is null
                ? Task.CompletedTask
                : _channelWriter.WriteAsync(task, token).AsTask();
    }

    public void Fail(Exception exception)
    {
        if (_failRequested.Task.IsCompleted)
            _logger.LogError($"Fail requested again with {exception}");
        else
            _failRequested.TrySetResult(exception);
    }

    // FIXME: Continue with to catch any errors in shutting down
    public void Shutdown() => Task.Run(async () => await ShutdownAsync(CancellationToken.None));

    public async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        if (_shutdownRequested.Task.IsCompleted)
        {
            _logger.LogDebug($"Shutdown was already requested once. Ignoring");
            return;
        }

        foreach (DevToolsQueue q in _queues)
            await q.Connection.ShutdownAsync(cancellationToken);

        _shutdownRequested.TrySetResult();
    }

    public void Dispose()
    {
        foreach (DevToolsQueue q in _queues)
            q.Connection.Dispose();
    }
}
