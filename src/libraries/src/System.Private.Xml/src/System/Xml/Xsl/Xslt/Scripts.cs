// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <spec>http://devdiv/Documents/Whidbey/CLR/CurrentSpecs/BCL/CodeDom%20Activation.doc</spec>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
#if FEATURE_COMPILED_XSL
using System.Xml.Xsl.IlGen;
#endif
using System.Xml.Xsl.Runtime;
using System.Runtime.Versioning;

namespace System.Xml.Xsl.Xslt
{
    internal class Scripts
    {
        private const string ScriptClassesNamespace = "System.Xml.Xsl.CompiledQuery";

        private Compiler _compiler;
        private Dictionary<string, Type> _nsToType = new Dictionary<string, Type>();
        private XmlExtensionFunctionTable _extFuncs = new XmlExtensionFunctionTable();

        public Scripts(Compiler compiler)
        {
            _compiler = compiler;
        }

        public Dictionary<string, Type> ScriptClasses
        {
            get { return _nsToType; }
        }

        public XmlExtensionFunction ResolveFunction(string name, string ns, int numArgs, IErrorHelper errorHelper)
        {
            Type type;
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
