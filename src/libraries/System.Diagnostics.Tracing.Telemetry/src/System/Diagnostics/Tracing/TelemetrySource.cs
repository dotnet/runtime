// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// This is the basic API to 'hook' parts of the framework.   It is like an EventSource
    /// (which can also write object), but is intended to log complex objects that can't be serialized.
    /// </summary>
    public abstract class TelemetrySource
    {
        /// <summary>
        /// The NotificationSource that you should be writing to by default.  This is what
        /// most of the class library does.   Data written here goes to DefaultListener
        /// which dispatches to all its subscribers.  
        /// </summary>
        public static TelemetrySource DefaultSource { get { return TelemetryListener.DefaultListener; } }

        /// <summary>
        /// WriteData is a generic way of logging complex payloads.  Each notification
        /// is given a name, which identifies it as well as a object (typically an anonymous type)
        /// that gives the information to pass to the notification, which is arbitrary.  
        /// 
        /// The Telemetry name should be short (so don't use fully qualified names unless you have to
        /// to avoid ambiguity), but you want the name to be globally unique.  Typically your componentName.eventName
        /// where componentName and eventName are strings less than 10 characters are a good compromise.  
        /// evnetNames should NOT have '.' in them because component names have dots and for them both
        /// to have dots would lead to ambiguity.   The suggestion is to use _ instead.  It is assumed 
        /// that listeners will use string prefixing to filter groups, thus having hierarchy in component 
        /// names is good.  
        /// </summary>
        public abstract void WriteTelemetry(string telemetryName, object parameters);

        /// <summary>
        /// Optional: if there is expensive setup for the notification, you can call IsEnabled
        /// before doing this setup.   Consumers should not be assuming that they only get notifications
        /// for which IsEnabled is true however, it is optional for producers to call this API. 
        /// The telemetryName should be the same as what is passed to WriteTelemetry.   
        /// </summary>
        public abstract bool IsEnabled(string telemetryName);
    }
}
