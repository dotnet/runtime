// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Net
{
    /// <devdoc>
    ///    <para>
    ///       The <see cref='System.Net.IWebRequestCreate'/> interface is used by the <see cref='System.Net.WebRequest'/>
    ///       class to create <see cref='System.Net.WebRequest'/>
    ///       instances for a registered scheme.
    ///    </para>
    /// </devdoc>
    public interface IWebRequestCreate
    {
        /// <devdoc>
        ///    <para>
        ///       Creates a <see cref='System.Net.WebRequest'/>
        ///       instance.
        ///    </para>
        /// </devdoc>
        WebRequest Create(Uri uri);
    }
}
