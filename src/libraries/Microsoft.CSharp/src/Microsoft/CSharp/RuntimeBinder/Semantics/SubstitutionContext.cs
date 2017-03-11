// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // Used to specify whether and which type variables should be normalized.
    [Flags]
    internal enum SubstTypeFlags
    {
        NormNone = 0x00,
        NormClass = 0x01,  // Replace class type variables with the normalized (standard) ones.
        NormMeth = 0x02,   // Replace method type variables with the normalized (standard) ones.
        NormAll = NormClass | NormMeth,
        DenormClass = 0x04,  // Replace normalized (standard) class type variables with the given class type args.
        DenormMeth = 0x08,   // Replace normalized (standard) method type variables with the given method type args.
        DenormAll = DenormClass | DenormMeth,
        NoRefOutDifference = 0x10
    }

    internal sealed class SubstContext
    {
        public CType[] prgtypeCls;
        public int ctypeCls;
        public CType[] prgtypeMeth;
        public int ctypeMeth;
        public SubstTypeFlags grfst;

        public SubstContext(TypeArray typeArgsCls, TypeArray typeArgsMeth, SubstTypeFlags grfst)
        {
            Init(typeArgsCls, typeArgsMeth, grfst);
        }

        public SubstContext(AggregateType type)
            : this(type, null, SubstTypeFlags.NormNone)
        {
        }

        public SubstContext(AggregateType type, TypeArray typeArgsMeth)
            : this(type, typeArgsMeth, SubstTypeFlags.NormNone)
        {
        }

        private SubstContext(AggregateType type, TypeArray typeArgsMeth, SubstTypeFlags grfst)
        {
            Init(type?.GetTypeArgsAll(), typeArgsMeth, grfst);
        }

        public SubstContext(CType[] prgtypeCls, int ctypeCls, CType[] prgtypeMeth, int ctypeMeth)
            : this(prgtypeCls, ctypeCls, prgtypeMeth, ctypeMeth, SubstTypeFlags.NormNone)
        {
        }

        private SubstContext(CType[] prgtypeCls, int ctypeCls, CType[] prgtypeMeth, int ctypeMeth, SubstTypeFlags grfst)
        {
            this.prgtypeCls = prgtypeCls;
            this.ctypeCls = ctypeCls;
            this.prgtypeMeth = prgtypeMeth;
            this.ctypeMeth = ctypeMeth;
            this.grfst = grfst;
        }

        public bool FNop()
        {
            return 0 == ctypeCls && 0 == ctypeMeth && 0 == (grfst & SubstTypeFlags.NormAll);
        }

        // Initializes a substitution context. Returns false iff no substitutions will ever be performed.
        private void Init(TypeArray typeArgsCls, TypeArray typeArgsMeth, SubstTypeFlags grfst)
        {
            if (typeArgsCls != null)
            {
                typeArgsCls.AssertValid();
                ctypeCls = typeArgsCls.Count;
                prgtypeCls = typeArgsCls.Items;
            }
            else
            {
                ctypeCls = 0;
                prgtypeCls = null;
            }

            if (typeArgsMeth != null)
            {
                typeArgsMeth.AssertValid();
                ctypeMeth = typeArgsMeth.Count;
                prgtypeMeth = typeArgsMeth.Items;
            }
            else
            {
                ctypeMeth = 0;
                prgtypeMeth = null;
            }

            this.grfst = grfst;
        }
    }
}
