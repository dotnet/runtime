// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.DirectoryServices.AccountManagement
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class DirectoryPropertyAttribute : Attribute
    {
        private string _schemaAttributeName;
        private Nullable<ContextType> _context;
        public DirectoryPropertyAttribute(string schemaAttributeName)
        {
            _schemaAttributeName = schemaAttributeName;
            _context = null;
        }
        public string SchemaAttributeName
        {
            get
            {
                return _schemaAttributeName;
            }
        }
        public Nullable<ContextType> Context
        {
            get
            {
                return _context;
            }
            set
            {
                _context = value;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DirectoryRdnPrefixAttribute : Attribute
    {
        private string _rdnPrefix;
        private Nullable<ContextType> _context;

        public DirectoryRdnPrefixAttribute(string rdnPrefix)
        {
            _rdnPrefix = rdnPrefix;
            _context = null;
        }
        public string RdnPrefix
        {
            get
            {
                return _rdnPrefix;
            }
        }
        public Nullable<ContextType> Context
        {
            get
            {
                return _context;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DirectoryObjectClassAttribute : Attribute
    {
        private string _objectClass;
        private Nullable<ContextType> _context;

        public DirectoryObjectClassAttribute(string objectClass)
        {
            _objectClass = objectClass;
            _context = null;
        }
        public string ObjectClass
        {
            get
            {
                return _objectClass;
            }
        }
        public Nullable<ContextType> Context
        {
            get
            {
                return _context;
            }
        }
    }
}

