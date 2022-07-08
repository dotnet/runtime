//------------------------------------------------------------------------------
// <copyright file="TypedElement.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System.Configuration;
using System;
using System.Reflection;
using System.Globalization;

namespace System.Diagnostics {
    internal class TypedElement : ConfigurationElement {
        protected static readonly ConfigurationProperty _propTypeName = new ConfigurationProperty("type", typeof(string), String.Empty, ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsTypeStringTransformationRequired);
        protected static readonly ConfigurationProperty _propInitData = new ConfigurationProperty("initializeData", typeof(string), String.Empty, ConfigurationPropertyOptions.None);

        protected ConfigurationPropertyCollection _properties;
        protected object _runtimeObject = null;
        private Type _baseType;

        public TypedElement(Type baseType) : base() {
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_propTypeName);
            _properties.Add(_propInitData);

            _baseType = baseType;
        }

        [ConfigurationProperty("initializeData", DefaultValue = "")]
        public string InitData {
            get {
                return (string) this[_propInitData];
            }
            // This is useful when the OM becomes public. In the meantime, this can be utilized via reflection
            set {
                this[_propInitData] = value;
            }

        }

        protected override ConfigurationPropertyCollection Properties {
            get {
                return _properties;
            }
        }

        [ConfigurationProperty("type", IsRequired = true, DefaultValue = "")]
        public virtual string TypeName {
            get {
                return (string) this[_propTypeName];
            }
            set {
                this[_propTypeName] = value;
            }
        }

        protected object BaseGetRuntimeObject() {
            if (_runtimeObject == null)
                _runtimeObject = TraceUtils.GetRuntimeObject(TypeName, _baseType, InitData);

            return _runtimeObject;
        }

    }
}
