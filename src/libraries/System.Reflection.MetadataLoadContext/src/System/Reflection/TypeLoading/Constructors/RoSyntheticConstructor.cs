// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all RoConstructors objects created by a MetadataLoadContext that appear on arrays.
    /// </summary>
    internal sealed partial class RoSyntheticConstructor : RoConstructor
    {
        private readonly RoType _declaringType;
        private readonly int _uniquifier;  // Since all array methods have the same "MetadataToken", this serves as a distinguisher so they don't all compare Equal
        private readonly RoType[] _parameterTypes;

        internal RoSyntheticConstructor(RoType declaringType, int uniquifier, params RoType[] parameterTypes)
            : base()
        {
            Debug.Assert(declaringType != null);
            _declaringType = declaringType;
            _uniquifier = uniquifier;
            _parameterTypes = parameterTypes;
        }

        internal sealed override RoType GetRoDeclaringType() => _declaringType;
        internal sealed override RoModule GetRoModule() => GetRoDeclaringType().GetRoModule();

        protected sealed override string ComputeName() => ConstructorInfo.ConstructorName;
        public sealed override int MetadataToken => 0x06000000;
        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => Array.Empty<CustomAttributeData>();
        protected sealed override MethodAttributes ComputeAttributes() => MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.RTSpecialName;
        protected sealed override CallingConventions ComputeCallingConvention() => CallingConventions.Standard | CallingConventions.HasThis;
        protected sealed override MethodImplAttributes ComputeMethodImplementationFlags() => MethodImplAttributes.IL;

        protected sealed override MethodSig<RoParameter> ComputeMethodSig()
        {
            int parameterCount = _parameterTypes.Length;
            MethodSig<RoParameter> sig = new MethodSig<RoParameter>(parameterCount);
            RoType returnType = GetRoModule().Loader.GetCoreType(CoreType.Void);
            sig[-1] = new RoThinMethodParameter(this, -1, returnType);
            for (int position = 0; position < parameterCount; position++)
            {
                sig[position] = new RoThinMethodParameter(this, position, _parameterTypes[position]);
            }
            return sig;
        }

        public sealed override MethodBody? GetMethodBody() => null;

        protected sealed override MethodSig<string> ComputeMethodSigStrings()
        {
            int parameterCount = _parameterTypes.Length;
            MethodSig<string> sig = new MethodSig<string>(parameterCount);
            MethodSig<RoParameter> psig = ComputeMethodSig();
            for (int position = -1; position < parameterCount; position++)
            {
                sig[position] = psig[position].ParameterType.ToString();
            }
            return sig;
        }

        public sealed override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is RoSyntheticConstructor other))
                return false;

            if (DeclaringType != other.DeclaringType)
                return false;

            if (_uniquifier != other._uniquifier)
                return false;

            return true;
        }

        public sealed override int GetHashCode() => DeclaringType.GetHashCode() ^ _uniquifier.GetHashCode();

        public sealed override TypeContext TypeContext => default;
    }
}
