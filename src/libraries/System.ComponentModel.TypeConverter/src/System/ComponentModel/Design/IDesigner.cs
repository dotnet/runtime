// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;
using System.ComponentModel;
using Microsoft.Win32;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para> Provides the basic framework for building a custom designer.
    ///       This interface stores the verbs available to the designer, as well as basic
    ///       services for the designer.</para>
    /// </summary>

    public interface IDesigner : IDisposable
    {
        /// <summary>
        ///    <para>Gets or sets the base component this designer is designing.</para>
        /// </summary>
        IComponent Component { get; }

        /// <summary>
        ///    <para> Gets or sets the design-time verbs supported by the designer.</para>
        /// </summary>
        DesignerVerbCollection Verbs { get; }

        /// <summary>
        ///    <para>
        ///       Performs the default action for this designer.
        ///    </para>
        /// </summary>
        void DoDefaultAction();

        /// <summary>
        ///    <para>
        ///       Initializes the designer with the given component.
        ///    </para>
        /// </summary>
        void Initialize(IComponent component);
    }
}

