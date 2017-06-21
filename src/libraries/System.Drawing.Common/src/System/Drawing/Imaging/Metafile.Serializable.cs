// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    using System.Runtime.Serialization;

    [Serializable]
    public sealed partial class Metafile : Image
    {
        /// <summary>
        /// Constructor used in deserialization
        /// </summary>
        private Metafile(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
