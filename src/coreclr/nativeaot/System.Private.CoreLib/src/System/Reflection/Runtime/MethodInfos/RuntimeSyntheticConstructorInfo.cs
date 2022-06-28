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
    // The runtime's implementation of constructors exposed on array types.
    //
    internal sealed partial class RuntimeSyntheticConstructorInfo : RuntimeConstructorInfo, IRuntimeMemberInfoWithNoMetadataDefinition
    {
        private RuntimeSyntheticConstructorInfo(SyntheticMethodId syntheticMethodId, RuntimeArrayTypeInfo declaringType, RuntimeTypeInfo[] runtimeParameterTypes, InvokerOptions options, CustomMethodInvokerAction action)
        {
            _syntheticMethodId = syntheticMethodId;
            _declaringType = declaringType;
            _options = options;
            _action = action;
            _runtimeParameterTypes = runtimeParameterTypes;
        }

        public sealed override MethodAttributes Attributes
        {
            get
            {
                return MethodAttributes.Public | MethodAttributes.PrivateScope | MethodAttributes.RTSpecialName;
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

        public sealed override Type DeclaringType
        {
            get
            {
                return _declaringType;
            }
        }

        public sealed override MethodBase MetadataDefinitionMethod
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public sealed override int MetadataToken
        {
            get
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }
        }

        [DebuggerGuidedStepThrough]
        public sealed override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            if (parameters == null)
                parameters = Array.Empty<object>();

            object ctorAllocatedObject = this.MethodInvoker.Invoke(null, parameters, binder, invokeAttr, culture)!;
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return ctorAllocatedObject;
        }

        public sealed override MethodImplAttributes MethodImplementationFlags
        {
            get
            {
                return MethodImplAttributes.IL;
            }
        }

        public sealed override string Name
        {
            get
            {
                return ConstructorName;
            }
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // This logic is written to match CoreCLR's behavior.
            return other is ConstructorInfo && other is IRuntimeMemberInfoWithNoMetadataDefinition;
        }

        public sealed override bool Equals(object obj)
        {
            if (!(obj is RuntimeSyntheticConstructorInfo other))
                return false;
            if (_syntheticMethodId != other._syntheticMethodId)
                return false;
            if (!(_declaringType.Equals(other._declaringType)))
                return false;
            return true;
        }

        public sealed override int GetHashCode()
        {
            return _declaringType.GetHashCode();
        }

        public sealed override string ToString()
        {
            // A constructor's "return type" is always System.Void and we don't want to allocate a ParameterInfo object to record that revelation.
            // In deference to that, ComputeToString() lets us pass null as a synonym for "void."
            return RuntimeMethodHelpers.ComputeToString(this, Array.Empty<RuntimeTypeInfo>(), RuntimeParameters, returnParameter: null);
        }

        public sealed override RuntimeMethodHandle MethodHandle
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        protected sealed override RuntimeParameterInfo[] RuntimeParameters
        {
            get
            {
                RuntimeParameterInfo[] parameters = _lazyParameters;
                if (parameters == null)
                {
                    RuntimeTypeInfo[] runtimeParameterTypes = _runtimeParameterTypes;
                    parameters = new RuntimeParameterInfo[runtimeParameterTypes.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        parameters[i] = RuntimeSyntheticParameterInfo.GetRuntimeSyntheticParameterInfo(this, i, runtimeParameterTypes[i]);
                    }
                    _lazyParameters = parameters;
                }
                return parameters;
            }
        }

        protected sealed override MethodInvoker UncachedMethodInvoker => new CustomMethodInvoker(_declaringType, _runtimeParameterTypes, _options, _action);

        private volatile RuntimeParameterInfo[] _lazyParameters;

        private readonly SyntheticMethodId _syntheticMethodId;
        private readonly RuntimeArrayTypeInfo _declaringType;
        private readonly RuntimeTypeInfo[] _runtimeParameterTypes;
        private readonly InvokerOptions _options;
        private readonly CustomMethodInvokerAction _action;
    }
}
