// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Helper code for the various activator services.
    /// </summary>
    public static class ActivatorUtilities
    {
#if NET8_0_OR_GREATER
        // Maximum number of fixed arguments for ConstructorInvoker.Invoke(arg1, etc).
        private const int FixedArgumentThreshold = 4;
#endif

        private static readonly MethodInfo GetServiceInfo =
            GetMethodInfo<Func<IServiceProvider, Type, Type, bool, object?>>((sp, t, r, c) => GetService(sp, t, r, c));

        /// <summary>
        /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <param name="instanceType">The type to activate</param>
        /// <param name="parameters">Constructor arguments not provided by the <paramref name="provider"/>.</param>
        /// <returns>An activated object of type instanceType</returns>
        public static object CreateInstance(
            IServiceProvider provider,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            params object[] parameters)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (instanceType.IsAbstract)
            {
                throw new InvalidOperationException(SR.CannotCreateAbstractClasses);
            }

            IServiceProviderIsService? serviceProviderIsService = provider.GetService<IServiceProviderIsService>();
            // if container supports using IServiceProviderIsService, we try to find the longest ctor that
            // (a) matches all parameters given to CreateInstance
            // (b) matches the rest of ctor arguments as either a parameter with a default value or as a service registered
            // if no such match is found we fallback to the same logic used by CreateFactory which would only allow creating an
            // instance if all parameters given to CreateInstance only match with a single ctor
            if (serviceProviderIsService != null)
            {
                int bestLength = -1;
                bool seenPreferred = false;

                ConstructorMatcher bestMatcher = default;
                bool multipleBestLengthFound = false;

                foreach (ConstructorInfo? constructor in instanceType.GetConstructors())
                {
                    var matcher = new ConstructorMatcher(constructor);
                    bool isPreferred = constructor.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute), false);
                    int length = matcher.Match(parameters, serviceProviderIsService);

                    if (isPreferred)
                    {
                        if (seenPreferred)
                        {
                            ThrowMultipleCtorsMarkedWithAttributeException();
                        }

                        if (length == -1)
                        {
                            ThrowMarkedCtorDoesNotTakeAllProvidedArguments();
                        }
                    }

                    if (isPreferred || bestLength < length)
                    {
                        bestLength = length;
                        bestMatcher = matcher;
                        multipleBestLengthFound = false;
                    }
                    else if (bestLength == length)
                    {
                        multipleBestLengthFound = true;
                    }

                    seenPreferred |= isPreferred;
                }

                if (bestLength != -1)
                {
                    if (multipleBestLengthFound)
                    {
                        throw new InvalidOperationException(SR.Format(SR.MultipleCtorsFoundWithBestLength, instanceType, bestLength));
                    }

                    return bestMatcher.CreateInstance(provider);
                }
            }

            Type?[] argumentTypes = new Type[parameters.Length];
            for (int i = 0; i < argumentTypes.Length; i++)
            {
                argumentTypes[i] = parameters[i]?.GetType();
            }

            FindApplicableConstructor(instanceType, argumentTypes, out ConstructorInfo constructorInfo, out int?[] parameterMap);
            var constructorMatcher = new ConstructorMatcher(constructorInfo);
            constructorMatcher.MapParameters(parameterMap, parameters);
            return constructorMatcher.CreateInstance(provider);
        }

        /// <summary>
        /// Create a delegate that will instantiate a type with constructor arguments provided directly
        /// and/or from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="instanceType">The type to activate</param>
        /// <param name="argumentTypes">
        /// The types of objects, in order, that will be passed to the returned function as its second parameter
        /// </param>
        /// <returns>
        /// A factory that will instantiate instanceType using an <see cref="IServiceProvider"/>
        /// and an argument array containing objects matching the types defined in argumentTypes
        /// </returns>
        public static ObjectFactory CreateFactory(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type[] argumentTypes)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                // Create a reflection-based factory when dynamic code is not compiled\jitted as would be the case with
                // NativeAOT, iOS or WASM.
                // For NativeAOT and iOS, using the reflection-based factory is faster than reflection-fallback interpreted
                // expressions and also doesn't pull in the large System.Linq.Expressions dependency.
                // For WASM, although it has the ability to use expressions (with dynamic code) and interpet the dynamic code
                // efficiently, the size savings of not using System.Linq.Expressions is more important than CPU perf.
                return CreateFactoryReflection(instanceType, argumentTypes);
            }
