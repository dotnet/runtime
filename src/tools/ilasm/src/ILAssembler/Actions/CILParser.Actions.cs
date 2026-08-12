// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;

namespace ILAssembler;

public partial class CILParser
{
    private readonly Stack<bool> _parseTreeModes = new();
    private bool _detachNextOuterAlternative;

    internal GrammarActions Actions { get; set; } = null!;

    private void BeginSubtree()
    {
        bool previousMode = BuildParseTree;
        _parseTreeModes.Push(previousMode);
        BuildParseTree = true;
        _detachNextOuterAlternative = !previousMode;
    }

    private void BeginStreaming()
    {
        _parseTreeModes.Push(BuildParseTree);
        BuildParseTree = false;
    }

    private void EndParseTreeMode()
    {
        Debug.Assert(_parseTreeModes.Count > 0);
        _detachNextOuterAlternative = false;
        BuildParseTree = _parseTreeModes.Pop();
    }

    public override void EnterOuterAlt(ParserRuleContext localContext, int alternativeNumber)
    {
        if (!_detachNextOuterAlternative)
        {
            base.EnterOuterAlt(localContext, alternativeNumber);
            return;
        }

        _detachNextOuterAlternative = false;
        BuildParseTree = false;
        try
        {
            base.EnterOuterAlt(localContext, alternativeNumber);
        }
        finally
        {
            BuildParseTree = true;
        }
    }

    internal void VerifyParseTreeModesBalanced() => Debug.Assert(_parseTreeModes.Count == 0);
}
