// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies which event is fired on initialization.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class InitializationEventAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.InitializationEventAttribute'/> class.
        ///    </para>
        /// </summary>
        public InitializationEventAttribute(string eventName)
        {
            EventName = eventName;
        }

        /// <summary>
        ///    <para>
        ///       Gets the name of the initialization event.
        ///    </para>
        /// </summary>
        public string EventName { get; }
    }
}
