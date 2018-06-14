// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.Context.Delegation
{
    internal class DelegatingParameterInfo : ParameterInfo
    {
        public DelegatingParameterInfo(ParameterInfo parameter)
        {
            Debug.Assert(null != parameter);

            UnderlyingParameter = parameter;
        }

        public override ParameterAttributes Attributes
        {
            get { return UnderlyingParameter.Attributes; }
        }

        public override object DefaultValue
        {
            get { return UnderlyingParameter.DefaultValue; }
        }

        public override MemberInfo Member
        {
            get { return UnderlyingParameter.Member; }
        }

        public override int MetadataToken
        {
            get { return UnderlyingParameter.MetadataToken; }
        }

        public override string Name
        {
            get { return UnderlyingParameter.Name; }
        }

        public override Type ParameterType
        {
            get { return UnderlyingParameter.ParameterType; }
        }

        public override int Position
        {
            get { return UnderlyingParameter.Position; }
        }

        public override object RawDefaultValue
        {
            get { return UnderlyingParameter.RawDefaultValue; }
        }

        public ParameterInfo UnderlyingParameter { get; }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return UnderlyingParameter.GetCustomAttributes(attributeType, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return UnderlyingParameter.GetCustomAttributes(inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return UnderlyingParameter.GetCustomAttributesData();
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return UnderlyingParameter.GetOptionalCustomModifiers();
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return UnderlyingParameter.GetRequiredCustomModifiers();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return UnderlyingParameter.IsDefined(attributeType, inherit);
        }

        public override string ToString()
        {
            return UnderlyingParameter.ToString();
        }
	}
}
