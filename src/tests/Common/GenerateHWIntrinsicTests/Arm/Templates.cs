// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

static class TestTemplates
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

    public static (string templateFileName, string outputTemplateName, Dictionary<string, string> templateData)[] Templates =
    {
        ("_UnaryOpScalarTestTemplate.template",              "DuplicateTest.template",                      new Dictionary<string, string> { ["TemplateName"] = "Duplicate",  ["TemplateValidationLogic"] = SimpleOpTest_ValidationLogic }),
        ("_ImmUnaryOpTestTemplate.template",                 "ImmUnOpTest.template",                        new Dictionary<string, string> { ["TemplateName"] = "Imm",        ["TemplateValidationLogic"] = SimpleOpTest_ValidationLogic }),
        ("_ImmUnaryOpTestTemplate.template",                 "VecImmUnOpTest.template",                     new Dictionary<string, string> { ["TemplateName"] = "Imm",        ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic }),
        ("_ImmTernaryOpTestTemplate.template",               "ImmTernOpTest.template",                      new Dictionary<string, string> { ["TemplateName"] = "Imm",        ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic }),
        ("_ImmOpTestTemplate.template",                      "ImmOpTest.template",                          new Dictionary<string, string> { ["TemplateName"] = "Imm",        ["TemplateValidationLogic"] = SimpleOpTest_ValidationLogic }),
        ("_ImmBinaryOpTestTemplate.template",                "ImmBinOpTest.template",                       new Dictionary<string, string> { ["TemplateName"] = "Imm",        ["TemplateValidationLogic"] = SimpleOpTest_ValidationLogic }),
        ("_ImmBinaryOpTestTemplate.template",                "VecImmBinOpTest.template",                    new Dictionary<string, string> { ["TemplateName"] = "Imm",        ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic }),
        ("_BinaryOpTestTemplate.template",                   "SimpleBinOpTest.template",                    new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleOpTest_ValidationLogic }),
        ("_TernaryOpTestTemplate.template",                  "VecTernOpTest.template",                      new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic }),
        ("_ImmTernaryOpTestTemplate.template",               "VecImmTernOpTest.template",                   new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic }),
        ("_ImmTernaryOpTestTemplate.template",               "SimpleImmTernOpTest.template",                new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleOpTest_ValidationLogic }),
        ("_UnaryOpTestTemplate.template",                    "SimpleUnOpTest.template",                     new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleOpTest_ValidationLogic }),
        ("_UnaryOpTestTemplate.template",                    "SimpleVecOpTest.template",                    new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic }),
        ("_BinaryOpTestTemplate.template",                   "VecPairBinOpTest.template",                   new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = VecPairBinOpTest_ValidationLogic }),
        ("_BinaryOp_SveTestTemplate.template",               "SveVecPairBinOpTest.template",                new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = VecPairBinOpTest_ValidationLogic }),
        ("_BinaryOp_SveTestTemplate.template",               "SveVecBinaryOpValidateTest.template",         new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SveValidateForEachRetElementCount_ValidationLogic }),
        ("_UnaryOpTestTemplate.template",                    "VecReduceUnOpTest.template",                  new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = VecReduceOpTest_ValidationLogic }),
        ("_BinaryOpTestTemplate.template",                   "VecBinOpTest.template",                       new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic }),
        ("_TernaryOpTestTemplate.template",                  "SimpleTernOpTest.template",                   new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleOpTest_ValidationLogic }),
        ("_UnaryOpTestTemplate.template",                    "SecureHashUnOpTest.template",                 new Dictionary<string, string> { ["TemplateName"] = "SecureHash", ["TemplateValidationLogic"] = SecureHashOpTest_ValidationLogic }),
        ("_BinaryOpTestTemplate.template",                   "SecureHashBinOpTest.template",                new Dictionary<string, string> { ["TemplateName"] = "SecureHash", ["TemplateValidationLogic"] = SecureHashOpTest_ValidationLogic }),
        ("_TernaryOpTestTemplate.template",                  "SecureHashTernOpTest.template",               new Dictionary<string, string> { ["TemplateName"] = "SecureHash", ["TemplateValidationLogic"] = SecureHashOpTest_ValidationLogic }),
        ("_SveUnaryOpTestTemplate.template",                 "SveSimpleVecOpTest.template",                 new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveUnaryOpDifferentRetTypeTestTemplate.template", "SveSimpleVecOpDiffRetTypeTest.template",      new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveUnaryOpDifferentRetTypeTestTemplate.template", "SveSimpleVecOpNarrowingTest.template",        new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogicForNarrowing, ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_ValidationLogicForCndSelForNarrowing, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_ValidationLogicForCndSelForNarrowing_FalseValue }),
        ("_SveUnaryOpDifferentRetTypeTestTemplate.template", "SveSimpleVecOpDiffRetTypeTestVec.template",   new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_VectorValidationLogic,       ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_VectorValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue }),
        ("_SveBinaryOpTestTemplate.template",                "SveVecBinOpTest.template",                    new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveBinaryOpTestTemplate.template",                "SveVecBinOpVecTest.template",                 new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_VectorValidationLogic,       ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_VectorValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue }),
        ("_SveBinaryOpTestTemplate.template",                "SveVecBinOpConvertTest.template",             new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleTernVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveBinaryOpDifferentRetTypeTestTemplate.template", "SveVecBinOpDifferentRetType.template",       new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_ValidationLogicForCndSel_FalseValue}),
        ("_SveMasklessBinaryOpTestTemplate.template",        "SveVecBinOpTestScalarRet.template",           new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleScalarOpTest_ValidationLogic }),
        ("_SveBinaryRetMaskOpTestTemplate.template",         "SveVecBinRetMaskOpConvertTest.template",      new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleTernVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue, ["TemplateValidationLogicForCndSelMask"] = SimpleVecOpTest_ValidationLogicForCndSelMask }),
        ("_SveBinaryOpDifferentTypesTestTemplate.template",  "SveVecBinOpDifferentTypesTest.template",      new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveBinaryMaskOpTestTemplate.template",            "SveMaskVecBinOpConvertTest.template",         new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveImmBinaryOpTestTemplate.template",             "SveVecImmBinOpTest.template",                 new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveImmBinaryOpTestTemplate.template",             "SveVecImmBinOpVecTest.template",              new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_VectorValidationLogic,       ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_VectorValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue }),
        ("_SveImmUnaryOpTestTemplate.template",              "SveVecImmUnOpTest.template",                  new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveTernOpTestTemplate.template",                  "SveVecTernOpTest.template",                   new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleTernVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveTernOpTestTemplate.template",                  "SveVecTernOpVecTest.template",                new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_VectorValidationLogic,       ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_VectorValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue }),
        ("_SveTernOpFirstArgTestTemplate.template",          "SveVecTernOpFirstArgTest.template",           new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleTernVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveImmTernOpTestTemplate.template",               "SveVecImmTernOpTest.template",                new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleTernVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveImmTernOpTestTemplate.template",               "SveVecImmTernOpVecTest.template",             new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_VectorValidationLogic,       ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_VectorValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue }),
        ("_SveImm2TernOpTestTemplate.template",              "SveVecImm2TernOpVecTest.template",            new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_VectorValidationLogic,       ["TemplateValidationLogicForCndSel"] = SimpleVecOpTest_VectorValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleVecOpTest_VectorValidationLogicForCndSel_FalseValue }),
        ("_SveTernOpMaskedOpTestTemplate.template",          "SveVecTernOpMaskedTest.template",             new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleTernVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveImmTernOpFirstArgTestTemplate.template",       "SveVecImmTernOpFirstArgTest.template",        new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleTernVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveImm2TernOpTestTemplate.template",              "SveVecTernOpImm2Test.template",               new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleTernVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveImmBinaryOpDifferentRetTypeTestTemplate.template", "SveVecImmBinOpDifferentRetType.template", new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = SimpleTernVecOpTest_ValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = SimpleTernVecOpTest_ValidationLogicForCndSel_FalseValue }),
        ("_SveScalarBinOpTestTemplate.template",             "SveScalarBinOpTest.template",                 new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleScalarOpTest_ValidationLogic }),
        ("_SveScalarTernOpTestTemplate.template",            "SveScalarTernOpTest.template",                new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleScalarOpTest_ValidationLogic }),
        ("_SveImm2UnaryOpTestTemplate.template",             "SveVecImm2UnOpTest.template",                 new Dictionary<string, string> { ["TemplateName"] = "Imm",        ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic }),
        ("_SveMinimalUnaryOpTestTemplate.template",          "SveVecReduceUnOpTest.template",               new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = VecReduceOpTest_ValidationLogic,             ["TemplateValidationLogicForCndSel"] = VecReduceUnOpTest_VectorValidationLogicForCndSel, ["TemplateValidationLogicForCndSel_FalseValue"] = VecReduceUnOpTest_VectorValidationLogicForCndSel_FalseValue }),
        ("_SveMasklessUnaryOpTestTemplate.template",         "SveMasklessSimpleVecOpTest.template",         new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic }),
        ("_SveVecAndScalarOpTest.template",                  "SveVecAndScalarOpTest.template",              new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_VectorValidationLogic }),
        ("_SveMasklessBinaryOpTestTemplate.template",        "SveMasklessVecBinOpTest.template",            new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic }),
        ("_SveStoreTemplate.template",                       "SveStoreTest.template",                       new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic }),
        ("_SveStoreTemplate.template",                       "SveStoreNarrowTest.template",                 new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogicForNarrowing }),
        ("_SveStoreTemplate.template",                       "SveStoreNonTemporalTest.template",            new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SimpleVecOpTest_ValidationLogic }),
        ("_SveTernaryOpValidateTestTemplate.template",       "SveVecTernOpValidateTest.template",           new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SveValidateForEachRetElementCount_ValidationLogic }),
        ("_SveBinaryOpTupleTestTemplate.template",           "SveVecBinOpTupleTest.template",               new Dictionary<string, string> { ["TemplateName"] = "Simple",     ["TemplateValidationLogic"] = SveValidateForEachRetElementCount_ValidationLogic }),
    };
}
