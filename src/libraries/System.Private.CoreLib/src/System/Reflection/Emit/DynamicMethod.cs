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
    /// For more information about this API, see <see href="/dotnet/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod">Supplemental API remarks for DynamicMethod</see>.
    /// </remarks>
    /// <example>
    /// The following example creates a dynamic method, emits a method body, and executes it via a delegate and via <see cref="System.Reflection.Emit.DynamicMethod.Invoke"/>.
    /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Overview.cs" region="CreateAndInvoke" title="Creating and invoking a DynamicMethod" />
    /// </example>
    /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/how-to-define-and-execute-dynamic-methods">How to: Define and Execute Dynamic Methods</related>
    /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit">Security Issues in Reflection Emit</related>
    /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/walkthrough-emitting-code-in-partial-trust-scenarios">Walkthrough: Emitting Code in Partial Trust Scenarios</related>
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
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null"/>.</param>
        /// <param name="returnType">A <see cref="T:System.Type"/> object that specifies the return type of the dynamic method, or <see langword="null"/> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects specifying the types of the parameters of the dynamic method, or <see langword="null"/> if the method has no parameters.</param>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="parameterTypes"/> is <see langword="null"/> or <see cref="T:System.Void"/>.</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="T:System.NotSupportedException">.NET Framework and .NET Core versions older than 2.1: <paramref name="returnType"/> is a type for which <see cref="P:System.Type.IsByRef"/> returns <see langword="true"/>.</exception>
        /// <remarks>
        /// <note type="note">
        /// This constructor was introduced in the .NET Framework 3.5 or later.
        /// </note>
        /// The dynamic method that is created by this constructor is associated with an anonymous assembly instead of an existing type or module. The anonymous assembly exists only to provide a sandbox environment for dynamic methods, that is, to isolate them from other code. This environment makes it safe for the dynamic method to be emitted and executed by partially trusted code.
        /// This constructor specifies that just-in-time (JIT) visibility checks will be enforced for the Microsoft intermediate language (MSIL) of the dynamic method. That is, the code in the dynamic method has access to public methods of public classes. Exceptions are thrown if the method tries to access types or members that are <c>private</c>, <c>protected</c>, or <c>internal</c> (<c>Friend</c> in Visual Basic). To create a dynamic method that has restricted ability to skip JIT visibility checks, use the <see cref="System.Reflection.Emit.DynamicMethod.#ctor%28System.String%2CSystem.Type%2CSystem.Type%5B%5D%2CSystem.Boolean%29"/> constructor.
        /// When an anonymously hosted dynamic method is constructed, the call stack of the emitting assembly is included. When the method is invoked, the permissions of the emitting assembly are used instead of the permissions of the actual caller. Thus, the dynamic method cannot execute at a higher level of privilege than that of the assembly that emitted it, even if it is passed to and executed by an assembly that has a higher trust level.
        /// This constructor specifies the method attributes <see cref="System.Reflection.MethodAttributes.Public">Public</see> and <see cref="System.Reflection.MethodAttributes.Static">Static</see>, and the calling convention <see cref="System.Reflection.CallingConventions.Standard">Standard</see>.
        /// </remarks>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/how-to-define-and-execute-dynamic-methods">How to: Define and Execute Dynamic Methods</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit">Security Issues in Reflection Emit</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/walkthrough-emitting-code-in-partial-trust-scenarios">Walkthrough: Emitting Code in Partial Trust Scenarios</related>
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
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null"/>.</param>
        /// <param name="returnType">A <see cref="T:System.Type"/> object that specifies the return type of the dynamic method, or <see langword="null"/> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects specifying the types of the parameters of the dynamic method, or <see langword="null"/> if the method has no parameters.</param>
        /// <param name="restrictedSkipVisibility"><see langword="true"/> to skip JIT visibility checks on types and members accessed by the MSIL of the dynamic method, with this restriction: the trust level of the assemblies that contain those types and members must be equal to or less than the trust level of the call stack that emits the dynamic method; otherwise, <see langword="false"/>.</param>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="parameterTypes"/> is <see langword="null"/> or <see cref="T:System.Void"/>.</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="T:System.NotSupportedException">.NET Framework and .NET Core versions older than 2.1: <paramref name="returnType"/> is a type for which <see cref="P:System.Type.IsByRef"/> returns <see langword="true"/>.</exception>
        /// <remarks>
        /// <note type="important">
        /// </note>
        /// <note type="note">
        /// This constructor was introduced in the .NET Framework 3.5 or later.
        /// </note>
        /// The dynamic method that is created by this constructor is associated with an anonymous assembly instead of an existing type or module. The anonymous assembly exists only to provide a sandbox environment for dynamic methods, that is, to isolate them from other code. This environment makes it safe for the dynamic method to be emitted and executed by partially trusted code.
        /// Anonymously hosted dynamic methods do not have automatic access to any types or members that are <c>private</c>, <c>protected</c>, or <c>internal</c> (<c>Friend</c> in Visual Basic). This is different from dynamic methods that are associated with an existing type or module, which have access to hidden members in their associated scope.
        /// Specify <c>true</c> for <c>restrictedSkipVisibility</c> if your dynamic method has to access types or members that are <c>private</c>, <c>protected</c>, or <c>internal</c>. This gives the dynamic method restricted access to these members. That is, the members can be accessed only if the following conditions are met:
        /// - The target members belong to an assembly that has a level of trust equal to or lower than the call stack that emits the dynamic method.
        /// - The call stack that emits the dynamic method is granted <see cref="System.Security.Permissions.ReflectionPermission"/> with the <see cref="System.Security.Permissions.ReflectionPermissionFlag.RestrictedMemberAccess">RestrictedMemberAccess</see> flag. This is always true when the code is executed with full trust. For partially trusted code, it is true only if the host explicitly grants the permission.
        /// >  If the permission has not been granted, a security exception is thrown when <see cref="System.Reflection.Emit.DynamicMethod.CreateDelegate"/> is called or when the dynamic method is invoked, not when this constructor is called. No special permissions are required to emit the dynamic method.
        /// For example, a dynamic method that is created with <c>restrictedSkipVisibility</c> set to <c>true</c> can access a private member of any assembly on the call stack if the call stack has been granted restricted member access. If the dynamic method is created with partially trusted code on the call stack, it cannot access a private member of a type in a .NET Framework assembly, because such assemblies are fully trusted.
        /// If <c>restrictedSkipVisibility</c> is <c>false</c>, JIT visibility checks are enforced. The code in the dynamic method has access to public methods of public classes, and exceptions are thrown if it tries to access types or members that are <c>private</c>, <c>protected</c>, or <c>internal</c>.
        /// When an anonymously hosted dynamic method is constructed, the call stack of the emitting assembly is included. When the method is invoked, the permissions of the emitting call stack are used instead of the permissions of the actual caller. Thus, the dynamic method cannot execute at a higher level of privilege than that of the assembly that emitted it, even if it is passed to and executed by an assembly that has a higher trust level.
        /// This constructor specifies the method attributes <see cref="System.Reflection.MethodAttributes.Public">Public</see> and <see cref="System.Reflection.MethodAttributes.Static">Static</see>, and the calling convention <see cref="System.Reflection.CallingConventions.Standard">Standard</see>.
        /// </remarks>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/how-to-define-and-execute-dynamic-methods">How to: Define and Execute Dynamic Methods</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit">Security Issues in Reflection Emit</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/walkthrough-emitting-code-in-partial-trust-scenarios">Walkthrough: Emitting Code in Partial Trust Scenarios</related>
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
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null"/>.</param>
        /// <param name="returnType">A <see cref="T:System.Type"/> object that specifies the return type of the dynamic method, or <see langword="null"/> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects specifying the types of the parameters of the dynamic method, or <see langword="null"/> if the method has no parameters.</param>
        /// <param name="m">A <see cref="T:System.Reflection.Module"/> representing the module with which the dynamic method is to be logically associated.</param>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="parameterTypes"/> is <see langword="null"/> or <see cref="T:System.Void"/>. -or- <paramref name="m"/> is a module that provides anonymous hosting for dynamic methods.</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="name"/> is <see langword="null"/>. -or- <paramref name="m"/> is <see langword="null"/>.</exception>
        /// <exception cref="T:System.NotSupportedException">.NET Framework and .NET Core versions older than 2.1: <paramref name="returnType"/> is a type for which <see cref="P:System.Type.IsByRef"/> returns <see langword="true"/>.</exception>
        /// <remarks>
        /// <note type="note">
        /// </note>
        /// This constructor specifies method attributes <see cref="System.Reflection.MethodAttributes.Public">Public</see> and <see cref="System.Reflection.MethodAttributes.Static">Static</see>, calling convention <see cref="System.Reflection.CallingConventions.Standard">Standard</see>, and does not skip just-in-time (JIT) visibility checks.
        /// The dynamic method created with this constructor has access to public and <c>internal</c> (<c>Friend</c> in Visual Basic) members of all the types contained in module <c>m</c>.
        /// >  For backward compatibility, this constructor demands <see cref="System.Security.Permissions.SecurityPermission"/> with the <see cref="System.Security.Permissions.SecurityPermissionFlag.ControlEvidence">ControlEvidence</see> flag if the following conditions are both true: <c>m</c> is a module other than the calling module, and the demand for <see cref="System.Security.Permissions.ReflectionPermission"/> with the <see cref="System.Security.Permissions.ReflectionPermissionFlag.MemberAccess">MemberAccess</see> flag has failed. If the demand for <see cref="System.Security.Permissions.SecurityPermission"/> succeeds, the operation is allowed.
        /// The following code example creates a dynamic method that takes two parameters. The example emits a simple function body that prints the first parameter to the console, and the example uses the second parameter as the return value of the method. The example completes the method by creating a delegate, invokes the delegate with different parameters, and finally invokes the dynamic method using the <see cref="System.Reflection.Emit.DynamicMethod.Invoke%28System.Object%2CSystem.Reflection.BindingFlags%2CSystem.Reflection.Binder%2CSystem.Object%5B%5D%2CSystem.Globalization.CultureInfo%29"/> method.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Overview.cs" region="CreateAndInvoke" title="Creating a DynamicMethod associated with a module" />
        /// </example>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/how-to-define-and-execute-dynamic-methods">How to: Define and Execute Dynamic Methods</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit">Security Issues in Reflection Emit</related>
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
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null"/>.</param>
        /// <param name="returnType">A <see cref="T:System.Type"/> object that specifies the return type of the dynamic method, or <see langword="null"/> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects specifying the types of the parameters of the dynamic method, or <see langword="null"/> if the method has no parameters.</param>
        /// <param name="m">A <see cref="T:System.Reflection.Module"/> representing the module with which the dynamic method is to be logically associated.</param>
        /// <param name="skipVisibility"><see langword="true"/> to skip JIT visibility checks on types and members accessed by the MSIL of the dynamic method.</param>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="parameterTypes"/> is <see langword="null"/> or <see cref="T:System.Void"/>. -or- <paramref name="m"/> is a module that provides anonymous hosting for dynamic methods.</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="name"/> is <see langword="null"/>. -or- <paramref name="m"/> is <see langword="null"/>.</exception>
        /// <exception cref="T:System.NotSupportedException">.NET Framework and .NET Core versions older than 2.1: <paramref name="returnType"/> is a type for which <see cref="P:System.Type.IsByRef"/> returns <see langword="true"/>.</exception>
        /// <remarks>
        /// <note type="note">
        /// For backward compatibility, this constructor demands <see cref="System.Security.Permissions.SecurityPermission"/> with the <see cref="System.Security.Permissions.SecurityPermissionFlag.ControlEvidence">ControlEvidence</see> flag if the following conditions are both true: <c>m</c> is a module other than the calling module, and the demand for <see cref="System.Security.Permissions.ReflectionPermission"/> with the <see cref="System.Security.Permissions.ReflectionPermissionFlag.MemberAccess">MemberAccess</see> flag has failed. If the demand for <see cref="System.Security.Permissions.SecurityPermission"/> succeeds, the operation is allowed.
        /// </note>
        /// This constructor specifies method attributes <see cref="System.Reflection.MethodAttributes.Public">Public</see> and <see cref="System.Reflection.MethodAttributes.Static">Static</see>, and calling convention <see cref="System.Reflection.CallingConventions.Standard">Standard</see>.
        /// The dynamic method created with this constructor has access to public and <c>internal</c> (<c>Friend</c> in Visual Basic) members of all the types in contained module <c>m</c>. Skipping the JIT compiler's visibility checks allows the dynamic method to access private and protected members of all other types as well. This is useful, for example, when writing code to serialize objects.
        /// </remarks>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/how-to-define-and-execute-dynamic-methods">How to: Define and Execute Dynamic Methods</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit">Security Issues in Reflection Emit</related>
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
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null"/>.</param>
        /// <param name="attributes">A bitwise combination of <see cref="T:System.Reflection.MethodAttributes"/> values that specifies the attributes of the dynamic method. The only combination allowed is <see cref="F:System.Reflection.MethodAttributes.Public"/> and <see cref="F:System.Reflection.MethodAttributes.Static"/>.</param>
        /// <param name="callingConvention">The calling convention for the dynamic method. Must be <see cref="F:System.Reflection.CallingConventions.Standard"/>.</param>
        /// <param name="returnType">A <see cref="T:System.Type"/> object that specifies the return type of the dynamic method, or <see langword="null"/> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects specifying the types of the parameters of the dynamic method, or <see langword="null"/> if the method has no parameters.</param>
        /// <param name="m">A <see cref="T:System.Reflection.Module"/> representing the module with which the dynamic method is to be logically associated.</param>
        /// <param name="skipVisibility"><see langword="true"/> to skip JIT visibility checks on types and members accessed by the MSIL of the dynamic method; otherwise, <see langword="false"/>.</param>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="parameterTypes"/> is <see langword="null"/> or <see cref="T:System.Void"/>. -or- <paramref name="m"/> is a module that provides anonymous hosting for dynamic methods.</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="name"/> is <see langword="null"/>. -or- <paramref name="m"/> is <see langword="null"/>.</exception>
        /// <exception cref="T:System.NotSupportedException"><paramref name="attributes"/> is a combination of flags other than <see cref="F:System.Reflection.MethodAttributes.Public"/> and <see cref="F:System.Reflection.MethodAttributes.Static"/>. -or- <paramref name="callingConvention"/> is not <see cref="F:System.Reflection.CallingConventions.Standard"/>. -or- <paramref name="returnType"/> is a type for which <see cref="P:System.Type.IsByRef"/> returns <see langword="true"/>.</exception>
        /// <remarks>
        /// <note type="note">
        /// For backward compatibility, this constructor demands <see cref="System.Security.Permissions.SecurityPermission"/> with the <see cref="System.Security.Permissions.SecurityPermissionFlag.ControlEvidence">ControlEvidence</see> flag if the following conditions are both true: <c>m</c> is a module other than the calling module, and the demand for <see cref="System.Security.Permissions.ReflectionPermission"/> with the <see cref="System.Security.Permissions.ReflectionPermissionFlag.MemberAccess">MemberAccess</see> flag has failed. If the demand for <see cref="System.Security.Permissions.SecurityPermission"/> succeeds, the operation is allowed.
        /// </note>
        /// The dynamic method created with this constructor has access to public and <c>internal</c> (<c>Friend</c> in Visual Basic) members of all the public and internal types contained in module <c>m</c>.
        /// Skipping the JIT compiler's visibility checks allows the dynamic method to access private and protected members of all other types in the module and in all other assemblies as well. This is useful, for example, when writing code to serialize objects.
        /// </remarks>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/how-to-define-and-execute-dynamic-methods">How to: Define and Execute Dynamic Methods</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit">Security Issues in Reflection Emit</related>
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
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null"/>.</param>
        /// <param name="returnType">A <see cref="T:System.Type"/> object that specifies the return type of the dynamic method, or <see langword="null"/> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects specifying the types of the parameters of the dynamic method, or <see langword="null"/> if the method has no parameters.</param>
        /// <param name="owner">A <see cref="T:System.Type"/> with which the dynamic method is logically associated. The dynamic method has access to all members of the type.</param>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="parameterTypes"/> is <see langword="null"/> or <see cref="T:System.Void"/>. -or- <paramref name="owner"/> is an interface, an array, an open generic type, or a type parameter of a generic type or method.</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="name"/> is <see langword="null"/>. -or- <paramref name="owner"/> is <see langword="null"/>.</exception>
        /// <exception cref="T:System.NotSupportedException">.NET Framework and .NET Core versions older than 2.1: <paramref name="returnType"/> is a type for which <see cref="P:System.Type.IsByRef"/> returns <see langword="true"/>.</exception>
        /// <remarks>
        /// <note type="note">
        /// For backward compatibility, this constructor demands <see cref="System.Security.Permissions.SecurityPermission"/> with the <see cref="System.Security.Permissions.SecurityPermissionFlag.ControlEvidence">ControlEvidence</see> flag if the following conditions are both true: <c>owner</c> is in a module other than the calling module, and the demand for <see cref="System.Security.Permissions.ReflectionPermission"/> with the <see cref="System.Security.Permissions.ReflectionPermissionFlag.MemberAccess">MemberAccess</see> flag has failed. If the demand for <see cref="System.Security.Permissions.SecurityPermission"/> succeeds, the operation is allowed.
        /// </note>
        /// <note type="note">
        /// In general, changing the internal fields of classes is not good object-oriented coding practice.
        /// </note>
        /// <note type="note">
        /// This is an example of the relaxed rules for delegate binding introduced in .NET Framework 2.0, along with new overloads of the <see cref="System.Delegate.CreateDelegate">CreateDelegate</see> method. For more information, see the <see cref="System.Delegate"/> class.
        /// </note>
        /// The dynamic method created with this constructor has access to all members of the type <c>owner</c>, and to public and <c>internal</c> (<c>Friend</c> in Visual Basic) members of all the other types in the module that contains <c>owner</c>.
        /// This constructor specifies method attributes <see cref="System.Reflection.MethodAttributes.Public">Public</see> and <see cref="System.Reflection.MethodAttributes.Static">Static</see>, calling convention <see cref="System.Reflection.CallingConventions.Standard">Standard</see>, and does not skip just-in-time (JIT) visibility checks.
        /// The following code example creates a <see cref="System.Reflection.Emit.DynamicMethod"/> that is logically associated with a type. This association gives it access to the private members of that type.
        /// The code example defines a class named <c>Example</c> with a private field, a class named <c>DerivedFromExample</c> that derives from the first class, a delegate type named <c>UseLikeStatic</c> that returns <see cref="System.Int32"/> and has parameters of type <c>Example</c> and <see cref="System.Int32"/>, and a delegate type named <c>UseLikeInstance</c> that returns <see cref="System.Int32"/> and has one parameter of type <see cref="System.Int32"/>.
        /// The example code then creates a <see cref="System.Reflection.Emit.DynamicMethod"/> that changes the private field of an instance of <c>Example</c> and returns the previous value.
        /// The example code creates an instance of <c>Example</c> and then creates two delegates. The first is of type <c>UseLikeStatic</c>, which has the same parameters as the dynamic method. The second is of type <c>UseLikeInstance</c>, which lacks the first parameter (of type <c>Example</c>). This delegate is created using the <see cref="System.Reflection.Emit.DynamicMethod.CreateDelegate%28System.Type%2CSystem.Object%29"/> method overload; the second parameter of that method overload is an instance of <c>Example</c>, in this case the instance just created, which is bound to the newly created delegate. Whenever that delegate is invoked, the dynamic method acts on the bound instance of <c>Example</c>.
        /// The <c>UseLikeStatic</c> delegate is invoked, passing in the instance of <c>Example</c> that is bound to the <c>UseLikeInstance</c> delegate. Then the <c>UseLikeInstance</c> delegate is invoked, so that both delegates act on the same instance of <c>Example</c>. The changes in the values of the internal field are displayed after each call. Finally, a <c>UseLikeInstance</c> delegate is bound to an instance of <c>DerivedFromExample</c>, and the delegate calls are repeated.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.CtorOwnerType.cs" region="OwnerTypeAccess" title="Creating a DynamicMethod with an owner type" />
        /// </example>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/how-to-define-and-execute-dynamic-methods">How to: Define and Execute Dynamic Methods</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit">Security Issues in Reflection Emit</related>
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
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null"/>.</param>
        /// <param name="returnType">A <see cref="T:System.Type"/> object that specifies the return type of the dynamic method, or <see langword="null"/> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects specifying the types of the parameters of the dynamic method, or <see langword="null"/> if the method has no parameters.</param>
        /// <param name="owner">A <see cref="T:System.Type"/> with which the dynamic method is logically associated. The dynamic method has access to all members of the type.</param>
        /// <param name="skipVisibility"><see langword="true"/> to skip JIT visibility checks on types and members accessed by the MSIL of the dynamic method; otherwise, <see langword="false"/>.</param>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="parameterTypes"/> is <see langword="null"/> or <see cref="T:System.Void"/>. -or- <paramref name="owner"/> is an interface, an array, an open generic type, or a type parameter of a generic type or method.</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="name"/> is <see langword="null"/>. -or- <paramref name="owner"/> is <see langword="null"/>.</exception>
        /// <exception cref="T:System.NotSupportedException">.NET Framework and .NET Core versions older than 2.1: <paramref name="returnType"/> is a type for which <see cref="P:System.Type.IsByRef"/> returns <see langword="true"/>.</exception>
        /// <remarks>
        /// <note type="note">
        /// For backward compatibility, this constructor demands <see cref="System.Security.Permissions.SecurityPermission"/> with the <see cref="System.Security.Permissions.SecurityPermissionFlag.ControlEvidence">ControlEvidence</see> flag if the following conditions are both true: <c>owner</c> is in a module other than the calling module, and the demand for <see cref="System.Security.Permissions.ReflectionPermission"/> with the <see cref="System.Security.Permissions.ReflectionPermissionFlag.MemberAccess">MemberAccess</see> flag has failed. If the demand for <see cref="System.Security.Permissions.SecurityPermission"/> succeeds, the operation is allowed.
        /// </note>
        /// The dynamic method created with this constructor has access to all members of the type <c>owner</c>, and to public and <c>internal</c> (<c>Friend</c> in Visual Basic) members of all the other types in the module that contains <c>owner</c>. Skipping the JIT compiler's visibility checks allows the dynamic method to access private and protected members of all other types as well. This is useful, for example, when writing code to serialize objects.
        /// This constructor specifies method attributes <see cref="System.Reflection.MethodAttributes.Public">Public</see> and <see cref="System.Reflection.MethodAttributes.Static">Static</see>, and calling convention <see cref="System.Reflection.CallingConventions.Standard">Standard</see>.
        /// </remarks>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/how-to-define-and-execute-dynamic-methods">How to: Define and Execute Dynamic Methods</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit">Security Issues in Reflection Emit</related>
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
        /// <param name="name">The name of the dynamic method. This can be a zero-length string, but it cannot be <see langword="null"/>.</param>
        /// <param name="attributes">A bitwise combination of <see cref="T:System.Reflection.MethodAttributes"/> values that specifies the attributes of the dynamic method. The only combination allowed is <see cref="F:System.Reflection.MethodAttributes.Public"/> and <see cref="F:System.Reflection.MethodAttributes.Static"/>.</param>
        /// <param name="callingConvention">The calling convention for the dynamic method. Must be <see cref="F:System.Reflection.CallingConventions.Standard"/>.</param>
        /// <param name="returnType">A <see cref="T:System.Type"/> object that specifies the return type of the dynamic method, or <see langword="null"/> if the method has no return type.</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects specifying the types of the parameters of the dynamic method, or <see langword="null"/> if the method has no parameters.</param>
        /// <param name="owner">A <see cref="T:System.Type"/> with which the dynamic method is logically associated. The dynamic method has access to all members of the type.</param>
        /// <param name="skipVisibility"><see langword="true"/> to skip JIT visibility checks on types and members accessed by the MSIL of the dynamic method; otherwise, <see langword="false"/>.</param>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="parameterTypes"/> is <see langword="null"/> or <see cref="T:System.Void"/>. -or- <paramref name="owner"/> is an interface, an array, an open generic type, or a type parameter of a generic type or method.</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="name"/> is <see langword="null"/>. -or- <paramref name="owner"/> is <see langword="null"/>.</exception>
        /// <exception cref="T:System.NotSupportedException"><paramref name="attributes"/> is a combination of flags other than <see cref="F:System.Reflection.MethodAttributes.Public"/> and <see cref="F:System.Reflection.MethodAttributes.Static"/>. -or- <paramref name="callingConvention"/> is not <see cref="F:System.Reflection.CallingConventions.Standard"/>. -or- <paramref name="returnType"/> is a type for which <see cref="P:System.Type.IsByRef"/> returns <see langword="true"/>.</exception>
        /// <remarks>
        /// <note type="note">
        /// For backward compatibility, this constructor demands <see cref="System.Security.Permissions.SecurityPermission"/> with the <see cref="System.Security.Permissions.SecurityPermissionFlag.ControlEvidence">ControlEvidence</see> flag if the following conditions are both true: <c>owner</c> is in a module other than the calling module, and the demand for <see cref="System.Security.Permissions.ReflectionPermission"/> with the <see cref="System.Security.Permissions.ReflectionPermissionFlag.MemberAccess">MemberAccess</see> flag has failed. If the demand for <see cref="System.Security.Permissions.SecurityPermission"/> succeeds, the operation is allowed.
        /// </note>
        /// The dynamic method is global to the module that contains the type <c>owner</c>. It has access to all members of the type <c>owner</c>.
        /// The dynamic method created with this constructor has access to all members of the type <c>owner</c>, and to public and <c>internal</c> (<c>Friend</c> in Visual Basic) members of all the types contained in the module that contains <c>owner</c>. Skipping the JIT compiler's visibility checks allows the dynamic method to access private and protected members of all other types as well. This is useful, for example, when writing code to serialize objects.
        /// </remarks>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/how-to-define-and-execute-dynamic-methods">How to: Define and Execute Dynamic Methods</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit">Security Issues in Reflection Emit</related>
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
        /// Returns the signature of the method, represented as a string.
        /// </summary>
        /// <returns>A string representing the method signature.</returns>
        /// <remarks>
        /// The signature includes only types and the method name, if any. Parameter names are not included.
        /// The following code example displays the <see cref="System.Reflection.Emit.DynamicMethod.ToString"/> method of a dynamic method. This code example is part of a larger example provided for the <see cref="System.Reflection.Emit.DynamicMethod"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="ToString" title="Getting the string representation of a DynamicMethod" />
        /// </example>
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
        /// <value>The simple name of the method.</value>
        /// <remarks>
        /// <note type="note">
        /// It is not necessary to name dynamic methods.
        /// </note>
        /// The following code example displays the name of a dynamic method. This code example is part of a larger example provided for  the <see cref="System.Reflection.Emit.DynamicMethod"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="Name" title="Getting the name of a DynamicMethod" />
        /// </example>
        public override string Name => _name;

        /// <summary>
        /// Gets the type that declares the method, which is always <see langword="null"/> for dynamic methods.
        /// </summary>
        /// <value>Always <see langword="null"/>.</value>
        /// <remarks>
        /// This property always returns <c>null</c> for dynamic methods. Even when a dynamic method is logically associated with a type, it is not declared by the type.
        /// The following code example displays the declaring type of a dynamic method. This code example is part of a larger example provided for the <see cref="System.Reflection.Emit.DynamicMethod"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="DeclaringType" title="Getting the declaring type of a DynamicMethod" />
        /// </example>
        public override Type? DeclaringType => null;

        /// <summary>
        /// Gets the class that was used in reflection to obtain the method.
        /// </summary>
        /// <value>Always <see langword="null"/>.</value>
        /// <remarks>
        /// This property always returns <c>null</c> for dynamic methods.
        /// The following code example displays the reflected type of a dynamic method. This code example is part of a larger example provided for the <see cref="System.Reflection.Emit.DynamicMethod"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="ReflectedType" title="Getting the reflected type of a DynamicMethod" />
        /// </example>
        public override Type? ReflectedType => null;

        /// <summary>
        /// Gets the module with which the dynamic method is logically associated.
        /// </summary>
        /// <value>The <see cref="T:System.Reflection.Module"/> with which the current dynamic method is associated.</value>
        /// <remarks>
        /// If a module was specified when the dynamic method was created, this property returns that module. If a type was specified as the owner when the dynamic method was created, this property returns the module that contains that type.
        /// The following code example displays the <see cref="System.Reflection.Emit.DynamicMethod.Module"/> property of a dynamic method. This code example is part of a larger example provided for the <see cref="System.Reflection.Emit.DynamicMethod"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="Module" title="Getting the module of a DynamicMethod" />
        /// </example>
        public override Module Module => _module;

        // we cannot return a MethodHandle because we cannot track it via GC so this method is off limits
        /// <summary>
        /// Not supported for dynamic methods.
        /// </summary>
        /// <value>Not supported for dynamic methods.</value>
        /// <exception cref="T:System.InvalidOperationException">Not allowed for dynamic methods.</exception>
        public override RuntimeMethodHandle MethodHandle => throw new InvalidOperationException(SR.InvalidOperation_NotAllowedInDynamicMethod);

        /// <summary>
        /// Gets the attributes specified when the dynamic method was created.
        /// </summary>
        /// <value>A bitwise combination of the <see cref="T:System.Reflection.MethodAttributes"/> values representing the attributes for the method.</value>
        /// <remarks>
        /// Currently, the method attributes for a dynamic method are always <see cref="System.Reflection.MethodAttributes.Public"/> and <see cref="System.Reflection.MethodAttributes.Static"/>.
        /// The following code example displays the method attributes of a dynamic method. This code example is part of a larger example provided for the <see cref="System.Reflection.Emit.DynamicMethod"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="Attributes" title="Getting the attributes of a DynamicMethod" />
        /// </example>
        public override MethodAttributes Attributes => _attributes;

        /// <summary>
        /// Gets the calling convention specified when the dynamic method was created.
        /// </summary>
        /// <value>One of the <see cref="T:System.Reflection.CallingConventions"/> values that indicates the calling convention of the method.</value>
        /// <remarks>
        /// Currently, the calling convention for a dynamic method is always <see cref="System.Reflection.CallingConventions.Standard"/>.
        /// The following code example displays the calling convention of a dynamic method. This code example is part of a larger example provided for the <see cref="System.Reflection.Emit.DynamicMethod"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="CallingConvention" title="Getting the calling convention of a DynamicMethod" />
        /// </example>
        public override CallingConventions CallingConvention => _callingConvention;

        /// <summary>
        /// Returns the base implementation for the method.
        /// </summary>
        /// <returns>The base implementation of the method.</returns>
        /// <remarks>
        /// This method always returns the current <c>DynamicMethod</c> object.
        /// </remarks>
        public override MethodInfo GetBaseDefinition() => this;

        /// <summary>
        /// Returns the parameters of the dynamic method.
        /// </summary>
        /// <returns>An array of <see cref="T:System.Reflection.ParameterInfo"/> objects that represent the parameters of the dynamic method.</returns>
        /// <remarks>
        /// The <see cref="System.Reflection.ParameterInfo"/> objects returned by this method are for information only. Use the <see cref="System.Reflection.Emit.DynamicMethod.DefineParameter"/> method to set or change the characteristics of the parameters.
        /// The following code example displays the parameters of a dynamic method. This code example is part of a larger example provided for the <see cref="System.Reflection.Emit.DynamicMethod"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="GetParameters" title="Getting parameter info from a DynamicMethod" />
        /// </example>
        public override ParameterInfo[] GetParameters() =>
            GetParametersAsSpan().ToArray();

        internal override ReadOnlySpan<ParameterInfo> GetParametersAsSpan() => LoadParameters();

        /// <summary>
        /// Returns the implementation flags for the method.
        /// </summary>
        /// <returns>A bitwise combination of <see cref="T:System.Reflection.MethodImplAttributes"/> values representing the implementation flags for the method.</returns>
        /// <remarks>
        /// Currently, method implementation attributes for dynamic methods are always <see cref="System.Reflection.MethodImplAttributes.IL"/> and <see cref="System.Reflection.MethodImplAttributes.NoInlining"/>.
        /// </remarks>
        public override MethodImplAttributes GetMethodImplementationFlags() =>
            MethodImplAttributes.IL | MethodImplAttributes.NoInlining;

        /// <summary>
        /// Gets a value that indicates whether the current dynamic method is security-critical or security-safe-critical, and therefore can perform critical operations.
        /// </summary>
        /// <value><see langword="true"/> if the current dynamic method is security-critical or security-safe-critical; <see langword="false"/> if it is transparent.</value>
        /// <exception cref="T:System.InvalidOperationException">The dynamic method doesn't have a method body.</exception>
        /// <remarks>
        /// <note type="note">
        /// </note>
        /// The <see cref="System.Reflection.Emit.DynamicMethod.IsSecurityCritical"/>, <see cref="System.Reflection.Emit.DynamicMethod.IsSecuritySafeCritical"/>, and <see cref="System.Reflection.Emit.DynamicMethod.IsSecurityTransparent"/> properties report the transparency level of the dynamic method as determined by the common language runtime (CLR). The combinations of these properties are shown in the following table:
        /// |Security level|IsSecurityCritical|IsSecuritySafeCritical|IsSecurityTransparent|
        /// |--------------------|------------------------|----------------------------|---------------------------|
        /// |Critical|<c>true</c>|<c>false</c>|<c>false</c>|
        /// |Safe critical|<c>true</c>|<c>true</c>|<c>false</c>|
        /// |Transparent|<c>false</c>|<c>false</c>|<c>true</c>|
        /// Using these properties is much simpler than examining the security annotations of an assembly and its types, checking the current trust level, and attempting to duplicate the runtime's rules.
        /// The transparency of a dynamic method depends on the module it is associated with. If the dynamic method is associated with a type rather than a module, its transparency depends on the module that contains the type. Dynamic methods do not have security annotations, so they are assigned the default transparency for the associated module.
        /// - Anonymously hosted dynamic methods are always transparent, because the system-provided module that contains them is transparent.
        /// - The transparency of a dynamic method that is associated with a trusted assembly (that is, a strong-named assembly that is installed in the global assembly cache) is described in the following table.
        /// |Assembly annotation|Level 1 transparency|Level 2 transparency|
        /// |-------------------------|--------------------------|--------------------------|
        /// |Fully transparent|Transparent|Transparent|
        /// |Fully critical|Critical|Critical|
        /// |Mixed transparency|Transparent|Transparent|
        /// |Security-agnostic|Safe-critical|Critical|
        /// For example, if you associate a dynamic method with a type that is in mscorlib.dll, which has level 2 mixed transparency, the dynamic method is transparent and cannot execute critical code. For information about transparency levels, see [Security-Transparent Code, Level 1](/dotnet/framework/misc/security-transparent-code-level-1) and [Security-Transparent Code, Level 2](/dotnet/framework/misc/security-transparent-code-level-2).
        /// >  Associating a dynamic method with a module in a trusted level 1 assembly that is security-agnostic, such as System.dll, does not permit elevation of trust. If the grant set of the code that calls the dynamic method does not include the grant set of System.dll (that is, full trust), <see cref="System.Security.SecurityException"/> is thrown when the dynamic method is called.
        /// - The transparency of a dynamic method that is associated with a partially trusted assembly depends on how the assembly is loaded. If the assembly is loaded with partial trust (for example, into a sandboxed application domain), the runtime ignores the security annotations of the assembly. The assembly and all its types and members, including dynamic methods, are treated as transparent. The runtime pays attention to security annotations only if the partial-trust assembly is loaded with full trust (for example, into the default application domain of a desktop application). In that case, the runtime assigns the dynamic method the default transparency for methods according to the assembly's annotations.
        /// For more information about reflection emit and transparency, see [Security Issues in Reflection Emit](/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit). For information about transparency, see [Security Changes](/dotnet/framework/security/security-changes).
        /// </remarks>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit">Security Issues in Reflection Emit</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-considerations-for-reflection">Security Considerations for Reflection</related>
        /// <related type="Article" href="/dotnet/framework/security/security-changes">Security Changes in the .NET Framework Version 4.0</related>
        /// <related type="Article" href="/dotnet/framework/misc/security-transparent-code-level-1">Security-Transparent Code, Level 1</related>
        /// <related type="Article" href="/dotnet/framework/misc/security-transparent-code-level-2">Security-Transparent Code, Level 2</related>
        public override bool IsSecurityCritical => true;

        /// <summary>
        /// Gets a value that indicates whether the current dynamic method is security-safe-critical at the current trust level; that is, whether it can perform critical operations and can be accessed by transparent code.
        /// </summary>
        /// <value><see langword="true"/> if the dynamic method is security-safe-critical at the current trust level; <see langword="false"/> if it is security-critical or transparent.</value>
        /// <exception cref="T:System.InvalidOperationException">The dynamic method doesn't have a method body.</exception>
        /// <remarks>
        /// <note type="note">
        /// </note>
        /// The <see cref="System.Reflection.Emit.DynamicMethod.IsSecurityCritical"/>, <see cref="System.Reflection.Emit.DynamicMethod.IsSecuritySafeCritical"/>, and <see cref="System.Reflection.Emit.DynamicMethod.IsSecurityTransparent"/> properties report the transparency level of the dynamic method as determined by the common language runtime (CLR). The combinations of these properties are shown in the following table:
        /// |Security level|IsSecurityCritical|IsSecuritySafeCritical|IsSecurityTransparent|
        /// |--------------------|------------------------|----------------------------|---------------------------|
        /// |Critical|<c>true</c>|<c>false</c>|<c>false</c>|
        /// |Safe critical|<c>true</c>|<c>true</c>|<c>false</c>|
        /// |Transparent|<c>false</c>|<c>false</c>|<c>true</c>|
        /// Using these properties is much simpler than examining the security annotations of an assembly and its types, checking the current trust level, and attempting to duplicate the runtime's rules.
        /// The transparency of a dynamic method depends on the module it is associated with. If the dynamic method is associated with a type rather than a module, its transparency depends on the module that contains the type. Dynamic methods do not have security annotations, so they are assigned the default transparency for the associated module.
        /// - Anonymously hosted dynamic methods are always transparent, because the system-provided module that contains them is transparent.
        /// - The transparency of a dynamic method that is associated with a trusted assembly (that is, a strong-named assembly that is installed in the global assembly cache) is described in the following table.
        /// |Assembly annotation|Level 1 transparency|Level 2 transparency|
        /// |-------------------------|--------------------------|--------------------------|
        /// |Fully transparent|Transparent|Transparent|
        /// |Fully critical|Critical|Critical|
        /// |Mixed transparency|Transparent|Transparent|
        /// |Security-agnostic|Safe-critical|Critical|
        /// For example, if you associate a dynamic method with a type that is in mscorlib.dll, which has level 2 mixed transparency, the dynamic method is transparent and cannot execute critical code. For information about transparency levels, see [Security-Transparent Code, Level 1](/dotnet/framework/misc/security-transparent-code-level-1) and [Security-Transparent Code, Level 2](/dotnet/framework/misc/security-transparent-code-level-2).
        /// >  Associating a dynamic method with a module in a trusted level 1 assembly that is security-agnostic, such as System.dll, does not permit elevation of trust. If the grant set of the code that calls the dynamic method does not include the grant set of System.dll (that is, full trust), <see cref="System.Security.SecurityException"/> is thrown when the dynamic method is called.
        /// - The transparency of a dynamic method that is associated with a partially trusted assembly depends on how the assembly is loaded. If the assembly is loaded with partial trust (for example, into a sandboxed application domain), the runtime ignores the security annotations of the assembly. The assembly and all its types and members, including dynamic methods, are treated as transparent. The runtime pays attention to security annotations only if the partial-trust assembly is loaded with full trust (for example, into the default application domain of a desktop application). In that case, the runtime assigns the dynamic method the default transparency for methods according to the assembly's annotations.
        /// For more information about reflection emit and transparency, see [Security Issues in Reflection Emit](/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit). For information about transparency, see [Security Changes](/dotnet/framework/security/security-changes).
        /// </remarks>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit">Security Issues in Reflection Emit</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-considerations-for-reflection">Security Considerations for Reflection</related>
        /// <related type="Article" href="/dotnet/framework/security/security-changes">Security Changes in the .NET Framework Version 4.0</related>
        /// <related type="Article" href="/dotnet/framework/misc/security-transparent-code-level-1">Security-Transparent Code, Level 1</related>
        /// <related type="Article" href="/dotnet/framework/misc/security-transparent-code-level-2">Security-Transparent Code, Level 2</related>
        public override bool IsSecuritySafeCritical => false;

        /// <summary>
        /// Gets a value that indicates whether the current dynamic method is transparent at the current trust level, and therefore cannot perform critical operations.
        /// </summary>
        /// <value><see langword="true"/> if the dynamic method is security-transparent at the current trust level; otherwise, <see langword="false"/>.</value>
        /// <exception cref="T:System.InvalidOperationException">The dynamic method doesn't have a method body.</exception>
        /// <remarks>
        /// <note type="note">
        /// </note>
        /// The <see cref="System.Reflection.Emit.DynamicMethod.IsSecurityCritical"/>, <see cref="System.Reflection.Emit.DynamicMethod.IsSecuritySafeCritical"/>, and <see cref="System.Reflection.Emit.DynamicMethod.IsSecurityTransparent"/> properties report the transparency level of the dynamic method as determined by the common language runtime (CLR). The combinations of these properties are shown in the following table:
        /// |Security level|IsSecurityCritical|IsSecuritySafeCritical|IsSecurityTransparent|
        /// |--------------------|------------------------|----------------------------|---------------------------|
        /// |Critical|<c>true</c>|<c>false</c>|<c>false</c>|
        /// |Safe critical|<c>true</c>|<c>true</c>|<c>false</c>|
        /// |Transparent|<c>false</c>|<c>false</c>|<c>true</c>|
        /// Using these properties is much simpler than examining the security annotations of an assembly and its types, checking the current trust level, and attempting to duplicate the runtime's rules.
        /// The transparency of a dynamic method depends on the module it is associated with. If the dynamic method is associated with a type rather than a module, its transparency depends on the module that contains the type. Dynamic methods do not have security annotations, so they are assigned the default transparency for the associated module.
        /// - Anonymously hosted dynamic methods are always transparent, because the system-provided module that contains them is transparent.
        /// - The transparency of a dynamic method that is associated with a trusted assembly (that is, a strong-named assembly that is installed in the global assembly cache) is described in the following table.
        /// |Assembly annotation|Level 1 transparency|Level 2 transparency|
        /// |-------------------------|--------------------------|--------------------------|
        /// |Fully transparent|Transparent|Transparent|
        /// |Fully critical|Critical|Critical|
        /// |Mixed transparency|Transparent|Transparent|
        /// |Security-agnostic|Safe-critical|Critical|
        /// For example, if you associate a dynamic method with a type that is in mscorlib.dll, which has level 2 mixed transparency, the dynamic method is transparent and cannot execute critical code. For information about transparency levels, see [Security-Transparent Code, Level 1](/dotnet/framework/misc/security-transparent-code-level-1) and [Security-Transparent Code, Level 2](/dotnet/framework/misc/security-transparent-code-level-2).
        /// >  Associating a dynamic method with a module in a trusted level 1 assembly that is security-agnostic, such as System.dll, does not permit elevation of trust. If the grant set of the code that calls the dynamic method does not include the grant set of System.dll (that is, full trust), <see cref="System.Security.SecurityException"/> is thrown when the dynamic method is called.
        /// - The transparency of a dynamic method that is associated with a partially trusted assembly depends on how the assembly is loaded. If the assembly is loaded with partial trust (for example, into a sandboxed application domain), the runtime ignores the security annotations of the assembly. The assembly and all its types and members, including dynamic methods, are treated as transparent. The runtime pays attention to security annotations only if the partial-trust assembly is loaded with full trust (for example, into the default application domain of a desktop application). In that case, the runtime assigns the dynamic method the default transparency for methods according to the assembly's annotations.
        /// For more information about reflection emit and transparency, see [Security Issues in Reflection Emit](/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit). For information about transparency, see [Security Changes](/dotnet/framework/security/security-changes).
        /// </remarks>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-issues-in-reflection-emit">Security Issues in Reflection Emit</related>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/security-considerations-for-reflection">Security Considerations for Reflection</related>
        /// <related type="Article" href="/dotnet/framework/security/security-changes">Security Changes in the .NET Framework Version 4.0</related>
        /// <related type="Article" href="/dotnet/framework/misc/security-transparent-code-level-1">Security-Transparent Code, Level 1</related>
        /// <related type="Article" href="/dotnet/framework/misc/security-transparent-code-level-2">Security-Transparent Code, Level 2</related>
        public override bool IsSecurityTransparent => false;

        /// <summary>
        /// Returns the custom attributes of the specified type that have been applied to the method.
        /// </summary>
        /// <param name="attributeType">A <see cref="T:System.Type"/> representing the type of custom attribute to return.</param>
        /// <param name="inherit"><see langword="true"/> to search the method's inheritance chain to find the custom attributes; <see langword="false"/> to check only the current method.</param>
        /// <returns>An array of objects representing the attributes of the method that are of type <paramref name="attributeType"/> or derive from type <paramref name="attributeType"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="attributeType"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <note type="note">
        /// Custom attributes are not currently supported on dynamic methods. The only attribute returned is <see cref="System.Runtime.CompilerServices.MethodImplAttribute"/>; you can get the method implementation flags more easily using the <see cref="System.Reflection.Emit.DynamicMethod.GetMethodImplementationFlags"/> method.
        /// </note>
        /// For dynamic methods, specifying <c>true</c> for <c>inherit</c> has no effect, because the method is not declared in a type.
        /// </remarks>
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
        /// Returns all the custom attributes defined for the method.
        /// </summary>
        /// <param name="inherit"><see langword="true"/> to search the method's inheritance chain to find the custom attributes; <see langword="false"/> to check only the current method.</param>
        /// <returns>An array of objects representing all the custom attributes of the method.</returns>
        /// <remarks>
        /// <note type="note">
        /// Custom attributes are not currently supported on dynamic methods. The only attribute returned is <see cref="System.Runtime.CompilerServices.MethodImplAttribute"/>; you can get the method implementation flags more easily using the <see cref="System.Reflection.Emit.DynamicMethod.GetMethodImplementationFlags"/> method.
        /// </note>
        /// For dynamic methods, specifying <c>true</c> for <c>inherit</c> has no effect, because the method is not declared in a type.
        /// </remarks>
        public override object[] GetCustomAttributes(bool inherit)
        {
            // support for MethodImplAttribute PCA
            return [new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags())];
        }

        /// <summary>
        /// Indicates whether the specified custom attribute type is defined.
        /// </summary>
        /// <param name="attributeType">A <see cref="T:System.Type"/> representing the type of custom attribute to search for.</param>
        /// <param name="inherit"><see langword="true"/> to search the method's inheritance chain to find the custom attributes; <see langword="false"/> to check only the current method.</param>
        /// <returns><see langword="true"/> if the specified custom attribute type is defined; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <note type="note">
        /// Custom attributes are not currently supported on dynamic methods.
        /// </note>
        /// For dynamic methods, specifying <c>true</c> for <c>inherit</c> has no effect. Dynamic methods have no inheritance chain.
        /// </remarks>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            return attributeType.IsAssignableFrom(typeof(MethodImplAttribute));
        }

        /// <summary>
        /// Gets the type of return value for the dynamic method.
        /// </summary>
        /// <value>A <see cref="T:System.Type"/> representing the type of the return value of the current method; <see cref="T:System.Void"/> if the method has no return type.</value>
        /// <remarks>
        /// If <c>null</c> was specified for the return type when the dynamic method was created, this property returns <see cref="System.Void">Void</see>.
        /// The following code example displays the return type of a dynamic method. This code example is part of a larger example provided for the <see cref="System.Reflection.Emit.DynamicMethod"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="ReturnType" title="Getting the return type of a DynamicMethod" />
        /// </example>
        public override Type ReturnType => _returnType;

        /// <summary>
        /// Gets the return parameter of the dynamic method.
        /// </summary>
        /// <value>Always <see langword="null"/>.</value>
        /// <remarks>
        /// This property always returns <c>null</c> for dynamic methods.
        /// </remarks>
        public override ParameterInfo ReturnParameter => new RuntimeParameterInfo(this, null, _returnType, -1);

        /// <summary>
        /// Gets the custom attributes of the return type for the dynamic method.
        /// </summary>
        /// <value>An <see cref="T:System.Reflection.ICustomAttributeProvider"/> representing the custom attributes of the return type for the dynamic method.</value>
        /// <remarks>
        /// Custom attributes are not supported on the return type of a dynamic method, so the array of custom attributes returned by the <see cref="System.Reflection.ICustomAttributeProvider.GetCustomAttributes"/> method is always empty.
        /// The following code example shows how to display the custom attributes of the return type of a dynamic method. This code example is part of a larger example provided for the <see cref="System.Reflection.Emit.DynamicMethod"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="ReturnTypeCustomAttributes" title="Getting return type custom attributes" />
        /// </example>
        public override ICustomAttributeProvider ReturnTypeCustomAttributes => new EmptyCAHolder();

        //
        // DynamicMethod specific methods
        //

        /// <summary>
        /// Defines a parameter of the dynamic method.
        /// </summary>
        /// <param name="position">The position of the parameter in the parameter list. Parameters are indexed beginning with the number 1 for the first parameter.</param>
        /// <param name="attributes">A bitwise combination of <see cref="T:System.Reflection.ParameterAttributes"/> values that specifies the attributes of the parameter.</param>
        /// <param name="parameterName">The name of the parameter. The name can be a zero-length string.</param>
        /// <returns>Always returns <see langword="null"/>.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The method has no parameters. -or- <paramref name="position"/> is less than 0. -or- <paramref name="position"/> is greater than the number of the method's parameters.</exception>
        /// <remarks>
        /// If <c>position</c> is 0, the <see cref="System.Reflection.Emit.DynamicMethod.DefineParameter"/> method refers to the return value. Setting parameter information has no effect on the return value.
        /// If the dynamic method has already been completed, by calling the <see cref="System.Reflection.Emit.DynamicMethod.CreateDelegate"/> or <see cref="System.Reflection.Emit.DynamicMethod.Invoke"/> method, the <see cref="System.Reflection.Emit.DynamicMethod.DefineParameter"/> method has no effect. No exception is thrown.
        /// The following code example shows how to define parameter information for a dynamic method. This code example is part of a larger example provided for the <see cref="System.Reflection.Emit.DynamicMethod"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="DefineParameter" title="Defining parameters on a DynamicMethod" />
        /// </example>
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
        /// <returns>An <see cref="T:System.Reflection.Emit.ILGenerator"/> object for the method.</returns>
        /// <remarks>
        /// <note type="note">
        /// There are restrictions on unverifiable code in dynamic methods, even in some full-trust scenarios. See the "Verification" section in Remarks for <see cref="System.Reflection.Emit.DynamicMethod"/>.
        /// </note>
        /// After a dynamic method has been completed, by calling the <see cref="System.Reflection.Emit.DynamicMethod.CreateDelegate"/> or <see cref="System.Reflection.Emit.DynamicMethod.Invoke"/> method, any further attempt to add MSIL is ignored. No exception is thrown.
        /// The following code example creates a dynamic method that takes two parameters. The example emits a simple function body that prints the first parameter to the console, and the example uses the second parameter as the return value of the method. The example completes the method by creating a delegate, invokes the delegate with different parameters, and finally invokes the dynamic method using the <see cref="System.Reflection.Emit.DynamicMethod.Invoke"/> method.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="GetILGenerator" title="Getting the IL generator for a DynamicMethod" />
        /// </example>
        /// <related type="Article" href="/dotnet/framework/reflection-and-codedom/how-to-define-and-execute-dynamic-methods">How to: Define and Execute Dynamic Methods</related>
        public ILGenerator GetILGenerator()
        {
            return GetILGenerator(64);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the local variables in the method are zero-initialized.
        /// </summary>
        /// <value><see langword="true"/> if the local variables in the method are zero-initialized; otherwise, <see langword="false"/>. The default is <see langword="true"/>.</value>
        /// <remarks>
        /// If this property is set to <c>true</c>, the emitted Microsoft intermediate language (MSIL) includes initialization of local variables. If it is set to <c>false</c>, local variables are not initialized and the generated code is unverifiable.
        /// The following code example displays the <see cref="System.Reflection.Emit.DynamicMethod.InitLocals"/> property of a dynamic method. This code example is part of a larger example provided for the <see cref="System.Reflection.Emit.DynamicMethod"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="cs" source="../../../../samples/System/Reflection/Emit/DynamicMethod.Examples.cs" region="InitLocals" title="Checking InitLocals on a DynamicMethod" />
        /// </example>
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
