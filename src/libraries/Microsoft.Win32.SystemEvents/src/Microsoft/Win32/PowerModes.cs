//------------------------------------------------------------------------------
// <copyright file="PowerModes.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace Microsoft.Win32 {
    using System.Diagnostics;
    using System;

    /// <devdoc>
    ///    <para> Specifies how the system
    ///       power mode changes.</para>
    /// </devdoc>
    public enum PowerModes {
    
        /// <devdoc>
        ///    <para> The system is about to resume.</para>
        /// </devdoc>
        Resume = 1,
        
        /// <devdoc>
        ///      The power mode status has changed.  This may
        ///      indicate a weak or charging battery, a transition
        ///      from AC power from battery, or other change in the
        ///      status of the system power supply.
        /// </devdoc>
        StatusChange = 2,
        
        /// <devdoc>
        ///      The system is about to be suspended.
        /// </devdoc>
        Suspend = 3,
    
    }
}

