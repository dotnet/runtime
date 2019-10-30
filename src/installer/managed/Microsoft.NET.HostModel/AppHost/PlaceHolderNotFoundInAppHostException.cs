// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.NET.HostModel.AppHost
{
    /// <summary>
    /// Unable to use input file as a valid application host executable, as it does not contain 
    /// the expected placeholder byte sequence.
    /// </summary>
    public class PlaceHolderNotFoundInAppHostException : AppHostUpdateException
    {
        public byte[] MissingPattern { get; }
        public PlaceHolderNotFoundInAppHostException(byte[] pattern)
        {
            MissingPattern = pattern;
        }
    }
}

