// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Antlr4.Runtime;
using LabelHandle = ILAssembler.MethodBodyWriter.Label;

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
            // The diagnosed unresolved reference retains a zero target in error-tolerant output.
            _currentMethod.Definition.MethodBody.MarkLabel(_currentMethod.Labels[undefinedLabel.Key], 0);
        }
        _diagnostics.AddRange(_currentMethod.Definition.MethodBody.Complete(_currentMethod.Definition.ExceptionRegions));
    }

    private static LabelHandle GetOrCreateMethodLabel(
        CurrentMethodContext method,
        string name,
        IToken reference)
    {
        if (!method.Labels.TryGetValue(name, out LabelHandle label))
        {
            label = method.Definition.MethodBody.DefineLabel();
            method.Labels[name] = label;
            method.UndefinedLabelReferences.TryAdd(name, reference);
        }

        return label;
    }

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
    internal string GetMethodName(IToken token) => token.Text;
#pragma warning restore CA1822
}
