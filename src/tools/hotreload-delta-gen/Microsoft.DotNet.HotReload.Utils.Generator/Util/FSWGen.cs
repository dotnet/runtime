// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.DotNet.HotReload.Utils.Generator.Util;
public class FSWGen : IDisposable {

    Channel<System.IO.FileSystemEventArgs>? _channel;
    readonly System.IO.FileSystemWatcher _fsw;

    public FSWGen (string directoryPath, string filter)
    {
        _channel = Channel.CreateUnbounded<System.IO.FileSystemEventArgs> (new UnboundedChannelOptions { SingleReader = true, AllowSynchronousContinuations = true});
        _fsw = new System.IO.FileSystemWatcher(directoryPath, filter) {
            NotifyFilter = System.IO.NotifyFilters.LastWrite /* FIXME: generalize */
        };
        _fsw.Changed += OnChanged;
        // _fsw.Created += OnChanged;
        // _fsw.Deleted += OnChanged; // FIXME: deletion is interesting, actually
    }

    private void OnChanged (object sender, System.IO.FileSystemEventArgs eventArgs)
    {
        _channel?.Writer.WriteAsync (eventArgs).AsTask().Wait();
    }

    ~FSWGen () => Dispose (false);

    public void Dispose () {
        Dispose (true);
        GC.SuppressFinalize (this);
    }

    public virtual void Dispose (bool disposing)
    {
        if (disposing) {
            _fsw.EnableRaisingEvents = false;
            _fsw.Dispose();

            _channel?.Writer.Complete();
            _channel = null;
        }
    }


    enum WhenAnyResult {
        Completion,
        Read
    }
    public async IAsyncEnumerable<System.IO.FileSystemEventArgs> Watch ([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try {
            _fsw.EnableRaisingEvents = true;
            var completion = _channel!.Reader.Completion.ContinueWith((t) => WhenAnyResult.Completion);
            while (true) {
                var readOne = _channel!.Reader.ReadAsync(cancellationToken).AsTask();
                Task<WhenAnyResult> t = await Task.WhenAny(completion, readOne.ContinueWith((t) => WhenAnyResult.Read)).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                switch (t.Result) {
                    case WhenAnyResult.Completion:
                        yield break;
                    case WhenAnyResult.Read:
                        yield return readOne.Result;
                        break;
                }
            }
        } finally {
            var fsw = _fsw;
            if (fsw != null)
                fsw.EnableRaisingEvents = false;
        }
    }
}
