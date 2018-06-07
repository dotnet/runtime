// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Defines GUID specifiers that contain GUIDs which reference the standard set of tool windows that are available in
    /// the design environment.
    /// </summary>
    public class StandardToolWindows
    {
        /// <summary>
        /// Gets the GUID for the object browser.
        /// </summary>
        public static readonly Guid ObjectBrowser = new Guid("{970d9861-ee83-11d0-a778-00a0c91110c3}");

        /// <summary>
        /// Gets the GUID for the output window.
        /// </summary>
        public static readonly Guid OutputWindow = new Guid("{34e76e81-ee4a-11d0-ae2e-00a0c90fffc3}");

        /// <summary>
        /// Gets the GUID for the project explorer.
        /// </summary>
        public static readonly Guid ProjectExplorer = new Guid("{3ae79031-e1bc-11d0-8f78-00a0c9110057}");
        
        /// <summary>
        /// Gets the GUID for the properties window.
        /// </summary>
        public static readonly Guid PropertyBrowser = new Guid("{eefa5220-e298-11d0-8f78-00a0c9110057}");

        /// <summary>
        /// Gets the GUID for the related links frame.
        /// </summary>
        public static readonly Guid RelatedLinks = new Guid("{66dba47c-61df-11d2-aa79-00c04f990343}");

        /// <summary>
        /// Gets the GUID for the server explorer.
        /// </summary>
        public static readonly Guid ServerExplorer = new Guid("{74946827-37a0-11d2-a273-00c04f8ef4ff}");

        /// <summary>
        /// Gets the GUID for the task list.
        /// </summary>
        public static readonly Guid TaskList = new Guid("{4a9b7e51-aa16-11d0-a8c5-00a0c921a4d2}");

        /// <summary>
        /// Gets the GUID for the toolbox.
        /// </summary>
        public static readonly Guid Toolbox = new Guid("{b1e99781-ab81-11d0-b683-00aa00a3ee26}");
    }
}
