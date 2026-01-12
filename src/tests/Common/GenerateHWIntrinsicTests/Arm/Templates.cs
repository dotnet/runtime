// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

static class Templates
{
    const string SimpleOpTest_ValidationLogic = @"if ({ValidateFirstResult})
                {
                    succeeded = false;
                }
                else
                {
                    for (var i = 1; i < RetElementCount; i++)
                    {
                        if ({ValidateRemainingResults})
                        {
                            succeeded = false;
                            break;
                        }
                    }
                }";

    const string SimpleVecOpTest_ValidationLogic = @"for (var i = 0; i < RetElementCount; i++)
                {
                    if ({ValidateIterResult})
                    {
                        succeeded = false;
                        break;
                    }
                }";

    const string SimpleVecOpTest_ValidationLogicForNarrowing = @"for (var i = 0; i < Op1ElementCount; i++)
                {
                    if ({ValidateIterResult})
                    {
                        succeeded = false;
                        break;
                    }
                }";

    const string SimpleVecOpTest_VectorValidationLogic = @"succeeded = !({ValidateVectorResult});";

    const string SimpleScalarOpTest_ValidationLogic = @"succeeded = !({ValidateScalarResult});";

    const string SimpleTernVecOpTest_ValidationLogic = @"for (var i = 0; i < RetElementCount; i++)
                {
                    if ({ValidateIterResult})
                    {
                        succeeded = false;
                        break;
                    }
                }";


    const string SimpleVecOpTest_ValidationLogicForCndSel = @"for (var i = 0; i < RetElementCount; i++)
                {
                    {RetBaseType} iterResult = (mask[i] != 0) ? {GetIterResult} : falseVal[i];
                    if (iterResult != result[i])
                    {
                        succeeded = false;
                        break;
                    }
                }";

    const string SimpleVecOpTest_ValidationLogicForCndSelMask = @"for (var i = 0; i < RetElementCount; i++)
                {
                    {RetBaseType} iterResult = ({GetIterResult} != 0) ? trueVal[i] : falseVal[i];
                    if (iterResult != result[i])
                    {
                        succeeded = false;
                        break;
                    }
                }";

    const string SimpleVecOpTest_ValidationLogicForCndSel_FalseValue = @"for (var i = 0; i < RetElementCount; i++)
                {
                    {RetBaseType} iterResult = (mask[i] != 0) ? trueVal[i] : {GetIterResult};
                    if (mask[i] != 0)
                    {
                        // Pick the trueValue
                        if (iterResult != result[i])
                        {
                            succeeded = false;
                            break;
                        }
                    }
                    else
                    {
                        // For false, the values are merged with destination, and we do not know
                        // those contents would be, so skip verification for them.
                    }
                }";

    const string SimpleVecOpTest_ValidationLogicForCndSelForNarrowing = @"for (var i = 0; i < Op1ElementCount; i++)
                {
                    {RetBaseType} iterResult = (mask[i] != 0) ? {GetIterResult} : falseVal[i];
                    if ({ConvertFunc}(iterResult) != {ConvertFunc}(result[i]))
                    {
                        succeeded = false;
                        break;
                    }
                }";

    const string SimpleVecOpTest_ValidationLogicForCndSelForNarrowing_FalseValue = @"for (var i = 0; i < Op1ElementCount; i++)
                {
                    {RetBaseType} iterResult = (mask[i] != 0) ? trueVal[i] : {GetIterResult};
                    if (mask[i] != 0)
                    {
                        // Pick the trueValue
                        if ({ConvertFunc}(iterResult) != {ConvertFunc}(result[i]))
                        {
                            succeeded = false;
                            break;
                        }
                    }
                    else
                    {
                        // For false, the values are merged with destination, and we do not know
                        // those contents would be, so skip verification for them.
                    }
                }";


    const string SimpleVecOpTest_VectorValidationLogicForCndSel = @"
                
                    {RetBaseType}[] vectorResult = {GetVectorResult};
                    {RetBaseType}[] maskedVectorResult = new {RetBaseType}[vectorResult.Length];

