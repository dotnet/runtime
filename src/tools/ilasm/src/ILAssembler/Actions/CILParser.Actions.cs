// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace ILAssembler;

public partial class CILParser
{
    private readonly Stack<bool> _parseTreeModes = new();

    internal GrammarActions Actions { get; set; } = null!;

    private void BeginStreaming()
    {
        _parseTreeModes.Push(BuildParseTree);
        BuildParseTree = false;
    }

    private void EndParseTreeMode()
    {
        Debug.Assert(_parseTreeModes.Count > 0);
        BuildParseTree = _parseTreeModes.Pop();
    }

    internal void VerifyParseTreeModesBalanced() => Debug.Assert(_parseTreeModes.Count == 0);
}
