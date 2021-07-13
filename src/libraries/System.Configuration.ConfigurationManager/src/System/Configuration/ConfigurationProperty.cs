// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace System.Configuration
{
    public sealed class ConfigurationProperty
    {
        internal static readonly ConfigurationValidatorBase s_nonEmptyStringValidator = new StringValidator(1);
        private static readonly ConfigurationValidatorBase s_defaultValidatorInstance = new DefaultValidator();
        internal const string DefaultCollectionPropertyName = "";
        private TypeConverter _converter;
        private volatile bool _isConfigurationElementType;
        private volatile bool _isTypeInited;
        private ConfigurationPropertyOptions _options;

        public ConfigurationProperty(string name, Type type)
        {
            object defaultValue = null;

            ConstructorInit(name, type, ConfigurationPropertyOptions.None, null, null, null);

            if (type == typeof(string))
            {
                defaultValue = string.Empty;
            }
            else if (type.IsValueType)
            {
                defaultValue = TypeUtil.CreateInstance(type);
            }

            SetDefaultValue(defaultValue);
        }

        public ConfigurationProperty(string name, Type type, object defaultValue)
            : this(name, type, defaultValue, ConfigurationPropertyOptions.None)
        { }

        public ConfigurationProperty(string name, Type type, object defaultValue, ConfigurationPropertyOptions options)
            : this(name, type, defaultValue, null, null, options)
        { }

        public ConfigurationProperty(string name,
            Type type,
            object defaultValue,
            TypeConverter typeConverter,
            ConfigurationValidatorBase validator,
            ConfigurationPropertyOptions options)
            : this(name, type, defaultValue, typeConverter, validator, options, null)
        { }

        public ConfigurationProperty(string name,
            Type type,
            object defaultValue,
            TypeConverter typeConverter,
            ConfigurationValidatorBase validator,
            ConfigurationPropertyOptions options,
            string description)
        {
            ConstructorInit(name, type, options, validator, typeConverter, description);

            SetDefaultValue(defaultValue);
        }

        internal ConfigurationProperty(PropertyInfo info)
        {
            Debug.Assert(info != null, "info != null");

            ConfigurationPropertyAttribute propertyAttribute = null;
            DescriptionAttribute descriptionAttribute = null;

            // For compatibility we read the component model default value attribute. It is only
            // used if ConfigurationPropertyAttribute doesn't provide the default value.
            DefaultValueAttribute defaultValueAttribute = null;

            TypeConverter typeConverter = null;
            ConfigurationValidatorBase validator = null;

            // Look for relevant attributes
            foreach (Attribute attribute in Attribute.GetCustomAttributes(info))
            {
                if (attribute is TypeConverterAttribute)
                {
                    typeConverter = TypeUtil.CreateInstance<TypeConverter>(((TypeConverterAttribute)attribute).ConverterTypeName);
                }
                else if (attribute is ConfigurationPropertyAttribute)
                {
                    propertyAttribute = (ConfigurationPropertyAttribute)attribute;
                }
                else if (attribute is ConfigurationValidatorAttribute)
                {
                    if (validator != null)
                    {
                        // We only allow one validator to be specified on a property.
                        //
                        // Consider: introduce a new validator type ( CompositeValidator ) that is a
                        // list of validators and executes them all

                        throw new ConfigurationErrorsException(
                            SR.Format(SR.Validator_multiple_validator_attributes, info.Name));
                    }

                    ConfigurationValidatorAttribute validatorAttribute = (ConfigurationValidatorAttribute)attribute;
                    validatorAttribute.SetDeclaringType(info.DeclaringType);
                    validator = validatorAttribute.ValidatorInstance;
                }
                else if (attribute is DescriptionAttribute)
                {
                    descriptionAttribute = (DescriptionAttribute)attribute;
                }
                else if (attribute is DefaultValueAttribute)
                {
                    defaultValueAttribute = (DefaultValueAttribute)attribute;
                }
            }

            Type propertyType = info.PropertyType;

            // If the property is a Collection we need to look for the ConfigurationCollectionAttribute for
            // additional customization.
            if (typeof(ConfigurationElementCollection).IsAssignableFrom(propertyType))
            {
                // Look for the ConfigurationCollection attribute on the property itself, fall back
                // on the property type.
                ConfigurationCollectionAttribute collectionAttribute =
                    Attribute.GetCustomAttribute(info,
                        typeof(ConfigurationCollectionAttribute)) as ConfigurationCollectionAttribute ??
                    Attribute.GetCustomAttribute(propertyType,
                        typeof(ConfigurationCollectionAttribute)) as ConfigurationCollectionAttribute;

                if (collectionAttribute != null)
                {
#if NETCOREAPP
                    if (!collectionAttribute.AddItemName.Contains(','))
#else
                    if (collectionAttribute.AddItemName.IndexOf(',') == -1)
#endif
                    {
                      AddElementName = collectionAttribute.AddItemName;
                    }
                    RemoveElementName = collectionAttribute.RemoveItemName;
                    ClearElementName = collectionAttribute.ClearItemsName;
                }
            }

            // This constructor shouldn't be invoked if the reflection info is not for an actual config property
            Debug.Assert(propertyAttribute != null, "attribProperty != null");

            ConstructorInit(propertyAttribute.Name,
                info.PropertyType,
                propertyAttribute.Options,
                validator,
                typeConverter,
                descriptionAttribute?.Description);

            // Figure out the default value
            InitDefaultValueFromTypeInfo(propertyAttribute, defaultValueAttribute);
        }

        public string Name { get; private set; }

        public string Description { get; private set; }

        internal string ProvidedName { get; private set; }

        internal bool IsConfigurationElementType
        {
            get
            {
                if (_isTypeInited)
                    return _isConfigurationElementType;

                _isConfigurationElementType = typeof(ConfigurationElement).IsAssignableFrom(Type);
                _isTypeInited = true;
                return _isConfigurationElementType;
            }
        }

        public Type Type { get; private set; }

        public object DefaultValue { get; private set; }

        public bool IsRequired => (_options & ConfigurationPropertyOptions.IsRequired) != 0;

        public bool IsKey => (_options & ConfigurationPropertyOptions.IsKey) != 0;

        public bool IsDefaultCollection => (_options & ConfigurationPropertyOptions.IsDefaultCollection) != 0;

        public bool IsTypeStringTransformationRequired
            => (_options & ConfigurationPropertyOptions.IsTypeStringTransformationRequired) != 0;

        public bool IsAssemblyStringTransformationRequired
            => (_options & ConfigurationPropertyOptions.IsAssemblyStringTransformationRequired) != 0;

        public bool IsVersionCheckRequired => (_options & ConfigurationPropertyOptions.IsVersionCheckRequired) != 0;

        public TypeConverter Converter
        {
            get
            {
                CreateConverter();
                return _converter;
            }
        }

        public ConfigurationValidatorBase Validator { get; private set; }

        internal string AddElementName { get; }

        internal string RemoveElementName { get; }

        internal string ClearElementName { get; }

        private void ConstructorInit(
            string name,
            Type type,
            ConfigurationPropertyOptions options,
            ConfigurationValidatorBase validator,
            TypeConverter converter,
            string description)
        {
            if (typeof(ConfigurationSection).IsAssignableFrom(type))
            {
                throw new ConfigurationErrorsException(
                    SR.Format(SR.Config_properties_may_not_be_derived_from_configuration_section, name));
            }

            // save the provided name so we can check for default collection names
            ProvidedName = name;

            if (((options & ConfigurationPropertyOptions.IsDefaultCollection) != 0) && string.IsNullOrEmpty(name))
            {
                name = DefaultCollectionPropertyName;
            }
            else
            {
                ValidatePropertyName(name);
            }

            Name = name;
            Description = description;
            Type = type;
            _options = options;
            Validator = validator;
            _converter = converter;

            // Use the default validator if none was supplied
            if (Validator == null)
            {
                Validator = s_defaultValidatorInstance;
            }
            else
            {
                // Make sure the supplied validator supports the type of this property
                if (!Validator.CanValidate(Type))
                    throw new ConfigurationErrorsException(SR.Format(SR.Validator_does_not_support_prop_type, Name));
            }
        }

        private void ValidatePropertyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(SR.String_null_or_empty, nameof(name));

            if (BaseConfigurationRecord.IsReservedAttributeName(name))
                throw new ArgumentException(SR.Format(SR.Property_name_reserved, name));
        }

        private void SetDefaultValue(object value)
        {
            // Validate the default value if any. This should make errors from invalid defaults easier to catch
            if (ConfigurationElement.IsNullOrNullProperty(value))
                return;

            if (!Type.IsInstanceOfType(value))
            {
                if (!Converter.CanConvertFrom(value.GetType()))
                    throw new ConfigurationErrorsException(SR.Format(SR.Default_value_wrong_type, Name));

                value = Converter.ConvertFrom(value);
            }

            Validate(value);
            DefaultValue = value;
        }

        private void InitDefaultValueFromTypeInfo(
            ConfigurationPropertyAttribute configurationProperty,
            DefaultValueAttribute defaultValueAttribute)
        {
            object defaultValue = configurationProperty.DefaultValue;

            // If the ConfigurationPropertyAttribute has no default try the DefaultValueAttribute
            if (ConfigurationElement.IsNullOrNullProperty(defaultValue))
                defaultValue = defaultValueAttribute?.Value;

            // Convert the default value from string if necessary
            if (defaultValue is string && (Type != typeof(string)))
            {
                try
                {
                    defaultValue = Converter.ConvertFromInvariantString((string)defaultValue);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationErrorsException(SR.Format(SR.Default_value_conversion_error_from_string,
                        Name, ex.Message));
                }
            }

            // If we still have no default, use string Empty for string or the default for value types
            if (ConfigurationElement.IsNullOrNullProperty(defaultValue))
            {
                if (Type == typeof(string))
                {
                    defaultValue = string.Empty;
                }
                else if (Type.IsValueType)
                {
                    defaultValue = TypeUtil.CreateInstance(Type);
                }
            }

            SetDefaultValue(defaultValue);
        }

        internal object ConvertFromString(string value)
        {
            object result;

            try
            {
                result = Converter.ConvertFromInvariantString(value);
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException(SR.Format(SR.Top_level_conversion_error_from_string, Name,
                    ex.Message));
            }

            return result;
        }

        internal string ConvertToString(object value)
        {
            try
            {
                if (Type == typeof(bool))
                {
                    // The boolean converter will break 1.1 compat for bool
                    return (bool)value ? "true" : "false";
                }
                else
                {
                    return Converter.ConvertToInvariantString(value);
                }
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException(
                    SR.Format(SR.Top_level_conversion_error_to_string, Name, ex.Message));
            }
        }

        internal void Validate(object value)
        {
            try
            {
                Validator.Validate(value);
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException(
                    SR.Format(SR.Top_level_validation_error, Name, ex.Message), ex);
            }
        }

        private void CreateConverter()
        {
            if (_converter != null) return;

            if (Type.IsEnum)
            {
                // We use our custom converter for all enums
                _converter = new GenericEnumConverter(Type);
            }
            else if (Type.IsSubclassOf(typeof(ConfigurationElement)))
            {
                // Type converters aren't allowed on ConfigurationElement
                // derived classes.
                return;
            }
            else
            {
                _converter = TypeDescriptor.GetConverter(Type);

                if ((_converter == null) ||
                    !_converter.CanConvertFrom(typeof(string)) ||
                    !_converter.CanConvertTo(typeof(string)))
                {
                    // Need to be able to convert to/from string
                    throw new ConfigurationErrorsException(SR.Format(SR.No_converter, Name, Type.Name));
                }
            }
        }
    }
}
