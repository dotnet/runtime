// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /*****************************************************************************
    A GlobalSymbolContext represents the global symbol tables for a compilation. 
    This includes symbols, types, declarations.
    *****************************************************************************/

    internal sealed class GlobalSymbolContext
    {
        private readonly PredefinedTypes _predefTypes;
        private readonly NameManager _nameManager;

        public GlobalSymbolContext(NameManager namemgr)
        {
            TypeManager = new TypeManager();
            GlobalSymbols = new BSYMMGR(namemgr, TypeManager);
            _predefTypes = new PredefinedTypes(GlobalSymbols);
            TypeManager.Init(GlobalSymbols, _predefTypes);
            GlobalSymbols.Init();

            _nameManager = namemgr;
        }

        public TypeManager TypeManager { get; }
        public TypeManager GetTypes() { return TypeManager; }
        private BSYMMGR GlobalSymbols { get; }
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
