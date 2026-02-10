// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;

namespace System.Reflection.Emit
{
    /// <summary>
    /// Defines and represents a dynamic method that can be compiled, executed, and discarded. Discarded methods are available for garbage collection.
    /// </summary>
    /// <remarks>
    /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
    /// </remarks>
    public sealed partial class DynamicMethod : MethodInfo
    {
        // The context when the method was created. We use this to do the RestrictedMemberAccess checks.
        // These checks are done when the method is compiled. This can happen at an arbitrary time,
        // when CreateDelegate or Invoke is called, or when another DynamicMethod executes OpCodes.Call.
        // We capture the creation context so that we can do the checks against the same context,
        // irrespective of when the method gets compiled. Note that the DynamicMethod does not know when
        // it is ready for use since there is not API which indictates that IL generation has completed.
        private static volatile Module? s_anonymouslyHostedDynamicMethodsModule;
        private static readonly object s_anonymouslyHostedDynamicMethodsModuleLock = new object();

        //
        // class initialization (ctor and init)
        //

        /// <summary>
        /// Initializes an anonymously hosted dynamic method, specifying the method name, return type, and parameter types.
        /// </summary>
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null" />.</param>
        /// <param name="returnType">A <see cref="Type" /> object that specifies the return type of the dynamic method, or <see langword="null" /> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="Type" /> objects specifying the types of the parameters of the dynamic method, or <see langword="null" /> if the method has no parameters.</param>
        /// <exception cref="ArgumentException">An element of <paramref name="parameterTypes" /> is <see langword="null" /> or <see cref="Void" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="NotSupportedException">.NET Framework and .NET Core versions older than 2.1: <paramref name="returnType" /> is a type for which <see cref="Type.IsByRef" /> returns <see langword="true" />.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             Type? returnType,
                             Type[]? parameterTypes)
        {
            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner
                null,   // m
                false,  // skipVisibility
                true);
        }

        /// <summary>
        /// Initializes an anonymously hosted dynamic method, specifying the method name, return type, parameter types, and whether just-in-time (JIT) visibility checks should be skipped for types and members accessed by the Microsoft intermediate language (MSIL) of the dynamic method.
        /// </summary>
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null" />.</param>
        /// <param name="returnType">A <see cref="Type" /> object that specifies the return type of the dynamic method, or <see langword="null" /> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="Type" /> objects specifying the types of the parameters of the dynamic method, or <see langword="null" /> if the method has no parameters.</param>
        /// <param name="restrictedSkipVisibility"><see langword="true" /> to skip JIT visibility checks on types and members accessed by the MSIL of the dynamic method, with this restriction: the trust level of the assemblies that contain those types and members must be equal to or less than the trust level of the call stack that emits the dynamic method; otherwise, <see langword="false" />.</param>
        /// <exception cref="ArgumentException">An element of <paramref name="parameterTypes" /> is <see langword="null" /> or <see cref="Void" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="NotSupportedException">.NET Framework and .NET Core versions older than 2.1: <paramref name="returnType" /> is a type for which <see cref="Type.IsByRef" /> returns <see langword="true" />.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             Type? returnType,
                             Type[]? parameterTypes,
                             bool restrictedSkipVisibility)
        {
            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner
                null,   // m
                restrictedSkipVisibility,
                true);
        }

        /// <summary>
        /// Creates a dynamic method that is global to a module, specifying the method name, return type, parameter types, and module.
        /// </summary>
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null" />.</param>
        /// <param name="returnType">A <see cref="Type" /> object that specifies the return type of the dynamic method, or <see langword="null" /> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="Type" /> objects specifying the types of the parameters of the dynamic method, or <see langword="null" /> if the method has no parameters.</param>
        /// <param name="m">A <see cref="Module" /> representing the module with which the dynamic method is to be logically associated.</param>
        /// <exception cref="ArgumentException">An element of <paramref name="parameterTypes" /> is <see langword="null" /> or <see cref="Void" />.
        /// -or-
        /// <paramref name="m" /> is a module that provides anonymous hosting for dynamic methods.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="m" /> is <see langword="null" />.</exception>
        /// <exception cref="NotSupportedException">.NET Framework and .NET Core versions older than 2.1: <paramref name="returnType" /> is a type for which <see cref="Type.IsByRef" /> returns <see langword="true" />.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             Type? returnType,
                             Type[]? parameterTypes,
                             Module m)
        {
            ArgumentNullException.ThrowIfNull(m);

            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner
                m,      // m
                false,  // skipVisibility
                false);
        }

        /// <summary>
        /// Creates a dynamic method that is global to a module, specifying the method name, return type, parameter types, module, and whether just-in-time (JIT) visibility checks should be skipped for types and members accessed by the Microsoft intermediate language (MSIL) of the dynamic method.
        /// </summary>
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null" />.</param>
        /// <param name="returnType">A <see cref="Type" /> object that specifies the return type of the dynamic method, or <see langword="null" /> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="Type" /> objects specifying the types of the parameters of the dynamic method, or <see langword="null" /> if the method has no parameters.</param>
        /// <param name="m">A <see cref="Module" /> representing the module with which the dynamic method is to be logically associated.</param>
        /// <param name="skipVisibility"><see langword="true" /> to skip JIT visibility checks on types and members accessed by the MSIL of the dynamic method; otherwise, <see langword="false" />.</param>
        /// <exception cref="ArgumentException">An element of <paramref name="parameterTypes" /> is <see langword="null" /> or <see cref="Void" />.
        /// -or-
        /// <paramref name="m" /> is a module that provides anonymous hosting for dynamic methods.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="m" /> is <see langword="null" />.</exception>
        /// <exception cref="NotSupportedException">.NET Framework and .NET Core versions older than 2.1: <paramref name="returnType" /> is a type for which <see cref="Type.IsByRef" /> returns <see langword="true" />.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             Type? returnType,
                             Type[]? parameterTypes,
                             Module m,
                             bool skipVisibility)
        {
            ArgumentNullException.ThrowIfNull(m);

            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner
                m,      // m
                skipVisibility,
                false);
        }

        /// <summary>
        /// Creates a dynamic method that is global to a module, specifying the method name, attributes, calling convention, return type, parameter types, module, and whether just-in-time (JIT) visibility checks should be skipped for types and members accessed by the Microsoft intermediate language (MSIL) of the dynamic method.
        /// </summary>
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null" />.</param>
        /// <param name="attributes">A bitwise combination of <see cref="MethodAttributes" /> values that specifies the attributes of the dynamic method. The only combination allowed is <see cref="MethodAttributes.Public" /> and <see cref="MethodAttributes.Static" />.</param>
        /// <param name="callingConvention">The calling convention for the dynamic method. Must be <see cref="CallingConventions.Standard" />.</param>
        /// <param name="returnType">A <see cref="Type" /> object that specifies the return type of the dynamic method, or <see langword="null" /> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="Type" /> objects specifying the types of the parameters of the dynamic method, or <see langword="null" /> if the method has no parameters.</param>
        /// <param name="m">A <see cref="Module" /> representing the module with which the dynamic method is to be logically associated.</param>
        /// <param name="skipVisibility"><see langword="true" /> to skip JIT visibility checks on types and members accessed by the MSIL of the dynamic method; otherwise, <see langword="false" />.</param>
        /// <exception cref="ArgumentException">An element of <paramref name="parameterTypes" /> is <see langword="null" /> or <see cref="Void" />.
        /// -or-
        /// <paramref name="m" /> is a module that provides anonymous hosting for dynamic methods.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="m" /> is <see langword="null" />.</exception>
        /// <exception cref="NotSupportedException"><paramref name="attributes" /> is a combination of flags other than <see cref="MethodAttributes.Public" /> and <see cref="MethodAttributes.Static" />.
        /// -or-
        /// <paramref name="callingConvention" /> is not <see cref="CallingConventions.Standard" />.
        /// -or-
        /// .NET Framework and .NET Core versions older than 2.1: <paramref name="returnType" /> is a type for which <see cref="Type.IsByRef" /> returns <see langword="true" />.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             MethodAttributes attributes,
                             CallingConventions callingConvention,
                             Type? returnType,
                             Type[]? parameterTypes,
                             Module m,
                             bool skipVisibility)
        {
            ArgumentNullException.ThrowIfNull(m);

            Init(name,
                attributes,
                callingConvention,
                returnType,
                parameterTypes,
                null,   // owner
                m,      // m
                skipVisibility,
                false);
        }

        /// <summary>
        /// Creates a dynamic method, specifying the method name, return type, parameter types, and the type with which the dynamic method is logically associated.
        /// </summary>
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null" />.</param>
        /// <param name="returnType">A <see cref="Type" /> object that specifies the return type of the dynamic method, or <see langword="null" /> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="Type" /> objects specifying the types of the parameters of the dynamic method, or <see langword="null" /> if the method has no parameters.</param>
        /// <param name="owner">A <see cref="Type" /> with which the dynamic method is logically associated. The dynamic method has access to all members of the type.</param>
        /// <exception cref="ArgumentException">An element of <paramref name="parameterTypes" /> is <see langword="null" /> or <see cref="Void" />.
        /// -or-
        /// <paramref name="owner" /> is an interface, an array, an open generic type, or a type parameter of a generic type or method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="owner" /> is <see langword="null" />.</exception>
        /// <exception cref="NotSupportedException">.NET Framework and .NET Core versions older than 2.1: <paramref name="returnType" /> is a type for which <see cref="Type.IsByRef" /> returns <see langword="true" />.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             Type? returnType,
                             Type[]? parameterTypes,
                             Type owner)
        {
            ArgumentNullException.ThrowIfNull(owner);

            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                owner,  // owner
                null,   // m
                false,  // skipVisibility
                false);
        }

        /// <summary>
        /// Creates a dynamic method, specifying the method name, return type, parameter types, the type with which the dynamic method is logically associated, and whether just-in-time (JIT) visibility checks should be skipped for types and members accessed by the Microsoft intermediate language (MSIL) of the dynamic method.
        /// </summary>
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null" />.</param>
        /// <param name="returnType">A <see cref="Type" /> object that specifies the return type of the dynamic method, or <see langword="null" /> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="Type" /> objects specifying the types of the parameters of the dynamic method, or <see langword="null" /> if the method has no parameters.</param>
        /// <param name="owner">A <see cref="Type" /> with which the dynamic method is logically associated. The dynamic method has access to all members of the type.</param>
        /// <param name="skipVisibility"><see langword="true" /> to skip JIT visibility checks on types and members accessed by the MSIL of the dynamic method; otherwise, <see langword="false" />.</param>
        /// <exception cref="ArgumentException">An element of <paramref name="parameterTypes" /> is <see langword="null" /> or <see cref="Void" />.
        /// -or-
        /// <paramref name="owner" /> is an interface, an array, an open generic type, or a type parameter of a generic type or method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="owner" /> is <see langword="null" />.</exception>
        /// <exception cref="NotSupportedException">.NET Framework and .NET Core versions older than 2.1: <paramref name="returnType" /> is a type for which <see cref="Type.IsByRef" /> returns <see langword="true" />.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             Type? returnType,
                             Type[]? parameterTypes,
                             Type owner,
                             bool skipVisibility)
        {
            ArgumentNullException.ThrowIfNull(owner);

            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                owner,  // owner
                null,   // m
                skipVisibility,
                false);
        }

        /// <summary>
        /// Creates a dynamic method, specifying the method name, attributes, calling convention, return type, parameter types, the type with which the dynamic method is logically associated, and whether just-in-time (JIT) visibility checks should be skipped for types and members accessed by the Microsoft intermediate language (MSIL) of the dynamic method.
        /// </summary>
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null" />.</param>
        /// <param name="attributes">A bitwise combination of <see cref="MethodAttributes" /> values that specifies the attributes of the dynamic method. The only combination allowed is <see cref="MethodAttributes.Public" /> and <see cref="MethodAttributes.Static" />.</param>
        /// <param name="callingConvention">The calling convention for the dynamic method. Must be <see cref="CallingConventions.Standard" />.</param>
        /// <param name="returnType">A <see cref="Type" /> object that specifies the return type of the dynamic method, or <see langword="null" /> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="Type" /> objects specifying the types of the parameters of the dynamic method, or <see langword="null" /> if the method has no parameters.</param>
        /// <param name="owner">A <see cref="Type" /> with which the dynamic method is logically associated. The dynamic method has access to all members of the type.</param>
        /// <param name="skipVisibility"><see langword="true" /> to skip JIT visibility checks on types and members accessed by the MSIL of the dynamic method; otherwise, <see langword="false" />.</param>
        /// <exception cref="ArgumentException">An element of <paramref name="parameterTypes" /> is <see langword="null" /> or <see cref="Void" />.
        /// -or-
        /// <paramref name="owner" /> is an interface, an array, an open generic type, or a type parameter of a generic type or method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="owner" /> is <see langword="null" />.</exception>
        /// <exception cref="NotSupportedException"><paramref name="attributes" /> is a combination of flags other than <see cref="MethodAttributes.Public" /> and <see cref="MethodAttributes.Static" />.
        /// -or-
        /// <paramref name="callingConvention" /> is not <see cref="CallingConventions.Standard" />.
        /// -or-
        /// .NET Framework and .NET Core versions older than 2.1: <paramref name="returnType" /> is a type for which <see cref="Type.IsByRef" /> returns <see langword="true" />.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             MethodAttributes attributes,
                             CallingConventions callingConvention,
                             Type? returnType,
                             Type[]? parameterTypes,
                             Type owner,
                             bool skipVisibility)
        {
            ArgumentNullException.ThrowIfNull(owner);

            Init(name,
                attributes,
                callingConvention,
                returnType,
                parameterTypes,
                owner,  // owner
                null,   // m
                skipVisibility,
                false);
        }

        // We create a transparent assembly to host DynamicMethods. Since the assembly does not have any
        // non-public fields (or any fields at all), it is a safe anonymous assembly to host DynamicMethods
        private static Module GetDynamicMethodsModule()
        {
            if (s_anonymouslyHostedDynamicMethodsModule != null)
                return s_anonymouslyHostedDynamicMethodsModule;

            AssemblyBuilder.EnsureDynamicCodeSupported();

            lock (s_anonymouslyHostedDynamicMethodsModuleLock)
            {
                if (s_anonymouslyHostedDynamicMethodsModule != null)
                    return s_anonymouslyHostedDynamicMethodsModule;

                AssemblyName assemblyName = new AssemblyName("Anonymously Hosted DynamicMethods Assembly");

                var assembly = RuntimeAssemblyBuilder.InternalDefineDynamicAssembly(assemblyName,
                    AssemblyBuilderAccess.Run, AssemblyLoadContext.Default, null);

                // this always gets the internal module.
                s_anonymouslyHostedDynamicMethodsModule = assembly.ManifestModule;
            }

            return s_anonymouslyHostedDynamicMethodsModule;
        }

        [MemberNotNull(nameof(_parameterTypes))]
        [MemberNotNull(nameof(_returnType))]
        [MemberNotNull(nameof(_module))]
        [MemberNotNull(nameof(_name))]
        private void Init(string name,
                          MethodAttributes attributes,
                          CallingConventions callingConvention,
                          Type? returnType,
                          Type[]? signature,
                          Type? owner,
                          Module? m,
                          bool skipVisibility,
                          bool transparentMethod)
        {
            ArgumentNullException.ThrowIfNull(name);

            AssemblyBuilder.EnsureDynamicCodeSupported();

            if (attributes != (MethodAttributes.Static | MethodAttributes.Public) || callingConvention != CallingConventions.Standard)
                throw new NotSupportedException(SR.NotSupported_DynamicMethodFlags);

            // check and store the signature
            if (signature != null)
            {
                _parameterTypes = new RuntimeType[signature.Length];
                for (int i = 0; i < signature.Length; i++)
                {
                    if (signature[i] == null)
                        throw new ArgumentException(SR.Arg_InvalidTypeInSignature);
                    _parameterTypes[i] = (signature[i].UnderlyingSystemType as RuntimeType)!;
                    if (_parameterTypes[i] == null || _parameterTypes[i] == typeof(void))
                        throw new ArgumentException(SR.Arg_InvalidTypeInSignature);
                }
            }
            else
            {
                _parameterTypes = [];
            }

            // check and store the return value
            _returnType = returnType is null ?
                (RuntimeType)typeof(void) :
                (returnType.UnderlyingSystemType as RuntimeType) ?? throw new NotSupportedException(SR.Arg_InvalidTypeInRetType);

            if (transparentMethod)
            {
                Debug.Assert(owner == null && m == null, "owner and m cannot be set for transparent methods");
                _module = GetDynamicMethodsModule();
                _restrictedSkipVisibility = skipVisibility;
            }
            else
            {
                Debug.Assert(m != null || owner != null, "Constructor should ensure that either m or owner is set");
                Debug.Assert(m == null || !m.Equals(s_anonymouslyHostedDynamicMethodsModule), "The user cannot explicitly use this assembly");
                Debug.Assert(m == null || owner == null, "m and owner cannot both be set");

                if (m != null)
                    _module = RuntimeModuleBuilder.GetRuntimeModuleFromModule(m); // this returns the underlying module for all RuntimeModule and ModuleBuilder objects.
                else
                {
                    if (owner?.UnderlyingSystemType is RuntimeType rtOwner)
                    {
                        if (rtOwner.HasElementType || rtOwner.ContainsGenericParameters
                            || rtOwner.IsGenericParameter || rtOwner.IsActualInterface)
                            throw new ArgumentException(SR.Argument_InvalidTypeForDynamicMethod);

                        _typeOwner = rtOwner;
                        _module = rtOwner.GetRuntimeModule();
                    }
                    else
                    {
                        _module = null!;
                    }
                }

                _skipVisibility = skipVisibility;
            }

            // initialize remaining fields
            _ilGenerator = null;
            _initLocals = true;
            _methodHandle = null;
            _name = name;
            _attributes = attributes;
            _callingConvention = callingConvention;
        }

        //
        // MethodInfo api.
        //

        /// <summary>
        /// Returns a string representation of the dynamic method.
        /// </summary>
        /// <returns>A string representation of the dynamic method, showing the return type, name, and parameter types.</returns>
        public override string ToString()
        {
            var sbName = new ValueStringBuilder(MethodNameBufferSize);

            sbName.Append(ReturnType.FormatTypeName());
            sbName.Append(' ');
            sbName.Append(Name);

            sbName.Append('(');
            AppendParameters(ref sbName, GetParameterTypes(), CallingConvention);
            sbName.Append(')');

            return sbName.ToString();
        }

        /// <summary>
        /// Gets the name of the dynamic method.
        /// </summary>
        /// <value>The name of the dynamic method.</value>
        public override string Name => _name;

        /// <summary>
        /// Gets the type that declares the dynamic method.
        /// </summary>
        /// <value>Always <see langword="null" /> for dynamic methods.</value>
        public override Type? DeclaringType => null;

        /// <summary>
        /// Gets the class object that was used to obtain the instance of the dynamic method.
        /// </summary>
        /// <value>Always <see langword="null" /> for dynamic methods.</value>
        public override Type? ReflectedType => null;

        /// <summary>
        /// Gets the module associated with the dynamic method.
        /// </summary>
        /// <value>The <see cref="System.Reflection.Module" /> associated with the dynamic method.</value>
        public override Module Module => _module;

        // we cannot return a MethodHandle because we cannot track it via GC so this method is off limits
        /// <summary>
        /// Not supported for dynamic methods.
        /// </summary>
        /// <value>Not supported for dynamic methods.</value>
        /// <exception cref="InvalidOperationException">Always thrown. The <see cref="RuntimeMethodHandle" /> of a dynamic method is not supported.</exception>
        public override RuntimeMethodHandle MethodHandle => throw new InvalidOperationException(SR.InvalidOperation_NotAllowedInDynamicMethod);

        /// <summary>
        /// Gets the attributes specified when the dynamic method was created.
        /// </summary>
        /// <value>A bitwise combination of the <see cref="MethodAttributes" /> values representing the attributes for the method.</value>
        public override MethodAttributes Attributes => _attributes;

        /// <summary>
        /// Gets the calling convention specified when the dynamic method was created.
        /// </summary>
        /// <value>One of the <see cref="CallingConventions" /> values that indicates the calling convention of the method.</value>
        public override CallingConventions CallingConvention => _callingConvention;

        /// <summary>
        /// Returns the base definition for the dynamic method.
        /// </summary>
        /// <returns>Always returns this dynamic method.</returns>
        public override MethodInfo GetBaseDefinition() => this;

        /// <summary>
        /// Returns an array of <see cref="ParameterInfo" /> objects representing the parameters of the dynamic method.
        /// </summary>
        /// <returns>An array of <see cref="ParameterInfo" /> objects representing the parameters of the dynamic method, or an empty array if the method has no parameters.</returns>
        public override ParameterInfo[] GetParameters() =>
            GetParametersAsSpan().ToArray();

        internal override ReadOnlySpan<ParameterInfo> GetParametersAsSpan() => LoadParameters();

        /// <summary>
        /// Returns the implementation flags for the method.
        /// </summary>
        /// <returns>A bitwise combination of <see cref="MethodImplAttributes" /> values representing the implementation flags for the method.</returns>
        public override MethodImplAttributes GetMethodImplementationFlags() =>
            MethodImplAttributes.IL | MethodImplAttributes.NoInlining;

        /// <summary>
        /// Gets a value that indicates whether the dynamic method is security-critical.
        /// </summary>
        /// <value><see langword="true" /> for all dynamic methods.</value>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        public override bool IsSecurityCritical => true;

        /// <summary>
        /// Gets a value that indicates whether the dynamic method is security-safe-critical.
        /// </summary>
        /// <value><see langword="false" /> for all dynamic methods.</value>
        public override bool IsSecuritySafeCritical => false;

        /// <summary>
        /// Gets a value that indicates whether the dynamic method is security-transparent.
        /// </summary>
        /// <value><see langword="false" /> for all dynamic methods.</value>
        public override bool IsSecurityTransparent => false;

        /// <summary>
        /// Returns an array of all custom attributes defined on the dynamic method.
        /// </summary>
        /// <param name="attributeType">The type of attribute to search for. Only attributes that are assignable to this type are returned.</param>
        /// <param name="inherit">This parameter is ignored for dynamic methods, because they do not support inheritance.</param>
        /// <returns>An array of custom attributes defined on the dynamic method. If no attributes of the specified type are defined, an empty array is returned.</returns>
        /// <exception cref="ArgumentException"><paramref name="attributeType" /> is not a <see cref="RuntimeType" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="attributeType" /> is <see langword="null" />.</exception>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            bool includeMethodImplAttribute = attributeType.IsAssignableFrom(typeof(MethodImplAttribute));
            object[] result = CustomAttribute.CreateAttributeArrayHelper(attributeRuntimeType, includeMethodImplAttribute ? 1 : 0);
            if (includeMethodImplAttribute)
            {
                result[0] = new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags());
            }
            return result;
        }

        /// <summary>
        /// Returns an array of all custom attributes defined on the dynamic method.
        /// </summary>
        /// <param name="inherit">This parameter is ignored for dynamic methods, because they do not support inheritance.</param>
        /// <returns>An array of all custom attributes defined on the dynamic method.</returns>
        public override object[] GetCustomAttributes(bool inherit)
        {
            // support for MethodImplAttribute PCA
            return [new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags())];
        }

        /// <summary>
        /// Indicates whether one or more attributes of the specified type or of its derived types is applied to this method.
        /// </summary>
        /// <param name="attributeType">The type of custom attribute to search for. The search includes derived types.</param>
        /// <param name="inherit">This parameter is ignored for dynamic methods, because they do not support inheritance.</param>
        /// <returns><see langword="true" /> if one or more instances of <paramref name="attributeType" /> or any of its derived types is applied to this method; otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="attributeType" /> is <see langword="null" />.</exception>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            return attributeType.IsAssignableFrom(typeof(MethodImplAttribute));
        }

        /// <summary>
        /// Gets the return type of the dynamic method.
        /// </summary>
        /// <value>A <see cref="Type" /> representing the return type of the dynamic method; or <see cref="void" /> if the method has no return type.</value>
        public override Type ReturnType => _returnType;

        /// <summary>
        /// Gets the return parameter of the dynamic method.
        /// </summary>
        /// <value>A <see cref="ParameterInfo" /> object that represents the return parameter of the dynamic method.</value>
        public override ParameterInfo ReturnParameter => new RuntimeParameterInfo(this, null, _returnType, -1);

        /// <summary>
        /// Gets the custom attributes of the return type for the dynamic method.
        /// </summary>
        /// <value>An <see cref="ICustomAttributeProvider" /> representing the custom attributes of the return type for the dynamic method.</value>
        public override ICustomAttributeProvider ReturnTypeCustomAttributes => new EmptyCAHolder();

        //
        // DynamicMethod specific methods
        //

        /// <summary>
        /// Defines a parameter of the dynamic method.
        /// </summary>
        /// <param name="position">The position of the parameter in the parameter list. Parameters are indexed beginning with the number 1 for the first parameter.</param>
        /// <param name="attributes">A bitwise combination of <see cref="ParameterAttributes" /> values that specifies the attributes of the parameter.</param>
        /// <param name="parameterName">The name of the parameter. The name can be a zero-length string.</param>
        /// <returns>Always returns <see langword="null" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The method has no parameters.
        /// -or-
        /// <paramref name="position" /> is less than 0.
        /// -or-
        /// <paramref name="position" /> is greater than the number of parameters of the dynamic method.</exception>
        public ParameterBuilder? DefineParameter(int position, ParameterAttributes attributes, string? parameterName)
        {
            if (position < 0 || position > _parameterTypes.Length)
                throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_ParamSequence);
            position--; // it's 1 based. 0 is the return value

            if (position >= 0)
            {
                RuntimeParameterInfo[] parameters = LoadParameters();
                parameters[position].SetName(parameterName);
                parameters[position].SetAttributes(attributes);
            }
            return null;
        }

        /// <summary>
        /// Returns a Microsoft intermediate language (MSIL) generator for the method with a default MSIL stream size of 64 bytes.
        /// </summary>
        /// <returns>An <see cref="ILGenerator" /> object for the method.</returns>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        public ILGenerator GetILGenerator()
        {
            return GetILGenerator(64);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the local variables in the method are zero-initialized.
        /// </summary>
        /// <value><see langword="true" /> if the local variables in the method are zero-initialized; otherwise, <see langword="false" />. The default is <see langword="true" />.</value>
        public bool InitLocals
        {
            get => _initLocals;
            set => _initLocals = value;
        }

        internal RuntimeType[] ArgumentTypes => _parameterTypes;

        private RuntimeParameterInfo[] LoadParameters()
        {
            if (_parameters == null)
            {
                Type[] parameterTypes = _parameterTypes;
                RuntimeParameterInfo[] parameters = new RuntimeParameterInfo[parameterTypes.Length];
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    parameters[i] = new RuntimeParameterInfo(this, null, parameterTypes[i], i);
                }

                _parameters ??= parameters; // should we Interlocked.CompareExchange?
            }

            return _parameters;
        }
    }
}
