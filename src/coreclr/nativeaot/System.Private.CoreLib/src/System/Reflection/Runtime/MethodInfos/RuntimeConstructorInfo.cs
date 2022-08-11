// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.ParameterInfos;

using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    //
    // The runtime's implementation of ConstructorInfo.
    //
    internal abstract partial class RuntimeConstructorInfo : ConstructorInfo
    {
        public abstract override MethodAttributes Attributes { get; }

        public abstract override CallingConventions CallingConvention { get; }

        public sealed override bool ContainsGenericParameters
        {
            get
            {
                return DeclaringType.ContainsGenericParameters;
            }
        }

        public abstract override IEnumerable<CustomAttributeData> CustomAttributes { get; }

        public abstract override Type DeclaringType { get; }

        public sealed override Type[] GetGenericArguments()
        {
            // Constructors cannot be generic. Desktop compat dictates that We throw NotSupported rather than returning a 0-length array.
            throw new NotSupportedException();
        }

        [RequiresUnreferencedCode("Trimming may change method bodies. For example it can change some instructions, remove branches or local variables.")]
        public sealed override MethodBody GetMethodBody()
        {
            throw new PlatformNotSupportedException();
        }

        public sealed override ParameterInfo[] GetParameters()
        {
            RuntimeParameterInfo[] parameters = RuntimeParameters;
            if (parameters.Length == 0)
                return Array.Empty<ParameterInfo>();
            ParameterInfo[] result = new ParameterInfo[parameters.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = parameters[i];
            return result;
        }

        public sealed override ParameterInfo[] GetParametersNoCopy()
        {
            return RuntimeParameters;
        }

        public abstract override bool HasSameMetadataDefinitionAs(MemberInfo other);

        public abstract override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture);

        [DebuggerGuidedStepThrough]
        public sealed override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            parameters ??= Array.Empty<object>();
            MethodInvoker methodInvoker;
            try
            {
                methodInvoker = this.MethodInvoker;
            }
            catch (Exception)
            {
                //
                // Project N compat note: On the desktop, ConstructorInfo.Invoke(Object[]) specifically forbids invoking static constructors (and
                // for us, that check is embedded inside the MethodInvoker property call.) Howver, MethodBase.Invoke(Object, Object[]) allows it. This was
                // probably an oversight on the desktop. We choose not to support this loophole on Project N for the following reasons:
                //
                //  1. The Project N toolchain aggressively replaces static constructors with static initialization data whenever possible.
                //     So the static constructor may no longer exist.
                //
                //  2. Invoking the static constructor through Reflection is not very useful as it invokes the static constructor whether or not
                //     it was already run. Since static constructors are specifically one-shot deals, this will almost certainly mess up the
                //     type's internal assumptions.
                //

                if (this.IsStatic)
                    throw new PlatformNotSupportedException(SR.Acc_NotClassInit);
                throw;
            }

            object? result = methodInvoker.Invoke(obj, parameters, binder, invokeAttr, culture);
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result!;
        }

        public abstract override MethodBase MetadataDefinitionMethod { get; }

        public abstract override int MetadataToken
        {
            get;
        }

        public sealed override Module Module
        {
            get
            {
                return DeclaringType.Module;
            }
        }

        public sealed override bool IsConstructedGenericMethod
        {
            get
            {
                return false;
            }
        }

        public sealed override bool IsGenericMethod
        {
            get
            {
                return false;
            }
        }

        public sealed override bool IsGenericMethodDefinition
        {
            get
            {
                return false;
            }
        }

        public abstract override MethodImplAttributes MethodImplementationFlags { get; }

        public abstract override string Name { get; }

        public abstract override bool Equals(object obj);

        public abstract override int GetHashCode();

        public sealed override Type ReflectedType
        {
            get
            {
                // Constructors are always looked up as if BindingFlags.DeclaredOnly were specified. Thus, the ReflectedType will always be the DeclaringType.
                return DeclaringType;
            }
        }

        public abstract override string ToString();

        public abstract override RuntimeMethodHandle MethodHandle { get; }

        protected MethodInvoker MethodInvoker
        {
            get
            {
                return _lazyMethodInvoker ??= UncachedMethodInvoker;
            }
        }

        internal IntPtr LdFtnResult => MethodInvoker.LdFtnResult;

        protected abstract RuntimeParameterInfo[] RuntimeParameters { get; }

        protected abstract MethodInvoker UncachedMethodInvoker { get; }

        private volatile MethodInvoker _lazyMethodInvoker;
    }
}
