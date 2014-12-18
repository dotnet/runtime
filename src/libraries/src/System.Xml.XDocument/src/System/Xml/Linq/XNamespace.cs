// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Debug = System.Diagnostics.Debug;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
using Interlocked = System.Threading.Interlocked;

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents an XML namespace. This class cannot be inherited.
    /// </summary>
    public sealed class XNamespace
    {
        internal const string xmlPrefixNamespace = "http://www.w3.org/XML/1998/namespace";
        internal const string xmlnsPrefixNamespace = "http://www.w3.org/2000/xmlns/";

        static XHashtable<WeakReference> namespaces;
        static WeakReference refNone;
        static WeakReference refXml;
        static WeakReference refXmlns;

        string namespaceName;
        int hashCode;
        XHashtable<XName> names;

        const int NamesCapacity = 8;           // Starting capacity of XName table, which must be power of 2
        const int NamespacesCapacity = 32;     // Starting capacity of XNamespace table, which must be power of 2

        /// <summary>
        /// Constructor, internal so that external users must go through the Get() method to create an XNamespace.
        /// </summary>
        internal XNamespace(string namespaceName)
        {
            this.namespaceName = namespaceName;
            this.hashCode = namespaceName.GetHashCode();
            names = new XHashtable<XName>(ExtractLocalName, NamesCapacity);
        }

        /// <summary>
        /// Gets the namespace name of the namespace.
        /// </summary>
        public string NamespaceName
        {
            get { return namespaceName; }
        }

        /// <summary>
        /// Returns an <see cref="XName"/> object created from the current instance and the specified local name.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="XName"/> object is guaranteed to be atomic (i.e. the only one in the system for this
        /// particular expanded name).
        /// </remarks>
        public XName GetName(string localName)
        {
            if (localName == null) throw new ArgumentNullException("localName");
            return GetName(localName, 0, localName.Length);
        }

        /// <summary>
        /// Returns the namespace name of this <see cref="XNamespace"/>.
        /// </summary>
        /// <returns>A string value containing the namespace name.</returns>
        public override string ToString()
        {
            return namespaceName;
        }

        /// <summary>
        /// Gets the <see cref="XNamespace"/> object that corresponds to no namespace.
        /// </summary>
        /// <remarks>
        /// If an element or attribute is in no namespace, its namespace
        /// will be set to the namespace returned by this property.
        /// </remarks>
        public static XNamespace None
        {
            get
            {
                return EnsureNamespace(ref refNone, string.Empty);
            }
        }

        /// <summary>
        /// Gets the <see cref="XNamespace"/> object that corresponds to the xml uri (http://www.w3.org/XML/1998/namespace).
        /// </summary>
        public static XNamespace Xml
        {
            get
            {
                return EnsureNamespace(ref refXml, xmlPrefixNamespace);
            }
        }

        /// <summary>
        /// Gets the <see cref="XNamespace"/> object that corresponds to the xmlns uri (http://www.w3.org/2000/xmlns/).
        /// </summary>
        public static XNamespace Xmlns
        {
            get
            {
                return EnsureNamespace(ref refXmlns, xmlnsPrefixNamespace);
            }
        }

        /// <summary>
        /// Gets an <see cref="XNamespace"/> created from the specified namespace name.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="XNamespace"/> object is guaranteed to be atomic
        /// (i.e. the only one in the system for that particular namespace name).
        /// </remarks>
        public static XNamespace Get(string namespaceName)
        {
            if (namespaceName == null) throw new ArgumentNullException("namespaceName");
            return Get(namespaceName, 0, namespaceName.Length);
        }

        /// <summary>
        /// Converts a string containing a namespace name to an <see cref="XNamespace"/>.
        /// </summary>
        /// <param name="namespaceName">A string containing the namespace name.</param>
        /// <returns>An <see cref="XNamespace"/> constructed from the namespace name string.</returns>
        [CLSCompliant(false)]
        public static implicit operator XNamespace(string namespaceName)
        {
            return namespaceName != null ? Get(namespaceName) : null;
        }

        /// <summary>
        /// Combines an <see cref="XNamespace"/> object with a local name to create an <see cref="XName"/>.
        /// </summary>
        /// <param name="ns">The namespace for the expanded name.</param>
        /// <param name="localName">The local name for the expanded name.</param>
        /// <returns>The new XName constructed from the namespace and local name.</returns>        
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Functionality available via XNamespace.Get().")]
        public static XName operator +(XNamespace ns, string localName)
        {
            if (ns == null) throw new ArgumentNullException("ns");
            return ns.GetName(localName);
        }

        /// <summary>
        /// Determines whether the specified <see cref="XNamespace"/> is equal to the current <see cref="XNamespace"/>.
        /// </summary>
        /// <param name="obj">The <see cref="XNamespace"/> to compare to the current <see cref="XNamespace"/>.</param>
        /// <returns>
        /// true if the specified <see cref="XNamespace"/> is equal to the current <see cref="XNamespace"/>; otherwise false.
        /// </returns>
        /// <remarks>
        /// For two <see cref="XNamespace"/> objects to be equal they must have the same 
        /// namespace name.
        /// </remarks>
        public override bool Equals(object obj)
        {
            return (object)this == obj;
        }

        /// <summary>
        /// Serves as a hash function for <see cref="XNamespace"/>. GetHashCode is suitable 
        /// for use in hashing algorithms and data structures like a hash table.  
        /// </summary>
        public override int GetHashCode()
        {
            return hashCode;
        }


        // The overloads of == and != are included to enable comparisons between
        // XNamespace and string (e.g. element.Name.Namespace == "foo"). C#'s
        // predefined reference equality operators require one operand to be
        // convertible to the type of the other through reference conversions only
        // and do not consider the implicit conversion from string to XNamespace.

        /// <summary>
        /// Returns a value indicating whether two instances of <see cref="XNamespace"/> are equal.
        /// </summary>
        /// <param name="left">The first <see cref="XNamespace"/> to compare.</param>
        /// <param name="right">The second <see cref="XNamespace"/> to compare.</param>
        /// <returns>true if left and right are equal; otherwise false.</returns>
        /// <remarks>
        /// This overload is included to enable the comparison between
        /// an instance of <see cref="XNamespace"/> and string.
        /// </remarks>
        public static bool operator ==(XNamespace left, XNamespace right)
        {
            return (object)left == (object)right;
        }

        /// <summary>
        /// Returns a value indicating whether two instances of <see cref="XNamespace"/> are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="XNamespace"/> to compare.</param>
        /// <param name="right">The second <see cref="XNamespace"/> to compare.</param>
        /// <returns>true if left and right are not equal; otherwise false.</returns>
        /// <remarks>
        /// This overload is included to enable the comparison between
        /// an instance of <see cref="XNamespace"/> and string.
        /// </remarks>
        public static bool operator !=(XNamespace left, XNamespace right)
        {
            return (object)left != (object)right;
        }

        /// <summary>
        /// Returns an <see cref="XName"/> created from this XNamespace <see cref="XName"/> and a portion of the passed in
        /// local name parameter.  The returned <see cref="XName"/> object is guaranteed to be atomic (i.e. the only one in the system for
        /// this particular expanded name).
        /// </summary>
        internal XName GetName(string localName, int index, int count)
        {
            Debug.Assert(index >= 0 && index <= localName.Length, "Caller should have checked that index was in bounds");
            Debug.Assert(count >= 0 && index + count <= localName.Length, "Caller should have checked that count was in bounds");

            // Attempt to get the local name from the hash table
            XName name;
            if (names.TryGetValue(localName, index, count, out name))
                return name;

            // No local name has yet been added, so add it now
            return names.Add(new XName(this, localName.Substring(index, count)));
        }

        /// <summary>
        /// Returns an <see cref="XNamespace"/> created from a portion of the passed in namespace name parameter.  The returned <see cref="XNamespace"/>
        /// object is guaranteed to be atomic (i.e. the only one in the system for this particular namespace name).
        /// </summary>
        internal static XNamespace Get(string namespaceName, int index, int count)
        {
            Debug.Assert(index >= 0 && index <= namespaceName.Length, "Caller should have checked that index was in bounds");
            Debug.Assert(count >= 0 && index + count <= namespaceName.Length, "Caller should have checked that count was in bounds");

            if (count == 0) return None;

            // Use CompareExchange to ensure that exactly one XHashtable<WeakReference> is used to store namespaces
            if (namespaces == null)
                Interlocked.CompareExchange(ref namespaces, new XHashtable<WeakReference>(ExtractNamespace, NamespacesCapacity), null);

            WeakReference refNamespace;
            XNamespace ns;

            // Keep looping until a non-null namespace has been retrieved
            do
            {
                // Attempt to get the WeakReference for the namespace from the hash table
                if (!namespaces.TryGetValue(namespaceName, index, count, out refNamespace))
                {
                    // If it is not there, first determine whether it's a special namespace
                    if (count == xmlPrefixNamespace.Length && string.CompareOrdinal(namespaceName, index, xmlPrefixNamespace, 0, count) == 0) return Xml;
                    if (count == xmlnsPrefixNamespace.Length && string.CompareOrdinal(namespaceName, index, xmlnsPrefixNamespace, 0, count) == 0) return Xmlns;

                    // Go ahead and create the namespace and add it to the table
                    refNamespace = namespaces.Add(new WeakReference(new XNamespace(namespaceName.Substring(index, count))));
                }

                ns = (refNamespace != null) ? (XNamespace)refNamespace.Target : null;
            }
            while (ns == null);

            return ns;
        }

        /// <summary>
        /// This function is used by the <see cref="XHashtable{XName}"/> to extract the local name part from an <see cref="XName"/>.  The hash table
        /// uses the local name as the hash key.
        /// </summary>
        private static string ExtractLocalName(XName n)
        {
            Debug.Assert(n != null, "Null name should never exist here");
            return n.LocalName;
        }

        /// <summary>
        /// This function is used by the <see cref="XHashtable{WeakReference}"/> to extract the XNamespace that the WeakReference is
        /// referencing.  In cases where the XNamespace has been cleaned up, this function returns null.
        /// </summary>
        private static string ExtractNamespace(WeakReference r)
        {
            XNamespace ns;

            if (r == null || (ns = (XNamespace)r.Target) == null)
                return null;

            return ns.NamespaceName;
        }

        /// <summary>
        /// Ensure that an XNamespace object for 'namespaceName' has been atomically created.  In other words, all outstanding
        /// references to this particular namespace, on any thread, must all be to the same object.  Care must be taken,
        /// since other threads can be concurrently calling this method, and the target of a WeakReference can be cleaned up
        /// at any time by the GC.
        /// </summary>
        private static XNamespace EnsureNamespace(ref WeakReference refNmsp, string namespaceName)
        {
            WeakReference refOld;

            // Keep looping until a non-null namespace has been retrieved
            while (true)
            {
                // Save refNmsp in local variable, so we can work on a value that will not be changed by another thread
                refOld = refNmsp;

                if (refOld != null)
                {
                    // If the target of the WeakReference is non-null, then we're done--just return the value
                    XNamespace ns = (XNamespace)refOld.Target;
                    if (ns != null) return ns;
                }

                // Either refNmsp is null, or its target is null, so update it
                // Make sure to do this atomically, so that we can guarantee atomicity of XNamespace objects
                Interlocked.CompareExchange(ref refNmsp, new WeakReference(new XNamespace(namespaceName)), refOld);
            }
        }
    }
}
