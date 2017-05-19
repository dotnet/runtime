//------------------------------------------------------------------------------
// <copyright file="IToolboxUser.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Drawing.Design {

    using System.Diagnostics;

    /// <include file='doc\IToolboxUser.uex' path='docs/doc[@for="IToolboxUser"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides notifications of toolbox actions
    ///       to designers which implement this interface.
    ///    </para>
    /// </devdoc>
    public interface IToolboxUser {
        /// <include file='doc\IToolboxUser.uex' path='docs/doc[@for="IToolboxUser.GetToolSupported"]/*' />
        /// <devdoc>
        ///    <para>Gets a value indicating whether the specified tool is supported by the current
        ///       designer.</para>
        /// </devdoc>

 
        //
        bool GetToolSupported(ToolboxItem tool);

        /// <include file='doc\IToolboxUser.uex' path='docs/doc[@for="IToolboxUser.ToolPicked"]/*' />
        /// <devdoc>
        ///    <para>Selects the specified tool.</para>
        /// </devdoc>
        void ToolPicked(ToolboxItem tool);
    }
}

