// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.ParameterInfos;
using System.Reflection.Runtime.BindingFlagSupport;

using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    //
    // Abstract base class for RuntimeNamedMethodInfo, RuntimeConstructedGenericMethodInfo.
    //
    [DebuggerDisplay("{_debugName}")]
    internal abstract partial class RuntimeMethodInfo : MethodInfo
    {
        protected RuntimeMethodInfo()
        {
        }

        public abstract override MethodAttributes Attributes
        {
            get;
        }

        public abstract override CallingConventions CallingConvention
        {
            get;
        }

        public sealed override bool ContainsGenericParameters
        {
            get
            {
                if (DeclaringType.ContainsGenericParameters)
                    return true;

                if (!IsGenericMethod)
                    return false;

                Type[] pis = GetGenericArguments();
                for (int i = 0; i < pis.Length; i++)
                {
                    if (pis[i].ContainsGenericParameters)
                        return true;
                }

                return false;
            }
        }

        // V4.5 api - Creates open delegates over static or instance methods.
        public sealed override Delegate CreateDelegate(Type delegateType)
        {
            return CreateDelegateWorker(delegateType, null, allowClosed: false);
        }

        // V4.5 api - Creates open or closed delegates over static or instance methods.
        public sealed override Delegate CreateDelegate(Type delegateType, object target)
        {
            return CreateDelegateWorker(delegateType, target, allowClosed: true);
        }

        private Delegate CreateDelegateWorker(Type delegateType, object target, bool allowClosed)
        {
            if (delegateType == null)
                throw new ArgumentNullException(nameof(delegateType));

            if (!(delegateType is RuntimeTypeInfo runtimeDelegateType))
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(delegateType));

            if (!runtimeDelegateType.IsDelegate)
                throw new ArgumentException(SR.Arg_MustBeDelegate);

            Delegate result = CreateDelegateNoThrowOnBindFailure(runtimeDelegateType, target, allowClosed);
            if (result == null)
                throw new ArgumentException(SR.Arg_DlgtTargMeth);
            return result;
        }

        public abstract override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get;
        }

        public sealed override Type DeclaringType
        {
            get
            {
                return this.RuntimeDeclaringType;
            }
        }

        public abstract override bool Equals(object obj);

        public abstract override int GetHashCode();

        public sealed override MethodInfo GetBaseDefinition()
        {
            // This check is for compatibility. Yes, it happens before we normalize constructed generic methods back to their backing definition.
            Type declaringType = DeclaringType;
            if (!IsVirtual || IsStatic || declaringType == null || declaringType.IsInterface)
                return this;

            MethodInfo method = this;

            // For compat: Remove any instantiation on generic methods.
            if (method.IsConstructedGenericMethod)
                method = method.GetGenericMethodDefinition();

            while (true)
            {
                MethodInfo next = method.GetImplicitlyOverriddenBaseClassMember();
                if (next == null)
                    return ((RuntimeMethodInfo)method).WithReflectedTypeSetToDeclaringType;

                method = next;
            }
        }

        public sealed override Type[] GetGenericArguments()
        {
            return RuntimeGenericArgumentsOrParameters.CloneTypeArray();
        }

        internal abstract override int GenericParameterCount { get; }

        public abstract override MethodInfo GetGenericMethodDefinition();

        [RequiresUnreferencedCode("Trimming may change method bodies. For example it can change some instructions, remove branches or local variables.")]
        public sealed override MethodBody GetMethodBody()
        {
            throw new PlatformNotSupportedException();
        }

        public sealed override ParameterInfo[] GetParameters()
        {
            RuntimeParameterInfo[] runtimeParameterInfos = RuntimeParameters;
            if (runtimeParameterInfos.Length == 0)
                return Array.Empty<ParameterInfo>();
            ParameterInfo[] result = new ParameterInfo[runtimeParameterInfos.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = runtimeParameterInfos[i];
            return result;
        }

        public sealed override ParameterInfo[] GetParametersNoCopy()
        {
            return RuntimeParameters;
        }

        public abstract override bool HasSameMetadataDefinitionAs(MemberInfo other);

        [DebuggerGuidedStepThroughAttribute]
        public sealed override object? Invoke(object? obj, BindingFlags invokeAttr, Binder binder, object?[]? parameters, CultureInfo culture)
        {
            MethodInvoker methodInvoker = this.MethodInvoker;
            object? result = methodInvoker.Invoke(obj, parameters, binder, invokeAttr, culture);
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        public abstract override bool IsConstructedGenericMethod
        {
            get;
        }

        public abstract override bool IsGenericMethod
        {
            get;
        }

        public abstract override bool IsGenericMethodDefinition
        {
            get;
        }

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public abstract override MethodInfo MakeGenericMethod(params Type[] typeArguments);

        public abstract override MethodBase MetadataDefinitionMethod { get; }

        public abstract override int MetadataToken
        {
            get;
        }

        public abstract override MethodImplAttributes MethodImplementationFlags
        {
            get;
        }

        public abstract override Module Module
        {
            get;
        }

        public sealed override string Name
        {
            get
            {
                return this.RuntimeName;
            }
        }

        public abstract override Type ReflectedType { get; }

        public sealed override ParameterInfo ReturnParameter
        {
            get
            {
                return this.RuntimeReturnParameter;
            }
        }

        public sealed override Type ReturnType
        {
            get
            {
                return ReturnParameter.ParameterType;
            }
        }

        public abstract override string ToString();

        public abstract override RuntimeMethodHandle MethodHandle { get; }

        internal abstract RuntimeTypeInfo RuntimeDeclaringType
        {
            get;
        }

        internal abstract string RuntimeName
        {
            get;
        }

        internal abstract RuntimeMethodInfo WithReflectedTypeSetToDeclaringType { get; }

        protected abstract MethodInvoker UncachedMethodInvoker { get; }

        //
        // The non-public version of MethodInfo.GetGenericArguments() (does not array-copy and has a more truthful name.)
        //
        internal abstract RuntimeTypeInfo[] RuntimeGenericArgumentsOrParameters { get; }

        internal abstract RuntimeParameterInfo[] GetRuntimeParameters(RuntimeMethodInfo contextMethod, out RuntimeParameterInfo returnParameter);

        //
        // The non-public version of MethodInfo.GetParameters() (does not array-copy.)
        //
        internal RuntimeParameterInfo[] RuntimeParameters
        {
            get
            {
                RuntimeParameterInfo[] parameters = _lazyParameters;
                if (parameters == null)
                {
                    RuntimeParameterInfo returnParameter;
                    parameters = _lazyParameters = GetRuntimeParameters(this, out returnParameter);
                    _lazyReturnParameter = returnParameter;  // Opportunistically initialize the _lazyReturnParameter latch as well.
                }
                return parameters;
            }
        }

        internal RuntimeParameterInfo RuntimeReturnParameter
        {
            get
            {
                RuntimeParameterInfo returnParameter = _lazyReturnParameter;
                if (returnParameter == null)
                {
                    // Though the returnParameter is our primary objective, we can opportunistically initialize the _lazyParameters latch too.
                    _lazyParameters = GetRuntimeParameters(this, out returnParameter);
                    _lazyReturnParameter = returnParameter;
                }
                return returnParameter;
            }
        }

        private volatile RuntimeParameterInfo[] _lazyParameters;
        private volatile RuntimeParameterInfo _lazyReturnParameter;

        internal MethodInvoker MethodInvoker
        {
            get
            {
                return _lazyMethodInvoker ??= UncachedMethodInvoker;
            }
        }

        internal IntPtr LdFtnResult => MethodInvoker.LdFtnResult;

        private volatile MethodInvoker _lazyMethodInvoker;

        /// <summary>
        /// Common CreateDelegate worker. NOTE: If the method signature is not compatible, this method returns null rather than throwing an ArgumentException.
        /// This is needed to support the api overloads that have a "throwOnBindFailure" parameter.
        /// </summary>
        internal Delegate CreateDelegateNoThrowOnBindFailure(RuntimeTypeInfo runtimeDelegateType, object target, bool allowClosed)
        {
            Debug.Assert(runtimeDelegateType.IsDelegate);

            ExecutionEnvironment executionEnvironment = ReflectionCoreExecution.ExecutionEnvironment;
            MethodInfo invokeMethod = runtimeDelegateType.GetInvokeMethod();

            // Make sure the return type is assignment-compatible.
            Type expectedReturnType = ReturnParameter.ParameterType;
            Type actualReturnType = invokeMethod.ReturnParameter.ParameterType;
            if (!IsAssignableFrom(executionEnvironment, actualReturnType, expectedReturnType))
                return null;
            if (expectedReturnType.IsValueType && !actualReturnType.IsValueType)
            {
                // For value type returning methods, conversions between enums and primitives are allowed (and screened by the above call to IsAssignableFrom)
                // but conversions to Object or interfaces implemented by the value type are not.
                return null;
            }

            IList<ParameterInfo> delegateParameters = invokeMethod.GetParametersNoCopy();
            IList<ParameterInfo> targetParameters = this.GetParametersNoCopy();
            IEnumerator<ParameterInfo> delegateParameterEnumerator = delegateParameters.GetEnumerator();
            IEnumerator<ParameterInfo> targetParameterEnumerator = targetParameters.GetEnumerator();

            bool isStatic = this.IsStatic;
            bool isOpen;
            if (isStatic)
            {
                if (delegateParameters.Count == targetParameters.Count)
                {
                    // Open static: This is the "typical" case of calling a static method.
                    isOpen = true;
                    if (target != null)
                        return null;
                }
                else
                {
                    // Closed static: This is the "weird" v2.0 case where the delegate is closed over the target method's first parameter.
                    //   (it make some kinda sense if you think of extension methods.)
                    if (!allowClosed)
                        return null;
                    isOpen = false;
                    if (!targetParameterEnumerator.MoveNext())
                        return null;
                    if (target != null && !IsAssignableFrom(executionEnvironment, targetParameterEnumerator.Current.ParameterType, target.GetType()))
                        return null;
                }
            }
            else
            {
                if (delegateParameters.Count == targetParameters.Count)
                {
                    // Closed instance: This is the "typical" case of invoking an instance method.
                    isOpen = false;
                    if (!allowClosed)
                        return null;
                    if (target != null && !IsAssignableFrom(executionEnvironment, this.DeclaringType, target.GetType()))
                        return null;
                }
                else
                {
                    // Open instance: This is the "weird" v2.0 case where the delegate has a leading extra parameter that's assignable to the target method's
                    // declaring type.
                    if (!delegateParameterEnumerator.MoveNext())
                        return null;
                    isOpen = true;
                    Type firstParameterOfMethodType = this.DeclaringType;
                    if (firstParameterOfMethodType.IsValueType)
                        firstParameterOfMethodType = firstParameterOfMethodType.MakeByRefType();

                    if (!IsAssignableFrom(executionEnvironment, firstParameterOfMethodType, delegateParameterEnumerator.Current.ParameterType))
                        return null;
                    if (target != null)
                        return null;
                }
            }

            // Verify that the parameters that the delegate and method have in common are assignment-compatible.
            while (delegateParameterEnumerator.MoveNext())
            {
                if (!targetParameterEnumerator.MoveNext())
                    return null;
                if (!IsAssignableFrom(executionEnvironment, targetParameterEnumerator.Current.ParameterType, delegateParameterEnumerator.Current.ParameterType))
                    return null;
            }
            if (targetParameterEnumerator.MoveNext())
                return null;

            return CreateDelegateWithoutSignatureValidation(runtimeDelegateType, target, isStatic: isStatic, isOpen: isOpen);
        }

        internal Delegate CreateDelegateWithoutSignatureValidation(Type delegateType, object target, bool isStatic, bool isOpen)
        {
            return MethodInvoker.CreateDelegate(delegateType.TypeHandle, target, isStatic: isStatic, isVirtual: false, isOpen: isOpen);
        }

        private static bool IsAssignableFrom(ExecutionEnvironment executionEnvironment, Type dstType, Type srcType)
        {
            // byref types do not have a TypeHandle so we must treat these separately.
            if (dstType.IsByRef && srcType.IsByRef)
            {
                if (!dstType.Equals(srcType))
                    return false;
            }

            // Enable pointers (which don't necessarily have typehandles). todo:be able to handle intptr <-> pointer, check if we need to handle
            // casts via pointer where the pointer types aren't identical
            if (dstType.Equals(srcType))
            {
                return true;
            }

            // If assignment compatible in the normal way, allow
            if (executionEnvironment.IsAssignableFrom(dstType.TypeHandle, srcType.TypeHandle))
            {
                return true;
            }

            // they are not compatible yet enums can go into each other if their underlying element type is the same
            // or into their equivalent integral type
            Type dstTypeUnderlying = dstType;
            if (dstType.IsEnum)
            {
                dstTypeUnderlying = Enum.GetUnderlyingType(dstType);
            }
            Type srcTypeUnderlying = srcType;
            if (srcType.IsEnum)
            {
                srcTypeUnderlying = Enum.GetUnderlyingType(srcType);
            }
            if (dstTypeUnderlying.Equals(srcTypeUnderlying))
            {
                return true;
            }

            return false;
        }

        protected RuntimeMethodInfo WithDebugName()
        {
            bool populateDebugNames = DeveloperExperienceState.DeveloperExperienceModeEnabled;
#if DEBUG
            populateDebugNames = true;
#endif
            if (!populateDebugNames)
                return this;

            if (_debugName == null)
            {
                _debugName = "Constructing..."; // Protect against any inadvertent reentrancy.
                _debugName = RuntimeName;
            }
            return this;
        }

        private string _debugName;
    }
}
