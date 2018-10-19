// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

namespace System.Drawing.Design
{
    /// <summary>
    /// Provides access to the toolbox in the development environment.
    /// </summary>
    [ComImport(), Guid("4BACD258-DE64-4048-BC4E-FEDBEF9ACB76"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IToolboxService
    {
        /// <summary>
        /// Gets the names of all the tool categories currently on the toolbox.
        /// </summary>
        CategoryNameCollection CategoryNames { get; }

        /// <summary>
        /// Gets the name of the currently selected tool category from the toolbox.
        /// </summary>
        string SelectedCategory { get; set; }

        /// <summary>
        /// Adds a new toolbox item creator.
        /// </summary>
        void AddCreator(ToolboxItemCreatorCallback creator, string format);

        /// <summary>
        /// Adds a new toolbox item creator.
        /// </summary>
        void AddCreator(ToolboxItemCreatorCallback creator, string format, IDesignerHost host);

        /// <summary>
        /// Adds a new tool to the toolbox under the default category.
        /// </summary>
        void AddLinkedToolboxItem(ToolboxItem toolboxItem, IDesignerHost host);

        /// <summary>
        /// Adds a new tool to the toolbox under the specified category.
        /// </summary>
        void AddLinkedToolboxItem(ToolboxItem toolboxItem, string category, IDesignerHost host);

        /// <summary>
        /// Adds a new tool to the toolbox under the default category.
        /// </summary>
        void AddToolboxItem(ToolboxItem toolboxItem);

        /// <summary>
        /// Adds a new tool to the toolbox under the specified category.
        /// </summary>
        void AddToolboxItem(ToolboxItem toolboxItem, string category);

        /// <summary>
        /// Gets a toolbox item from a previously serialized object.
        /// </summary>
        ToolboxItem DeserializeToolboxItem(object serializedObject);

        /// <summary>
        /// Gets a toolbox item from a previously serialized object.
        /// </summary>
        ToolboxItem DeserializeToolboxItem(object serializedObject, IDesignerHost host);

        /// <summary>
        /// Get the currently selected tool.
        /// </summary>
        ToolboxItem GetSelectedToolboxItem();

        /// <summary>
        /// Get the currently selected tool.
        /// </summary>
        ToolboxItem GetSelectedToolboxItem(IDesignerHost host);

        /// <summary>
        /// Gets all .NET Framework tools on the toolbox.
        /// </summary>
        ToolboxItemCollection GetToolboxItems();

        /// <summary>
        /// Gets all .NET Framework tools on the toolbox.
        /// </summary>
        ToolboxItemCollection GetToolboxItems(IDesignerHost host);

        /// <summary>
        /// Gets all .NET Framework tools on the toolbox fopr specific category.
        /// </summary>
        ToolboxItemCollection GetToolboxItems(string category);

        /// <summary>
        /// Gets all .NET Framework tools on the toolbox for specific category.
        /// </summary>
        ToolboxItemCollection GetToolboxItems(string category, IDesignerHost host);

        /// <summary>
        /// Determines if the given designer host contains a designer that supports the serialized
        /// toolbox item.  This will return false if the designer doesn't support the item, or if the
        /// serializedObject parameter does not contain a toolbox item.
        /// </summary>
        bool IsSupported(object serializedObject, IDesignerHost host);

        /// <summary>
        /// Determines if the serialized toolbox item contains a matching collection of filter attributes.
        /// This will return false if the serializedObject parameter doesn't contain a toolbox item,
        /// or if the collection of filter attributes does not match.
        /// </summary>
        bool IsSupported(object serializedObject, ICollection filterAttributes);

        /// <summary>
        /// Gets a value indicating whether the specified object contains a serialized toolbox item.
        /// </summary>
        bool IsToolboxItem(object serializedObject);

        /// <summary>
        /// Gets a value indicating whether the specified object contains a serialized toolbox item.
        /// </summary>
        bool IsToolboxItem(object serializedObject, IDesignerHost host);

        /// <summary>
        /// Refreshes the state of the toolbox items.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Removes a previously added toolbox creator.
        /// </summary>
        void RemoveCreator(string format);

        /// <summary>
        /// Removes a previously added toolbox creator.
        /// </summary>
        void RemoveCreator(string format, IDesignerHost host);

        /// <summary>
        /// Removes the specified tool from the toolbox.
        /// </summary>
        void RemoveToolboxItem(ToolboxItem toolboxItem);

        /// <summary>
        /// Removes the specified tool from the toolbox.
        /// </summary>
        void RemoveToolboxItem(ToolboxItem toolboxItem, string category);

        /// <summary>
        /// Notifies the toolbox that the selected tool has been used.
        /// </summary>
        void SelectedToolboxItemUsed();

        /// <summary>
        /// Takes the given toolbox item and serializes it to a persistent object.  This object can then
        /// be stored in a stream or passed around in a drag and drop or clipboard operation.
        /// </summary>
        object SerializeToolboxItem(ToolboxItem toolboxItem);

        /// <summary>
        /// Sets the current application's cursor to a cursor that represents the 
        /// currently selected tool.
        /// </summary>
        /// <returns>bool</returns>
        bool SetCursor();

        /// <summary>
        /// Sets the currently selected tool in the toolbox.
        /// </summary>
        void SetSelectedToolboxItem(ToolboxItem toolboxItem);
    }
}

