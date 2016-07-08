// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Transactions.Diagnostics
{
    internal static class DiagnosticTraceCode
    {
        private const string Prefix = "http://msdn.microsoft.com/TraceCodes/System/ActivityTracing/2004/07/";
        private const string DiagnosticsFeature = "Diagnostics/";
        private const string ReliabilityFeature = "Reliability/";

        internal const string ActivityIdSet = Prefix + DiagnosticsFeature + "ActivityId/Set";
        internal const string ActivityName = Prefix + DiagnosticsFeature + "ActivityId/Name";
        internal const string AppDomainUnload = Prefix + DiagnosticsFeature + "AppDomainUnload";
        internal const string NewActivityIdIssued = Prefix + DiagnosticsFeature + "ActivityId/IssuedNew";
        internal const string UnhandledException = Prefix + ReliabilityFeature + "Exception/Unhandled";
    }
}