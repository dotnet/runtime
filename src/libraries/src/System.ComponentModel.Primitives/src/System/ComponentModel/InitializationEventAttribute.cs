// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies which event is fired on initialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class InitializationEventAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.InitializationEventAttribute'/> class.
        /// </summary>
        public InitializationEventAttribute(string eventName)
        {
            EventName = eventName;
        }

        /// <summary>
        /// Gets the name of the initialization event.
        /// </summary>
        public string EventName { get; }
    }
}
