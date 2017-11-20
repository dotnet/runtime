// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed partial class ExpressionBinder
    {
        private RuntimeBinderException ReportReadOnlyError(ExprField field, bool isNested)
        {
            Debug.Assert(field != null);

            FieldWithType fieldWithType = field.FieldWithType;
            bool isStatic = fieldWithType.Field().isStatic;
            ErrArg[] args;
            ErrorCode err;
            if (isNested)
            {
                args = new ErrArg[]{ fieldWithType };
                err = isStatic ? ErrorCode.ERR_AssgReadonlyStatic2 : ErrorCode.ERR_AssgReadonly2;
            }
            else
            {
                args = Array.Empty<ErrArg>();
                err = isStatic ? ErrorCode.ERR_AssgReadonlyStatic : ErrorCode.ERR_AssgReadonly;
            }

            return ErrorContext.Error(err, args);
        }

        private void TryReportLvalueFailure(Expr expr, CheckLvalueKind kind)
        {
            Debug.Assert(expr != null);
            Debug.Assert(!(expr is ExprLocal));

            // We have a lvalue failure. Was the reason because this field
            // was marked readonly? Give special messages for this case.

            bool isNested = false; // Did we recurse on a field or property to give a better error?

            while (true)
            {
                Debug.Assert(expr != null);

                Expr pObject = null;

                if (expr is ExprProperty prop)
                {
                    // We've already reported read-only-property errors.
                    Debug.Assert(prop.MethWithTypeSet != null);
                    pObject = prop.MemberGroup.OptionalObject;
                }
                else if (expr is ExprField field)
                {
                    if (field.FieldWithType.Field().isReadOnly)
                    {
                        throw ReportReadOnlyError(field, isNested);
                    }
                    if (!field.FieldWithType.Field().isStatic)
                    {
                        pObject = field.OptionalObject;
                    }
                }

                if (pObject != null && pObject.Type.isStructOrEnum())
                {
                    if (pObject is ExprWithArgs withArgs)
                    {
                        // assigning to RHS of method or property getter returning a value-type on the stack or
                        // passing RHS of method or property getter returning a value-type on the stack, as ref or out
                        throw ErrorContext.Error(ErrorCode.ERR_ReturnNotLValue, withArgs.GetSymWithType());
                    }
                    if (pObject is ExprCast)
                    {
                        // An unboxing conversion.
                        //
                        // In the static compiler, we give the following error here:
                        // ErrorContext.Error(pObject.GetTree(), ErrorCode.ERR_UnboxNotLValue);
                        //
                        // But in the runtime, we allow this - mark that we're doing an
                        // unbox here, so that we gen the correct expression tree for it.
                        pObject.Flags |= EXPRFLAG.EXF_UNBOXRUNTIME;
                        return;
                    }
                }

                // everything else
                if (pObject != null && !pObject.isLvalue() && (expr is ExprField || !isNested))
                {
                    Debug.Assert(pObject.Type.isStructOrEnum());
                    Debug.Assert(!(pObject is ExprLocal));
                    expr = pObject;
                }
                else
                {
                    throw ErrorContext.Error(GetStandardLvalueError(kind));
                }

                isNested = true;
            }
        }
    }
}
