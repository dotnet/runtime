// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Specifies a set of technologies designer hosts should support.
    /// </summary>
    public enum ViewTechnology
    {
        /// <summary>
        /// Specifies that the view for a root designer is defined by some
        /// private interface contract between the designer and the
        /// development environment. This implies a tight coupling
        /// between the development environment and the designer, and should
        /// be avoided. This does allow older COM2 technologies to
        /// be shown in development environments that support
        /// COM2 interface technologies such as doc objects and ActiveX
        /// controls.
        /// </summary>
        [Obsolete("This value has been deprecated. Use ViewTechnology.Default instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        Passthrough = 0,

        /// <summary>
        /// Specifies that the view for a root designer is supplied through
        /// a Windows Forms control object. The designer host will fill the
        /// development environment's document window with this control.
        /// </summary>
        [Obsolete("This value has been deprecated. Use ViewTechnology.Default instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        WindowsForms = 1,

        /// <summary>
        /// Specifies the default view technology support. Here, the root designer may return
        /// any type of object it wishes, but it must be an object that can be "fitted" with
        /// an adapter to the technology of the host. Hosting environments such as Visual
        /// Studio will provide a way to plug in new view technology adapters. The default
        /// view object for the Windows Forms designer is a Control instance, while the
        /// default view object for the Avalon designer is an Element instance. 
        /// </summary>
        Default = 2
    }
}
