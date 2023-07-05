// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using ILCompiler.Reflection.ReadyToRun;
using Internal.Runtime;

namespace R2RDump
{
    /// <summary>
    /// Helper class for diffing a pair of R2R images.
    /// </summary>
    internal sealed class R2RDiff
    {
        private const int InvalidModule = -1;
        private const int AllModules = -2;

        /// <summary>
        /// Left dumper to use for the diff
        /// </summary>
        private readonly Dumper _leftDumper;

        /// <summary>
        /// Right dumper to use for the diff
        /// </summary>
        private readonly Dumper _rightDumper;

        /// <summary>
        /// Text writer to use for common output
        /// </summary>
        private readonly TextWriter _writer;

        /// <summary>
        /// Store the left and right file and output writer.
        /// </summary>
        /// <param name="leftDumper">Dumper to use for the left diff output</param>
        /// <param name="rightDumper">Dumper to use for the right diff output</param>
        /// <param name="writer">Writer to use for output common to left / right side</param>
        public R2RDiff(Dumper leftDumper, Dumper rightDumper, TextWriter writer)
        {
            _leftDumper = leftDumper;
            _rightDumper = rightDumper;
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
            DiffImportSections();
            DiffR2RMethods();

            DiffMethodsForModule(AllModules, AllModules);

            if (_leftDumper.Reader.Composite && _rightDumper.Reader.Composite)
            {
                HashSet<string> allComponentAssemblies = new HashSet<string>(_leftDumper.Reader.ManifestReferenceAssemblies.Keys);
                allComponentAssemblies.UnionWith(_rightDumper.Reader.ManifestReferenceAssemblies.Keys);
                foreach (string assemblyName in allComponentAssemblies.OrderBy(name => name))
                {
                    int leftModuleIndex = _leftDumper.Reader.ManifestReferenceAssemblies[assemblyName];
                    int rightModuleIndex = _rightDumper.Reader.ManifestReferenceAssemblies[assemblyName];
                    DiffMethodsForModule(leftModuleIndex, rightModuleIndex);
                }
            }
        }

        private IEnumerable<ReadyToRunMethod> TryGetMethods(ReadyToRunReader reader, int moduleIndex)
        {
            List<ReadyToRunMethod> methods = new List<ReadyToRunMethod>();
            switch (moduleIndex)
            {
                case InvalidModule:
                    break;

                case AllModules:
                    methods.AddRange(reader.InstanceMethods.Select(im => im.Method));
                    foreach (ReadyToRunAssembly assembly in reader.ReadyToRunAssemblies)
                    {
                        methods.AddRange(assembly.Methods);
                    }
                    break;

                default:
                    methods.AddRange(reader.ReadyToRunAssemblies[moduleIndex].Methods);
                    break;
            }
            return methods;
        }

        private void DiffMethodsForModule(int leftModuleIndex, int rightModuleIndex)
        {
            IEnumerable<ReadyToRunMethod> leftSectionMethods = TryGetMethods(_leftDumper.Reader, leftModuleIndex);
            IEnumerable<ReadyToRunMethod> rightSectionMethods = TryGetMethods(_rightDumper.Reader, rightModuleIndex);

            Dictionary<string, ReadyToRunMethod> leftMethods = new Dictionary<string, ReadyToRunMethod>(leftSectionMethods
                .Select(method => new KeyValuePair<string, ReadyToRunMethod>(method.SignatureString, method)));
            Dictionary<string, ReadyToRunMethod> rightMethods = new Dictionary<string, ReadyToRunMethod>(rightSectionMethods
                .Select(method => new KeyValuePair<string, ReadyToRunMethod>(method.SignatureString, method)));
            Dictionary<string, MethodPair> commonMethods = new Dictionary<string, MethodPair>(leftMethods
                .Select(kvp => new KeyValuePair<string, MethodPair>(kvp.Key,
                    new MethodPair(kvp.Value, rightMethods.TryGetValue(kvp.Key, out ReadyToRunMethod rightMethod) ? rightMethod : null)))
                .Where(kvp => kvp.Value.RightMethod != null));
            if (_leftDumper.Model.DiffHideSameDisasm)
            {
                commonMethods = new Dictionary<string, MethodPair>(HideMethodsWithSameDisassembly(commonMethods));
            }
            DumpCommonMethods(_leftDumper, leftModuleIndex, commonMethods);
            DumpCommonMethods(_rightDumper, rightModuleIndex, commonMethods);
        }

