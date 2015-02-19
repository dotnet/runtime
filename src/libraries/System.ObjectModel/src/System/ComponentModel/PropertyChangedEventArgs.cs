// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.ComponentModel
{
    /// <devdoc>
    /// <para>Provides data for the <see langword='PropertyChanged'/>
    /// event.</para>
    /// </devdoc>
    public class PropertyChangedEventArgs : EventArgs
    {
        private readonly string _propertyName;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.PropertyChangedEventArgs'/>
        /// class.</para>
        /// </devdoc>
        public PropertyChangedEventArgs(string propertyName)
        {
            _propertyName = propertyName;
        }

        /// <devdoc>
        ///    <para>Indicates the name of the property that changed.</para>
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
