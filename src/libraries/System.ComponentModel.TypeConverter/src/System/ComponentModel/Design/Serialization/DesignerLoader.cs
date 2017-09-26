// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Security.Permissions;

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    ///     DesignerLoader.  This class is responsible for loading a designer document.  
    ///     Where and how this load occurs is a private matter for the designer loader.
    ///     The designer loader will be handed to an IDesignerHost instance.  This instance, 
    ///     when it is ready to load the document, will call BeginLoad, passing an instance
    ///     of IDesignerLoaderHost.  The designer loader will load up the design surface
    ///     using the host interface, and call EndLoad on the interface when it is done.
    ///     The error collection passed into EndLoad should be empty or null to indicate a
    ///     successful load, or it should contain a collection of exceptions that 
    ///     describe the error.
    ///
    ///     Once a document is loaded, the designer loader is also responsible for
    ///     writing any changes made to the document back whatever storage the
    ///     loader used when loading the document.  
    /// </summary>
    public abstract class DesignerLoader
    {
        /// <summary>
        ///     Returns true when the designer is in the process of loading.  Clients that are
        ///     sinking notifications from the designer often want to ignore them while the designer is loading
        ///     and only respond to them if they result from user interactions.
        /// </summary>
        public virtual bool Loading => false;

        /// <summary>
        ///     Called by the designer host to begin the loading process.  The designer
        ///     host passes in an instance of a designer loader host (which is typically
        ///     the same object as the designer host.  This loader host allows
        ///     the designer loader to reload the design document and also allows
        ///     the designer loader to indicate that it has finished loading the
        ///     design document.
        /// </summary>
        public abstract void BeginLoad(IDesignerLoaderHost host);

        /// <summary>
        ///     Disposes this designer loader.  The designer host will call this method
        ///     when the design document itself is being destroyed.  Once called, the
        ///     designer loader will never be called again.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        ///     The designer host will call this periodically when it wants to
        ///     ensure that any changes that have been made to the document
        ///     have been saved by the designer loader.  This method allows
        ///     designer loaders to implement a lazy-write scheme to improve
        ///     performance.  The default implementation does nothing.
        /// </summary>
        public virtual void Flush() { }
    }
}

