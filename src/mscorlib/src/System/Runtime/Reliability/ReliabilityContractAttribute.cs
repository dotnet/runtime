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

using System.Runtime.InteropServices;
using System;

namespace System.Runtime.ConstrainedExecution
{
    [Serializable]
    public enum Consistency : int
    {
        MayCorruptProcess = 0,
        MayCorruptAppDomain = 1,
        MayCorruptInstance = 2,
        WillNotCorruptState = 3,
    }

    [Serializable]
    public enum Cer : int
    {
        None = 0,
        MayFail = 1,  // Might fail, but the method will say it failed
        Success = 2,
    }

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Interface /* | AttributeTargets.Delegate*/, Inherited = false)]
    public sealed class ReliabilityContractAttribute : Attribute
    {
        private Consistency _consistency;
        private Cer _cer;

        public ReliabilityContractAttribute(Consistency consistencyGuarantee, Cer cer)
        {
            _consistency = consistencyGuarantee;
            _cer = cer;
        }

        public Consistency ConsistencyGuarantee
        {
            get { return _consistency; }
        }

        public Cer Cer
        {
            get { return _cer; }
        }
    }
}
