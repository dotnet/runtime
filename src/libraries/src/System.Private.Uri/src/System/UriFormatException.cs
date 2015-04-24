// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System
{
    /// <summary>
    /// An exception class used when an invalid Uniform Resource Identifier is detected.
    /// </summary>
    public class UriFormatException : FormatException
    {
        public UriFormatException() : base()
        {
        }

        public UriFormatException(string textString) : base(textString)
        {
        }

        public UriFormatException(string textString, Exception e) : base(textString, e)
        {
        }
    }; // class UriFormatException
} // namespace System
