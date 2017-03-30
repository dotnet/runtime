// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CSharp.RuntimeBinder.Errors;

namespace Microsoft.CSharp.RuntimeBinder
{
    /////////////////////////////////////////////////////////////////////////////////
    // This class merely wraps a controller and throws a runtime binder exception
    // whenever we get an error during binding.

    internal sealed class RuntimeBinderController : CController
    {
        public override void SubmitError(CError pError)
        {
            throw new RuntimeBinderException(pError.Text);
        }
    }
}
