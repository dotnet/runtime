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
                && _pendingClassMethodOverrides.Count == 0,
            "Nested compiler state must be released by its owning declaration.");
        EndMethod();
        ResetTypeScopes();
        ClearPendingCustomAttributeOwners();
        _pendingClassMethodOverrides.Clear();
        _currentDocumentPath = null;
        _syntaxErrorCount = 0;
    }
}
