// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Runtime.Remoting;
using System.Security;
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
            ArgumentNullException.ThrowIfNull(type);

            if (type is Reflection.Emit.TypeBuilder)
                throw new NotSupportedException(SR.NotSupported_CreateInstanceWithTypeBuilder);

            // If they didn't specify a lookup, then we will provide the default lookup.
            const int LookupMask = 0x000000FF;
            if ((bindingAttr & (BindingFlags)LookupMask) == 0)
                bindingAttr |= ConstructorDefault;

            if (activationAttributes?.Length > 0)
                throw new PlatformNotSupportedException(SR.NotSupported_ActivAttr);

            if (type.UnderlyingSystemType is not RuntimeType rt)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));

            return rt.CreateInstanceImpl(bindingAttr, binder, args, culture);
        }

        [DynamicSecurityMethod]
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

        [DynamicSecurityMethod]
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

        [DynamicSecurityMethod]
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
            ArgumentNullException.ThrowIfNull(type);

            if (type.UnderlyingSystemType is not RuntimeType rt)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));

            return rt.CreateInstanceDefaultCtor(publicOnly: !nonPublic, wrapExceptions: wrapExceptions);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Implementation detail of Activator that linker intrinsically recognizes")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern",
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
            RuntimeAssembly assembly;
            if (assemblyString == null)
            {
                assembly = Assembly.GetExecutingAssembly(ref stackMark);
            }
            else
            {
                AssemblyName assemblyName = new AssemblyName(assemblyString);
                assembly = RuntimeAssembly.InternalLoad(assemblyName, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
            }

            // Issues IL2026 warning.
            Type? type = assembly.GetType(typeName, throwOnError: true, ignoreCase);

            // Issues IL2072 warning.
            object? o = CreateInstance(type!, bindingAttr, binder, args, culture, activationAttributes);

            return o != null ? new ObjectHandle(o) : null;
        }

        [Intrinsic]
        public static T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>()
            where T : allows ref struct
        {
            var rtType = (RuntimeType)typeof(T);
            if (!rtType.IsValueType)
            {
                object o = rtType.CreateInstanceOfT()!;

                // Casting the above object to T is technically invalid because
                // T can be ByRefLike (that is, ref struct). Roslyn blocks the
                // cast this in function with a "CS0030: Cannot convert type 'object' to 'T'",
                // which is correct. However, since we are doing the IsValueType
                // check above, we know this code path will only be taken with
                // reference types and therefore the below Unsafe.As<> is safe.
                return Unsafe.As<object, T>(ref o);
            }
            else
            {
                T t = default!;
                rtType.CallDefaultStructConstructor(ref Unsafe.As<T, byte>(ref t));
                return t;
            }
        }

        private static T CreateDefaultInstance<T>() where T : struct => default;
    }
}
