// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given property
    internal class ProjectingPropertyInfo : DelegatingPropertyInfo, IProjectable
    {
        public ProjectingPropertyInfo(PropertyInfo property, Projector projector)
            : base(property)
        {
            Debug.Assert(null != projector);

            Projector = projector;
        }

        public Projector Projector { get; }

        public override Type? DeclaringType
        {
            get { return Projector.ProjectType(base.DeclaringType); }
        }

        public override Module Module
        {
            get { return Projector.ProjectModule(base.Module); }
        }

        public override Type PropertyType
        {
            get { return Projector.ProjectType(base.PropertyType); }
        }

        public override Type? ReflectedType
        {
            get { return Projector.ProjectType(base.ReflectedType); }
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            return Projector.Project(base.GetAccessors(nonPublic), Projector.ProjectMethod);
        }

        public override MethodInfo? GetGetMethod(bool nonPublic)
        {
            return Projector.ProjectMethod(base.GetGetMethod(nonPublic));
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            return Projector.Project(base.GetIndexParameters(), Projector.ProjectParameter);
        }

        public override MethodInfo? GetSetMethod(bool nonPublic)
        {
            return Projector.ProjectMethod(base.GetSetMethod(nonPublic));
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

        public override bool Equals([NotNullWhen(true)] object? o)
        {
            return o is ProjectingPropertyInfo other &&
                Projector == other.Projector &&
                UnderlyingProperty.Equals(other.UnderlyingProperty);
        }

        public override int GetHashCode()
        {
            return Projector.GetHashCode() ^ UnderlyingProperty.GetHashCode();
        }
    }
}
