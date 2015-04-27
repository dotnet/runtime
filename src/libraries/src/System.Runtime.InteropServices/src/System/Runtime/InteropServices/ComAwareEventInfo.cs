// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.InteropServices
{
    [System.Security.SecuritySafeCritical]
    public class ComAwareEventInfo : System.Reflection.EventInfo
    {
        private System.Reflection.EventInfo _innerEventInfo;

        public ComAwareEventInfo(Type type, string eventName)
        {
            _innerEventInfo = type.GetEvent(eventName);
        }

        [System.Security.SecuritySafeCritical]
        public override void AddEventHandler(object target, Delegate handler)
        {
            if (Marshal.IsComObject(target))
            {
                // retrieve sourceIid and dispid
                Guid sourceIid;
                int dispid;
                GetDataForComInvocation(_innerEventInfo, out sourceIid, out dispid);
                System.Runtime.InteropServices.ComEventsHelper.Combine(target, sourceIid, dispid, handler);
            }
            else
            {
                // we are dealing with a managed object - just add the delegate through reflection
                _innerEventInfo.AddEventHandler(target, handler);
            }
        }

        [System.Security.SecuritySafeCritical]
        public override void RemoveEventHandler(object target, Delegate handler)
        {
            if (Marshal.IsComObject(target))
            {
                // retrieve sourceIid and dispid
                Guid sourceIid;
                int dispid;
                GetDataForComInvocation(_innerEventInfo, out sourceIid, out dispid);

                System.Runtime.InteropServices.ComEventsHelper.Remove(target, sourceIid, dispid, handler);
            }
            else
            {
                // we are dealing with a managed object - just add the delegate through relection
                _innerEventInfo.RemoveEventHandler(target, handler);
            }
        }

        public override System.Reflection.EventAttributes Attributes
        {
            get { return _innerEventInfo.Attributes; }
        }

        public override System.Reflection.MethodInfo GetAddMethod(bool nonPublic)
        {
            return _innerEventInfo.GetAddMethod(nonPublic);
        }

        public override System.Reflection.MethodInfo GetRaiseMethod(bool nonPublic)
        {
            return _innerEventInfo.GetRaiseMethod(nonPublic);
        }

        public override System.Reflection.MethodInfo GetRemoveMethod(bool nonPublic)
        {
            return _innerEventInfo.GetRemoveMethod(nonPublic);
        }

        public override Type DeclaringType
        {
            get { return _innerEventInfo.DeclaringType; }
        }

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

        public override string Name
        {
            get { return _innerEventInfo.Name; }
        }

        public override Type ReflectedType
        {
            get { return _innerEventInfo.ReflectedType; }
        }

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
                throw new System.Reflection.AmbiguousMatchException("more than one ComSourceInterfaceGuidAttribute found");
            }

            Type sourceItf = ((ComEventInterfaceAttribute)comEventInterfaces[0]).SourceInterface;
            Guid guid = sourceItf.GUID;

            System.Reflection.MethodInfo methodInfo = sourceItf.GetMethod(eventInfo.Name);
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
