// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CSharp.RuntimeBinder.Errors;

namespace Microsoft.CSharp.RuntimeBinder
{
    /////////////////////////////////////////////////////////////////////////////////
    // This class merely wraps a controller and throws a runtime binder exception
    // whenever we get an error during binding.

    internal class RuntimeBinderController : CController
    {
        public override void SubmitError(CError pError)
        {
            throw new RuntimeBinderException(pError.Text);
        }
    }
}
