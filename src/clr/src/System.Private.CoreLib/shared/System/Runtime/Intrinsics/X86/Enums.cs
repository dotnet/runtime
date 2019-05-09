// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Intrinsics.X86
{
    public enum FloatComparisonMode : byte
    {
        /// <summary>
        /// _CMP_EQ_OQ
        /// </summary>
        OrderedEqualNonSignaling = 0,

        /// <summary>
        /// _CMP_LT_OS
        /// </summary>
        OrderedLessThanSignaling = 1,

        /// <summary>
        /// _CMP_LE_OS
        /// </summary>
        OrderedLessThanOrEqualSignaling = 2,

        /// <summary>
        /// _CMP_UNORD_Q
        /// </summary>
        UnorderedNonSignaling = 3,

        /// <summary>
        /// _CMP_NEQ_UQ
        /// </summary>
        UnorderedNotEqualNonSignaling = 4,

        /// <summary>
        /// _CMP_NLT_US
        /// </summary>
        UnorderedNotLessThanSignaling = 5,

        /// <summary>
        /// _CMP_NLE_US
        /// </summary>
        UnorderedNotLessThanOrEqualSignaling = 6,

        /// <summary>
        /// _CMP_ORD_Q
        /// </summary>
        OrderedNonSignaling = 7,

        /// <summary>
        /// _CMP_EQ_UQ
        /// </summary>
        UnorderedEqualNonSignaling = 8,

        /// <summary>
        /// _CMP_NGE_US
        /// </summary>
        UnorderedNotGreaterThanOrEqualSignaling = 9,

        /// <summary>
        /// _CMP_NGT_US
        /// </summary>
        UnorderedNotGreaterThanSignaling = 10,

        /// <summary>
        /// _CMP_FALSE_OQ
        /// </summary>
        OrderedFalseNonSignaling = 11,

        /// <summary>
        /// _CMP_NEQ_OQ
        /// </summary>
        OrderedNotEqualNonSignaling = 12,

        /// <summary>
        /// _CMP_GE_OS
        /// </summary>
        OrderedGreaterThanOrEqualSignaling = 13,

        /// <summary>
        /// _CMP_GT_OS
        /// </summary>
        OrderedGreaterThanSignaling = 14,

        /// <summary>
        /// _CMP_TRUE_UQ
        /// </summary>
        UnorderedTrueNonSignaling = 15,

        /// <summary>
        /// _CMP_EQ_OS
        /// </summary>
        OrderedEqualSignaling = 16,

        /// <summary>
        /// _CMP_LT_OQ
        /// </summary>
        OrderedLessThanNonSignaling = 17,

        /// <summary>
        /// _CMP_LE_OQ
        /// </summary>
        OrderedLessThanOrEqualNonSignaling = 18,

        /// <summary>
        /// _CMP_UNORD_S
        /// </summary>
        UnorderedSignaling = 19,

        /// <summary>
        /// _CMP_NEQ_US
        /// </summary>
        UnorderedNotEqualSignaling = 20,

        /// <summary>
        /// _CMP_NLT_UQ
        /// </summary>
        UnorderedNotLessThanNonSignaling = 21,

        /// <summary>
        /// _CMP_NLE_UQ
        /// </summary>
        UnorderedNotLessThanOrEqualNonSignaling = 22,

        /// <summary>
        /// _CMP_ORD_S
        /// </summary>
        OrderedSignaling = 23,

        /// <summary>
        /// _CMP_EQ_US
        /// </summary>
        UnorderedEqualSignaling = 24,

        /// <summary>
        /// _CMP_NGE_UQ
        /// </summary>
        UnorderedNotGreaterThanOrEqualNonSignaling = 25,

        /// <summary>
        /// _CMP_NGT_UQ
        /// </summary>
        UnorderedNotGreaterThanNonSignaling = 26,

        /// <summary>
        /// _CMP_FALSE_OS
        /// </summary>
        OrderedFalseSignaling = 27,

        /// <summary>
        /// _CMP_NEQ_OS
        /// </summary>
        OrderedNotEqualSignaling = 28,

        /// <summary>
        /// _CMP_GE_OQ
        /// </summary>
        OrderedGreaterThanOrEqualNonSignaling = 29,

        /// <summary>
        /// _CMP_GT_OQ
        /// </summary>
        OrderedGreaterThanNonSignaling = 30,

        /// <summary>
        /// _CMP_TRUE_US
        /// </summary>
        UnorderedTrueSignaling = 31,
    }
}
