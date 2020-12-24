// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.SrgsCompiler
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CfgScriptRef
    {
        #region Internal Fields

        // should be private but the order is absolutely key for marshalling
        internal int _idRule;

        internal int _idMethod;

        internal RuleMethodScript _method;

        #endregion
    }
}
