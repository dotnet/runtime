// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ActivatorUtilities
    {
#if NET8_0_OR_GREATER // Use the faster ConstructorInvoker which also has alloc-free APIs when <= 4 parameters.
        private static object ReflectionFactoryServiceOnlyFixed(
            ConstructorInvoker invoker,
            Type[] parameterTypes,
            Type declaringType,
            IServiceProvider serviceProvider)
        {
            Debug.Assert(parameterTypes.Length >= 1 && parameterTypes.Length <= FixedArgumentThreshold);
            Debug.Assert(FixedArgumentThreshold == 4);

            ArgumentNullException.ThrowIfNull(nameof(serviceProvider));

            object? arg1 = null;
            object? arg2 = null;
            object? arg3 = null;
            object? arg4 = null;

            switch (parameterTypes.Length)
            {
                case 4:
                    arg4 = GetService(serviceProvider, parameterTypes[3], declaringType, false);
                    goto case 3;
                case 3:
                    arg3 = GetService(serviceProvider, parameterTypes[2], declaringType, false);
                    goto case 2;
                case 2:
                    arg2 = GetService(serviceProvider, parameterTypes[1], declaringType, false);
                    goto case 1;
                case 1:
                    arg1 = GetService(serviceProvider, parameterTypes[0], declaringType, false);
                    break;
            }

            return invoker.Invoke(arg1, arg2, arg3, arg4)!;
        }

        private static object ReflectionFactoryServiceOnlySpan(
            ConstructorInvoker invoker,
            Type[] parameterTypes,
            Type declaringType,
            IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(nameof(serviceProvider));

            object?[] arguments = new object?[parameterTypes.Length];
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                arguments[i] = GetService(serviceProvider, parameterTypes[i], declaringType, false);
            }

            return invoker.Invoke(arguments.AsSpan())!;
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

            ArgumentNullException.ThrowIfNull(nameof(serviceProvider));

            object? arg1 = null;
            object? arg2 = null;
            object? arg3 = null;
            object? arg4 = null;

            switch (parameters.Length)
            {
                case 4:
                    ref FactoryParameterContext parameter4 = ref parameters[3];
                    arg4 = ((parameter4.ArgumentIndex != -1)
                        // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                        ? arguments![parameter4.ArgumentIndex]
                        : GetService(
                            serviceProvider,
                            parameter4.ParameterType,
                            declaringType,
                            parameter4.HasDefaultValue)) ?? parameter4.DefaultValue;
                    goto case 3;
                case 3:
                    ref FactoryParameterContext parameter3 = ref parameters[2];
                    arg3 = ((parameter3.ArgumentIndex != -1)
                        ? arguments![parameter3.ArgumentIndex]
                        : GetService(
                            serviceProvider,
                            parameter3.ParameterType,
                            declaringType,
                            parameter3.HasDefaultValue)) ?? parameter3.DefaultValue;
                    goto case 2;
                case 2:
                    ref FactoryParameterContext parameter2 = ref parameters[1];
                    arg2 = ((parameter2.ArgumentIndex != -1)
                        ? arguments![parameter2.ArgumentIndex]
                        : GetService(
                            serviceProvider,
                            parameter2.ParameterType,
                            declaringType,
                            parameter2.HasDefaultValue)) ?? parameter2.DefaultValue;
                    goto case 1;
                case 1:
                    ref FactoryParameterContext parameter1 = ref parameters[0];
                    arg1 = ((parameter1.ArgumentIndex != -1)
                        ? arguments![parameter1.ArgumentIndex]
                        : GetService(
                            serviceProvider,
                            parameter1.ParameterType,
                            declaringType,
                            parameter1.HasDefaultValue)) ?? parameter1.DefaultValue;
                    break;
            }

            return invoker.Invoke(arg1, arg2, arg3, arg4)!;
        }

        private static object ReflectionFactoryCanonicalSpan(
            ConstructorInvoker invoker,
            FactoryParameterContext[] parameters,
            Type declaringType,
            IServiceProvider serviceProvider,
            object?[]? arguments)
        {
            ArgumentNullException.ThrowIfNull(nameof(serviceProvider));

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

            return invoker.Invoke(constructorArguments.AsSpan())!;
        }

        private static object ReflectionFactoryDirect(
            ConstructorInvoker invoker,
            IServiceProvider serviceProvider,
            object?[]? arguments)
        {
            if (serviceProvider is null)
            {
                // Cannot use ArgumentNullException.ThrowIfNull here since that causes a compile error since serviceProvider is not used.
                ThrowHelperArgumentNullExceptionServiceProvider();
            }

            if (arguments is null)
                ThrowHelperNullReferenceException(); //AsSpan() will not throw NullReferenceException.

            return invoker.Invoke(arguments.AsSpan())!;
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
#endif

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        [DoesNotReturn]
        private static void ThrowHelperArgumentNullExceptionServiceProvider()
        {
            throw new ArgumentNullException("serviceProvider");
        }
#endif
    }
}
