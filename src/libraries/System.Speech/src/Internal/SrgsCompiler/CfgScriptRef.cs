// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.SrgsCompiler
{
    /// <summary>
    /// Summary description for CfgScriptRef.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct CfgScriptRef
    {
        #region Internal Fields

        // should be private but the order is absolutly key for marshalling
        internal int _idRule;

        internal int _idMethod;

        internal RuleMethodScript _method;

        #endregion
    }
}
