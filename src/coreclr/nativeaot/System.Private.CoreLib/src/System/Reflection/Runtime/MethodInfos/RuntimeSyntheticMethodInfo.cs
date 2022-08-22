// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.ParameterInfos;

using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    //
    // These methods implement the Get/Set methods on array types.
    //
    internal sealed partial class RuntimeSyntheticMethodInfo : RuntimeMethodInfo, IRuntimeMemberInfoWithNoMetadataDefinition
    {
        private RuntimeSyntheticMethodInfo(SyntheticMethodId syntheticMethodId, string name, RuntimeArrayTypeInfo declaringType, RuntimeTypeInfo[] parameterTypes, RuntimeTypeInfo returnType, InvokerOptions options, CustomMethodInvokerAction action)
        {
            _syntheticMethodId = syntheticMethodId;
            _name = name;
            _declaringType = declaringType;
            _options = options;
            _action = action;
            _runtimeParameterTypes = parameterTypes;
            _returnType = returnType;
        }

        public sealed override MethodAttributes Attributes
        {
            get
            {
                return MethodAttributes.Public | MethodAttributes.PrivateScope;
            }
        }

        public sealed override CallingConventions CallingConvention
        {
            get
            {
                return CallingConventions.Standard | CallingConventions.HasThis;
            }
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return Array.Empty<CustomAttributeData>();
            }
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // This logic is written to match CoreCLR's behavior.
            return other is MethodInfo && other is IRuntimeMemberInfoWithNoMetadataDefinition;
        }

        public sealed override bool Equals(object obj)
        {
            if (!(obj is RuntimeSyntheticMethodInfo other))
                return false;
            if (_syntheticMethodId != other._syntheticMethodId)
                return false;
            if (!(_declaringType.Equals(other._declaringType)))
                return false;
            return true;
        }

        public sealed override MethodInfo GetGenericMethodDefinition()
        {
            throw new InvalidOperationException();
        }

        public sealed override int GetHashCode()
        {
            return _declaringType.GetHashCode();
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

        internal sealed override int GenericParameterCount => 0;

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public sealed override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));
        }

        public sealed override MethodBase MetadataDefinitionMethod
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public sealed override MethodImplAttributes MethodImplementationFlags
        {
            get
            {
                return MethodImplAttributes.IL;
            }
        }

        public sealed override Module Module
        {
            get
            {
                return this.DeclaringType.Assembly.ManifestModule;
            }
        }

        public sealed override int MetadataToken
        {
            get
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }
        }

        public sealed override Type ReflectedType
        {
            get
            {
                // The only synthetic methods come from array types which can never be inherited from. So unless that changes,
                // we don't provide a way to specify the ReflectedType.
                return DeclaringType;
            }
        }

        public sealed override string ToString()
        {
            return RuntimeMethodHelpers.ComputeToString(this, Array.Empty<RuntimeTypeInfo>(), RuntimeParameters, RuntimeReturnParameter);
        }

        public sealed override RuntimeMethodHandle MethodHandle
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        protected sealed override MethodInvoker UncachedMethodInvoker => new CustomMethodInvoker(_declaringType, _runtimeParameterTypes, _options, _action);

        internal sealed override RuntimeTypeInfo[] RuntimeGenericArgumentsOrParameters
        {
            get
            {
                return Array.Empty<RuntimeTypeInfo>();
            }
        }

        internal sealed override RuntimeTypeInfo RuntimeDeclaringType
        {
            get
            {
                return _declaringType;
            }
        }

        internal sealed override string RuntimeName
        {
            get
            {
                return _name;
            }
        }

        internal sealed override RuntimeParameterInfo[] GetRuntimeParameters(RuntimeMethodInfo contextMethod, out RuntimeParameterInfo returnParameter)
        {
            RuntimeTypeInfo[] runtimeParameterTypes = _runtimeParameterTypes;
            RuntimeParameterInfo[] parameters = new RuntimeParameterInfo[runtimeParameterTypes.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = RuntimeSyntheticParameterInfo.GetRuntimeSyntheticParameterInfo(this, i, runtimeParameterTypes[i]);
            }
            returnParameter = RuntimeSyntheticParameterInfo.GetRuntimeSyntheticParameterInfo(this, -1, _returnType);
            return parameters;
        }

        internal sealed override RuntimeMethodInfo WithReflectedTypeSetToDeclaringType
        {
            get
            {
                Debug.Assert(ReflectedType.Equals(DeclaringType));
                return this;
            }
        }

        private readonly string _name;
        private readonly SyntheticMethodId _syntheticMethodId;
        private readonly RuntimeArrayTypeInfo _declaringType;
        private readonly RuntimeTypeInfo[] _runtimeParameterTypes;
        private readonly RuntimeTypeInfo _returnType;
        private readonly InvokerOptions _options;
        private readonly CustomMethodInvokerAction _action;
    }
}
