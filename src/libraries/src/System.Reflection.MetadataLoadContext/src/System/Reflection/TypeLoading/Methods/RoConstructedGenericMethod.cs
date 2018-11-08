// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Class for all RoMethod objects created by a MetadataLoadContext for which IsConstructedGenericMethod returns true.
    /// </summary>
    internal sealed partial class RoConstructedGenericMethod : RoMethod
    {
        private readonly RoDefinitionMethod _genericMethodDefinition;
        private readonly RoType[] _genericMethodArguments;

        internal RoConstructedGenericMethod(RoDefinitionMethod genericMethodDefinition, RoType[] genericMethodArguments) 
            : base(genericMethodDefinition.ReflectedType)
        {
            Debug.Assert(genericMethodDefinition != null);
            Debug.Assert(genericMethodArguments != null);

            _genericMethodDefinition = genericMethodDefinition;
            _genericMethodArguments = genericMethodArguments;
        }

        internal sealed override RoType GetRoDeclaringType() => _genericMethodDefinition.GetRoDeclaringType();
        internal sealed override RoModule GetRoModule() => _genericMethodDefinition.GetRoModule();

        protected sealed override string ComputeName() => _genericMethodDefinition.Name;
        public sealed override int MetadataToken => _genericMethodDefinition.MetadataToken;
        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => _genericMethodDefinition.CustomAttributes;
        public sealed override bool IsConstructedGenericMethod => true;
        public sealed override bool IsGenericMethodDefinition => false;
        protected sealed override MethodAttributes ComputeAttributes() => _genericMethodDefinition.Attributes;
        protected sealed override CallingConventions ComputeCallingConvention() => _genericMethodDefinition.CallingConvention;
        protected sealed override MethodImplAttributes ComputeMethodImplementationFlags() => _genericMethodDefinition.MethodImplementationFlags;

        protected sealed override MethodSig<RoParameter> ComputeMethodSig() => _genericMethodDefinition.SpecializeMethodSig(this);
        protected sealed override MethodSig<RoType> ComputeCustomModifiers() => _genericMethodDefinition.SpecializeCustomModifiers(TypeContext);

        public sealed override MethodBody GetMethodBody() => _genericMethodDefinition.SpecializeMethodBody(this);

        protected sealed override RoType[] ComputeGenericArgumentsOrParameters() => _genericMethodArguments;

        internal sealed override RoType[] GetGenericTypeArgumentsNoCopy() => _genericMethodArguments;
        internal sealed override RoType[] GetGenericTypeParametersNoCopy() => Array.Empty<RoType>();

        public sealed override MethodInfo GetGenericMethodDefinition() => _genericMethodDefinition;

        public sealed override MethodInfo MakeGenericMethod(params Type[] typeArguments) => throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));

        public sealed override bool Equals(object obj)
        {
            if (!(obj is RoConstructedGenericMethod other))
                return false;

            if (!(_genericMethodDefinition == other._genericMethodDefinition))
                return false;

            if (_genericMethodArguments.Length != other._genericMethodArguments.Length)
                return false;

            for (int i = 0; i < _genericMethodArguments.Length; i++)
            {
                if (_genericMethodArguments[i] != other._genericMethodArguments[i])
                    return false;
            }

            return true;
        }

        public sealed override int GetHashCode()
        {
            int hashCode = _genericMethodDefinition.GetHashCode();
            foreach (Type genericMethodArgument in _genericMethodArguments)
            {
                hashCode ^= genericMethodArgument.GetHashCode();
            }
            return hashCode;
        }

        protected sealed override MethodSig<string> ComputeMethodSigStrings() => _genericMethodDefinition.SpecializeMethodSigStrings(TypeContext);

        public sealed override TypeContext TypeContext => new TypeContext(_genericMethodDefinition.TypeContext.GenericTypeArguments, _genericMethodArguments);
    }
}
