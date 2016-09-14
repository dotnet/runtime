// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Xml
{
    // Provides read-only access to a set of (prefix, namespace) mappings.  Each distinct prefix is mapped to exactly
    // one namespace, but multiple prefixes may be mapped to the same namespace (e.g. xmlns:foo="ns" xmlns:bar="ns").
    public interface IXmlNamespaceResolver
    {
        // This pragma disables a warning that the return type is not CLS-compliant, but generics are part of CLS in Whidbey. 
#pragma warning disable 3002
        // Returns a collection of defined prefix-namespace mappings.
        IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope);
#pragma warning restore 3002

        // Return the namespace to which the specified prefix is mapped.  Returns null if the prefix isn't mapped to
        // a namespace.  
        // The "xml" prefix is always mapped to the "http://www.w3.org/XML/1998/namespace" namespace.
        // The "xmlns" prefix is always mapped to the "http://www.w3.org/2000/xmlns/" namespace.
        // If the default namespace has not been defined, then the "" prefix is mapped to "" (the empty namespace).
        string LookupNamespace(string prefix);

        // Return a prefix which is mapped to the specified namespace.  Multiple prefixes can be mapped to the
        // same namespace, and it is undefined which prefix will be returned.  Returns null if no prefixes are
        // mapped to the namespace.  
        // The "xml" prefix is always mapped to the "http://www.w3.org/XML/1998/namespace" namespace.
        // The "xmlns" prefix is always mapped to the "http://www.w3.org/2000/xmlns/" namespace.
        // If the default namespace has not been defined, then the "" prefix is mapped to "" (the empty namespace).
        string LookupPrefix(string namespaceName);
    }
}
