// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given constructor
    internal class ProjectingConstructorInfo : DelegatingConstructorInfo, IProjectable
    {
        public ProjectingConstructorInfo(ConstructorInfo constructor, Projector projector)
            : base(constructor)
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

        public override Type[] GetGenericArguments()
        {
            return Projector.Project(base.GetGenericArguments(), Projector.ProjectType);
        }

        public override MethodBody? GetMethodBody()
        {
            return Projector.ProjectMethodBody(base.GetMethodBody());
        }

        public override ParameterInfo[] GetParameters()
        {
            return Projector.Project(base.GetParameters(), Projector.ProjectParameter);
        }

        public override bool Equals(object? o)
        {
            return o is ProjectingConstructorInfo other &&
                   Projector == other.Projector &&
                   UnderlyingConstructor.Equals(other.UnderlyingConstructor);
        }

        public override int GetHashCode()
        {
            return Projector.GetHashCode() ^ UnderlyingConstructor.GetHashCode();
        }
    }
}
