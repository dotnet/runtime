// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Speech.Internal.SrgsParser;
using System.Text;

#pragma warning disable 56507 // check for null or empty strings

namespace System.Speech.Internal.SrgsCompiler
{
    internal class CustomGrammar
    {
        #region Constructors

        internal CustomGrammar()
        {
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Add the scripts defined in 'cg' to the set of scripts defined in 'cgCombined'.
        /// Build the union of t codebehind files and assembly references
        /// </summary>
        internal void Combine(CustomGrammar cg, string innerCode)
        {
            if (_rules.Count == 0)
            {
                _language = cg._language;
            }
            else
            {
                if (_language != cg._language)
                {
                    XmlParser.ThrowSrgsException(SRID.IncompatibleLanguageProperties);
                }
            }

            if (_namespace == null)
            {
                _namespace = cg._namespace;
            }
            else
            {
                if (_namespace != cg._namespace)
                {
                    XmlParser.ThrowSrgsException(SRID.IncompatibleNamespaceProperties);
                }
            }

            _fDebugScript |= cg._fDebugScript;

            foreach (string codebehind in cg._codebehind)
            {
                if (!_codebehind.Contains(codebehind))
                {
                    _codebehind.Add(codebehind);
                }
            }

            foreach (string assemblyReferences in cg._assemblyReferences)
            {
                if (!_assemblyReferences.Contains(assemblyReferences))
                {
                    _assemblyReferences.Add(assemblyReferences);
                }
            }

            foreach (string importNamespaces in cg._importNamespaces)
            {
                if (!_importNamespaces.Contains(importNamespaces))
                {
                    _importNamespaces.Add(importNamespaces);
                }
            }

            _keyFile = cg._keyFile;

            _types.AddRange(cg._types);
            foreach (Rule rule in cg._rules)
            {
                if (_types.Contains(rule.Name))
                {
                    XmlParser.ThrowSrgsException(SRID.RuleDefinedMultipleTimes2, rule.Name);
                }
            }

            // Combine all the scripts
            _script.Append(innerCode);
        }

        #endregion

        #region Internal Properties

        internal bool HasScript
        {
            get
            {
                bool has_script = _script.Length > 0 || _codebehind.Count > 0;
                if (!has_script)
                {
                    foreach (Rule rule in _rules)
                    {
                        if (rule.Script.Length > 0)
                        {
                            has_script = true;
                            break;
                        }
                    }
                }
                return has_script;
            }
        }

        #endregion

        #region Internal Types

        internal class CfgResource
        {
            internal string name;
            internal byte[] data;
        }

        #endregion

        #region Internal Fields

        // 'C#', 'VB' or 'JScript'
        internal string _language = "C#";

        // namespace for the class wrapping the inline code
        internal string _namespace;

        // namespace for the class wrapping the inline code
        internal List<Rule> _rules = new();

        // code behind dll
        internal Collection<string> _codebehind = new();

        // if set generates #line statements
        internal bool _fDebugScript;

        // List of assembly references to import
        internal Collection<string> _assemblyReferences = new();

        // List of namespaces to import
        internal Collection<string> _importNamespaces = new();

        // Key file for the strong name
        internal string _keyFile;

        // CFG scripts definition
        internal Collection<ScriptRef> _scriptRefs = new();

        // inline script
        internal List<string> _types = new();

        // inline script
        internal StringBuilder _script = new();

        #endregion
    }
}
