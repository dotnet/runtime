// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Represents the method that will handle the
    ///    <see langword='PropertyChanging'/> event raised when a
    ///       property is changing on a component.</para>
    /// </devdoc>
    public delegate void PropertyChangingEventHandler(object sender, PropertyChangingEventArgs e);
}
