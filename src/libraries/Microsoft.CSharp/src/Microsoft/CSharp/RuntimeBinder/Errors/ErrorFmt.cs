// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        SymKind,
        Sym,
        Type,
        Name,
        Str,
        SymWithType,
        MethWithInst,
    }

    [Flags]
    internal enum ErrArgFlags
    {
        None = 0x0000,
        NoStr = 0x0002,  // The arg should NOT be included in the error message, just the location
        Unique = 0x0004,  // The string should be distinct from other args marked with Unique
        UseGetErrorInfo = 0x0008,
    }

    internal sealed class SymWithTypeMemo
    {
        public Symbol sym;
        public AggregateType ats;
    }

    internal sealed class MethPropWithInstMemo
    {
        public Symbol sym;
        public AggregateType ats;
        public TypeArray typeArgs;
    }

    internal class ErrArg
    {
        public ErrArgKind eak;
        public ErrArgFlags eaf;
        internal int n;
        internal SYMKIND sk;
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

        public ErrArg(Name name)
        {
            this.eak = ErrArgKind.Name;
            this.eaf = ErrArgFlags.None;
            this.name = name;
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

        private ErrArg(Symbol pSym, ErrArgFlags eaf)
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
        public static implicit operator ErrArg(CType type)
        {
            return new ErrArg(type);
        }
        public static implicit operator ErrArg(string psz)
        {
            return new ErrArg(psz);
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

    internal sealed class ErrArgRefOnly : ErrArg
    {
        public ErrArgRefOnly(Symbol sym)
            : base(sym)
        {
            eaf = ErrArgFlags.NoStr;
        }
    }

    // This is used with COMPILER_BASE::ErrorRef to indicate no reference.
    internal sealed class ErrArgNoRef : ErrArg
    {
        public ErrArgNoRef(CType pType)
        {
            this.eak = ErrArgKind.Type;
            this.eaf = ErrArgFlags.None;
            this.pType = pType;
        }
    }

    internal sealed class ErrArgSymKind : ErrArg
    {
        public ErrArgSymKind(Symbol sym)
        {
            eak = ErrArgKind.SymKind;
            eaf = ErrArgFlags.None;
            sk = sym.getKind();
        }
    }
}
