// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    /// IDesignerLoaderHost. This is an extension of IDesignerHost that is passed
    /// to the designer loader in the BeginLoad method. It is isolated from
    /// IDesignerHost to emphasize that all loading and reloading of the design
    /// document actually should be initiated by the designer loader, and not by
    /// the designer host. However, the loader must inform the designer host that
    /// it wishes to invoke a load or reload.
    /// </summary>
    public interface IDesignerLoaderHost : IDesignerHost
    {
        /// <summary>
        /// This is called by the designer loader to indicate that the load has 
        /// terminated. If there were errors, they should be passed in the errorCollection
        /// as a collection of exceptions (if they are not exceptions the designer
        /// loader host may just call ToString on them). If the load was successful then
        /// errorCollection should either be null or contain an empty collection.
        /// </summary>
        void EndLoad(string baseClassName, bool successful, ICollection errorCollection);

        /// <summary>
        /// This is called by the designer loader when it wishes to reload the
        /// design document. The reload will happen immediately so the caller
        /// should ensure that it is in a state where BeginLoad may be called again.
        /// </summary>
        void Reload();
    }

    /// <summary>
    /// IgnoreErrorsDuringReload - specifies whether errors should be ignored when Reload() is called.
    ///                   We only allow to set to true if we CanReloadWithErrors. If we cannot
    ///                   we simply ignore rather than throwing an exception. We probably should,
    ///                   but we are avoiding localization.
    /// CanReloadWithErrors - specifies whether it is possible to reload with errors. There are certain
    ///              scenarios where errors cannot be ignored.
    /// </summary>
    public interface IDesignerLoaderHost2 : IDesignerLoaderHost
    {
        bool IgnoreErrorsDuringReload { get; set; }
        bool CanReloadWithErrors { get; set; }
    }
}
