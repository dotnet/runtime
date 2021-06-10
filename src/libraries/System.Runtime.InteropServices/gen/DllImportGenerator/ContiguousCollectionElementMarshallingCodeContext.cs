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
    internal sealed class ContiguousCollectionElementMarshallingCodeContext : StubCodeContext
    {
        private readonly string indexerIdentifier;
        private readonly string nativeSpanIdentifier;
        private readonly StubCodeContext parentContext;

        public override bool SingleFrameSpansNativeContext => false;

        public override bool AdditionalTemporaryStateLivesAcrossStages => false;

        /// <summary>
        /// Create a <see cref="StubCodeContext"/> for marshalling elements of an collection.
        /// </summary>
        /// <param name="currentStage">The current marshalling stage.</param>
        /// <param name="indexerIdentifier">The indexer in the loop to get the element to marshal from the collection.</param>
        /// <param name="nativeSpanIdentifier">The identifier of the native value storage cast to the target element type.</param>
        /// <param name="parentContext">The parent context.</param>
        public ContiguousCollectionElementMarshallingCodeContext(
            Stage currentStage,
            string indexerIdentifier,
            string nativeSpanIdentifier,
            StubCodeContext parentContext)
        {
            CurrentStage = currentStage;
            this.indexerIdentifier = indexerIdentifier;
            this.nativeSpanIdentifier = nativeSpanIdentifier;
            this.parentContext = parentContext;
        }

        /// <summary>
        /// Get managed and native instance identifiers for the <paramref name="info"/>
        /// </summary>
        /// <param name="info">Object for which to get identifiers</param>
        /// <returns>Managed and native identifiers</returns>
        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            var (_, native) = parentContext.GetIdentifiers(info);
            return (
                $"{native}.ManagedValues[{indexerIdentifier}]",
                $"{nativeSpanIdentifier}[{indexerIdentifier}]"
            );
        }

        public override string GetAdditionalIdentifier(TypePositionInfo info, string name)
        {
            return $"{nativeSpanIdentifier}__{indexerIdentifier}__{name}";
        }

        public override TypePositionInfo? GetTypePositionInfoForManagedIndex(int index)
        {
            // We don't have parameters to look at when we're in the middle of marshalling an array.
            return null;
        }
    }
}
