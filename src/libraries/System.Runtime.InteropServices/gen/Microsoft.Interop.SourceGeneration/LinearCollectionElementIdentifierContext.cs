// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.Interop
{
    internal sealed record LinearCollectionElementIdentifierContext : StubIdentifierContext
    {
        private readonly StubIdentifierContext _globalContext;
        private readonly TypePositionInfo _elementInfo;
        private readonly string _managedSpanIdentifier;
        private readonly string _nativeSpanIdentifier;
        private readonly int _elementIndirectionLevel;

        public string IndexerIdentifier => MarshallerHelpers.GetIndexerIdentifier(_elementIndirectionLevel - 1);

        /// <summary>
        /// Create a <see cref="StubIdentifierContext"/> for marshalling elements of an collection.
        /// </summary>
        /// <param name="elementInfo">The type information for elements in the collection. Used to determine which identifiers to provide.</param>
        /// <param name="elementIndirectionLevel">The indirection level of the elements in the collection.</param>
        /// <param name="managedSpanIdentifier">The identifier of the managed value storage cast to the target element type.</param>
        /// <param name="nativeSpanIdentifier">The identifier of the native value storage cast to the target element type.</param>
        /// <param name="globalContext">The context in which we are marshalling the collection that owns these elements.</param>
        public LinearCollectionElementIdentifierContext(
            StubIdentifierContext globalContext,
            TypePositionInfo elementInfo,
            string managedSpanIdentifier,
            string nativeSpanIdentifier,
            int elementIndirectionLevel)
        {
            _globalContext = globalContext;
            _elementInfo = elementInfo;
            _managedSpanIdentifier = managedSpanIdentifier;
            _nativeSpanIdentifier = nativeSpanIdentifier;
            _elementIndirectionLevel = elementIndirectionLevel;
        }

        /// <summary>
        /// Get managed and native instance identifiers for the <paramref name="info"/>
        /// </summary>
        /// <param name="info">Object for which to get identifiers</param>
        /// <returns>Managed and native identifiers</returns>
        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            // For this element info, index into the marshaller spans.
            if (_elementInfo.PositionsEqual(info))
            {
                return (
                    $"{_managedSpanIdentifier}[{IndexerIdentifier}]",
                    $"{_nativeSpanIdentifier}[{IndexerIdentifier}]"
                );
            }
            // For other element infos, return the names from the global context.
            else
            {
                return _globalContext.GetIdentifiers(info);
            }
        }

        public override string GetAdditionalIdentifier(TypePositionInfo info, string name)
        {
            return $"{_nativeSpanIdentifier}__{IndexerIdentifier}__{name}";
        }
    }
}
