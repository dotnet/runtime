// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace System.Configuration
{
    public sealed class ConfigurationProperty
    {
        internal static readonly ConfigurationValidatorBase s_nonEmptyStringValidator = new StringValidator(1);
        private static readonly ConfigurationValidatorBase s_defaultValidatorInstance = new DefaultValidator();
        internal static readonly string s_defaultCollectionPropertyName = "";
        private TypeConverter _converter;
        private volatile bool _isConfigurationElementType;
        private volatile bool _isTypeInited;
        private ConfigurationPropertyOptions _options;

        public ConfigurationProperty(string name, Type type)
        {
            object defaultValue = null;

            ConstructorInit(name, type, ConfigurationPropertyOptions.None, null, null);

            if (type == typeof(string)) defaultValue = string.Empty;
            else
            {
                if (type.IsValueType) defaultValue = TypeUtil.CreateInstance(type);
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
            ConstructorInit(name, type, options, validator, typeConverter);

            SetDefaultValue(defaultValue);
        }

        internal ConfigurationProperty(PropertyInfo info)
        {
            Debug.Assert(info != null, "info != null");

            // Bellow are the attributes we handle
            ConfigurationPropertyAttribute attribProperty = null;

            // Compatability attributes
            // If the approprite data is provided in the ConfigPropAttribute then the one bellow will be ignored
            DescriptionAttribute attribStdDescription = null;
            DefaultValueAttribute attribStdDefault = null;

            TypeConverter typeConverter = null;
            ConfigurationValidatorBase validator = null;

            // Find the interesting attributes in the collection
            foreach (Attribute attribute in Attribute.GetCustomAttributes(info))
                if (attribute is TypeConverterAttribute)
                {
                    TypeConverterAttribute attribConverter = (TypeConverterAttribute)attribute;
                    typeConverter = TypeUtil.CreateInstance<TypeConverter>(attribConverter.ConverterTypeName);
                }
                else
                {
                    if (attribute is ConfigurationPropertyAttribute)
                        attribProperty = (ConfigurationPropertyAttribute)attribute;
                    else
                    {
                        if (attribute is ConfigurationValidatorAttribute)
                        {
                            // There could be more then one validator attribute specified on a property
                            // Currently we consider this an error since it's too late to fix it for whidbey
                            // but the right thing to do is to introduce new validator type ( CompositeValidator ) that is a list of validators and executes
                            // them all

                            if (validator != null)
                            {
                                throw new ConfigurationErrorsException(
                                    string.Format(SR.Validator_multiple_validator_attributes, info.Name));
                            }

                            ConfigurationValidatorAttribute attribValidator = (ConfigurationValidatorAttribute)attribute;
                            attribValidator.SetDeclaringType(info.DeclaringType);
                            validator = attribValidator.ValidatorInstance;
                        }
                        else
                        {
                            if (attribute is DescriptionAttribute)
                                attribStdDescription = (DescriptionAttribute)attribute;
                            else
                            {
                                if (attribute is DefaultValueAttribute)
                                    attribStdDefault = (DefaultValueAttribute)attribute;
                            }
                        }
                    }
                }

            Type propertyType = info.PropertyType;
            // Collections need some customization when the collection attribute is present
            if (typeof(ConfigurationElementCollection).IsAssignableFrom(propertyType))
            {
                ConfigurationCollectionAttribute attribCollection =
                    Attribute.GetCustomAttribute(info,
                        typeof(ConfigurationCollectionAttribute)) as ConfigurationCollectionAttribute ??
                    Attribute.GetCustomAttribute(propertyType,
                        typeof(ConfigurationCollectionAttribute)) as ConfigurationCollectionAttribute;

                // If none on the property - see if there is an attribute on the collection type itself
                if (attribCollection != null)
                {
                    if (attribCollection.AddItemName.IndexOf(',') == -1) AddElementName = attribCollection.AddItemName;
                    RemoveElementName = attribCollection.RemoveItemName;
                    ClearElementName = attribCollection.ClearItemsName;
                }
            }

            // This constructor shouldnt be invoked if the reflection info is not for an actual config property
            Debug.Assert(attribProperty != null, "attribProperty != null");

            ConstructorInit(attribProperty.Name,
                info.PropertyType,
                attribProperty.Options,
                validator,
                typeConverter);

            // Figure out the default value
            InitDefaultValueFromTypeInfo(attribProperty, attribStdDefault);

            // Get the description
            if (!string.IsNullOrEmpty(attribStdDescription?.Description))
                Description = attribStdDescription.Description;
        }

        public string Name { get; private set; }

        public string Description { get; }

        internal string ProvidedName { get; private set; }

        internal bool IsConfigurationElementType
        {
            get
            {
                if (_isTypeInited) return _isConfigurationElementType;

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

        private void ConstructorInit(string name,
            Type type,
            ConfigurationPropertyOptions options,
            ConfigurationValidatorBase validator,
            TypeConverter converter)
        {
            if (typeof(ConfigurationSection).IsAssignableFrom(type))
            {
                throw new ConfigurationErrorsException(
                    string.Format(SR.Config_properties_may_not_be_derived_from_configuration_section, name));
            }

            ProvidedName = name; // save the provided name so we can check for default collection names
            if (((options & ConfigurationPropertyOptions.IsDefaultCollection) != 0) &&
                string.IsNullOrEmpty(name))
                name = s_defaultCollectionPropertyName;
            else ValidatePropertyName(name);

            Name = name;
            Type = type;
            _options = options;
            Validator = validator;
            _converter = converter;

            // Use the default validator if none was supplied
            if (Validator == null) Validator = s_defaultValidatorInstance;
            else
            {
                // Make sure the supplied validator supports the type of this property
                if (!Validator.CanValidate(Type))
                    throw new ConfigurationErrorsException(string.Format(SR.Validator_does_not_support_prop_type, Name));
            }
        }

        private void ValidatePropertyName(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException(SR.String_null_or_empty, nameof(name));

            if (BaseConfigurationRecord.IsReservedAttributeName(name))
                throw new ArgumentException(string.Format(SR.Property_name_reserved, name));
        }

        private void SetDefaultValue(object value)
        {
            // Validate the default value if any. This should make errors from invalid defaults easier to catch
            if ((value == null) || (value == ConfigurationElement.s_nullPropertyValue)) return;

            if (!Type.IsInstanceOfType(value))
            {
                if (Converter.CanConvertFrom(value.GetType()))
                    value = Converter.ConvertFrom(value);
                else
                    throw new ConfigurationErrorsException(string.Format(SR.Default_value_wrong_type, Name));
            }

            Validate(value);
            DefaultValue = value;
        }

        private void InitDefaultValueFromTypeInfo(ConfigurationPropertyAttribute attribProperty,
            DefaultValueAttribute attribStdDefault)
        {
            object defaultValue = attribProperty.DefaultValue;

            // If there is no default value there - try the other attribute ( the clr standard one )
            if (((defaultValue == null) || (defaultValue == ConfigurationElement.s_nullPropertyValue)) &&
                (attribStdDefault != null))
                defaultValue = attribStdDefault.Value;

            // If there was a default value in the prop attribute - check if we need to convert it from string
            if (defaultValue is string && (Type != typeof(string)))
            {
                // Use the converter to parse this property default value
                try
                {
                    defaultValue = Converter.ConvertFromInvariantString((string)defaultValue);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationErrorsException(string.Format(SR.Default_value_conversion_error_from_string,
                        Name, ex.Message));
                }
            }

            if ((defaultValue == null) || (defaultValue == ConfigurationElement.s_nullPropertyValue))
            {
                if (Type == typeof(string)) defaultValue = string.Empty;
                else
                {
                    if (Type.IsValueType) defaultValue = TypeUtil.CreateInstance(Type);
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
                throw new ConfigurationErrorsException(string.Format(SR.Top_level_conversion_error_from_string, Name,
                    ex.Message));
            }

            return result;
        }

        internal string ConvertToString(object value)
        {
            string result;

            try
            {
                if (Type == typeof(bool))
                    result = (bool)value ? "true" : "false"; // the converter will break 1.1 compat for bool
                else result = Converter.ConvertToInvariantString(value);
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException(string.Format(SR.Top_level_conversion_error_to_string, Name,
                    ex.Message));
            }

            return result;
        }

        internal void Validate(object value)
        {
            try
            {
                Validator.Validate(value);
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException(string.Format(SR.Top_level_validation_error, Name, ex.Message),
                    ex);
            }
        }

        private void CreateConverter()
        {
            // Some properties cannot have type converters.
            // Such examples are properties that are ConfigurationElement ( derived classes )
            // or properties which are user-defined and the user code handles serialization/desirialization so
            // the property itself is never converted to/from string

            if (_converter != null) return;

            // Enums are exception. We use our custom converter for all enums
            if (Type.IsEnum) _converter = new GenericEnumConverter(Type);
            else
            {
                if (Type.IsSubclassOf(typeof(ConfigurationElement))) return;

                _converter = TypeDescriptor.GetConverter(Type);
                if ((_converter == null) ||
                    !_converter.CanConvertFrom(typeof(string)) ||
                    !_converter.CanConvertTo(typeof(string)))
                    throw new ConfigurationErrorsException(string.Format(SR.No_converter, Name, Type.Name));
            }
        }
    }
}