                    for (var i = 0; i < vectorResult.Length; i++)
                    {
                        maskedVectorResult[i] = (mask[i] != 0) ? vectorResult[i] : falseVal[i];
                    }

                    if (!result.SequenceEqual(maskedVectorResult))
                    {
                        succeeded = false;
                    }
                ";

    const string SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue = @"
                {
                    {RetBaseType}[] vectorResult = {GetVectorResult};
                    {RetBaseType}[] maskedVectorResult = new {RetBaseType}[vectorResult.Length];

                    for (var i = 0; i < vectorResult.Length; i++)
                    {
                        {RetBaseType} iterResult = (mask[i] != 0) ? trueVal[i] : vectorResult[i];
                        if (mask[i] != 0)
                        {
                            // Pick the trueValue
                            if (iterResult != result[i])
                            {
                                succeeded = false;
                                break;
                            }
                        }
                        else
                        {
                            // For false, the values are merged with destination, and we do not know
                            // those contents would be, so skip verification for them.
                        }
                    }
                }";

    const string SimpleTernVecOpTest_ValidationLogicForCndSel = @"for (var i = 0; i < RetElementCount; i++)
                {
                    {RetBaseType} iterResult = (mask[i] != 0) ? {GetIterResult} : falseVal[i];
                    if ({ConvertFunc}(iterResult) != {ConvertFunc}(result[i]))
                    {
                        succeeded = false;
                        break;
                    }
                }";

    const string SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue = @"for (var i = 0; i < RetElementCount; i++)
                {
                    {RetBaseType} iterResult = (mask[i] != 0) ? trueVal[i] : {GetIterResult};
                    if (mask[i] != 0)
                    {
                        // Pick the trueValue
                        if ({ConvertFunc}(iterResult) !=  {ConvertFunc}(result[i]))
                        {
                            succeeded = false;
                            break;
                        }
                    }
                    else
                    {
                        // For false, the values are merged with destination, and we do not know
                        // those contents would be, so skip verification for them.
                    }
                }";

    const string VecPairBinOpTest_ValidationLogic = @"
                int index = 0;
                int half  = RetElementCount / 2;
                for (var i = 0; i < RetElementCount; i+=2, index++)
                {
                        if ({ValidateEntry})
                        {
                            succeeded = false;
                            break;
                        }
                }";

    const string SveValidateForEachRetElementCount_ValidationLogic = @"
                for (var i = 0; i < RetElementCount; i++)
                {
                    if ({ValidateEntry})
                    {
                        succeeded = false;
                        break;
                    }
                }";

    const string VecReduceUnOpTest_VectorValidationLogicForCndSel = @"
                {
                    var hasFailed = (mask[0] != 0) ? ({ValidateReduceOpResult}): (falseVal[0] != result[0]);

                    if (hasFailed)
                    {
                        succeeded = false;
                    }
                    else
                    {
                        for (var i = 1; i < RetElementCount; i++)
                        {
                            hasFailed = (mask[i] != 0) ? ({ValidateRemainingResults}) : (falseVal[i] != result[i]);
                            if (hasFailed)
                            {
                                succeeded = false;
                                break;
                            }
                        }
                    }
                }";

    const string VecReduceUnOpTest_VectorValidationLogicForCndSel_FalseValue = @"
                {
                    var hasFailed = (mask[0] != 0) ? (trueVal[0] != result[0]): ({ValidateReduceOpResult});
                    if (hasFailed)
                    {
                        succeeded = false;
                    }
                    else
                    {
                        for (var i = 1; i < RetElementCount; i++)
                        {
                            hasFailed = (mask[i] != 0) ? (trueVal[i] != result[i]) : ({ValidateRemainingResults});
                            if (hasFailed)
                            {
                                succeeded = false;
                                break;
                            }
                        }
                    }
                }";

