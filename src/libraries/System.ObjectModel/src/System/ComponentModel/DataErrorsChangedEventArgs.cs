// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.ComponentModel
{
    /// <devdoc>
    /// <para>Provides data for the <see langword='ErrorsChanged'/>
    /// event.</para>
    /// </devdoc>
    public class DataErrorsChangedEventArgs : EventArgs
    {
        private readonly string _propertyName;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.DataErrorsChangedEventArgs'/>
        /// class.</para>
        /// </devdoc>
        public DataErrorsChangedEventArgs(string propertyName)
        {
            _propertyName = propertyName;
        }

        /// <devdoc>
        ///    <para>Indicates the name of the property whose errors changed.</para>
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
