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

        private static object? GetService(IServiceProvider sp, Type type, Type requiredBy, bool isDefaultParameterRequired)
        {
            object? service = sp.GetService(type);
            if (service == null && !isDefaultParameterRequired)
            {
                throw new InvalidOperationException(SR.Format(SR.UnableToResolveService, type, requiredBy));
            }
            return service;
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
        private static ObjectFactory CreateFactoryReflection(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type?[] argumentTypes)
        {
            FindApplicableConstructor(instanceType, argumentTypes, out ConstructorInfo constructor, out int?[] parameterMap);

            ParameterInfo[] constructorParameters = constructor.GetParameters();
            if (constructorParameters.Length == 0)
            {
                return (IServiceProvider serviceProvider, object?[]? arguments) =>
                    constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: null, culture: null);
            }

            FactoryParameterContext[] parameters = new FactoryParameterContext[constructorParameters.Length];
            for (int i = 0; i < constructorParameters.Length; i++)
            {
                ParameterInfo constructorParameter = constructorParameters[i];
                bool hasDefaultValue = ParameterDefaultValue.TryGetDefaultValue(constructorParameter, out object? defaultValue);

                parameters[i] = new FactoryParameterContext(constructorParameter.ParameterType, hasDefaultValue, defaultValue, parameterMap[i] ?? -1);
            }
            Type declaringType = constructor.DeclaringType!;

            return (IServiceProvider serviceProvider, object?[]? arguments) =>
            {
                if (serviceProvider is null)
                {
                    throw new ArgumentNullException(nameof(serviceProvider));
                }

                object?[] constructorArguments = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    ref FactoryParameterContext parameter = ref parameters[i];
                    constructorArguments[i] = ((parameter.ArgumentIndex != -1)
                        // Throws an NullReferenceException if arguments is null. Consistent with expression-based factory.
                        ? arguments![parameter.ArgumentIndex]
                        : GetService(
                            serviceProvider,
                            parameter.ParameterType,
                            declaringType,
                            parameter.HasDefaultValue)) ?? parameter.DefaultValue;
                }

                return constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, constructorArguments, culture: null);
            };
        }

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
#endif

        private static void FindApplicableConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type?[] argumentTypes,
            out ConstructorInfo matchingConstructor,
            out int?[] matchingParameterMap)
        {
            ConstructorInfo? constructorInfo = null;
            int?[]? parameterMap = null;

            if (!TryFindPreferredConstructor(instanceType, argumentTypes, ref constructorInfo, ref parameterMap) &&
                !TryFindMatchingConstructor(instanceType, argumentTypes, ref constructorInfo, ref parameterMap))
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
            [NotNullWhen(true)] ref ConstructorInfo? matchingConstructor,
            [NotNullWhen(true)] ref int?[]? parameterMap)
        {
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
            [NotNullWhen(true)] ref ConstructorInfo? matchingConstructor,
            [NotNullWhen(true)] ref int?[]? parameterMap)
        {
            bool seenPreferred = false;
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
                        !serviceProviderIsService.IsService(_parameters[i].ParameterType))
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
                        object? value = provider.GetService(_parameters[index].ParameterType);
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
    }
}
