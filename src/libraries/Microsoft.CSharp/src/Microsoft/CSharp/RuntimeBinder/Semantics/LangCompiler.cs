// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class LangCompiler :
         CSemanticChecker,
         IErrorSink
    {
        private SymbolLoader _symbolLoader;
        private CController _pController;   // This is our parent "controller"
        private ErrorHandling _errorContext;
        private GlobalSymbolContext _globalSymbolContext;
        private UserStringBuilder _userStringBuilder;

        ////////////////////////////////////////////////////////////////////////////////
        // Construct a compiler. All the real work is done in the Init() routine. This 
        // primary initializes all the sub-components.

        public LangCompiler(CController pCtrl, NameManager pNameMgr)
        {
            Debug.Assert(pCtrl != null);

            _pController = pCtrl;
            _globalSymbolContext = new GlobalSymbolContext(pNameMgr);
            _userStringBuilder = new UserStringBuilder(_globalSymbolContext);
            _errorContext = new ErrorHandling(_userStringBuilder, this, pCtrl.GetErrorFactory());
            _symbolLoader = new SymbolLoader(_globalSymbolContext, null, _errorContext);
        }

        public new ErrorHandling GetErrorContext()
        {
            return _errorContext;
        }

        public override SymbolLoader SymbolLoader { get { return _symbolLoader; } }
        public override SymbolLoader GetSymbolLoader() { return _symbolLoader; }

        ////////////////////////////////////////////////////////////////////////////////
        // Searches the class [atsSearch] to see if it contains a method which is 
        // sufficient to implement [mwt]. Does not search base classes. [mwt] is 
        // typically a method in some interface.  We may be implementing this interface
        // at some particular type, e.g. IList<String>, and so the required signature is
        // the instantiation (i.e. substitution) of [mwt] for that instance. Similarly, 
        // the implementation may be provided by some base class that exists via
        // polymorphic inheritance, e.g. Foo : List<String>, and so we must instantiate
        // the parameters for each potential implementation. [atsSearch] may thus be an
        // instantiated type.
        //
        // If fOverride is true, this checks for a method with swtSlot set to the 
        // particular method.
        public void SubmitError(CParameterizedError error)
        {
            CError pError = GetErrorContext().RealizeError(error);

            if (pError == null)
            {
                return;
            }
            _pController.SubmitError(pError);
        }

        public int ErrorCount()
        {
            return 0;
        }
    }
}
