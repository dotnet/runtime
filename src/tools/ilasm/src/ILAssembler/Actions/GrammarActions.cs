// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    /// <summary>
    /// Resets the semantic state that must not flow from one document to the next.
    /// </summary>
    /// <remarks>
    /// The same <see cref="GrammarActions"/> instance compiles every document of a compilation so
    /// that they share an entity registry. Every rule that introduces namespace, type, method or
    /// scope state releases it from its own <c>finally</c> block, so this is only a safety net for
    /// release builds.
    /// </remarks>
    internal void BeginDocument()
    {
        Debug.Assert(
            _currentMethod is null
                && _typeOwners.Count == 0
                && _namespaceOwners.Count == 0
                && _scopeStack.Count == 0
                && _semanticRootFrames.Count == 0
                && _dottedNameFrames.Count == 0
                && _slashedNameFrames.Count == 0
                && _typeSignatureFrames.Count == 0
                && _typeArgumentsFrames.Count == 0
                && _boundsFrames.Count == 0
                && _signatureArgumentsFrames.Count == 0
                && _parameterAttributesFrames.Count == 0,
            "Per-document semantic state must be released by the owning rule's finally block.");

        EndMethod();
        ResetTypeScopes();
        ClearPendingCustomAttributeOwners();
        _semanticRootFrames.Clear();
        _dottedNameFrames.Clear();
        _slashedNameFrames.Clear();
        _typeSignatureFrames.Clear();
        _typeArgumentsFrames.Clear();
        _boundsFrames.Clear();
        _signatureArgumentsFrames.Clear();
        _parameterAttributesFrames.Clear();
        _syntaxErrorCount = 0;
    }
}
