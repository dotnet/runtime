// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Runtime.Remoting;
using System.Threading;
using System.Diagnostics;

namespace System
{
    public static partial class Activator
    {
        //
        // Note: CreateInstance returns null for Nullable<T>, e.g. CreateInstance(typeof(int?)) returns null.
        //

        public static object? CreateInstance(Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
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

            if (type.UnderlyingSystemType is RuntimeType rt)
                return rt.CreateInstanceDefaultCtor(publicOnly: !nonPublic, skipCheckThis: false, fillCache: true, wrapExceptions: wrapExceptions);

            throw new ArgumentException(SR.Arg_MustBeType, nameof(type));
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2006:UnrecognizedReflectionPattern",
            Justification = "Implementation detail of Activator that linker intrinsically recognizes")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
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
            return (T)((RuntimeType)typeof(T)).CreateInstanceDefaultCtor(publicOnly: true, skipCheckThis: true, fillCache: true, wrapExceptions: true)!;
        }

        public unsafe static Func<T> CreateFactory<T>()
        {
            // ObjectFactory<T> ctor will perform correctness checks
            ObjectFactory<T> factory = new ObjectFactory<T>();
            return factory.CreateInstance;
        }

        public static unsafe Func<object?> CreateFactory(Type type, bool nonPublic)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!(type.UnderlyingSystemType is RuntimeType rt))
            {
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));
            }

            type = null!; // just to make sure we don't use 'type' for the rest of the method

            if (rt.IsPointer || rt.IsByRef || rt.IsByRefLike || !RuntimeHelpers.IsFastInstantiable(rt))
            {
                throw new ArgumentException(
                    paramName: nameof(type),
                    message: SR.NotSupported_Type);
            }

            MethodTable* pMT = RuntimeTypeHandle.GetMethodTable(rt);
            Debug.Assert(pMT != null);

            if (!pMT->HasDefaultConstructor)
            {
                // If no parameterless ctor exists, we can still fabricate an instance for value
                // types, returning a boxed default(T). Unless the incoming type is a
                // Nullable<T>, at which point we'll return null instead of a "boxed null".

                if (pMT->IsValueType)
                {
                    if (pMT->IsNullable)
                    {
                        return () => null;
                    }
                    else
                    {
                        ObjectFactory factory = ObjectFactory.CreateFactoryForValueTypeDefaultOfT(rt);
                        return factory.CreateInstance;
                    }
                }
            }
            else
            {
                // If a parameterless ctor exists, perform visibility checks before linking to it.

                RuntimeMethodHandleInternal hCtor = RuntimeTypeHandle.GetDefaultConstructor(rt);
                Debug.Assert(!hCtor.IsNullHandle());

                if (nonPublic || (RuntimeMethodHandle.GetAttributes(hCtor) & MethodAttributes.MemberAccessMask) == MethodAttributes.Public)
                {
                    ObjectFactory factory = new ObjectFactory(hCtor);
                    return factory.CreateInstance;
                }
            }

            // If we reached this point, no parameterless ctor was found, or the ctor
            // was found but we can't link to it due to member access restrictions.

            throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, rt));
        }

        private static T CreateDefaultInstance<T>() where T : struct => default;
    }
}
