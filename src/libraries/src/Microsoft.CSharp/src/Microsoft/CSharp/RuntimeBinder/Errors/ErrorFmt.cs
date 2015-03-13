// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Semantics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Errors
{
    // Things related to the construction of a formatted error. Reporting
    // an error involves constructing a formatted message, and then passing
    // that message on to an object that gets it to the user. The interface
    // that declares the error submission API is separate from this.

    internal enum ErrArgKind
    {
        Int,
        Hresult,
        Ids,
        SymKind,
        Sym,
        Type,
        Name,
        Str,
        PredefName,
        LocNode,
        Ptr,
        SymWithType,
        MethWithInst,
        Expr,
        Lim
    }

    internal enum ErrArgFlags
    {
        None = 0x0000,
        Ref = 0x0001,  // The arg's location should be included in the error message
        NoStr = 0x0002,  // The arg should NOT be included in the error message, just the location
        RefOnly = Ref | NoStr,
        Unique = 0x0004,  // The string should be distinct from other args marked with Unique
        UseGetErrorInfo = 0x0008,
    }

    internal class SymWithTypeMemo
    {
        public Symbol sym;
        public AggregateType ats;
    }

    internal class MethPropWithInstMemo
    {
        public Symbol sym;
        public AggregateType ats;
        public TypeArray typeArgs;
    }

    internal class ErrArg
    {
        public ErrArgKind eak;
        public ErrArgFlags eaf;
        internal MessageID ids;
        internal int n;
        internal SYMKIND sk;
        internal PredefinedName pdn;
        internal Name name;
        internal Symbol sym;
        internal string psz;
        internal CType pType;
        internal MethPropWithInstMemo mpwiMemo;
        internal SymWithTypeMemo swtMemo;

        public ErrArg()
        {
        }
        public ErrArg(int n)
        {
            this.eak = ErrArgKind.Int;
            this.eaf = ErrArgFlags.None;
            this.n = n;
        }
        public ErrArg(SYMKIND sk)
        {
            Debug.Assert(sk != SYMKIND.SK_AssemblyQualifiedNamespaceSymbol);
            this.eaf = ErrArgFlags.None;
            this.eak = ErrArgKind.SymKind;
            this.sk = sk;
        } // NSAIDSYMs are treated differently based on the Symbol not the SK

        public ErrArg(Name name)
        {
            this.eak = ErrArgKind.Name;
            this.eaf = ErrArgFlags.None;
            this.name = name;
        }
        public ErrArg(PredefinedName pdn)
        {
            this.eak = ErrArgKind.PredefName;
            this.eaf = ErrArgFlags.None;
            this.pdn = pdn;
        }

        public ErrArg(string psz)
        {
            this.eak = ErrArgKind.Str;
            this.eaf = ErrArgFlags.None;
            this.psz = psz;
        }
        public ErrArg(CType pType)
            : this(pType, ErrArgFlags.None)
        {
        }
        public ErrArg(CType pType, ErrArgFlags eaf)
        {
            this.eak = ErrArgKind.Type;
            this.eaf = eaf;
            this.pType = pType;
        }
        public ErrArg(Symbol pSym)
            : this(pSym, ErrArgFlags.None)
        {
        }
        public ErrArg(Symbol pSym, ErrArgFlags eaf)
        {
            this.eak = ErrArgKind.Sym;
            this.eaf = eaf;
            this.sym = pSym;
        }
        public ErrArg(SymWithType swt)
        {
            this.eak = ErrArgKind.SymWithType;
            this.eaf = ErrArgFlags.None;
            this.swtMemo = new SymWithTypeMemo();
            this.swtMemo.sym = swt.Sym;
            this.swtMemo.ats = swt.Ats;
        }
        public ErrArg(MethPropWithInst mpwi)
        {
            this.eak = ErrArgKind.MethWithInst;
            this.eaf = ErrArgFlags.None;
            this.mpwiMemo = new MethPropWithInstMemo();
            this.mpwiMemo.sym = mpwi.Sym;
            this.mpwiMemo.ats = mpwi.Ats;
            this.mpwiMemo.typeArgs = mpwi.TypeArgs;
        }
        public static implicit operator ErrArg(int n)
        {
            return new ErrArg(n);
        }
        public static implicit operator ErrArg(SYMKIND sk)
        {
            return new ErrArg(sk);
        }
        public static implicit operator ErrArg(CType type)
        {
            return new ErrArg(type);
        }
        public static implicit operator ErrArg(string psz)
        {
            return new ErrArg(psz);
        }
        public static implicit operator ErrArg(PredefinedName pdn)
        {
            return new ErrArg(pdn);
        }
        public static implicit operator ErrArg(Name name)
        {
            return new ErrArg(name);
        }
        public static implicit operator ErrArg(Symbol pSym)
        {
            return new ErrArg(pSym);
        }
        public static implicit operator ErrArg(SymWithType swt)
        {
            return new ErrArg(swt);
        }
        public static implicit operator ErrArg(MethPropWithInst mpwi)
        {
            return new ErrArg(mpwi);
        }
    }


    internal class ErrArgRef : ErrArg
    {
        public ErrArgRef()
        {
        }
        public ErrArgRef(int n)
            : base(n)
        {
        }
        public ErrArgRef(Name name)
            : base(name)
        {
            this.eaf = ErrArgFlags.Ref;
        }
        public ErrArgRef(string psz)
            : base(psz)
        {
            this.eaf = ErrArgFlags.Ref;
        }
        public ErrArgRef(Symbol sym)
            : base(sym)
        {
            this.eaf = ErrArgFlags.Ref;
        }
        public ErrArgRef(CType pType)
            : base(pType)
        {
            this.eaf = ErrArgFlags.Ref;
        }
        public ErrArgRef(SymWithType swt)
            : base(swt)
        {
            this.eaf = ErrArgFlags.Ref;
        }
        public ErrArgRef(MethPropWithInst mpwi)
            : base(mpwi)
        {
            this.eaf = ErrArgFlags.Ref;
        }
        public ErrArgRef(CType pType, ErrArgFlags eaf)
            : base(pType)
        {
            this.eaf = eaf | ErrArgFlags.Ref;
        }
        public static implicit operator ErrArgRef(string s)
        {
            return new ErrArgRef(s);
        }
        public static implicit operator ErrArgRef(Name name)
        {
            return new ErrArgRef(name);
        }
        public static implicit operator ErrArgRef(int n)
        {
            return new ErrArgRef(n);
        }
        public static implicit operator ErrArgRef(Symbol sym)
        {
            return new ErrArgRef(sym);
        }
        public static implicit operator ErrArgRef(CType type)
        {
            return new ErrArgRef(type);
        }
        public static implicit operator ErrArgRef(SymWithType swt)
        {
            return new ErrArgRef(swt);
        }
        public static implicit operator ErrArgRef(MethPropWithInst mpwi)
        {
            return new ErrArgRef(mpwi);
        }
    }

    internal class ErrArgRefOnly : ErrArgRef
    {
        public ErrArgRefOnly(Symbol sym)
            : base(sym)
        {
            eaf = ErrArgFlags.RefOnly;
        }
    }

    // This is used with COMPILER_BASE::ErrorRef to indicate no reference.
    internal class ErrArgNoRef : ErrArgRef
    {
        public ErrArgNoRef(CType pType)
        {
            this.eak = ErrArgKind.Type;
            this.eaf = ErrArgFlags.None;
            this.pType = pType;
        }
    }

    internal class ErrArgIds : ErrArgRef
    {
        public ErrArgIds(MessageID ids)
        {
            this.eak = ErrArgKind.Ids;
            this.eaf = ErrArgFlags.None;
            this.ids = ids;
        }
    }

    sealed internal class ErrArgSymKind : ErrArgRef
    {
        public ErrArgSymKind(Symbol sym)
        {
            eak = ErrArgKind.SymKind;
            eaf = ErrArgFlags.None;
            sk = sym.getKind();
            if (sk == SYMKIND.SK_AssemblyQualifiedNamespaceSymbol)
            {
                if (!String.IsNullOrEmpty(sym.AsAssemblyQualifiedNamespaceSymbol().GetNS().name.Text))
                {
                    // Non-empty namespace name means it's not the root
                    // so treat it like a namespace instead of an alias
                    sk = SYMKIND.SK_NamespaceSymbol;
                }
                else
                {
                    // An empty namespace name means it's just an alias for the root
                    sk = SYMKIND.SK_ExternalAliasDefinitionSymbol;
                }
            }
        }
    }
}
