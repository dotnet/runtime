// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all RoParameter's returned by MethodBase.GetParameters().
    /// </summary>
    internal abstract class RoMethodParameter : RoParameter
    {
        private readonly Type _parameterType;
        private RoModifiedType? _modifiedType;

        protected RoMethodParameter(IRoMethodBase roMethodBase, int position, Type parameterType)
            : base(roMethodBase.MethodBase, position)
        {
            Debug.Assert(roMethodBase != null);
            Debug.Assert(parameterType != null);

            if (parameterType is RoModifiedType modifiedType)
            {
                _modifiedType = modifiedType;
                _parameterType = parameterType.UnderlyingSystemType;
            }
            else
            {
                _parameterType = parameterType;
            }
        }

        public sealed override Type ParameterType => _parameterType;

        protected RoModifiedType ModifiedType
        {
            get
            {
                _modifiedType ??= RoModifiedType.Create((RoType)_parameterType);
                return _modifiedType;
            }
        }

        public sealed override Type[] GetOptionalCustomModifiers() => ModifiedType.GetOptionalCustomModifiers();
        public sealed override Type[] GetRequiredCustomModifiers() => ModifiedType.GetRequiredCustomModifiers();

        public sealed override Type GetModifiedParameterType()
        {
            return ModifiedType;
        }

        public sealed override string ToString() => Loader.GetDisposedString() ?? GetRoMethodBase().GetMethodSigString(Position) + " " + Name;

        internal IRoMethodBase GetRoMethodBase() => (IRoMethodBase)Member;
        private MetadataLoadContext Loader => GetRoMethodBase().Loader;
    }
}
