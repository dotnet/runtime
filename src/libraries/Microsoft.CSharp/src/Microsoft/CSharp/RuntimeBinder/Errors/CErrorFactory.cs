// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
