// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <spec>http://devdiv/Documents/Whidbey/CLR/CurrentSpecs/BCL/CodeDom%20Activation.doc</spec>
//------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.Xslt
{
    internal sealed class Scripts
    {
        private readonly Compiler _compiler;
        private readonly TrimSafeDictionary _nsToType = new TrimSafeDictionary();
        private readonly XmlExtensionFunctionTable _extFuncs = new XmlExtensionFunctionTable();
        internal const string ExtensionFunctionCannotBeStaticallyAnalyzed = "The extension function referenced will be called from the stylesheet which cannot be statically analyzed.";

        public Scripts(Compiler compiler)
        {
            _compiler = compiler;
        }

        public TrimSafeDictionary ScriptClasses
        {
            get { return _nsToType; }
        }

        [RequiresUnreferencedCode(ExtensionFunctionCannotBeStaticallyAnalyzed)]
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

        internal sealed class TrimSafeDictionary
        {
            private readonly Dictionary<string, Type?> _backingDictionary = new Dictionary<string, Type?>();

            public Type? this[string key]
            {
                [UnconditionalSuppressMessage("TrimAnalysis", "IL2073:MissingDynamicallyAccessedMembers",
                    Justification = "The getter of the dictionary is not annotated to preserve the constructor, but the sources that are adding the items to " +
                    "the dictionary are annotated so we can suppress the message as we know the constructor will be preserved.")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                get => _backingDictionary[key];
                [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                set => _backingDictionary[key] = value;
            }

            public ICollection<string> Keys => _backingDictionary.Keys;

            public int Count => _backingDictionary.Count;

            public bool ContainsKey(string key) => _backingDictionary.ContainsKey(key);

            public bool TryGetValue(string key, [MaybeNullWhen(false)] out Type value) => _backingDictionary.TryGetValue(key, out value);
        }
    }
}
