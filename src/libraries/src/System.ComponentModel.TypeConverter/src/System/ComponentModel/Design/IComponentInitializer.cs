// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///     IComponentInitializer can be implemented on an object that also implements IDesigner. 
    ///     This interface allows a newly created component to be given some stock default values,
    ///     such as a caption, default size, or other values.  Recommended default values for
    ///     the component's properties are passed in as a dictionary.
    /// </summary>
    public interface IComponentInitializer
    {
        /// <summary>
        ///     This method is called when an existing component is being re-initialized.  This may occur after
        ///     dragging a component to another container, for example.  The defaultValues
        ///     property contains a name/value dictionary of default values that should be applied
        ///     to properties. This dictionary may be null if no default values are specified.
        ///     You may use the defaultValues dictionary to apply recommended defaults to properties
        ///     but you should not modify component properties beyond what is stored in the
        ///     dictionary, because this is an existing component that may already have properties
        ///     set on it.
        /// </summary>
        void InitializeExistingComponent(IDictionary defaultValues);

        /// <summary>
        ///     This method is called when a component is first initialized, typically after being first added
        ///     to a design surface.  The defaultValues property contains a name/value dictionary of default
        ///     values that should be applied to properties.  This dictionary may be null if no default values
        ///     are specified.  You may perform any initialization of this component that you like, and you
        ///     may even ignore the defaultValues dictionary altogether if you wish.  
        /// </summary>
        void InitializeNewComponent(IDictionary defaultValues);
    }
}

