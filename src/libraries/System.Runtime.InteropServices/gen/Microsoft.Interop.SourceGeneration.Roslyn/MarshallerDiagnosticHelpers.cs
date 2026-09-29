// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop;

/// <summary>Reports marshalling diagnostics that require compiler source locations.</summary>
public static class MarshallerDiagnosticHelpers
{
    extension(MarshallerHelpers)
    {
        /// <summary>
        /// Ensures that the count of a collection is available at call time if the parameter is not an out parameter.
        /// Only the outer collection is checked.
        /// </summary>
        /// <param name="stubDirection">The direction of the stub.</param>
        /// <param name="info">The collection's positional information.</param>
        /// <param name="generatorDiagnostics">The compiler diagnostics bag.</param>
        /// <param name="symbol">The containing method symbol.</param>
        /// <param name="outParamDescriptor">The descriptor for an unavailable output parameter count.</param>
        /// <param name="returnValueDescriptor">The descriptor for an unavailable return value count.</param>
        public static void ValidateCountInfoAvailableAtCall(MarshalDirection stubDirection, TypePositionInfo info, GeneratorDiagnosticsBag generatorDiagnostics, IMethodSymbol symbol, DiagnosticDescriptor outParamDescriptor, DiagnosticDescriptor returnValueDescriptor)
        {
            // In managed to unmanaged stubs, we can always just get the length of the managed object.
            if (stubDirection is MarshalDirection.ManagedToUnmanaged)
                return;

            if (!(info.RefKind is RefKind.Out
                    || info.ManagedIndex is TypePositionInfo.ReturnIndex)
                && info.MarshallingAttributeInfo is NativeLinearCollectionMarshallingInfo collectionMarshallingInfo
                && collectionMarshallingInfo.ElementCountInfo is CountElementCountInfo countInfo)
            {
                if (countInfo.ElementInfo.IsByRef && countInfo.ElementInfo.RefKind is RefKind.Out)
                {
                    Location location = TypePositionInfo.GetLocation(info, symbol);
                    generatorDiagnostics.ReportDiagnostic(
                        DiagnosticInfo.Create(
                            outParamDescriptor,
                            location,
                            info.InstanceIdentifier,
                            countInfo.ElementInfo.InstanceIdentifier));
                }
                else if (countInfo.ElementInfo.ManagedIndex is TypePositionInfo.ReturnIndex)
                {
                    Location location = TypePositionInfo.GetLocation(info, symbol);
                    generatorDiagnostics.ReportDiagnostic(
                        DiagnosticInfo.Create(
                            returnValueDescriptor,
                            location,
                            info.InstanceIdentifier));
                }
                // If the parameter is multidimensional and a higher indirection level parameter is ByValue [Out], then we should warn.
            }
        }
    }
}
