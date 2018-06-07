// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// An InstanceCreationEditor allows the user to create an instance of a particular type of property from a dropdown
    /// Within the PropertyGrid. Usually, the text specified by InstanceCreationEditor.Text will be displayed on the 
    /// dropdown from the PropertyGrid as a link or button. When clicked, the InstanceCreationEditor.CreateInstance
    /// method will be called with the Type of the object to create.
    /// </summary>
    public abstract class InstanceCreationEditor
    {
        public virtual string Text => SR.InstanceCreationEditorDefaultText;

        /// <summary>
        /// This method is invoked when you user chooses the link displayed by the PropertyGrid for the InstanceCreationEditor.
        /// The object returned from this method must be an instance of the specified type, or null in which case the editor will do nothing.
        ///
        /// </summary>
        public abstract object CreateInstance(ITypeDescriptorContext context, Type instanceType);
    }
}
