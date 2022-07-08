// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Configuration;

namespace System.Diagnostics
{
    internal class TypedElement : ConfigurationElement
    {
        protected static readonly ConfigurationProperty _propTypeName = new ConfigurationProperty("type", typeof(string), string.Empty, ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsTypeStringTransformationRequired);
        protected static readonly ConfigurationProperty _propInitData = new ConfigurationProperty("initializeData", typeof(string), string.Empty, ConfigurationPropertyOptions.None);

        protected ConfigurationPropertyCollection _properties;
        protected object _runtimeObject;
        private Type _baseType;

        public TypedElement(Type baseType) : base()
        {
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_propTypeName);
            _properties.Add(_propInitData);

            _baseType = baseType;
        }

        [ConfigurationProperty("initializeData", DefaultValue = "")]
        public string InitData
        {
            get
            {
                return (string)this[_propInitData];
            }
            // This is useful when the OM becomes public. In the meantime, this can be utilized via reflection
            set
            {
                this[_propInitData] = value;
            }

        }

        protected internal override ConfigurationPropertyCollection Properties
        {
            get
            {
                return _properties;
            }
        }

        [ConfigurationProperty("type", IsRequired = true, DefaultValue = "")]
        public virtual string TypeName
        {
            get
            {
                return (string)this[_propTypeName];
            }
            set
            {
                this[_propTypeName] = value;
            }
        }

        protected object BaseGetRuntimeObject()
        {
            if (_runtimeObject == null)
                _runtimeObject = TraceUtils.GetRuntimeObject(TypeName, _baseType, InitData);

            return _runtimeObject;
        }

    }
}
