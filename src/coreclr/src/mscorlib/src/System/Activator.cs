// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Globalization;

namespace System
{
    /// <summary>
    /// Activator contains the Activation (CreateInstance/New) methods for late bound support.
    /// </summary>
    public static class Activator
    {
        internal const BindingFlags ConstructorDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

        public static object CreateInstance(Type type, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture) =>
            CreateInstance(type, bindingAttr, binder, args, culture, null);

        public static object CreateInstance(Type type, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
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
                return rt.CreateInstanceImpl(bindingAttr, binder, args, culture, activationAttributes);

            throw new ArgumentException(SR.Arg_MustBeType, nameof(type));
        }

        public static object CreateInstance(Type type, params object[] args) =>
            CreateInstance(type, ConstructorDefault, null, args, null, null);

        public static object CreateInstance(Type type, object[] args, object[] activationAttributes) =>
            CreateInstance(type, ConstructorDefault, null, args, null, activationAttributes);

        public static object CreateInstance(Type type) =>
            CreateInstance(type, nonPublic: false);

        public static object CreateInstance(Type type, bool nonPublic) =>
            CreateInstance(type, nonPublic, wrapExceptions: true);

        internal static object CreateInstance(Type type, bool nonPublic, bool wrapExceptions)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (type.UnderlyingSystemType is RuntimeType rt)
                return rt.CreateInstanceDefaultCtor(!nonPublic, false, true, wrapExceptions);

            throw new ArgumentException(SR.Arg_MustBeType, nameof(type));
        }

        public static T CreateInstance<T>()
        {
            var rt = (RuntimeType)typeof(T);

            // This is a workaround to maintain compatibility with V2. Without this we would throw a NotSupportedException for void[].
            // Array, Ref, and Pointer types don't have default constructors.
            if (rt.HasElementType)
                throw new MissingMethodException(SR.Arg_NoDefCTor);

            // Skip the CreateInstanceCheckThis call to avoid perf cost and to maintain compatibility with V2 (throwing the same exceptions).
            return (T)rt.CreateInstanceDefaultCtor(publicOnly: true, skipCheckThis: true, fillCache: true, wrapExceptions: true);
        }
    }
}
