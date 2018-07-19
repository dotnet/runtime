// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace System.Drawing.Design
{
    /// <summary>
    /// Provides a base class for editors that may provide users with a user interface to visually edit the values of the supported type or types.
    /// </summary>
    public class UITypeEditor
    {
        static UITypeEditor()
        {
            Hashtable intrinsicEditors = new Hashtable
            {
                // Our set of intrinsic editors.
                [typeof(DateTime)] = "System.ComponentModel.Design.DateTimeEditor, " + AssemblyRef.SystemDesign,
                [typeof(Array)] = "System.ComponentModel.Design.ArrayEditor, " + AssemblyRef.SystemDesign,
                [typeof(IList)] = "System.ComponentModel.Design.CollectionEditor, " + AssemblyRef.SystemDesign,
                [typeof(ICollection)] = "System.ComponentModel.Design.CollectionEditor, " + AssemblyRef.SystemDesign,
                [typeof(byte[])] = "System.ComponentModel.Design.BinaryEditor, " + AssemblyRef.SystemDesign,
                [typeof(Stream)] = "System.ComponentModel.Design.BinaryEditor, " + AssemblyRef.SystemDesign,
                [typeof(string[])] = "System.Windows.Forms.Design.StringArrayEditor, " + AssemblyRef.SystemDesign,
                [typeof(Collection<string>)] = "System.Windows.Forms.Design.StringCollectionEditor, " + AssemblyRef.SystemDesign
            };

            // Add our intrinsic editors to TypeDescriptor.
            TypeDescriptor.AddEditorTable(typeof(UITypeEditor), intrinsicEditors);
        }
        
        /// <summary>
        /// Determines if drop-down editors should be resizable by the user.
        /// </summary>
        public virtual bool IsDropDownResizable => false;

        /// <summary>
        /// Edits the specified value using the editor style provided by <see cref='System.Drawing.Design.UITypeEditor.GetEditStyle'/>.
        /// </summary>
        /// <param name="provider">An <see cref="System.IServiceProvider" /> that this editor can use to obtain services.</param>
        /// <param name="value">The object to edit.</param>
        public object EditValue(IServiceProvider provider, object value) => EditValue(null, provider, value);

        /// <summary>
        /// Edits the specified value using the editor style provided by <see cref='System.Drawing.Design.UITypeEditor.GetEditStyle'/>.
        /// </summary>
        /// <param name="context">The <see cref="System.ComponentModel.ITypeDescriptorContext" /> that can be used to gain additional context information.</param>
        /// <param name="provider">The <see cref="System.IServiceProvider" /> that this editor can use to obtain services.</param>
        /// <param name="value">The object to edit.</param>
        public virtual object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) => value;
        
        /// <summary>
        /// Gets the <see cref='System.Drawing.Design.UITypeEditorEditStyle'/> of the Edit method.
        /// </summary>
        public UITypeEditorEditStyle GetEditStyle() => GetEditStyle(null);
        
        /// <summary>
        /// Gets a value indicating whether this editor supports painting a representation of an object's value.
        /// </summary>
        public bool GetPaintValueSupported() => GetPaintValueSupported(null);

        /// <summary>
        /// Gets a value indicating whether this editor supports painting a representation of an object's value.
        /// </summary>
        /// <param name="context">The <see cref="System.ComponentModel.ITypeDescriptorContext" /> that can be used to gain additional context information. </param>
        public virtual bool GetPaintValueSupported(ITypeDescriptorContext context) => false;

        /// <summary>
        /// Gets the editing style of the Edit method.
        /// </summary>
        /// <param name="context">The <see cref="System.ComponentModel.ITypeDescriptorContext" /> that can be used to gain additional context information. </param>
        public virtual UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.None;

        /// <summary>
        /// Paints a representative value of the specified object to the specified canvas.
        /// </summary>
        /// <param name="value">The object whose value this type editor will display. </param>
        /// <param name="canvas">A drawing canvas on which to paint the representation of the object's value. </param>
        /// <param name="rectangle">A <see cref="System.Drawing.Rectangle" /> within whose boundaries to paint the value. </param>
        public void PaintValue(object value, Graphics canvas, Rectangle rectangle) => PaintValue(new PaintValueEventArgs(null, value, canvas, rectangle));

        /// <summary>
        /// Paints a representative value of the specified object to the specified canvas.
        /// </summary>
        /// <param name="e">A <see cref="System.Drawing.Design.PaintValueEventArgs" /> that indicates what to paint and where to paint it. </param>
        [SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        public virtual void PaintValue(PaintValueEventArgs e) { }
    }
}
