// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
            Type[] argumentTypes = parameters
                ?.Select(parameter => parameter.GetType())
                .ToArray()
                ?? Array.Empty<Type>();

            FindApplicableConstructor(
                instanceType,
                argumentTypes,
                out ConstructorInfo? constructor,
                out int?[]? matchingParameterMap);

            return CreateInstance(constructor, parameters, provider, matchingParameterMap);
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
            FindApplicableConstructor(instanceType, argumentTypes, out ConstructorInfo? constructor, out int?[]? parameterMap);

            ParameterExpression? provider = Expression.Parameter(typeof(IServiceProvider), "provider");
            ParameterExpression? argumentArray = Expression.Parameter(typeof(object[]), "argumentArray");
            Expression? factoryExpressionBody = BuildFactoryExpression(constructor, parameterMap, provider, argumentArray);

            var factoryLambda = Expression.Lambda<Func<IServiceProvider, object[], object>>(
                factoryExpressionBody, provider, argumentArray);

            Func<IServiceProvider, object[], object>? result = factoryLambda.Compile();
            return result.Invoke;
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
                string? message = $"Unable to resolve service for type '{type}' while attempting to activate '{requiredBy}'.";
                throw new InvalidOperationException(message);
            }
            return service;
        }

        private static Expression BuildFactoryExpression(
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

            return Expression.New(constructor, constructorArguments);
        }

        private static void FindApplicableConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type[] argumentTypes,
            out ConstructorInfo matchingConstructor,
            out int?[] matchingParameterMap)
        {
            ConstructorInfo? constructorInfo = null;
            int?[]? parameterMap = null;

            if (instanceType.IsAbstract ||
                (!TryFindPreferredConstructor(instanceType, argumentTypes, ref constructorInfo, ref parameterMap) &&
                !TryFindMatchingConstructor(instanceType, argumentTypes, ref constructorInfo, ref parameterMap)))
            {
                string? message = $"A suitable constructor for type '{instanceType}' could not be located. Ensure the type is concrete and all parameters of a public constructor are either registered as services or passed as arguments. Also ensure no extraneous arguments are provided.";
                throw new InvalidOperationException(message);
            }

            matchingConstructor = constructorInfo;
            matchingParameterMap = parameterMap;
        }

        // Tries to find constructor based on provided argument types
        private static bool TryFindMatchingConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type[] argumentTypes,
            [NotNullWhen(true)] ref ConstructorInfo? matchingConstructor,
            [NotNullWhen(true)] ref int?[]? parameterMap)
        {
            int unresolvedParameters = int.MaxValue;

            foreach (ConstructorInfo? constructor in instanceType.GetConstructors())
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                if (TryCreateParameterMap(parameters, argumentTypes, out int?[] tempParameterMap))
                {
                    var tempUnresolvedParameters = CountUnresolvedParameters(parameters, tempParameterMap);
                    if (tempUnresolvedParameters < unresolvedParameters
                        || (tempUnresolvedParameters == unresolvedParameters && tempParameterMap.Length > parameterMap.Length))
                    {
                        matchingConstructor = constructor;
                        parameterMap = tempParameterMap;
                        unresolvedParameters = tempUnresolvedParameters;
                    }
                }
            }

            if (matchingConstructor != null)
            {
                Debug.Assert(parameterMap != null);
                return true;
            }

            return false;
        }

        private static int CountUnresolvedParameters(ParameterInfo[] parameters, int?[] parameterMap)
        {
            int unresolvedParameters = 0;
            for (int index = 0; index < parameters.Length; ++index)
            {
                if (ParameterDefaultValue.TryGetDefaultValue(parameters[index], out object? _)
                    || parameterMap[index].HasValue)
                {
                    continue;
                }

                ++unresolvedParameters;
            }

            return unresolvedParameters;
        }

        // Tries to find constructor marked with ActivatorUtilitiesConstructorAttribute
        private static bool TryFindPreferredConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type[] argumentTypes,
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
                        throw new InvalidOperationException(
                            $"Multiple constructors were marked with {nameof(ActivatorUtilitiesConstructorAttribute)}.");
                    }

                    if (!TryCreateParameterMap(constructor.GetParameters(), argumentTypes, out int?[] tempParameterMap))
                    {
                        throw new InvalidOperationException(
                            $"Constructor marked with {nameof(ActivatorUtilitiesConstructorAttribute)} does not accept all given argument types.");
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
        private static bool TryCreateParameterMap(ParameterInfo[] constructorParameters, Type[] argumentTypes, out int?[] parameterMap)
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

        private static object CreateInstance(
            ConstructorInfo constructor,
            object[] givenArguments,
            IServiceProvider provider,
            int?[]? matchingParameterMap)
        {
            var parameters = constructor.GetParameters();
            var appliedArguments = new object?[parameters.Length];

            for (int applyIndex = 0; applyIndex != parameters.Length; applyIndex++)
            {
                if (matchingParameterMap != null && matchingParameterMap[applyIndex].HasValue)
                {
                    int givenArgumentIndex = matchingParameterMap[applyIndex].Value;
                    appliedArguments[applyIndex] = givenArguments[givenArgumentIndex];
                }
                else
                {
                    object? value = provider.GetService(parameters[applyIndex].ParameterType);
                    if (value == null)
                    {
                        if (!ParameterDefaultValue.TryGetDefaultValue(parameters[applyIndex], out object? defaultValue))
                        {
                            throw new InvalidOperationException(
                                $"Unable to resolve service for type '{parameters[applyIndex].ParameterType}' while " +
                                $"attempting to activate '{constructor.DeclaringType}'.");
                        }
                        else
                        {
                            appliedArguments[applyIndex] = defaultValue;
                        }
                    }
                    else
                    {
                        appliedArguments[applyIndex] = value;
                    }
                }
            }

#if NETCOREAPP
                return _constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: appliedArguments, culture: null);
#else
            try
            {
                return constructor.Invoke(appliedArguments);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                // The above line will always throw, but the compiler requires we throw explicitly.
                throw;
            }
#endif
        }
    }
}
