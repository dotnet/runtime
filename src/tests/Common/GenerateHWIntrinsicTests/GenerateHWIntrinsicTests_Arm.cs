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

class GenerateHWIntrinsicTests_Arm
{
    static void Main(string[] args)
    {
        string projectName = args[0];
        string templateDirectory = args[1];
        string outputDirectory = args[2];
        string testListFileName = args[3];

        ProcessInputs("AdvSimd", AdvSimdTests.AdvSimdInputs);
        ProcessInputs("AdvSimd.Arm64", AdvSimdTests.AdvSimd_Arm64Inputs);
        ProcessInputs("Aes", AdvSimdTests.AesInputs);
        ProcessInputs("ArmBase", BaseTests.ArmBaseInputs);
        ProcessInputs("ArmBase.Arm64", BaseTests.ArmBase_Arm64Inputs);
        ProcessInputs("Crc32", BaseTests.Crc32Inputs);
        ProcessInputs("Crc32.Arm64", BaseTests.Crc32_Arm64Inputs);
        ProcessInputs("Dp", AdvSimdTests.DpInputs);
        ProcessInputs("Rdm", AdvSimdTests.RdmInputs);
        ProcessInputs("Rdm.Arm64", AdvSimdTests.Rdm_Arm64Inputs);
        ProcessInputs("Sha1", AdvSimdTests.Sha1Inputs);
        ProcessInputs("Sha256", AdvSimdTests.Sha256Inputs);
        ProcessInputs("Sha256", AdvSimdTests.Sha256Inputs);
        ProcessInputs("Sve", SveTests.SveInputs);
        ProcessInputs("Sve2", Sve2Tests.Sve2Inputs);

        void ProcessInputs(string groupName, (string templateFileName, Dictionary<string, string> templateData)[] inputs)
        {
            if (!projectName.Equals($"{groupName}_r") && !projectName.Equals($"{groupName}_ro"))
            {
                return;
            }

            Directory.CreateDirectory(outputDirectory);

            using (var testListFile = new StreamWriter(testListFileName, append: false))
            {
                foreach (var input in inputs)
                {
                    ProcessInput(testListFile, groupName, input);
                }
            }
        }
        void ProcessInput(StreamWriter testListFile, string groupName, (string templateFileName, Dictionary<string, string> templateData) input)
        {
            var testName = input.templateData["TestName"];
            var fileName = Path.Combine(outputDirectory, $"{testName.Replace('_', '.')}.cs");

            var matchingTemplate = TestTemplates.Templates.Where((t) => t.outputTemplateName.Equals(input.templateFileName)).SingleOrDefault();
            var template = string.Empty;

            if (matchingTemplate.templateFileName is null)
            {
                string templateFileName = Path.Combine(templateDirectory, input.templateFileName);
                template = File.ReadAllText(templateFileName);
            }
            else
            {
                string templateFileName = Path.Combine(templateDirectory, matchingTemplate.templateFileName);
                template = File.ReadAllText(templateFileName);

                foreach (var kvp in matchingTemplate.templateData)
                {
                    template = template.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
            }

            foreach (var kvp in input.templateData)
            {
                template = template.Replace($"{{{kvp.Key}}}", kvp.Value);
            }
            template = template.Replace("namespace JIT.HardwareIntrinsics.Arm", $"namespace JIT.HardwareIntrinsics.Arm._{groupName}");

            testListFile.WriteLine(fileName);
            File.WriteAllText(fileName, template);
        }
    }
}