#endif
            CreateFactoryInternal(instanceType, argumentTypes, out ParameterExpression provider, out ParameterExpression argumentArray, out Expression factoryExpressionBody);

            var factoryLambda = Expression.Lambda<Func<IServiceProvider, object?[]?, object>>(
                factoryExpressionBody, provider, argumentArray);

            Func<IServiceProvider, object?[]?, object>? result = factoryLambda.Compile();
            return result.Invoke;
        }

        /// <summary>
        /// Create a delegate that will instantiate a type with constructor arguments provided directly
        /// and/or from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type to activate</typeparam>
        /// <param name="argumentTypes">
        /// The types of objects, in order, that will be passed to the returned function as its second parameter
        /// </param>
        /// <returns>
        /// A factory that will instantiate type T using an <see cref="IServiceProvider"/>
        /// and an argument array containing objects matching the types defined in argumentTypes
        /// </returns>
        public static ObjectFactory<T>
            CreateFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
                Type[] argumentTypes)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                // See the comment above in the non-generic CreateFactory() for why we use 'IsDynamicCodeCompiled' here.
                var factory = CreateFactoryReflection(typeof(T), argumentTypes);
                return (serviceProvider, arguments) => (T)factory(serviceProvider, arguments);
            }
#endif
            CreateFactoryInternal(typeof(T), argumentTypes, out ParameterExpression provider, out ParameterExpression argumentArray, out Expression factoryExpressionBody);

            var factoryLambda = Expression.Lambda<Func<IServiceProvider, object?[]?, T>>(
                factoryExpressionBody, provider, argumentArray);

            Func<IServiceProvider, object?[]?, T>? result = factoryLambda.Compile();
            return result.Invoke;
        }

        private static void CreateFactoryInternal([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType, Type[] argumentTypes, out ParameterExpression provider, out ParameterExpression argumentArray, out Expression factoryExpressionBody)
        {
            FindApplicableConstructor(instanceType, argumentTypes, out ConstructorInfo constructor, out int?[] parameterMap);

            provider = Expression.Parameter(typeof(IServiceProvider), "provider");
            argumentArray = Expression.Parameter(typeof(object[]), "argumentArray");
            factoryExpressionBody = BuildFactoryExpression(constructor, parameterMap, provider, argumentArray);
        }

        /// <summary>
        /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type to activate</typeparam>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <param name="parameters">Constructor arguments not provided by the <paramref name="provider"/>.</param>
        /// <returns>An activated object of type T</returns>
        public static T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(IServiceProvider provider, params object[] parameters)
        {
            return (T)CreateInstance(provider, typeof(T), parameters);
        }

        /// <summary>
        /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
        /// </summary>
        /// <typeparam name="T">The type of the service</typeparam>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <returns>The resolved service or created instance</returns>
        public static T GetServiceOrCreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(IServiceProvider provider)
        {
            return (T)GetServiceOrCreateInstance(provider, typeof(T));
        }

        /// <summary>
        /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
        /// </summary>
        /// <param name="provider">The service provider</param>
        /// <param name="type">The type of the service</param>
        /// <returns>The resolved service or created instance</returns>
        public static object GetServiceOrCreateInstance(
            IServiceProvider provider,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            return provider.GetService(type) ?? CreateInstance(provider, type);
        }

        private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
        {
            var mc = (MethodCallExpression)expr.Body;
            return mc.Method;
        }

        private static object? GetService(IServiceProvider sp, Type type, Type requiredBy, bool hasDefaultValue)
        {
            object? service = sp.GetService(type);
            if (service is null && !hasDefaultValue)
            {
                ThrowHelperUnableToResolveService(type, requiredBy);
            }
            return service;
        }

        [DoesNotReturn]
        private static void ThrowHelperUnableToResolveService(Type type, Type requiredBy)
        {
            throw new InvalidOperationException(SR.Format(SR.UnableToResolveService, type, requiredBy));
        }

        private static BlockExpression BuildFactoryExpression(
            ConstructorInfo constructor,
            int?[] parameterMap,
            Expression serviceProvider,
            Expression factoryArgumentArray)
        {
            ParameterInfo[]? constructorParameters = constructor.GetParameters();
            var constructorArguments = new Expression[constructorParameters.Length];

            for (int i = 0; i < constructorParameters.Length; i++)
            {
                ParameterInfo? constructorParameter = constructorParameters[i];
                Type? parameterType = constructorParameter.ParameterType;
                bool hasDefaultValue = ParameterDefaultValue.TryGetDefaultValue(constructorParameter, out object? defaultValue);

                if (parameterMap[i] != null)
                {
                    constructorArguments[i] = Expression.ArrayAccess(factoryArgumentArray, Expression.Constant(parameterMap[i]));
                }
                else
                {
                    var parameterTypeExpression = new Expression[] { serviceProvider,
                        Expression.Constant(parameterType, typeof(Type)),
                        Expression.Constant(constructor.DeclaringType, typeof(Type)),
                        Expression.Constant(hasDefaultValue) };
                    constructorArguments[i] = Expression.Call(GetServiceInfo, parameterTypeExpression);
                }

                // Support optional constructor arguments by passing in the default value
                // when the argument would otherwise be null.
                if (hasDefaultValue)
                {
                    ConstantExpression? defaultValueExpression = Expression.Constant(defaultValue);
                    constructorArguments[i] = Expression.Coalesce(constructorArguments[i], defaultValueExpression);
                }

                constructorArguments[i] = Expression.Convert(constructorArguments[i], parameterType);
            }

            return Expression.Block(Expression.IfThen(Expression.Equal(serviceProvider, Expression.Constant(null)), Expression.Throw(Expression.Constant(new ArgumentNullException(nameof(serviceProvider))))),
                Expression.New(constructor, constructorArguments));
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        [DoesNotReturn]
        private static void ThrowHelperArgumentNullExceptionServiceProvider()
        {
            throw new ArgumentNullException("serviceProvider");
        }

        private static ObjectFactory CreateFactoryReflection(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type?[] argumentTypes)
        {
            FindApplicableConstructor(instanceType, argumentTypes, out ConstructorInfo constructor, out int?[] parameterMap);
            Type declaringType = constructor.DeclaringType!;

#if NET8_0_OR_GREATER
            ConstructorInvoker invoker = ConstructorInvoker.Create(constructor);

            ParameterInfo[] constructorParameters = constructor.GetParameters();
            if (constructorParameters.Length == 0)
            {
                return (IServiceProvider serviceProvider, object?[]? arguments) =>
                    invoker.Invoke();
            }

            // Gather some metrics to determine what fast path to take, if any.
            bool useFixedValues = constructorParameters.Length <= FixedArgumentThreshold;
            bool hasAnyDefaultValues = false;
            int matchedArgCount = 0;
            int matchedArgCountWithMap = 0;
            for (int i = 0; i < constructorParameters.Length; i++)
            {
                hasAnyDefaultValues |= constructorParameters[i].HasDefaultValue;

                if (parameterMap[i] is not null)
                {
                    matchedArgCount++;
                    if (parameterMap[i] == i)
                    {
                        matchedArgCountWithMap++;
                    }
                }
            }

            // No fast path; contains default values or arg mapping.
            if (hasAnyDefaultValues || matchedArgCount != matchedArgCountWithMap)
            {
                return InvokeCanonical();
            }

            if (matchedArgCount == 0)
            {
                // All injected; use a fast path.
                Type[] types = GetParameterTypes();
                return useFixedValues ?
                    (serviceProvider, arguments) => ReflectionFactoryServiceOnlyFixed(invoker, types, declaringType, serviceProvider) :
                    (serviceProvider, arguments) => ReflectionFactoryServiceOnlySpan(invoker, types, declaringType, serviceProvider);
            }

            if (matchedArgCount == constructorParameters.Length)
            {
                // All direct with no mappings; use a fast path.
                return (serviceProvider, arguments) => ReflectionFactoryDirect(invoker, serviceProvider, arguments);
            }

            return InvokeCanonical();

            ObjectFactory InvokeCanonical()
            {
                FactoryParameterContext[] parameters = GetFactoryParameterContext();
                return useFixedValues ?
                    (serviceProvider, arguments) => ReflectionFactoryCanonicalFixed(invoker, parameters, declaringType, serviceProvider, arguments) :
                    (serviceProvider, arguments) => ReflectionFactoryCanonicalSpan(invoker, parameters, declaringType, serviceProvider, arguments);
            }

            Type[] GetParameterTypes()
            {
                Type[] types = new Type[constructorParameters.Length];
                for (int i = 0; i < constructorParameters.Length; i++)
                {
                    types[i] = constructorParameters[i].ParameterType;
                }
                return types;
            }
#else
            ParameterInfo[] constructorParameters = constructor.GetParameters();
            if (constructorParameters.Length == 0)
            {
                return (IServiceProvider serviceProvider, object?[]? arguments) =>
                    constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: null, culture: null);
            }

            FactoryParameterContext[] parameters = GetFactoryParameterContext();
            return (serviceProvider, arguments) => ReflectionFactoryCanonical(constructor, parameters, declaringType, serviceProvider, arguments);
#endif // NET8_0_OR_GREATER

            FactoryParameterContext[] GetFactoryParameterContext()
            {
                FactoryParameterContext[] parameters = new FactoryParameterContext[constructorParameters.Length];
                for (int i = 0; i < constructorParameters.Length; i++)
                {
                    ParameterInfo constructorParameter = constructorParameters[i];
                    bool hasDefaultValue = ParameterDefaultValue.TryGetDefaultValue(constructorParameter, out object? defaultValue);
                    parameters[i] = new FactoryParameterContext(constructorParameter.ParameterType, hasDefaultValue, defaultValue, parameterMap[i] ?? -1);
                }

                return parameters;
            }
        }
#endif // NETSTANDARD2_1_OR_GREATER || NETCOREAPP

        private readonly struct FactoryParameterContext
        {
            public FactoryParameterContext(Type parameterType, bool hasDefaultValue, object? defaultValue, int argumentIndex)
            {
                ParameterType = parameterType;
                HasDefaultValue = hasDefaultValue;
                DefaultValue = defaultValue;
                ArgumentIndex = argumentIndex;
            }

            public Type ParameterType { get; }
            public bool HasDefaultValue { get; }
            public object? DefaultValue { get; }
            public int ArgumentIndex { get; }
        }

        private static void FindApplicableConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type?[] argumentTypes,
            out ConstructorInfo matchingConstructor,
            out int?[] matchingParameterMap)
        {
            ConstructorInfo? constructorInfo;
            int?[]? parameterMap;

            if (!TryFindPreferredConstructor(instanceType, argumentTypes, out constructorInfo, out parameterMap) &&
                !TryFindMatchingConstructor(instanceType, argumentTypes, out constructorInfo, out parameterMap))
            {
                throw new InvalidOperationException(SR.Format(SR.CtorNotLocated, instanceType));
            }

            matchingConstructor = constructorInfo;
            matchingParameterMap = parameterMap;
        }

        // Tries to find constructor based on provided argument types
        private static bool TryFindMatchingConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type?[] argumentTypes,
            [NotNullWhen(true)] out ConstructorInfo? matchingConstructor,
            [NotNullWhen(true)] out int?[]? parameterMap)
        {
            matchingConstructor = null;
            parameterMap = null;

            foreach (ConstructorInfo? constructor in instanceType.GetConstructors())
            {
                if (TryCreateParameterMap(constructor.GetParameters(), argumentTypes, out int?[] tempParameterMap))
                {
                    if (matchingConstructor != null)
                    {
                        throw new InvalidOperationException(SR.Format(SR.MultipleCtorsFound, instanceType));
                    }

                    matchingConstructor = constructor;
                    parameterMap = tempParameterMap;
                }
            }

            if (matchingConstructor != null)
            {
                Debug.Assert(parameterMap != null);
                return true;
            }

            return false;
        }

        // Tries to find constructor marked with ActivatorUtilitiesConstructorAttribute
        private static bool TryFindPreferredConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type?[] argumentTypes,
            [NotNullWhen(true)] out ConstructorInfo? matchingConstructor,
            [NotNullWhen(true)] out int?[]? parameterMap)
        {
            bool seenPreferred = false;
            matchingConstructor = null;
            parameterMap = null;

            foreach (ConstructorInfo? constructor in instanceType.GetConstructors())
            {
                if (constructor.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute), false))
                {
                    if (seenPreferred)
                    {
                        ThrowMultipleCtorsMarkedWithAttributeException();
                    }

                    if (!TryCreateParameterMap(constructor.GetParameters(), argumentTypes, out int?[] tempParameterMap))
                    {
                        ThrowMarkedCtorDoesNotTakeAllProvidedArguments();
                    }

                    matchingConstructor = constructor;
                    parameterMap = tempParameterMap;
                    seenPreferred = true;
                }
            }

            if (matchingConstructor != null)
            {
                Debug.Assert(parameterMap != null);
                return true;
            }

            return false;
        }

        // Creates an injective parameterMap from givenParameterTypes to assignable constructorParameters.
        // Returns true if each given parameter type is assignable to a unique; otherwise, false.
        private static bool TryCreateParameterMap(ParameterInfo[] constructorParameters, Type?[] argumentTypes, out int?[] parameterMap)
        {
            parameterMap = new int?[constructorParameters.Length];

            for (int i = 0; i < argumentTypes.Length; i++)
            {
                bool foundMatch = false;
                Type? givenParameter = argumentTypes[i];

                for (int j = 0; j < constructorParameters.Length; j++)
                {
                    if (parameterMap[j] != null)
                    {
                        // This ctor parameter has already been matched
                        continue;
                    }

                    if (constructorParameters[j].ParameterType.IsAssignableFrom(givenParameter))
                    {
                        foundMatch = true;
                        parameterMap[j] = i;
                        break;
                    }
                }

                if (!foundMatch)
                {
                    return false;
                }
            }

            return true;
        }

        private static object? GetService(IServiceProvider serviceProvider, ParameterInfo parameterInfo)
        {
            // Handle keyed service
            if (TryGetServiceKey(parameterInfo, out object? key))
            {
                if (serviceProvider is IKeyedServiceProvider keyedServiceProvider)
                {
                    return keyedServiceProvider.GetKeyedService(parameterInfo.ParameterType, key);
                }
                throw new InvalidOperationException(SR.KeyedServicesNotSupported);
            }
            // Try non keyed service
            return serviceProvider.GetService(parameterInfo.ParameterType);
        }

        private static bool IsService(IServiceProviderIsService serviceProviderIsService, ParameterInfo parameterInfo)
        {
            // Handle keyed service
            if (TryGetServiceKey(parameterInfo, out object? key))
            {
                if (serviceProviderIsService is IServiceProviderIsKeyedService serviceProviderIsKeyedService)
                {
                    return serviceProviderIsKeyedService.IsKeyedService(parameterInfo.ParameterType, key);
                }
                throw new InvalidOperationException(SR.KeyedServicesNotSupported);
            }
            // Try non keyed service
            return serviceProviderIsService.IsService(parameterInfo.ParameterType);
        }

        private static bool TryGetServiceKey(ParameterInfo parameterInfo, out object? key)
        {
            foreach (var attribute in parameterInfo.GetCustomAttributes<FromKeyedServicesAttribute>(false))
            {
                key = attribute.Key;
                return true;
            }
            key = null;
            return false;
        }

        private readonly struct ConstructorMatcher
        {
            private readonly ConstructorInfo _constructor;
            private readonly ParameterInfo[] _parameters;
            private readonly object?[] _parameterValues;

            public ConstructorMatcher(ConstructorInfo constructor)
            {
                _constructor = constructor;
                _parameters = _constructor.GetParameters();
                _parameterValues = new object?[_parameters.Length];
            }

            public int Match(object[] givenParameters, IServiceProviderIsService serviceProviderIsService)
            {
                for (int givenIndex = 0; givenIndex < givenParameters.Length; givenIndex++)
                {
                    Type? givenType = givenParameters[givenIndex]?.GetType();
                    bool givenMatched = false;

                    for (int applyIndex = 0; applyIndex < _parameters.Length; applyIndex++)
                    {
                        if (_parameterValues[applyIndex] == null &&
                            _parameters[applyIndex].ParameterType.IsAssignableFrom(givenType))
                        {
                            givenMatched = true;
                            _parameterValues[applyIndex] = givenParameters[givenIndex];
                            break;
                        }
                    }

                    if (!givenMatched)
                    {
                        return -1;
                    }
                }

                // confirms the rest of ctor arguments match either as a parameter with a default value or as a service registered
                for (int i = 0; i < _parameters.Length; i++)
                {
                    if (_parameterValues[i] == null &&
                        !IsService(serviceProviderIsService, _parameters[i]))
                    {
                        if (ParameterDefaultValue.TryGetDefaultValue(_parameters[i], out object? defaultValue))
                        {
                            _parameterValues[i] = defaultValue;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                }

                return _parameters.Length;
            }

            public object CreateInstance(IServiceProvider provider)
            {
                for (int index = 0; index < _parameters.Length; index++)
                {
                    if (_parameterValues[index] == null)
                    {
                        object? value = GetService(provider, _parameters[index]);
                        if (value == null)
                        {
                            if (!ParameterDefaultValue.TryGetDefaultValue(_parameters[index], out object? defaultValue))
                            {
                                throw new InvalidOperationException(SR.Format(SR.UnableToResolveService, _parameters[index].ParameterType, _constructor.DeclaringType));
                            }
                            else
                            {
                                _parameterValues[index] = defaultValue;
                            }
                        }
                        else
                        {
                            _parameterValues[index] = value;
                        }
                    }
                }

#if NETFRAMEWORK || NETSTANDARD2_0
                try
                {
                    return _constructor.Invoke(_parameterValues);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    // The above line will always throw, but the compiler requires we throw explicitly.
                    throw;
                }
#else
                return _constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: _parameterValues, culture: null);
#endif
            }

            public void MapParameters(int?[] parameterMap, object[] givenParameters)
            {
                for (int i = 0; i < _parameters.Length; i++)
                {
                    if (parameterMap[i] != null)
                    {
                        _parameterValues[i] = givenParameters[(int)parameterMap[i]!];
                    }
                }
            }
        }

        private static void ThrowMultipleCtorsMarkedWithAttributeException()
        {
            throw new InvalidOperationException(SR.Format(SR.MultipleCtorsMarkedWithAttribute, nameof(ActivatorUtilitiesConstructorAttribute)));
        }

        private static void ThrowMarkedCtorDoesNotTakeAllProvidedArguments()
        {
            throw new InvalidOperationException(SR.Format(SR.MarkedCtorMissingArgumentTypes, nameof(ActivatorUtilitiesConstructorAttribute)));
        }

#if NET8_0_OR_GREATER // Use the faster ConstructorInvoker which also has alloc-free APIs when <= 4 parameters.
        private static object ReflectionFactoryServiceOnlyFixed(
            ConstructorInvoker invoker,
            Type[] parameterTypes,
            Type declaringType,
            IServiceProvider serviceProvider)
        {
            Debug.Assert(parameterTypes.Length >= 1 && parameterTypes.Length <= FixedArgumentThreshold);
            Debug.Assert(FixedArgumentThreshold == 4);

            if (serviceProvider is null)
                ThrowHelperArgumentNullExceptionServiceProvider();

            switch (parameterTypes.Length)
            {
                case 1:
                    return invoker.Invoke(
                        GetService(serviceProvider, parameterTypes[0], declaringType, false));

                case 2:
                    return invoker.Invoke(
                        GetService(serviceProvider, parameterTypes[0], declaringType, false),
                        GetService(serviceProvider, parameterTypes[1], declaringType, false));

                case 3:
                    return invoker.Invoke(
                        GetService(serviceProvider, parameterTypes[0], declaringType, false),
                        GetService(serviceProvider, parameterTypes[1], declaringType, false),
                        GetService(serviceProvider, parameterTypes[2], declaringType, false));

                case 4:
                    return invoker.Invoke(
                        GetService(serviceProvider, parameterTypes[0], declaringType, false),
                        GetService(serviceProvider, parameterTypes[1], declaringType, false),
                        GetService(serviceProvider, parameterTypes[2], declaringType, false),
                        GetService(serviceProvider, parameterTypes[3], declaringType, false));
            }

            return null!;
        }

        private static object ReflectionFactoryServiceOnlySpan(
            ConstructorInvoker invoker,
            Type[] parameterTypes,
            Type declaringType,
            IServiceProvider serviceProvider)
        {
            if (serviceProvider is null)
                ThrowHelperArgumentNullExceptionServiceProvider();

            object?[] arguments = new object?[parameterTypes.Length];
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                arguments[i] = GetService(serviceProvider, parameterTypes[i], declaringType, false);
            }

            return invoker.Invoke(arguments.AsSpan());
        }

        private static object ReflectionFactoryCanonicalFixed(
            ConstructorInvoker invoker,
            FactoryParameterContext[] parameters,
            Type declaringType,
            IServiceProvider serviceProvider,
            object?[]? arguments)
        {
            Debug.Assert(parameters.Length >= 1 && parameters.Length <= FixedArgumentThreshold);
            Debug.Assert(FixedArgumentThreshold == 4);

            if (serviceProvider is null)
                ThrowHelperArgumentNullExceptionServiceProvider();

            ref FactoryParameterContext parameter1 = ref parameters[0];

            switch (parameters.Length)
            {
                case 1:
                    return invoker.Invoke(
                         ((parameter1.ArgumentIndex != -1)
                            // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                            ? arguments![parameter1.ArgumentIndex]
                            : GetService(
                                serviceProvider,
                                parameter1.ParameterType,
                                declaringType,
                                parameter1.HasDefaultValue)) ?? parameter1.DefaultValue);
                case 2:
                    {
                        ref FactoryParameterContext parameter2 = ref parameters[1];

                        return invoker.Invoke(
                             ((parameter1.ArgumentIndex != -1)
                                // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                                ? arguments![parameter1.ArgumentIndex]
                                : GetService(
                                    serviceProvider,
                                    parameter1.ParameterType,
                                    declaringType,
                                    parameter1.HasDefaultValue)) ?? parameter1.DefaultValue,
                             ((parameter2.ArgumentIndex != -1)
                                // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                                ? arguments![parameter2.ArgumentIndex]
                                : GetService(
                                    serviceProvider,
                                    parameter2.ParameterType,
                                    declaringType,
                                    parameter2.HasDefaultValue)) ?? parameter2.DefaultValue);
                    }
                case 3:
                    {
                        ref FactoryParameterContext parameter2 = ref parameters[1];
                        ref FactoryParameterContext parameter3 = ref parameters[2];

                        return invoker.Invoke(
                             ((parameter1.ArgumentIndex != -1)
                                // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                                ? arguments![parameter1.ArgumentIndex]
                                : GetService(
                                    serviceProvider,
                                    parameter1.ParameterType,
                                    declaringType,
                                    parameter1.HasDefaultValue)) ?? parameter1.DefaultValue,
                             ((parameter2.ArgumentIndex != -1)
                                // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                                ? arguments![parameter2.ArgumentIndex]
                                : GetService(
                                    serviceProvider,
                                    parameter2.ParameterType,
                                    declaringType,
                                    parameter2.HasDefaultValue)) ?? parameter2.DefaultValue,
                             ((parameter3.ArgumentIndex != -1)
                                // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                                ? arguments![parameter3.ArgumentIndex]
                                : GetService(
                                    serviceProvider,
                                    parameter3.ParameterType,
                                    declaringType,
                                    parameter3.HasDefaultValue)) ?? parameter3.DefaultValue);
                    }
                case 4:
                    {
                        ref FactoryParameterContext parameter2 = ref parameters[1];
                        ref FactoryParameterContext parameter3 = ref parameters[2];
                        ref FactoryParameterContext parameter4 = ref parameters[3];

                        return invoker.Invoke(
                             ((parameter1.ArgumentIndex != -1)
                                // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                                ? arguments![parameter1.ArgumentIndex]
                                : GetService(
                                    serviceProvider,
                                    parameter1.ParameterType,
                                    declaringType,
                                    parameter1.HasDefaultValue)) ?? parameter1.DefaultValue,
                             ((parameter2.ArgumentIndex != -1)
                                // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                                ? arguments![parameter2.ArgumentIndex]
                                : GetService(
                                    serviceProvider,
                                    parameter2.ParameterType,
                                    declaringType,
                                    parameter2.HasDefaultValue)) ?? parameter2.DefaultValue,
                             ((parameter3.ArgumentIndex != -1)
                                // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                                ? arguments![parameter3.ArgumentIndex]
                                : GetService(
                                    serviceProvider,
                                    parameter3.ParameterType,
                                    declaringType,
                                    parameter3.HasDefaultValue)) ?? parameter3.DefaultValue,
                             ((parameter4.ArgumentIndex != -1)
                                // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                                ? arguments![parameter4.ArgumentIndex]
                                : GetService(
                                    serviceProvider,
                                    parameter4.ParameterType,
                                    declaringType,
                                    parameter4.HasDefaultValue)) ?? parameter4.DefaultValue);
                    }

            }

            return null!;
        }

        private static object ReflectionFactoryCanonicalSpan(
            ConstructorInvoker invoker,
            FactoryParameterContext[] parameters,
            Type declaringType,
            IServiceProvider serviceProvider,
            object?[]? arguments)
        {
            if (serviceProvider is null)
                ThrowHelperArgumentNullExceptionServiceProvider();

            object?[] constructorArguments = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ref FactoryParameterContext parameter = ref parameters[i];
                constructorArguments[i] = ((parameter.ArgumentIndex != -1)
                    // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                    ? arguments![parameter.ArgumentIndex]
                    : GetService(
                        serviceProvider,
                        parameter.ParameterType,
                        declaringType,
                        parameter.HasDefaultValue)) ?? parameter.DefaultValue;
            }

            return invoker.Invoke(constructorArguments.AsSpan());
        }

        private static object ReflectionFactoryDirect(
            ConstructorInvoker invoker,
            IServiceProvider serviceProvider,
            object?[]? arguments)
        {
            if (serviceProvider is null)
                ThrowHelperArgumentNullExceptionServiceProvider();

            if (arguments is null)
                ThrowHelperNullReferenceException(); //AsSpan() will not throw NullReferenceException.

            return invoker.Invoke(arguments.AsSpan());
        }

        /// <summary>
        /// For consistency with the expression-based factory, throw NullReferenceException.
        /// </summary>
        [DoesNotReturn]
        private static void ThrowHelperNullReferenceException()
        {
            throw new NullReferenceException();
        }
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        private static object ReflectionFactoryCanonical(
            ConstructorInfo constructor,
            FactoryParameterContext[] parameters,
            Type declaringType,
            IServiceProvider serviceProvider,
            object?[]? arguments)
        {
            if (serviceProvider is null)
                ThrowHelperArgumentNullExceptionServiceProvider();

            object?[] constructorArguments = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ref FactoryParameterContext parameter = ref parameters[i];
                constructorArguments[i] = ((parameter.ArgumentIndex != -1)
                    // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                    ? arguments![parameter.ArgumentIndex]
                    : GetService(
                        serviceProvider,
                        parameter.ParameterType,
                        declaringType,
                        parameter.HasDefaultValue)) ?? parameter.DefaultValue;
            }

            return constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, constructorArguments, culture: null);
        }
#endif // NET8_0_OR_GREATER
    }
}
