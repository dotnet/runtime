// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// DIRECTIONS:
//    This file isn't very robust and makes several assumptions
//    It executes automatically when building the relevant hwintrinsic test projects
//
//    New tests can be generated from the template by adding an entry to the
//    appropriate Inputs array in Arm/*Tests.cs
//
//    You can support a new Isa by creating a new array and adding a new
//    "ProcessInputs" call at the bottom of the script.
//
//    There are currently four inputs expected in the following order
//    * projectName       - This should be '$(MSBuildProjectName)' and is used to filter which tests are being generated
//    * templateDirectory - This should be '$(MSBuildThisFileDirectory)Shared' and is used to locate the test templates
//    * outputDirectory   - This should be somewhere under the obj folder for the project and is where generated tests are written
//    * testListFileName  - This should likewise be somewhere under the obj folder and is where the list of generated tests is written

public struct TestGroup
{
    public string Isa { get; set; }
    public string LoadIsa { get; set; }
    public (TemplateConfig TemplateConfig, Dictionary<string, string> KeyValuePairs)[] Tests { get; set; }

    public TestGroup(string Isa, string LoadIsa, (TemplateConfig, Dictionary<string, string>)[] Tests)
    {
        this.Isa = Isa;
        this.LoadIsa = LoadIsa;
        this.Tests = Tests;
    }

    public (TemplateConfig, Dictionary<string, string>)[] GetTests()
    {
        var self = this;
        return Tests.Select(t =>
        {
            var kvp = new Dictionary<string, string>(t.KeyValuePairs);
            if (!string.IsNullOrEmpty(self.Isa)) kvp["Isa"] = self.Isa;
            if (!string.IsNullOrEmpty(self.LoadIsa)) kvp["LoadIsa"] = self.LoadIsa;
            kvp["Namespace"] = $"JIT.HardwareIntrinsics.Arm._{self.Isa}";
            return (t.TemplateConfig, kvp);
        }).ToArray();
    }
}

public struct TemplateConfig
{
    public string Filename { get; }
    public Dictionary<string, string> KeyValuePairs { get; }

    public TemplateConfig(
        string filename,
        string configurationName = null,
        string templateValidationLogic = null,
        string templateValidationLogicForCndSel = null,
        string templateValidationLogicForCndSel_FalseValue = null,
        string templateValidationLogicForCndSelMask = null)
    {
        Filename = filename;
        KeyValuePairs = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(configurationName))
            KeyValuePairs["TemplateName"] = configurationName;

        if (!string.IsNullOrEmpty(templateValidationLogic))
            KeyValuePairs["TemplateValidationLogic"] = templateValidationLogic;

        if (!string.IsNullOrEmpty(templateValidationLogicForCndSel))
            KeyValuePairs["TemplateValidationLogicForCndSel"] = templateValidationLogicForCndSel;

        if (!string.IsNullOrEmpty(templateValidationLogicForCndSel_FalseValue))
            KeyValuePairs["TemplateValidationLogicForCndSel_FalseValue"] = templateValidationLogicForCndSel_FalseValue;

        if (!string.IsNullOrEmpty(templateValidationLogicForCndSelMask))
            KeyValuePairs["TemplateValidationLogicForCndSelMask"] = templateValidationLogicForCndSelMask;
    }
}

class GenerateHWIntrinsicTests_Arm
{
    static void Main(string[] args)
    {
        string projectName = args[0];
        string templateDirectory = args[1];
        string outputDirectory = args[2];
        string testListFileName = args[3];

        ProcessInputs(AdvSimdTests.AdvSimdInputs);
        ProcessInputs(AdvSimdTests.AdvSimd_Arm64Inputs);
        ProcessInputs(AdvSimdTests.AesInputs);
        ProcessInputs(BaseTests.Crc32_Arm64Inputs);
        ProcessInputs(AdvSimdTests.DpInputs);
        ProcessInputs(AdvSimdTests.RdmInputs);
        ProcessInputs(AdvSimdTests.Rdm_Arm64Inputs);
        ProcessInputs(AdvSimdTests.Sha1Inputs);
        ProcessInputs(AdvSimdTests.Sha256Inputs);
        ProcessInputs(AdvSimdTests.Sha256Inputs);
        ProcessInputs(BaseTests.ArmBaseInputs);
        ProcessInputs(BaseTests.ArmBase_Arm64Inputs);
        ProcessInputs(BaseTests.Crc32Inputs);
        ProcessInputs(SveTests.SveInputs);
        ProcessInputs(Sve2Tests.Sve2Inputs);

        void ProcessInputs(TestGroup testGroup)
        {
            if (!projectName.Equals($"{testGroup.Isa}_r") && !projectName.Equals($"{testGroup.Isa}_ro"))
            {
                return;
            }

            Directory.CreateDirectory(outputDirectory);

            using (var testListFile = new StreamWriter(testListFileName, append: false))
            {
                foreach (var test in testGroup.GetTests())
                {
                    ProcessTest(testListFile, test);
                }
            }
        }

        void ProcessTest(StreamWriter testListFile, (TemplateConfig templateConfig, Dictionary<string, string> keyValuePairs) test)
        {
            var testName = test.keyValuePairs["TestName"];
            var fileName = Path.Combine(outputDirectory, $"{testName.Replace('_', '.')}.cs");
            var template = string.Empty;
            string templateFilePath = Path.Combine(templateDirectory, test.templateConfig.Filename);
            template = File.ReadAllText(templateFilePath);

            foreach (var kvp in test.templateConfig.KeyValuePairs)
            {
                template = template.Replace($"{{{kvp.Key}}}", kvp.Value);
            }

            foreach (var kvp in test.keyValuePairs)
            {
                template = template.Replace($"{{{kvp.Key}}}", kvp.Value);
            }

            testListFile.WriteLine(fileName);
            File.WriteAllText(fileName, template);
        }
    }
}
