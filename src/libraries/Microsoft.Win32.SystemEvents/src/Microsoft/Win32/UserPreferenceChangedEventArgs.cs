// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Win32
{
    /// <devdoc>
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.UserPreferenceChanged'/> event.</para>
    /// </devdoc>
    public class UserPreferenceChangedEventArgs : EventArgs
    {
        private readonly UserPreferenceCategory _category;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='Microsoft.Win32.UserPreferenceChangedEventArgs'/> class.</para>
        /// </devdoc>
        public UserPreferenceChangedEventArgs(UserPreferenceCategory category)
        {
            _category = category;
        }

        /// <devdoc>
        ///    <para>Gets the category of user preferences that has changed.</para>
        /// </devdoc>
        public UserPreferenceCategory Category
        {
            get
            {
                return _category;
            }
        }
    }
}

