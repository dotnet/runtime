// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Static helper class that allows binding strongly typed objects to configuration values.
    /// </summary>
    public static class ConfigurationBinder
    {
        private const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        private const string TrimmingWarningMessage = "In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.";
        private const string InstanceGetTypeTrimmingWarningMessage = "Cannot statically analyze the type of instance so its members may be trimmed";
        private const string PropertyTrimmingWarningMessage = "Cannot statically analyze property.PropertyType so its members may be trimmed.";

        /// <summary>
        /// Attempts to bind the configuration instance to a new instance of type T.
        /// If this configuration section has a value, that will be used.
        /// Otherwise binding by matching property names against configuration keys recursively.
        /// </summary>
        /// <typeparam name="T">The type of the new instance to bind.</typeparam>
        /// <param name="configuration">The configuration instance to bind.</param>
        /// <returns>The new instance of T if successful, default(T) otherwise.</returns>
        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        public static T? Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this IConfiguration configuration)
            => configuration.Get<T>(_ => { });

        /// <summary>
        /// Attempts to bind the configuration instance to a new instance of type T.
        /// If this configuration section has a value, that will be used.
        /// Otherwise binding by matching property names against configuration keys recursively.
        /// </summary>
        /// <typeparam name="T">The type of the new instance to bind.</typeparam>
        /// <param name="configuration">The configuration instance to bind.</param>
        /// <param name="configureOptions">Configures the binder options.</param>
        /// <returns>The new instance of T if successful, default(T) otherwise.</returns>
        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        public static T? Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this IConfiguration configuration, Action<BinderOptions>? configureOptions)
        {
            ThrowHelper.ThrowIfNull(configuration);

            object? result = configuration.Get(typeof(T), configureOptions);
            if (result == null)
            {
                return default(T);
            }
            return (T)result;
        }

        /// <summary>
        /// Attempts to bind the configuration instance to a new instance of type T.
        /// If this configuration section has a value, that will be used.
        /// Otherwise binding by matching property names against configuration keys recursively.
        /// </summary>
        /// <param name="configuration">The configuration instance to bind.</param>
        /// <param name="type">The type of the new instance to bind.</param>
        /// <returns>The new instance if successful, null otherwise.</returns>
        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        public static object? Get(this IConfiguration configuration, Type type)
            => configuration.Get(type, _ => { });

        /// <summary>
        /// Attempts to bind the configuration instance to a new instance of type T.
        /// If this configuration section has a value, that will be used.
        /// Otherwise binding by matching property names against configuration keys recursively.
        /// </summary>
        /// <param name="configuration">The configuration instance to bind.</param>
        /// <param name="type">The type of the new instance to bind.</param>
        /// <param name="configureOptions">Configures the binder options.</param>
        /// <returns>The new instance if successful, null otherwise.</returns>
        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        public static object? Get(
            this IConfiguration configuration,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            Type type,
            Action<BinderOptions>? configureOptions)
        {
            ThrowHelper.ThrowIfNull(configuration);

            var options = new BinderOptions();
            configureOptions?.Invoke(options);
            var bindingPoint = new BindingPoint();
            BindInstance(type, bindingPoint, config: configuration, options: options);
            return bindingPoint.Value;
        }

        /// <summary>
        /// Attempts to bind the given object instance to the configuration section specified by the key by matching property names against configuration keys recursively.
        /// </summary>
        /// <param name="configuration">The configuration instance to bind.</param>
        /// <param name="key">The key of the configuration section to bind.</param>
        /// <param name="instance">The object to bind.</param>
        [RequiresUnreferencedCode(InstanceGetTypeTrimmingWarningMessage)]
        public static void Bind(this IConfiguration configuration, string key, object? instance)
            => configuration.GetSection(key).Bind(instance);

        /// <summary>
        /// Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.
        /// </summary>
        /// <param name="configuration">The configuration instance to bind.</param>
        /// <param name="instance">The object to bind.</param>
        [RequiresUnreferencedCode(InstanceGetTypeTrimmingWarningMessage)]
        public static void Bind(this IConfiguration configuration, object? instance)
            => configuration.Bind(instance, _ => { });

        /// <summary>
        /// Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.
        /// </summary>
        /// <param name="configuration">The configuration instance to bind.</param>
        /// <param name="instance">The object to bind.</param>
        /// <param name="configureOptions">Configures the binder options.</param>
        [RequiresUnreferencedCode(InstanceGetTypeTrimmingWarningMessage)]
        public static void Bind(this IConfiguration configuration, object? instance, Action<BinderOptions>? configureOptions)
        {
            ThrowHelper.ThrowIfNull(configuration);

            if (instance != null)
            {
                var options = new BinderOptions();
                configureOptions?.Invoke(options);
                var bindingPoint = new BindingPoint(instance, isReadOnly: true);
                BindInstance(instance.GetType(), bindingPoint, configuration, options);
            }
        }

        /// <summary>
        /// Extracts the value with the specified key and converts it to type T.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="configuration">The configuration.</param>
        /// <param name="key">The key of the configuration section's value to convert.</param>
        /// <returns>The converted value.</returns>
        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        public static T? GetValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this IConfiguration configuration, string key)
        {
            return GetValue(configuration, key, default(T));
        }

        /// <summary>
        /// Extracts the value with the specified key and converts it to type T.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="configuration">The configuration.</param>
        /// <param name="key">The key of the configuration section's value to convert.</param>
        /// <param name="defaultValue">The default value to use if no value is found.</param>
        /// <returns>The converted value.</returns>
        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        public static T? GetValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this IConfiguration configuration, string key, T defaultValue)
        {
            return (T?)GetValue(configuration, typeof(T), key, defaultValue);
        }

        /// <summary>
        /// Extracts the value with the specified key and converts it to the specified type.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="type">The type to convert the value to.</param>
        /// <param name="key">The key of the configuration section's value to convert.</param>
        /// <returns>The converted value.</returns>
        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        public static object? GetValue(
            this IConfiguration configuration,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            Type type,
            string key)
        {
            return GetValue(configuration, type, key, defaultValue: null);
        }

        /// <summary>
        /// Extracts the value with the specified key and converts it to the specified type.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="type">The type to convert the value to.</param>
        /// <param name="key">The key of the configuration section's value to convert.</param>
        /// <param name="defaultValue">The default value to use if no value is found.</param>
        /// <returns>The converted value.</returns>
        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        public static object? GetValue(
            this IConfiguration configuration,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            Type type, string key,
            object? defaultValue)
        {
            IConfigurationSection section = configuration.GetSection(key);
            string? value = section.Value;
            if (value != null)
            {
                return ConvertValue(type, value, section.Path);
            }
            return defaultValue;
        }

        [RequiresUnreferencedCode(PropertyTrimmingWarningMessage)]
        private static void BindNonScalar(this IConfiguration configuration, object instance, BinderOptions options)
        {
            PropertyInfo[] modelProperties = GetAllProperties(instance.GetType());

            if (options.ErrorOnUnknownConfiguration)
            {
                HashSet<string> propertyNames = new(modelProperties.Select(mp => mp.Name),
                    StringComparer.OrdinalIgnoreCase);

                IEnumerable<IConfigurationSection> configurationSections = configuration.GetChildren();
                List<string> missingPropertyNames = configurationSections
                    .Where(cs => !propertyNames.Contains(cs.Key))
                    .Select(mp => $"'{mp.Key}'")
                    .ToList();

                if (missingPropertyNames.Count > 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.Error_MissingConfig,
                        nameof(options.ErrorOnUnknownConfiguration), nameof(BinderOptions), instance.GetType(),
                        string.Join(", ", missingPropertyNames)));
                }
            }

            foreach (PropertyInfo property in modelProperties)
            {
                BindProperty(property, instance, configuration, options);
            }
        }

        [RequiresUnreferencedCode(PropertyTrimmingWarningMessage)]
        private static void BindProperty(PropertyInfo property, object instance, IConfiguration config, BinderOptions options)
        {
            // We don't support set only, non public, or indexer properties
            if (property.GetMethod == null ||
                (!options.BindNonPublicProperties && !property.GetMethod.IsPublic) ||
                property.GetMethod.GetParameters().Length > 0)
            {
                return;
            }

            var propertyBindingPoint = new BindingPoint(
                initialValueProvider: () => property.GetValue(instance),
                isReadOnly: property.SetMethod is null || (!property.SetMethod.IsPublic && !options.BindNonPublicProperties));

            BindInstance(
                property.PropertyType,
                propertyBindingPoint,
                config.GetSection(GetPropertyName(property)),
                options);

            if (propertyBindingPoint.HasNewValue)
            {
                property.SetValue(instance, propertyBindingPoint.Value);
            }
        }

        [RequiresUnreferencedCode("Cannot statically analyze what the element type is of the object collection in type so its members may be trimmed.")]
        private static object BindToCollection(Type type, IConfiguration config, BinderOptions options)
        {
            Type genericType = typeof(List<>).MakeGenericType(type.GenericTypeArguments[0]);
            object instance = Activator.CreateInstance(genericType)!;
            BindCollection(instance, genericType, config, options);
            return instance;
        }

        // Try to create an array/dictionary instance to back various collection interfaces
        [RequiresUnreferencedCode("In case type is a Dictionary, cannot statically analyze what the element type is of the value objects in the dictionary so its members may be trimmed.")]
        private static object? AttemptBindToCollectionInterfaces(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            Type type,
            IConfiguration config, BinderOptions options)
        {
            if (!type.IsInterface)
            {
                return null;
            }

            Type? collectionInterface = FindOpenGenericInterface(typeof(IReadOnlyList<>), type);
            if (collectionInterface != null)
            {
                // IEnumerable<T> is guaranteed to have exactly one parameter
                return BindToCollection(type, config, options);
            }

            collectionInterface = FindOpenGenericInterface(typeof(IReadOnlyDictionary<,>), type);
            if (collectionInterface != null)
            {
                Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(type.GenericTypeArguments[0], type.GenericTypeArguments[1]);
                object instance = Activator.CreateInstance(dictionaryType)!;
                BindDictionary(instance, dictionaryType, config, options);
                return instance;
            }

            collectionInterface = FindOpenGenericInterface(typeof(IDictionary<,>), type);
            if (collectionInterface != null)
            {
                object instance = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(type.GenericTypeArguments[0], type.GenericTypeArguments[1]))!;
                BindDictionary(instance, collectionInterface, config, options);
                return instance;
            }

            collectionInterface = FindOpenGenericInterface(typeof(IReadOnlyCollection<>), type);
            if (collectionInterface != null)
            {
                // IReadOnlyCollection<T> is guaranteed to have exactly one parameter
                return BindToCollection(type, config, options);
            }

            collectionInterface = FindOpenGenericInterface(typeof(ICollection<>), type);
            if (collectionInterface != null)
            {
                // ICollection<T> is guaranteed to have exactly one parameter
                return BindToCollection(type, config, options);
            }

            collectionInterface = FindOpenGenericInterface(typeof(IEnumerable<>), type);
            if (collectionInterface != null)
            {
                // IEnumerable<T> is guaranteed to have exactly one parameter
                return BindToCollection(type, config, options);
            }

            return null;
        }

        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        private static void BindInstance(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
            BindingPoint bindingPoint,
            IConfiguration config,
            BinderOptions options)
        {
            // if binding IConfigurationSection, break early
            if (type == typeof(IConfigurationSection))
            {
                bindingPoint.TrySetValue(config);
                return;
            }

            var section = config as IConfigurationSection;
            string? configValue = section?.Value;
            if (configValue != null && TryConvertValue(type, configValue, section?.Path, out object? convertedValue, out Exception? error))
            {
                if (error != null)
                {
                    throw error;
                }

                // Leaf nodes are always reinitialized
                bindingPoint.TrySetValue(convertedValue);
                return;
            }

            if (config != null && config.GetChildren().Any())
            {
                // for arrays and read-only list-like interfaces, we concatenate on to what is already there
                if (type.IsArray || IsArrayCompatibleReadOnlyInterface(type))
                {
                    if (!bindingPoint.IsReadOnly)
                    {
                        bindingPoint.SetValue(BindArray(type, (IEnumerable?)bindingPoint.Value, config, options));
                    }
                    return;
                }

                // If we don't have an instance, try to create one
                if (bindingPoint.Value is null)
                {
                    // if the binding point doesn't let us set a new instance, there's nothing more we can do
                    if (bindingPoint.IsReadOnly)
                    {
                        return;
                    }

                    object? boundFromInterface = AttemptBindToCollectionInterfaces(type, config, options);
                    if (boundFromInterface != null)
                    {
                        bindingPoint.SetValue(boundFromInterface);
                        return; // We are already done if binding to a new collection instance worked
                    }

                    bindingPoint.SetValue(CreateInstance(type, config, options));
                }

                // See if it's a Dictionary
                Type? collectionInterface = FindOpenGenericInterface(typeof(IDictionary<,>), type);
                if (collectionInterface != null)
                {
                    BindDictionary(bindingPoint.Value!, collectionInterface, config, options);
                }
                else
                {
                    // See if it's an ICollection
                    collectionInterface = FindOpenGenericInterface(typeof(ICollection<>), type);
                    if (collectionInterface != null)
                    {
                        BindCollection(bindingPoint.Value!, collectionInterface, config, options);
                    }
                    // Something else
                    else
                    {
                        BindNonScalar(config, bindingPoint.Value!, options);
                    }
                }
            }
        }

        [RequiresUnreferencedCode(
            "In case type is a Nullable<T>, cannot statically analyze what the underlying type is so its members may be trimmed.")]
        private static object CreateInstance(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                        DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type,
            IConfiguration config,
            BinderOptions options)
        {
            Debug.Assert(!type.IsArray);

            if (type.IsInterface || type.IsAbstract)
            {
                throw new InvalidOperationException(SR.Format(SR.Error_CannotActivateAbstractOrInterface, type));
            }

            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            bool hasParameterlessConstructor =
                type.IsValueType || constructors.Any(ctor => ctor.GetParameters().Length == 0);

            if (!type.IsValueType && constructors.Length == 0)
            {
                throw new InvalidOperationException(SR.Format(SR.Error_MissingPublicInstanceConstructor, type));
            }

            if (constructors.Length > 1 && !hasParameterlessConstructor)
            {
                throw new InvalidOperationException(SR.Format(SR.Error_MultipleParameterizedConstructors, type));
            }

            if (constructors.Length == 1 && !hasParameterlessConstructor)
            {
                ConstructorInfo constructor = constructors[0];
                ParameterInfo[] parameters = constructor.GetParameters();

                if (!CanBindToTheseConstructorParameters(parameters, out string nameOfInvalidParameter))
                {
                    throw new InvalidOperationException(SR.Format(SR.Error_CannotBindToConstructorParameter, type, nameOfInvalidParameter));
                }


                PropertyInfo[] properties = GetAllProperties(type);

                if (!DoAllParametersHaveEquivalentProperties(parameters, properties, out string nameOfInvalidParameters))
                {
                    throw new InvalidOperationException(SR.Format(SR.Error_ConstructorParametersDoNotMatchProperties, type, nameOfInvalidParameters));
                }

                object?[] parameterValues = new object?[parameters.Length];

                for (int index = 0; index < parameters.Length; index++)
                {
                    parameterValues[index] = BindParameter(parameters[index], type, config, options);
                }

                return constructor.Invoke(parameterValues);
            }

            object? instance;
            try
            {
                instance = Activator.CreateInstance(Nullable.GetUnderlyingType(type) ?? type);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(SR.Format(SR.Error_FailedToActivate, type), ex);
            }

            return instance ?? throw new InvalidOperationException(SR.Format(SR.Error_FailedToActivate, type));
        }

        private static bool DoAllParametersHaveEquivalentProperties(ParameterInfo[] parameters,
            PropertyInfo[] properties, out string missing)
        {
            HashSet<string> propertyNames = new(StringComparer.OrdinalIgnoreCase);
            foreach (PropertyInfo prop in properties)
            {
                propertyNames.Add(prop.Name);
            }

            List<string> missingParameters = new();

            foreach (ParameterInfo parameter in parameters)
            {
                string name = parameter.Name!;
                if (!propertyNames.Contains(name))
                {
                    missingParameters.Add(name);
                }
            }

            missing = string.Join(",", missingParameters);

            return missing.Length == 0;
        }

        private static bool CanBindToTheseConstructorParameters(ParameterInfo[] constructorParameters, out string nameOfInvalidParameter)
        {
            nameOfInvalidParameter = string.Empty;
            foreach (ParameterInfo p in constructorParameters)
            {
                if (p.IsOut || p.IsIn || p.ParameterType.IsByRef)
                {
                    nameOfInvalidParameter = p.Name!; // never null as we're not passed return value parameters: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.parameterinfo.name?view=net-6.0#remarks
                    return false;
                }
            }

            return true;
        }

        [RequiresUnreferencedCode("Cannot statically analyze what the element type is of the value objects in the dictionary so its members may be trimmed.")]
        private static void BindDictionary(
            object dictionary,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            Type dictionaryType,
            IConfiguration config, BinderOptions options)
        {
            // IDictionary<K,V> is guaranteed to have exactly two parameters
            Type keyType = dictionaryType.GenericTypeArguments[0];
            Type valueType = dictionaryType.GenericTypeArguments[1];
            bool keyTypeIsEnum = keyType.IsEnum;

            if (keyType != typeof(string) && !keyTypeIsEnum)
            {
                // We only support string and enum keys
                return;
            }
            MethodInfo tryGetValue = dictionaryType.GetMethod("TryGetValue")!;
            PropertyInfo setter = dictionaryType.GetProperty("Item", DeclaredOnlyLookup)!;
            foreach (IConfigurationSection child in config.GetChildren())
            {
                try
                {
                    object key = keyTypeIsEnum ? Enum.Parse(keyType, child.Key) : child.Key;
                    var valueBindingPoint = new BindingPoint(
                        initialValueProvider: () =>
                        {
                            var tryGetValueArgs = new object?[] { key, null };
                            return (bool)tryGetValue.Invoke(dictionary, tryGetValueArgs)! ? tryGetValueArgs[1] : null;
                        },
                        isReadOnly: false);
                    BindInstance(
                        type: valueType,
                        bindingPoint: valueBindingPoint,
                        config: child,
                        options: options);
                    if (valueBindingPoint.HasNewValue)
                    {
                        setter.SetValue(dictionary, valueBindingPoint.Value, new object[] { key });
                    }
                }
                catch
                {
                }
            }
        }

        [RequiresUnreferencedCode("Cannot statically analyze what the element type is of the object collection so its members may be trimmed.")]
        private static void BindCollection(
            object collection,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
            Type collectionType,
            IConfiguration config, BinderOptions options)
        {
            // ICollection<T> is guaranteed to have exactly one parameter
            Type itemType = collectionType.GenericTypeArguments[0];
            MethodInfo? addMethod = collectionType.GetMethod("Add", DeclaredOnlyLookup);

            foreach (IConfigurationSection section in config.GetChildren())
            {
                try
                {
                    BindingPoint itemBindingPoint = new();
                    BindInstance(
                        type: itemType,
                        bindingPoint: itemBindingPoint,
                        config: section,
                        options: options);
                    if (itemBindingPoint.HasNewValue)
                    {
                        addMethod?.Invoke(collection, new[] { itemBindingPoint.Value });
                    }
                }
                catch
                {
                }
            }
        }

        [RequiresUnreferencedCode("Cannot statically analyze what the element type is of the Array so its members may be trimmed.")]
        private static Array BindArray(Type type, IEnumerable? source, IConfiguration config, BinderOptions options)
        {
            Type elementType;
            if (type.IsArray)
            {
                if (type.GetArrayRank() > 1)
                {
                    throw new InvalidOperationException(SR.Format(SR.Error_UnsupportedMultidimensionalArray, type));
                }
                elementType = type.GetElementType()!;
            }
            else // e. g. IEnumerable<T>
            {
                elementType = type.GetGenericArguments()[0];
            }

            IList list = new List<object?>();

            if (source != null)
            {
                foreach (object? item in source)
                {
                    list.Add(item);
                }
            }

            foreach (IConfigurationSection section in config.GetChildren())
            {
                var itemBindingPoint = new BindingPoint();
                try
                {
                    BindInstance(
                        type: elementType,
                        bindingPoint: itemBindingPoint,
                        config: section,
                        options: options);
                    if (itemBindingPoint.HasNewValue)
                    {
                        list.Add(itemBindingPoint.Value);
                    }
                }
                catch
                {
                }
            }

            Array result = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(result, 0);
            return result;
        }

        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        private static bool TryConvertValue(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            Type type,
            string value, string? path, out object? result, out Exception? error)
        {
            error = null;
            result = null;
            if (type == typeof(object))
            {
                result = value;
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (string.IsNullOrEmpty(value))
                {
                    return true;
                }
                return TryConvertValue(Nullable.GetUnderlyingType(type)!, value, path, out result, out error);
            }

            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(typeof(string)))
            {
                try
                {
                    result = converter.ConvertFromInvariantString(value);
                }
                catch (Exception ex)
                {
                    error = new InvalidOperationException(SR.Format(SR.Error_FailedBinding, path, type), ex);
                }
                return true;
            }

            if (type == typeof(byte[]))
            {
                try
                {
                    result = Convert.FromBase64String(value);
                }
                catch (FormatException ex)
                {
                    error = new InvalidOperationException(SR.Format(SR.Error_FailedBinding, path, type), ex);
                }
                return true;
            }

            return false;
        }

        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        private static object? ConvertValue(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            Type type,
            string value, string? path)
        {
            TryConvertValue(type, value, path, out object? result, out Exception? error);
            if (error != null)
            {
                throw error;
            }
            return result;
        }

        private static bool IsArrayCompatibleReadOnlyInterface(Type type)
        {
            if (!type.IsInterface || !type.IsConstructedGenericType) { return false; }

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            return genericTypeDefinition == typeof(IEnumerable<>)
                || genericTypeDefinition == typeof(IReadOnlyCollection<>)
                || genericTypeDefinition == typeof(IReadOnlyList<>);
        }

        private static Type? FindOpenGenericInterface(
            Type expected,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
            Type actual)
        {
            if (actual.IsGenericType &&
                actual.GetGenericTypeDefinition() == expected)
            {
                return actual;
            }

            Type[] interfaces = actual.GetInterfaces();
            foreach (Type interfaceType in interfaces)
            {
                if (interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == expected)
                {
                    return interfaceType;
                }
            }
            return null;
        }

        private static PropertyInfo[] GetAllProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
            => type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        [RequiresUnreferencedCode(PropertyTrimmingWarningMessage)]
        private static object? BindParameter(ParameterInfo parameter, Type type, IConfiguration config,
            BinderOptions options)
        {
            string? parameterName = parameter.Name;

            if (parameterName is null)
            {
                throw new InvalidOperationException(SR.Format(SR.Error_ParameterBeingBoundToIsUnnamed, type));
            }

            var propertyBindingPoint = new BindingPoint(initialValue: config.GetSection(parameterName).Value, isReadOnly: false);

            if (propertyBindingPoint.Value is null)
            {
                if (ParameterDefaultValue.TryGetDefaultValue(parameter, out object? defaultValue))
                {
                    propertyBindingPoint.SetValue(defaultValue);
                }
                else
                {
                    throw new InvalidOperationException(SR.Format(SR.Error_ParameterHasNoMatchingConfig, type, parameterName));
                }
            }

            BindInstance(
                parameter.ParameterType,
                propertyBindingPoint,
                config.GetSection(parameterName),
                options);

            return propertyBindingPoint.Value;
        }

        private static string GetPropertyName(MemberInfo property)
        {
            ThrowHelper.ThrowIfNull(property);

            // Check for a custom property name used for configuration key binding
            foreach (var attributeData in property.GetCustomAttributesData())
            {
                if (attributeData.AttributeType != typeof(ConfigurationKeyNameAttribute))
                {
                    continue;
                }

                // Ensure ConfigurationKeyName constructor signature matches expectations
                if (attributeData.ConstructorArguments.Count != 1)
                {
                    break;
                }

                // Assumes ConfigurationKeyName constructor first arg is the string key name
                string? name = attributeData
                    .ConstructorArguments[0]
                    .Value?
                    .ToString();

                return !string.IsNullOrWhiteSpace(name) ? name : property.Name;
            }

            return property.Name;
        }
    }
}
