using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal sealed class ArrayMarshallingCodeContext : StubCodeContext
    {
        public const string LocalManagedIdentifierSuffix = "_local";

        private readonly string indexerIdentifier;
        private readonly StubCodeContext parentContext;
        private readonly bool appendLocalManagedIdentifierSuffix;

        public override bool PinningSupported => false;

        public override bool StackSpaceUsable => false;

        /// <summary>
        /// Additional variables other than the {managedIdentifier} and {nativeIdentifier} variables
        /// can be added to the stub to track additional state for the marshaller in the stub.
        /// </summary>
        /// <remarks>
        /// Currently, array scenarios do not support declaring additional temporary variables to support
        /// marshalling. This can be accomplished in the future with some additional infrastructure to support
        /// declaring arrays additional arrays in the stub to support the temporary state.
        /// </remarks>
        public override bool CanUseAdditionalTemporaryState => false;

        /// <summary>
        /// Create a <see cref="StubCodeContext"/> for marshalling elements of an array.
        /// </summary>
        /// <param name="currentStage">The current marshalling stage.</param>
        /// <param name="indexerIdentifier">The indexer in the loop to get the element to marshal from the array.</param>
        /// <param name="parentContext">The parent context.</param>
        /// <param name="appendLocalManagedIdentifierSuffix">
        /// For array marshalling, we sometimes cache the array in a local to avoid multithreading issues.
        /// Set this to <c>true</c> to add the <see cref="LocalManagedIdentifierSuffix"/> to the managed identifier when
        /// marshalling the array elements to ensure that we use the local copy instead of the managed identifier
        /// when marshalling elements.
        /// </param>
        public ArrayMarshallingCodeContext(
            Stage currentStage,
            string indexerIdentifier,
            StubCodeContext parentContext,
            bool appendLocalManagedIdentifierSuffix)
        {
            CurrentStage = currentStage;
            this.indexerIdentifier = indexerIdentifier;
            this.parentContext = parentContext;
            this.appendLocalManagedIdentifierSuffix = appendLocalManagedIdentifierSuffix;
        }

        /// <summary>
        /// Get managed and native instance identifiers for the <paramref name="info"/>
        /// </summary>
        /// <param name="info">Object for which to get identifiers</param>
        /// <returns>Managed and native identifiers</returns>
        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            var (managed, native) = parentContext.GetIdentifiers(info);
            if (appendLocalManagedIdentifierSuffix)
            {
                managed += LocalManagedIdentifierSuffix;
            }
            return ($"{managed}[{indexerIdentifier}]", $"{native}[{indexerIdentifier}]");
        }

        public override TypePositionInfo? GetTypePositionInfoForManagedIndex(int index)
        {
            // We don't have parameters to look at when we're in the middle of marshalling an array.
            return null;
        }
    }
}
