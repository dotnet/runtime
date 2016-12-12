// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System.Globalization;

    public class ActiveDirectoryReplicationMetadata : DictionaryBase
    {
        private DirectoryServer _server = null;
        private Hashtable _nameTable = null;
        private AttributeMetadataCollection _dataValueCollection = new AttributeMetadataCollection();
        private ReadOnlyStringCollection _dataNameCollection = new ReadOnlyStringCollection();

        internal ActiveDirectoryReplicationMetadata(DirectoryServer server)
        {
            _server = server;
            Hashtable tempNameTable = new Hashtable();
            _nameTable = Hashtable.Synchronized(tempNameTable);
        }

        public AttributeMetadata this[string name]
        {
            get
            {
                string tempName = name.ToLower(CultureInfo.InvariantCulture);
                if (Contains(tempName))
                {
                    return (AttributeMetadata)InnerHashtable[tempName];
                }
                else
                    return null;
            }
        }

        public ReadOnlyStringCollection AttributeNames
        {
            get
            {
                return _dataNameCollection;
            }
        }

        public AttributeMetadataCollection Values
        {
            get
            {
                return _dataValueCollection;
            }
        }

        public bool Contains(string attributeName)
        {
            string tempName = attributeName.ToLower(CultureInfo.InvariantCulture);
            return Dictionary.Contains(tempName);
        }

        /// <include file='doc\ResultPropertyCollection.uex' path='docs/doc[@for="ResultPropertyCollection.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(AttributeMetadata[] array, int index)
        {
            Dictionary.Values.CopyTo((Array)array, index);
        }

        private void Add(string name, AttributeMetadata value)
        {
            Dictionary.Add(name.ToLower(CultureInfo.InvariantCulture), value);

            _dataNameCollection.Add(name);
            _dataValueCollection.Add(value);
        }

        internal void AddHelper(int count, IntPtr info, bool advanced)
        {
            IntPtr addr = (IntPtr)0;

            for (int i = 0; i < count; i++)
            {
                if (advanced)
                {
                    addr = IntPtr.Add(info, Marshal.SizeOf(typeof(int)) * 2 + i * Marshal.SizeOf(typeof(DS_REPL_ATTR_META_DATA_2)));

                    AttributeMetadata managedMetaData = new AttributeMetadata(addr, true, _server, _nameTable);
                    Add(managedMetaData.Name, managedMetaData);
                }
                else
                {
                    addr = IntPtr.Add(info, Marshal.SizeOf(typeof(int)) * 2 + i * Marshal.SizeOf(typeof(DS_REPL_ATTR_META_DATA)));

                    AttributeMetadata managedMetaData = new AttributeMetadata(addr, false, _server, _nameTable);
                    Add(managedMetaData.Name, managedMetaData);
                }
            }
        }
    }
}
