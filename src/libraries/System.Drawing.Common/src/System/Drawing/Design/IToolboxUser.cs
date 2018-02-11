// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

