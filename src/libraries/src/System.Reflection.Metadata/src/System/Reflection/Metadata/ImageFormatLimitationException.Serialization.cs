// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;

namespace System.Reflection.Metadata
{
    [Serializable]
    public partial class ImageFormatLimitationException : Exception
    {
        protected ImageFormatLimitationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
