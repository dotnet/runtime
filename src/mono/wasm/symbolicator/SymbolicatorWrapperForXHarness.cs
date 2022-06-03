// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.DotNet.XHarness.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.WebAssembly.Internal;

public class SymbolicatorWrapperForXHarness : WasmSymbolicatorBase
{
    private WasmSymbolicator? _symbolicator;

    public override bool Init(string? symbolsMapFile, string? symbolPatternsFile, ILogger logger)
    {
        if (!base.Init(symbolsMapFile, symbolPatternsFile, logger))
            return false;

        _symbolicator = new WasmSymbolicator(SymbolsFile, SymbolsPatternFile, throwOnMissing: false, logger);
        if (!_symbolicator.CanSymbolicate)
        {
            _symbolicator = null;
            return false;
        }

        return true;
    }

    public override string Symbolicate(string msg)
    {
        if (_symbolicator is null)
            return msg;

        return _symbolicator.Symbolicate(msg);
    }
}
