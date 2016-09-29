// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;
using Microsoft.Win32;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// <para>Provides data for the <see cref='System.ComponentModel.Design.IDesignerEventService.ActiveDesigner'/>
    /// event.</para>
    /// </summary>
    public class ActiveDesignerEventArgs : EventArgs
    {
        /// <summary>
        ///     The document that is losing activation.
        /// </summary>
        private readonly IDesignerHost _oldDesigner;

        /// <summary>
        ///     The document that is gaining activation.
        /// </summary>
        private readonly IDesignerHost _newDesigner;

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.Design.ActiveDesignerEventArgs'/>
        /// class.</para>
        /// </summary>
        public ActiveDesignerEventArgs(IDesignerHost oldDesigner, IDesignerHost newDesigner)
        {
            _oldDesigner = oldDesigner;
            _newDesigner = newDesigner;
        }

        /// <summary>
        ///    <para>
        ///       Gets or
        ///       sets the document that is losing activation.
        ///    </para>
        /// </summary>
        public IDesignerHost OldDesigner
        {
            get
            {
                return _oldDesigner;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets or
        ///       sets the document that is gaining activation.
        ///    </para>
        /// </summary>
        public IDesignerHost NewDesigner
        {
            get
            {
                return _newDesigner;
            }
        }
    }
}