        /// <summary>
        /// Diff title shows the names of the files being compared and their lengths.
        /// </summary>
        private void DiffTitle()
        {
            _writer.WriteLine($@"Left file:  {_leftDumper.Reader.Filename} ({_leftDumper.Reader.Image.Length} B)");
            _writer.WriteLine($@"Right file: {_rightDumper.Reader.Filename} ({_rightDumper.Reader.Image.Length} B)");
            _writer.WriteLine();
        }

        /// <summary>
        /// Diff raw PE sections.
        /// </summary>
        private void DiffPESections()
        {
            ShowDiff(GetPESectionMap(_leftDumper.Reader), GetPESectionMap(_rightDumper.Reader), "PE sections");
        }

        /// <summary>
        /// Diff R2R header sections.
        /// </summary>
        private void DiffR2RSections()
        {
            ShowDiff(GetR2RSectionMap(_leftDumper.Reader), GetR2RSectionMap(_rightDumper.Reader), "R2R sections");
        }

        private void DiffImportSections()
        {
            Dictionary<string, ReadyToRunImportSection.ImportSectionEntry> leftImports = GetImports(_leftDumper.Reader);
            Dictionary<string, ReadyToRunImportSection.ImportSectionEntry> rightImports = GetImports(_rightDumper.Reader);
            HashSet<string> commonKeys = new HashSet<string>(leftImports.Keys);
            commonKeys.IntersectWith(rightImports.Keys);

            _writer.WriteLine("Import entries");
            _writer.WriteLine("--------------");

            foreach (string key in commonKeys.OrderBy(k => k))
            {
                ReadyToRunImportSection.ImportSectionEntry leftEntry = leftImports[key];
                ReadyToRunImportSection.ImportSectionEntry rightEntry = rightImports[key];
                StringWriter leftInfo = new StringWriter();
                StringWriter rightInfo = new StringWriter();
                leftEntry.GCRefMap?.WriteTo(leftInfo);
                rightEntry.GCRefMap?.WriteTo(rightInfo);
                string leftGCRefMap = leftInfo.ToString();
                string rightGCRefMap = rightInfo.ToString();
                if (leftGCRefMap != rightGCRefMap)
                {
                    _writer.WriteLine($@"Method:           {key}");
                    _writer.WriteLine($@"Left GC ref map:  {leftGCRefMap}");
                    _writer.WriteLine($@"Right GC ref map: {rightGCRefMap}");
                }
            }

            _writer.WriteLine();
        }

        /// <summary>
        /// Diff the R2R method maps.
        /// </summary>
        private void DiffR2RMethods()
        {
            ShowMethodDiff(GetR2RMethodMap(_leftDumper.Reader, AllModules), GetR2RMethodMap(_rightDumper.Reader, AllModules), "R2R methods");

            if (_leftDumper.Reader.Composite && _rightDumper.Reader.Composite)
            {
                HashSet<string> allComponentAssemblies = new HashSet<string>(_leftDumper.Reader.ManifestReferenceAssemblies.Keys);
                allComponentAssemblies.UnionWith(_rightDumper.Reader.ManifestReferenceAssemblies.Keys);
                foreach (string assemblyName in allComponentAssemblies.OrderBy(name => name))
                {
                    if (!_leftDumper.Reader.ManifestReferenceAssemblies.TryGetValue(assemblyName, out int leftModuleIndex))
                    {
                        leftModuleIndex = InvalidModule;
                    }
                    if (!_rightDumper.Reader.ManifestReferenceAssemblies.TryGetValue(assemblyName, out int rightModuleIndex))
                    {
                        rightModuleIndex = InvalidModule;
                    }
                    Dictionary<string, int> leftMap = GetR2RMethodMap(_leftDumper.Reader, leftModuleIndex);
                    Dictionary<string, int> rightMap = GetR2RMethodMap(_rightDumper.Reader, rightModuleIndex);
                    ShowMethodDiff(leftMap, rightMap, $"{assemblyName}: component R2R methods");
                }
            }
        }

