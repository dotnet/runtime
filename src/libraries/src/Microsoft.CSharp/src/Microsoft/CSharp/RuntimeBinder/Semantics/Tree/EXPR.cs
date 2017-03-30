// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal abstract class Expr
    {
        internal object RuntimeObject { get; set; }

        internal CType RuntimeObjectActualType { get; set; }

        public ExpressionKind Kind { get; set; }

        public EXPRFLAG Flags { get; set; }

        public bool IsOptionalArgument { get; set; }

        public void SetInaccessibleBit()
        {
            HasError = true;
        }

        public void SetMismatchedStaticBit()
        {
            if (Kind == ExpressionKind.EK_CALL && this.asCALL().MemberGroup != null)
                this.asCALL().MemberGroup.SetMismatchedStaticBit();
            HasError = true;
        }

        public string ErrorString { get; set; }

        public CType Type { get; set; }

        public void SetAssignment()
        {
            Debug.Assert(!this.isSTMT());
            Flags |= EXPRFLAG.EXF_ASSGOP;
        }

        public bool IsOK => !HasError;

        public bool HasError { get; private set; }

        public void SetError()
        {
            HasError = true;
        }

        public bool HasObject
        {
            get
            {
                switch (Kind)
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
        }

        public Expr Args
        {
            get
            {
                Debug.Assert(this.isCALL() || this.isPROP() || this.isFIELD() || this.isARRAYINDEX());
                if (this.isFIELD())
                    return null;

                switch (Kind)
                {
                    case ExpressionKind.EK_CALL:
                        return this.asCALL().OptionalArguments;

                    case ExpressionKind.EK_PROP:
                        return this.asPROP().OptionalArguments;

                    case ExpressionKind.EK_ARRAYINDEX:
                        return this.asARRAYINDEX().Index;
                }

                Debug.Assert(false, "Shouldn't get here without a CALL, PROP, FIELD or ARRINDEX");
                return null;
            }

            set
            {
                Debug.Assert(this.isCALL() || this.isPROP() || this.isFIELD() || this.isARRAYINDEX());
                if (this.isFIELD())
                {
                    Debug.Assert(false, "Setting arguments on a field.");
                    return;
                }

                switch (Kind)
                {
                    case ExpressionKind.EK_CALL:
                        this.asCALL().OptionalArguments = value;
                        return;

                    case ExpressionKind.EK_PROP:
                        this.asPROP().OptionalArguments = value;
                        return;

                    case ExpressionKind.EK_ARRAYINDEX:
                        this.asARRAYINDEX().Index = value;
                        return;
                }

                Debug.Assert(false, "Shouldn't get here without a CALL, PROP, FIELD or ARRINDEX");
            }
        }

        public Expr Object
        {
            get
            {
                Debug.Assert(HasObject);
                switch (Kind)
                {
                    case ExpressionKind.EK_FIELD:
                        return this.asFIELD().OptionalObject;
                    case ExpressionKind.EK_PROP:
                        return this.asPROP().MemberGroup.OptionalObject;
                    case ExpressionKind.EK_CALL:
                        return this.asCALL().MemberGroup.OptionalObject;
                    case ExpressionKind.EK_MEMGRP:
                        return this.asMEMGRP().OptionalObject;
                    case ExpressionKind.EK_EVENT:
                        return this.asEVENT().OptionalObject;
                    case ExpressionKind.EK_FUNCPTR:
                        return this.asFUNCPTR().OptionalObject;
                }

                return null;
            }

            set
            {
                Debug.Assert(HasObject);
                switch (Kind)
                {
                    case ExpressionKind.EK_FIELD:
                        this.asFIELD().OptionalObject = value;
                        break;
                    case ExpressionKind.EK_PROP:
                        this.asPROP().MemberGroup.OptionalObject = value;
                        break;
                    case ExpressionKind.EK_CALL:
                        this.asCALL().MemberGroup.OptionalObject = value;
                        break;
                    case ExpressionKind.EK_MEMGRP:
                        this.asMEMGRP().OptionalObject = value;
                        break;
                    case ExpressionKind.EK_EVENT:
                        this.asEVENT().OptionalObject = value;
                        break;
                    case ExpressionKind.EK_FUNCPTR:
                        this.asFUNCPTR().OptionalObject = value;
                        break;
                }
            }
        }

        public SymWithType GetSymWithType()
        {
            switch (Kind)
            {
                default:
                    Debug.Assert(false, "Bad expr kind in GetSymWithType");
                    return ((ExprCall)this).MethWithInst;
                case ExpressionKind.EK_CALL:
                    return ((ExprCall)this).MethWithInst;
                case ExpressionKind.EK_PROP:
                    return ((ExprProperty)this).PropWithTypeSlot;
                case ExpressionKind.EK_FIELD:
                    return ((ExprField)this).FieldWithType;
                case ExpressionKind.EK_EVENT:
                    return ((ExprEvent)this).EventWithType;
            }
        }
    }
}
