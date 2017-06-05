//------------------------------------------------------------------------------
// <copyright file="IToolboxService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Drawing.Design {

    using System;
    using System.Collections;
    using System.ComponentModel.Design;
    using System.Runtime.InteropServices;

    /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService"]/*' />
    /// <devdoc>
    ///    <para> 
    ///       Provides access to the toolbox in the development environment.</para>
    /// </devdoc>
    [ComImport(), Guid("4BACD258-DE64-4048-BC4E-FEDBEF9ACB76"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IToolboxService {
    
        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.CategoryNames"]/*' />
        /// <devdoc>
        ///    <para>Gets the names of all the tool categories currently on the toolbox.</para>
        /// </devdoc>
        CategoryNameCollection CategoryNames { get; }

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.SelectedCategory"]/*' />
        /// <devdoc>
        ///    <para>Gets the name of the currently selected tool category from the toolbox.</para>
        /// </devdoc>
        string SelectedCategory { get; set; }

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.AddCreator"]/*' />
        /// <devdoc>
        ///    <para>Adds a new toolbox item creator.</para>
        /// </devdoc>
        void AddCreator(ToolboxItemCreatorCallback creator, string format);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.AddCreator1"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Adds a new toolbox
        ///       item creator.</para>
        /// </devdoc>
        void AddCreator(ToolboxItemCreatorCallback creator, string format, IDesignerHost host);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.AddLinkedToolboxItem"]/*' />
        /// <devdoc>
        ///    <para>Adds a new tool to the toolbox under the default category.</para>
        /// </devdoc>
        void AddLinkedToolboxItem(ToolboxItem toolboxItem, IDesignerHost host);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.AddLinkedToolboxItem1"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Adds a
        ///       new tool to the toolbox under the specified category.</para>
        /// </devdoc>
        void AddLinkedToolboxItem(ToolboxItem toolboxItem, string category, IDesignerHost host);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.AddToolboxItem"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Adds a new tool
        ///       to the toolbox under the default category.</para>
        /// </devdoc>
        void AddToolboxItem(ToolboxItem toolboxItem);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.AddToolboxItem1"]/*' />
        /// <devdoc>
        ///    <para>Adds a new tool to the toolbox under the specified category.</para>
        /// </devdoc>
        void AddToolboxItem(ToolboxItem toolboxItem, string category);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.DeserializeToolboxItem"]/*' />
        /// <devdoc>
        ///    <para>Gets a toolbox item from a previously serialized object.</para>
        /// </devdoc>
        ToolboxItem DeserializeToolboxItem(object serializedObject);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.DeserializeToolboxItem1"]/*' />
        /// <devdoc>
        ///    <para>Gets a toolbox item from a previously serialized object.</para>
        /// </devdoc>
        ToolboxItem DeserializeToolboxItem(object serializedObject, IDesignerHost host);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.GetSelectedToolboxItem"]/*' />
        /// <devdoc>
        ///    <para>Gets the currently selected tool.</para>
        /// </devdoc>
        ToolboxItem GetSelectedToolboxItem();

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.GetSelectedToolboxItem1"]/*' />
        /// <devdoc>
        ///    <para>Gets the currently selected tool.</para>
        /// </devdoc>
        ToolboxItem GetSelectedToolboxItem(IDesignerHost host);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.GetToolboxItems"]/*' />
        /// <devdoc>
        ///    <para>Gets all .NET Framework tools on the toolbox.</para>
        /// </devdoc>
        ToolboxItemCollection GetToolboxItems();

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.GetToolboxItems1"]/*' />
        /// <devdoc>
        ///    <para>Gets all .NET Framework tools on the toolbox.</para>
        /// </devdoc>
        ToolboxItemCollection GetToolboxItems(IDesignerHost host);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.GetToolboxItems2"]/*' />
        /// <devdoc>
        ///    <para>Gets all .NET Framework tools on the specified toolbox category.</para>
        /// </devdoc>
        ToolboxItemCollection GetToolboxItems(String category);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.GetToolboxItems3"]/*' />
        /// <devdoc>
        ///    <para>Gets all .NET Framework tools on the specified toolbox category.</para>
        /// </devdoc>
        ToolboxItemCollection GetToolboxItems(String category, IDesignerHost host);
        
        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.IsSupported"]/*' />
        /// <devdoc>
        ///     Determines if the given designer host contains a designer that supports the serialized
        ///     toolbox item.  This will return false if the designer doesn't support the item, or if the
        ///     serializedObject parameter does not contain a toolbox item.
        /// </devdoc>
        bool IsSupported(object serializedObject, IDesignerHost host);
        
        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.IsSupported1"]/*' />
        /// <devdoc>
        ///     Determines if the serialized toolbox item contains a matching collection of filter attributes.
        ///     This will return false if the serializedObject parameter doesn't contain a toolbox item,
        ///     or if the collection of filter attributes does not match.
        /// </devdoc>
        bool IsSupported(object serializedObject, ICollection filterAttributes);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.IsToolboxItem"]/*' />
        /// <devdoc>
        ///    <para>Gets a value indicating whether the specified object contains a serialized toolbox item.</para>
        /// </devdoc>
        bool IsToolboxItem(object serializedObject);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.IsToolboxItem1"]/*' />
        /// <devdoc>
        ///    <para>Gets a value indicating whether the specified object contains a serialized toolbox item.</para>
        /// </devdoc>
        bool IsToolboxItem(object serializedObject, IDesignerHost host);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.Refresh"]/*' />
        /// <devdoc>
        ///    <para> Refreshes the state of the toolbox items.</para>
        /// </devdoc>
        void Refresh();

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.RemoveCreator"]/*' />
        /// <devdoc>
        ///    <para>Removes a previously added toolbox creator.</para>
        /// </devdoc>
        void RemoveCreator(string format);
        
        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.RemoveCreator1"]/*' />
        /// <devdoc>
        ///      Removes a previously added toolbox creator.
        /// </devdoc>
        void RemoveCreator(string format, IDesignerHost host);
        
        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.RemoveToolboxItem"]/*' />
        /// <devdoc>
        ///    <para>Removes the specified tool from the toolbox.</para>
        /// </devdoc>
        void RemoveToolboxItem(ToolboxItem toolboxItem);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.RemoveToolboxItem1"]/*' />
        /// <devdoc>
        ///    <para>Removes the specified tool from the toolbox.</para>
        /// </devdoc>
        void RemoveToolboxItem(ToolboxItem toolboxItem, string category);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.SelectedToolboxItemUsed"]/*' />
        /// <devdoc>
        ///    <para>Notifies the toolbox that the selected tool has been used.</para>
        /// </devdoc>
        void SelectedToolboxItemUsed();

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.SerializeToolboxItem"]/*' />
        /// <devdoc>
        ///     Takes the given toolbox item and serializes it to a persistent object.  This object can then
        ///     be stored in a stream or passed around in a drag and drop or clipboard operation.
        /// </devdoc>
        object SerializeToolboxItem(ToolboxItem toolboxItem);

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.SetCursor"]/*' />
        /// <devdoc>
        ///    <para>Sets the current application's cursor to a cursor that represents the 
        ///       currently selected tool.</para>
        /// </devdoc>
        bool SetCursor();

        /// <include file='doc\IToolboxService.uex' path='docs/doc[@for="IToolboxService.SetSelectedToolboxItem"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Sets the currently selected tool in the toolbox.</para>
        /// </devdoc>
        void SetSelectedToolboxItem(ToolboxItem toolboxItem);
    }
}

