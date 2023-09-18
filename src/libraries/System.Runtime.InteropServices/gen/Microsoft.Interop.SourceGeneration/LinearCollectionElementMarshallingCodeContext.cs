// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Interop
{
    internal sealed record LinearCollectionElementMarshallingCodeContext : StubCodeContext
    {
        private readonly string _managedSpanIdentifier;
        private readonly string _nativeSpanIdentifier;

        public override bool SingleFrameSpansNativeContext => false;

        public override bool AdditionalTemporaryStateLivesAcrossStages => false;

        public string IndexerIdentifier { get; }

        /// <summary>
        /// Create a <see cref="StubCodeContext"/> for marshalling elements of an collection.
        /// </summary>
        /// <param name="currentStage">The current marshalling stage.</param>
        /// <param name="indexerIdentifier">The indexer in the loop to get the element to marshal from the collection.</param>
        /// <param name="nativeSpanIdentifier">The identifier of the native value storage cast to the target element type.</param>
        /// <param name="parentContext">The parent context.</param>
        public LinearCollectionElementMarshallingCodeContext(
            Stage currentStage,
            string managedSpanIdentifier,
            string nativeSpanIdentifier,
            StubCodeContext parentContext)
        {
            CurrentStage = currentStage;
            IndexerIdentifier = CalculateIndexerIdentifierBasedOnParentContext(parentContext);
            _managedSpanIdentifier = managedSpanIdentifier;
            _nativeSpanIdentifier = nativeSpanIdentifier;
            ParentContext = parentContext;
            Direction = ParentContext.Direction;
            CodeEmitOptions = ParentContext.CodeEmitOptions;
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

        private static string CalculateIndexerIdentifierBasedOnParentContext(StubCodeContext? parentContext)
        {
            int i = 0;
            while (parentContext is StubCodeContext context)
            {
                if (context is LinearCollectionElementMarshallingCodeContext)
                {
                    i++;
                }
                parentContext = context.ParentContext;
            }

            // Follow a progression of indexers of the following form:
            // __i0, __i1, __i2, __i3, etc/
            return $"__i{i}";
        }
    }
}
