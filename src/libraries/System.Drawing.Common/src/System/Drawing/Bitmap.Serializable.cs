// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Runtime.Serialization;

    [Serializable]
    public sealed partial class Bitmap : Image
    {
        /// <summary>
        /// Constructor used in deserialization
        /// </summary>
        private Bitmap(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
