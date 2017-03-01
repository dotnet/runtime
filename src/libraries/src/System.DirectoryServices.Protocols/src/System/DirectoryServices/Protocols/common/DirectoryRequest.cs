// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Diagnostics;
    using System.Globalization;
    using System.ComponentModel;
    using System.Collections.Specialized;
    using System.Security.Permissions;

    public abstract class DirectoryRequest : DirectoryOperation
    {
        internal DirectoryControlCollection directoryControlCollection = null;

        internal DirectoryRequest()
        {
            directoryControlCollection = new DirectoryControlCollection();
        }

        public string RequestId
        {
            get
            {
                return directoryRequestID;
            }
            set
            {
                directoryRequestID = value;
            }
        }

        public DirectoryControlCollection Controls
        {
            get
            {
                return directoryControlCollection;
            }
        }
    }

    public class DeleteRequest : DirectoryRequest
    {
        //
        // Public
        //

        public DeleteRequest() { }

        public DeleteRequest(string distinguishedName)
        {
            _dn = distinguishedName;
        }

        // Member properties
        public string DistinguishedName
        {
            get
            {
                return _dn;
            }

            set
            {
                _dn = value;
            }
        }

        //
        // Private/protected
        //

        private string _dn;
    }

    public class AddRequest : DirectoryRequest
    {
        //
        // Public
        //

        public AddRequest()
        {
            _attributeList = new DirectoryAttributeCollection();
        }

        public AddRequest(string distinguishedName, params DirectoryAttribute[] attributes) : this()
        {
            // Store off the distinguished name
            _dn = distinguishedName;

            if (attributes != null)
            {
                for (int i = 0; i < attributes.Length; i++)
                {
                    _attributeList.Add(attributes[i]);
                }
            }
        }

        public AddRequest(string distinguishedName, string objectClass) : this()
        {
            // parameter validation
            if (objectClass == null)
                throw new ArgumentNullException("objectClass");

            // Store off the distinguished name
            _dn = distinguishedName;

            // Store off the objectClass in an object class attribute
            DirectoryAttribute objClassAttr = new DirectoryAttribute();

            objClassAttr.Name = "objectClass";
            objClassAttr.Add(objectClass);
            _attributeList.Add(objClassAttr);
        }

        // Properties
        public string DistinguishedName
        {
            get
            {
                return _dn;
            }

            set
            {
                _dn = value;
            }
        }

        public DirectoryAttributeCollection Attributes
        {
            get
            {
                return _attributeList;
            }
        }

        //
        // Private/protected
        //

        private string _dn;
        private DirectoryAttributeCollection _attributeList;
    }

    public class ModifyRequest : DirectoryRequest
    {
        //
        // Public
        //
        public ModifyRequest()
        {
            _attributeModificationList = new DirectoryAttributeModificationCollection();
        }

        public ModifyRequest(string distinguishedName, params DirectoryAttributeModification[] modifications) : this()
        {
            // Store off the distinguished name
            _dn = distinguishedName;

            // Store off the initial list of modifications
            _attributeModificationList.AddRange(modifications);
        }

        public ModifyRequest(string distinguishedName, DirectoryAttributeOperation operation, string attributeName, params object[] values) : this()
        {
            // Store off the distinguished name
            _dn = distinguishedName;

            // validate the attributeName
            if (attributeName == null)
                throw new ArgumentNullException("attributeName");

            DirectoryAttributeModification mod = new DirectoryAttributeModification();
            mod.Operation = operation;
            mod.Name = attributeName;
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                    mod.Add(values[i]);
            }

            _attributeModificationList.Add(mod);
        }

        // Properties
        public string DistinguishedName
        {
            get
            {
                return _dn;
            }

            set
            {
                _dn = value;
            }
        }

        public DirectoryAttributeModificationCollection Modifications
        {
            get
            {
                return _attributeModificationList;
            }
        }

        //
        // Private/protected
        //

        private string _dn;
        private DirectoryAttributeModificationCollection _attributeModificationList;

    }

    public class CompareRequest : DirectoryRequest
    {
        //
        // Public
        //
        public CompareRequest() { }

        public CompareRequest(string distinguishedName, string attributeName, string value)
        {
            CompareRequestHelper(distinguishedName, attributeName, value);
        }

        public CompareRequest(string distinguishedName, string attributeName, byte[] value)
        {
            CompareRequestHelper(distinguishedName, attributeName, value);
        }

        public CompareRequest(string distinguishedName, string attributeName, Uri value)
        {
            CompareRequestHelper(distinguishedName, attributeName, value);
        }

        public CompareRequest(string distinguishedName, DirectoryAttribute assertion)
        {
            if (assertion == null)
                throw new ArgumentNullException("assertion");

            if (assertion.Count != 1)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.WrongNumValuesCompare));

            CompareRequestHelper(distinguishedName, assertion.Name, assertion[0]);
        }

        private void CompareRequestHelper(string distinguishedName, string attributeName, object value)
        {
            // parameter validation
            if (attributeName == null)
                throw new ArgumentNullException("attributeName");

            if (value == null)
                throw new ArgumentNullException("value");

            // store off the DN
            _dn = distinguishedName;

            // store off the attribute name and value            
            _attribute.Name = attributeName;
            _attribute.Add(value);
        }

        // Properties
        public string DistinguishedName
        {
            get
            {
                return _dn;
            }

            set
            {
                _dn = value;
            }
        }

        public DirectoryAttribute Assertion
        {
            get
            {
                return _attribute;
            }
        }

        //
        // Private/protected
        //

        private string _dn;
        private DirectoryAttribute _attribute = new DirectoryAttribute();
    }

    public class ModifyDNRequest : DirectoryRequest
    {
        //
        // Public
        //
        public ModifyDNRequest() { }

        public ModifyDNRequest(string distinguishedName,
                                string newParentDistinguishedName,
                                string newName)
        {
            // store off the DN
            _dn = distinguishedName;

            _newSuperior = newParentDistinguishedName;
            _newRDN = newName;
        }

        // Properties
        public string DistinguishedName
        {
            get
            {
                return _dn;
            }

            set
            {
                _dn = value;
            }
        }

        public string NewParentDistinguishedName
        {
            get
            {
                return _newSuperior;
            }

            set
            {
                _newSuperior = value;
            }
        }

        public string NewName
        {
            get
            {
                return _newRDN;
            }

            set
            {
                _newRDN = value;
            }
        }

        public bool DeleteOldRdn
        {
            get
            {
                return _deleteOldRDN;
            }

            set
            {
                _deleteOldRDN = value;
            }
        }

        //
        // Private/protected
        //

        private string _dn;
        private string _newSuperior;
        private string _newRDN;
        private bool _deleteOldRDN = true;
    }

    /// <summary>
    /// The representation of a <extendedRequest>
    /// </summary>
    public class ExtendedRequest : DirectoryRequest
    {
        //
        // Public
        //
        public ExtendedRequest() { }

        public ExtendedRequest(string requestName)
        {
            _requestName = requestName;
        }

        public ExtendedRequest(string requestName, byte[] requestValue) : this(requestName)
        {
            _requestValue = requestValue;
        }

        // Properties
        public string RequestName
        {
            get
            {
                return _requestName;
            }

            set
            {
                _requestName = value;
            }
        }

        public byte[] RequestValue
        {
            get
            {
                if (_requestValue == null)
                    return new byte[0];
                else
                {
                    byte[] tempValue = new byte[_requestValue.Length];
                    for (int i = 0; i < _requestValue.Length; i++)
                        tempValue[i] = _requestValue[i];

                    return tempValue;
                }
            }

            set
            {
                _requestValue = value;
            }
        }

        //
        // Private/protected
        //

        private string _requestName;
        private byte[] _requestValue = null;
    }

    public class SearchRequest : DirectoryRequest
    {
        //
        // Public
        //
        public SearchRequest()
        {
            _directoryAttributes = new StringCollection();
        }

        public SearchRequest(string distinguishedName,
                             string ldapFilter,
                             SearchScope searchScope,
                             params string[] attributeList) : this()
        {
            _dn = distinguishedName;

            if (attributeList != null)
            {
                for (int i = 0; i < attributeList.Length; i++)
                    _directoryAttributes.Add(attributeList[i]);
            }

            Scope = searchScope;

            Filter = ldapFilter;
        }

        // Properties
        public string DistinguishedName
        {
            get
            {
                return _dn;
            }

            set
            {
                _dn = value;
            }
        }

        public StringCollection Attributes
        {
            get
            {
                return _directoryAttributes;
            }
        }

        public object Filter
        {
            get
            {
                return _directoryFilter;
            }

            set
            {
                // do we need to validate the filter here?
                if ((value is string) || (value == null))
                    _directoryFilter = value;
                else
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.ValidFilterType), "value");
            }
        }

        public SearchScope Scope
        {
            get
            {
                return _directoryScope;
            }

            set
            {
                if (value < SearchScope.Base || value > SearchScope.Subtree)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(SearchScope));

                _directoryScope = value;
            }
        }

        public DereferenceAlias Aliases
        {
            get
            {
                return _directoryRefAlias;
            }

            set
            {
                if (value < DereferenceAlias.Never || value > DereferenceAlias.Always)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(DereferenceAlias));

                _directoryRefAlias = value;
            }
        }

        public int SizeLimit
        {
            get
            {
                return _directorySizeLimit;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NoNegativeSizeLimit), "value");
                }

                _directorySizeLimit = value;
            }
        }

        public TimeSpan TimeLimit
        {
            get
            {
                return _directoryTimeLimit;
            }

            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NoNegativeTime), "value");
                }

                // prevent integer overflow
                if (value.TotalSeconds > Int32.MaxValue)
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.TimespanExceedMax), "value");

                _directoryTimeLimit = value;
            }
        }

        public bool TypesOnly
        {
            get
            {
                return _directoryTypesOnly;
            }

            set
            {
                _directoryTypesOnly = value;
            }
        }

        //
        // Private/protected
        //

        private string _dn = null;
        private StringCollection _directoryAttributes = new StringCollection();
        private object _directoryFilter = null;
        private SearchScope _directoryScope = SearchScope.Subtree;
        private DereferenceAlias _directoryRefAlias = DereferenceAlias.Never;
        private int _directorySizeLimit = 0;
        private TimeSpan _directoryTimeLimit = new TimeSpan(0);
        private bool _directoryTypesOnly = false;
    }
}

namespace System.DirectoryServices.Protocols
{
    using System;

    public class DsmlAuthRequest : DirectoryRequest
    {
        private string _directoryPrincipal = "";

        public DsmlAuthRequest() { }

        public DsmlAuthRequest(string principal)
        {
            _directoryPrincipal = principal;
        }

        public string Principal
        {
            get
            {
                return _directoryPrincipal;
            }
            set
            {
                _directoryPrincipal = value;
            }
        }
    }
}
