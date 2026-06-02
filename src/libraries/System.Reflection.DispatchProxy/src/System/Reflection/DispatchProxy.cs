// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection
{
    /// <summary>
    /// Provides a mechanism for instantiating proxy objects and handling their method dispatch.
    /// </summary>
    public abstract class DispatchProxy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DispatchProxy"/> class.
        /// </summary>
        protected DispatchProxy()
        {
        }

        /// <summary>
        /// Whenever any method on the generated proxy type is called, this method is invoked to dispatch control.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <param name="args">The arguments the caller passed to the method.</param>
        /// <returns>The object to return to the caller, or <see langword="null" /> for void methods.</returns>
        protected abstract object? Invoke(MethodInfo? targetMethod, object?[]? args);

        /// <summary>
        /// Creates an object instance that derives from class <typeparamref name="TProxy"/>
        /// and implements interface <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The interface the proxy should implement.</typeparam>
        /// <typeparam name="TProxy">The base class to use for the proxy class.</typeparam>
        /// <returns>An object instance that implements <typeparamref name="T"/>.</returns>
        /// <exception cref="System.ArgumentException"><typeparamref name="T"/> is not an interface type, or <typeparamref name="TProxy"/> is sealed or abstract or does not have a parameterless constructor.</exception>
        //
        // https://github.com/dotnet/runtime/issues/73136 - we can remove the RequiresDynamicCode annotation.
        // This has been done AOT-safely with .NET Native in the past.
        [RequiresDynamicCode("Creating a proxy instance requires generating code at runtime")]
        public static T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TProxy>()
            where TProxy : DispatchProxy
        {
            return (T)DispatchProxyGenerator.CreateProxyInstance(typeof(TProxy), typeof(T), "T", "TProxy");
        }

        /// <summary>
        /// Creates an object instance that derives from class <paramref name="proxyType"/>
        /// and implements interface <paramref name="interfaceType"/>.
        /// </summary>
        /// <param name="interfaceType">The interface the proxy should implement.</param>
        /// <param name="proxyType">The base class to use for the proxy class.</param>
        /// <returns>An object instance that implements <paramref name="interfaceType"/>.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="interfaceType"/> or <paramref name="proxyType"/> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">
        /// <para><paramref name="interfaceType"/> is not an interface type.</para>
        /// <para>-or-</para>
        /// <para><paramref name="proxyType"/> is sealed or abstract, or does not inherit from the <see cref="System.Reflection.DispatchProxy"/> type or does not have a parameterless constructor.</para>
        /// </exception>
        [RequiresDynamicCode("Creating a proxy instance requires generating code at runtime")]
        public static object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type proxyType)
        {
            ArgumentNullException.ThrowIfNull(interfaceType);
            ArgumentNullException.ThrowIfNull(proxyType);

            if (!proxyType.IsAssignableTo(typeof(DispatchProxy)))
            {
                throw new ArgumentException(SR.Format(SR.ProxyType_Must_Be_Derived_From_DispatchProxy, proxyType.Name), nameof(proxyType));
            }

            return DispatchProxyGenerator.CreateProxyInstance(proxyType, interfaceType, "interfaceType", "proxyType");
        }
    }
}
