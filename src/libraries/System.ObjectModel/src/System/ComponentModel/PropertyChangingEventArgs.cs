// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.ComponentModel
{
    /// <devdoc>
    /// <para>Provides data for the <see langword='PropertyChanging'/>
    /// event.</para>
    /// </devdoc>
    public class PropertyChangingEventArgs : EventArgs
    {
        private readonly string _propertyName;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.PropertyChangingEventArgs'/>
        /// class.</para>
        /// </devdoc>
        public PropertyChangingEventArgs(string propertyName)
        {
            _propertyName = propertyName;
        }

        /// <devdoc>
        ///    <para>Indicates the name of the property that is changing.</para>
        /// </devdoc>
        public virtual string PropertyName
        {
            get
            {
                return _propertyName;
            }
        }
    }
}
