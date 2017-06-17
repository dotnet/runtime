// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Security;

namespace System.Runtime.InteropServices
{
    [SecuritySafeCritical]
    public class ComAwareEventInfo : EventInfo
    {
        private EventInfo _innerEventInfo;

        public ComAwareEventInfo(Type type, string eventName)
        {
            _innerEventInfo = type.GetEvent(eventName);
        }

        [SecuritySafeCritical]
        public override void AddEventHandler(object target, Delegate handler)
        {
            if (Marshal.IsComObject(target))
            {
                // retrieve sourceIid and dispid
                GetDataForComInvocation(_innerEventInfo, out Guid sourceIid, out int dispid);
                ComEventsHelper.Combine(target, sourceIid, dispid, handler);
            }
            else
            {
                // we are dealing with a managed object - just add the delegate through reflection
                _innerEventInfo.AddEventHandler(target, handler);
            }
        }

        [SecuritySafeCritical]
        public override void RemoveEventHandler(object target, Delegate handler)
        {
            if (Marshal.IsComObject(target))
            {
                // retrieve sourceIid and dispid
                GetDataForComInvocation(_innerEventInfo, out Guid sourceIid, out int dispid);

                ComEventsHelper.Remove(target, sourceIid, dispid, handler);
            }
            else
            {
                // we are dealing with a managed object - just add the delegate through reflection
                _innerEventInfo.RemoveEventHandler(target, handler);
            }
        }

        public override EventAttributes Attributes => _innerEventInfo.Attributes;

        public override MethodInfo GetAddMethod(bool nonPublic) => _innerEventInfo.GetAddMethod(nonPublic);

        public override MethodInfo GetRaiseMethod(bool nonPublic) => _innerEventInfo.GetRaiseMethod(nonPublic);

        public override MethodInfo GetRemoveMethod(bool nonPublic) => _innerEventInfo.GetRemoveMethod(nonPublic);

        public override Type DeclaringType => _innerEventInfo.DeclaringType;

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return _innerEventInfo.GetCustomAttributes(attributeType, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return _innerEventInfo.GetCustomAttributes(inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return _innerEventInfo.IsDefined(attributeType, inherit);
        }

        public override string Name => _innerEventInfo.Name;

        public override Type ReflectedType => _innerEventInfo.ReflectedType;

        private static void GetDataForComInvocation(System.Reflection.EventInfo eventInfo, out Guid sourceIid, out int dispid)
        {
            object[] comEventInterfaces = eventInfo.DeclaringType.GetCustomAttributes(typeof(ComEventInterfaceAttribute), false);

            if (comEventInterfaces == null || comEventInterfaces.Length == 0)
            {
                // TODO: event strings need to be localizable
                throw new InvalidOperationException("event invocation for COM objects requires interface to be attributed with ComSourceInterfaceGuidAttribute");
            }

            if (comEventInterfaces.Length > 1)
            {
                // TODO: event strings need to be localizable
                throw new AmbiguousMatchException("more than one ComSourceInterfaceGuidAttribute found");
            }

            Type sourceItf = ((ComEventInterfaceAttribute)comEventInterfaces[0]).SourceInterface;
            Guid guid = sourceItf.GUID;

            MethodInfo methodInfo = sourceItf.GetMethod(eventInfo.Name);
            Attribute dispIdAttribute = Attribute.GetCustomAttribute(methodInfo, typeof(DispIdAttribute));
            if (dispIdAttribute == null)
            {
                // TODO: event strings need to be localizable
                throw new InvalidOperationException("event invocation for COM objects requires event to be attributed with DispIdAttribute");
            }

            sourceIid = guid;
            dispid = ((DispIdAttribute)dispIdAttribute).Value;
        }
    }
}