    const string VecReduceOpTest_ValidationLogic = @"if ({ValidateReduceOpResult})
                {
                    succeeded = false;
                }
                else
                {
                    for (int i = 1; i < RetElementCount; i++)
                    {
                        if ({ValidateRemainingResults})
                        {
                            succeeded = false;
                            break;
                        }
                    }
                }";

    const string SecureHashOpTest_ValidationLogic = @"{RetBaseType}[] expectedResult = new {RetBaseType}[]{ExpectedResult};

                for (int i = 0; i < RetElementCount; i++)
                {
                    if (result[i] != expectedResult[i])
                    {
                        succeeded = false;
                        break;
                    }
                }";

    public static TemplateConfig DuplicateTest = new TemplateConfig("_UnaryOpScalarTestTemplate.template", "Duplicate", SimpleOpTest_ValidationLogic);
    public static TemplateConfig ImmUnOpTest = new TemplateConfig("_ImmUnaryOpTestTemplate.template", "Imm", SimpleOpTest_ValidationLogic);
    public static TemplateConfig VecImmUnOpTest = new TemplateConfig("_ImmUnaryOpTestTemplate.template", "Imm", SimpleVecOpTest_ValidationLogic);
    public static TemplateConfig ImmTernOpTest = new TemplateConfig("_ImmTernaryOpTestTemplate.template", "Imm", SimpleVecOpTest_ValidationLogic);
    public static TemplateConfig ImmOpTest = new TemplateConfig("_ImmOpTestTemplate.template", "Imm", SimpleOpTest_ValidationLogic);
    public static TemplateConfig ImmBinOpTest = new TemplateConfig("_ImmBinaryOpTestTemplate.template", "Imm", SimpleOpTest_ValidationLogic);
    public static TemplateConfig VecImmBinOpTest = new TemplateConfig("_ImmBinaryOpTestTemplate.template", "Imm", SimpleVecOpTest_ValidationLogic);
    public static TemplateConfig SimpleBinOpTest = new TemplateConfig("_BinaryOpTestTemplate.template", "Simple", SimpleOpTest_ValidationLogic);
    public static TemplateConfig VecTernOpTest = new TemplateConfig("_TernaryOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic);
    public static TemplateConfig VecImmTernOpTest = new TemplateConfig("_ImmTernaryOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic);
    public static TemplateConfig SimpleImmTernOpTest = new TemplateConfig("_ImmTernaryOpTestTemplate.template", "Simple", SimpleOpTest_ValidationLogic);
    public static TemplateConfig SimpleUnOpTest = new TemplateConfig("_UnaryOpTestTemplate.template", "Simple", SimpleOpTest_ValidationLogic);
    public static TemplateConfig SimpleVecOpTest = new TemplateConfig("_UnaryOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic);
    public static TemplateConfig VecPairBinOpTest = new TemplateConfig("_BinaryOpTestTemplate.template", "Simple", VecPairBinOpTest_ValidationLogic);
    public static TemplateConfig SveVecPairBinOpTest = new TemplateConfig("_BinaryOp_SveTestTemplate.template", "Simple", VecPairBinOpTest_ValidationLogic);
    public static TemplateConfig SveVecBinaryOpValidateTest = new TemplateConfig("_BinaryOp_SveTestTemplate.template", "Simple", SveValidateForEachRetElementCount_ValidationLogic);
    public static TemplateConfig VecReduceUnOpTest = new TemplateConfig("_UnaryOpTestTemplate.template", "Simple", VecReduceOpTest_ValidationLogic);
    public static TemplateConfig VecBinOpTest = new TemplateConfig("_BinaryOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic);
    public static TemplateConfig SimpleTernOpTest = new TemplateConfig("_TernaryOpTestTemplate.template", "Simple", SimpleOpTest_ValidationLogic);
    public static TemplateConfig SecureHashUnOpTest = new TemplateConfig("_UnaryOpTestTemplate.template", "SecureHash", SecureHashOpTest_ValidationLogic);
    public static TemplateConfig SecureHashBinOpTest = new TemplateConfig("_BinaryOpTestTemplate.template", "SecureHash", SecureHashOpTest_ValidationLogic);
    public static TemplateConfig SecureHashTernOpTest = new TemplateConfig("_TernaryOpTestTemplate.template", "SecureHash", SecureHashOpTest_ValidationLogic);
    public static TemplateConfig SveSimpleVecOpTest = new TemplateConfig("_SveUnaryOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleVecOpTest_ValidationLogicForCndSel, SimpleVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveSimpleVecOpDiffRetTypeTest = new TemplateConfig("_SveUnaryOpDifferentRetTypeTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleVecOpTest_ValidationLogicForCndSel, SimpleVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveSimpleVecOpDiffRetTypeFloats = new TemplateConfig("_SveUnaryOpDifferentRetTypeTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleVecOpTest_ValidationLogicForCndSelForNarrowing, SimpleVecOpTest_ValidationLogicForCndSelForNarrowing_FalseValue);
    public static TemplateConfig SveSimpleVecOpNarrowingTest = new TemplateConfig("_SveUnaryOpDifferentRetTypeTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogicForNarrowing, SimpleVecOpTest_ValidationLogicForCndSelForNarrowing, SimpleVecOpTest_ValidationLogicForCndSelForNarrowing_FalseValue);
    public static TemplateConfig SveSimpleVecOpDiffRetTypeTestVec = new TemplateConfig("_SveUnaryOpDifferentRetTypeTestTemplate.template", "Simple", SimpleVecOpTest_VectorValidationLogic, SimpleVecOpTest_VectorValidationLogicForCndSel, SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecBinOpTest = new TemplateConfig("_SveBinaryOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleVecOpTest_ValidationLogicForCndSel, SimpleVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecBinOpVecTest = new TemplateConfig("_SveBinaryOpTestTemplate.template", "Simple", SimpleVecOpTest_VectorValidationLogic, SimpleVecOpTest_VectorValidationLogicForCndSel, SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecBinOpConvertTest = new TemplateConfig("_SveBinaryOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleTernVecOpTest_ValidationLogicForCndSel, SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecBinOpDifferentRetType = new TemplateConfig("_SveBinaryOpDifferentRetTypeTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleVecOpTest_ValidationLogicForCndSel, SimpleVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecBinOpDiffRetTypeFloats = new TemplateConfig("_SveBinaryOpDifferentRetTypeTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleVecOpTest_ValidationLogicForCndSelForNarrowing, SimpleVecOpTest_ValidationLogicForCndSelForNarrowing_FalseValue);
    public static TemplateConfig SveVecBinOpTestScalarRet = new TemplateConfig("_SveMasklessBinaryOpTestTemplate.template", "Simple", SimpleScalarOpTest_ValidationLogic);
    public static TemplateConfig SveVecBinRetMaskOpConvertTest = new TemplateConfig("_SveBinaryRetMaskOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleTernVecOpTest_ValidationLogicForCndSel, SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue, SimpleVecOpTest_ValidationLogicForCndSelMask);
    public static TemplateConfig SveVecBinOpDifferentTypesTest = new TemplateConfig("_SveBinaryOpDifferentTypesTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleVecOpTest_ValidationLogicForCndSel, SimpleVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveMaskVecBinOpConvertTest = new TemplateConfig("_SveBinaryMaskOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleVecOpTest_ValidationLogicForCndSel, SimpleVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecImmBinOpTest = new TemplateConfig("_SveImmBinaryOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleVecOpTest_ValidationLogicForCndSel, SimpleVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecImmBinOpVecTest = new TemplateConfig("_SveImmBinaryOpTestTemplate.template", "Simple", SimpleVecOpTest_VectorValidationLogic, SimpleVecOpTest_VectorValidationLogicForCndSel, SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecImmUnOpTest = new TemplateConfig("_SveImmUnaryOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleVecOpTest_ValidationLogicForCndSel, SimpleVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecTernOpTest = new TemplateConfig("_SveTernOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleTernVecOpTest_ValidationLogicForCndSel, SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecTernOpVecTest = new TemplateConfig("_SveTernOpTestTemplate.template", "Simple", SimpleVecOpTest_VectorValidationLogic, SimpleVecOpTest_VectorValidationLogicForCndSel, SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecTernOpFirstArgTest = new TemplateConfig("_SveTernOpFirstArgTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleTernVecOpTest_ValidationLogicForCndSel, SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecImmTernOpTest = new TemplateConfig("_SveImmTernOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleTernVecOpTest_ValidationLogicForCndSel, SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecImmTernOpVecTest = new TemplateConfig("_SveImmTernOpTestTemplate.template", "Simple", SimpleVecOpTest_VectorValidationLogic, SimpleVecOpTest_VectorValidationLogicForCndSel, SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecImm2TernOpVecTest = new TemplateConfig("_SveImm2TernOpTestTemplate.template", "Simple", SimpleVecOpTest_VectorValidationLogic, SimpleVecOpTest_VectorValidationLogicForCndSel, SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecTernOpMaskedTest = new TemplateConfig("_SveTernOpMaskedOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleTernVecOpTest_ValidationLogicForCndSel, SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecImmTernOpFirstArgTest = new TemplateConfig("_SveImmTernOpFirstArgTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleTernVecOpTest_ValidationLogicForCndSel, SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecTernOpImm2Test = new TemplateConfig("_SveImm2TernOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleTernVecOpTest_ValidationLogicForCndSel, SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveVecImmBinOpDifferentRetType = new TemplateConfig("_SveImmBinaryOpDifferentRetTypeTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic, SimpleTernVecOpTest_ValidationLogicForCndSel, SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveScalarBinOpTest = new TemplateConfig("_SveScalarBinOpTestTemplate.template", "Simple", SimpleScalarOpTest_ValidationLogic);
    public static TemplateConfig SveScalarTernOpTest = new TemplateConfig("_SveScalarTernOpTestTemplate.template", "Simple", SimpleScalarOpTest_ValidationLogic);
    public static TemplateConfig SveVecImm2UnOpTest = new TemplateConfig("_SveImm2UnaryOpTestTemplate.template", "Imm", SimpleVecOpTest_ValidationLogic);
    public static TemplateConfig SveVecReduceUnOpTest = new TemplateConfig("_SveMinimalUnaryOpTestTemplate.template", "Simple", VecReduceOpTest_ValidationLogic, VecReduceUnOpTest_VectorValidationLogicForCndSel, VecReduceUnOpTest_VectorValidationLogicForCndSel_FalseValue);
    public static TemplateConfig SveMasklessSimpleVecOpTest = new TemplateConfig("_SveMasklessUnaryOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic);
    public static TemplateConfig SveVecAndScalarOpTest = new TemplateConfig("_SveVecAndScalarOpTest.template", "Simple", SimpleVecOpTest_VectorValidationLogic);
    public static TemplateConfig SveMasklessVecBinOpTest = new TemplateConfig("_SveMasklessBinaryOpTestTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic);
    public static TemplateConfig SveStoreTest = new TemplateConfig("_SveStoreTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic);
    public static TemplateConfig SveStoreNarrowTest = new TemplateConfig("_SveStoreTemplate.template", "Simple", SimpleVecOpTest_ValidationLogicForNarrowing);
    public static TemplateConfig SveStoreNonTemporalTest = new TemplateConfig("_SveStoreTemplate.template", "Simple", SimpleVecOpTest_ValidationLogic);
    public static TemplateConfig SveVecTernOpValidateTest = new TemplateConfig("_SveTernaryOpValidateTestTemplate.template", "Simple", SveValidateForEachRetElementCount_ValidationLogic);
    public static TemplateConfig SveVecBinOpTupleTest = new TemplateConfig("_SveBinaryOpTupleTestTemplate.template", "Simple", SveValidateForEachRetElementCount_ValidationLogic);
    public static TemplateConfig SveStoreAndZipTestx2 = new TemplateConfig("SveStoreAndZipTestx2.template");
    public static TemplateConfig SveStoreAndZipTestx3 = new TemplateConfig("SveStoreAndZipTestx3.template");
    public static TemplateConfig SveStoreAndZipTestx4 = new TemplateConfig("SveStoreAndZipTestx4.template");
    public static TemplateConfig SveTestTest = new TemplateConfig("SveTestTest.template");
    public static TemplateConfig SveScatterVectorOffsets = new TemplateConfig("SveScatterVectorOffsets.template");
    public static TemplateConfig ScalarImm2UnOpTest = new TemplateConfig("ScalarImm2UnOpTest.template");
    public static TemplateConfig SveSaturatingByActiveElementCount = new TemplateConfig("SveSaturatingByActiveElementCount.template");
    public static TemplateConfig SveLoad2xVectorAndUnzipTest = new TemplateConfig("SveLoad2xVectorAndUnzipTest.template");
    public static TemplateConfig SveLoad3xVectorAndUnzipTest = new TemplateConfig("SveLoad3xVectorAndUnzipTest.template");
    public static TemplateConfig SveLoad4xVectorAndUnzipTest = new TemplateConfig("SveLoad4xVectorAndUnzipTest.template");
    public static TemplateConfig SveScatterVector = new TemplateConfig("SveScatterVector.template");
    public static TemplateConfig SveScatterVectorBases = new TemplateConfig("SveScatterVectorBases.template");
    public static TemplateConfig SveGatherVectorFirstFaulting = new TemplateConfig("SveGatherVectorFirstFaulting.template");
    public static TemplateConfig AesBinOpTest = new TemplateConfig("AesBinOpTest.template");
    public static TemplateConfig AesUnOpTest = new TemplateConfig("AesUnOpTest.template");
    public static TemplateConfig ExtractTest = new TemplateConfig("ExtractTest.template");
    public static TemplateConfig ExtractVectorTest = new TemplateConfig("ExtractVectorTest.template");
    public static TemplateConfig InsertScalarTest = new TemplateConfig("InsertScalarTest.template");
    public static TemplateConfig InsertSelectedScalarTest = new TemplateConfig("InsertSelectedScalarTest.template");
    public static TemplateConfig InsertTest = new TemplateConfig("InsertTest.template");
    public static TemplateConfig LoadAndInsertScalarTest = new TemplateConfig("LoadAndInsertScalarTest.template");
    public static TemplateConfig LoadAndInsertScalarx2Test = new TemplateConfig("LoadAndInsertScalarx2Test.template");
    public static TemplateConfig LoadAndInsertScalarx3Test = new TemplateConfig("LoadAndInsertScalarx3Test.template");
    public static TemplateConfig LoadAndInsertScalarx4Test = new TemplateConfig("LoadAndInsertScalarx4Test.template");
    public static TemplateConfig LoadPairVectorTest = new TemplateConfig("LoadPairVectorTest.template");
    public static TemplateConfig LoadUnOpTest = new TemplateConfig("LoadUnOpTest.template");
    public static TemplateConfig LoadVectorx2Test = new TemplateConfig("LoadVectorx2Test.template");
    public static TemplateConfig LoadVectorx3Test = new TemplateConfig("LoadVectorx3Test.template");
    public static TemplateConfig LoadVectorx4Test = new TemplateConfig("LoadVectorx4Test.template");
    public static TemplateConfig ScalarBinOpRetVecTest = new TemplateConfig("ScalarBinOpRetVecTest.template");
    public static TemplateConfig ScalarBinOpTest = new TemplateConfig("ScalarBinOpTest.template");
    public static TemplateConfig ScalarUnOpTest = new TemplateConfig("ScalarUnOpTest.template");
    public static TemplateConfig StoreBinOpTest = new TemplateConfig("StoreBinOpTest.template");
    public static TemplateConfig StoreSelectedScalarTest = new TemplateConfig("StoreSelectedScalarTest.template");
    public static TemplateConfig StoreSelectedScalarx2Test = new TemplateConfig("StoreSelectedScalarx2Test.template");
    public static TemplateConfig StoreSelectedScalarx3Test = new TemplateConfig("StoreSelectedScalarx3Test.template");
    public static TemplateConfig StoreSelectedScalarx4Test = new TemplateConfig("StoreSelectedScalarx4Test.template");
    public static TemplateConfig StoreUnOpTest = new TemplateConfig("StoreUnOpTest.template");
    public static TemplateConfig StoreVectorx2Test = new TemplateConfig("StoreVectorx2Test.template");
    public static TemplateConfig StoreVectorx3Test = new TemplateConfig("StoreVectorx3Test.template");
    public static TemplateConfig StoreVectorx4Test = new TemplateConfig("StoreVectorx4Test.template");
    public static TemplateConfig SveConditionalSelect = new TemplateConfig("SveConditionalSelect.template");
    public static TemplateConfig SveCreateTrueMaskTest = new TemplateConfig("SveCreateTrueMaskTest.template");
    public static TemplateConfig SveExtractVectorTest = new TemplateConfig("SveExtractVectorTest.template");
    public static TemplateConfig SveFfrTest = new TemplateConfig("SveFfrTest.template");
    public static TemplateConfig SveGatherVector = new TemplateConfig("SveGatherVector.template");
    public static TemplateConfig SveGatherPrefetchIndices = new TemplateConfig("SveGatherPrefetchIndices.template");
    public static TemplateConfig SveGatherPrefetchVectorBases = new TemplateConfig("SveGatherPrefetchVectorBases.template");
    public static TemplateConfig SveGatherVectorByteOffsetFirstFaulting = new TemplateConfig("SveGatherVectorByteOffsetFirstFaulting.template");
    public static TemplateConfig SveGatherVectorByteOffsets = new TemplateConfig("SveGatherVectorByteOffsets.template");
    public static TemplateConfig SveGatherVectorFirstFaultingIndices = new TemplateConfig("SveGatherVectorFirstFaultingIndices.template");
    public static TemplateConfig SveGatherVectorFirstFaultingVectorBases = new TemplateConfig("SveGatherVectorFirstFaultingVectorBases.template");
    public static TemplateConfig SveGatherVectorIndices = new TemplateConfig("SveGatherVectorIndices.template");
    public static TemplateConfig SveGatherVectorVectorBases = new TemplateConfig("SveGatherVectorVectorBases.template");
    public static TemplateConfig SveLoadNonFaultingMaskedUnOpTest = new TemplateConfig("SveLoadNonFaultingMaskedUnOpTest.template");
    public static TemplateConfig SveLoadVectorFirstFaultingTest = new TemplateConfig("SveLoadVectorFirstFaultingTest.template");
    public static TemplateConfig SveLoadVectorMaskedTest = new TemplateConfig("SveLoadVectorMaskedTest.template");
    public static TemplateConfig SvePrefetchTest = new TemplateConfig("SvePrefetchTest.template");
    public static TemplateConfig SveSimpleNoOpTest = new TemplateConfig("SveSimpleNoOpTest.template");
    public static TemplateConfig SveVecReduceToScalarBinOpTest = new TemplateConfig("SveVecReduceToScalarBinOpTest.template");
    public static TemplateConfig VectorLookupExtension_2Test = new TemplateConfig("VectorLookupExtension_2Test.template");
    public static TemplateConfig VectorLookupExtension_3Test = new TemplateConfig("VectorLookupExtension_3Test.template");
    public static TemplateConfig VectorLookupExtension_4Test = new TemplateConfig("VectorLookupExtension_4Test.template");
    public static TemplateConfig VectorLookup_2Test = new TemplateConfig("VectorLookup_2Test.template");
    public static TemplateConfig VectorLookup_3Test = new TemplateConfig("VectorLookup_3Test.template");
    public static TemplateConfig VectorLookup_4Test = new TemplateConfig("VectorLookup_4Test.template");
}
