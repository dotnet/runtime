// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Activator is an object that contains the Activation (CreateInstance/New)
//  methods for late bound support.
//

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Runtime;

using Internal.Reflection.Augments;
using Internal.Runtime.CompilerServices;

namespace System
{
    public static partial class Activator
    {
        // The following methods and helper class implement the functionality of Activator.CreateInstance<T>()
        // The implementation relies on several compiler intrinsics that expand to quick dictionary lookups in shared
        // code, and direct constant references in unshared code.
        //
        // This method is the public surface area. It wraps the CreateInstance intrinsic with the appropriate try/catch
        // block so that the correct exceptions are generated. Also, it handles the cases where the T type doesn't have
        // a default constructor.
        //
        // This method is intrinsic. The compiler might replace it with more efficient implementation.
        [DebuggerGuidedStepThrough]
        [Intrinsic]
        public static unsafe T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
        {
            // Grab the pointer to the default constructor of the type. If T doesn't have a default
            // constructor, the intrinsic returns a marker pointer that we check for.
            IntPtr defaultConstructor = DefaultConstructorOf<T>();

            // Check if we got the marker back.
            //
            // TODO: might want to disambiguate the different cases for abstract class, interface, etc.
            if (defaultConstructor == (IntPtr)(delegate*<Guid>)&MissingConstructorMethod)
                throw new MissingMethodException(SR.Format(SR.MissingConstructor_Name, typeof(T)));

            T t;
            try
            {
                // Call the default constructor on the allocated instance.
                if (RuntimeHelpers.IsReference<T>())
                {
                    // Grab a pointer to the optimized allocator for the type and call it.
                    IntPtr allocator = AllocatorOf<T>();
                    t = RawCalliHelper.Call<T>(allocator, EETypePtr.EETypePtrOf<T>().RawValue);
                    RawCalliHelper.Call(defaultConstructor, t);

                    // Debugger goo so that stepping in works. Only affects debug info generation.
                    // The call gets optimized away.
                    DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                }
                else
                {
                    t = default!;
                    RawCalliHelper.Call(defaultConstructor, ref Unsafe.As<T, byte>(ref t));

                    // Debugger goo so that stepping in works. Only affects debug info generation.
                    // The call gets optimized away.
                    DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                }

                return t;
            }
            catch (Exception e)
            {
                throw new TargetInvocationException(e);
            }
        }

        [Intrinsic]
        private static IntPtr DefaultConstructorOf<T>()
        {
            // Codegens must expand this intrinsic to the pointer to the default constructor of T
            // or to a marker that lets us detect there's no default constructor.
            // We could implement a fallback with the type loader if we wanted to, but it will be slow and unreliable.
            throw new NotSupportedException();
        }

        [Intrinsic]
        private static IntPtr AllocatorOf<T>()
        {
            // Codegens must expand this intrinsic to the pointer to the allocator suitable to allocate an instance of T.
            // We could implement a fallback with the type loader if we wanted to, but it will be slow and unreliable.
            throw new NotSupportedException();
        }

        internal static unsafe IntPtr GetFallbackDefaultConstructor()
        {
            return (IntPtr)(delegate*<Guid>)&MissingConstructorMethod;
        }

        // This is a marker method. We return a GUID just to make sure the body is unique
        // and under no circumstances gets folded.
        private static Guid MissingConstructorMethod() => new Guid(0x68be9718, 0xf787, 0x45ab, 0x84, 0x3b, 0x1f, 0x31, 0xb6, 0x12, 0x65, 0xeb);
        // The constructor of this struct is used when there's no constructor
        struct StructWithNoConstructor { public StructWithNoConstructor() { } }

        [DebuggerHidden]
        [DebuggerStepThrough]
        public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type, bool nonPublic)
            => ReflectionAugments.ReflectionCoreCallbacks.ActivatorCreateInstance(type, nonPublic);

        [DebuggerHidden]
        [DebuggerStepThrough]
        public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
            => ReflectionAugments.ReflectionCoreCallbacks.ActivatorCreateInstance(type, bindingAttr, binder, args, culture, activationAttributes);

        [RequiresUnreferencedCode("Type and its constructor could be removed")]
        public static ObjectHandle CreateInstance(string assemblyName, string typeName)
        {
            throw new PlatformNotSupportedException(); // https://github.com/dotnet/runtime/issues/26701
        }

        [RequiresUnreferencedCode("Type and its constructor could be removed")]
        public static ObjectHandle CreateInstance(string assemblyName,
                                                  string typeName,
                                                  bool ignoreCase,
                                                  BindingFlags bindingAttr,
                                                  Binder? binder,
                                                  object?[]? args,
                                                  CultureInfo? culture,
                                                  object?[]? activationAttributes)
        {
            throw new PlatformNotSupportedException(); // https://github.com/dotnet/runtime/issues/26701
        }

        [RequiresUnreferencedCode("Type and its constructor could be removed")]
        public static ObjectHandle CreateInstance(string assemblyName, string typeName, object?[]? activationAttributes)
        {
            throw new PlatformNotSupportedException(); // https://github.com/dotnet/runtime/issues/26701
        }
    }
}