        private void ShowMethodDiff(Dictionary<string, int> leftMethods, Dictionary<string, int> rightMethods, string diffName)
        {
            Dictionary<string, int> empty = new Dictionary<string, int>();
            Dictionary<string, int> leftOnly = new Dictionary<string, int>();
            Dictionary<string, int> rightOnly = new Dictionary<string, int>();
            Dictionary<string, int> leftCommon = new Dictionary<string, int>();
            Dictionary<string, int> rightCommon = new Dictionary<string, int>();

            foreach (KeyValuePair<string, int> left in leftMethods)
            {
                (rightMethods.ContainsKey(left.Key) ? leftCommon : leftOnly).Add(left.Key, left.Value);
            }

            foreach (KeyValuePair<string, int> right in rightMethods)
            {
                (leftMethods.ContainsKey(right.Key) ? rightCommon : rightOnly).Add(right.Key, right.Value);
            }

            string statTitle = $"LEFT COUNT | RIGHT COUNT | LEFT SIZE | RIGHT SIZE | {diffName}";
            _writer.WriteLine(statTitle);
            _writer.WriteLine(new string('-', statTitle.Length));
            _writer.WriteLine($"{leftOnly.Count,10} | {"---",11} | {leftOnly.Sum(m => m.Value),9} | {"---",10} | LEFT-ONLY METHODS");
            _writer.WriteLine($"{"---",10} | {rightOnly.Count,11} | {"---",9} | {rightOnly.Sum(m => m.Value),10} | RIGHT-ONLY METHODS");
            _writer.WriteLine($"{leftCommon.Count,10} | {rightCommon.Count,11} | {leftCommon.Sum(m => m.Value),9} | {rightCommon.Sum(m => m.Value),10} | METHODS IN BOTH");
            _writer.WriteLine($"{leftMethods.Count,10} | {rightMethods.Count,11} | {leftMethods.Sum(m => m.Value),9} | {rightMethods.Sum(m => m.Value),10} | (total)");
            _writer.WriteLine();

            ShowDiff(leftOnly, empty, diffName + " / left only");
            ShowDiff(empty, rightOnly, diffName + " / right only");
            ShowDiff(leftCommon, rightCommon, diffName + " / common");
        }

