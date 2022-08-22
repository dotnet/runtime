// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.ParameterInfos;

using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    //
    // The runtime's implementation of ConstructorInfo's represented in the metadata (this is the 99% case.)
    //
    internal sealed partial class RuntimePlainConstructorInfo<TRuntimeMethodCommon> : RuntimeConstructorInfo where TRuntimeMethodCommon : IRuntimeMethodCommon<TRuntimeMethodCommon>, IEquatable<TRuntimeMethodCommon>
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
        private RuntimePlainConstructorInfo(TRuntimeMethodCommon common)
        {
            _common = common;
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
                return _common.TrueCustomAttributes;
            }
        }

        public sealed override Type DeclaringType
        {
            get
            {
                return _common.DeclaringType;
            }
        }

        [DebuggerGuidedStepThrough]
        public sealed override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            // Most objects are allocated by NewObject and their constructors return "void". But in many frameworks,
            // there are "weird" cases (e.g. String) where the constructor must do both the allocation and initialization.
            // Reflection.Core does not hardcode these special cases. It's up to the ExecutionEnvironment to steer
            // us the right way by coordinating the implementation of NewObject and MethodInvoker.
            object newObject = ReflectionCoreExecution.ExecutionEnvironment.NewObject(this.DeclaringType.TypeHandle);
            object ctorAllocatedObject = this.MethodInvoker.Invoke(newObject, parameters, binder, invokeAttr, culture)!;
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return newObject ?? ctorAllocatedObject;
        }

        public sealed override MethodBase MetadataDefinitionMethod
        {
            get
            {
                return RuntimePlainConstructorInfo<TRuntimeMethodCommon>.GetRuntimePlainConstructorInfo(_common.RuntimeMethodCommonOfUninstantiatedMethod);
            }
        }

        public sealed override MethodImplAttributes MethodImplementationFlags
        {
            get
            {
                return _common.MethodImplementationFlags;
            }
        }

        public sealed override string Name
        {
            get
            {
                return _common.Name;
            }
        }

        public sealed override int MetadataToken
        {
            get
            {
                return _common.MetadataToken;
            }
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (!(other is RuntimePlainConstructorInfo<TRuntimeMethodCommon> otherConstructor))
                return false;

            return _common.HasSameMetadataDefinitionAs(otherConstructor._common);
        }

        public sealed override bool Equals(object obj)
        {
            if (!(obj is RuntimePlainConstructorInfo<TRuntimeMethodCommon> other))
                return false;
            return _common.Equals(other._common);
        }

        public sealed override int GetHashCode()
        {
            return _common.GetHashCode();
        }

        public sealed override string ToString()
        {
            return RuntimeMethodHelpers.ComputeToString(ref _common, this, Array.Empty<RuntimeTypeInfo>());
        }

        public sealed override RuntimeMethodHandle MethodHandle => _common.GetRuntimeMethodHandle(null);

        protected sealed override RuntimeParameterInfo[] RuntimeParameters
        {
            get
            {
                return _lazyParameters ??= RuntimeMethodHelpers.GetRuntimeParameters(ref _common, this, Array.Empty<RuntimeTypeInfo>(), out _);
            }
        }

        protected sealed override MethodInvoker UncachedMethodInvoker
        {
            get
            {
                if (_common.DefiningTypeInfo.IsAbstract)
                    throw new MemberAccessException(SR.Format(SR.Acc_CreateAbstEx, _common.DefiningTypeInfo.FullName));

                if (this.IsStatic)
                    throw new MemberAccessException(SR.Acc_NotClassInit);

                MethodInvoker invoker = this.GetCustomMethodInvokerIfNeeded();
                if (invoker != null)
                    return invoker;

                invoker = _common.GetUncachedMethodInvoker(Array.Empty<RuntimeTypeInfo>(), this, out Exception exception);
                if (invoker == null)
                    throw exception;

                return invoker;
            }
        }

        private volatile RuntimeParameterInfo[] _lazyParameters;
        private TRuntimeMethodCommon _common;
    }
}
