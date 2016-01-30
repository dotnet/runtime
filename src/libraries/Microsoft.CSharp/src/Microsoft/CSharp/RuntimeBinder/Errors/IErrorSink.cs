// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Errors
{
    // This interface is used to decouple the error reporting
    // implementation from the error detection source. 
    internal interface IErrorSink
    {
        void SubmitError(CParameterizedError error);
        int ErrorCount();
    }
}
