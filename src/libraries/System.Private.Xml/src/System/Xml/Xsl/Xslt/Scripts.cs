// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <spec>http://devdiv/Documents/Whidbey/CLR/CurrentSpecs/BCL/CodeDom%20Activation.doc</spec>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.Xslt
{
    internal class Scripts
    {
        private readonly Compiler _compiler;
        private readonly Dictionary<string, Type?> _nsToType = new Dictionary<string, Type?>();
        private readonly XmlExtensionFunctionTable _extFuncs = new XmlExtensionFunctionTable();

        public Scripts(Compiler compiler)
        {
            _compiler = compiler;
        }

        public Dictionary<string, Type?> ScriptClasses
        {
            get { return _nsToType; }
        }

        public XmlExtensionFunction? ResolveFunction(string name, string ns, int numArgs, IErrorHelper errorHelper)
        {
            Type? type;
            if (_nsToType.TryGetValue(ns, out type))
            {
                try
                {
                    return _extFuncs.Bind(name, ns, numArgs, type, XmlQueryRuntime.EarlyBoundFlags);
                }
                catch (XslTransformException e)
                {
                    errorHelper.ReportError(e.Message);
                }
            }
            return null;
        }
    }
}
