using System;
using System.Collections.Generic;
using System.Text;


namespace System.DirectoryServices.AccountManagement
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class DirectoryPropertyAttribute : Attribute
    {
        private string schemaAttributeName;
        private Nullable<ContextType> context;
        public DirectoryPropertyAttribute(string schemaAttributeName)
        {
            this.schemaAttributeName = schemaAttributeName;
            this.context = null;
        }
        public string SchemaAttributeName
        {
            get
            {
                return schemaAttributeName;
            }
        }
        public Nullable<ContextType> Context
        {
            get
            {
                return context;
            }
            set
            {
                context = value;
            }
        }


    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DirectoryRdnPrefixAttribute : Attribute
    {
        private string rdnPrefix;
        private Nullable<ContextType> context;

        public DirectoryRdnPrefixAttribute (string rdnPrefix)
        {
            this.rdnPrefix = rdnPrefix;
            this.context = null;
        }
        public string RdnPrefix
        {
            get
            {
                return rdnPrefix;
            }
        }
        public Nullable<ContextType> Context
        {
            get
            {
                return context;
            }
        }


    }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DirectoryObjectClassAttribute : Attribute
    {
        private string objectClass;
        private Nullable<ContextType> context;

        public DirectoryObjectClassAttribute (string objectClass)
        {
            this.objectClass = objectClass;
            this.context = null;
        }
        public string ObjectClass
        {
            get
            {
                return objectClass;
            }
        }
        public Nullable<ContextType> Context
        {
            get
            {
                return context;
            }
        }


    }
}

