// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;

namespace R2RDump
{
    /// <summary>
    /// Helper class for diffing a pair of R2R images.
    /// </summary>
    class R2RDiff
    {
        /// <summary>
        /// Left R2R image for the diff.
        /// </summary>
        private readonly R2RReader _leftFile;

        /// <summary>
        /// Right R2R image for the diff.
        /// </summary>
        private readonly R2RReader _rightFile;

        /// <summary>
        /// Text writer to receive diff output.
        /// </summary>
        private readonly TextWriter _writer;

        /// <summary>
        /// Store the left and right file and output writer.
        /// </summary>
        /// <param name="leftFile">Left R2R file</param>
        /// <param name="rightFile">Right R2R file</param>
        /// <param name="writer">Output writer to receive the diff</param>
        public R2RDiff(R2RReader leftFile, R2RReader rightFile, TextWriter writer)
        {
            _leftFile = leftFile;
            _rightFile = rightFile;
            _writer = writer;
        }

        /// <summary>
        /// Public API runs all available diff algorithms in sequence.
        /// </summary>
        public void Run()
        {
            DiffTitle();
            DiffPESections();
            DiffR2RSections();
            DiffR2RMethods();
        }

        /// <summary>
        /// Diff title shows the names of the files being compared and their lengths.
        /// </summary>
        private void DiffTitle()
        {
            _writer.WriteLine($@"Left file:  {_leftFile.Filename} ({_leftFile.Image.Length} B)");
            _writer.WriteLine($@"Right file: {_rightFile.Filename} ({_rightFile.Image.Length} B)");
            _writer.WriteLine();
        }

        /// <summary>
        /// Diff raw PE sections.
        /// </summary>
        private void DiffPESections()
        {
            ShowDiff(GetPESectionMap(_leftFile), GetPESectionMap(_rightFile), "PE sections");
        }

        /// <summary>
        /// Diff R2R header sections.
        /// </summary>
        private void DiffR2RSections()
        {
            ShowDiff(GetR2RSectionMap(_leftFile), GetR2RSectionMap(_rightFile), "R2R sections");
        }

        /// <summary>
        /// Diff the R2R method maps.
        /// </summary>
        private void DiffR2RMethods()
        {
            ShowDiff(GetR2RMethodMap(_leftFile), GetR2RMethodMap(_rightFile), "R2R methods");
        }

        /// <summary>
        /// Show a difference summary between the sets of "left objects" and "right objects".
        /// </summary>
        /// <param name="leftObjects">Dictionary of left object sizes keyed by their names</param>
        /// <param name="rightObjects">Dictionary of right object sizes keyed by their names</param>
        /// <param name="diffName">Logical name of the diffing operation to display in the header line</param>
        private void ShowDiff(Dictionary<string, int> leftObjects, Dictionary<string, int> rightObjects, string diffName)
        {
            HashSet<string> allKeys = new HashSet<string>(leftObjects.Keys);
            allKeys.UnionWith(rightObjects.Keys);

            string title = $@" LEFT_SIZE RIGHT_SIZE       DIFF  {diffName} ({allKeys.Count} ELEMENTS)";

            _writer.WriteLine(title);
            _writer.WriteLine(new string('-', title.Length));

            int leftTotal = 0;
            int rightTotal = 0;
            foreach (string key in allKeys)
            {
                int leftSize;
                bool inLeft = leftObjects.TryGetValue(key, out leftSize);
                int rightSize;
                bool inRight = rightObjects.TryGetValue(key, out rightSize);

                leftTotal += leftSize;
                rightTotal += rightSize;

                StringBuilder line = new StringBuilder();
                if (inLeft)
                {
                    line.AppendFormat("{0,10}", leftSize);
                }
                else
                {
                    line.Append(' ', 10);
                }
                if (inRight)
                {
                    line.AppendFormat("{0,11}", rightSize);
                }
                else
                {
                    line.Append(' ', 11);
                }
                if (leftSize != rightSize)
                {
                    line.AppendFormat("{0,11}", rightSize - leftSize);
                }
                else
                {
                    line.Append(' ', 11);
                }
                line.Append("  ");
                line.Append(key);
                _writer.WriteLine(line);
            }
            _writer.WriteLine($@"{leftTotal,10} {rightTotal,10} {(rightTotal - leftTotal),10}  <TOTAL>");

            _writer.WriteLine();
        }

        /// <summary>
        /// Read the PE file section map for a given R2R image.
        /// </summary>
        /// <param name="reader">R2R image to scan</param>
        /// <returns></returns>
        private Dictionary<string, int> GetPESectionMap(R2RReader reader)
        {
            Dictionary<string, int> sectionMap = new Dictionary<string, int>();

            foreach (SectionHeader sectionHeader in reader.PEReader.PEHeaders.SectionHeaders)
            {
                sectionMap.Add(sectionHeader.Name, sectionHeader.SizeOfRawData);
            }

            return sectionMap;
        }

        /// <summary>
        /// Read the R2R header section map for a given R2R image.
        /// </summary>
        /// <param name="reader">R2R image to scan</param>
        /// <returns></returns>
        private Dictionary<string, int> GetR2RSectionMap(R2RReader reader)
        {
            Dictionary<string, int> sectionMap = new Dictionary<string, int>();

            foreach (KeyValuePair<R2RSection.SectionType, R2RSection> typeAndSection in reader.R2RHeader.Sections)
            {
                string name = typeAndSection.Key.ToString();
                sectionMap.Add(name, typeAndSection.Value.Size);
            }

            return sectionMap;
        }

        /// <summary>
        /// Read the R2R method map for a given R2R image.
        /// </summary>
        /// <param name="reader">R2R image to scan</param>
        /// <returns></returns>
        private Dictionary<string, int> GetR2RMethodMap(R2RReader reader)
        {
            Dictionary<string, int> methodMap = new Dictionary<string, int>();

            foreach (R2RMethod method in reader.R2RMethods)
            {
                int size = method.RuntimeFunctions.Sum(rf => rf.Size);
                methodMap.Add(method.SignatureString, size);
            }

            return methodMap;
        }
    }
}
