// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// Represents a filter which allows only certain Unicode code points through.
    /// </summary>
    public interface ICodePointFilter
    {
        /// <summary>
        /// Gets an enumeration of all allowed code points.
        /// </summary>
        IEnumerable<int> GetAllowedCodePoints();
    }
}
