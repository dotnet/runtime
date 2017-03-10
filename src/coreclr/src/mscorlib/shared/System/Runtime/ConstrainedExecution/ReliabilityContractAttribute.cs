// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
/*============================================================
**
**
**
** Purpose: Defines a publically documentable contract for 
** reliability between a method and its callers, expressing
** what state will remain consistent in the presence of 
** failures (ie async exceptions like thread abort) and whether
** the method needs to be called from within a CER.
**
**
===========================================================*/

namespace System.Runtime.ConstrainedExecution
{
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
