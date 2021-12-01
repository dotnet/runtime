// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.SrgsCompiler
{
    // list of rules with scripts
    [DebuggerDisplay("rule=\"{_rule}\" method=\"{_sMethod}\" operation=\"{_method.ToString ()}\"")]
    internal class ScriptRef
    {
        #region Constructors

        internal ScriptRef(string rule, string sMethod, RuleMethodScript method)
        {
            _rule = rule;
            _sMethod = sMethod;
            _method = method;
        }

        #endregion

        #region internal Methods

        internal void Serialize(StringBlob symbols, StreamMarshaler streamBuffer)
        {
            CfgScriptRef script = new();

            // Get the symbol id for the rule
            script._idRule = symbols.Find(_rule);

            script._method = _method;

            script._idMethod = _idSymbol;

            System.Diagnostics.Debug.Assert(script._idRule != -1 && script._idMethod != -1);

            streamBuffer.WriteStream(script);
        }

        internal static string OnInitMethod(ScriptRef[] scriptRefs, string rule)
        {
            if (scriptRefs != null)
            {
                foreach (ScriptRef script in scriptRefs)
                {
                    if (script._rule == rule && script._method == RuleMethodScript.onInit)
                    {
                        return script._sMethod;
                    }
                }
            }
            return null;
        }

        #endregion

        #region Internal Fields

        internal string _rule;

        internal string _sMethod;

        internal RuleMethodScript _method;

        internal int _idSymbol;

        #endregion
    }
}
