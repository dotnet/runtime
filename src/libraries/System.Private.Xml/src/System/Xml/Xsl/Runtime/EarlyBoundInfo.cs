// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// This class contains information about early bound function objects.
    /// </summary>
    internal sealed class EarlyBoundInfo
    {
        private readonly string _namespaceUri;            // Namespace Uri mapped to these early bound functions
        private readonly ConstructorInfo _constrInfo;     // Constructor for the early bound function object
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        private readonly Type _ebType;

        public EarlyBoundInfo(string namespaceUri, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type ebType)
        {
            Debug.Assert(namespaceUri != null && ebType != null);

            // Get the default constructor
            _namespaceUri = namespaceUri;
            _ebType = ebType;
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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public Type EarlyBoundType
        {
            get { return _ebType; }
        }

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
