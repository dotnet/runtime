// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /*****************************************************************************
    A GlobalSymbolContext represents the global symbol tables for a compilation. 
    This includes symbols, types, declarations.
    *****************************************************************************/

    internal class GlobalSymbolContext
    {
        private PredefinedTypes _predefTypes;
        private NameManager _nameManager;

        public GlobalSymbolContext(NameManager namemgr)
        {
            TypeManager = new TypeManager();
            GlobalSymbols = new BSYMMGR(namemgr, TypeManager);
            _predefTypes = new PredefinedTypes(GlobalSymbols);
            TypeManager.Init(GlobalSymbols, _predefTypes);
            GlobalSymbols.Init();

            _nameManager = namemgr;
        }

        public TypeManager TypeManager { get; private set; }
        public TypeManager GetTypes() { return TypeManager; }
        public BSYMMGR GlobalSymbols { get; private set; }
        public BSYMMGR GetGlobalSymbols() { return GlobalSymbols; }
        public NameManager GetNameManager() { return _nameManager; }
        public PredefinedTypes GetPredefTypes() { return _predefTypes; }

        public SymFactory GetGlobalSymbolFactory()
        {
            return GetGlobalSymbols().GetSymFactory();
        }

        public MiscSymFactory GetGlobalMiscSymFactory()
        {
            return GetGlobalSymbols().GetMiscSymFactory();
        }
    }
}
