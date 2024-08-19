// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Intrinsics.X86
{
    public enum FloatComparisonMode : byte
    {
        /// <summary>
        ///   <para>_CMP_EQ_OQ</para>
        /// </summary>
        OrderedEqualNonSignaling = 0,

        /// <summary>
        ///   <para>_CMP_LT_OS</para>
        /// </summary>
        OrderedLessThanSignaling = 1,

        /// <summary>
        ///   <para>_CMP_LE_OS</para>
        /// </summary>
        OrderedLessThanOrEqualSignaling = 2,

        /// <summary>
        ///   <para>_CMP_UNORD_Q</para>
        /// </summary>
        UnorderedNonSignaling = 3,

        /// <summary>
        ///   <para>_CMP_NEQ_UQ</para>
        /// </summary>
        UnorderedNotEqualNonSignaling = 4,

        /// <summary>
        ///   <para>_CMP_NLT_US</para>
        /// </summary>
        UnorderedNotLessThanSignaling = 5,

        /// <summary>
        ///   <para>_CMP_NLE_US</para>
        /// </summary>
        UnorderedNotLessThanOrEqualSignaling = 6,

        /// <summary>
        ///   <para>_CMP_ORD_Q</para>
        /// </summary>
        OrderedNonSignaling = 7,

        /// <summary>
        ///   <para>_CMP_EQ_UQ</para>
        /// </summary>
        UnorderedEqualNonSignaling = 8,

        /// <summary>
        ///   <para>_CMP_NGE_US</para>
        /// </summary>
        UnorderedNotGreaterThanOrEqualSignaling = 9,

        /// <summary>
        ///   <para>_CMP_NGT_US</para>
        /// </summary>
        UnorderedNotGreaterThanSignaling = 10,

        /// <summary>
        ///   <para>_CMP_FALSE_OQ</para>
        /// </summary>
        OrderedFalseNonSignaling = 11,

        /// <summary>
        ///   <para>_CMP_NEQ_OQ</para>
        /// </summary>
        OrderedNotEqualNonSignaling = 12,

        /// <summary>
        ///   <para>_CMP_GE_OS</para>
        /// </summary>
        OrderedGreaterThanOrEqualSignaling = 13,

        /// <summary>
        ///   <para>_CMP_GT_OS</para>
        /// </summary>
        OrderedGreaterThanSignaling = 14,

        /// <summary>
        ///   <para>_CMP_TRUE_UQ</para>
        /// </summary>
        UnorderedTrueNonSignaling = 15,

        /// <summary>
        ///   <para>_CMP_EQ_OS</para>
        /// </summary>
        OrderedEqualSignaling = 16,

        /// <summary>
        ///   <para>_CMP_LT_OQ</para>
        /// </summary>
        OrderedLessThanNonSignaling = 17,

        /// <summary>
        ///   <para>_CMP_LE_OQ</para>
        /// </summary>
        OrderedLessThanOrEqualNonSignaling = 18,

        /// <summary>
        ///   <para>_CMP_UNORD_S</para>
        /// </summary>
        UnorderedSignaling = 19,

        /// <summary>
        ///   <para>_CMP_NEQ_US</para>
        /// </summary>
        UnorderedNotEqualSignaling = 20,

        /// <summary>
        ///   <para>_CMP_NLT_UQ</para>
        /// </summary>
        UnorderedNotLessThanNonSignaling = 21,

        /// <summary>
        ///   <para>_CMP_NLE_UQ</para>
        /// </summary>
        UnorderedNotLessThanOrEqualNonSignaling = 22,

        /// <summary>
        ///   <para>_CMP_ORD_S</para>
        /// </summary>
        OrderedSignaling = 23,

        /// <summary>
        ///   <para>_CMP_EQ_US</para>
        /// </summary>
        UnorderedEqualSignaling = 24,

        /// <summary>
        ///   <para>_CMP_NGE_UQ</para>
        /// </summary>
        UnorderedNotGreaterThanOrEqualNonSignaling = 25,

        /// <summary>
        ///   <para>_CMP_NGT_UQ</para>
        /// </summary>
        UnorderedNotGreaterThanNonSignaling = 26,

        /// <summary>
        ///   <para>_CMP_FALSE_OS</para>
        /// </summary>
        OrderedFalseSignaling = 27,

        /// <summary>
        ///   <para>_CMP_NEQ_OS</para>
        /// </summary>
        OrderedNotEqualSignaling = 28,

        /// <summary>
        ///   <para>_CMP_GE_OQ</para>
        /// </summary>
        OrderedGreaterThanOrEqualNonSignaling = 29,

        /// <summary>
        ///   <para>_CMP_GT_OQ</para>
        /// </summary>
        OrderedGreaterThanNonSignaling = 30,

        /// <summary>
        ///   <para>_CMP_TRUE_US</para>
        /// </summary>
        UnorderedTrueSignaling = 31,
    }

    public enum FloatRoundingMode : byte
    {
        /// <summary>
        ///   <para>_MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC</para>
        /// </summary>
        ToEven = 0x08,
        /// <summary>
        ///   <para>_MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC</para>
        /// </summary>
        ToNegativeInfinity = 0x09,
        /// <summary>
        ///   <para>_MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC</para>
        /// </summary>
        ToPositiveInfinity = 0x0A,
        /// <summary>
        ///   <para>_MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC</para>
        /// </summary>
        ToZero = 0x0B,
    }
}
