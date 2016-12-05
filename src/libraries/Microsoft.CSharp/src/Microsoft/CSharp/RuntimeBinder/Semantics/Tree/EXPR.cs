// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal abstract class EXPR
    {
        protected static void RETAILVERIFY(bool f)
        {
            //if (!f)
            //Debug.Assert(false, "panic!");
        }

        internal object RuntimeObject;
        internal CType RuntimeObjectActualType;

        public ExpressionKind kind;
        public EXPRFLAG flags;
        public bool IsError;
        public bool IsOptionalArgument;
        public void SetInaccessibleBit()
        {
            IsError = true;
        }

        public void SetMismatchedStaticBit()
        {
            switch (kind)
            {
                case ExpressionKind.EK_CALL:
                    if (this.asCALL().GetMemberGroup() != null)
                        this.asCALL().GetMemberGroup().SetMismatchedStaticBit();
                    break;
            }
            IsError = true;
        }

        public string errorString;
        public CType type;
        public void setType(CType t)
        {
            type = t;
        }

        public void setAssignment()
        {
            Debug.Assert(!this.isSTMT());
            flags |= EXPRFLAG.EXF_ASSGOP;
        }

        public bool isOK()
        {
            return !HasError();
        }

        public bool HasError()
        {
            return IsError;
        }
        public void SetError()
        {
            IsError = true;
        }

        public bool HasObject()
        {
            switch (kind)
            {
                case ExpressionKind.EK_FIELD:
                case ExpressionKind.EK_PROP:
                case ExpressionKind.EK_CALL:
                case ExpressionKind.EK_EVENT:
                case ExpressionKind.EK_MEMGRP:
                case ExpressionKind.EK_FUNCPTR:
                    return true;
            }
            return false;
        }

        public EXPR getArgs()
        {
            RETAILVERIFY(this.isCALL() || this.isPROP() || this.isFIELD() || this.isARRAYINDEX());
            if (this.isFIELD())
                return null;
            switch (kind)
            {
                case ExpressionKind.EK_CALL:
                    return this.asCALL().GetOptionalArguments();

                case ExpressionKind.EK_PROP:
                    return this.asPROP().GetOptionalArguments();

                case ExpressionKind.EK_ARRAYINDEX:
                    return this.asARRAYINDEX().GetIndex();
            }
            Debug.Assert(false, "Shouldn't get here without a CALL, PROP, FIELD or ARRINDEX");
            return null;
        }

        public void setArgs(EXPR args)
        {
            RETAILVERIFY(this.isCALL() || this.isPROP() || this.isFIELD() || this.isARRAYINDEX());
            if (this.isFIELD())
            {
                Debug.Assert(false, "Setting arguments on a field.");
                return;
            }
            switch (kind)
            {
                case ExpressionKind.EK_CALL:
                    this.asCALL().SetOptionalArguments(args);
                    return;

                case ExpressionKind.EK_PROP:
                    this.asPROP().SetOptionalArguments(args);
                    return;

                case ExpressionKind.EK_ARRAYINDEX:
                    this.asARRAYINDEX().SetIndex(args);
                    return;
            }
            Debug.Assert(false, "Shouldn't get here without a CALL, PROP, FIELD or ARRINDEX");
        }

        public EXPR getObject()
        {
            RETAILVERIFY(HasObject());
            switch (kind)
            {
                case ExpressionKind.EK_FIELD:
                    return this.asFIELD().OptionalObject;
                case ExpressionKind.EK_PROP:
                    return this.asPROP().GetMemberGroup().OptionalObject;
                case ExpressionKind.EK_CALL:
                    return this.asCALL().GetMemberGroup().OptionalObject;
                case ExpressionKind.EK_MEMGRP:
                    return this.asMEMGRP().OptionalObject;
                case ExpressionKind.EK_EVENT:
                    return this.asEVENT().OptionalObject;
                case ExpressionKind.EK_FUNCPTR:
                    return this.asFUNCPTR().OptionalObject;
            }
            return null;
        }
        public void SetObject(EXPR pExpr)
        {
            RETAILVERIFY(HasObject());
            switch (kind)
            {
                case ExpressionKind.EK_FIELD:
                    this.asFIELD().OptionalObject = pExpr;
                    break;
                case ExpressionKind.EK_PROP:
                    this.asPROP().GetMemberGroup().OptionalObject = pExpr;
                    break;
                case ExpressionKind.EK_CALL:
                    this.asCALL().GetMemberGroup().OptionalObject = pExpr;
                    break;
                case ExpressionKind.EK_MEMGRP:
                    this.asMEMGRP().OptionalObject = pExpr;
                    break;
                case ExpressionKind.EK_EVENT:
                    this.asEVENT().OptionalObject = pExpr;
                    break;
                case ExpressionKind.EK_FUNCPTR:
                    this.asFUNCPTR().OptionalObject = pExpr;
                    break;
            }
        }

        public SymWithType GetSymWithType()
        {
            switch (kind)
            {
                default:
                    Debug.Assert(false, "Bad expr kind in GetSymWithType");
                    return ((EXPRCALL)this).mwi;
                case ExpressionKind.EK_CALL:
                    return ((EXPRCALL)this).mwi;
                case ExpressionKind.EK_PROP:
                    return ((EXPRPROP)this).pwtSlot;
                case ExpressionKind.EK_FIELD:
                    return ((EXPRFIELD)this).fwt;
                case ExpressionKind.EK_EVENT:
                    return ((EXPREVENT)this).ewt;
            }
        }
    }
}
