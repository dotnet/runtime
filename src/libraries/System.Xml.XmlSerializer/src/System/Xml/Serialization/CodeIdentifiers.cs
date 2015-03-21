// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

namespace System.Xml.Serialization
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Globalization;
    using System.Xml.Extensions;
    // this[key] api throws KeyNotFoundException
    using Hashtable = System.Collections.InternalHashtable;


    /// <include file='doc\CodeIdentifiers.uex' path='docs/doc[@for="CodeIdentifiers"]/*' />
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    internal class CodeIdentifiers
    {
        private Hashtable _identifiers;
        private Hashtable _reservedIdentifiers;
        private ArrayList _list;

        public CodeIdentifiers()
        {
            _identifiers = new Hashtable();
            _reservedIdentifiers = new Hashtable();
            _list = new ArrayList();
        }




        /// <include file='doc\CodeIdentifiers.uex' path='docs/doc[@for="CodeIdentifiers.MakeUnique"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string MakeUnique(string identifier)
        {
            if (IsInUse(identifier))
            {
                for (int i = 1; ; i++)
                {
                    string newIdentifier = identifier + i.ToString(CultureInfo.InvariantCulture);
                    if (!IsInUse(newIdentifier))
                    {
                        identifier = newIdentifier;
                        break;
                    }
                }
            }
            // Check that we did not violate the identifier length after appending the suffix.
            if (identifier.Length > CodeIdentifier.MaxIdentifierLength)
            {
                return MakeUnique("Item");
            }
            return identifier;
        }



        /// <include file='doc\CodeIdentifiers.uex' path='docs/doc[@for="CodeIdentifiers.AddUnique"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string AddUnique(string identifier, object value)
        {
            identifier = MakeUnique(identifier);
            Add(identifier, value);
            return identifier;
        }

        /// <include file='doc\CodeIdentifiers.uex' path='docs/doc[@for="CodeIdentifiers.IsInUse"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsInUse(string identifier)
        {
            return _identifiers.Contains(identifier) || _reservedIdentifiers.Contains(identifier);
        }

        /// <include file='doc\CodeIdentifiers.uex' path='docs/doc[@for="CodeIdentifiers.Add"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Add(string identifier, object value)
        {
            _identifiers.Add(identifier, value);
            _list.Add(value);
        }
    }
}
