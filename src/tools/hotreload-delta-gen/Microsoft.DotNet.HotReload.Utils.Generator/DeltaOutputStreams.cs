// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.HotReload.Utils.Generator;
public sealed class DeltaOutputStreams : IAsyncDisposable {
    public Stream MetaStream {get; private set;}
    public Stream  IlStream {get; private set;}
    public Stream PdbStream {get; private set;}

    public Stream UpdateHandlerInfoStream {get; private set;}

    public DeltaOutputStreams(Stream dmeta, Stream dil, Stream dpdb, Stream updateHandlerInfo) {
        MetaStream = dmeta;
        IlStream = dil;
        PdbStream = dpdb;
        UpdateHandlerInfoStream = updateHandlerInfo;
    }

    public void Dispose () {
        MetaStream?.Dispose();
        IlStream?.Dispose();
        PdbStream?.Dispose();
        UpdateHandlerInfoStream?.Dispose();
    }

    public async ValueTask DisposeAsync () {
        if  (MetaStream != null) await MetaStream.DisposeAsync();
        if  (IlStream != null) await IlStream.DisposeAsync();
        if  (PdbStream != null) await PdbStream.DisposeAsync();
        if  (UpdateHandlerInfoStream != null) await UpdateHandlerInfoStream.DisposeAsync();
    }

}
