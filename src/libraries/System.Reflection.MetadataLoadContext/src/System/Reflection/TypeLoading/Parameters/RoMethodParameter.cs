// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all RoParameter's returned by MethodBase.GetParameters().
    /// </summary>
    internal abstract class RoMethodParameter : RoParameter
    {
        private readonly Type _parameterType;

        protected RoMethodParameter(IRoMethodBase roMethodBase, int position, Type parameterType) 
            : base(roMethodBase.MethodBase, position)
        {
            Debug.Assert(roMethodBase != null);
            Debug.Assert(parameterType != null);

            _parameterType = parameterType;
        }

        public sealed override Type ParameterType => _parameterType;

        public sealed override Type[] GetOptionalCustomModifiers() => GetRoMethodBase().GetCustomModifiers(Position, isRequired: false).CloneArray();
        public sealed override Type[] GetRequiredCustomModifiers() => GetRoMethodBase().GetCustomModifiers(Position, isRequired: true).CloneArray();

        public sealed override string ToString() => Loader.GetDisposedString() ?? GetRoMethodBase().GetMethodSigString(Position) + " " + Name;

        internal IRoMethodBase GetRoMethodBase() => (IRoMethodBase)Member;
        private MetadataLoadContext Loader => GetRoMethodBase().Loader;
    }
}
