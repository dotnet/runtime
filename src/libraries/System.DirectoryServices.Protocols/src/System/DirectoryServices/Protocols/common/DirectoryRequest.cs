// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Collections.Specialized;

namespace System.DirectoryServices.Protocols
{
    public abstract class DirectoryRequest : DirectoryOperation
    {
        internal DirectoryRequest()
        {
        }

        public string RequestId
        {
            get => _directoryRequestID;
            set => _directoryRequestID = value;
        }

        public DirectoryControlCollection Controls { get; } = new DirectoryControlCollection();
    }

    public class DeleteRequest : DirectoryRequest
    {
        public DeleteRequest() { }

        public DeleteRequest(string distinguishedName)
        {
            DistinguishedName = distinguishedName;
        }

        public string DistinguishedName { get; set; }
    }

    public class AddRequest : DirectoryRequest
    {
        public AddRequest() { }

        public AddRequest(string distinguishedName, params DirectoryAttribute[] attributes) : this()
        {
            DistinguishedName = distinguishedName;

            if (attributes != null)
            {
                for (int i = 0; i < attributes.Length; i++)
                {
                    Attributes.Add(attributes[i]);
                }
            }
        }

        public AddRequest(string distinguishedName, string objectClass) : this()
        {
            if (objectClass == null)
            {
                throw new ArgumentNullException(nameof(objectClass));
            }
            
            DistinguishedName = distinguishedName;

            var objClassAttr = new DirectoryAttribute()
            {
                Name = "objectClass"
            };
            objClassAttr.Add(objectClass);
            Attributes.Add(objClassAttr);
        }

        public string DistinguishedName { get; set; }

        public DirectoryAttributeCollection Attributes { get; } = new DirectoryAttributeCollection();
    }

    public class ModifyRequest : DirectoryRequest
    {
        public ModifyRequest() { }

        public ModifyRequest(string distinguishedName, params DirectoryAttributeModification[] modifications) : this()
        {
            DistinguishedName = distinguishedName;
            Modifications.AddRange(modifications);
        }

        public ModifyRequest(string distinguishedName, DirectoryAttributeOperation operation, string attributeName, params object[] values) : this()
        {
            if (attributeName == null)
            {
                throw new ArgumentNullException(nameof(attributeName));
            }

            DistinguishedName = distinguishedName;
            var mod = new DirectoryAttributeModification()
            {
                Operation = operation,
                Name = attributeName
            };
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    mod.Add(values[i]);
                }
            }

            Modifications.Add(mod);
        }

        public string DistinguishedName { get; set; }

        public DirectoryAttributeModificationCollection Modifications { get; } = new DirectoryAttributeModificationCollection();
    }

    public class CompareRequest : DirectoryRequest
    {
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
            {
                throw new ArgumentNullException(nameof(assertion));
            }
            if (assertion.Count != 1)
            {
                throw new ArgumentException(SR.WrongNumValuesCompare);
            }

            CompareRequestHelper(distinguishedName, assertion.Name, assertion[0]);
        }

        private void CompareRequestHelper(string distinguishedName, string attributeName, object value)
        {
            if (attributeName == null)
            {
                throw new ArgumentNullException(nameof(attributeName));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            DistinguishedName = distinguishedName;           
            Assertion.Name = attributeName;
            Assertion.Add(value);
        }

        public string DistinguishedName { get; set; }

        public DirectoryAttribute Assertion { get; } = new DirectoryAttribute();
    }

    public class ModifyDNRequest : DirectoryRequest
    {
        public ModifyDNRequest() { }

        public ModifyDNRequest(string distinguishedName, string newParentDistinguishedName, string newName)
        {
            DistinguishedName = distinguishedName;
            NewParentDistinguishedName = newParentDistinguishedName;
            NewName = newName;
        }

        public string DistinguishedName { get; set; }

        public string NewParentDistinguishedName { get; set; }

        public string NewName { get; set; }

        public bool DeleteOldRdn { get; set; } = true;
    }

    public class ExtendedRequest : DirectoryRequest
    {
        private byte[] _requestValue = null;

        public ExtendedRequest() { }

        public ExtendedRequest(string requestName)
        {
            RequestName = requestName;
        }

        public ExtendedRequest(string requestName, byte[] requestValue) : this(requestName)
        {
            _requestValue = requestValue;
        }

        public string RequestName { get; set; }

        public byte[] RequestValue
        {
            get
            {
                if (_requestValue == null)
                {
                    return Array.Empty<byte>();
                }

                byte[] tempValue = new byte[_requestValue.Length];
                for (int i = 0; i < _requestValue.Length; i++)
                {
                    tempValue[i] = _requestValue[i];
                }
                return tempValue;
            }
            set => _requestValue = value;
        }
    }

    public class SearchRequest : DirectoryRequest
    {
        public SearchRequest() { }

        public SearchRequest(string distinguishedName, string ldapFilter, SearchScope searchScope, params string[] attributeList) : this()
        {
            DistinguishedName = distinguishedName;

            if (attributeList != null)
            {
                for (int i = 0; i < attributeList.Length; i++)
                {
                    Attributes.Add(attributeList[i]);
                }
            }

            Scope = searchScope;
            Filter = ldapFilter;
        }

        public string DistinguishedName { get; set; }

        public StringCollection Attributes { get; } = new StringCollection();

        public object Filter
        {
            get => _directoryFilter;
            set
            {
                if (value != null && !(value is string))
                {
                    throw new ArgumentException(SR.ValidFilterType, nameof(value));
                }

                _directoryFilter = value;
            }
        }

        public SearchScope Scope
        {
            get => _directoryScope;
            set
            {
                if (value < SearchScope.Base || value > SearchScope.Subtree)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(SearchScope));
                }

                _directoryScope = value;
            }
        }

        public DereferenceAlias Aliases
        {
            get => _directoryRefAlias;
            set
            {
                if (value < DereferenceAlias.Never || value > DereferenceAlias.Always)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(DereferenceAlias));
                }

                _directoryRefAlias = value;
            }
        }

        public int SizeLimit
        {
            get => _directorySizeLimit;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.NoNegativeSizeLimit, nameof(value));
                }

                _directorySizeLimit = value;
            }
        }

        public TimeSpan TimeLimit
        {
            get => _directoryTimeLimit;
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException(SR.NoNegativeTime, nameof(value));
                }

                // Prevent integer overflow.
                if (value.TotalSeconds > int.MaxValue)
                {
                    throw new ArgumentException(SR.TimespanExceedMax, nameof(value));
                }

                _directoryTimeLimit = value;
            }
        }

        public bool TypesOnly { get; set; }

        private object _directoryFilter = null;
        private SearchScope _directoryScope = SearchScope.Subtree;
        private DereferenceAlias _directoryRefAlias = DereferenceAlias.Never;
        private int _directorySizeLimit = 0;
        private TimeSpan _directoryTimeLimit = new TimeSpan(0);
    }
}

namespace System.DirectoryServices.Protocols
{
    public class DsmlAuthRequest : DirectoryRequest
    {
        public DsmlAuthRequest() => Principal = string.Empty;

        public DsmlAuthRequest(string principal) => Principal = principal;

        public string Principal { get; set; }
    }
}
