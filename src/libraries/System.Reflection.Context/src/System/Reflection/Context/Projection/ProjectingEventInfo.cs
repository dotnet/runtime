// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given event
    internal class ProjectingEventInfo : DelegatingEventInfo, IProjectable
    {
        public ProjectingEventInfo(EventInfo @event, Projector projector)
            : base(@event)
        {
            Debug.Assert(null != projector);

            Projector = projector;
        }

        public Projector Projector { get; }

        public override Type DeclaringType
        {
            get { return Projector.ProjectType(base.DeclaringType); }
        }

        public override Type EventHandlerType
        {
            get { return Projector.ProjectType(base.EventHandlerType); }
        }

        public override Module Module
        {
            get { return Projector.ProjectModule(base.Module); }
        }
        public override Type ReflectedType
        {
            get { return Projector.ProjectType(base.ReflectedType); }
        }

        public override MethodInfo GetAddMethod(bool nonPublic)
        {
            return Projector.ProjectMethod(base.GetAddMethod(nonPublic));
        }

        public override MethodInfo[] GetOtherMethods(bool nonPublic)
        {
            return Projector.Project(base.GetOtherMethods(nonPublic), Projector.ProjectMethod);
        }

        public override MethodInfo GetRaiseMethod(bool nonPublic)
        {
            return Projector.ProjectMethod(base.GetRaiseMethod(nonPublic));
        }

        public override MethodInfo GetRemoveMethod(bool nonPublic)
        {
            return Projector.ProjectMethod(base.GetRemoveMethod(nonPublic));
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

        public override bool Equals(object o)
        {
            var other = o as ProjectingEventInfo;

            return other != null &&
                   Projector == other.Projector &&
                   UnderlyingEvent.Equals(other.UnderlyingEvent);
        }

        public override int GetHashCode()
        {
            return Projector.GetHashCode() ^ UnderlyingEvent.GetHashCode();
        }
    }
}
