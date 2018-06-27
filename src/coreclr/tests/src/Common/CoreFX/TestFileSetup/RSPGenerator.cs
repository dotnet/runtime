// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CoreFX.TestUtils.TestFileSetup
{
    /// <summary>
    /// A class which generates .rsp files to be passed to the test executable
    /// The file contains a list of methods, classes and namespaces to be excluded from running.
    /// </summary>
    public class RSPGenerator
    {
        /// <summary>
        /// Generate an rsp file from an XUnitTestAssembly class
        /// </summary>
        /// <param name="testDefinition">The XUnitTestAssembly object parsed from a specified test list</param>
        /// <param name="outputPath">Path to which to output a .rsp file</param>
        public void GenerateRSPFile(XUnitTestAssembly testDefinition, string outputPath)
        {
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            string rspFilePath = Path.Combine(outputPath, testDefinition.Name + ".rsp");

            if (File.Exists(rspFilePath))
                File.Delete(rspFilePath);

            // Write RSP file to disk
            using (StreamWriter sr = File.CreateText(rspFilePath))
            {
                // If no exclusions are defined, we don't need to generate an .rsp file
                if (testDefinition.Exclusions == null)
                    return;

                // Method exclusions
                if (testDefinition.Exclusions.Methods != null)
                {
                    foreach (Exclusion exclusion in testDefinition.Exclusions.Methods)
                    {
                        if (String.IsNullOrWhiteSpace(exclusion.Name))
                            continue;
                        sr.Write("-skipmethod ");
                        sr.Write(exclusion.Name);
                        sr.WriteLine();
                    }
                }

                // Class exclusions
                if (testDefinition.Exclusions.Classes != null)
                {
                    foreach (Exclusion exclusion in testDefinition.Exclusions.Classes)
                    {
                        if (String.IsNullOrWhiteSpace(exclusion.Name))
                            continue;
                        sr.Write("-skipclass ");
                        sr.Write(exclusion.Name);
                        sr.WriteLine();
                    }

                }

                // Namespace exclusions
                if (testDefinition.Exclusions.Namespaces != null)
                {
                    foreach (Exclusion exclusion in testDefinition.Exclusions.Namespaces)
                    {
                        if (String.IsNullOrWhiteSpace(exclusion.Name))
                            continue;
                        sr.Write("-skipnamespace ");
                        sr.Write(exclusion.Name);
                        sr.WriteLine();
                    }
                }
            }
        }
    }
}
