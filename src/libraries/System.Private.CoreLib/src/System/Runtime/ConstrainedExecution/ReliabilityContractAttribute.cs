// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.ConstrainedExecution
{
    /// <summary>
    /// Defines a contract for reliability between the author of some code, and the developers who have a dependency on that code.
    /// </summary>
    [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Interface /* | AttributeTargets.Delegate*/, Inherited = false)]
    public sealed class ReliabilityContractAttribute : Attribute
    {
        public ReliabilityContractAttribute(Consistency consistencyGuarantee, Cer cer)
        {
            ConsistencyGuarantee = consistencyGuarantee;
            Cer = cer;
        }

        public Consistency ConsistencyGuarantee { get; }
        public Cer Cer { get; }
    }
}
