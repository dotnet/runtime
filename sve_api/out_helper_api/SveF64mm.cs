// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Numerics;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM SVE hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class SveF64mm : AdvSimd
    {
        internal SveF64mm() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        ///  ConcatenateEvenInt128FromTwoInputs : Concatenate even quadwords from two inputs

        /// <summary>
        /// svint8_t svuzp1q[_s8](svint8_t op1, svint8_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<sbyte> ConcatenateEvenInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint16_t svuzp1q[_s16](svint16_t op1, svint16_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<short> ConcatenateEvenInt128FromTwoInputs(Vector<short> left, Vector<short> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint32_t svuzp1q[_s32](svint32_t op1, svint32_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<int> ConcatenateEvenInt128FromTwoInputs(Vector<int> left, Vector<int> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint64_t svuzp1q[_s64](svint64_t op1, svint64_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<long> ConcatenateEvenInt128FromTwoInputs(Vector<long> left, Vector<long> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint8_t svuzp1q[_u8](svuint8_t op1, svuint8_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<byte> ConcatenateEvenInt128FromTwoInputs(Vector<byte> left, Vector<byte> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint16_t svuzp1q[_u16](svuint16_t op1, svuint16_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<ushort> ConcatenateEvenInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint32_t svuzp1q[_u32](svuint32_t op1, svuint32_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<uint> ConcatenateEvenInt128FromTwoInputs(Vector<uint> left, Vector<uint> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint64_t svuzp1q[_u64](svuint64_t op1, svuint64_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<ulong> ConcatenateEvenInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat32_t svuzp1q[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<float> ConcatenateEvenInt128FromTwoInputs(Vector<float> left, Vector<float> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat64_t svuzp1q[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<double> ConcatenateEvenInt128FromTwoInputs(Vector<double> left, Vector<double> right) => ConcatenateEvenInt128FromTwoInputs(left, right);


        ///  ConcatenateOddInt128FromTwoInputs : Concatenate odd quadwords from two inputs

        /// <summary>
        /// svint8_t svuzp2q[_s8](svint8_t op1, svint8_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<sbyte> ConcatenateOddInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint16_t svuzp2q[_s16](svint16_t op1, svint16_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<short> ConcatenateOddInt128FromTwoInputs(Vector<short> left, Vector<short> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint32_t svuzp2q[_s32](svint32_t op1, svint32_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<int> ConcatenateOddInt128FromTwoInputs(Vector<int> left, Vector<int> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint64_t svuzp2q[_s64](svint64_t op1, svint64_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<long> ConcatenateOddInt128FromTwoInputs(Vector<long> left, Vector<long> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint8_t svuzp2q[_u8](svuint8_t op1, svuint8_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<byte> ConcatenateOddInt128FromTwoInputs(Vector<byte> left, Vector<byte> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint16_t svuzp2q[_u16](svuint16_t op1, svuint16_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<ushort> ConcatenateOddInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint32_t svuzp2q[_u32](svuint32_t op1, svuint32_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<uint> ConcatenateOddInt128FromTwoInputs(Vector<uint> left, Vector<uint> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint64_t svuzp2q[_u64](svuint64_t op1, svuint64_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<ulong> ConcatenateOddInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat32_t svuzp2q[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<float> ConcatenateOddInt128FromTwoInputs(Vector<float> left, Vector<float> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat64_t svuzp2q[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  UZP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<double> ConcatenateOddInt128FromTwoInputs(Vector<double> left, Vector<double> right) => ConcatenateOddInt128FromTwoInputs(left, right);


        ///  InterleaveEvenInt128FromTwoInputs : Interleave even quadwords from two inputs

        /// <summary>
        /// svint8_t svtrn1q[_s8](svint8_t op1, svint8_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_P1, REG_P3, REG_P4, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<sbyte> InterleaveEvenInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint16_t svtrn1q[_s16](svint16_t op1, svint16_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_P1, REG_P3, REG_P4, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<short> InterleaveEvenInt128FromTwoInputs(Vector<short> left, Vector<short> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint32_t svtrn1q[_s32](svint32_t op1, svint32_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_P1, REG_P3, REG_P4, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<int> InterleaveEvenInt128FromTwoInputs(Vector<int> left, Vector<int> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint64_t svtrn1q[_s64](svint64_t op1, svint64_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_P1, REG_P3, REG_P4, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<long> InterleaveEvenInt128FromTwoInputs(Vector<long> left, Vector<long> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint8_t svtrn1q[_u8](svuint8_t op1, svuint8_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_P1, REG_P3, REG_P4, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<byte> InterleaveEvenInt128FromTwoInputs(Vector<byte> left, Vector<byte> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint16_t svtrn1q[_u16](svuint16_t op1, svuint16_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_P1, REG_P3, REG_P4, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<ushort> InterleaveEvenInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint32_t svtrn1q[_u32](svuint32_t op1, svuint32_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_P1, REG_P3, REG_P4, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<uint> InterleaveEvenInt128FromTwoInputs(Vector<uint> left, Vector<uint> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint64_t svtrn1q[_u64](svuint64_t op1, svuint64_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_P1, REG_P3, REG_P4, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<ulong> InterleaveEvenInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat32_t svtrn1q[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_P1, REG_P3, REG_P4, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<float> InterleaveEvenInt128FromTwoInputs(Vector<float> left, Vector<float> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat64_t svtrn1q[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_P1, REG_P3, REG_P4, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<double> InterleaveEvenInt128FromTwoInputs(Vector<double> left, Vector<double> right) => InterleaveEvenInt128FromTwoInputs(left, right);


        ///  InterleaveInt128FromHighHalvesOfTwoInputs : Interleave quadwords from high halves of two inputs

        /// <summary>
        /// svint8_t svzip2q[_s8](svint8_t op1, svint8_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V30, REG_V31, REG_V0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V1, REG_V2, REG_V3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<sbyte> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<sbyte> left, Vector<sbyte> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svint16_t svzip2q[_s16](svint16_t op1, svint16_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V30, REG_V31, REG_V0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V1, REG_V2, REG_V3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<short> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<short> left, Vector<short> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svint32_t svzip2q[_s32](svint32_t op1, svint32_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V30, REG_V31, REG_V0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V1, REG_V2, REG_V3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<int> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<int> left, Vector<int> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svint64_t svzip2q[_s64](svint64_t op1, svint64_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V30, REG_V31, REG_V0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V1, REG_V2, REG_V3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<long> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<long> left, Vector<long> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint8_t svzip2q[_u8](svuint8_t op1, svuint8_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V30, REG_V31, REG_V0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V1, REG_V2, REG_V3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<byte> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<byte> left, Vector<byte> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint16_t svzip2q[_u16](svuint16_t op1, svuint16_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V30, REG_V31, REG_V0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V1, REG_V2, REG_V3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<ushort> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<ushort> left, Vector<ushort> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint32_t svzip2q[_u32](svuint32_t op1, svuint32_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V30, REG_V31, REG_V0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V1, REG_V2, REG_V3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<uint> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<uint> left, Vector<uint> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint64_t svzip2q[_u64](svuint64_t op1, svuint64_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V30, REG_V31, REG_V0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V1, REG_V2, REG_V3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<ulong> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<ulong> left, Vector<ulong> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svfloat32_t svzip2q[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V30, REG_V31, REG_V0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V1, REG_V2, REG_V3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<float> left, Vector<float> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svfloat64_t svzip2q[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V30, REG_V31, REG_V0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V1, REG_V2, REG_V3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<double> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<double> left, Vector<double> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);


        ///  InterleaveInt128FromLowHalvesOfTwoInputs : Interleave quadwords from low halves of two inputs

        /// <summary>
        /// svint8_t svzip1q[_s8](svint8_t op1, svint8_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V24, REG_V25, REG_V26, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V27, REG_V28, REG_V29, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<sbyte> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<sbyte> left, Vector<sbyte> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svint16_t svzip1q[_s16](svint16_t op1, svint16_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V24, REG_V25, REG_V26, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V27, REG_V28, REG_V29, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<short> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<short> left, Vector<short> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svint32_t svzip1q[_s32](svint32_t op1, svint32_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V24, REG_V25, REG_V26, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V27, REG_V28, REG_V29, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<int> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<int> left, Vector<int> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svint64_t svzip1q[_s64](svint64_t op1, svint64_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V24, REG_V25, REG_V26, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V27, REG_V28, REG_V29, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<long> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<long> left, Vector<long> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint8_t svzip1q[_u8](svuint8_t op1, svuint8_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V24, REG_V25, REG_V26, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V27, REG_V28, REG_V29, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<byte> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<byte> left, Vector<byte> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint16_t svzip1q[_u16](svuint16_t op1, svuint16_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V24, REG_V25, REG_V26, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V27, REG_V28, REG_V29, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<ushort> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<ushort> left, Vector<ushort> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint32_t svzip1q[_u32](svuint32_t op1, svuint32_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V24, REG_V25, REG_V26, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V27, REG_V28, REG_V29, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<uint> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<uint> left, Vector<uint> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint64_t svzip1q[_u64](svuint64_t op1, svuint64_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V24, REG_V25, REG_V26, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V27, REG_V28, REG_V29, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<ulong> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<ulong> left, Vector<ulong> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svfloat32_t svzip1q[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V24, REG_V25, REG_V26, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V27, REG_V28, REG_V29, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<float> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<float> left, Vector<float> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svfloat64_t svzip1q[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V24, REG_V25, REG_V26, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V27, REG_V28, REG_V29, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  ZIP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<double> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<double> left, Vector<double> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);


        ///  InterleaveOddInt128FromTwoInputs : Interleave odd quadwords from two inputs

        /// <summary>
        /// svint8_t svtrn2q[_s8](svint8_t op1, svint8_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_P5, REG_P2, REG_P7, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<sbyte> InterleaveOddInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint16_t svtrn2q[_s16](svint16_t op1, svint16_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_P5, REG_P2, REG_P7, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<short> InterleaveOddInt128FromTwoInputs(Vector<short> left, Vector<short> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint32_t svtrn2q[_s32](svint32_t op1, svint32_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_P5, REG_P2, REG_P7, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<int> InterleaveOddInt128FromTwoInputs(Vector<int> left, Vector<int> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint64_t svtrn2q[_s64](svint64_t op1, svint64_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_P5, REG_P2, REG_P7, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<long> InterleaveOddInt128FromTwoInputs(Vector<long> left, Vector<long> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint8_t svtrn2q[_u8](svuint8_t op1, svuint8_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_P5, REG_P2, REG_P7, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<byte> InterleaveOddInt128FromTwoInputs(Vector<byte> left, Vector<byte> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint16_t svtrn2q[_u16](svuint16_t op1, svuint16_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_P5, REG_P2, REG_P7, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<ushort> InterleaveOddInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint32_t svtrn2q[_u32](svuint32_t op1, svuint32_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_P5, REG_P2, REG_P7, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<uint> InterleaveOddInt128FromTwoInputs(Vector<uint> left, Vector<uint> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint64_t svtrn2q[_u64](svuint64_t op1, svuint64_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_P5, REG_P2, REG_P7, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<ulong> InterleaveOddInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat32_t svtrn2q[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_P5, REG_P2, REG_P7, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> InterleaveOddInt128FromTwoInputs(Vector<float> left, Vector<float> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat64_t svtrn2q[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A  TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B  TRN2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A  TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///        theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_P5, REG_P2, REG_P7, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<double> InterleaveOddInt128FromTwoInputs(Vector<double> left, Vector<double> right) => InterleaveOddInt128FromTwoInputs(left, right);


        ///  LoadVector256AndReplicateToVector : Load and replicate 256 bits of data

        /// <summary>
        /// svint8_t svld1ro[_s8](svbool_t pg, const int8_t *base)
        ///   LD1ROB Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LD1ROB Zresult.B, Pg/Z, [Xarray, #index]
        ///   LD1ROB Zresult.B, Pg/Z, [Xbase, #0]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IO_3A  LD1ROB {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_ld1rob, EA_SCALABLE, REG_V0, REG_P1, REG_R2, 0, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_IP_4A  LD1ROB {<Zt>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]
        ///        theEmitter->emitIns_R_R_R_R(INS_sve_ld1rob, EA_SCALABLE, REG_V0, REG_P1, REG_R3, REG_R2, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector256AndReplicateToVector(Vector<sbyte> mask, sbyte* address) => LoadVector256AndReplicateToVector(mask, address);

        /// <summary>
        /// svint16_t svld1ro[_s16](svbool_t pg, const int16_t *base)
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, #index * 2]
        ///   LD1ROH Zresult.H, Pg/Z, [Xbase, #0]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IO_3A  LD1ROH {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_ld1roh, EA_SCALABLE, REG_V8, REG_P3, REG_R1, -256, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IP_4A  LD1ROH {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        ///        theEmitter->emitIns_R_R_R_R(INS_sve_ld1roh, EA_SCALABLE, REG_V4, REG_P3, REG_R2, REG_R1, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe Vector<short> LoadVector256AndReplicateToVector(Vector<short> mask, short* address) => LoadVector256AndReplicateToVector(mask, address);

        /// <summary>
        /// svint32_t svld1ro[_s32](svbool_t pg, const int32_t *base)
        ///   LD1ROW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1ROW Zresult.S, Pg/Z, [Xarray, #index * 4]
        ///   LD1ROW Zresult.S, Pg/Z, [Xbase, #0]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IO_3A  LD1ROW {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_ld1row, EA_SCALABLE, REG_V3, REG_P4, REG_R0, 224, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_IP_4A  LD1ROW {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]
        ///        theEmitter->emitIns_R_R_R_R(INS_sve_ld1row, EA_SCALABLE, REG_V1, REG_P3, REG_R2, REG_R4, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe Vector<int> LoadVector256AndReplicateToVector(Vector<int> mask, int* address) => LoadVector256AndReplicateToVector(mask, address);

        /// <summary>
        /// svint64_t svld1ro[_s64](svbool_t pg, const int64_t *base)
        ///   LD1ROD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1ROD Zresult.D, Pg/Z, [Xarray, #index * 8]
        ///   LD1ROD Zresult.D, Pg/Z, [Xbase, #0]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IO_3A  LD1ROD {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_ld1rod, EA_SCALABLE, REG_V4, REG_P5, REG_R6, -32, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_IP_4A  LD1ROD {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]
        ///        theEmitter->emitIns_R_R_R_R(INS_sve_ld1rod, EA_SCALABLE, REG_V0, REG_P2, REG_R1, REG_R3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe Vector<long> LoadVector256AndReplicateToVector(Vector<long> mask, long* address) => LoadVector256AndReplicateToVector(mask, address);

        /// <summary>
        /// svuint8_t svld1ro[_u8](svbool_t pg, const uint8_t *base)
        ///   LD1ROB Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LD1ROB Zresult.B, Pg/Z, [Xarray, #index]
        ///   LD1ROB Zresult.B, Pg/Z, [Xbase, #0]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IO_3A  LD1ROB {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_ld1rob, EA_SCALABLE, REG_V0, REG_P1, REG_R2, 0, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_IP_4A  LD1ROB {<Zt>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]
        ///        theEmitter->emitIns_R_R_R_R(INS_sve_ld1rob, EA_SCALABLE, REG_V0, REG_P1, REG_R3, REG_R2, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<byte> LoadVector256AndReplicateToVector(Vector<byte> mask, byte* address) => LoadVector256AndReplicateToVector(mask, address);

        /// <summary>
        /// svuint16_t svld1ro[_u16](svbool_t pg, const uint16_t *base)
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, #index * 2]
        ///   LD1ROH Zresult.H, Pg/Z, [Xbase, #0]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IO_3A  LD1ROH {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_ld1roh, EA_SCALABLE, REG_V8, REG_P3, REG_R1, -256, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IP_4A  LD1ROH {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        ///        theEmitter->emitIns_R_R_R_R(INS_sve_ld1roh, EA_SCALABLE, REG_V4, REG_P3, REG_R2, REG_R1, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe Vector<ushort> LoadVector256AndReplicateToVector(Vector<ushort> mask, ushort* address) => LoadVector256AndReplicateToVector(mask, address);

        /// <summary>
        /// svuint32_t svld1ro[_u32](svbool_t pg, const uint32_t *base)
        ///   LD1ROW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1ROW Zresult.S, Pg/Z, [Xarray, #index * 4]
        ///   LD1ROW Zresult.S, Pg/Z, [Xbase, #0]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IO_3A  LD1ROW {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_ld1row, EA_SCALABLE, REG_V3, REG_P4, REG_R0, 224, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_IP_4A  LD1ROW {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]
        ///        theEmitter->emitIns_R_R_R_R(INS_sve_ld1row, EA_SCALABLE, REG_V1, REG_P3, REG_R2, REG_R4, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe Vector<uint> LoadVector256AndReplicateToVector(Vector<uint> mask, uint* address) => LoadVector256AndReplicateToVector(mask, address);

        /// <summary>
        /// svuint64_t svld1ro[_u64](svbool_t pg, const uint64_t *base)
        ///   LD1ROD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1ROD Zresult.D, Pg/Z, [Xarray, #index * 8]
        ///   LD1ROD Zresult.D, Pg/Z, [Xbase, #0]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IO_3A  LD1ROD {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_ld1rod, EA_SCALABLE, REG_V4, REG_P5, REG_R6, -32, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_IP_4A  LD1ROD {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]
        ///        theEmitter->emitIns_R_R_R_R(INS_sve_ld1rod, EA_SCALABLE, REG_V0, REG_P2, REG_R1, REG_R3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe Vector<ulong> LoadVector256AndReplicateToVector(Vector<ulong> mask, ulong* address) => LoadVector256AndReplicateToVector(mask, address);

        /// <summary>
        /// svfloat32_t svld1ro[_f32](svbool_t pg, const float32_t *base)
        ///   LD1ROW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1ROW Zresult.S, Pg/Z, [Xarray, #index * 4]
        ///   LD1ROW Zresult.S, Pg/Z, [Xbase, #0]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IO_3A  LD1ROW {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_ld1row, EA_SCALABLE, REG_V3, REG_P4, REG_R0, 224, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_IP_4A  LD1ROW {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]
        ///        theEmitter->emitIns_R_R_R_R(INS_sve_ld1row, EA_SCALABLE, REG_V1, REG_P3, REG_R2, REG_R4, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe Vector<float> LoadVector256AndReplicateToVector(Vector<float> mask, float* address) => LoadVector256AndReplicateToVector(mask, address);

        /// <summary>
        /// svfloat64_t svld1ro[_f64](svbool_t pg, const float64_t *base)
        ///   LD1ROD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1ROD Zresult.D, Pg/Z, [Xarray, #index * 8]
        ///   LD1ROD Zresult.D, Pg/Z, [Xbase, #0]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IO_3A  LD1ROD {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_ld1rod, EA_SCALABLE, REG_V4, REG_P5, REG_R6, -32, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_IP_4A  LD1ROD {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]
        ///        theEmitter->emitIns_R_R_R_R(INS_sve_ld1rod, EA_SCALABLE, REG_V0, REG_P2, REG_R1, REG_R3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe Vector<double> LoadVector256AndReplicateToVector(Vector<double> mask, double* address) => LoadVector256AndReplicateToVector(mask, address);


        ///  MatrixMultiplyAccumulate : Matrix multiply-accumulate

        /// <summary>
        /// svfloat64_t svmmla[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FMMLA Ztied1.D, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult, Zop1; FMMLA Zresult.D, Zop2.D, Zop3.D
        ///
        /// codegenarm64test:
        ///    IF_SVE_HD_3A_A  FMMLA <Zda>.D, <Zn>.D, <Zm>.D
        ///        theEmitter->emitIns_R_R_R(INS_sve_fmmla, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<double> MatrixMultiplyAccumulate(Vector<double> op1, Vector<double> op2, Vector<double> op3) => MatrixMultiplyAccumulate(op1, op2, op3);

    }
}