        /// <summary>
        /// Show a difference summary between the sets of "left objects" and "right objects".
        /// </summary>
        /// <param name="leftObjects">Dictionary of left object sizes keyed by their names</param>
        /// <param name="rightObjects">Dictionary of right object sizes keyed by their names</param>
        /// <param name="diffName">Logical name of the diffing operation to display in the header line</param>
        private void ShowDiff(Dictionary<string, int> leftObjects, Dictionary<string, int> rightObjects, string diffName)
        {
            Dictionary<string, int> allObjects = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> left in leftObjects)
            {
                allObjects.TryGetValue(left.Key, out int previousValue);
                allObjects[left.Key] = previousValue - left.Value;
            }
            foreach (KeyValuePair<string, int> right in rightObjects)
            {
                allObjects.TryGetValue(right.Key, out int previousValue);
                allObjects[right.Key] = previousValue + right.Value;
            }
            ShowDiff(leftObjects, rightObjects, allObjects.OrderByDescending(kvp => kvp.Value).Select(kvp => kvp.Key), diffName + " by delta");
            ShowDiff(leftObjects, rightObjects, allObjects.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Key), diffName + " by name");
        }

        private void ShowDiff(Dictionary<string, int> leftObjects, Dictionary<string, int> rightObjects, IEnumerable<string> orderedKeys, string diffName)
        {
            string title = $@" LEFT_SIZE RIGHT_SIZE       DIFF  {diffName} ({orderedKeys.Count()} ELEMENTS)";

            _writer.WriteLine(title);
            _writer.WriteLine(new string('-', title.Length));

            int leftTotal = 0;
            int rightTotal = 0;
            foreach (string key in orderedKeys)
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
                    line.Append($"{leftSize,10}");
                }
                else
                {
                    line.Append(' ', 10);
                }
                if (inRight)
                {
                    line.Append($"{rightSize,11}");
                }
                else
                {
                    line.Append(' ', 11);
                }
                if (leftSize != rightSize)
                {
                    line.Append($"{rightSize - leftSize,11}");
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
        private Dictionary<string, int> GetPESectionMap(ReadyToRunReader reader)
        {
            Dictionary<string, int> sectionMap = new Dictionary<string, int>();

            foreach (SectionHeader sectionHeader in reader.CompositeReader.PEHeaders.SectionHeaders)
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
        private Dictionary<string, int> GetR2RSectionMap(ReadyToRunReader reader)
        {
            Dictionary<string, int> sectionMap = new Dictionary<string, int>();

            foreach (KeyValuePair<ReadyToRunSectionType, ReadyToRunSection> typeAndSection in reader.ReadyToRunHeader.Sections)
            {
                string name = typeAndSection.Key.ToString();
                sectionMap.Add(name, typeAndSection.Value.Size);
            }

            return sectionMap;
        }

        private static ReadyToRunSectionType[] s_methodSections = new[] { ReadyToRunSectionType.MethodDefEntryPoints, ReadyToRunSectionType.InstanceMethodEntryPoints };

        /// <summary>
        /// Read the R2R method map for a given R2R image.
        /// </summary>
        /// <param name="reader">R2R image to scan</param>
        /// <returns></returns>
        private Dictionary<string, int> GetR2RMethodMap(ReadyToRunReader reader, int moduleIndex)
        {
            Dictionary<string, int> methodMap = new Dictionary<string, int>();
            foreach (ReadyToRunMethod method in TryGetMethods(reader, moduleIndex))
            {
                methodMap.Add(method.SignatureString, method.Size);
            }
            return methodMap;
        }

        /// <summary>
        /// Dump the subset of methods common to both sides of the diff to the given dumper.
        /// </summary>
        /// <param name="dumper">Output dumper to use</param>
        /// <param name="signatureFilter">Set of common signatures of methods to dump</param>
        private void DumpCommonMethods(Dumper dumper, int moduleIndex, Dictionary<string, MethodPair> signatureFilter)
        {
            IEnumerable<ReadyToRunMethod> filteredMethods = TryGetMethods(dumper.Reader, moduleIndex)
                .Where(method => signatureFilter.ContainsKey(method.SignatureString))
                .OrderBy(method => method.SignatureString);

            foreach (ReadyToRunMethod method in filteredMethods)
            {
                dumper.DumpMethod(method);
            }
        }

        private static Dictionary<string, ReadyToRunImportSection.ImportSectionEntry> GetImports(ReadyToRunReader reader)
        {
            var result = new Dictionary<string, ReadyToRunImportSection.ImportSectionEntry>();
            var signatureOptions = new SignatureFormattingOptions() { Naked = true };
            foreach (ReadyToRunImportSection section in reader.ImportSections)
            {
                foreach (ReadyToRunImportSection.ImportSectionEntry entry in section.Entries)
                {
                    result[entry.Signature.ToString(signatureOptions)] = entry;
                }
            }
            return result;
        }

        /// <summary>
        /// Filter out methods that have identical left / right disassembly.
        /// </summary>
        /// <param name="commonMethods">Enumeration of common methods to filter</param>
        /// <returns>Filtered method enumeration</returns>
        private IEnumerable<KeyValuePair<string, MethodPair>> HideMethodsWithSameDisassembly(IEnumerable<KeyValuePair<string, MethodPair>> commonMethods)
        {
            bool first = true;
            foreach (KeyValuePair<string, MethodPair> commonMethod in commonMethods)
            {
                bool match = (commonMethod.Value.LeftMethod.RuntimeFunctions.Count == commonMethod.Value.RightMethod.RuntimeFunctions.Count);
                if (match)
                {
                    for (int rtfIndex = 0; match && rtfIndex < commonMethod.Value.LeftMethod.RuntimeFunctions.Count; rtfIndex++)
                    {
                        RuntimeFunction leftRuntimeFunction = commonMethod.Value.LeftMethod.RuntimeFunctions[rtfIndex];
                        RuntimeFunction rightRuntimeFunction = commonMethod.Value.RightMethod.RuntimeFunctions[rtfIndex];
                        int leftOffset = 0;
                        int rightOffset = 0;
                        for (; ;)
                        {
                            bool leftAtEnd = (leftOffset >= leftRuntimeFunction.Size);
                            bool rightAtEnd = (rightOffset >= rightRuntimeFunction.Size);
                            if (leftAtEnd || rightAtEnd)
                            {
                                if (!leftAtEnd || !rightAtEnd)
                                {
                                    match = false;
                                }
                                break;
                            }
                            leftOffset += _leftDumper.Disassembler.GetInstruction(leftRuntimeFunction,
                                _leftDumper.Reader.GetOffset(leftRuntimeFunction.StartAddress),
                                leftOffset, out string leftInstruction);
                            rightOffset += _rightDumper.Disassembler.GetInstruction(rightRuntimeFunction,
                                _rightDumper.Reader.GetOffset(rightRuntimeFunction.StartAddress),
                                rightOffset, out string rightInstruction);
                            if (leftInstruction != rightInstruction)
                            {
                                match = false;
                                break;
                            }
                        }
                    }
                }
                if (match)
                {
                    if (first)
                    {
                        _writer.WriteLine("Methods with identical disassembly skipped in common method diff:");
                        first = false;
                    }
                    _writer.WriteLine(commonMethod.Key);
                }
                else
                {
                    yield return commonMethod;
                }
            }
        }

        struct MethodPair
        {
            public readonly ReadyToRunMethod LeftMethod;
            public readonly ReadyToRunMethod RightMethod;

            public MethodPair(ReadyToRunMethod leftMethod, ReadyToRunMethod rightMethod)
            {
                LeftMethod = leftMethod;
                RightMethod = rightMethod;
            }
        }
    }
}
