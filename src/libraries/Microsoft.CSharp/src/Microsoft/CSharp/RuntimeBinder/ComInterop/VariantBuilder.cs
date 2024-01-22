// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    /// <summary>
    /// VariantBuilder handles packaging of arguments into an ComVariant for a call to IDispatch.Invoke
    /// </summary>
    internal sealed class VariantBuilder
    {
        private MemberExpression _variant;
        private readonly ArgBuilder _argBuilder;
        private readonly VarEnum _targetComType;
        internal ParameterExpression TempVariable { get; private set; }

        internal VariantBuilder(VarEnum targetComType, ArgBuilder builder)
        {
            _targetComType = targetComType;
            _argBuilder = builder;
        }

        internal bool IsByRef
        {
            get { return (_targetComType & VarEnum.VT_BYREF) != 0; }
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal Expression InitializeArgumentVariant(MemberExpression variant, Expression parameter)
        {
            //NOTE: we must remember our variant
            //the reason is that argument order does not map exactly to the order of variants for invoke
            //and when we are doing clean-up we must be sure we are cleaning the variant we have initialized.

            _variant = variant;

            if (IsByRef)
            {
                // temp = argument
                // paramVariants._elementN.SetAsByrefT(ref temp)
                Debug.Assert(TempVariable == null);
                Expression argExpr = _argBuilder.MarshalToRef(parameter);

                TempVariable = Expression.Variable(argExpr.Type, null);
                return Expression.Block(
                    Expression.Assign(TempVariable, argExpr),
                    Expression.Call(
                        DynamicVariantExtensions.GetByrefSetter(_targetComType & ~VarEnum.VT_BYREF),
                        variant,
                        TempVariable
                    )
                );
            }

            Expression argument = _argBuilder.Marshal(parameter);

            // we are forced to special case ConvertibleArgBuilder since it does not have
            // a corresponding _targetComType.
            if (_argBuilder is ConvertibleArgBuilder)
            {
                return Expression.Call(
                    typeof(DynamicVariantExtensions).GetMethod(nameof(DynamicVariantExtensions.SetAsIConvertible)),
                    variant,
                    argument
                );
            }

            if (_targetComType.IsPrimitiveType() ||
               (_targetComType == VarEnum.VT_DISPATCH) ||
               (_targetComType == VarEnum.VT_UNKNOWN) ||
               (_targetComType == VarEnum.VT_VARIANT) ||
               (_targetComType == VarEnum.VT_RECORD) ||
               (_targetComType == VarEnum.VT_ARRAY))
            {
                // paramVariants._elementN.AsT = (cast)argN
                return Expression.Call(null,
                    DynamicVariantExtensions.GetSetter(_targetComType),
                    variant,
                    argument);
            }

            switch (_targetComType)
            {
                case VarEnum.VT_EMPTY:
                    return null;

                case VarEnum.VT_NULL:
                    // paramVariants._elementN = ComVariant.Null;
                    return Expression.Assign(variant, Expression.Property(null, typeof(ComVariant).GetProperty(nameof(ComVariant.Null), BindingFlags.Public | BindingFlags.Static)));

                default:
                    Debug.Assert(false, "Unexpected VarEnum");
                    return null;
            }
        }

        private static Expression Release(Expression pUnk)
        {
            return Expression.Call(typeof(UnsafeMethods).GetMethod(nameof(UnsafeMethods.IUnknownReleaseNotZero)), pUnk);
        }

        internal Expression Clear()
        {
            if (IsByRef)
            {
                if (_argBuilder is StringArgBuilder)
                {
                    Debug.Assert(TempVariable != null);
                    return Expression.Call(typeof(Marshal).GetMethod(nameof(Marshal.FreeBSTR)), TempVariable);
                }

                if (_argBuilder is DispatchArgBuilder)
                {
                    Debug.Assert(TempVariable != null);
                    return Release(TempVariable);
                }

                if (_argBuilder is UnknownArgBuilder)
                {
                    Debug.Assert(TempVariable != null);
                    return Release(TempVariable);
                }

                if (_argBuilder is VariantArgBuilder)
                {
                    Debug.Assert(TempVariable != null);
                    return Expression.Call(TempVariable, typeof(ComVariant).GetMethod(nameof(ComVariant.Dispose)));
                }
                return null;
            }

            switch (_targetComType)
            {
                case VarEnum.VT_EMPTY:
                case VarEnum.VT_NULL:
                    return null;

                case VarEnum.VT_BSTR:
                case VarEnum.VT_UNKNOWN:
                case VarEnum.VT_DISPATCH:
                case VarEnum.VT_ARRAY:
                case VarEnum.VT_RECORD:
                case VarEnum.VT_VARIANT:
                    // paramVariants._elementN.Dispose()
                    return Expression.Call(_variant, typeof(ComVariant).GetMethod(nameof(ComVariant.Dispose)));

                default:
                    Debug.Assert(_targetComType.IsPrimitiveType(), "Unexpected VarEnum");
                    return null;
            }
        }

        internal Expression UpdateFromReturn(Expression parameter)
        {
            if (TempVariable == null)
            {
                return null;
            }
            return Expression.Assign(
                parameter,
                Helpers.Convert(
                    _argBuilder.UnmarshalFromRef(TempVariable),
                    parameter.Type
                )
            );
        }
    }
}
