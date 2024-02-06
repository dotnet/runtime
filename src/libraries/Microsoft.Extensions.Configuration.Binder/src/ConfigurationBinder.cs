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
        private const string DynamicCodeWarningMessage = "Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.";
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
        [RequiresDynamicCode(DynamicCodeWarningMessage)]
        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        public static T? Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this IConfiguration configuration)
            => configuration.Get<T>(null);

        /// <summary>
        /// Attempts to bind the configuration instance to a new instance of type T.
        /// If this configuration section has a value, that will be used.
        /// Otherwise binding by matching property names against configuration keys recursively.
        /// </summary>
        /// <typeparam name="T">The type of the new instance to bind.</typeparam>
        /// <param name="configuration">The configuration instance to bind.</param>
        /// <param name="configureOptions">Configures the binder options.</param>
        /// <returns>The new instance of T if successful, default(T) otherwise.</returns>
        [RequiresDynamicCode(DynamicCodeWarningMessage)]
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
        [RequiresDynamicCode(DynamicCodeWarningMessage)]
        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        public static object? Get(this IConfiguration configuration, Type type)
            => configuration.Get(type, null);

        /// <summary>
        /// Attempts to bind the configuration instance to a new instance of type T.
        /// If this configuration section has a value, that will be used.
        /// Otherwise binding by matching property names against configuration keys recursively.
        /// </summary>
        /// <param name="configuration">The configuration instance to bind.</param>
        /// <param name="type">The type of the new instance to bind.</param>
        /// <param name="configureOptions">Configures the binder options.</param>
        /// <returns>The new instance if successful, null otherwise.</returns>
        [RequiresDynamicCode(DynamicCodeWarningMessage)]
        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        public static object? Get(
            this IConfiguration configuration,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            Type type,
            Action<BinderOptions>? configureOptions)
        {
            ThrowHelper.ThrowIfNull(configuration);
            ThrowHelper.ThrowIfNull(type);

            var options = new BinderOptions();
            configureOptions?.Invoke(options);
            var bindingPoint = new BindingPoint();
            BindInstance(type, bindingPoint, config: configuration, options: options, isParentCollection: false);
            return bindingPoint.Value;
        }

        /// <summary>
        /// Attempts to bind the given object instance to the configuration section specified by the key by matching property names against configuration keys recursively.
        /// </summary>
        /// <param name="configuration">The configuration instance to bind.</param>
        /// <param name="key">The key of the configuration section to bind.</param>
        /// <param name="instance">The object to bind.</param>
        [RequiresDynamicCode(DynamicCodeWarningMessage)]
        [RequiresUnreferencedCode(InstanceGetTypeTrimmingWarningMessage)]
        public static void Bind(this IConfiguration configuration, string key, object? instance)
        {
            ThrowHelper.ThrowIfNull(configuration);
            configuration.GetSection(key).Bind(instance);
        }

        /// <summary>
        /// Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.
        /// </summary>
        /// <param name="configuration">The configuration instance to bind.</param>
        /// <param name="instance">The object to bind.</param>
        [RequiresDynamicCode(DynamicCodeWarningMessage)]
        [RequiresUnreferencedCode(InstanceGetTypeTrimmingWarningMessage)]
        public static void Bind(this IConfiguration configuration, object? instance)
            => configuration.Bind(instance, null);

        /// <summary>
        /// Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.
        /// </summary>
        /// <param name="configuration">The configuration instance to bind.</param>
        /// <param name="instance">The object to bind.</param>
        /// <param name="configureOptions">Configures the binder options.</param>
        [RequiresDynamicCode(DynamicCodeWarningMessage)]
        [RequiresUnreferencedCode(InstanceGetTypeTrimmingWarningMessage)]
        public static void Bind(this IConfiguration configuration, object? instance, Action<BinderOptions>? configureOptions)
        {
            ThrowHelper.ThrowIfNull(configuration);

            if (instance != null)
            {
                var options = new BinderOptions();
                configureOptions?.Invoke(options);
                var bindingPoint = new BindingPoint(instance, isReadOnly: true);
                BindInstance(instance.GetType(), bindingPoint, configuration, options, false);
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
            ThrowHelper.ThrowIfNull(configuration);
            ThrowHelper.ThrowIfNull(type);

            IConfigurationSection section = configuration.GetSection(key);
            string? value = section.Value;
            if (value != null)
            {
                return ConvertValue(type, value, section.Path);
            }
            return defaultValue;
        }

        [RequiresDynamicCode(DynamicCodeWarningMessage)]
        [RequiresUnreferencedCode(PropertyTrimmingWarningMessage)]
        private static void BindProperties(object instance, IConfiguration configuration, BinderOptions options)
        {
            List<PropertyInfo> modelProperties = GetAllProperties(instance.GetType());

            if (options.ErrorOnUnknownConfiguration)
            {
                HashSet<string> propertyNames = new(modelProperties.Select(mp => mp.Name),
                    StringComparer.OrdinalIgnoreCase);

                List<string>? missingPropertyNames = null;
                foreach (IConfigurationSection cs in configuration.GetChildren())
                {
                    if (!propertyNames.Contains(cs.Key))
                    {
                        (missingPropertyNames ??= new()).Add($"'{cs.Key}'");
                    }
                }

                if (missingPropertyNames != null)
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

        [RequiresDynamicCode(DynamicCodeWarningMessage)]
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
                options,
                false);

            // For property binding, there are some cases when HasNewValue is not set in BindingPoint while a non-null Value inside that object can be retrieved from the property getter.
            // As example, when binding a property which not having a configuration entry matching this property and the getter can initialize the Value.
            // It is important to call the property setter as the setters can have a logic adjusting the Value.
            if (!propertyBindingPoint.IsReadOnly && propertyBindingPoint.Value is not null)
            {
                property.SetValue(instance, propertyBindingPoint.Value);
            }
        }

        [RequiresDynamicCode(DynamicCodeWarningMessage)]
        [RequiresUnreferencedCode(TrimmingWarningMessage)]
        private static void BindInstance(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
            BindingPoint bindingPoint,
            IConfiguration config,
            BinderOptions options,
            bool isParentCollection)
        {
            // if binding IConfigurationSection, break early
            if (type == typeof(IConfigurationSection))
            {
                bindingPoint.TrySetValue(config);
                return;
            }

            if (config is null)
            {
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

            if (config.GetChildren().Any())
            {
                // for arrays and read-only list-like interfaces, we concatenate on to what is already there, if we can
                if (type.IsArray || IsImmutableArrayCompatibleInterface(type))
                {
                    if (!bindingPoint.IsReadOnly)
                    {
                        bindingPoint.SetValue(BindArray(type, (IEnumerable?)bindingPoint.Value, config, options));
                    }

                    // for getter-only collection properties that we can't add to, nothing more we can do
                    return;
                }

                // -----------------------------------------------------------------------------------------------------------------------------
                //                  |  bindingPoint |  bindingPoint |
                //     Interface    |     Value     |   IsReadOnly  |  Behavior
                // -----------------------------------------------------------------------------------------------------------------------------
                //  ISet<T>         |   not null    |  true/false   | Use the Value instance to populate the configuration
                //  ISet<T>         |     null      |     false     | Create HashSet<T> instance to populate the configuration
                //  ISet<T>         |     null      |     true      | nothing
                //  IReadOnlySet<T> | null/not null |     false     | Create HashSet<T> instance, copy over existing values, and populate the configuration
                //  IReadOnlySet<T> | null/not null |     true      | nothing
                // -----------------------------------------------------------------------------------------------------------------------------
                if (TypeIsASetInterface(type))
                {
                    if (!bindingPoint.IsReadOnly || bindingPoint.Value is not null)
                    {
                        object? newValue = BindSet(type, (IEnumerable?)bindingPoint.Value, config, options);
                        if (!bindingPoint.IsReadOnly && newValue != null)
                        {
                            bindingPoint.SetValue(newValue);
                        }
                    }

                    return;
                }

                // -----------------------------------------------------------------------------------------------------------------------------
                //                         |  bindingPoint |  bindingPoint |
                //       Interface         |     Value     |   IsReadOnly  |  Behavior
                // -----------------------------------------------------------------------------------------------------------------------------
                //  IDictionary<T>         |   not null    |  true/false   | Use the Value instance to populate the configuration
                //  IDictionary<T>         |     null      |     false     | Create Dictionary<T> instance to populate the configuration
                //  IDictionary<T>         |     null      |     true      | nothing
                //  IReadOnlyDictionary<T> | null/not null |     false     | Create Dictionary<K,V> instance, copy over existing values, and populate the configuration
                //  IReadOnlyDictionary<T> | null/not null |     true      | nothing
                // -----------------------------------------------------------------------------------------------------------------------------
                if (TypeIsADictionaryInterface(type))
                {
                    if (!bindingPoint.IsReadOnly || bindingPoint.Value is not null)
                    {
                        object? newValue = BindDictionaryInterface(bindingPoint.Value, type, config, options);
                        if (!bindingPoint.IsReadOnly && newValue != null)
                        {
                            bindingPoint.SetValue(newValue);
                        }
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

                    Type? interfaceGenericType = type.IsInterface && type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : null;

                    if (interfaceGenericType is not null &&
                        (interfaceGenericType == typeof(ICollection<>) || interfaceGenericType == typeof(IList<>)))
                    {
                        // For ICollection<T> and IList<T> we bind them to mutable List<T> type.
                        Type genericType = typeof(List<>).MakeGenericType(type.GenericTypeArguments);
                        bindingPoint.SetValue(Activator.CreateInstance(genericType));
                    }
                    else
                    {
                        bindingPoint.SetValue(CreateInstance(type, config, options));
                    }
                }

                Debug.Assert(bindingPoint.Value is not null);

                // At this point we know that we have a non-null bindingPoint.Value, we just have to populate the items
                // using the IDictionary<> or ICollection<> interfaces, or properties using reflection.
                Type? dictionaryInterface = FindOpenGenericInterface(typeof(IDictionary<,>), type);

                if (dictionaryInterface != null)
                {
                    BindDictionary(bindingPoint.Value, dictionaryInterface, config, options);
                }
                else
                {
                    Type? collectionInterface = FindOpenGenericInterface(typeof(ICollection<>), type);
                    if (collectionInterface != null)
                    {
                        BindCollection(bindingPoint.Value, collectionInterface, config, options);
                    }
                    else
                    {
                        BindProperties(bindingPoint.Value, config, options);
                    }
                }
            }
            else
            {
                if (isParentCollection && bindingPoint.Value is null && string.IsNullOrEmpty(configValue))
                {
                    // If we don't have an instance, try to create one
                    bindingPoint.TrySetValue(CreateInstance(type, config, options));
                }
            }
        }

        [RequiresDynamicCode(DynamicCodeWarningMessage)]
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


                List<PropertyInfo> properties = GetAllProperties(type);

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
            List<PropertyInfo> properties, out string missing)
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

        [RequiresDynamicCode(DynamicCodeWarningMessage)]
        [RequiresUnreferencedCode("Cannot statically analyze what the element type is of the value objects in the dictionary so its members may be trimmed.")]
        private static object? BindDictionaryInterface(
            object? source,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            Type dictionaryType,
            IConfiguration config, BinderOptions options)
        {
            // IDictionary<K,V> is guaranteed to have exactly two parameters
            Type keyType = dictionaryType.GenericTypeArguments[0];
            Type valueType = dictionaryType.GenericTypeArguments[1];
            bool keyTypeIsEnum = keyType.IsEnum;
            bool keyTypeIsInteger =
                keyType == typeof(sbyte) ||
                keyType == typeof(byte) ||
                keyType == typeof(short) ||
                keyType == typeof(ushort) ||
                keyType == typeof(int) ||
                keyType == typeof(uint) ||
                keyType == typeof(long) ||
                keyType == typeof(ulong);

            if (keyType != typeof(string) && !keyTypeIsEnum && !keyTypeIsInteger)
            {
                // We only support string, enum and integer (except nint-IntPtr and nuint-UIntPtr) keys
                return null;
            }

            // addMethod can only be null if dictionaryType is IReadOnlyDictionary<TKey, TValue> rather than IDictionary<TKey, TValue>.
            MethodInfo? addMethod = dictionaryType.GetMethod("Add", DeclaredOnlyLookup);
            if (addMethod is null || source is null)
            {
                dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                object? dictionary = Activator.CreateInstance(dictionaryType);
                addMethod = dictionaryType.GetMethod("Add", DeclaredOnlyLookup);

                var orig = source as IEnumerable;
                if (orig is not null)
                {
                    Type kvpType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
                    PropertyInfo keyMethod = kvpType.GetProperty("Key", DeclaredOnlyLookup)!;
                    PropertyInfo valueMethod = kvpType.GetProperty("Value", DeclaredOnlyLookup)!;
                    object?[] arguments = new object?[2];

                    foreach (object? item in orig)
                    {
                        object? k = keyMethod.GetMethod!.Invoke(item, null);
                        object? v = valueMethod.GetMethod!.Invoke(item, null);
                        arguments[0] = k;
                        arguments[1] = v;
                        addMethod!.Invoke(dictionary, arguments);
                    }
                }

                source = dictionary;
            }

            Debug.Assert(source is not null);
            Debug.Assert(addMethod is not null);

            BindDictionary(source, dictionaryType, config, options);

            return source;
        }

        // Binds and potentially overwrites a dictionary object.
        // This differs from BindDictionaryInterface because this method doesn't clone
        // the dictionary; it sets and/or overwrites values directly.
        // When a user specifies a concrete dictionary or a concrete class implementing IDictionary<,>
        // in their config class, then that value is used as-is. When a user specifies an interface (instantiated)
        // in their config class, then it is cloned to a new dictionary, the same way as other collections.
        [RequiresDynamicCode(DynamicCodeWarningMessage)]
        [RequiresUnreferencedCode("Cannot statically analyze what the element type is of the value objects in the dictionary so its members may be trimmed.")]
        private static void BindDictionary(
            object dictionary,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            Type dictionaryType,
            IConfiguration config, BinderOptions options)
        {
            Debug.Assert(dictionaryType.IsGenericType &&
                         (dictionaryType.GetGenericTypeDefinition() == typeof(IDictionary<,>) || dictionaryType.GetGenericTypeDefinition() == typeof(Dictionary<,>)));

            Type keyType = dictionaryType.GenericTypeArguments[0];
            Type valueType = dictionaryType.GenericTypeArguments[1];
            bool keyTypeIsEnum = keyType.IsEnum;
            bool keyTypeIsInteger =
                keyType == typeof(sbyte) ||
                keyType == typeof(byte) ||
                keyType == typeof(short) ||
                keyType == typeof(ushort) ||
                keyType == typeof(int) ||
                keyType == typeof(uint) ||
                keyType == typeof(long) ||
                keyType == typeof(ulong);

            if (keyType != typeof(string) && !keyTypeIsEnum && !keyTypeIsInteger)
            {
                // We only support string, enum and integer (except nint-IntPtr and nuint-UIntPtr) keys
                return;
            }

            MethodInfo tryGetValue = dictionaryType.GetMethod("TryGetValue", DeclaredOnlyLookup)!;
            PropertyInfo indexerProperty = dictionaryType.GetProperty("Item", DeclaredOnlyLookup)!;

            foreach (IConfigurationSection child in config.GetChildren())
            {
                try
                {
                    object key = keyTypeIsEnum ? Enum.Parse(keyType, child.Key, true) :
                        keyTypeIsInteger ? Convert.ChangeType(child.Key, keyType) :
                        child.Key;

                    var valueBindingPoint = new BindingPoint(
                        initialValueProvider: () =>
                        {
                            object?[] tryGetValueArgs = { key, null };
                            return (bool)tryGetValue.Invoke(dictionary, tryGetValueArgs)! ? tryGetValueArgs[1] : null;
                        },
                        isReadOnly: false);
                    BindInstance(
                        type: valueType,
                        bindingPoint: valueBindingPoint,
                        config: child,
                        options: options,
                        true);
                    if (valueBindingPoint.HasNewValue)
                    {
                        indexerProperty.SetValue(dictionary, valueBindingPoint.Value, new object[] { key });
                    }
                }
                catch (Exception ex)
                {
                    if (options.ErrorOnUnknownConfiguration)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Error_GeneralErrorWhenBinding,
                            nameof(options.ErrorOnUnknownConfiguration)), ex);
                    }
                }
            }
        }

        [RequiresDynamicCode(DynamicCodeWarningMessage)]
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
                        options: options,
                        true);
                    if (itemBindingPoint.HasNewValue)
                    {
                        addMethod?.Invoke(collection, new[] { itemBindingPoint.Value });
                    }
                }
                catch (Exception ex)
                {
                    if (options.ErrorOnUnknownConfiguration)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Error_GeneralErrorWhenBinding,
                            nameof(options.ErrorOnUnknownConfiguration)), ex);
                    }

                }
            }
        }

        [RequiresDynamicCode(DynamicCodeWarningMessage)]
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

            var list = new List<object?>();

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
                        options: options,
                        isParentCollection: true);
                    if (itemBindingPoint.HasNewValue)
                    {
                        list.Add(itemBindingPoint.Value);
                    }
                }
                catch (Exception ex)
                {
                    if (options.ErrorOnUnknownConfiguration)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Error_GeneralErrorWhenBinding,
                            nameof(options.ErrorOnUnknownConfiguration)), ex);
                    }
                }
            }

            Array result = Array.CreateInstance(elementType, list.Count);
            ((IList)list).CopyTo(result, 0);
            return result;
        }

        [RequiresDynamicCode(DynamicCodeWarningMessage)]
        [RequiresUnreferencedCode("Cannot statically analyze what the element type is of the Array so its members may be trimmed.")]
        private static object? BindSet(Type type, IEnumerable? source, IConfiguration config, BinderOptions options)
        {
            Type elementType = type.GetGenericArguments()[0];

            bool elementTypeIsEnum = elementType.IsEnum;

            if (elementType != typeof(string) && !elementTypeIsEnum)
            {
                // We only support string and enum keys
                return null;
            }

            object?[] arguments = new object?[1];
            // addMethod can only be null if type is IReadOnlySet<T> rather than ISet<T>.
            MethodInfo? addMethod = type.GetMethod("Add", DeclaredOnlyLookup);
            if (addMethod is null || source is null)
            {
                Type genericType = typeof(HashSet<>).MakeGenericType(elementType);
                object instance = Activator.CreateInstance(genericType)!;
                addMethod = genericType.GetMethod("Add", DeclaredOnlyLookup);

                if (source != null)
                {
                    foreach (object? item in source)
                    {
                        arguments[0] = item;
                        addMethod!.Invoke(instance, arguments);
                    }
                }

                source = (IEnumerable)instance;
            }

            Debug.Assert(source is not null);
            Debug.Assert(addMethod is not null);

            foreach (IConfigurationSection section in config.GetChildren())
            {
                var itemBindingPoint = new BindingPoint();
                try
                {
                    BindInstance(
                        type: elementType,
                        bindingPoint: itemBindingPoint,
                        config: section,
                        options: options,
                        true);
                    if (itemBindingPoint.HasNewValue)
                    {
                        arguments[0] = itemBindingPoint.Value;

                        addMethod.Invoke(source, arguments);
                    }
                }
                catch (Exception ex)
                {
                    if (options.ErrorOnUnknownConfiguration)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Error_GeneralErrorWhenBinding,
                            nameof(options.ErrorOnUnknownConfiguration)), ex);
                    }
                }
            }

            return source;
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

        private static bool TypeIsADictionaryInterface(Type type)
        {
            if (!type.IsInterface || !type.IsConstructedGenericType) { return false; }

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            return genericTypeDefinition == typeof(IDictionary<,>)
                || genericTypeDefinition == typeof(IReadOnlyDictionary<,>);
        }

        private static bool IsImmutableArrayCompatibleInterface(Type type)
        {
            if (!type.IsInterface || !type.IsConstructedGenericType) { return false; }

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            return genericTypeDefinition == typeof(IEnumerable<>)
                || genericTypeDefinition == typeof(IReadOnlyCollection<>)
                || genericTypeDefinition == typeof(IReadOnlyList<>);
        }

        private static bool TypeIsASetInterface(Type type)
        {
            if (!type.IsInterface || !type.IsConstructedGenericType) { return false; }

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            return genericTypeDefinition == typeof(ISet<>)
#if NETCOREAPP
                   || genericTypeDefinition == typeof(IReadOnlySet<>)
#endif
                   ;
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

        private static List<PropertyInfo> GetAllProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
        {
            var allProperties = new List<PropertyInfo>();

            Type baseType = type;
            while (baseType != typeof(object))
            {
                PropertyInfo[] properties = baseType.GetProperties(DeclaredOnlyLookup);

                foreach (PropertyInfo property in properties)
                {
                    // if the property is virtual, only add the base-most definition so
                    // overridden properties aren't duplicated in the list.
                    MethodInfo? setMethod = property.GetSetMethod(true);

                    if (setMethod is null || !setMethod.IsVirtual || setMethod == setMethod.GetBaseDefinition())
                    {
                        allProperties.Add(property);
                    }
                }

                baseType = baseType.BaseType!;
            }

            return allProperties;
        }

        [RequiresDynamicCode(DynamicCodeWarningMessage)]
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

            BindInstance(
                parameter.ParameterType,
                propertyBindingPoint,
                config.GetSection(parameterName),
                options,
                false);

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

            return propertyBindingPoint.Value;
        }

        private static string GetPropertyName(PropertyInfo property)
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
