// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given parameter
    internal class ProjectingParameterInfo : DelegatingParameterInfo, IProjectable
    {
        public ProjectingParameterInfo(ParameterInfo parameter, Projector projector)
            : base(parameter)
        {
            Debug.Assert(null != projector);

            Projector = projector;
        }

        public Projector Projector { get; }

        public override MemberInfo Member
        {
            get { return Projector.ProjectMember(base.Member); }
        }

        public override Type ParameterType
        {
            get { return Projector.ProjectType(base.ParameterType); }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            attributeType = Projector.Unproject(attributeType);

            return base.GetCustomAttributes(attributeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return Projector.Project(base.GetCustomAttributesData(), Projector.ProjectCustomAttributeData);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            attributeType = Projector.Unproject(attributeType);

            return base.IsDefined(attributeType, inherit);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return Projector.Project(base.GetOptionalCustomModifiers(), Projector.ProjectType);
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return Projector.Project(base.GetRequiredCustomModifiers(), Projector.ProjectType);
        }

        public override bool Equals(object o)
        {
            return o is ProjectingParameterInfo other &&
                Projector == other.Projector &&
                UnderlyingParameter.Equals(other.UnderlyingParameter);
        }

        public override int GetHashCode()
        {
            return Projector.GetHashCode() ^ UnderlyingParameter.GetHashCode();
        }
    }
}
