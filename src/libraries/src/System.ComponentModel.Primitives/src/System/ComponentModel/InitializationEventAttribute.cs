// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies which event is fired on initialization.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class InitializationEventAttribute : Attribute
    {
        private string _eventName = null;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.InitializationEventAttribute'/> class.
        ///    </para>
        /// </devdoc>
        public InitializationEventAttribute(string eventName)
        {
            _eventName = eventName;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the name of the initialization event.
        ///    </para>
        /// </devdoc>
        public string EventName
        {
            get
            {
                return _eventName;
            }
        }
    }
}
