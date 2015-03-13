// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Errors
{
    internal class CErrorFactory
    {
        public CError CreateError(ErrorCode iErrorIndex, params string[] args)
        {
            CError output = new CError();
            output.Initialize(iErrorIndex, args);
            return output;
        }
    }
}
