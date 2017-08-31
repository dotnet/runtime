// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Diagnostics
{
    /// <summary>
    /// This is the basic API to 'hook' parts of the framework.   It is like an EventSource
    /// (which can also write object), but is intended to log complex objects that can't be serialized.
    /// 
    /// Please See the DiagnosticSource Users Guide 
    /// https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md
    /// for instructions on its use.  
    /// </summary>
    public abstract partial class DiagnosticSource
    { 
        /// <summary>
        /// Write is a generic way of logging complex payloads.  Each notification
        /// is given a name, which identifies it as well as a object (typically an anonymous type)
        /// that gives the information to pass to the notification, which is arbitrary.  
        /// 
        /// The name should be short (so don't use fully qualified names unless you have to
        /// to avoid ambiguity), but you want the name to be globally unique.  Typically your componentName.eventName
        /// where componentName and eventName are strings less than 10 characters are a good compromise.  
        /// notification names should NOT have '.' in them because component names have dots and for them both
        /// to have dots would lead to ambiguity.   The suggestion is to use _ instead.  It is assumed 
        /// that listeners will use string prefixing to filter groups, thus having hierarchy in component 
        /// names is good.  
        /// </summary>
        /// <param name="name">The name of the event being written.</param>
        /// <param name="value">An object that represent the value being passed as a payload for the event.
        /// This is often an anonymous type which contains several sub-values.</param>
        public abstract void Write(string name, object value);

        /// <summary>
        /// Optional: if there is expensive setup for the notification, you can call IsEnabled
        /// before doing this setup.   Consumers should not be assuming that they only get notifications
        /// for which IsEnabled is true however, it is optional for producers to call this API. 
        /// The name should be the same as what is passed to Write.   
        /// </summary>
        /// <param name="name">The name of the event being written.</param>
        public abstract bool IsEnabled(string name);

        /// <summary>
        /// Optional: if there is expensive setup for the notification, you can call IsEnabled
        /// before doing this setup with context 
        /// </summary>
        /// <param name="name">The name of the event being written.</param>
        /// <param name="arg1">An object that represents the additional context for IsEnabled.
        /// Consumers should expect to receive null which may indicate that producer called pure 
        /// IsEnabled(string)  to check if consumer wants to get notifications for such events at all. 
        /// Based on it, producer may call IsEnabled(string, object, object) again with non-null context </param>
        /// <param name="arg2">Optional. An object that represents the additional context for IsEnabled. 
        /// Null by default. Consumers should expect to receive null which may indicate that producer 
        /// called pure IsEnabled(string) or producer passed all necessary context in arg1</param>
        /// <seealso cref="IsEnabled(string)"/>
        public virtual bool IsEnabled(string name, object arg1, object arg2 = null)
        {
            return IsEnabled(name);
        }

    }
}
