// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*============================================================
**
**
** Purpose: ComEventHelpers APIs allow binding 
** managed delegates to COM's connection point based events.
**
**/
namespace System.Runtime.InteropServices {
    //
    // #ComEventsFeature
    // 
    // code:#ComEventsFeature defines two public methods allowing to add/remove .NET delegates handling
    // events from COM objects. Those methods are defined as part of code:ComEventsHelper static class
    // * code:ComEventsHelper.Combine - will create/reuse-an-existing COM event sink and register the
    //     specified delegate to be raised when corresponding COM event is raised
    // * code:ComEventsHelper.Remove
    // 
    // 
    // To bind an event handler to the COM object you need to provide the following data:
    //  * rcw - the instance of the COM object you want to bind to
    //  * iid - Guid of the source interface you want the sink to implement
    //  * dispid - dispatch identifier of the event on the source interface you are interested in
    //  * d - delegate to invoked when corresponding COM event is raised.
    // 
    // #ComEventsArchitecture:
    // In COM world, events are handled by so-called event sinks. What these are? COM-based Object Models
    // (OMs) define "source" interfaces that need to be implemented by the COM clients to receive events. So,
    // event sinks are COM objects implementing a source interfaces. Once an event sink is passed to the COM
    // server (through a mechanism known as 'binding/advising to connection point'), COM server will be
    // calling source interface methods to "fire events" (advising, connection points, firing events etc. -
    // is all COM jargon).
    // 
    // There are few interesting obervations about source interfaces. Usually source interfaces are defined
    // as 'dispinterface' - meaning that only late-bound invocations on this interface are allowed. Even
    // though it is not illegal to use early bound invocations on source interfaces - the practice is
    // discouraged because of versioning concerns.
    // 
    // Notice also that each COM server object might define multiple source interfaces and hence have
    // multiple connection points (each CP handles exactly one source interface). COM objects that want to
    // fire events are required to implement IConnectionPointContainer interface which is used by the COM
    // clients to discovery connection poitns - objects implementing IConnectionPoint interface. Once
    // connection point is found - clients can bind to it using IConnectionPoint::Advise (see
    // code:ComEventsSink.Advise).
    // 
    // The idea behind code:#ComEventsFeature is to write a "universal event sink" COM component that is
    // generic enough to handle all late-bound event firings and invoke corresponding COM delegates (through
    // reflection).
    // 
    // When delegate is registered (using code:ComEventsHelper.Combine) we will verify we have corresponding
    // event sink created and bound.
    // 
    // But what happens when COM events are fired? code:ComEventsSink.Invoke implements IDispatch::Invoke method
    // and this is the entry point that is called. Once our event sink is invoked, we need to find the
    // corresponding delegate to invoke . We need to match the dispid of the call that is coming in to a
    // dispid of .NET delegate that has been registered for this object. Once this is found we do call the
    // delegates using reflection (code:ComEventsMethod.Invoke).
    // 
    // #ComEventsArgsMarshalling
    // Notice, that we may not have a delegate registered against every method on the source interface. If we
    // were to marshal all the input parameters for methods that do not reach user code - we would end up
    // generatic RCWs that are not reachable for user code (the inconvenience it might create is there will
    // be RCWs that users can not call Marshal.ReleaseComObject on to explicitly manage the lifetime of these
    // COM objects). The above behavior was one of the shortcoimings of legacy TLBIMP's implementation of COM
    // event sinking. In our code we will not marshal any data if there is no delegate registered to handle
    // the event. (code:ComEventsMethod.Invoke)
    // 
    // #ComEventsFinalization:
    // Additional area of interest is when COM sink should be unadvised from the connection point. Legacy
    // TLBIMP's implementation of COM event sinks will unadvises the sink when corresponding RCW is GCed.
    // This is achieved by rooting the event sinks in a finalizable object stored in RCW's property bag
    // (using Marshal.SetComObjectData). Hence, once RCW is no longer reachable - the finalizer is called and
    // it would unadvise all the event sinks. We are employing the same strategy here. See storing an
    // instance in the RCW at code:ComEventsInfo.FromObject and undadvsing the sinks at
    // code:ComEventsInfo.~ComEventsInfo
    // 
    // Classes of interest:
    // * code:ComEventsHelpers - defines public methods but there are also a number of internal classes that
    //     implement the actual COM event sink:
    // * code:ComEventsInfo - represents a finalizable container for all event sinks for a particular RCW.
    //     Lifetime of this instance corresponds to the lifetime of the RCW object
    // * code:ComEventsSink - represents a single event sink. Maintains an internal pointer to the next
    //     instance (in a singly linked list). A collection of code:ComEventsSink is stored at
    //     code:ComEventsInfo._sinks
    // * code:ComEventsMethod - represents a single method from the source interface which has .NET delegates
    //     attached to it. Maintains an internal pointer to the next instance (in a singly linked list). A
    //     collection of code:ComEventMethod is stored at code:ComEventsSink._methods
    //     
    // #ComEventsRetValIssue:
    // Issue: normally, COM events would not return any value. However, it may happen as described in
    // http://support.microsoft.com/kb/810228. Such design might represent a problem for us - e.g. what is
    // the return value of a chain of delegates - is it the value of the last call in the chain or the the
    // first one? As the above KB article indicates, in cases where OM has events returning values, it is
    // suggested that people implement their event sink by explicitly implementing the source interface. This
    // means that the problem is already quite complex and we should not be dealing with it - see
    // code:ComEventsMethod.Invoke

