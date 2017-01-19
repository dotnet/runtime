// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Errors
{
    ////////////////////////////////////////////////////////////////////////////////
    //
    // This is the "controller" for compiler objects. The controller is the object
    // that exposes/implements ICSCompiler for external consumption.  Compiler
    // options are configured through this object, and for an actual compilation,
    // this object instantiates a LangCompiler, feeds it the appropriate information,
    // tells it to compile, and then destroys it.

    internal abstract class CController
    {
        private readonly CErrorFactory _errorFactory;

        protected CController()
        {
            _errorFactory = new CErrorFactory();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // This function places a fully-constructed CError object into an error container
        // and sends it to the compiler host (this would be the place to batch these guys
        // up if we decide to.
        //
        // Note that if the error can't be put into a container (if, for example, we
        // can't create a container) the error is destroyed and the host is notified via
        // exception.

        public abstract void SubmitError(CError pError);

        public CErrorFactory GetErrorFactory()
        {
            return _errorFactory;
        }
    }
}
