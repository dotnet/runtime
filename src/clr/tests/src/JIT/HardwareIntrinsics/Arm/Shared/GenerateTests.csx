// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// DIRECTIONS:
//    This file isn't very robust and makes several assumptions
//    You can execute it by calling "csi .\GenerateTests.csx"
//
//    csi can be found under the <repo-root>\tools\net46\roslyn directory
//    It must be run such from the directory that contains the csx script
//
//    New tests can be generated from the template by adding an entry to the
//    appropriate Inputs array below.
//
//    You can support a new Isa by creating a new array and adding a new
//    "ProcessInputs" call at the bottom of the script.

private const string SimpleOpTest_ValidationLogic = @"if ({ValidateFirstResult})
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

private static readonly (string templateFileName, string outputTemplateName, Dictionary<string, string> templateData)[] Templates = new[]
{
    ("_BinaryOpTestTemplate.template",        "SimpleBinOpTest.template",       new Dictionary<string, string> { ["TemplateName"] = "Simple",      ["TemplateValidationLogic"] = SimpleOpTest_ValidationLogic }),
    ("_UnaryOpTestTemplate.template",         "SimpleUnOpTest.template",        new Dictionary<string, string> { ["TemplateName"] = "Simple",      ["TemplateValidationLogic"] = SimpleOpTest_ValidationLogic }),
};

