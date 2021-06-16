// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Globalization;
using System.Runtime.Loader;
using System.Runtime.Remoting;
using System.Threading;

namespace System
{
    public static partial class Activator
    {
        //
        // Note: CreateInstance returns null for Nullable<T>, e.g. CreateInstance(typeof(int?)) returns null.
        //

        public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (type is System.Reflection.Emit.TypeBuilder)
                throw new NotSupportedException(SR.NotSupported_CreateInstanceWithTypeBuilder);

            // If they didn't specify a lookup, then we will provide the default lookup.
            const int LookupMask = 0x000000FF;
            if ((bindingAttr & (BindingFlags)LookupMask) == 0)
                bindingAttr |= ConstructorDefault;

            if (activationAttributes?.Length > 0)
                throw new PlatformNotSupportedException(SR.NotSupported_ActivAttr);

            if (type.UnderlyingSystemType is RuntimeType rt)
                return rt.CreateInstanceImpl(bindingAttr, binder, args, culture);

            throw new ArgumentException(SR.Arg_MustBeType, nameof(type));
        }

        [System.Security.DynamicSecurityMethod]
        [RequiresUnreferencedCode("Type and its constructor could be removed")]
        public static ObjectHandle? CreateInstance(string assemblyName, string typeName)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstanceInternal(assemblyName,
                                          typeName,
                                          false,
                                          ConstructorDefault,
                                          null,
                                          null,
                                          null,
                                          null,
                                          ref stackMark);
        }

        [System.Security.DynamicSecurityMethod]
        [RequiresUnreferencedCode("Type and its constructor could be removed")]
        public static ObjectHandle? CreateInstance(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstanceInternal(assemblyName,
                                          typeName,
                                          ignoreCase,
                                          bindingAttr,
                                          binder,
                                          args,
                                          culture,
                                          activationAttributes,
                                          ref stackMark);
        }

        [System.Security.DynamicSecurityMethod]
        [RequiresUnreferencedCode("Type and its constructor could be removed")]
        public static ObjectHandle? CreateInstance(string assemblyName, string typeName, object?[]? activationAttributes)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstanceInternal(assemblyName,
                                          typeName,
                                          false,
                                          ConstructorDefault,
                                          null,
                                          null,
                                          null,
                                          activationAttributes,
                                          ref stackMark);
        }

        public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type, bool nonPublic) =>
            CreateInstance(type, nonPublic, wrapExceptions: true);

        internal static object? CreateInstance(Type type, bool nonPublic, bool wrapExceptions)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (type.UnderlyingSystemType is not RuntimeType rt)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));

            return rt.CreateInstanceDefaultCtor(publicOnly: !nonPublic, wrapExceptions: wrapExceptions);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Implementation detail of Activator that linker intrinsically recognizes")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:UnrecognizedReflectionPattern",
            Justification = "Implementation detail of Activator that linker intrinsically recognizes")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern",
            Justification = "Implementation detail of Activator that linker intrinsically recognizes")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2096:UnrecognizedReflectionPattern",
            Justification = "Implementation detail of Activator that linker intrinsically recognizes")]
        private static ObjectHandle? CreateInstanceInternal(string assemblyString,
                                                           string typeName,
                                                           bool ignoreCase,
                                                           BindingFlags bindingAttr,
                                                           Binder? binder,
                                                           object?[]? args,
                                                           CultureInfo? culture,
                                                           object?[]? activationAttributes,
                                                           ref StackCrawlMark stackMark)
        {
            Type? type = null;
            Assembly? assembly = null;
            if (assemblyString == null)
            {
                assembly = Assembly.GetExecutingAssembly(ref stackMark);
            }
            else
            {
                AssemblyName assemblyName = new AssemblyName(assemblyString);

                if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime)
                {
                    // WinRT type - we have to use Type.GetType
                    type = Type.GetType(typeName + ", " + assemblyString, throwOnError: true, ignoreCase);
                }
                else
                {
                    // Classic managed type
                    assembly = RuntimeAssembly.InternalLoad(assemblyName, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
                }
            }

            if (type == null)
            {
                type = assembly!.GetType(typeName, throwOnError: true, ignoreCase);
            }

            object? o = CreateInstance(type!, bindingAttr, binder, args, culture, activationAttributes);

            return o != null ? new ObjectHandle(o) : null;
        }

        [System.Runtime.CompilerServices.Intrinsic]
        public static T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
        {
            return (T)((RuntimeType)typeof(T)).CreateInstanceOfT()!;
        }

        private static T CreateDefaultInstance<T>() where T : struct => default;
    }
}
