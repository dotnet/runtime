// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;
    using System.Diagnostics;

    public class AttributeMetadata
    {
        private string _pszAttributeName = null;
        private int _dwVersion;
        private DateTime _ftimeLastOriginatingChange;
        private Guid _uuidLastOriginatingDsaInvocationID;
        private long _usnOriginatingChange;
        private long _usnLocalChange;
        private string _pszLastOriginatingDsaDN = null;

        private string _originatingServerName = null;
        private DirectoryServer _server = null;
        private Hashtable _nameTable = null;
        private bool _advanced = false;

        internal AttributeMetadata(IntPtr info, bool advanced, DirectoryServer server, Hashtable table)
        {
            if (advanced)
            {
                DS_REPL_ATTR_META_DATA_2 attrMetaData = new DS_REPL_ATTR_META_DATA_2();
                Marshal.PtrToStructure(info, attrMetaData);
                Debug.Assert(attrMetaData != null);

                _pszAttributeName = Marshal.PtrToStringUni(attrMetaData.pszAttributeName);
                _dwVersion = attrMetaData.dwVersion;
                long ftimeChangeValue = (long)((uint)attrMetaData.ftimeLastOriginatingChange1 + (((long)attrMetaData.ftimeLastOriginatingChange2) << 32));
                _ftimeLastOriginatingChange = DateTime.FromFileTime(ftimeChangeValue);
                _uuidLastOriginatingDsaInvocationID = attrMetaData.uuidLastOriginatingDsaInvocationID;
                _usnOriginatingChange = attrMetaData.usnOriginatingChange;
                _usnLocalChange = attrMetaData.usnLocalChange;
                _pszLastOriginatingDsaDN = Marshal.PtrToStringUni(attrMetaData.pszLastOriginatingDsaDN);
            }
            else
            {
                DS_REPL_ATTR_META_DATA attrMetaData = new DS_REPL_ATTR_META_DATA();
                Marshal.PtrToStructure(info, attrMetaData);
                Debug.Assert(attrMetaData != null);

                _pszAttributeName = Marshal.PtrToStringUni(attrMetaData.pszAttributeName);
                _dwVersion = attrMetaData.dwVersion;
                long ftimeChangeValue = (long)((uint)attrMetaData.ftimeLastOriginatingChange1 + (((long)attrMetaData.ftimeLastOriginatingChange2) << 32));
                _ftimeLastOriginatingChange = DateTime.FromFileTime(ftimeChangeValue);
                _uuidLastOriginatingDsaInvocationID = attrMetaData.uuidLastOriginatingDsaInvocationID;
                _usnOriginatingChange = attrMetaData.usnOriginatingChange;
                _usnLocalChange = attrMetaData.usnLocalChange;
            }
            _server = server;
            _nameTable = table;
            _advanced = advanced;
        }

        public string Name
        {
            get
            {
                return _pszAttributeName;
            }
        }

        public int Version
        {
            get
            {
                return _dwVersion;
            }
        }

        public DateTime LastOriginatingChangeTime
        {
            get
            {
                return _ftimeLastOriginatingChange;
            }
        }

        public Guid LastOriginatingInvocationId
        {
            get
            {
                return _uuidLastOriginatingDsaInvocationID;
            }
        }

        public long OriginatingChangeUsn
        {
            get
            {
                return _usnOriginatingChange;
            }
        }

        public long LocalChangeUsn
        {
            get
            {
                return _usnLocalChange;
            }
        }

        public string OriginatingServer
        {
            get
            {
                if (_originatingServerName == null)
                {
                    // check whether we have got it before
                    if (_nameTable.Contains(LastOriginatingInvocationId))
                    {
                        _originatingServerName = (string)_nameTable[LastOriginatingInvocationId];
                    }
                    // do the translation for downlevel platform or kcc is able to do the name translation
                    else if (!_advanced || (_advanced && _pszLastOriginatingDsaDN != null))
                    {
                        _originatingServerName = Utils.GetServerNameFromInvocationID(_pszLastOriginatingDsaDN, LastOriginatingInvocationId, _server);

                        // add it to the hashtable
                        _nameTable.Add(LastOriginatingInvocationId, _originatingServerName);
                    }
                }

                return _originatingServerName;
            }
        }
    }
}
