// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Win32 {
    using System;
    using System.Diagnostics;
    using System.Security.Permissions;
    
    /// <devdoc>
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.UserPreferenceChanging'/> event.</para>
    /// </devdoc>
    public class UserPreferenceChangingEventArgs : EventArgs {
    
        private readonly UserPreferenceCategory category;
    
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='Microsoft.Win32.UserPreferenceChangingEventArgs'/> class.</para>
        /// </devdoc>
        public UserPreferenceChangingEventArgs(UserPreferenceCategory category) {
            this.category = category;
        }
    
        /// <devdoc>
        ///    <para>Gets the category of user preferences that has Changing.</para>
        /// </devdoc>
        public UserPreferenceCategory Category {
            get {
                return category;
            }
        }
    }
}

