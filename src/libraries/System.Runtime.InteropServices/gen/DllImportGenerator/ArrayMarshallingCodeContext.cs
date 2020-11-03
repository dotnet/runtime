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
        private readonly string indexerIdentifier;
        private readonly StubCodeContext parentContext;

        public override bool PinningSupported => false;

        public override bool StackSpaceUsable => false;

        public override bool CanUseAdditionalTemporaryState => false;

        public ArrayMarshallingCodeContext(Stage currentStage, string indexerIdentifier, StubCodeContext parentContext)
        {
            CurrentStage = currentStage;
            this.indexerIdentifier = indexerIdentifier;
            this.parentContext = parentContext;
        }

        /// <summary>
        /// Get managed and native instance identifiers for the <paramref name="info"/>
        /// </summary>
        /// <param name="info">Object for which to get identifiers</param>
        /// <returns>Managed and native identifiers</returns>
        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            var (managed, native) = parentContext.GetIdentifiers(info);
            return ($"{managed}[{indexerIdentifier}]", $"{native}[{indexerIdentifier}]");
        }

        public override TypePositionInfo? GetTypePositionInfoForManagedIndex(int index)
        {
            // We don't have parameters to look at when we're in the middle of marshalling an array.
            return null;
        }
    }
}
