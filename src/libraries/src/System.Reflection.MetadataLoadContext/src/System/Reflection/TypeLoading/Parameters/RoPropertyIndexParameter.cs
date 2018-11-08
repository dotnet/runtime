// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all RoParameter's returned by PropertyInfo.GetParameters(). These are identical to the associated
    /// getter's ParameterInfo's except for the Member property returning a property.
    /// </summary>
    internal sealed class RoPropertyIndexParameter : RoParameter
    {
        private readonly RoParameter _backingParameter;

        internal RoPropertyIndexParameter(RoProperty member, RoParameter backingParameter) 
            : base(member, backingParameter.Position)
        {
            Debug.Assert(member != null);
            Debug.Assert(backingParameter != null);

            _backingParameter = backingParameter;
        }

        public sealed override int MetadataToken => _backingParameter.MetadataToken;
        public sealed override string Name => _backingParameter.Name;
        public sealed override Type ParameterType => _backingParameter.ParameterType;
        public sealed override ParameterAttributes Attributes => _backingParameter.Attributes;
        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => _backingParameter.CustomAttributes;
        public sealed override bool HasDefaultValue => _backingParameter.HasDefaultValue;
        public sealed override object RawDefaultValue => _backingParameter.RawDefaultValue;
        public sealed override Type[] GetOptionalCustomModifiers() => _backingParameter.GetOptionalCustomModifiers();
        public sealed override Type[] GetRequiredCustomModifiers() => _backingParameter.GetRequiredCustomModifiers();
        public sealed override string ToString() => _backingParameter.ToString();
    }
}
