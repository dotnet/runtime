// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private void ValidateLabelReferences()
    {
        if (_currentMethod is null)
        {
            return;
        }

        // Report errors for any labels that were referenced but never declared
        foreach (var undefinedLabel in _currentMethod.UndefinedLabelReferences)
        {
            ReportError(
                DiagnosticIds.LabelNotFound,
                string.Format(DiagnosticMessageTemplates.LabelNotFound, undefinedLabel.Key),
                undefinedLabel.Value);
        }
    }

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
    internal string GetMethodName(IToken token) => token.Text;
#pragma warning restore CA1822
}
