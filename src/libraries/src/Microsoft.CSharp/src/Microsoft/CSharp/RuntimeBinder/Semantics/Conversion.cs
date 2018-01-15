// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum ConvKind
    {
        Identity = 1,  // Identity conversion
        Implicit = 2,  // Implicit conversion
        Explicit = 3,  // Explicit conversion
        Unknown = 4,  // Unknown so call canConvert
        None = 5,  // None
    }

    // Flags for bindImplicitConversion/bindExplicitConversion
    [Flags]
    internal enum CONVERTTYPE
    {
        NOUDC = 0x01,  // Do not consider user defined conversions.
        STANDARD = 0x02,  // standard only, but never pass it in, used only to check...
        ISEXPLICIT = 0x04,  // implicit conversion is really explicit
        CHECKOVERFLOW = 0x08,  // check overflow (like in a checked context).
        FORCECAST = 0x10,  // Do not optimize out the cast
        STANDARDANDNOUDC = 0x03,  // pass this in if you mean standard conversions only
    };

    internal enum BetterType
    {
        Same = 0,
        Left = 1,
        Right = 2,
        Neither = 3,
    }

    internal sealed partial class ExpressionBinder
    {
        private delegate bool ConversionFunc(
            Expr pSourceExpr,
            CType pSourceType,
            CType pDestinationType,
            bool needsExprDest,
            out Expr ppDestinationExpr,
            CONVERTTYPE flags);

        private static void RoundToFloat(double d, out float f)
        {
            f = (float)d;
        }
        private static long I64(long x) { return x; }
        private static long I64(ulong x) { return (long)x; }

        // 13.1.2 Implicit numeric conversions
        //
        // The implicit numeric conversions are:
        //
        // *   From sbyte to short, int, long, float, double, or decimal.
        // *   From byte to short, ushort, int, uint, long, ulong, float, double, or decimal.
        // *   From short to int, long, float, double, or decimal.
        // *   From ushort to int, uint, long, ulong, float, double, or decimal.
        // *   From int to long, float, double, or decimal.
        // *   From uint to long, ulong, float, double, or decimal.
        // *   From long to float, double, or decimal.
        // *   From ulong to float, double, or decimal.
        // *   From char to ushort, int, uint, long, ulong, float, double, or decimal.
        // *   From float to double.
        //
        // Conversions from int, uint, long or ulong to float and from long or ulong to double can cause a
        // loss of precision, but will never cause a loss of magnitude. The other implicit numeric
        // conversions never lose any information.
        //
        // There are no implicit conversions to the char type, so values of the other integral types do not
        // automatically convert to the char type.
        //
        // 13.2.1 Explicit numeric conversions
        //
        // The explicit numeric conversions are the conversions from a numeric-type to another numeric-type
        // for which an implicit numeric conversion (13.1.2) does not already exist:
        //
        // * From sbyte to byte, ushort, uint, ulong, or char.
        // * From byte to sbyte or char.
        // * From short to sbyte, byte, ushort, uint, ulong, or char.
        // * From ushort to sbyte, byte, short, or char.
        // * From int to sbyte, byte, short, ushort, uint, ulong, or char.
        // * From uint to sbyte, byte, short, ushort, int, or char.
        // * From long to sbyte, byte, short, ushort, int, uint, ulong, or char.
        // * From ulong to sbyte, byte, short, ushort, int, uint, long, or char.
        // * From char to sbyte, byte, or short.
        // * From float to sbyte, byte, short, ushort, int, uint, long, ulong, char, or decimal.
        // * From double to sbyte, byte, short, ushort, int, uint, long, ulong, char, float, or decimal.
        // * From decimal to sbyte, byte, short, ushort, int, uint, long, ulong, char, float, or double.


        private const byte ID = (byte)ConvKind.Identity;  // 0x01
        private const byte IMP = (byte)ConvKind.Implicit; // 0x02
        private const byte EXP = (byte)ConvKind.Explicit; // 0x03
        private const byte NO = (byte)ConvKind.None;      // 0x05
        private const byte CONV_KIND_MASK = 0x0F;
        private const byte UDC = 0x40;
        private const byte XUD = EXP | UDC;
        private const byte IUD = IMP | UDC;

        private static readonly byte[][] s_simpleTypeConversions =
        {
            //        to: BYTE  I2    I4    I8    FLT   DBL   DEC  CHAR  BOOL SBYTE U2    U4    U8
            /* from */
             new byte[]   /* BYTE */ {  ID  , IMP , IMP , IMP , IMP , IMP , IUD, EXP , NO , EXP , IMP , IMP , IMP },
             new byte[]   /*   I2 */ {  EXP , ID  , IMP , IMP , IMP , IMP , IUD, EXP , NO , EXP , EXP , EXP , EXP },
             new byte[]   /*   I4 */ {  EXP , EXP , ID  , IMP , IMP , IMP , IUD, EXP , NO , EXP , EXP , EXP , EXP },
             new byte[]   /*   I8 */ {  EXP , EXP , EXP , ID  , IMP , IMP , IUD, EXP , NO , EXP , EXP , EXP , EXP },
             new byte[]   /*  FLT */ {  EXP , EXP , EXP , EXP , ID  , IMP , XUD, EXP , NO , EXP , EXP , EXP , EXP },
             new byte[]   /*  DBL */ {  EXP , EXP , EXP , EXP , EXP , ID  , XUD, EXP , NO , EXP , EXP , EXP , EXP },
             new byte[]   /*  DEC */ {  XUD , XUD , XUD , XUD , XUD , XUD , ID , XUD , NO , XUD , XUD , XUD , XUD },
             new byte[]   /* CHAR */ {  EXP , EXP , IMP , IMP , IMP , IMP , IUD, ID  , NO , EXP , IMP , IMP , IMP },
             new byte[]   /* BOOL */ {  NO  , NO  , NO  , NO  , NO  , NO  , NO , NO  , ID , NO  , NO  , NO  , NO  },
             new byte[]   /*SBYTE */ {  EXP , IMP , IMP , IMP , IMP , IMP , IUD, EXP , NO , ID  , EXP , EXP , EXP },
             new byte[]   /*   U2 */ {  EXP , EXP , IMP , IMP , IMP , IMP , IUD, EXP , NO , EXP , ID  , IMP , IMP },
             new byte[]   /*   U4 */ {  EXP , EXP , EXP , IMP , IMP , IMP , IUD, EXP , NO , EXP , EXP , ID  , IMP },
             new byte[]   /*   U8 */ {  EXP , EXP , EXP , EXP , IMP , IMP , IUD, EXP , NO , EXP , EXP , EXP , ID  },
        };

        private const int NUM_SIMPLE_TYPES = (int)PredefinedType.PT_ULONG + 1;
        private const int NUM_EXT_TYPES = (int)PredefinedType.PT_OBJECT + 1;

        private static ConvKind GetConvKind(PredefinedType ptSrc, PredefinedType ptDst)
        {
            if ((int)ptSrc < NUM_SIMPLE_TYPES && (int)ptDst < NUM_SIMPLE_TYPES)
            {
                return (ConvKind)(s_simpleTypeConversions[(int)ptSrc][(int)ptDst] & CONV_KIND_MASK);
            }
            if (ptSrc == ptDst || ptDst == PredefinedType.PT_OBJECT && ptSrc < PredefinedType.PT_COUNT)
            {
                return ConvKind.Implicit;
            }
            if (ptSrc == PredefinedType.PT_OBJECT && ptDst < PredefinedType.PT_COUNT)
            {
                return ConvKind.Explicit;
            }
            return ConvKind.Unknown;
        }

        private static bool isUserDefinedConversion(PredefinedType ptSrc, PredefinedType ptDst)
        {
            if ((int)ptSrc < NUM_SIMPLE_TYPES && (int)ptDst < NUM_SIMPLE_TYPES)
            {
                return 0 != (s_simpleTypeConversions[(int)ptSrc][(int)ptDst] & UDC);
            }
            return false;
        }

        // 14.4.2.3 Better conversion
        //
        // Given an implicit conversion C1 that converts from a type S to a type T1, and an implicit
        // conversion C2 that converts from a type S to a type T2, the better conversion of the two
        // conversions is determined as follows:
        //
        // * If T1 and T2 are the same type, neither conversion is better.
        // * If S is T1, C1 is the better conversion.
        // * If S is T2, C2 is the better conversion.
        // * If an implicit conversion from T1 to T2 exists, and no implicit conversion from T2 to T1
        //   exists, C1 is the better conversion.
        // * If an implicit conversion from T2 to T1 exists, and no implicit conversion from T1 to T2
        //   exists, C2 is the better conversion.
        // * If T1 is sbyte and T2 is byte, ushort, uint, or ulong, C1 is the better conversion.
        // * If T2 is sbyte and T1 is byte, ushort, uint, or ulong, C2 is the better conversion.
        // * If T1 is short and T2 is ushort, uint, or ulong, C1 is the better conversion.
        // * If T2 is short and T1 is ushort, uint, or ulong, C2 is the better conversion.
        // * If T1 is int and T2 is uint, or ulong, C1 is the better conversion.
        // * If T2 is int and T1 is uint, or ulong, C2 is the better conversion.
        // * If T1 is long and T2 is ulong, C1 is the better conversion.
        // * If T2 is long and T1 is ulong, C2 is the better conversion.
        // * Otherwise, neither conversion is better.
        //
        // If an implicit conversion C1 is defined by these rules to be a better conversion than an
        // implicit conversion C2, then it is also the case that C2 is a worse conversion than C1.

        private const byte same = (byte)BetterType.Same;
        private const byte left = (byte)BetterType.Left;
        private const byte right = (byte)BetterType.Right;
        private const byte neither = (byte)BetterType.Neither;


        private static readonly byte[][] s_simpleTypeBetter =
        {
            //           BYTE    SHORT   INT     LONG    FLOAT   DOUBLE  DECIMAL CHAR    BOOL    SBYTE   USHORT  UINT    ULONG   IPTR    UIPTR   OBJECT
            new byte[]  /* BYTE   */{same   ,left   ,left   ,left   ,left   ,left   ,left   ,neither,neither,right  ,left   ,left   ,left   ,neither,neither,left   },
              new byte[]  /* SHORT  */{right  ,same   ,left   ,left   ,left   ,left   ,left   ,neither,neither,right  ,left   ,left   ,left   ,neither,neither,left   },
              new byte[]  /* INT    */{right  ,right  ,same   ,left   ,left   ,left   ,left   ,right  ,neither,right  ,right  ,left   ,left   ,neither,neither,left   },
              new byte[]  /* LONG   */{right  ,right  ,right  ,same   ,left   ,left   ,left   ,right  ,neither,right  ,right  ,right  ,left   ,neither,neither,left   },
              new byte[]  /* FLOAT  */{right  ,right  ,right  ,right  ,same   ,left   ,neither,right  ,neither,right  ,right  ,right  ,right  ,neither,neither,left   },
              new byte[]  /* DOUBLE */{right  ,right  ,right  ,right  ,right  ,same   ,neither,right  ,neither,right  ,right  ,right  ,right  ,neither,neither,left   },
              new byte[]  /* DECIMAL*/{right  ,right  ,right  ,right  ,neither,neither,same   ,right  ,neither,right  ,right  ,right  ,right  ,neither,neither,left   },
              new byte[]  /* CHAR   */{neither,neither,left   ,left   ,left   ,left   ,left   ,same   ,neither,neither,left   ,left   ,left   ,neither,neither,left   },
              new byte[]  /* BOOL   */{neither,neither,neither,neither,neither,neither,neither,neither,same   ,neither,neither,neither,neither,neither,neither,left   },
              new byte[]  /* SBYTE  */{left   ,left   ,left   ,left   ,left   ,left   ,left   ,neither,neither,same   ,left   ,left   ,left   ,neither,neither,left   },
              new byte[]  /* USHORT */{right  ,right  ,left   ,left   ,left   ,left   ,left   ,right  ,neither,right  ,same   ,left   ,left   ,neither,neither,left   },
              new byte[]  /* UINT   */{right  ,right  ,right  ,left   ,left   ,left   ,left   ,right  ,neither,right  ,right  ,same   ,left   ,neither,neither,left   },
              new byte[]  /* ULONG  */{right  ,right  ,right  ,right  ,left   ,left   ,left   ,right  ,neither,right  ,right  ,right  ,same   ,neither,neither,left   },
              new byte[]  /* IPTR   */{neither,neither,neither,neither,neither,neither,neither,neither,neither,neither,neither,neither,neither,same   ,neither,left   },
              new byte[]  /* UIPTR  */{neither,neither,neither,neither,neither,neither,neither,neither,neither,neither,neither,neither,neither,neither,same   ,left   },
              new byte[]  /* OBJECT */{right  ,right  ,right  ,right  ,right  ,right  ,right  ,right  ,right  ,right  ,right  ,right  ,right  ,right  ,right  ,same   }
        };
#if DEBUG
        private static volatile bool s_fCheckedBetter = false;
        private void CheckBetterTable()
        {
            if (s_fCheckedBetter)
            {
                return;
            }
            for (int i = 0; i < NUM_EXT_TYPES; i++)
            {
                Debug.Assert(s_simpleTypeBetter[i][i] == same);
                for (int j = 0; j < i; j++)
                {
                    Debug.Assert(s_simpleTypeBetter[i][j] != same && s_simpleTypeBetter[j][i] != same);
                    Debug.Assert(
                        (s_simpleTypeBetter[i][j] == left && s_simpleTypeBetter[j][i] == right) ||
                        (s_simpleTypeBetter[i][j] == right && s_simpleTypeBetter[j][i] == left) ||
                        (s_simpleTypeBetter[i][j] == neither && s_simpleTypeBetter[j][i] == neither));
                    Debug.Assert(
                        GetPredefindType((PredefinedType)i) == null ||
                        GetPredefindType((PredefinedType)j) == null ||
                        (!canConvert(GetPredefindType((PredefinedType)i), GetPredefindType((PredefinedType)j), CONVERTTYPE.NOUDC) || s_simpleTypeBetter[i][j] == left) &&
                        (!canConvert(GetPredefindType((PredefinedType)j), GetPredefindType((PredefinedType)i), CONVERTTYPE.NOUDC) || s_simpleTypeBetter[j][i] == left));
                }
            }
            s_fCheckedBetter = true;
        }
#endif // DEBUG

        private BetterType WhichSimpleConversionIsBetter(PredefinedType pt1, PredefinedType pt2)
        {
#if DEBUG
            CheckBetterTable();
#endif // DEBUG
            Debug.Assert((int)pt1 < NUM_EXT_TYPES);
            Debug.Assert((int)pt2 < NUM_EXT_TYPES);
            return (BetterType)s_simpleTypeBetter[(int)pt1][(int)pt2];
        }



        /***************************************************************************************************
            Determined which conversion to a predefined type is better relative to a given type. It is
            assumed that the given type is implicitly convertible to both of the predefined types
            (possibly via a user defined conversion, method group conversion, etc).
        ***************************************************************************************************/
        private BetterType WhichTypeIsBetter(PredefinedType pt1, PredefinedType pt2, CType typeGiven)
        {
            if (pt1 == pt2)
            {
                return BetterType.Same;
            }
            if (typeGiven.isPredefType(pt1))
            {
                return BetterType.Left;
            }
            if (typeGiven.isPredefType(pt2))
            {
                return BetterType.Right;
            }
            if ((int)pt1 < NUM_EXT_TYPES && (int)pt2 < NUM_EXT_TYPES)
            {
                return WhichSimpleConversionIsBetter(pt1, pt2);
            }
            if (pt2 == PredefinedType.PT_OBJECT && pt1 < PredefinedType.PT_COUNT)
            {
                return BetterType.Left;
            }
            if (pt1 == PredefinedType.PT_OBJECT && pt2 < PredefinedType.PT_COUNT)
            {
                return BetterType.Right;
            }
            return WhichTypeIsBetter(GetPredefindType(pt1), GetPredefindType(pt2), typeGiven);
        }


        /***************************************************************************************************
            Determined which conversion is better relative to a given type. It is assumed that the given type
            (or its associated expression) is implicitly convertible to both of the types (possibly via
            a user defined conversion, method group conversion, etc).
        ***************************************************************************************************/
        private BetterType WhichTypeIsBetter(CType type1, CType type2, CType typeGiven)
        {
            Debug.Assert(type1 != null && type2 != null);
            if (type1 == type2)
            {
                return BetterType.Same;
            }
            if (typeGiven == type1)
            {
                return BetterType.Left;
            }
            if (typeGiven == type2)
            {
                return BetterType.Right;
            }

            bool f12 = canConvert(type1, type2);
            bool f21 = canConvert(type2, type1);
            if (f12 != f21)
            {
                return f12 ? BetterType.Left : BetterType.Right;
            }

            if (!(type1 is NullableType nub1) || !(type2 is NullableType nub2) ||
                !nub1.UnderlyingType.isPredefined() ||
                !nub2.UnderlyingType.isPredefined())
            {
                return BetterType.Neither;
            }

            PredefinedType pt1 = (type1 as NullableType).UnderlyingType.getPredefType();
            PredefinedType pt2 = (type2 as NullableType).UnderlyingType.getPredefType();

            if ((int)pt1 < NUM_EXT_TYPES && (int)pt2 < NUM_EXT_TYPES)
            {
                return WhichSimpleConversionIsBetter(pt1, pt2);
            }

            return BetterType.Neither;
        }

        // returns true if an implicit conversion exists from source type to dest type. flags is an optional parameter.
        private bool canConvert(CType src, CType dest, CONVERTTYPE flags) => BindImplicitConversion(null, src, dest, flags);

        public bool canConvert(CType src, CType dest) => canConvert(src, dest, 0);

        // returns true if a implicit conversion exists from source expr to dest type. flags is an optional parameter.
        private bool canConvert(Expr expr, CType dest) => canConvert(expr, dest, 0);

        private bool canConvert(Expr expr, CType dest, CONVERTTYPE flags) =>
            BindImplicitConversion(expr, expr.Type, dest, flags);

        // performs an implicit conversion if it's possible. otherwise displays an error. flags is an optional parameter.

        private Expr mustConvertCore(Expr expr, CType destExpr) => mustConvertCore(expr, destExpr, 0);

        private Expr mustConvertCore(Expr expr, CType dest, CONVERTTYPE flags)
        {
            Debug.Assert(!(expr is ExprMemberGroup));

            if (BindImplicitConversion(expr, expr.Type, dest, out Expr exprResult, flags))
            {
                // Conversion works.
                checkUnsafe(expr.Type); // added to the binder so we don't bind to pointer ops
                checkUnsafe(dest); // added to the binder so we don't bind to pointer ops
                return exprResult;
            }

            // don't report cascading error.

            // For certain situations, try to give a better error.

            FUNDTYPE ftSrc = expr.Type.fundType();
            FUNDTYPE ftDest = dest.fundType();

            if (expr is ExprConstant constant &&
                expr.Type.isSimpleType() && dest.isSimpleType())
            {
                if ((ftSrc == FUNDTYPE.FT_I4 && (ftDest <= FUNDTYPE.FT_LASTNONLONG || ftDest == FUNDTYPE.FT_U8)) ||
                    (ftSrc == FUNDTYPE.FT_I8 && ftDest == FUNDTYPE.FT_U8))
                {
                    // Failed because value was out of range. Report nifty error message.
                    string value = constant.Int64Value.ToString(CultureInfo.InvariantCulture);
                    throw ErrorContext.Error(ErrorCode.ERR_ConstOutOfRange, value, dest);
                }
            }

            if (expr.Type is NullType && dest.fundType() != FUNDTYPE.FT_REF)
            {
                throw ErrorContext.Error(ErrorCode.ERR_ValueCantBeNull, dest);
            }

            // canCast => can't convert, but explicit exists and can be specified by the user (no anonymous types).
            // !canCast => Generic "can't convert" error.
            throw ErrorContext.Error(canCast(expr.Type, dest, flags) ? ErrorCode.ERR_NoImplicitConvCast : ErrorCode.ERR_NoImplicitConv, new ErrArg(expr.Type, ErrArgFlags.Unique), new ErrArg(dest, ErrArgFlags.Unique));
        }

        // performs an implicit conversion if its possible. otherwise returns null. flags is an optional parameter.
        // Only call this if you are ALWAYS going to use the returned result (and you're not just going to test and
        // possibly throw away the result)
        // If the conversion is possible it will modify an Anonymous Method expr thus changing results of
        // future conversions.  It will also produce possible binding errors for method groups.

        public Expr tryConvert(Expr expr, CType dest)
        {
            return tryConvert(expr, dest, 0);
        }

        private Expr tryConvert(Expr expr, CType dest, CONVERTTYPE flags)
        {
            if (BindImplicitConversion(expr, expr.Type, dest, out Expr exprResult, flags))
            {
                checkUnsafe(expr.Type); // added to the binder so we don't bind to pointer ops
                checkUnsafe(dest); // added to the binder so we don't bind to pointer ops
                // Conversion works.
                return exprResult;
            }

            return null;
        }

        public Expr mustConvert(Expr expr, CType dest) => mustConvert(expr, dest, (CONVERTTYPE)0);

        private Expr mustConvert(Expr expr, CType dest, CONVERTTYPE flags) => mustConvertCore(expr, dest, flags);

        //        public bool canCast(Expr expr, CType dest)
        //        {
        //            EXPRCLASS destExpr = GetExprFactory().MakeClass(dest);
        //            return BindExplicitConversion(expr, expr.type, destExpr, dest, 0);
        //        }

        // performs an explicit conversion if its possible. otherwise displays an error.
        private Expr mustCastCore(Expr expr, CType dest, CONVERTTYPE flags)
        {
            Debug.Assert(!(expr is ExprMemberGroup));
            Debug.Assert(dest != null);

            SemanticChecker.CheckForStaticClass(null, dest, ErrorCode.ERR_ConvertToStaticClass);
            if (BindExplicitConversion(expr, expr.Type, dest, out Expr exprResult, flags))
            {
                // Conversion works.
                checkUnsafe(expr.Type); // added to the binder so we don't bind to pointer ops
                checkUnsafe(dest); // added to the binder so we don't bind to pointer ops
                return exprResult;
            }

            // For certain situations, try to give a better error.
            Expr exprConst = expr.GetConst();
            bool simpleConstToSimpleDestination = exprConst != null && expr.Type.isSimpleOrEnum() &&
                dest.isSimpleOrEnum();

            if (simpleConstToSimpleDestination)
            {
                FUNDTYPE exprType = expr.Type.fundType();
                if (exprType == FUNDTYPE.FT_STRUCT)
                {
                    // We have a constant decimal that is out of range of the destination type.
                    // In both checked and unchecked contexts we issue an error. No need to recheck conversion in unchecked context.
                    // Decimal is a SimpleType represented in a FT_STRUCT
                    throw ErrorContext.Error(
                        ErrorCode.ERR_ConstOutOfRange,
                        ((ExprConstant)exprConst).Val.DecimalVal.ToString(CultureInfo.InvariantCulture), dest);
                }

                if (Context.Checked)
                {
                    // check if we failed because we are in checked mode...
                    if (!canExplicitConversionBeBoundInUncheckedContext(expr, expr.Type, dest, flags | CONVERTTYPE.NOUDC))
                    {
                        throw CantConvert(expr, dest);
                    }

                    // Failed because value was out of range. Report nifty error message.
                    string value;
                    if (exprType <= FUNDTYPE.FT_LASTINTEGRAL)
                    {
                        value = expr.Type.isUnsigned()
                            ? ((ulong)((ExprConstant)exprConst).Int64Value).ToString(CultureInfo.InvariantCulture)
                            : ((ExprConstant)exprConst).Int64Value.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        Debug.Assert(exprType <= FUNDTYPE.FT_LASTNUMERIC, "Error in constant conversion logic!");
                        value = ((ExprConstant)exprConst).Val.DoubleVal.ToString(CultureInfo.InvariantCulture);
                    }

                    throw ErrorContext.Error(ErrorCode.ERR_ConstOutOfRangeChecked, value, dest);
                }
            }

            if (expr.Type is NullType && dest.fundType() != FUNDTYPE.FT_REF)
            {
                throw ErrorContext.Error(ErrorCode.ERR_ValueCantBeNull, dest);
            }

            throw CantConvert(expr, dest);
        }

        private RuntimeBinderException CantConvert(Expr expr, CType dest)
        {
            // Generic "can't convert" error.
            Debug.Assert(expr.Type != null);
            return ErrorContext.Error(ErrorCode.ERR_NoExplicitConv, new ErrArg(expr.Type, ErrArgFlags.Unique), new ErrArg(dest, ErrArgFlags.Unique));
        }

        public Expr mustCast(Expr expr, CType dest) => mustCast(expr, dest, 0);

        public Expr mustCast(Expr expr, CType dest, CONVERTTYPE flags) => mustCastCore(expr, dest, flags);

        private Expr mustCastInUncheckedContext(Expr expr, CType dest, CONVERTTYPE flags)
        {
            BindingContext ctx = new BindingContext(Context);
            return (new ExpressionBinder(ctx)).mustCast(expr, dest, flags);
        }

        // returns true if an explicit conversion exists from source type to dest type. flags is an optional parameter.
        private bool canCast(CType src, CType dest, CONVERTTYPE flags) => BindExplicitConversion(null, src, dest, flags);

        private bool BindImplicitConversion(Expr pSourceExpr, CType pSourceType, CType destinationType, CONVERTTYPE flags)
        {
            ImplicitConversion binder = new ImplicitConversion(this, pSourceExpr, pSourceType, destinationType, false, flags);
            return binder.Bind();
        }
        private bool BindImplicitConversion(Expr pSourceExpr, CType pSourceType, CType destinationType, out Expr ppDestinationExpr, CONVERTTYPE flags)
        {
            ImplicitConversion binder = new ImplicitConversion(this, pSourceExpr, pSourceType, destinationType, true, flags);
            bool result = binder.Bind();
            ppDestinationExpr = binder.ExprDest;
            return result;
        }

        private bool BindImplicitConversion(Expr pSourceExpr, CType pSourceType, CType destinationType, bool needsExprDest, out Expr ppDestinationExpr, CONVERTTYPE flags)
        {
            ImplicitConversion binder = new ImplicitConversion(this, pSourceExpr, pSourceType, destinationType, needsExprDest, flags);
            bool result = binder.Bind();
            ppDestinationExpr = needsExprDest ? binder.ExprDest : null;
            return result;
        }

        private bool BindExplicitConversion(Expr pSourceExpr, CType pSourceType, CType destinationType, bool needsExprDest, out Expr ppDestinationExpr, CONVERTTYPE flags)
        {
            ExplicitConversion binder = new ExplicitConversion(this, pSourceExpr, pSourceType, destinationType, needsExprDest, flags);
            bool result = binder.Bind();
            ppDestinationExpr = needsExprDest ? binder.ExprDest : null;
            return result;
        }

        private bool BindExplicitConversion(Expr pSourceExpr, CType pSourceType, CType destinationType, out Expr ppDestinationExpr, CONVERTTYPE flags)
        {
            ExplicitConversion binder = new ExplicitConversion(this, pSourceExpr, pSourceType, destinationType, true, flags);
            bool result = binder.Bind();
            ppDestinationExpr = binder.ExprDest;
            return result;
        }

        private bool BindExplicitConversion(Expr pSourceExpr, CType pSourceType, CType destinationType, CONVERTTYPE flags)
        {
            ExplicitConversion binder = new ExplicitConversion(this, pSourceExpr, pSourceType, destinationType, false, flags);
            return binder.Bind();
        }

        /***************************************************************************************************
            Binds a user-defined conversion. The parameters to this procedure are the same as
            BindImplicitConversion, except the last: implicitOnly - only consider implicit conversions.

            This is a helper routine for BindImplicitConversion and BindExplicitConversion.

            It's non trivial to get this right in the presence of generics. e.g.

                class D<B,C> {
                    static implicit operator B (D<B,C> x) { ... }
                }

                class E<A> : D<List<A>, A> { }

                E<int> x;
                List<int> y = x;

            The locals below would have the following values:

                typeList->sym: D<List<A>, A>
                typeCur: E<int>
                typeConv = subst(typeList->sym, typeCur)
                         = subst(D<List<!0>, !0>, <int>) = D<List<int>, int>

                retType: B
                typeTo = subst(retType, typeConv)
                       = subst(!0, <List<int>, int>) = List<int>
                params->Item(0): D<B,C>
                typeFrom = subst(params->Item(0), typeConv)
                         = subst(D<!0,!1>, <List<int>, int>)
                         = D<List<int>, int> = typeConv

            For lifting over nullable:
            * Look in the most base types for the conversions (not in System.Nullable).
            * We only lift if both the source type and destination type are nullable and the input
              or output of the conversion is not a nullable.
            * When we lift we count the number of types (0, 1, 2) that need to be lifted.
              A conversion that needs fewer lifts is better than one that requires more (if the lifted
              forms have identical signatures).
        ***************************************************************************************************/
        private bool bindUserDefinedConversion(Expr exprSrc, CType typeSrc, CType typeDst, bool needExprDest, out Expr pexprDst, bool fImplicitOnly)
        {
            pexprDst = null;
            Debug.Assert(exprSrc == null || exprSrc.Type == typeSrc);

            // If either type is an interface we should never employ a UD conversion.
            if (typeSrc == null || typeDst == null || typeSrc.isInterfaceType() || typeDst.isInterfaceType())
                return false;
            CType typeSrcBase = typeSrc.StripNubs();
            CType typeDstBase = typeDst.StripNubs();

            // Whether we should consider lifted (over nullable) operators. This is
            // true exactly when both the source and destination types are nullable.
            bool fLiftSrc = typeSrcBase != typeSrc;
            bool fLiftDst = typeDstBase != typeDst;
            bool fDstHasNull = fLiftDst || typeDst.IsRefType() || typeDst is PointerType;
            AggregateType[] rgats = new AggregateType[2];
            int cats = 0;

            // This will be true if it must be the case that either the operator is implicit
            // or the from-type of the operator must be the same as the source type.
            // This is true when the source type is a type variable.
            bool fImplicitOrExactSrc = fImplicitOnly;

            // This flag will be true if we should ignore the IntPtr/UIntPtr -> int/uint conversion
            // in favor of the IntPtr/UIntPtr -> long/ulong conversion.
            bool fIntPtrOverride2 = false;

            // Get the list of operators from the source.
            if (typeSrcBase is AggregateType atSrcBase && atSrcBase.getAggregate().HasConversion(GetSymbolLoader()))
            {
                rgats[cats++] = atSrcBase;
                fIntPtrOverride2 = atSrcBase.isPredefType(PredefinedType.PT_INTPTR) || atSrcBase.isPredefType(PredefinedType.PT_UINTPTR);
            }

            // Get the list of operators from the destination.
            if (typeDstBase is AggregateType atDstBase)
            {
                if (typeDstBase.getAggregate().HasConversion(GetSymbolLoader()))
                {
                    rgats[cats++] = atDstBase;
                }

                if (fIntPtrOverride2 && !typeDstBase.isPredefType(PredefinedType.PT_LONG) && !typeDstBase.isPredefType(PredefinedType.PT_ULONG))
                {
                    fIntPtrOverride2 = false;
                }
            }
            else
            {
                fIntPtrOverride2 = false;
            }

            // If there are no user defined conversions, we're done.
            if (cats == 0)
                return false;

            List<UdConvInfo> prguci = new List<UdConvInfo>();
            CType typeBestSrc = null;
            CType typeBestDst = null;
            bool fBestSrcExact = false;
            bool fBestDstExact = false;
            int iuciBestSrc = -1;
            int iuciBestDst = -1;

            CType typeFrom;
            CType typeTo;

            // In the first pass if we find types that are non-comparable, keep one of the types and keep going.
            for (int iats = 0; iats < cats; iats++)
            {
                for (AggregateType atsCur = rgats[iats]; atsCur != null && atsCur.getAggregate().HasConversion(GetSymbolLoader()); atsCur = atsCur.GetBaseClass())
                {
                    AggregateSymbol aggCur = atsCur.getAggregate();

                    // We need to replicate behavior that allows non-standard conversions with these guys.
                    PredefinedType aggPredefType = aggCur.GetPredefType();
                    bool fIntPtrStandard = (aggCur.IsPredefined() &&
                            (aggPredefType == PredefinedType.PT_INTPTR ||
                             aggPredefType == PredefinedType.PT_UINTPTR ||
                             aggPredefType == PredefinedType.PT_DECIMAL));

                    for (MethodSymbol convCur = aggCur.GetFirstUDConversion(); convCur != null; convCur = convCur.ConvNext())
                    {
                        if (convCur.Params.Count != 1)
                        {
                            // If we have a user-defined conversion that
                            // does not specify the correct number of parameters, we may
                            // still get here. At this point, we don't want to consider
                            // the broken conversion, so we simply skip it and move on.
                            continue;
                        }
                        Debug.Assert(convCur.getClass() == aggCur);

                        if (fImplicitOnly && !convCur.isImplicit())
                            continue;

                        // Get the substituted src and dst types.
                        typeFrom = GetTypes().SubstType(convCur.Params[0], atsCur);
                        typeTo = GetTypes().SubstType(convCur.RetType, atsCur);

                        bool fNeedImplicit = fImplicitOnly;

                        // If fImplicitOrExactSrc is set then it must be the case that either the conversion
                        // is implicit or the from-type must be the src type (modulo nullables).
                        if (fImplicitOrExactSrc && !fNeedImplicit && typeFrom.StripNubs() != typeSrcBase)
                        {
                            if (!convCur.isImplicit())
                                continue;
                            fNeedImplicit = true;
                        }

                        {
                            FUNDTYPE ftFrom;
                            FUNDTYPE ftTo;

                            if ((ftTo = typeTo.fundType()) <= FUNDTYPE.FT_LASTNUMERIC && ftTo > FUNDTYPE.FT_NONE &&
                                (ftFrom = typeFrom.fundType()) <= FUNDTYPE.FT_LASTNUMERIC && ftFrom > FUNDTYPE.FT_NONE)
                            {
                                continue;
                            }
                        }

                        // Ignore the IntPtr/UIntPtr -> int/uint conversion in favor of
                        // the IntPtr/UIntPtr -> long/ulong conversion.
                        if (fIntPtrOverride2 && (typeTo.isPredefType(PredefinedType.PT_INT) || typeTo.isPredefType(PredefinedType.PT_UINT)))
                            continue;

                        // Lift the conversion if needed.
                        if (fLiftSrc && (fDstHasNull || !fNeedImplicit) && typeFrom.IsNonNubValType())
                            typeFrom = GetTypes().GetNullable(typeFrom);
                        if (fLiftDst && typeTo.IsNonNubValType())
                            typeTo = GetTypes().GetNullable(typeTo);

                        // Check for applicability.
                        bool fFromImplicit = exprSrc != null ? canConvert(exprSrc, typeFrom, CONVERTTYPE.STANDARDANDNOUDC) : canConvert(typeSrc, typeFrom, CONVERTTYPE.STANDARDANDNOUDC);
                        if (!fFromImplicit && (fNeedImplicit ||
                                               !canConvert(typeFrom, typeSrc, CONVERTTYPE.STANDARDANDNOUDC) &&
                                               // We allow IntPtr and UIntPtr to use non-standard explicit casts as long as they don't involve pointer types.
                                               // This is because the framework uses it and RTM allowed it.
                                               (!fIntPtrStandard || typeSrc is PointerType || typeFrom is PointerType || !canCast(typeSrc, typeFrom, CONVERTTYPE.NOUDC))))
                        {
                            continue;
                        }
                        bool fToImplicit = canConvert(typeTo, typeDst, CONVERTTYPE.STANDARDANDNOUDC);
                        if (!fToImplicit && (fNeedImplicit ||
                                             !canConvert(typeDst, typeTo, CONVERTTYPE.STANDARDANDNOUDC) &&
                                             // We allow IntPtr and UIntPtr to use non-standard explicit casts as long as they don't involve pointer types.
                                             // This is because the framework uses it and RTM allowed it.
                                             (!fIntPtrStandard || typeDst is PointerType || typeTo is PointerType || !canCast(typeTo, typeDst, CONVERTTYPE.NOUDC))))
                        {
                            continue;
                        }
                        if (isConvInTable(prguci, convCur, atsCur, fFromImplicit, fToImplicit))
                        {
                            // VSWhidbey 579325: duplicate conversions in the convInfo table cause false ambiguity:
                            // If a user defined implicit conversion exists in a generic base type,
                            // it is possible to reach that conversion from both Src and Dst types. In the following
                            // example, the same implicit conversion is found from both src and dst types.
                            //
                            //    class A<T> { public static implicit operator B(A<T> a) { return a; } }
                            //    class B : A<C> {}
                            //    class C { void M () { B b = new A<C>(); } }
                            //
                            // Note that, this UD implicit conversion is legal. C#20.1.11:
                            //    "If a pre-defined explicit conversion (Section 6.2) exists from type S to type T,
                            //     any user-defined explicit conversions from S to T are ignored. However,
                            //     user-defined implicit conversions from S to T are still considered."
                            // Also notice that this check is O(n2) in found UD conversions.
                            continue;
                        }

                        // The conversion is applicable so it affects the best types.

                        prguci.Add(new UdConvInfo());
                        prguci[prguci.Count - 1].mwt = new MethWithType();
                        prguci[prguci.Count - 1].mwt.Set(convCur, atsCur);
                        prguci[prguci.Count - 1].fSrcImplicit = fFromImplicit;
                        prguci[prguci.Count - 1].fDstImplicit = fToImplicit;

                        if (!fBestSrcExact)
                        {
                            if (typeFrom == typeSrc)
                            {
                                Debug.Assert((typeBestSrc == null) == (typeBestDst == null)); // If typeBestSrc is null then typeBestDst should be null.
                                Debug.Assert(fFromImplicit);
                                typeBestSrc = typeFrom;
                                iuciBestSrc = prguci.Count - 1;
                                fBestSrcExact = true;
                            }
                            else if (typeBestSrc == null)
                            {
                                Debug.Assert(iuciBestSrc == -1);
                                typeBestSrc = typeFrom;
                                iuciBestSrc = prguci.Count - 1;
                            }
                            else if (typeBestSrc != typeFrom)
                            {
                                Debug.Assert(0 <= iuciBestSrc && iuciBestSrc < prguci.Count - 1);
                                int n = CompareSrcTypesBased(typeBestSrc, prguci[iuciBestSrc].fSrcImplicit, typeFrom, fFromImplicit);
                                if (n > 0)
                                {
                                    typeBestSrc = typeFrom;
                                    iuciBestSrc = prguci.Count - 1;
                                }
                            }
                        }

                        if (!fBestDstExact)
                        {
                            if (typeTo == typeDst)
                            {
                                Debug.Assert(fToImplicit);
                                typeBestDst = typeTo;
                                iuciBestDst = prguci.Count - 1;
                                fBestDstExact = true;
                            }
                            else if (typeBestDst == null)
                            {
                                Debug.Assert(iuciBestDst == -1);
                                typeBestDst = typeTo;
                                iuciBestDst = prguci.Count - 1;
                            }
                            else if (typeBestDst != typeTo)
                            {
                                Debug.Assert(0 <= iuciBestDst && iuciBestDst < prguci.Count - 1);
                                int n = CompareDstTypesBased(typeBestDst, prguci[iuciBestDst].fDstImplicit, typeTo, fToImplicit);
                                if (n > 0)
                                {
                                    typeBestDst = typeTo;
                                    iuciBestDst = prguci.Count - 1;
                                }
                            }
                        }
                    }
                }
            }

            Debug.Assert((typeBestSrc == null) == (typeBestDst == null));
            if (typeBestSrc == null)
            {
                Debug.Assert(iuciBestSrc == -1 && iuciBestDst == -1);
                return false;
            }

            Debug.Assert(0 <= iuciBestSrc && iuciBestSrc < prguci.Count);
            Debug.Assert(0 <= iuciBestDst && iuciBestDst < prguci.Count);

            int ctypeLiftBest = 3; // Bigger than any legal value on purpose.
            int iuciBest = -1;
            int iuciAmbig = -1;

            // In the second pass, we verify that the types we ended up with are indeed minimal and find the one valid conversion.
            for (int iuci = 0; iuci < prguci.Count; iuci++)
            {
                UdConvInfo uci = prguci[iuci];

                // Get the substituted src and dst types.
                typeFrom = GetTypes().SubstType(uci.mwt.Meth().Params[0], uci.mwt.GetType());
                typeTo = GetTypes().SubstType(uci.mwt.Meth().RetType, uci.mwt.GetType());

                int ctypeLift = 0;

                // Lift the conversion if needed.
                if (fLiftSrc && typeFrom.IsNonNubValType())
                {
                    typeFrom = GetTypes().GetNullable(typeFrom);
                    ctypeLift++;
                }
                if (fLiftDst && typeTo.IsNonNubValType())
                {
                    typeTo = GetTypes().GetNullable(typeTo);
                    ctypeLift++;
                }

                if (typeFrom == typeBestSrc && typeTo == typeBestDst)
                {
                    // Record the matching conversions.
                    if (ctypeLiftBest > ctypeLift)
                    {
                        // This one is better.
                        iuciBest = iuci;
                        iuciAmbig = -1;
                        ctypeLiftBest = ctypeLift;
                        continue;
                    }

                    if (ctypeLiftBest < ctypeLift)
                    {
                        // Current answer is better.
                        continue;
                    }

                    // Ambiguous at this lifting level. This only guarantees an error if the
                    // lifting level is zero.
                    if (iuciAmbig < 0)
                    {
                        iuciAmbig = iuci;
                        if (ctypeLift == 0)
                        {
                            // No point continuing. We have an error.
                            break;
                        }
                    }
                    continue;
                }

                Debug.Assert(typeFrom != typeBestSrc || typeTo != typeBestDst);

                // Verify that the best types are indeed best. Must NOT compare if the best type is exact.
                // This is not just an efficiency issue. With nullables there are types that are implicitly
                // convertible to each other (eg, int? and int??) and hence not distinguishable by CompareXxxTypesBase.
                if (!fBestSrcExact && typeFrom != typeBestSrc)
                {
                    int n = CompareSrcTypesBased(typeBestSrc, prguci[iuciBestSrc].fSrcImplicit, typeFrom, uci.fSrcImplicit);
                    Debug.Assert(n <= 0);
                    if (n >= 0)
                    {
                        if (needExprDest)
                        {
                            throw HandleAmbiguity(typeSrc, typeDst, prguci, iuciBestSrc, iuci);
                        }

                        return true;
                    }
                }
                if (!fBestDstExact && typeTo != typeBestDst)
                {
                    int n = CompareDstTypesBased(typeBestDst, prguci[iuciBestDst].fDstImplicit, typeTo, uci.fDstImplicit);
                    Debug.Assert(n <= 0);
                    if (n >= 0)
                    {
                        if (needExprDest)
                        {
                            throw HandleAmbiguity(typeSrc, typeDst, prguci, iuciBestSrc, iuci);
                        }

                        return true;
                    }
                }
            }

            if (!needExprDest)
                return true;

            if (iuciBest < 0)
            {
                throw HandleAmbiguity(typeSrc, typeDst, prguci, iuciBestSrc, iuciBestDst);
            }
            if (iuciAmbig >= 0)
            {
                throw HandleAmbiguity(typeSrc, typeDst, prguci, iuciBest, iuciAmbig);
            }

            MethWithInst mwiBest = new MethWithInst(prguci[iuciBest].mwt.Meth(), prguci[iuciBest].mwt.GetType(), null);

            Debug.Assert(ctypeLiftBest <= 2);

            typeFrom = GetTypes().SubstType(mwiBest.Meth().Params[0], mwiBest.GetType());
            typeTo = GetTypes().SubstType(mwiBest.Meth().RetType, mwiBest.GetType());

            Expr exprDst;
            Expr pTransformedArgument = exprSrc;

            if (ctypeLiftBest > 0 && !(typeFrom is NullableType) && fDstHasNull)
            {
                // Create the memgroup.
                ExprMemberGroup pMemGroup = ExprFactory.CreateMemGroup(null, mwiBest);

                // Need to lift over the null.
                Debug.Assert(fLiftSrc || fLiftDst);
                ExprCall call = ExprFactory.CreateCall(0, typeDst, exprSrc, pMemGroup, mwiBest);
                exprDst = call;

                // We want to bind the unlifted conversion first.
                Expr nonLiftedArg = mustCast(exprSrc, typeFrom);
                MarkAsIntermediateConversion(nonLiftedArg);
                Expr nonLiftedResult = BindUDConversionCore(nonLiftedArg, typeFrom, typeTo, typeDst, mwiBest);

                call.CastOfNonLiftedResultToLiftedType = mustCast(nonLiftedResult, typeDst);
                call.NullableCallLiftKind = NullableCallLiftKind.UserDefinedConversion;

                if (fLiftSrc)
                {
                    // If lifting of the source is required, we need to figure out the intermediate conversion
                    // from the type of the source to the type of the UD conversion parameter. Note that typeFrom
                    // is not a nullable type.
                    Expr pConversionArgument;
                    if (typeFrom != typeSrcBase)
                    {
                        // There is an intermediate conversion.
                        NullableType pConversionNubSourceType = SymbolLoader.GetTypeManager().GetNullable(typeFrom);
                        pConversionArgument = mustCast(exprSrc, pConversionNubSourceType);
                        MarkAsIntermediateConversion(pConversionArgument);
                    }
                    else
                    {
                        if (typeTo is NullableType)
                        {
                            // We need to generate a nullable value access, the conversion will be used without lifting.
                            pConversionArgument = mustCast(exprSrc, typeFrom);
                        }
                        else
                        {
                            pConversionArgument = exprSrc;
                        }
                    }

                    Debug.Assert(pConversionArgument != null);
                    ExprCall pConversionCall = ExprFactory.CreateCall(0, typeDst, pConversionArgument, pMemGroup, mwiBest);
                    pConversionCall.NullableCallLiftKind = NullableCallLiftKind.NotLiftedIntermediateConversion;
                    call.PConversions = pConversionCall;
                }
                else
                {
                    Expr pConversionCall = BindUDConversionCore(nonLiftedArg, typeFrom, typeTo, typeDst, mwiBest);
                    MarkAsIntermediateConversion(pConversionCall);
                    call.PConversions = pConversionCall;
                }
            }
            else
            {
                exprDst = BindUDConversionCore(exprSrc, typeFrom, typeTo, typeDst, mwiBest, out pTransformedArgument);
            }

            pexprDst = ExprFactory.CreateUserDefinedConversion(pTransformedArgument, exprDst, mwiBest);
            return true;
        }

        private RuntimeBinderException HandleAmbiguity(CType typeSrc, CType typeDst, List<UdConvInfo> prguci, int iuciBestSrc, int iuciBestDst)
        {
            Debug.Assert(0 <= iuciBestSrc && iuciBestSrc < prguci.Count);
            Debug.Assert(0 <= iuciBestDst && iuciBestDst < prguci.Count);
            return ErrorContext.Error(ErrorCode.ERR_AmbigUDConv, prguci[iuciBestSrc].mwt, prguci[iuciBestDst].mwt, typeSrc, typeDst);
        }

        private void MarkAsIntermediateConversion(Expr pExpr)
        {
            for (;;)
            {
                Debug.Assert(pExpr != null);
                if (pExpr is ExprCall call)
                {
                    switch (call.NullableCallLiftKind)
                    {
                        case NullableCallLiftKind.NotLifted:
                            call.NullableCallLiftKind = NullableCallLiftKind.NotLiftedIntermediateConversion;
                            break;
                        case NullableCallLiftKind.NullableConversion:
                            call.NullableCallLiftKind = NullableCallLiftKind.NullableIntermediateConversion;
                            break;
                        case NullableCallLiftKind.NullableConversionConstructor:
                            pExpr = call.OptionalArguments;
                            continue;
                    }
                }
                else if (pExpr is ExprUserDefinedConversion udc)
                {
                    pExpr = udc.UserDefinedCall;
                    continue;
                }

                return;
            }
        }

        private Expr BindUDConversionCore(Expr pFrom, CType pTypeFrom, CType pTypeTo, CType pTypeDestination, MethWithInst mwiBest)
        {
            Expr ppTransformedArgument;
            return BindUDConversionCore(pFrom, pTypeFrom, pTypeTo, pTypeDestination, mwiBest, out ppTransformedArgument);
        }

        private Expr BindUDConversionCore(Expr pFrom, CType pTypeFrom, CType pTypeTo, CType pTypeDestination, MethWithInst mwiBest, out Expr ppTransformedArgument)
        {
            Expr pTransformedArgument = mustCastCore(pFrom, pTypeFrom, CONVERTTYPE.NOUDC);
            Debug.Assert(pTransformedArgument != null);
            ExprMemberGroup pMemGroup = ExprFactory.CreateMemGroup(null, mwiBest);
            ExprCall pCall = ExprFactory.CreateCall(0, pTypeTo, pTransformedArgument, pMemGroup, mwiBest);
            Expr pCast = mustCastCore(pCall, pTypeDestination, CONVERTTYPE.NOUDC);
            Debug.Assert(pCast != null);
            ppTransformedArgument = pTransformedArgument;
            return pCast;
        }

        /*
         * Fold a constant cast. Returns true if the constant could be folded.
         */
        private ConstCastResult bindConstantCast(Expr exprSrc, CType typeDest, bool needExprDest, out Expr pexprDest, bool explicitConversion)
        {
            pexprDest = null;
            long valueInt = 0;
            double valueFlt = 0;
            FUNDTYPE ftSrc = exprSrc.Type.fundType();
            FUNDTYPE ftDest = typeDest.fundType();
            bool srcIntegral = (ftSrc <= FUNDTYPE.FT_LASTINTEGRAL);
            bool srcNumeric = (ftSrc <= FUNDTYPE.FT_LASTNUMERIC);

            ExprConstant constSrc = (ExprConstant)exprSrc.GetConst();
            Debug.Assert(constSrc != null);
            if (ftSrc == FUNDTYPE.FT_STRUCT || ftDest == FUNDTYPE.FT_STRUCT)
            {
                // Do constant folding involving decimal constants.
                Expr expr = bindDecimalConstCast(typeDest, exprSrc.Type, constSrc);

                if (expr == null)
                {
                    if (explicitConversion)
                    {
                        return ConstCastResult.CheckFailure;
                    }
                    return ConstCastResult.Failure;
                }
                if (needExprDest)
                    pexprDest = expr;
                return ConstCastResult.Success;
            }

            if (explicitConversion && Context.Checked && !isConstantInRange(constSrc, typeDest, true))
            {
                return ConstCastResult.CheckFailure;
            }

            if (!needExprDest)
            {
                return ConstCastResult.Success;
            }


            // Get the source constant value into valueInt or valueFlt.
            if (srcIntegral)
            {
                if (constSrc.Type.fundType() == FUNDTYPE.FT_U8)
                {
                    // If we're going from ulong to something, make sure we can fit.
                    if (ftDest == FUNDTYPE.FT_U8)
                    {
                        ConstVal cv = ConstVal.Get(constSrc.UInt64Value);
                        pexprDest = ExprFactory.CreateConstant(typeDest, cv);
                        return ConstCastResult.Success;
                    }
                    valueInt = (long)(constSrc.UInt64Value& 0xFFFFFFFFFFFFFFFF);
                }
                else
                {
                    valueInt = constSrc.Int64Value;
                }
            }
            else if (srcNumeric)
            {
                valueFlt = constSrc.Val.DoubleVal;
            }
            else
            {
                return ConstCastResult.Failure;
            }

            // Convert constant to the destination type, truncating if necessary.
            // valueInt or valueFlt contains the result of the conversion.
            switch (ftDest)
            {
                case FUNDTYPE.FT_I1:
                    if (!srcIntegral)
                    {
                        valueInt = (long)valueFlt;
                    }
                    valueInt = unchecked((sbyte)(valueInt & 0xFF));
                    break;
                case FUNDTYPE.FT_I2:
                    if (!srcIntegral)
                    {
                        valueInt = (long)valueFlt;
                    }
                    valueInt = unchecked((short)(valueInt & 0xFFFF));
                    break;
                case FUNDTYPE.FT_I4:
                    if (!srcIntegral)
                    {
                        valueInt = (long)valueFlt;
                    }
                    valueInt = unchecked((int)(valueInt & 0xFFFFFFFF));
                    break;
                case FUNDTYPE.FT_I8:
                    if (!srcIntegral)
                    {
                        valueInt = (long)valueFlt;
                    }
                    break;
                case FUNDTYPE.FT_U1:
                    if (!srcIntegral)
                    {
                        valueInt = (long)valueFlt;
                    }
                    valueInt = (byte)(valueInt & 0xFF);
                    break;
                case FUNDTYPE.FT_U2:
                    if (!srcIntegral)
                    {
                        valueInt = (long)valueFlt;
                    }
                    valueInt = (ushort)(valueInt & 0xFFFF);
                    break;
                case FUNDTYPE.FT_U4:
                    if (!srcIntegral)
                    {
                        valueInt = (long)valueFlt;
                    }
                    valueInt = (uint)(valueInt & 0xFFFFFFFF);
                    break;
                case FUNDTYPE.FT_U8:
                    if (!srcIntegral)
                    {
                        valueInt = (long)(ulong)valueFlt;
                        const double two63 = 2147483648.0 * 4294967296.0;
                        if (valueFlt < two63)
                        {
                            valueInt = (long)valueFlt;
                        }
                        else
                        {
                            valueInt = ((long)(valueFlt - two63)) + I64(0x8000000000000000);
                        }
                    }
                    break;
                case FUNDTYPE.FT_R4:
                case FUNDTYPE.FT_R8:
                    if (srcIntegral)
                    {
                        if (ftSrc == FUNDTYPE.FT_U8)
                        {
                            valueFlt = (double)(ulong)valueInt;
                        }
                        else
                        {
                            valueFlt = (double)valueInt;
                        }
                    }
                    if (ftDest == FUNDTYPE.FT_R4)
                    {
                        // Force to R4 precision/range.
                        float f;
                        RoundToFloat(valueFlt, out f);
                        valueFlt = f;
                    }
                    break;
                default:
                    // We got here because of LAF or Refactoring. We must have had a parser
                    // error here, because the user is not allowed to have a non-value type
                    // being cast, but we need to bind for errors anyway.
                    break;
            }

            // Create a new constant with the value in "valueInt" or "valueFlt".
            {
                ConstVal cv;
                if (ftDest == FUNDTYPE.FT_U4)
                {
                    cv = ConstVal.Get((uint)valueInt);
                }
                else if (ftDest <= FUNDTYPE.FT_LASTNONLONG)
                {
                    cv = ConstVal.Get((int)valueInt);
                }
                else if (ftDest <= FUNDTYPE.FT_LASTINTEGRAL)
                {
                    cv = ConstVal.Get(valueInt);
                }
                else
                {
                    cv = ConstVal.Get(valueFlt);
                }
                ExprConstant expr = ExprFactory.CreateConstant(typeDest, cv);
                pexprDest = expr;
            }
            return ConstCastResult.Success;
        }

        /***************************************************************************************************
            This is a helper method for bindUserDefinedConversion. "Compares" two types relative to a
            base type and indicates which is "closer" to base. fImplicit(1|2) specifies whether there is a
            standard implicit conversion from base to type(1|2). If fImplicit(1|2) is false there should
            be a standard explicit conversion from base to type(1|2). The partial ordering used is as
            follows:

            * If exactly one of fImplicit(1|2) is true then the corresponding type is closer.
            * Otherwise if there is a standard implicit conversion in neither direction or both directions
              then neither is closer.
            * Otherwise if both of fImplicit(1|2) are true:
                * If there is a standard implicit conversion from type(1|2) to type(2|1) then type(1|2)
                  is closer.
                * Otherwise neither is closer.
            * Otherwise both of fImplicit(1|2) are false and:
                * If there is a standard implicit conversion from type(1|2) to type(2|1) then type(2|1)
                  is closer.
                * Otherwise neither is closer.

            The return value is -1 if type1 is closer, +1 if type2 is closer and 0 if neither is closer.
        ***************************************************************************************************/
        private int CompareSrcTypesBased(CType type1, bool fImplicit1, CType type2, bool fImplicit2)
        {
            Debug.Assert(type1 != type2);
            if (fImplicit1 != fImplicit2)
                return fImplicit1 ? -1 : +1;
            bool fCon1 = canConvert(type1, type2, CONVERTTYPE.NOUDC);
            bool fCon2 = canConvert(type2, type1, CONVERTTYPE.NOUDC);
            if (fCon1 == fCon2)
                return 0;
            return (fImplicit1 == fCon1) ? -1 : +1;
        }

        /***************************************************************************************************
            This is a helper method for bindUserDefinedConversion. "Compares" two types relative to a
            base type and indicates which is "closer" to base. fImplicit(1|2) specifies whether there is a
            standard implicit conversion from type(1|2) to base. If fImplicit(1|2) is false there should
            be a standard explicit conversion from type(1|2) to base. The partial ordering used is as
            follows:

            * If exactly one of fImplicit(1|2) is true then the corresponding type is closer.
            * Otherwise if there is a standard implicit conversion in neither direction or both directions
              then neither is closer.
            * Otherwise if both of fImplicit(1|2) are true:
                * If there is a standard implicit conversion from type(1|2) to type(2|1) then type(2|1)
                  is closer.
                * Otherwise neither is closer.
            * Otherwise both of fImplicit(1|2) are false and:
                * If there is a standard implicit conversion from type(1|2) to type(2|1) then type(1|2)
                  is closer.
                * Otherwise neither is closer.

            The return value is -1 if type1 is closer, +1 if type2 is closer and 0 if neither is closer.
        ***************************************************************************************************/
        private int CompareDstTypesBased(CType type1, bool fImplicit1, CType type2, bool fImplicit2)
        {
            Debug.Assert(type1 != type2);
            if (fImplicit1 != fImplicit2)
                return fImplicit1 ? -1 : +1;
            bool fCon1 = canConvert(type1, type2, CONVERTTYPE.NOUDC);
            bool fCon2 = canConvert(type2, type1, CONVERTTYPE.NOUDC);
            if (fCon1 == fCon2)
                return 0;
            return (fImplicit1 == fCon1) ? +1 : -1;
        }
        /*
         * Bind a constant cast to or from decimal. Return null if cast can't be done.
         */
        private Expr bindDecimalConstCast(CType destType, CType srcType, ExprConstant src)
        {
            CType typeDecimal = SymbolLoader.GetPredefindType(PredefinedType.PT_DECIMAL);
            ConstVal cv;

            if (typeDecimal == null)
                return null;

            if (destType == typeDecimal)
            {
                // Casting to decimal.

                FUNDTYPE ftSrc = srcType.fundType();
                decimal result;

                switch (ftSrc)
                {
                    case FUNDTYPE.FT_I1:
                    case FUNDTYPE.FT_I2:
                    case FUNDTYPE.FT_I4:
                        result = Convert.ToDecimal(src.Val.Int32Val);
                        break;
                    case FUNDTYPE.FT_U1:
                    case FUNDTYPE.FT_U2:
                    case FUNDTYPE.FT_U4:
                        result = Convert.ToDecimal(src.Val.UInt32Val);
                        break;
                    case FUNDTYPE.FT_R4:
                        result = Convert.ToDecimal((float)src.Val.DoubleVal);
                        break;
                    case FUNDTYPE.FT_R8:
                        result = Convert.ToDecimal(src.Val.DoubleVal);
                        break;
                    case FUNDTYPE.FT_U8:
                        result = Convert.ToDecimal((ulong)src.Val.Int64Val);
                        break;
                    case FUNDTYPE.FT_I8:
                        result = Convert.ToDecimal(src.Val.Int64Val);
                        break;
                    default:
                        return null;  // Not supported cast.
                }

                cv = ConstVal.Get(result);
                ExprConstant exprConst = ExprFactory.CreateConstant(typeDecimal, cv);

                return exprConst;
            }

            if (srcType == typeDecimal)
            {
                // Casting from decimal
                decimal decTrunc = 0;

                FUNDTYPE ftDest = destType.fundType();
                try
                {
                    if (ftDest != FUNDTYPE.FT_R4 && ftDest != FUNDTYPE.FT_R8)
                    {
                        decTrunc = decimal.Truncate(src.Val.DecimalVal);
                    }
                    switch (ftDest)
                    {
                        case FUNDTYPE.FT_I1:
                            cv = ConstVal.Get(Convert.ToSByte(decTrunc));
                            break;
                        case FUNDTYPE.FT_U1:
                            cv = ConstVal.Get((uint)Convert.ToByte(decTrunc));
                            break;
                        case FUNDTYPE.FT_I2:
                            cv = ConstVal.Get(Convert.ToInt16(decTrunc));
                            break;
                        case FUNDTYPE.FT_U2:
                            cv = ConstVal.Get((uint)Convert.ToUInt16(decTrunc));
                            break;
                        case FUNDTYPE.FT_I4:
                            cv = ConstVal.Get(Convert.ToInt32(decTrunc));
                            break;
                        case FUNDTYPE.FT_U4:
                            cv = ConstVal.Get(Convert.ToUInt32(decTrunc));
                            break;
                        case FUNDTYPE.FT_I8:
                            cv = ConstVal.Get(Convert.ToInt64(decTrunc));
                            break;
                        case FUNDTYPE.FT_U8:
                            cv = ConstVal.Get(Convert.ToUInt64(decTrunc));
                            break;
                        case FUNDTYPE.FT_R4:
                            cv = ConstVal.Get(Convert.ToSingle(src.Val.DecimalVal));
                            break;
                        case FUNDTYPE.FT_R8:
                            cv = ConstVal.Get(Convert.ToDouble(src.Val.DecimalVal));
                            break;
                        default:
                            return null; // Not supported cast.
                    }
                }
                catch (OverflowException)
                {
                    return null;
                }
                ExprConstant exprConst = ExprFactory.CreateConstant(destType, cv);
                // Create the cast that was the original tree for this thing.
                return exprConst;
            }
            return null;
        }

        private bool canExplicitConversionBeBoundInUncheckedContext(Expr exprSrc, CType typeSrc, CType typeDest, CONVERTTYPE flags)
        {
            BindingContext ctx = new BindingContext(Context);
            Debug.Assert(typeDest != null);
            return (new ExpressionBinder(ctx)).BindExplicitConversion(exprSrc, typeSrc, typeDest, flags);
        }
    }

    internal static class ListExtensions
    {
        public static bool IsEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }
        public static T Head<T>(this List<T> list)
        {
            return list[0];
        }
        public static List<T> Tail<T>(this List<T> list)
        {
            T[] array = new T[list.Count];
            list.CopyTo(array, 0);
            List<T> newList = new List<T>(array);
            newList.RemoveAt(0);
            return newList;
        }
    }
}
