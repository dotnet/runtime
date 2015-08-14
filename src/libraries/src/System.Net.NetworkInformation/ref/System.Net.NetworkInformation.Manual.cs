// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.NetworkInformation
{
    public partial class NetworkInformationException : Exception
    {
        // Following property was added after removing the Win32Exception base class.
        public int ErrorCode { get { return default(int); } }
    }
}