private static readonly (string templateFileName, Dictionary<string, string> templateData)[] AdvSimd_Vector64Inputs = new []
{
    ("SimpleUnOpTest.template",   new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Abs",                                      ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "Byte",    ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "SByte",                                                               ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "(sbyte)-TestLibrary.Generator.GetSByte()",                                                         ["ValidateFirstResult"] = "result[0] != (byte)Math.Abs(firstOp[0])",                                                                                            ["ValidateRemainingResults"] = "result[i] != (byte)Math.Abs(firstOp[i])"}),
    ("SimpleUnOpTest.template",   new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Abs",                                      ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "UInt16",  ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "Int16",                                                               ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "(short)-TestLibrary.Generator.GetInt16()",                                                         ["ValidateFirstResult"] = "result[0] != (ushort)Math.Abs(firstOp[0])",                                                                                          ["ValidateRemainingResults"] = "result[i] != (ushort)Math.Abs(firstOp[i])"}),
    ("SimpleUnOpTest.template",   new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Abs",                                      ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "UInt32",  ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "Int32",                                                               ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "-TestLibrary.Generator.GetInt32()",                                                         ["ValidateFirstResult"] = "result[0] != (uint)Math.Abs(firstOp[0])",                                                                                            ["ValidateRemainingResults"] = "result[i] != (uint)Math.Abs(firstOp[i])"}),
    ("SimpleUnOpTest.template",   new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Abs",                                      ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "Single",  ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "Single",                                                              ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "-TestLibrary.Generator.GetSingle()",                                                        ["ValidateFirstResult"] = "BitConverter.SingleToInt32Bits(result[0]) != BitConverter.SingleToInt32Bits(Math.Abs(firstOp[0]))",                                  ["ValidateRemainingResults"] = "BitConverter.SingleToInt32Bits(result[i]) != BitConverter.SingleToInt32Bits(Math.Abs(firstOp[i]))"}),
    ("SimpleUnOpTest.template",   new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "AbsScalar",                                ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "Single",  ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "Single",                                                              ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "-TestLibrary.Generator.GetSingle()",                                                        ["ValidateFirstResult"] = "BitConverter.SingleToInt32Bits(result[0]) != BitConverter.SingleToInt32Bits(Math.Abs(firstOp[0]))",                                  ["ValidateRemainingResults"] = "BitConverter.SingleToInt32Bits(result[i]) != 0"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "Byte",    ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "Byte",   ["Op2VectorType"] = "Vector64",  ["Op2BaseType"] = "Byte",   ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "TestLibrary.Generator.GetByte()",   ["NextValueOp2"] = "TestLibrary.Generator.GetByte()",   ["ValidateFirstResult"] = "(byte)(left[0] + right[0]) != result[0]",                                                                                            ["ValidateRemainingResults"] = "(byte)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "Int16",   ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "Int16",  ["Op2VectorType"] = "Vector64",  ["Op2BaseType"] = "Int16",  ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "TestLibrary.Generator.GetInt16()",  ["NextValueOp2"] = "TestLibrary.Generator.GetInt16()",  ["ValidateFirstResult"] = "(short)(left[0] + right[0]) != result[0]",                                                                                           ["ValidateRemainingResults"] = "(short)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "Int32",   ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "Int32",  ["Op2VectorType"] = "Vector64",  ["Op2BaseType"] = "Int32",  ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "TestLibrary.Generator.GetInt32()",  ["NextValueOp2"] = "TestLibrary.Generator.GetInt32()",  ["ValidateFirstResult"] = "(int)(left[0] + right[0]) != result[0]",                                                                                             ["ValidateRemainingResults"] = "(int)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "SByte",   ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "SByte",  ["Op2VectorType"] = "Vector64",  ["Op2BaseType"] = "SByte",  ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "TestLibrary.Generator.GetSByte()",  ["NextValueOp2"] = "TestLibrary.Generator.GetSByte()",  ["ValidateFirstResult"] = "(sbyte)(left[0] + right[0]) != result[0]",                                                                                           ["ValidateRemainingResults"] = "(sbyte)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "Single",  ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "Single", ["Op2VectorType"] = "Vector64",  ["Op2BaseType"] = "Single", ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "TestLibrary.Generator.GetSingle()", ["NextValueOp2"] = "TestLibrary.Generator.GetSingle()", ["ValidateFirstResult"] = "BitConverter.SingleToInt32Bits(left[0] + right[0]) != BitConverter.SingleToInt32Bits(result[0])",                                    ["ValidateRemainingResults"] = "BitConverter.SingleToInt32Bits(left[i] + right[i]) != BitConverter.SingleToInt32Bits(result[i])"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "UInt16",  ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "UInt16", ["Op2VectorType"] = "Vector64",  ["Op2BaseType"] = "UInt16", ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "TestLibrary.Generator.GetUInt16()", ["NextValueOp2"] = "TestLibrary.Generator.GetUInt16()", ["ValidateFirstResult"] = "(ushort)(left[0] + right[0]) != result[0]",                                                                                          ["ValidateRemainingResults"] = "(ushort)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "UInt32",  ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "UInt32", ["Op2VectorType"] = "Vector64",  ["Op2BaseType"] = "UInt32", ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "TestLibrary.Generator.GetUInt32()", ["NextValueOp2"] = "TestLibrary.Generator.GetUInt32()", ["ValidateFirstResult"] = "(uint)(left[0] + right[0]) != result[0]",                                                                                            ["ValidateRemainingResults"] = "(uint)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "AddScalar",                                ["RetVectorType"] = "Vector64",  ["RetBaseType"] = "Single",  ["Op1VectorType"] = "Vector64",  ["Op1BaseType"] =  "Single", ["Op2VectorType"] = "Vector64",  ["Op2BaseType"] = "Single", ["LargestVectorSize"] = "8",  ["NextValueOp1"] = "TestLibrary.Generator.GetSingle()", ["NextValueOp2"] = "TestLibrary.Generator.GetSingle()", ["ValidateFirstResult"] = "BitConverter.SingleToInt32Bits(left[0] + right[0]) != BitConverter.SingleToInt32Bits(result[0])",                                    ["ValidateRemainingResults"] = "BitConverter.SingleToInt32Bits(result[i]) != 0"}),
};

private static readonly (string templateFileName, Dictionary<string, string> templateData)[] AdvSimd_Vector128Inputs = new []
{
    ("SimpleUnOpTest.template",   new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Abs",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "Byte",    ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "SByte",                                                               ["LargestVectorSize"] = "16", ["NextValueOp1"] = "(sbyte)-TestLibrary.Generator.GetSByte()",                                                         ["ValidateFirstResult"] = "result[0] != (byte)Math.Abs(firstOp[0])",                                                                                            ["ValidateRemainingResults"] = "result[i] != (byte)Math.Abs(firstOp[i])"}),
    ("SimpleUnOpTest.template",   new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Abs",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "UInt16",  ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "Int16",                                                               ["LargestVectorSize"] = "16", ["NextValueOp1"] = "(short)-TestLibrary.Generator.GetInt16()",                                                         ["ValidateFirstResult"] = "result[0] != (ushort)Math.Abs(firstOp[0])",                                                                                          ["ValidateRemainingResults"] = "result[i] != (ushort)Math.Abs(firstOp[i])"}),
    ("SimpleUnOpTest.template",   new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Abs",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "UInt32",  ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "Int32",                                                               ["LargestVectorSize"] = "16", ["NextValueOp1"] = "-TestLibrary.Generator.GetInt32()",                                                         ["ValidateFirstResult"] = "result[0] != (uint)Math.Abs(firstOp[0])",                                                                                            ["ValidateRemainingResults"] = "result[i] != (uint)Math.Abs(firstOp[i])"}),
    ("SimpleUnOpTest.template",   new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Abs",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "Single",  ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "Single",                                                              ["LargestVectorSize"] = "16", ["NextValueOp1"] = "-TestLibrary.Generator.GetSingle()",                                                        ["ValidateFirstResult"] = "BitConverter.SingleToInt32Bits(result[0]) != BitConverter.SingleToInt32Bits(Math.Abs(firstOp[0]))",                                  ["ValidateRemainingResults"] = "BitConverter.SingleToInt32Bits(result[i]) != BitConverter.SingleToInt32Bits(Math.Abs(firstOp[i]))"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "Byte",    ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "Byte",   ["Op2VectorType"] = "Vector128", ["Op2BaseType"] = "Byte",   ["LargestVectorSize"] = "16", ["NextValueOp1"] = "TestLibrary.Generator.GetByte()",   ["NextValueOp2"] = "TestLibrary.Generator.GetByte()",   ["ValidateFirstResult"] = "(byte)(left[0] + right[0]) != result[0]",                                                                                            ["ValidateRemainingResults"] = "(byte)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "Int16",   ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "Int16",  ["Op2VectorType"] = "Vector128", ["Op2BaseType"] = "Int16",  ["LargestVectorSize"] = "16", ["NextValueOp1"] = "TestLibrary.Generator.GetInt16()",  ["NextValueOp2"] = "TestLibrary.Generator.GetInt16()",  ["ValidateFirstResult"] = "(short)(left[0] + right[0]) != result[0]",                                                                                           ["ValidateRemainingResults"] = "(short)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "Int32",   ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "Int32",  ["Op2VectorType"] = "Vector128", ["Op2BaseType"] = "Int32",  ["LargestVectorSize"] = "16", ["NextValueOp1"] = "TestLibrary.Generator.GetInt32()",  ["NextValueOp2"] = "TestLibrary.Generator.GetInt32()",  ["ValidateFirstResult"] = "(int)(left[0] + right[0]) != result[0]",                                                                                             ["ValidateRemainingResults"] = "(int)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "Int64",   ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "Int64",  ["Op2VectorType"] = "Vector128", ["Op2BaseType"] = "Int64",  ["LargestVectorSize"] = "16", ["NextValueOp1"] = "TestLibrary.Generator.GetInt64()",  ["NextValueOp2"] = "TestLibrary.Generator.GetInt64()",  ["ValidateFirstResult"] = "(long)(left[0] + right[0]) != result[0]",                                                                                            ["ValidateRemainingResults"] = "(long)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "SByte",   ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "SByte",  ["Op2VectorType"] = "Vector128", ["Op2BaseType"] = "SByte",  ["LargestVectorSize"] = "16", ["NextValueOp1"] = "TestLibrary.Generator.GetSByte()",  ["NextValueOp2"] = "TestLibrary.Generator.GetSByte()",  ["ValidateFirstResult"] = "(sbyte)(left[0] + right[0]) != result[0]",                                                                                           ["ValidateRemainingResults"] = "(sbyte)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "Single",  ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "Single", ["Op2VectorType"] = "Vector128", ["Op2BaseType"] = "Single", ["LargestVectorSize"] = "16", ["NextValueOp1"] = "TestLibrary.Generator.GetSingle()", ["NextValueOp2"] = "TestLibrary.Generator.GetSingle()", ["ValidateFirstResult"] = "BitConverter.SingleToInt32Bits(left[0] + right[0]) != BitConverter.SingleToInt32Bits(result[0])",                                    ["ValidateRemainingResults"] = "BitConverter.SingleToInt32Bits(left[i] + right[i]) != BitConverter.SingleToInt32Bits(result[i])"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "UInt16",  ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "UInt16", ["Op2VectorType"] = "Vector128", ["Op2BaseType"] = "UInt16", ["LargestVectorSize"] = "16", ["NextValueOp1"] = "TestLibrary.Generator.GetUInt16()", ["NextValueOp2"] = "TestLibrary.Generator.GetUInt16()", ["ValidateFirstResult"] = "(ushort)(left[0] + right[0]) != result[0]",                                                                                          ["ValidateRemainingResults"] = "(ushort)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "UInt32",  ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "UInt32", ["Op2VectorType"] = "Vector128", ["Op2BaseType"] = "UInt32", ["LargestVectorSize"] = "16", ["NextValueOp1"] = "TestLibrary.Generator.GetUInt32()", ["NextValueOp2"] = "TestLibrary.Generator.GetUInt32()", ["ValidateFirstResult"] = "(uint)(left[0] + right[0]) != result[0]",                                                                                            ["ValidateRemainingResults"] = "(uint)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                      ["RetVectorType"] = "Vector128", ["RetBaseType"] = "UInt64",  ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "UInt64", ["Op2VectorType"] = "Vector128", ["Op2BaseType"] = "UInt64", ["LargestVectorSize"] = "16", ["NextValueOp1"] = "TestLibrary.Generator.GetUInt64()", ["NextValueOp2"] = "TestLibrary.Generator.GetUInt64()", ["ValidateFirstResult"] = "(ulong)(left[0] + right[0]) != result[0]",                                                                                           ["ValidateRemainingResults"] = "(ulong)(left[i] + right[i]) != result[i]"}),
};

private static readonly (string templateFileName, Dictionary<string, string> templateData)[] AdvSimd_Arm64_Vector128Inputs = new []
{
    ("SimpleUnOpTest.template",   new Dictionary<string, string> { ["Isa"] = "AdvSimd.Arm64", ["LoadIsa"] = "AdvSimd", ["Method"] = "Abs",                                ["RetVectorType"] = "Vector128", ["RetBaseType"] = "Double",  ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "Double",                                                              ["LargestVectorSize"] = "16", ["NextValueOp1"] = "-TestLibrary.Generator.GetDouble()",                                                        ["ValidateFirstResult"] = "BitConverter.DoubleToInt64Bits(result[0]) != BitConverter.DoubleToInt64Bits(Math.Abs(firstOp[0]))",                                  ["ValidateRemainingResults"] = "BitConverter.DoubleToInt64Bits(result[i]) != BitConverter.DoubleToInt64Bits(Math.Abs(firstOp[i]))"}),
    ("SimpleUnOpTest.template",   new Dictionary<string, string> { ["Isa"] = "AdvSimd.Arm64", ["LoadIsa"] = "AdvSimd", ["Method"] = "Abs",                                ["RetVectorType"] = "Vector128", ["RetBaseType"] = "UInt64",  ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "Int64",                                                               ["LargestVectorSize"] = "16", ["NextValueOp1"] = "-TestLibrary.Generator.GetInt64()",                                                         ["ValidateFirstResult"] = "result[0] != (ulong)Math.Abs(firstOp[0])",                                                                                           ["ValidateRemainingResults"] = "result[i] != (ulong)Math.Abs(firstOp[i])"}),
 // ("SimpleBinOpTest.template",  new Dictionary<string, string> { ["Isa"] = "AdvSimd.Arm64", ["LoadIsa"] = "AdvSimd", ["Method"] = "Add",                                ["RetVectorType"] = "Vector128", ["RetBaseType"] = "Double",  ["Op1VectorType"] = "Vector128", ["Op1BaseType"] =  "Double", ["Op2VectorType"] = "Vector128", ["Op2BaseType"] = "Double", ["LargestVectorSize"] = "16", ["NextValueOp1"] = "TestLibrary.Generator.GetDouble()", ["NextValueOp2"] = "TestLibrary.Generator.GetDouble()", ["ValidateFirstResult"] = "BitConverter.DoubleToInt64Bits(left[0] + right[0]) != BitConverter.DoubleToInt64Bits(result[0])",                                    ["ValidateRemainingResults"] = "BitConverter.DoubleToInt64Bits(left[i] + right[i]) != BitConverter.DoubleToInt64Bits(result[i])"}),
};

private static void ProcessInputs(string groupName, (string templateFileName, Dictionary<string, string> templateData)[] inputs)
{
    var testListFileName = Path.Combine("..", groupName, $"Program.{groupName}.cs");

    using (var testListFile = new StreamWriter(testListFileName, append: false))
    {
        testListFile.WriteLine(@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {");

        foreach (var input in inputs)
        {
            ProcessInput(testListFile, groupName, input);
        }

        testListFile.WriteLine(@"            };
        }
    }
}");
    }
}

private static void ProcessInput(StreamWriter testListFile, string groupName, (string templateFileName, Dictionary<string, string> templateData) input)

{
    var testName = $"{input.templateData["Method"]}.{input.templateData["RetBaseType"]}";
    var suffix = "";

    // Ex: ["Add.Single"] = AddSingle
    testListFile.WriteLine($@"                [""{testName}""] = {input.templateData["Method"]}{input.templateData["RetBaseType"]}{suffix},");

    var testFileName = Path.Combine("..", groupName, $"{testName}.cs");
    var matchingTemplate = Templates.Where((t) => t.outputTemplateName.Equals(input.templateFileName)).SingleOrDefault();
    var template = string.Empty;

    if (matchingTemplate.templateFileName is null)
    {
        template = File.ReadAllText(input.templateFileName);
    }
    else
    {
        template = File.ReadAllText(matchingTemplate.templateFileName);

        foreach (var kvp in matchingTemplate.templateData)
        {
            template = template.Replace($"{{{kvp.Key}}}", kvp.Value);
        }
    }

    foreach (var kvp in input.templateData)
    {
        template = template.Replace($"{{{kvp.Key}}}", kvp.Value);
    }

    File.WriteAllText(testFileName, template);
}

ProcessInputs("AdvSimd_Vector64", AdvSimd_Vector64Inputs);
ProcessInputs("AdvSimd_Vector128", AdvSimd_Vector128Inputs);
ProcessInputs("AdvSimd.Arm64_Vector128", AdvSimd_Arm64_Vector128Inputs);
