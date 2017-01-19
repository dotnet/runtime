// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal partial class ExpressionBinder
    {
        private static readonly ErrorCode[] s_ReadOnlyLocalErrors =
        {
            ErrorCode.ERR_RefReadonlyLocal,
            ErrorCode.ERR_AssgReadonlyLocal,
        };

        protected void ReportLocalError(LocalVariableSymbol local, CheckLvalueKind kind, bool isNested)
        {
            Debug.Assert(local != null);

            int index = kind == CheckLvalueKind.OutParameter ? 0 : 1;

            Debug.Assert(index != 2 && index != 3);
            // There is no way that we can have no cause AND a read-only local nested in a struct with a
            // writable field. What would make the local read-only if not one of the causes above?  (Const
            // locals may not be structs, so we would already have errored out in that scenario.)

            ErrorCode err = s_ReadOnlyLocalErrors[index];

            ErrorContext.Error(err, local.name);
        }

        private static readonly ErrorCode[] s_ReadOnlyErrors =
        {
            ErrorCode.ERR_RefReadonly,
            ErrorCode.ERR_AssgReadonly,
            ErrorCode.ERR_RefReadonlyStatic,
            ErrorCode.ERR_AssgReadonlyStatic,
            ErrorCode.ERR_RefReadonly2,
            ErrorCode.ERR_AssgReadonly2,
            ErrorCode.ERR_RefReadonlyStatic2,
            ErrorCode.ERR_AssgReadonlyStatic2
        };

        protected void ReportReadOnlyError(EXPRFIELD field, CheckLvalueKind kind, bool isNested)
        {
            Debug.Assert(field != null);

            bool isStatic = field.fwt.Field().isStatic;

            int index = (isNested ? 4 : 0) + (isStatic ? 2 : 0) + (kind == CheckLvalueKind.OutParameter ? 0 : 1);
            ErrorCode err = s_ReadOnlyErrors[index];

            if (isNested)
            {
                ErrorContext.Error(err, field.fwt);
            }
            else
            {
                ErrorContext.Error(err);
            }
        }

        // Return true if we actually report a failure.
        protected bool TryReportLvalueFailure(EXPR expr, CheckLvalueKind kind)
        {
            Debug.Assert(expr != null);

            // We have a lvalue failure. Was the reason because this field
            // was marked readonly? Give special messages for this case.

            bool isNested = false; // Did we recurse on a field or property to give a better error?

            EXPR walk = expr;
            while (true)
            {
                Debug.Assert(walk != null);

                if (walk.isANYLOCAL_OK())
                {
                    ReportLocalError(walk.asANYLOCAL().local, kind, isNested);
                    return true;
                }

                EXPR pObject = null;

                if (walk.isPROP())
                {
                    // We've already reported read-only-property errors.
                    Debug.Assert(walk.asPROP().mwtSet != null);
                    pObject = walk.asPROP().GetMemberGroup().GetOptionalObject();
                }
                else if (walk.isFIELD())
                {
                    EXPRFIELD field = walk.asFIELD();
                    if (field.fwt.Field().isReadOnly)
                    {
                        ReportReadOnlyError(field, kind, isNested);
                        return true;
                    }
                    if (!field.fwt.Field().isStatic)
                    {
                        pObject = field.GetOptionalObject();
                    }
                }

                if (pObject != null && pObject.type.isStructOrEnum())
                {
                    if (pObject.isCALL() || pObject.isPROP())
                    {
                        // assigning to RHS of method or property getter returning a value-type on the stack or
                        // passing RHS of method or property getter returning a value-type on the stack, as ref or out
                        ErrorContext.Error(ErrorCode.ERR_ReturnNotLValue, pObject.GetSymWithType());
                        return true;
                    }
                    if (pObject.isCAST())
                    {
                        // An unboxing conversion.
                        //
                        // In the static compiler, we give the following error here:
                        // ErrorContext.Error(pObject.GetTree(), ErrorCode.ERR_UnboxNotLValue);
                        //
                        // But in the runtime, we allow this - mark that we're doing an
                        // unbox here, so that we gen the correct expression tree for it.
                        pObject.flags |= EXPRFLAG.EXF_UNBOXRUNTIME;
                        return false;
                    }
                }

                // everything else
                if (pObject != null && !pObject.isLvalue() && (walk.isFIELD() || (!isNested && walk.isPROP())))
                {
                    Debug.Assert(pObject.type.isStructOrEnum());
                    walk = pObject;
                }
                else
                {
                    ErrorContext.Error(GetStandardLvalueError(kind));
                    return true;
                }
                isNested = true;
            }
        }

        public static void ReportTypeArgsNotAllowedError(SymbolLoader symbolLoader, int arity, ErrArgRef argName, ErrArgRef argKind)
        {
            symbolLoader.ErrorContext.ErrorRef(ErrorCode.ERR_TypeArgsNotAllowed, argName, argKind);
        }
    }
}
