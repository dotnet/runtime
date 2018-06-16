// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given method
    internal class ProjectingMethodInfo : DelegatingMethodInfo, IProjectable
    {
        public ProjectingMethodInfo(MethodInfo method, Projector projector)
            : base(method)
        {
            Debug.Assert(null != projector);

            Projector = projector;
        }

        public Projector Projector { get; }

        public override Type DeclaringType
        {
            get { return Projector.ProjectType(base.DeclaringType); }
        }

        public override Module Module
        {
            get { return Projector.ProjectModule(base.Module); }
        }

        public override Type ReflectedType
        {
            get { return Projector.ProjectType(base.ReflectedType); }
        }

        public override ParameterInfo ReturnParameter
        {
            get { return Projector.ProjectParameter(base.ReturnParameter); }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get
            {
                // We should just return MethodInfo.ReturnParameter here
                // but DynamicMethod returns a fake ICustomAttributeProvider.
                ICustomAttributeProvider provider = base.ReturnTypeCustomAttributes;
                if (provider is ParameterInfo)
                    return Projector.ProjectParameter(ReturnParameter);
                else
                    return provider;
            }
        }

        public override Type ReturnType
        {
            get { return Projector.ProjectType(base.ReturnType); }
        }
      
        public override MethodInfo GetBaseDefinition()
        {
            return Projector.ProjectMethod(base.GetBaseDefinition());
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

        public override MethodInfo GetGenericMethodDefinition()
        {
            return Projector.ProjectMethod(base.GetGenericMethodDefinition());
        }

        public override MethodBody GetMethodBody()
        {
            return Projector.ProjectMethodBody(base.GetMethodBody());
        }

        public override ParameterInfo[] GetParameters()
        {
            return Projector.Project(base.GetParameters(), Projector.ProjectParameter);
        }
   
        public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            return Projector.ProjectMethod(base.MakeGenericMethod(Projector.Unproject(typeArguments)));            
        }

        public override Delegate CreateDelegate(Type delegateType)
        {
            return base.CreateDelegate(Projector.Unproject(delegateType));
        }

        public override Delegate CreateDelegate(Type delegateType, object target)
        {
            return base.CreateDelegate(Projector.Unproject(delegateType), target);
        }

        public override bool Equals(object o)
        {
            var other = o as ProjectingMethodInfo;

            return other != null &&
                   Projector == other.Projector &&
                   UnderlyingMethod.Equals(other.UnderlyingMethod);
        }

        public override int GetHashCode()
        {
            return Projector.GetHashCode() ^ UnderlyingMethod.GetHashCode();
        }
    }
}
