//------------------------------------------------------------------------------
// <copyright file="UserPreferenceCategories.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace Microsoft.Win32 {
    using System.Diagnostics;
    using System;
    
    /// <devdoc>
    ///    <para> Identifies areas of user preferences that
    ///       have changed.</para>
    /// </devdoc>
    public enum UserPreferenceCategory {
    
        /// <devdoc>
        ///    <para> Specifies user
        ///       preferences associated with accessibility
        ///       of the system for users with disabilities.</para>
        /// </devdoc>
        Accessibility = 1,
    
        /// <devdoc>
        ///    <para> Specifies user preferences
        ///       associated with system colors, such as the
        ///       default color of windows or menus.</para>
        /// </devdoc>
        Color = 2,
    
        /// <devdoc>
        ///    <para> Specifies user
        ///       preferences associated with the system desktop.
        ///       This may reflect a change in desktop background
        ///       images, or desktop layout.</para>
        /// </devdoc>
        Desktop = 3,
        
        /// <devdoc>
        ///    <para> Specifies user preferences
        ///       that are not associated with any other category.</para>
        /// </devdoc>
        General = 4,
        
        /// <devdoc>
        ///    <para> Specifies
        ///       user preferences for icon settings. This includes
        ///       icon height and spacing.</para>
        /// </devdoc>
        Icon = 5,
    
        /// <devdoc>
        ///    <para> 
        ///       Specifies user preferences for keyboard settings,
        ///       such as the keyboard repeat rate.</para>
        /// </devdoc>
        Keyboard = 6,
        
        /// <devdoc>
        ///    <para> Specifies user preferences
        ///       for menu settings, such as menu delays and
        ///       text alignment.</para>
        /// </devdoc>
        Menu = 7,
    
        /// <devdoc>
        ///    <para> Specifies user preferences
        ///       for mouse settings, such as double click
        ///       time and mouse sensitivity.</para>
        /// </devdoc>
        Mouse = 8,
        
        /// <devdoc>
        ///    <para> Specifies user preferences
        ///       for policy settings, such as user rights and
        ///       access levels.</para>
        /// </devdoc>
        Policy = 9,
        
        /// <devdoc>
        ///    <para> Specifies user preferences
        ///       for system power settings. An example of a
        ///       power setting is the time required for the
        ///       system to automatically enter low power mode.</para>
        /// </devdoc>
        Power = 10,
    
        /// <devdoc>
        ///    <para> Specifies user preferences
        ///       associated with the screensaver.</para>
        /// </devdoc>
        Screensaver = 11,
    
        /// <devdoc>
        ///    <para> Specifies user preferences
        ///       associated with the dimensions and characteristics
        ///       of windows on the system.</para>
        /// </devdoc>
        Window = 12,
        
        /// <devdoc>
        ///    <para> Specifies user preferences
        ///       associated with the locale of the system.</para>
        /// </devdoc>
        Locale = 13,

        /// <devdoc>
        ///    <para> Specifies user preferences
        ///       associated with the visual style.</para>
        /// </devdoc>
        VisualStyle = 14,
    }
}

