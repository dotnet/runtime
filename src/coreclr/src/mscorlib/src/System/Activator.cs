// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// Activator is an object that contains the Activation (CreateInstance/New) 
//  methods for late bound support.
//
// 
// 
//

namespace System
{
    using System;
    using System.Reflection;
    using System.Security;
    using CultureInfo = System.Globalization.CultureInfo;
    using StackCrawlMark = System.Threading.StackCrawlMark;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using AssemblyHashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm;
    using System.Runtime.Versioning;

    // Only statics, does not need to be marked with the serializable attribute
    public sealed class Activator
    {
        internal const int LookupMask = 0x000000FF;
        internal const BindingFlags ConLookup = (BindingFlags)(BindingFlags.Instance | BindingFlags.Public);
        internal const BindingFlags ConstructorDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

        // This class only contains statics, so hide the worthless constructor
        private Activator()
        {
        }

        // CreateInstance
        // The following methods will create a new instance of an Object
        // Full Binding Support
        // For all of these methods we need to get the underlying RuntimeType and
        //  call the Impl version.
        static public Object CreateInstance(Type type,
                                            BindingFlags bindingAttr,
                                            Binder binder,
                                            Object[] args,
                                            CultureInfo culture)
        {
            return CreateInstance(type, bindingAttr, binder, args, culture, null);
        }

        static public Object CreateInstance(Type type,
                                            BindingFlags bindingAttr,
                                            Binder binder,
                                            Object[] args,
                                            CultureInfo culture,
                                            Object[] activationAttributes)
        {
            if ((object)type == null)
                throw new ArgumentNullException(nameof(type));

            if (type is System.Reflection.Emit.TypeBuilder)
                throw new NotSupportedException(SR.NotSupported_CreateInstanceWithTypeBuilder);

            // If they didn't specify a lookup, then we will provide the default lookup.
            if ((bindingAttr & (BindingFlags)LookupMask) == 0)
                bindingAttr |= Activator.ConstructorDefault;

            if (activationAttributes != null && activationAttributes.Length > 0)
            {
                throw new PlatformNotSupportedException(SR.NotSupported_ActivAttr);
            }

            RuntimeType rt = type.UnderlyingSystemType as RuntimeType;

            if (rt == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));

            return rt.CreateInstanceImpl(bindingAttr, binder, args, culture, activationAttributes);
        }

        static public Object CreateInstance(Type type, params Object[] args)
        {
            return CreateInstance(type,
                                  Activator.ConstructorDefault,
                                  null,
                                  args,
                                  null,
                                  null);
        }

        static public Object CreateInstance(Type type,
                                            Object[] args,
                                            Object[] activationAttributes)
        {
            return CreateInstance(type,
                                  Activator.ConstructorDefault,
                                  null,
                                  args,
                                  null,
                                  activationAttributes);
        }

        static public Object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type, false);
        }

        static public Object CreateInstance(Type type, bool nonPublic)
        {
            return CreateInstance(type, nonPublic, wrapExceptions: true);
        }

        static internal Object CreateInstance(Type type, bool nonPublic, bool wrapExceptions)
        {
            if ((object)type == null)
                throw new ArgumentNullException(nameof(type));

            RuntimeType rt = type.UnderlyingSystemType as RuntimeType;

            if (rt == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));

            return rt.CreateInstanceDefaultCtor(!nonPublic, false, true, wrapExceptions);
        }

        static public T CreateInstance<T>()
        {
            RuntimeType rt = typeof(T) as RuntimeType;

            // This is a workaround to maintain compatibility with V2. Without this we would throw a NotSupportedException for void[].
            // Array, Ref, and Pointer types don't have default constructors.
            if (rt.HasElementType)
                throw new MissingMethodException(SR.Arg_NoDefCTor);

            // Skip the CreateInstanceCheckThis call to avoid perf cost and to maintain compatibility with V2 (throwing the same exceptions).
            return (T)rt.CreateInstanceDefaultCtor(true /*publicOnly*/, true /*skipCheckThis*/, true /*fillCache*/, true /*wrapExceptions*/);
        }
    }
}
