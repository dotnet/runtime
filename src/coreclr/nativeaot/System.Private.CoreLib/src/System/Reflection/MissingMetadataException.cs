// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Reflection
{
    public sealed class MissingMetadataException : TypeAccessException
    {
        public MissingMetadataException()
        {
        }

        public MissingMetadataException(string message)
            : base(message)
        {
        }
    }
}
