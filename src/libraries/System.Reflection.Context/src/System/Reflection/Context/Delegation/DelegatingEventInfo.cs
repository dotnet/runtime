// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.Context.Delegation
{
    internal class DelegatingEventInfo : EventInfo
    {
        public DelegatingEventInfo(EventInfo @event)
        {
            Debug.Assert(null != @event);

            UnderlyingEvent = @event;
        }

        public override EventAttributes Attributes
        {
            get { return UnderlyingEvent.Attributes; }
        }

        public override Type DeclaringType
        {
            get { return UnderlyingEvent.DeclaringType; }
        }

        public override Type EventHandlerType
        {
            get { return UnderlyingEvent.EventHandlerType; }
        }

        public override bool IsMulticast
        {
            get { return UnderlyingEvent.IsMulticast; }
        }

        public override int MetadataToken
        {
            get { return UnderlyingEvent.MetadataToken; }
        }

        public override Module Module
        {
            get { return UnderlyingEvent.Module; }
        }

        public override string Name
        {
            get { return UnderlyingEvent.Name; }
        }

        public override Type ReflectedType
        {
            get { return UnderlyingEvent.ReflectedType; }
        }

        public EventInfo UnderlyingEvent { get; }

        public override void AddEventHandler(object target, Delegate handler)
        {
            UnderlyingEvent.AddEventHandler(target, handler);
        }

        public override MethodInfo GetAddMethod(bool nonPublic)
        {
            return UnderlyingEvent.GetAddMethod(nonPublic);
        }

        public override MethodInfo[] GetOtherMethods(bool nonPublic)
        {
            return UnderlyingEvent.GetOtherMethods(nonPublic);
        }

        public override MethodInfo GetRaiseMethod(bool nonPublic)
        {
            return UnderlyingEvent.GetRaiseMethod(nonPublic);
        }

        public override MethodInfo GetRemoveMethod(bool nonPublic)
        {
            return UnderlyingEvent.GetRemoveMethod(nonPublic);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return UnderlyingEvent.GetCustomAttributes(attributeType, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return UnderlyingEvent.GetCustomAttributes(inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return UnderlyingEvent.GetCustomAttributesData();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return UnderlyingEvent.IsDefined(attributeType, inherit);
        }

        public override void RemoveEventHandler(object target, Delegate handler)
        {
            UnderlyingEvent.RemoveEventHandler(target, handler);
        }

        public override string ToString()
        {
            return UnderlyingEvent.ToString();
        }
    }
}
