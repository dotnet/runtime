// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.Interop
{
    internal sealed record LinearCollectionElementMarshallingCodeContext : StubIdentifierContext
    {
        private readonly string _managedSpanIdentifier;
        private readonly string _nativeSpanIdentifier;

        public string IndexerIdentifier => MarshallerHelpers.GetIndexerIdentifier(CodeContext.ElementIndirectionLevel - 1);

        /// <summary>
        /// Create a <see cref="StubIdentifierContext"/> for marshalling elements of an collection.
        /// </summary>
        /// <param name="currentStage">The current marshalling stage.</param>
        /// <param name="indexerIdentifier">The indexer in the loop to get the element to marshal from the collection.</param>
        /// <param name="nativeSpanIdentifier">The identifier of the native value storage cast to the target element type.</param>
        /// <param name="parentContext">The parent context.</param>
        public LinearCollectionElementMarshallingCodeContext(
            Stage currentStage,
            string managedSpanIdentifier,
            string nativeSpanIdentifier)
        {
            CurrentStage = currentStage;
            _managedSpanIdentifier = managedSpanIdentifier;
            _nativeSpanIdentifier = nativeSpanIdentifier;
        }

        /// <summary>
        /// Get managed and native instance identifiers for the <paramref name="info"/>
        /// </summary>
        /// <param name="info">Object for which to get identifiers</param>
        /// <returns>Managed and native identifiers</returns>
        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            return (
                $"{_managedSpanIdentifier}[{IndexerIdentifier}]",
                $"{_nativeSpanIdentifier}[{IndexerIdentifier}]"
            );
        }

        public override string GetAdditionalIdentifier(TypePositionInfo info, string name)
        {
            return $"{_nativeSpanIdentifier}__{IndexerIdentifier}__{name}";
        }
    }
}