    using System;
    using System.Runtime.Remoting;

    /// <summary>
    /// The static methods provided in ComEventsHelper allow using .NET delegates to subscribe to events
    /// raised COM objects.
    /// </summary>
    public static class ComEventsHelper {

        /// <summary>
        /// Adds a delegate to the invocation list of events originating from the COM object.
        /// </summary>
        /// <param name="rcw">COM object firing the events the caller would like to respond to</param>
        /// <param name="iid">identifier of the source interface used by COM object to fire events</param>
        /// <param name="dispid">dispatch identifier of the method on the source interface</param>
        /// <param name="d">delegate to invoke when specifed COM event is fired</param>
        [System.Security.SecurityCritical]
        public static void Combine(object rcw, Guid iid, int dispid, System.Delegate d) {

            rcw = UnwrapIfTransparentProxy(rcw);

            lock (rcw) {
                ComEventsInfo eventsInfo = ComEventsInfo.FromObject(rcw);

                ComEventsSink sink = eventsInfo.FindSink(ref iid);
                if (sink == null) {
                    sink = eventsInfo.AddSink(ref iid);
                }


                ComEventsMethod method = sink.FindMethod(dispid);
                if (method == null) {
                    method = sink.AddMethod(dispid);
                }

                method.AddDelegate(d);
            }
        }

        /// <summary>
        /// Removes a delegate from the invocation list of events originating from the COM object.
        /// </summary>
        /// <param name="rcw">COM object the delegate is attached to</param>
        /// <param name="iid">identifier of the source interface used by COM object to fire events</param>
        /// <param name="dispid">dispatch identifier of the method on the source interface</param>
        /// <param name="d">delegate to remove from the invocation list</param>
        /// <returns></returns>
        [System.Security.SecurityCritical]
        public static Delegate Remove(object rcw, Guid iid, int dispid, System.Delegate d) {

            rcw = UnwrapIfTransparentProxy(rcw);

            lock (rcw) {

                ComEventsInfo eventsInfo = ComEventsInfo.Find(rcw);
                if (eventsInfo == null)
                    return null;
                ComEventsSink sink = eventsInfo.FindSink(ref iid);
                if (sink == null)
                    return null;
                ComEventsMethod method = sink.FindMethod(dispid);
                if (method == null)
                    return null;

                method.RemoveDelegate(d);

                if (method.Empty) {
                    // removed the last event handler for this dispid - need to remove dispid handler
                    method = sink.RemoveMethod(method);
                }
                if (method == null) {
                    // removed last dispid handler for this sink - need to remove the sink
                    sink = eventsInfo.RemoveSink(sink);
                }
                if (sink == null) {
                    // removed last sink for this rcw - need to remove all traces of event info
                    Marshal.SetComObjectData(rcw, typeof(ComEventsInfo), null);
                    GC.SuppressFinalize(eventsInfo);
                }

                return d;
            }
        }

        [System.Security.SecurityCritical]
        internal static object UnwrapIfTransparentProxy(object rcw) {
#if FEATURE_REMOTING
            if (RemotingServices.IsTransparentProxy(rcw)) {
                IntPtr punk = Marshal.GetIUnknownForObject(rcw);
                try {
                    rcw = Marshal.GetObjectForIUnknown(punk);
                } finally {
                    Marshal.Release(punk);
                }
            }
#endif
            return rcw;
        }
    }

}
