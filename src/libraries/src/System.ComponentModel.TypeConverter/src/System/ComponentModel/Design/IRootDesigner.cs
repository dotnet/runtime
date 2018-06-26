// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Defines the root designer. A root designer is the designer that sits
    /// at the top, or root, of the object hierarchy. The root designer's job
    /// is to provide the design-time user interface for the design surface.
    /// It does this through the View property.
    /// </summary>
    public interface IRootDesigner : IDesigner
    {
        /// <summary>
        /// The list of technologies that this designer can support
        /// for its view. Examples of different technologies are
        /// Windows Forms and Web Forms. Other object models can be
        /// supported at design time, but they most be able to
        /// provide a view in one of the supported technologies.
        /// </summary>
        ViewTechnology[] SupportedTechnologies { get; }

        /// <summary>
        /// The user interface to present to the user. The returning
        /// data type is an object because there can be a variety
        /// of different user interface technologies. Development
        /// environments typically support more than one technology.
        /// </summary>
        object GetView(ViewTechnology technology);
    }
}
