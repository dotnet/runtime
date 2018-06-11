// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// This class contains information about early bound function objects.
    /// </summary>
    internal sealed class EarlyBoundInfo
    {
        private string _namespaceUri;            // Namespace Uri mapped to these early bound functions
        private ConstructorInfo _constrInfo;     // Constructor for the early bound function object

        public EarlyBoundInfo(string namespaceUri, Type ebType)
        {
            Debug.Assert(namespaceUri != null && ebType != null);

            // Get the default constructor
            _namespaceUri = namespaceUri;
            _constrInfo = ebType.GetConstructor(Type.EmptyTypes);
            Debug.Assert(_constrInfo != null, "The early bound object type " + ebType.FullName + " must have a public default constructor");
        }

        /// <summary>
        /// Get the Namespace Uri mapped to these early bound functions.
        /// </summary>
        public string NamespaceUri { get { return _namespaceUri; } }

        /// <summary>
        /// Return the Clr Type of the early bound object.
        /// </summary>
        public Type EarlyBoundType { get { return _constrInfo.DeclaringType; } }

        /// <summary>
        /// Create an instance of the early bound object.
        /// </summary>
        public object CreateObject() { return _constrInfo.Invoke(Array.Empty<object>()); }

        /// <summary>
        /// Override Equals method so that EarlyBoundInfo to implement value comparison.
        /// </summary>
        public override bool Equals(object obj)
        {
            EarlyBoundInfo info = obj as EarlyBoundInfo;
            if (info == null)
                return false;

            return _namespaceUri == info._namespaceUri && _constrInfo == info._constrInfo;
        }

        /// <summary>
        /// Override GetHashCode since Equals is overridden.
        /// </summary>
        public override int GetHashCode()
        {
            return _namespaceUri.GetHashCode();
        }
    }
}

