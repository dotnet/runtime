// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members for a given field
    internal class ProjectingFieldInfo : DelegatingFieldInfo, IProjectable
    {
        public ProjectingFieldInfo(FieldInfo field, Projector projector)
            : base(field)
        {
            Debug.Assert(null != projector);

            Projector = projector;
        }

        public Projector Projector { get; }

        public override Type? DeclaringType
        {
            get { return Projector.ProjectType(base.DeclaringType); }
        }

        public override Type FieldType
        {
            get { return Projector.ProjectType(base.FieldType); }
        }

        public override Module Module
        {
            get { return Projector.ProjectModule(base.Module); }
        }

        public override Type? ReflectedType
        {
            get { return Projector.ProjectType(base.ReflectedType); }
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
            return o is ProjectingFieldInfo other &&
                   Projector == other.Projector &&
                   UnderlyingField.Equals(other.UnderlyingField);
        }

        public override int GetHashCode()
        {
            return Projector.GetHashCode() ^ UnderlyingField.GetHashCode();
        }
    }
}
