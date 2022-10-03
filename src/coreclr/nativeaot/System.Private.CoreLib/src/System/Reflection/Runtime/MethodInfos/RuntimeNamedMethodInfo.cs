// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.ParameterInfos;
using System.Reflection.Runtime.CustomAttributes;

using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    internal abstract class RuntimeNamedMethodInfo : RuntimeMethodInfo
    {
        protected internal abstract string ComputeToString(RuntimeMethodInfo contextMethod);
        internal abstract MethodInvoker GetUncachedMethodInvoker(RuntimeTypeInfo[] methodArguments, MemberInfo exceptionPertainant);
        internal abstract RuntimeMethodHandle GetRuntimeMethodHandle(Type[] methodArguments);
    }

    //
    // The runtime's implementation of non-constructor MethodInfo's that represent a method definition.
    //
    internal sealed partial class RuntimeNamedMethodInfo<TRuntimeMethodCommon> : RuntimeNamedMethodInfo
        where TRuntimeMethodCommon : IRuntimeMethodCommon<TRuntimeMethodCommon>, IEquatable<TRuntimeMethodCommon>
    {
        //
        // methodHandle    - the "tkMethodDef" that identifies the method.
        // definingType   - the "tkTypeDef" that defined the method (this is where you get the metadata reader that created methodHandle.)
        // contextType    - the type that supplies the type context (i.e. substitutions for generic parameters.) Though you
        //                  get your raw information from "definingType", you report "contextType" as your DeclaringType property.
        //
        //  For example:
        //
        //       typeof(Foo<>).GetTypeInfo().DeclaredMembers
        //
        //           The definingType and contextType are both Foo<>
        //
        //       typeof(Foo<int,String>).GetTypeInfo().DeclaredMembers
        //
        //          The definingType is "Foo<,>"
        //          The contextType is "Foo<int,String>"
        //
        //  We don't report any DeclaredMembers for arrays or generic parameters so those don't apply.
        //
        private RuntimeNamedMethodInfo(TRuntimeMethodCommon common, RuntimeTypeInfo reflectedType)
            : base()
        {
            _common = common;
            _reflectedType = reflectedType;
        }

        public sealed override MethodAttributes Attributes
        {
            get
            {
                return _common.Attributes;
            }
        }

        public sealed override CallingConventions CallingConvention
        {
            get
            {
                return _common.CallingConvention;
            }
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                foreach (CustomAttributeData cad in _common.TrueCustomAttributes)
                {
                    yield return cad;
                }

                MethodImplAttributes implAttributes = _common.MethodImplementationFlags;
                if (0 != (implAttributes & MethodImplAttributes.PreserveSig))
                    yield return new RuntimePseudoCustomAttributeData(typeof(PreserveSigAttribute), null);
            }
        }

        public sealed override MethodInfo GetGenericMethodDefinition()
        {
            if (IsGenericMethodDefinition)
                return this;
            throw new InvalidOperationException();
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
                return IsGenericMethodDefinition;
            }
        }

        public sealed override bool IsGenericMethodDefinition
        {
            get
            {
                return _common.IsGenericMethodDefinition;
            }
        }

        internal sealed override int GenericParameterCount => _common.GenericParameterCount;

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public sealed override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            if (typeArguments == null)
                throw new ArgumentNullException(nameof(typeArguments));
            if (GenericTypeParameters.Length == 0)
                throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));
            RuntimeTypeInfo[] genericTypeArguments = new RuntimeTypeInfo[typeArguments.Length];
            for (int i = 0; i < typeArguments.Length; i++)
            {
                Type typeArgument = typeArguments[i];
                if (typeArgument == null)
                    throw new ArgumentNullException();

                if (typeArgument is not RuntimeType)
                    throw new PlatformNotSupportedException(SR.Format(SR.Reflection_CustomReflectionObjectsNotSupported, typeArguments[i]));

                if (typeArgument.IsByRefLike)
                    throw new BadImageFormatException(SR.CannotUseByRefLikeTypeInInstantiation);

                genericTypeArguments[i] = typeArgument.CastToRuntimeTypeInfo();
            }
            if (typeArguments.Length != GenericTypeParameters.Length)
                throw new ArgumentException(SR.Format(SR.Argument_NotEnoughGenArguments, typeArguments.Length, GenericTypeParameters.Length));
            RuntimeMethodInfo methodInfo = (RuntimeMethodInfo)RuntimeConstructedGenericMethodInfo.GetRuntimeConstructedGenericMethodInfo(this, genericTypeArguments);
            MethodInvoker _ = methodInfo.MethodInvoker; // For compatibility with other Make* apis, trigger any MissingMetadataExceptions now rather than later.
            return methodInfo;
        }

        public sealed override MethodBase MetadataDefinitionMethod
        {
            get
            {
                return RuntimeNamedMethodInfo<TRuntimeMethodCommon>.GetRuntimeNamedMethodInfo(_common.RuntimeMethodCommonOfUninstantiatedMethod, _common.DefiningTypeInfo);
            }
        }

        public sealed override MethodImplAttributes MethodImplementationFlags
        {
            get
            {
                return _common.MethodImplementationFlags;
            }
        }

        public sealed override Module Module
        {
            get
            {
                return _common.Module;
            }
        }

        public sealed override Type ReflectedType
        {
            get
            {
                return _reflectedType;
            }
        }

        public sealed override int MetadataToken
        {
            get
            {
                return _common.MetadataToken;
            }
        }

        public sealed override string ToString()
        {
            return ComputeToString(this);
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // Do not rewrite as a call to IsConstructedGenericMethod - we haven't yet established that "other" is a runtime-implemented member yet!
            if (other is RuntimeConstructedGenericMethodInfo otherConstructedGenericMethod)
                other = otherConstructedGenericMethod.GetGenericMethodDefinition();

            if (!(other is RuntimeNamedMethodInfo<TRuntimeMethodCommon> otherMethod))
                return false;

            return _common.HasSameMetadataDefinitionAs(otherMethod._common);
        }

        public sealed override bool Equals(object obj)
        {
            if (!(obj is RuntimeNamedMethodInfo<TRuntimeMethodCommon> other))
                return false;
            if (!_common.Equals(other._common))
                return false;
            if (!(_reflectedType.Equals(other._reflectedType)))
                return false;
            return true;
        }

        public sealed override int GetHashCode()
        {
            return _common.GetHashCode();
        }

        public sealed override RuntimeMethodHandle MethodHandle => GetRuntimeMethodHandle(null);

        protected internal sealed override string ComputeToString(RuntimeMethodInfo contextMethod)
        {
            return RuntimeMethodHelpers.ComputeToString(ref _common, contextMethod, contextMethod.RuntimeGenericArgumentsOrParameters);
        }

        internal sealed override RuntimeTypeInfo[] RuntimeGenericArgumentsOrParameters
        {
            get
            {
                return this.GenericTypeParameters;
            }
        }

        internal sealed override RuntimeParameterInfo[] GetRuntimeParameters(RuntimeMethodInfo contextMethod, out RuntimeParameterInfo returnParameter)
        {
            return RuntimeMethodHelpers.GetRuntimeParameters(ref _common, contextMethod, contextMethod.RuntimeGenericArgumentsOrParameters, out returnParameter);
        }

        internal sealed override RuntimeTypeInfo RuntimeDeclaringType
        {
            get
            {
                return _common.DeclaringType;
            }
        }

        internal sealed override string RuntimeName
        {
            get
            {
                return _common.Name;
            }
        }

        internal sealed override RuntimeMethodInfo WithReflectedTypeSetToDeclaringType
        {
            get
            {
                if (_reflectedType.Equals(_common.DefiningTypeInfo))
                    return this;

                return RuntimeNamedMethodInfo<TRuntimeMethodCommon>.GetRuntimeNamedMethodInfo(_common, _common.ContextTypeInfo);
            }
        }

        private RuntimeTypeInfo[] GenericTypeParameters
        {
            get
            {
                RuntimeNamedMethodInfo<TRuntimeMethodCommon> owningMethod = this;
                if (DeclaringType.IsConstructedGenericType)
                {
                    // Desktop compat: Constructed generic types and their generic type definitions share the same Type objects for method generic parameters.
                    TRuntimeMethodCommon uninstantiatedCommon = _common.RuntimeMethodCommonOfUninstantiatedMethod;
                    owningMethod = RuntimeNamedMethodInfo<TRuntimeMethodCommon>.GetRuntimeNamedMethodInfo(uninstantiatedCommon, uninstantiatedCommon.DeclaringType);
                }
                else
                {
                    // Desktop compat: DeclaringMethod always returns a MethodInfo whose ReflectedType is equal to DeclaringType.
                    if (!_reflectedType.Equals(_common.DeclaringType))
                        owningMethod = RuntimeNamedMethodInfo<TRuntimeMethodCommon>.GetRuntimeNamedMethodInfo(_common, _common.DeclaringType);
                }

                return _common.GetGenericTypeParametersWithSpecifiedOwningMethod(owningMethod);
            }
        }

        internal sealed override MethodInvoker GetUncachedMethodInvoker(RuntimeTypeInfo[] methodArguments, MemberInfo exceptionPertainant)
        {
            MethodInvoker invoker = _common.GetUncachedMethodInvoker(methodArguments, exceptionPertainant, out Exception exception);
            if (invoker == null)
                throw exception;

            return invoker;
        }

        protected sealed override MethodInvoker UncachedMethodInvoker
        {
            get
            {
                MethodInvoker invoker = this.GetCustomMethodInvokerIfNeeded();
                if (invoker != null)
                    return invoker;

                return GetUncachedMethodInvoker(Array.Empty<RuntimeTypeInfo>(), this);
            }
        }

        internal sealed override RuntimeMethodHandle GetRuntimeMethodHandle(Type[] genericArgs)
        {
            return _common.GetRuntimeMethodHandle(genericArgs);
        }

        private TRuntimeMethodCommon _common;
        private readonly RuntimeTypeInfo _reflectedType;
    }
}
