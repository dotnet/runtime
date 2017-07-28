// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed partial class ExpressionBinder
    {
        private class BinOpSig
        {
            protected BinOpSig()
            {
            }

            public BinOpSig(PredefinedType pt1, PredefinedType pt2,
                BinOpMask mask, int cbosSkip, PfnBindBinOp pfn, OpSigFlags grfos, BinOpFuncKind fnkind)
            {
                this.pt1 = pt1;
                this.pt2 = pt2;
                this.mask = mask;
                this.cbosSkip = cbosSkip;
                this.pfn = pfn;
                this.grfos = grfos;
                this.fnkind = fnkind;
            }
            public PredefinedType pt1;
            public PredefinedType pt2;
            public BinOpMask mask;
            public int cbosSkip;
            public PfnBindBinOp pfn;
            public OpSigFlags grfos;
            public BinOpFuncKind fnkind;

            public bool ConvertOperandsBeforeBinding()
            {
                return (grfos & OpSigFlags.Convert) != 0;
            }

            public bool CanLift()
            {
                return (grfos & OpSigFlags.CanLift) != 0;
            }

            public bool AutoLift()
            {
                return (grfos & OpSigFlags.AutoLift) != 0;
            }
        }

        private sealed class BinOpFullSig : BinOpSig
        {
            private readonly LiftFlags _grflt;
            private readonly CType _type1;
            private readonly CType _type2;

            public BinOpFullSig(CType type1, CType type2, PfnBindBinOp pfn, OpSigFlags grfos,
                LiftFlags grflt, BinOpFuncKind fnkind)
            {
                this.pt1 = PredefinedType.PT_UNDEFINEDINDEX;
                this.pt2 = PredefinedType.PT_UNDEFINEDINDEX;
                this.mask = BinOpMask.None;
                this.cbosSkip = 0;
                this.pfn = pfn;
                this.grfos = grfos;
                _type1 = type1;
                _type2 = type2;
                _grflt = grflt;
                this.fnkind = fnkind;
            }

            /***************************************************************************************************
                Set the values of the BinOpFullSig from the given BinOpSig. The ExpressionBinder is needed to get
                the predefined types. Returns true iff the predef types are found.
            ***************************************************************************************************/
            public BinOpFullSig(ExpressionBinder fnc, BinOpSig bos)
            {
                this.pt1 = bos.pt1;
                this.pt2 = bos.pt2;
                this.mask = bos.mask;
                this.cbosSkip = bos.cbosSkip;
                this.pfn = bos.pfn;
                this.grfos = bos.grfos;
                this.fnkind = bos.fnkind;

                _type1 = pt1 != PredefinedType.PT_UNDEFINEDINDEX ? fnc.GetPredefindType(pt1) : null;
                _type2 = pt2 != PredefinedType.PT_UNDEFINEDINDEX ? fnc.GetPredefindType(pt2) : null;
                _grflt = LiftFlags.None;
            }

            public bool FPreDef()
            {
                return pt1 != PredefinedType.PT_UNDEFINEDINDEX;
            }

            public bool isLifted()
            {
                if (_grflt == LiftFlags.None)
                {
                    return false;
                }

                // We can't both convert and lift.
                Debug.Assert(((_grflt & LiftFlags.Lift1) == 0) || ((_grflt & LiftFlags.Convert1) == 0));
                Debug.Assert(((_grflt & LiftFlags.Lift2) == 0) || ((_grflt & LiftFlags.Convert2) == 0));

                return true;
            }

            public bool ConvertFirst()
            {
                return (_grflt & LiftFlags.Convert1) != 0;
            }

            public bool ConvertSecond()
            {
                return (_grflt & LiftFlags.Convert2) != 0;
            }

            public CType Type1()
            {
                return _type1;
            }

            public CType Type2()
            {
                return _type2;
            }
        }
    }
}

