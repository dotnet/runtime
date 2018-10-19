// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Design
{
    /// <summary>
    /// Provides notifications of toolbox actions to designers which implement this interface.
    /// </summary>
    public interface IToolboxUser
    {
        /// <summary>
        /// Gets a value indicating whether the specified tool is supported by the current designer
        /// </summary>
        bool GetToolSupported(ToolboxItem tool);

        /// <summary>
        /// Selects the specified tool.
        /// </summary>
        /// <param name="tool">toolbox item</param>
        void ToolPicked(ToolboxItem tool);
    }
}

