// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
    internal class TypedElement : ConfigurationElement
    {
        protected static readonly ConfigurationProperty s_propTypeName = new("type", typeof(string), string.Empty, ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsTypeStringTransformationRequired);
        protected static readonly ConfigurationProperty s_propInitData = new("initializeData", typeof(string), string.Empty, ConfigurationPropertyOptions.None);

        protected readonly ConfigurationPropertyCollection _properties;
        protected object _runtimeObject;
        private readonly Type _baseType;

        public TypedElement(Type baseType) : base()
        {
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(s_propTypeName);
            _properties.Add(s_propInitData);

            _baseType = baseType;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty("initializeData", DefaultValue = "")]
        public string InitData
        {
            get
            {
                return (string)this[s_propInitData];
            }
            // This is useful when the OM becomes public. In the meantime, this can be utilized via reflection.
            set
            {
                this[s_propInitData] = value;
            }
        }

        protected internal override ConfigurationPropertyCollection Properties => _properties;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty("type", IsRequired = true, DefaultValue = "")]
        public virtual string TypeName
        {
            get
            {
                return (string)this[s_propTypeName];
            }
            set
            {
                this[s_propTypeName] = value;
            }
        }

        protected object BaseGetRuntimeObject()
        {
            return _runtimeObject ??= TraceUtils.GetRuntimeObject(TypeName, _baseType, InitData);
        }
    }
}
