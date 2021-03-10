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
    internal class Scripts
    {
        private readonly Compiler _compiler;
        private readonly LinkerSafeDictionary _nsToType = new LinkerSafeDictionary();
        private readonly XmlExtensionFunctionTable _extFuncs = new XmlExtensionFunctionTable();

        public Scripts(Compiler compiler)
        {
            _compiler = compiler;
        }

        public LinkerSafeDictionary ScriptClasses
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

        internal class LinkerSafeDictionary : IDictionary<string, Type?>
        {
            private readonly Dictionary<string, Type?> _backingDictionary = new Dictionary<string, Type?>();

            public Type? this[string key]
            {
                [UnconditionalSuppressMessage("TrimAnalysis", "IL2093:MissingAttributeOnBaseClass", Justification = "This implementation of IDictionary must have extra annotation attributes in order to be trim safe")]
                [UnconditionalSuppressMessage("TrimAnalysis", "IL2073:MissingDynamicallyAccessedMembers",
                    Justification = "The getter of the dictionary is not annotated to preserve the constructor, but the sources that are adding the items to " +
                    "the dictionary are annotated so we can supress the message as we know the constructor will be preserved.")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                get => ((IDictionary<string, Type?>)_backingDictionary)[key];
                [UnconditionalSuppressMessage("TrimAnalysis", "IL2092:MissingAttributeOnBaseClass", Justification = "This implementation of IDictionary must have extra annotation attributes in order to be trim safe")]
                [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
                set => ((IDictionary<string, Type?>)_backingDictionary)[key] = value;
            }

            public ICollection<string> Keys => ((IDictionary<string, Type?>)_backingDictionary).Keys;

            public ICollection<Type?> Values => ((IDictionary<string, Type?>)_backingDictionary).Values;

            public int Count => ((ICollection<KeyValuePair<string, Type?>>)_backingDictionary).Count;

            public bool IsReadOnly => ((ICollection<KeyValuePair<string, Type?>>)_backingDictionary).IsReadOnly;

            [UnconditionalSuppressMessage("TrimAnalysis", "IL2092:MissingAttributeOnBaseClass", Justification = "This implementation of IDictionary must have extra annotation attributes in order to be trim safe")]
            public void Add(string key, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type? value) => ((IDictionary<string, Type?>)_backingDictionary).Add(key, value);
            public void Add(KeyValuePair<string, Type?> item) => ((ICollection<KeyValuePair<string, Type?>>)_backingDictionary).Add(item);
            public void Clear() => ((ICollection<KeyValuePair<string, Type?>>)_backingDictionary).Clear();
            public bool Contains(KeyValuePair<string, Type?> item) => ((ICollection<KeyValuePair<string, Type?>>)_backingDictionary).Contains(item);
            public bool ContainsKey(string key) => ((IDictionary<string, Type?>)_backingDictionary).ContainsKey(key);
            public void CopyTo(KeyValuePair<string, Type?>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, Type?>>)_backingDictionary).CopyTo(array, arrayIndex);
            public IEnumerator<KeyValuePair<string, Type?>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, Type?>>)_backingDictionary).GetEnumerator();
            public bool Remove(string key) => ((IDictionary<string, Type?>)_backingDictionary).Remove(key);
            public bool Remove(KeyValuePair<string, Type?> item) => ((ICollection<KeyValuePair<string, Type?>>)_backingDictionary).Remove(item);
            public bool TryGetValue(string key, [MaybeNullWhen(false)] out Type? value) => ((IDictionary<string, Type?>)_backingDictionary).TryGetValue(key, out value);
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_backingDictionary).GetEnumerator();
        }
    }
}
