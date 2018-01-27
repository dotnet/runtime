// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

// NOTE: This file isn't very robust and makes several assumptions.
//       It must be run such that its own directory is the working directory.

private static readonly (string templateFileName, string[] templateData)[] Sse2Inputs = new []
{
    // TemplateName                             Isa,    Method, BaseType, VectorType,  VectorSize, NextValue,                                              ValidateFirstResult,                                                                               ValidateRemainingResults
    ("SimpleBinOpTest.template", new string[] { "Sse2", "Add",  "Double", "Vector128", "16",       "(double)(random.NextDouble())",                        "BitConverter.DoubleToInt64Bits(left[0] + right[0]) != BitConverter.DoubleToInt64Bits(result[0])", "BitConverter.DoubleToInt64Bits(left[i] + right[i]) != BitConverter.DoubleToInt64Bits(result[i])"}),
    ("SimpleBinOpTest.template", new string[] { "Sse2", "Add",  "Byte",   "Vector128", "16",       "(byte)(random.Next(0, byte.MaxValue))",                "(byte)(left[0] + right[0]) != result[0]",                                                         "(byte)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template", new string[] { "Sse2", "Add",  "Int16",  "Vector128", "16",       "(short)(random.Next(short.MinValue, short.MaxValue))", "(short)(left[0] + right[0]) != result[0]",                                                        "(short)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template", new string[] { "Sse2", "Add",  "Int32",  "Vector128", "16",       "(int)(random.Next(int.MinValue, int.MaxValue))",       "(int)(left[0] + right[0]) != result[0]",                                                          "(int)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template", new string[] { "Sse2", "Add",  "Int64",  "Vector128", "16",       "(long)(random.Next(int.MinValue, int.MaxValue))",      "(long)(left[0] + right[0]) != result[0]",                                                         "(long)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template", new string[] { "Sse2", "Add",  "SByte",  "Vector128", "16",       "(sbyte)(random.Next(sbyte.MinValue, sbyte.MaxValue))", "(sbyte)(left[0] + right[0]) != result[0]",                                                        "(sbyte)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template", new string[] { "Sse2", "Add",  "UInt16", "Vector128", "16",       "(ushort)(random.Next(0, ushort.MaxValue))",            "(ushort)(left[0] + right[0]) != result[0]",                                                       "(ushort)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template", new string[] { "Sse2", "Add",  "UInt32", "Vector128", "16",       "(uint)(random.Next(0, int.MaxValue))",                 "(uint)(left[0] + right[0]) != result[0]",                                                         "(uint)(left[i] + right[i]) != result[i]"}),
    ("SimpleBinOpTest.template", new string[] { "Sse2", "Add",  "UInt64", "Vector128", "16",       "(ulong)(random.Next(0, int.MaxValue))",                "(ulong)(left[0] + right[0]) != result[0]",                                                        "(ulong)(left[i] + right[i]) != result[i]"}),
};

private static readonly (string templateFileName, string[] templateData)[] AvxInputs = new []
{
    // TemplateName                             Isa,    Method,     BaseType, VectorType,  VectorSize, NextValue,                      ValidateFirstResult,                                                                               ValidateRemainingResults
    ("SimpleBinOpTest.template", new string[] { "Avx", "Add",      "Double", "Vector256", "32",       "(double)(random.NextDouble())", "BitConverter.DoubleToInt64Bits(left[0] + right[0]) != BitConverter.DoubleToInt64Bits(result[0])", "BitConverter.DoubleToInt64Bits(left[i] + right[i]) != BitConverter.DoubleToInt64Bits(result[i])"}),
    ("SimpleBinOpTest.template", new string[] { "Avx", "Add",      "Single", "Vector256", "32",       "(float)(random.NextDouble())",  "BitConverter.SingleToInt32Bits(left[0] + right[0]) != BitConverter.SingleToInt32Bits(result[0])", "BitConverter.SingleToInt32Bits(left[i] + right[i]) != BitConverter.SingleToInt32Bits(result[i])"}),
    ("SimpleBinOpTest.template", new string[] { "Avx", "Multiply", "Double", "Vector256", "32",       "(double)(random.NextDouble())", "BitConverter.DoubleToInt64Bits(left[0] * right[0]) != BitConverter.DoubleToInt64Bits(result[0])", "BitConverter.DoubleToInt64Bits(left[i] * right[i]) != BitConverter.DoubleToInt64Bits(result[i])"}),
    ("SimpleBinOpTest.template", new string[] { "Avx", "Multiply", "Single", "Vector256", "32",       "(float)(random.NextDouble())",  "BitConverter.SingleToInt32Bits(left[0] * right[0]) != BitConverter.SingleToInt32Bits(result[0])", "BitConverter.SingleToInt32Bits(left[i] * right[i]) != BitConverter.SingleToInt32Bits(result[i])"}),
};

private static void ProcessInputs(string isa, (string templateFileName, string[] templateData)[] inputs)
{
    var testListFileName = Path.Combine("..", isa, $"Program.{isa}.cs");

    using (var testListFile = new StreamWriter(testListFileName, append: false))
    {
        testListFile.WriteLine(@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {");

        foreach (var input in inputs)
        {
            ProcessInput(testListFile, input);
        }

        testListFile.WriteLine(@"            };
        }
    }
}");
    }
}

private static void ProcessInput(StreamWriter testListFile, (string templateFileName, string[] templateData) input)
{
    var testName = $"{input.templateData[1]}.{input.templateData[2]}";

    // Ex: ["Add.Single"] = AddSingle
    testListFile.WriteLine($@"                [""{testName}""] = {input.templateData[1]}{input.templateData[2]},");

    var testFileName = Path.Combine("..", input.templateData[0], $"{testName}.cs");
    var template = File.ReadAllText(input.templateFileName);

    if (input.templateData.Length != 0)
    {
        template = string.Format(template, input.templateData);
    }

    File.WriteAllText(testFileName, template);
}

ProcessInputs("Sse2", Sse2Inputs);
ProcessInputs("Avx", AvxInputs);
