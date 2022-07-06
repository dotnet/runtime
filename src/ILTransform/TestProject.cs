// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Xml;

namespace ILTransform
{
    public struct DebugOptimize : IComparable<DebugOptimize>
    {
        public readonly string Debug;
        public readonly string Optimize;

        public DebugOptimize(string debug, string optimize)
        {
            Debug = debug;
            Optimize = optimize;
        }

        public int CompareTo(DebugOptimize other)
        {
            int result = Debug.CompareTo(other.Debug);
            if (result == 0)
            {
                result = Optimize.CompareTo(other.Optimize);
            }
            return result;
        }

        public override bool Equals(object? obj) => obj is DebugOptimize optimize && Debug == optimize.Debug && Optimize == optimize.Optimize;

        public override int GetHashCode() => HashCode.Combine(Debug, Optimize);

        public override string ToString()
        {
            return string.Format("DbgOpt ({0} | {1})", Debug, Optimize);
        }
    }

    public struct TestCount
    {
        public int Total;
        public int Pri0;
        public int Fact;
        public int ILProj;
        public HashSet<string> Properties;

        public int Pri1 => Total - Pri0;

        public static TestCount New() => new TestCount() { Properties = new HashSet<string>() };
    }

    public class TestProject
    {
        public readonly string AbsolutePath;
        public readonly string RelativePath;
        public readonly string OutputType;
        public readonly string CLRTestKind;
        public readonly string CLRTestProjectToRun;
        public readonly string CLRTestExecutionArguments;
        public readonly DebugOptimize DebugOptimize;
        public readonly string Priority;
        public readonly string[] CompileFiles;
        public readonly string[] ProjectReferences;
        public readonly string TestClassName;
        public readonly string[] TestClassBases;
        public readonly string TestClassNamespace;
        public readonly string TestClassSourceFile;
        public readonly int TestClassLine;
        public readonly int MainMethodLine;
        public readonly int LastUsingLine;
        public readonly int NamespaceLine;
        public readonly bool HasFactAttribute;
        public readonly Dictionary<string, string> AllProperties;
        public readonly bool IsILProject;

        public string? TestProjectAlias;
        public string? DeduplicatedClassName;
        public string? DeduplicatedNamespaceName;

        public TestProject(
            string absolutePath,
            string relativePath,
            Dictionary<string, string> allProperties,
            string[] compileFiles,
            string[] projectReferences,
            string testClassName,
            string[] testClassBases,
            string testClassNamespace,
            string testClassSourceFile,
            int testClassLine,
            int mainMethodLine,
            int lastUsingLine,
            int namespaceLine,
            bool hasFactAttribute)
        {
            AbsolutePath = absolutePath;
            RelativePath = relativePath;
            AllProperties = allProperties;

            OutputType = GetProperty("OutputType");
            CLRTestKind = GetProperty("CLRTestKind");
            CLRTestProjectToRun = SanitizeFileName(GetProperty("CLRTestProjectToRun"), AbsolutePath);
            CLRTestExecutionArguments = GetProperty("CLRTestExecutionArguments");
            string debugType = InitCaps(GetProperty("DebugType"));
            string optimize = InitCaps(GetProperty("Optimize"));
            if (optimize == "")
            {
                optimize = "False";
            }
            DebugOptimize = new DebugOptimize(debugType, optimize);
            Priority = GetProperty("CLRTestPriority");

            CompileFiles = compileFiles;
            ProjectReferences = projectReferences;
            TestClassName = testClassName;
            TestClassBases = testClassBases;
            TestClassNamespace = testClassNamespace;
            TestClassSourceFile = testClassSourceFile;
            TestClassLine = testClassLine;
            MainMethodLine = mainMethodLine;
            LastUsingLine = lastUsingLine;
            NamespaceLine = namespaceLine;
            HasFactAttribute = hasFactAttribute;

            IsILProject = Path.GetExtension(RelativePath).ToLower() == ".ilproj";
        }

        public static bool IsIdentifier(char c)
        {
            return char.IsDigit(c) || char.IsLetter(c) || c == '_' || c == '@' || c == '$';
        }

        public static bool MakePublic(bool isILTest, ref string line, bool force)
        {
            if (!line.Contains("public "))
            {
                if (line.Contains("private "))
                {
                    line = line.Replace("private ", "public ");
                    return true;
                }
                else if (line.Contains("internal "))
                {
                    line = line.Replace("internal ", "public ");
                    return true;
                }
                else if (force)
                {
                    int charIndex = 0;
                    while (charIndex < line.Length && char.IsWhiteSpace(line[charIndex]))
                    {
                        charIndex++;
                    }
                    if (isILTest)
                    {
                        while (charIndex < line.Length && !char.IsWhiteSpace(line[charIndex]))
                        {
                            charIndex++;
                        }
                        if (charIndex < line.Length)
                        {
                            charIndex++;
                        }
                    }
                    line = string.Concat(line.AsSpan(0, charIndex), "public ", line.AsSpan(charIndex));
                    return true;
                }
            }
            return false;
        }

        public static string SanitizeIdentifier(string source)
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (IsIdentifier(c))
                {
                    if (char.IsDigit(c) && output.Length == 0)
                    {
                        output.Append('_');
                    }
                    output.Append(c);
                }
                else if (c == '-')
                {
                    output.Append('_');
                }
                else
                {
                    output.Append("__");
                }
            }
            return output.ToString();
        }

        public static int GetIndent(string line)
        {
            int indentIndex = 0;
            while (indentIndex < line.Length && line[indentIndex] <= ' ')
            {
                indentIndex++;
            }
            return indentIndex;
        }

        public static string AddAfterIndent(string line, string add)
        {
            int indentIndex = GetIndent(line);
            return string.Concat(line.AsSpan(0, indentIndex), add, line.AsSpan(indentIndex));
        }

        public static string ReplaceIdentifier(string line, string originalIdent, string targetIdent)
        {
            int startIndex = 0;
            while (startIndex < line.Length)
            {
                int index = line.IndexOf(originalIdent, startIndex);
                if (index < 0)
                {
                    break;
                }
                int endIndex = index + originalIdent.Length;
                if ((index == 0 || !IsIdentifier(line[index - 1]))
                    && (endIndex >= line.Length || !IsIdentifier(line[endIndex])))
                {
                    line = string.Concat(line.AsSpan(0, index), targetIdent, line.AsSpan(endIndex));
                    startIndex = index + targetIdent.Length;
                }
                else
                {
                    startIndex = index + 1;
                }
            }
            return line;
        }

        public static bool GetILClassName(string line, out string? className)
        {
            int classIndex = line.IndexOf(".class");
            int structIndex = line.IndexOf(".struct");
            int scanIndex;
            if (classIndex >= 0)
            {
                scanIndex = classIndex + 6;
            }
            else if (structIndex >= 0)
            {
                scanIndex = structIndex + 7;
            }
            else
            {
                className = null;
                return false;
            }

            while (scanIndex < line.Length)
            {
                if (char.IsWhiteSpace(line[scanIndex]))
                {
                    scanIndex++;
                    continue;
                }
                if (scanIndex + 1 < line.Length && line[scanIndex] == '/' && line[scanIndex + 1] == '*')
                {
                    scanIndex += 2;
                    while (scanIndex + 1 < line.Length && !(line[scanIndex] == '*' && line[scanIndex + 1] == '/'))
                    {
                        scanIndex++;
                    }
                    scanIndex += 2;
                    continue;
                }
                if (line[scanIndex] == '\'')
                {
                    int identStart = ++scanIndex;
                    while (scanIndex < line.Length && line[scanIndex] != '\'')
                    {
                        scanIndex++;
                    }
                    className = line.Substring(identStart, scanIndex - identStart);
                    return true;
                }
                if (IsIdentifier(line[scanIndex]))
                {
                    int identStart = scanIndex;
                    while (++scanIndex < line.Length && IsIdentifier(line[scanIndex]))
                    {
                    }
                    className = line.Substring(identStart, scanIndex - identStart);
                    switch (className)
                    {
                        case "auto":
                        case "ansi":
                        case "interface":
                        case "public":
                        case "private":
                        case "sealed":
                        case "value":
                        case "beforefieldinit":
                        case "sequential":
                        case "explicit":
                        case "abstract":
                            continue;

                        case "nested":
                            className = null;
                            return false;

                        default:
                            return true;
                    }
                }
                break; // parse error
            }
            className = null;
            return false;
        }

        public bool HasSameContentAs(TestProject project2)
        {
            if (CompileFiles.Length == 0 || project2.CompileFiles.Length == 0)
            {
                return false;
            }
            if (ProjectReferences.Length != project2.ProjectReferences.Length)
            {
                return false;
            }
            if (CompileFiles.Length != project2.CompileFiles.Length)
            {
                return false;
            }
            for (int refIndex = 0; refIndex < ProjectReferences.Length; refIndex++)
            {
                string ref1 = ProjectReferences[refIndex];
                string ref2 = project2.ProjectReferences[refIndex];
                try
                {
                    if (ref1 != ref2 && File.ReadAllText(ref1) != File.ReadAllText(ref2))
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error comparing projects {ref1} and {ref2} referenced from {AbsolutePath} and {project2.AbsolutePath}: {ex.Message}");
                    return false;
                }
            }
            for (int fileIndex = 0; fileIndex < CompileFiles.Length; fileIndex++)
            {
                string file1 = CompileFiles[fileIndex];
                string file2 = project2.CompileFiles[fileIndex];
                try
                {
                    if (file1 != file2 && File.ReadAllText(file1) != File.ReadAllText(file2))
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error comparing files {file1} and {file2} referenced from {AbsolutePath} and {project2.AbsolutePath}: {ex.Message}");
                    return false;
                }
            }
            return true;
        }

        public void GetKeyNameRootNameAndSuffix(out string keyName, out string rootName, out string suffix)
        {
            string fileName = Path.GetFileName(RelativePath);
            suffix = Path.GetExtension(fileName);
            rootName = Path.GetFileNameWithoutExtension(fileName);
            int suffixIndex = rootName.Length;
            int keyNameIndex = suffixIndex;
            if (rootName.EndsWith("_il_ro") || rootName.EndsWith("_il_do"))
            {
                suffixIndex -= 6;
                keyNameIndex = suffixIndex;
            }
            else if (rootName.EndsWith("_cs_ro") || rootName.EndsWith("_cs_do"))
            {
                suffixIndex -= 6;
            }
            else if (rootName.EndsWith("_il_r") || rootName.EndsWith("_il_d"))
            {
                suffixIndex -= 5;
                keyNameIndex = suffixIndex;
            }
            else if (rootName.EndsWith("_cs_r") || rootName.EndsWith("_cs_d"))
            {
                suffixIndex -= 5;
            }
            else if (rootName.EndsWith("_do") || rootName.EndsWith("_ro"))
            {
                suffixIndex -= 3;
            }
            else if (rootName.EndsWith("_d") || rootName.EndsWith("_r"))
            {
                suffixIndex -= 2;
            }
            keyName = rootName.Substring(0, keyNameIndex);
            suffix = string.Concat(rootName.AsSpan(suffixIndex), suffix);
            rootName = rootName.Substring(0, suffixIndex);
        }

        private string GetProperty(string name, string defaultValue = "")
        {
            return AllProperties.TryGetValue(name, out string? property) ? property : defaultValue;
        }

        private static string SanitizeFileName(string fileName, string projectPath)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectPath);
            return Path.GetFullPath(
                fileName
                .Replace("$(MSBuildProjectName)", projectName)
                .Replace("$(MSBuildThisFileName)", projectName),
                Path.GetDirectoryName(projectPath)!);
        }

        private static string InitCaps(string s)
        {
            if (s.Length > 0)
            {
                s = string.Concat(s.Substring(0, 1).ToUpper(), s.AsSpan(1));
            }
            if (s.Equals("pdbonly", StringComparison.OrdinalIgnoreCase))
            {
                s = "PdbOnly";
            }
            return s;
        }
    }

    public class TestProjectStore
    {
        private readonly List<TestProject> _projects;
        private readonly Dictionary<string, List<TestProject>> _classNameMap;
        private readonly Dictionary<string, Dictionary<DebugOptimize, List<TestProject>>> _namespaceNameMap;
        private readonly HashSet<string> _rewrittenFiles;

        public TestProjectStore()
        {
            _projects = new List<TestProject>();
            _classNameMap = new Dictionary<string, List<TestProject>>();
            _namespaceNameMap = new Dictionary<string, Dictionary<DebugOptimize, List<TestProject>>>();
            _rewrittenFiles = new HashSet<string>();
        }

        public void ScanTree(string rootPath)
        {
            int projectCount = 0;
            Stopwatch sw = Stopwatch.StartNew();
            ScanRecursive(rootPath, "", ref projectCount);
            PopulateClassNameMap();
            Console.WriteLine("Done scanning {0} projects in {1} msecs", projectCount, sw.ElapsedMilliseconds);
        }

        public void GenerateExternAliases()
        {
            foreach (TestProject project in _projects)
            {
                project.TestProjectAlias = Path.GetFileNameWithoutExtension(project.RelativePath);
            }
        }

        private static string[] s_standardProperties = new string[]
        {
            "OutputType",
            "CLRTestKind",
            "CLRTestPriority",
            "AllowUnsafeBlocks",
            "DebugType",
            "Optimize",
        };

        public void DumpFolderStatistics(TextWriter writer)
        {
            for (int level = 1; level <= 3; level++)
            {
                string title = string.Format("COUNT |  PRI0  |  PRI1  |  FACT  | ILPROJ | TO FIX | {0} (PROPERTIES)", level);
                writer.WriteLine(title);
                writer.WriteLine(new string('-', title.Length));
                Dictionary<string, TestCount> folderCounts = new Dictionary<string, TestCount>();

                foreach (TestProject project in _projects.Where(p => p.TestClassName != ""))
                {
                    string[] folderSplit = project.RelativePath.Split(Path.DirectorySeparatorChar);
                    StringBuilder folderNameBuilder = new StringBuilder();
                    for (int component = 0; component < folderSplit.Length - 1 && component < level; component++)
                    {
                        if (folderNameBuilder.Length != 0)
                        {
                            folderNameBuilder.Append('/');
                        }
                        folderNameBuilder.Append(folderSplit[component]);
                    }
                    string folderName = folderNameBuilder.ToString();
                    if (!folderCounts.TryGetValue(folderName, out TestCount count))
                    {
                        count = TestCount.New();
                    }
                    count.Total++;
                    if (project.Priority != "1")
                    {
                        count.Pri0++;
                    }
                    if (project.HasFactAttribute)
                    {
                        count.Fact++;
                    }
                    if (Path.GetExtension(project.RelativePath).ToLower() == ".ilproj")
                    {
                        count.ILProj++;
                    }
                    count.Properties!.UnionWith(project.AllProperties.Keys);
                    folderCounts[folderName] = count;
                }
                foreach (KeyValuePair<string, TestCount> kvp in folderCounts.OrderBy(kvp => kvp.Key))
                {
                    string props = string.Join(' ', kvp.Value.Properties.Except(s_standardProperties).OrderBy(prop => prop));

                    writer.WriteLine(
                        "{0,5} | {1,6} | {2,6} | {3,6} | {4,6} | {5,6} | {6} ({7})",
                        kvp.Value.Total,
                        kvp.Value.Pri0,
                        kvp.Value.Pri1,
                        kvp.Value.Fact,
                        kvp.Value.ILProj,
                        kvp.Value.Pri1 - kvp.Value.Fact,
                        kvp.Key,
                        props);
                }
                writer.WriteLine();
            }
        }

        public void DumpDebugOptimizeStatistics(TextWriter writer)
        {
            Dictionary<DebugOptimize, int> debugOptimizeCountMap = new Dictionary<DebugOptimize, int>();

            foreach (TestProject project in _projects)
            {
                debugOptimizeCountMap.TryGetValue(project.DebugOptimize, out int projectCount);
                debugOptimizeCountMap[project.DebugOptimize] = projectCount + 1;
            }

            writer.WriteLine("DEBUG      | OPTIMIZE   | PROJECT COUNT");
            writer.WriteLine("----------------------------------------");

            foreach (KeyValuePair<DebugOptimize, int> kvp in debugOptimizeCountMap.OrderByDescending(kvp => kvp.Value))
            {
                writer.WriteLine("{0,-10} | {1,-10} | {2}", kvp.Key.Debug, kvp.Key.Optimize, kvp.Value);
            }
            writer.WriteLine();
        }

        public void DumpIrregularProjectSuffixes(TextWriter writer)
        {
            writer.WriteLine("ILPROJ projects not ending in _il_d/do/r/ro");
            writer.WriteLine("-------------------------------------------");

            foreach (TestProject project in _projects)
            {
                if (Path.GetExtension(project.RelativePath).ToLower() == ".ilproj")
                {
                    string projectName = Path.GetFileNameWithoutExtension(project.RelativePath);
                    if (!projectName.EndsWith("_il_do") &&
                        !projectName.EndsWith("_il_ro") &&
                        !projectName.EndsWith("_il_d") &&
                        !projectName.EndsWith("_il_r"))
                    {
                        writer.WriteLine(project.AbsolutePath);
                    }
                }
            }

            writer.WriteLine();

            writer.WriteLine("CSPROJ projects ending in _il_d/do/r/ro");
            writer.WriteLine("---------------------------------------");

            foreach (TestProject project in _projects)
            {
                if (Path.GetExtension(project.RelativePath).ToLower() == ".csproj")
                {
                    string projectName = Path.GetFileNameWithoutExtension(project.RelativePath);
                    if (projectName.EndsWith("_il_do") ||
                        projectName.EndsWith("_il_ro") ||
                        projectName.EndsWith("_il_d") ||
                        projectName.EndsWith("_il_r"))
                    {
                        writer.WriteLine(project.AbsolutePath);
                    }
                }
            }

            writer.WriteLine("CSPROJ projects not ending in _d/_do/_r/_ro");
            writer.WriteLine("-------------------------------------------");

            foreach (TestProject project in _projects)
            {
                if (Path.GetExtension(project.RelativePath).ToLower() == ".csproj")
                {
                    string projectName = Path.GetFileNameWithoutExtension(project.RelativePath);
                    if (!projectName.EndsWith("_do") &&
                        !projectName.EndsWith("_ro") &&
                        !projectName.EndsWith("_d") &&
                        !projectName.EndsWith("_r"))
                    {
                        writer.WriteLine(project.AbsolutePath);
                    }
                }
            }

            writer.WriteLine();
        }

        public void DumpMultiProjectSources(TextWriter writer)
        {
            Dictionary<string, Dictionary<DebugOptimize, List<TestProject>>> potentialDuplicateMap = new Dictionary<string, Dictionary<DebugOptimize, List<TestProject>>>();
            foreach (TestProject project in _projects)
            {
                foreach (string source in project.CompileFiles)
                {
                    if (!potentialDuplicateMap.TryGetValue(source, out Dictionary<DebugOptimize, List<TestProject>>? sourceMap))
                    {
                        sourceMap = new Dictionary<DebugOptimize, List<TestProject>>();
                        potentialDuplicateMap.Add(source, sourceMap);
                    }
                    if (!sourceMap.TryGetValue(project.DebugOptimize, out List<TestProject>? projects))
                    {
                        projects = new List<TestProject>();
                        sourceMap.Add(project.DebugOptimize, projects);
                    }
                    projects.Add(project);
                }
            }

            writer.WriteLine("SOURCES USED IN MULTIPLE PROJECTS");
            writer.WriteLine("---------------------------------");

            foreach (KeyValuePair<string, Dictionary<DebugOptimize, List<TestProject>>> sourceKvp in potentialDuplicateMap.Where(kvp => kvp.Value.Values.Any(l => l.Count > 1)).OrderBy(kvp => kvp.Key))
            {
                writer.WriteLine(sourceKvp.Key);
                foreach (KeyValuePair<DebugOptimize, List<TestProject>> debugOptKvp in sourceKvp.Value.Where(kvp => kvp.Value.Count > 1))
                {
                    writer.WriteLine("\\- {0}", debugOptKvp.Key);
                    foreach (TestProject project in debugOptKvp.Value)
                    {
                        writer.WriteLine("   \\- {0}", project.AbsolutePath);
                    }
                }
            }

            writer.WriteLine();
        }

        public void DumpDuplicateProjectContent(TextWriter writer)
        {
            Dictionary<string, List<TestProject>> potentialDuplicateMap = new Dictionary<string, List<TestProject>>();
            foreach (TestProject project in _projects)
            {
                StringBuilder projectKey = new StringBuilder();
                projectKey.AppendLine("Debug: " + project.DebugOptimize.Debug.ToLower());
                projectKey.AppendLine("Optimize: " + project.DebugOptimize.Optimize.ToLower());
                foreach (string projectReference in project.ProjectReferences.Select(p => Path.GetFileName(p)).OrderBy(p => p))
                {
                    projectKey.AppendLine("ProjectReference: " + projectReference);
                }
                foreach (string compileFile in project.CompileFiles.Select(p => Path.GetFileName(p)).OrderBy(p => p))
                {
                    projectKey.AppendLine("CompileFile: " + compileFile);
                }
                string key = projectKey.ToString();
                if (!potentialDuplicateMap.TryGetValue(key, out List<TestProject>? projectList))
                {
                    projectList = new List<TestProject>();
                    potentialDuplicateMap.Add(key, projectList);
                }
                projectList.Add(project);
            }

            writer.WriteLine("PROJECT PAIRS WITH DUPLICATE CONTENT");
            writer.WriteLine("------------------------------------");
            foreach (List<TestProject> projectGroup in potentialDuplicateMap.Values)
            {
                for (int index1 = 1; index1 < projectGroup.Count; index1++)
                {
                    for (int index2 = 0; index2 < index1; index2++)
                    {
                        TestProject project1 = projectGroup[index1];
                        TestProject project2 = projectGroup[index2];
                        if (project1.HasSameContentAs(project2))
                        {
                            writer.WriteLine(project1.AbsolutePath);
                            writer.WriteLine(project2.AbsolutePath);
                            writer.WriteLine();
                        }
                    }
                }
            }
        }

        public void DumpDuplicateSimpleProjectNames(TextWriter writer)
        {
            Dictionary<string, List<TestProject>> simpleNameMap = new Dictionary<string, List<TestProject>>();
            foreach (TestProject project in _projects)
            {
                string simpleName = Path.GetFileNameWithoutExtension(project.RelativePath);
                if (!simpleNameMap.TryGetValue(simpleName, out List<TestProject>? projectsForSimpleName))
                {
                    projectsForSimpleName = new List<TestProject>();
                    simpleNameMap.Add(simpleName, projectsForSimpleName);
                }
                projectsForSimpleName.Add(project);
            }

            foreach (KeyValuePair<string, List<TestProject>> kvp in simpleNameMap.Where(kvp => kvp.Value.Count > 1).OrderByDescending(kvp => kvp.Value.Count))
            {
                writer.WriteLine("DUPLICATE PROJECT NAME: ({0}x): {1}", kvp.Value.Count, kvp.Key);
                foreach (TestProject project in kvp.Value)
                {
                    writer.WriteLine("    {0}", project.AbsolutePath);
                }
                writer.WriteLine();
            }
        }

        public void DumpDuplicateEntrypointClasses(TextWriter writer)
        {
            Dictionary<string, List<TestProject>> duplicateClassNames = new Dictionary<string, List<TestProject>>();
            foreach (KeyValuePair<string, List<TestProject>> kvp in _classNameMap.Where(kvp => kvp.Value.Count > 1))
            {
                Dictionary<DebugOptimize, List<TestProject>> debugOptMap = new Dictionary<DebugOptimize, List<TestProject>>();
                foreach (TestProject project in kvp.Value)
                {
                    if (!debugOptMap.TryGetValue(project.DebugOptimize, out List<TestProject>? projects))
                    {
                        projects = new List<TestProject>();
                        debugOptMap.Add(project.DebugOptimize, projects);
                    }
                    projects.Add(project);
                }
                List<TestProject> filteredDuplicates = new List<TestProject>();
                foreach (List<TestProject> projectList in debugOptMap.Values.Where(v => v.Count > 1))
                {
                    filteredDuplicates.AddRange(projectList);
                }
                if (filteredDuplicates.Count > 0)
                {
                    duplicateClassNames.Add(kvp.Key, filteredDuplicates);
                }
            }

            writer.WriteLine("#PROJECTS | DUPLICATE TEST CLASS NAME");
            writer.WriteLine("-------------------------------------");
            writer.WriteLine("{0,-9} | (total)", duplicateClassNames.Where(kvp => kvp.Value.Count > 1).Sum(kvp => kvp.Value.Count));

            foreach (KeyValuePair<string, List<TestProject>> kvp in duplicateClassNames.Where(kvp => kvp.Value.Count > 1).OrderByDescending(kvp => kvp.Value.Count))
            {
                writer.WriteLine("{0,-9} | {1}", kvp.Value.Count, kvp.Key);
            }

            writer.WriteLine();

            foreach (KeyValuePair<string, List<TestProject>> kvp in _classNameMap.Where(kvp => kvp.Value.Count > 1).OrderByDescending(kvp => kvp.Value.Count))
            {
                string title = string.Format("{0} PROJECTS WITH CLASS NAME {1}:", kvp.Value.Count, kvp.Key);
                writer.WriteLine(title);
                writer.WriteLine(new string('-', title.Length));
                foreach (TestProject project in kvp.Value.OrderBy(prj => prj.RelativePath))
                {
                    writer.WriteLine(project.AbsolutePath);
                }
                writer.WriteLine();
            }

            writer.WriteLine();
        }

        public void DumpImplicitSharedLibraries(TextWriter writer)
        {
            writer.WriteLine("IMPLICIT SHARED LIBRARIES");
            writer.WriteLine("-------------------------");

            foreach (TestProject project in _projects.Where(p => p.OutputType.Equals("Library", StringComparison.OrdinalIgnoreCase) && p.CLRTestKind == "").OrderBy(p => p.RelativePath))
            {
                writer.WriteLine(project.AbsolutePath);
            }

            writer.WriteLine();
        }

        public void DumpProjectsWithoutFactAttributes(TextWriter writer)
        {
            writer.WriteLine("PROJECTS WITHOUT FACT ATTRIBUTES");
            writer.WriteLine("--------------------------------");

            foreach (TestProject project in _projects.Where(p => p.TestClassName != "" && !p.HasFactAttribute).OrderBy(p => p.RelativePath))
            {
                writer.WriteLine(project.AbsolutePath);
            }

            writer.WriteLine();
        }

        public void DumpCommandLineVariations(TextWriter writer)
        {
            Dictionary<string, List<TestProject>> commandlineVariations = new Dictionary<string, List<TestProject>>();
            foreach (TestProject project in _projects)
            {
                if (project.CLRTestExecutionArguments != "")
                {
                    if (!commandlineVariations.TryGetValue(project.CLRTestProjectToRun, out List<TestProject>? projects))
                    {
                        projects = new List<TestProject>();
                        commandlineVariations.Add(project.CLRTestProjectToRun, projects);
                    }
                    projects.Add(project);
                }
            }

            if (commandlineVariations.TryGetValue("", out List<TestProject>? singleProjects))
            {
                writer.WriteLine("SINGLE TESTS WITH COMMAND-LINE ARGUMENTS");
                writer.WriteLine("----------------------------------------");
                foreach (TestProject project in singleProjects.OrderBy(p => p.RelativePath))
                {
                    writer.WriteLine("{0} -> {1}", project.AbsolutePath, project.CLRTestExecutionArguments);
                }
            }
            writer.WriteLine();

            writer.WriteLine("TEST GROUPS WITH VARIANT ARGUMENTS");
            writer.WriteLine("----------------------------------");
            foreach (KeyValuePair<string, List<TestProject>> group in commandlineVariations.OrderByDescending(clv => clv.Value.Count))
            {
                writer.WriteLine(group.Key);
                foreach (TestProject project in group.Value.OrderBy(p => p.RelativePath))
                {
                    writer.WriteLine("    -> {0}", project.CLRTestExecutionArguments);
                }
            }
            writer.WriteLine();
        }

        public void RewriteAllTests(bool deduplicateClassNames, string classToDeduplicate, bool addILFactAttributes, bool cleanupILModuleAssembly)
        {
            HashSet<string> classNameDuplicates = new HashSet<string>(_classNameMap.Where(kvp => kvp.Value.Count > 1).Select(kvp => kvp.Key));

            int index = 0;
            foreach (TestProject project in _projects)
            {
                if (!string.IsNullOrEmpty(classToDeduplicate) && project.TestClassName != classToDeduplicate)
                {
                    continue;
                }
                new ILRewriter(project, classNameDuplicates, deduplicateClassNames, _rewrittenFiles, addILFactAttributes, cleanupILModuleAssembly).Rewrite();
                index++;
                if (index % 500 == 0)
                {
                    Console.WriteLine("Rewritten {0} / {1} projects", index, _projects.Count);
                }
            }
        }

        public void UnifyDbgRelProjects()
        {
            foreach (TestProject testProject in _projects)
            {
                string dir = Path.GetDirectoryName(testProject.AbsolutePath)!;
                string file = Path.GetFileNameWithoutExtension(testProject.RelativePath);
                string ext = Path.GetExtension(testProject.RelativePath);
                string renamedFile = file;
                if (renamedFile.StartsWith("_il"))
                {
                    renamedFile = string.Concat(renamedFile.AsSpan(3), "_il");
                }
                if (renamedFile.StartsWith("_speed_dbg"))
                {
                    renamedFile = string.Concat(renamedFile.AsSpan(10), "_do");
                }
                else if (renamedFile.StartsWith("_speed_rel"))
                {
                    renamedFile = string.Concat(renamedFile.AsSpan(10), "_ro");
                }
                else if (renamedFile.StartsWith("_opt_dbg"))
                {
                    renamedFile = string.Concat(renamedFile.AsSpan(8), "_do");
                }
                else if (renamedFile.StartsWith("_opt_rel"))
                {
                    renamedFile = string.Concat(renamedFile.AsSpan(8), "_ro");
                }
                else if (renamedFile.StartsWith("_odbg"))
                {
                    renamedFile = string.Concat(renamedFile.AsSpan(5), "_do");
                }
                else if (renamedFile.StartsWith("_orel"))
                {
                    renamedFile = string.Concat(renamedFile.AsSpan(5), "_ro");
                }
                else if (renamedFile.StartsWith("_dbg"))
                {
                    renamedFile = string.Concat(renamedFile.AsSpan(4), "_d");
                }
                else if (renamedFile.StartsWith("_rel"))
                {
                    renamedFile = string.Concat(renamedFile.AsSpan(4), "_r");
                }
                if (testProject.IsILProject)
                {
                    if (renamedFile.EndsWith("_d") && !renamedFile.EndsWith("_il_d"))
                    {
                        renamedFile = string.Concat(renamedFile.AsSpan(0, renamedFile.Length - 2), "_il_d");
                    }
                    else if (renamedFile.EndsWith("_do") && !renamedFile.EndsWith("_il_do"))
                    {
                        renamedFile = string.Concat(renamedFile.AsSpan(0, renamedFile.Length - 3), "_il_do");
                    }
                    else if (renamedFile.EndsWith("_r") && !renamedFile.EndsWith("_il_r"))
                    {
                        renamedFile = string.Concat(renamedFile.AsSpan(0, renamedFile.Length - 2), "_il_r");
                    }
                    else if (renamedFile.EndsWith("_ro") && !renamedFile.EndsWith("_il_ro"))
                    {
                        renamedFile = string.Concat(renamedFile.AsSpan(0, renamedFile.Length - 3), "_il_ro");
                    }
                }
                if (renamedFile != file)
                {
                    string renamedPath = Path.Combine(dir, renamedFile + ext);
                    File.Move(testProject.AbsolutePath, renamedPath, overwrite: false);
                }
            }
        }

        private static string[] s_wrapperGroups = new string[] { "_do", "_ro", "_d", "_r", "" };

        public void DeduplicateProjectNames()
        {
            foreach (string wrapperGroup in s_wrapperGroups)
            {
                Dictionary<string, List<TestProject>> rootNameToProjectMap = new Dictionary<string, List<TestProject>>();

                foreach (TestProject testProject in _projects)
                {
                    string projectName = Path.GetFileNameWithoutExtension(testProject.RelativePath);
                    if (wrapperGroup != "")
                    {
                        if (!projectName.EndsWith(wrapperGroup))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (s_wrapperGroups.Any(wg => wg != "" && projectName.EndsWith(wg)))
                        {
                            continue;
                        }
                    }

                    testProject.GetKeyNameRootNameAndSuffix(out string keyName, out string _, out string _);
                    if (!rootNameToProjectMap.TryGetValue(keyName, out List<TestProject>? projects))
                    {
                        projects = new List<TestProject>();
                        rootNameToProjectMap.Add(keyName, projects);
                    }
                    projects.Add(testProject);
                }

                foreach (List<TestProject> projectList in rootNameToProjectMap.Values.Where(pl => pl.Count > 1))
                {
                    int depth = 1;
                    do
                    {
                        HashSet<string> folderCollisions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        bool foundCollision = false;
                        foreach (TestProject project in projectList)
                        {
                            string projectDir = Path.GetDirectoryName(project.AbsolutePath)!;
                            string dirKey = "";
                            for (int i = 0; i < depth; i++)
                            {
                                dirKey += "/" + Path.GetFileName(projectDir);
                                projectDir = Path.GetDirectoryName(projectDir)!;
                            }
                            if (!folderCollisions.Add(dirKey))
                            {
                                foundCollision = true;
                                break;
                            }
                        }
                        if (!foundCollision)
                        {
                            break;
                        }
                    }
                    while (++depth < 2);

                    foreach (TestProject project in projectList)
                    {
                        project.GetKeyNameRootNameAndSuffix(out string _, out string rootName, out string suffix);
                        string projectDir = Path.GetDirectoryName(project.AbsolutePath)!;
                        string parent = projectDir;
                        string newRootName = rootName;
                        for (int i = 0; i < depth; i++)
                        {
                            newRootName += "_" + Path.GetFileName(parent);
                            parent = Path.GetDirectoryName(parent)!;
                        }
                        string newProjectPath = Path.Combine(projectDir, newRootName + suffix);
                        File.Move(project.AbsolutePath, newProjectPath, overwrite: false);
                        if (project.IsILProject)
                        {
                            RenameAssembly(project, rootName, newRootName);
                        }
                    }
                }
            }
        }

        private void RenameAssembly(TestProject project, string oldAssemblyName, string newAssemblyName)
        {
            string[] source = File.ReadAllLines(project.TestClassSourceFile);
            for (int lineIndex = 0; lineIndex < source.Length; lineIndex++)
            {
                string line = source[lineIndex];
                if (line.Contains(".assembly") && line.Contains(oldAssemblyName))
                {
                    source[lineIndex] = TestProject.ReplaceIdentifier(line, oldAssemblyName, newAssemblyName);
                    File.WriteAllLines(project.TestClassSourceFile, source);
                    break;
                }
            }
        }

        public void GenerateAllWrappers(string outputDir)
        {
            HashSet<DebugOptimize> debugOptimizeMap = new HashSet<DebugOptimize>();
            foreach (TestProject testProject in _projects)
            {
                debugOptimizeMap.Add(testProject.DebugOptimize);
            }
            foreach (DebugOptimize debugOpt in debugOptimizeMap.OrderBy(d => d))
            {
                GenerateWrapper(outputDir, debugOpt, maxProjectsPerWrapper: 100);
            }
        }

        private void GenerateWrapper(string rootDir, DebugOptimize debugOptimize, int maxProjectsPerWrapper)
        {
            string dbgOptName = "Dbg" + debugOptimize.Debug + "_Opt" + debugOptimize.Optimize;
            string outputDir = Path.Combine(rootDir, dbgOptName);

            Directory.CreateDirectory(outputDir);

            foreach (string preexistingFile in Directory.GetFiles(outputDir))
            {
                File.Delete(preexistingFile);
            }

            TestProject[] projects = _projects.Where(p => p.DebugOptimize.Equals(debugOptimize)).ToArray();
            for (int firstProject = 0; firstProject < projects.Length; firstProject += maxProjectsPerWrapper)
            {
                string nameBase = dbgOptName;
                if (projects.Length > maxProjectsPerWrapper)
                {
                    nameBase += $"_{firstProject}";
                }

                TestProject[] projectGroup = projects[firstProject..Math.Min(projects.Length, firstProject + maxProjectsPerWrapper)];

                string wrapperSourceName = nameBase + ".cs";
                string wrapperSourcePath = Path.Combine(outputDir, wrapperSourceName);

                string wrapperProjectName = nameBase + ".csproj";
                string wrapperProjectPath = Path.Combine(outputDir, wrapperProjectName);

                using (StreamWriter writer = new StreamWriter(wrapperSourcePath))
                {
                    foreach (TestProject project in projectGroup.Where(p => p.TestClassName != ""))
                    {
                        writer.WriteLine("extern alias " + project.TestProjectAlias + ";");
                    }
                    writer.WriteLine();

                    writer.WriteLine("using System;");
                    writer.WriteLine();

                    writer.WriteLine("public static class " + dbgOptName);
                    writer.WriteLine("{");
                    writer.WriteLine("    private static int s_passed = 0;");
                    writer.WriteLine("    private static int s_noClass = 0;");
                    writer.WriteLine("    private static int s_exitCode = 0;");
                    writer.WriteLine("    private static int s_crashed = 0;");
                    writer.WriteLine("    private static int s_total = 0;");
                    writer.WriteLine();
                    writer.WriteLine("    public static int Main(string[] args)");
                    writer.WriteLine("    {");

                    foreach (TestProject project in projectGroup)
                    {
                        string testName = project.RelativePath.Replace('\\', '/');
                        if (project.TestClassName != "")
                        {
                            writer.WriteLine("        TryTest(\"" + testName + "\", " + project.TestProjectAlias + "::" + project.TestClassName + ".TestEntrypoint, args);");
                        }
                        else
                        {
                            writer.WriteLine("        Console.WriteLine(\"Skipping test: '" + testName + "' - no class name\");");
                            writer.WriteLine("        s_total++;");
                            writer.WriteLine("        s_noClass++;");
                        }
                    }

                    writer.WriteLine("        Console.WriteLine(\"Total tests: {0}; {1} passed; {2} missing class name; {3} returned wrong exit code; {4} crashed\", s_total, s_passed, s_noClass, s_exitCode, s_crashed);");
                    writer.WriteLine("        return s_crashed != 0 ? 1 : s_exitCode != 0 ? 2 : 100;");
                    writer.WriteLine("    }");
                    writer.WriteLine();
                    writer.WriteLine("    private static void TryTest(string testName, Func<string[], int> testFn, string[] args)");
                    writer.WriteLine("    {");
                    writer.WriteLine("        try");
                    writer.WriteLine("        {");
                    writer.WriteLine("            s_total++;");
                    writer.WriteLine("            int exitCode = testFn(args);");
                    writer.WriteLine("            if (exitCode == 100)");
                    writer.WriteLine("            {");
                    writer.WriteLine("                Console.WriteLine(\"Test succeeded: '{0}'\", testName);");
                    writer.WriteLine("                s_passed++;");
                    writer.WriteLine("            }");
                    writer.WriteLine("            else");
                    writer.WriteLine("            {");
                    writer.WriteLine("                Console.Error.WriteLine(\"Wrong exit code: '{0}' - {1}\", testName, exitCode);");
                    writer.WriteLine("                s_exitCode++;");
                    writer.WriteLine("            }");
                    writer.WriteLine("        }");
                    writer.WriteLine("        catch (Exception ex)");
                    writer.WriteLine("        {");
                    writer.WriteLine("            Console.Error.WriteLine(\"Test crashed: '{0}' - {1}\", testName, ex.Message);");
                    writer.WriteLine("            s_crashed++;");
                    writer.WriteLine("        }");
                    writer.WriteLine("    }");
                    writer.WriteLine("}");
                }

                using (StreamWriter writer = new StreamWriter(wrapperProjectPath))
                {
                    writer.WriteLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
                    writer.WriteLine("    <PropertyGroup>");
                    writer.WriteLine("        <OutputType>Exe</OutputType>");
                    writer.WriteLine("        <CLRTestKind>BuildAndRun</CLRTestKind>");
                    writer.WriteLine("    </PropertyGroup>");
                    writer.WriteLine("    <ItemGroup>");
                    writer.WriteLine("        <Compile Include=\"" + wrapperSourceName + "\" />");
                    writer.WriteLine("    </ItemGroup>");
                    writer.WriteLine("    <ItemGroup>");
                    HashSet<string> transitiveDependencies = new HashSet<string>();
                    foreach (TestProject project in projectGroup)
                    {
                        string relativePath = Path.GetRelativePath(outputDir, project.AbsolutePath);
                        writer.WriteLine("        <ProjectReference Include=\"" + relativePath + "\" Aliases=\"" + project.TestProjectAlias + "\" />");
                        transitiveDependencies.UnionWith(project.ProjectReferences);
                    }
                    foreach (string transitiveDependency in transitiveDependencies)
                    {
                        string relativePath = Path.GetRelativePath(outputDir, transitiveDependency);
                        writer.WriteLine("        <ProjectReference Include=\"" + relativePath + "\" />");
                    }

                    writer.WriteLine("    </ItemGroup>");
                    writer.WriteLine("</Project>");
                }
            }
        }

        private void ScanRecursive(string absolutePath, string relativePath, ref int projectCount)
        {
            foreach (string absoluteProjectPath in Directory.EnumerateFiles(absolutePath, "*.*proj", SearchOption.TopDirectoryOnly))
            {
                string relativeProjectPath = Path.Combine(relativePath, Path.GetFileName(absoluteProjectPath));
                ScanProject(absoluteProjectPath, relativeProjectPath);
                if (++projectCount % 500 == 0)
                {
                    Console.WriteLine("Projects scanned: {0}", projectCount);
                }
            }
            foreach (string absoluteSubdirectoryPath in Directory.EnumerateDirectories(absolutePath, "*", SearchOption.TopDirectoryOnly))
            {
                string relativeSubdirectoryPath = Path.Combine(relativePath, Path.GetFileName(absoluteSubdirectoryPath));
                ScanRecursive(absoluteSubdirectoryPath, relativeSubdirectoryPath, ref projectCount);
            }
        }

        private void ScanProject(string absolutePath, string relativePath)
        {
            string projectName = Path.GetFileNameWithoutExtension(relativePath);
            string projectDir = Path.GetDirectoryName(absolutePath)!;

            List<string> compileFiles = new List<string>();
            List<string> projectReferences = new List<string>();
            Dictionary<string, string> allProperties = new Dictionary<string, string>();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(absolutePath);
            foreach (XmlNode project in xmlDoc.GetElementsByTagName("Project"))
            {
                foreach (XmlNode projectChild in project.ChildNodes)
                {
                    if (projectChild.Name == "PropertyGroup")
                    {
                        foreach (XmlNode property in projectChild.ChildNodes)
                        {
                            if (property.Name != "#comment")
                            {
                                allProperties[property.Name] = property.InnerText;
                            }
                        }
                    }
                    else if (projectChild.Name == "ItemGroup")
                    {
                        foreach (XmlNode item in projectChild.ChildNodes)
                        {
                            switch (item.Name)
                            {
                                case "Compile":
                                    {
                                        string? compileFileList = item.Attributes?["Include"]?.Value;
                                        if (compileFileList is not null)
                                        {
                                            string[] compileFileArray = compileFileList.Split(' ');
                                            foreach (string compileFile in compileFileArray)
                                            {
                                                string file = compileFile
                                                    .Replace("$(MSBuildProjectName)", projectName)
                                                    .Replace("$(MSBuildThisFileName)", projectName)
                                                    .Replace("$(InteropCommonDir)", "../common/");
                                                compileFiles.Add(Path.GetFullPath(file, projectDir));
                                            }
                                        }
                                    }
                                    break;

                                case "ProjectReference":
                                    {
                                        string? projectReference = item.Attributes?["Include"]?.Value;
                                        if (projectReference is not null)
                                        {
                                            projectReferences.Add(Path.GetFullPath(projectReference, projectDir));
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            string testClassName = "";
            List<string> testClassBases = new List<string>();
            string testClassNamespace = "";
            string testClassSourceFile = "";
            int testClassLine = -1;
            int mainMethodLine = -1;
            int lastUsingLine = -1;
            int namespaceLine = -1;
            bool hasFactAttribute = false;
            foreach (string compileFile in compileFiles)
            {
                try
                {
                    AnalyzeSource(
                        compileFile,
                        testClassName: ref testClassName,
                        testClassBases: ref testClassBases,
                        testClassNamespace: ref testClassNamespace,
                        testClassSourceFile: ref testClassSourceFile,
                        testClassLine: ref testClassLine,
                        mainMethodLine: ref mainMethodLine,
                        lastUsingLine: ref lastUsingLine,
                        namespaceLine: ref namespaceLine,
                        hasFactAttribute: ref hasFactAttribute);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error analyzing '{0}': {1}", compileFile, ex);
                }
            }

            _projects.Add(new TestProject(
                absolutePath: absolutePath,
                relativePath: relativePath,
                allProperties: allProperties,
                compileFiles: compileFiles.ToArray(),
                projectReferences: projectReferences.ToArray(),
                testClassName: testClassName,
                testClassBases: testClassBases.ToArray(),
                testClassNamespace: testClassNamespace,
                testClassSourceFile: testClassSourceFile,
                testClassLine: testClassLine,
                mainMethodLine: mainMethodLine,
                lastUsingLine: lastUsingLine,
                namespaceLine: namespaceLine,
                hasFactAttribute: hasFactAttribute));
        }

        private static void AnalyzeSource(
            string path,
            ref string testClassName,
            ref List<string> testClassBases,
            ref string testClassNamespace,
            ref string testClassSourceFile,
            ref int testClassLine,
            ref int mainMethodLine,
            ref int lastUsingLine,
            ref int namespaceLine,
            ref bool hasFactAttribute)
        {
            if (path.IndexOf('*') < 0 && path.IndexOf('?') < 0)
            {
                // Exact path
                AnalyzeFileSource(
                    path: path,
                    testClassName: ref testClassName,
                    testClassBases: ref testClassBases,
                    testClassNamespace: ref testClassNamespace,
                    testClassSourceFile: ref testClassSourceFile,
                    testClassLine: ref testClassLine,
                    mainMethodLine: ref mainMethodLine,
                    lastUsingLine: ref lastUsingLine,
                    namespaceLine: ref namespaceLine,
                    hasFactAttribute: ref hasFactAttribute);
                return;
            }

            string directory = Path.GetDirectoryName(path)!;
            string pattern = Path.GetFileName(path);
            SearchOption searchOption = SearchOption.TopDirectoryOnly;
            if (Path.GetFileName(directory) == "**")
            {
                searchOption = SearchOption.AllDirectories;
                directory = Path.GetDirectoryName(directory)!;
            }
            else if (pattern == "**")
            {
                searchOption = SearchOption.AllDirectories;
                pattern = "*";
            }

            foreach (string file in Directory.EnumerateFiles(directory, pattern, searchOption))
            {
                AnalyzeFileSource(
                    path: file,
                    testClassName: ref testClassName,
                    testClassBases: ref testClassBases,
                    testClassNamespace: ref testClassNamespace,
                    testClassSourceFile: ref testClassSourceFile,
                    testClassLine: ref testClassLine,
                    mainMethodLine: ref mainMethodLine,
                    lastUsingLine: ref lastUsingLine,
                    namespaceLine: ref namespaceLine,
                    hasFactAttribute: ref hasFactAttribute);
            }
        }

        private static void AnalyzeFileSource(
            string path,
            ref string testClassName,
            ref List<string> testClassBases,
            ref string testClassNamespace,
            ref string testClassSourceFile,
            ref int testClassLine,
            ref int mainMethodLine,
            ref int lastUsingLine,
            ref int namespaceLine,
            ref bool hasFactAttribute)
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".il":
                    AnalyzeILSource(
                        path: path,
                        testClassName: ref testClassName,
                        testClassBases: ref testClassBases,
                        testClassNamespace: ref testClassNamespace,
                        testClassSourceFile: ref testClassSourceFile,
                        testClassLine: ref testClassLine,
                        mainMethodLine: ref mainMethodLine,
                        lastUsingLine: ref lastUsingLine,
                        namespaceLine: ref namespaceLine,
                        hasFactAttribute: ref hasFactAttribute);
                    break;

                case ".cs":
                    AnalyzeCSSource(
                        path: path,
                        testClassName: ref testClassName,
                        testClassBases: ref testClassBases,
                        testClassNamespace: ref testClassNamespace,
                        testClassSourceFile: ref testClassSourceFile,
                        testClassLine: ref testClassLine,
                        mainMethodLine: ref mainMethodLine,
                        lastUsingLine: ref lastUsingLine,
                        namespaceLine: ref namespaceLine,
                        hasFactAttribute: ref hasFactAttribute);
                    break;

                default:
                    Console.Error.WriteLine("Cannot analyze source file '{0}'", path);
                    break;
            }
        }

        private static int GetIndent(string line)
        {
            int indent = 0;
            while (indent < line.Length && char.IsWhiteSpace(line[indent]))
            {
                indent++;
            }
            return indent;
        }

        private static void AnalyzeCSSource(
            string path,
            ref string testClassName,
            ref List<string> testClassBases,
            ref string testClassNamespace,
            ref string testClassSourceFile,
            ref int testClassLine,
            ref int mainMethodLine,
            ref int lastUsingLine,
            ref int namespaceLine,
            ref bool hasFactAttribute)
        {
            List<string> lines = new List<string>(File.ReadAllLines(path));

            if (Path.GetFileName(path).ToLower() == "expl_obj_1.cs")
            {
                Console.WriteLine("AnalyzeCSSource: {0}", path);
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            bool isMainFile = false;

            for (int mainLine = lines.Count; --mainLine >= 0;)
            {
                string line = lines[mainLine];
                if (line.Contains("[Fact]") || line.Contains("[ConditionalFact]"))
                {
                    hasFactAttribute = true;
                    isMainFile = true;
                }
                int mainPos = line.IndexOf("int Main()");
                int entrypointPos = -1;
                if (mainPos < 0)
                {
                    entrypointPos = line.IndexOf("TestEntrypoint()");
                }

                if (mainPos >= 0 || entrypointPos >= 0)
                {
                    int mainLineIndent = GetIndent(line);
                    mainMethodLine = mainLine;
                    isMainFile = true;
                    testClassSourceFile = path;
                    while (--mainLine >= 0)
                    {
                        line = lines[mainLine];
                        int lineIndent = GetIndent(line);
                        if (lineIndent < mainLineIndent && line.Contains('{'))
                        {
                            do
                            {
                                line = lines[mainLine];
                                int classPos = line.IndexOf("class ");
                                int classNameStart = -1;
                                if (classPos >= 0)
                                {
                                    classNameStart = classPos + 6;
                                }
                                else
                                {
                                    classPos = line.IndexOf("struct ");
                                    if (classPos >= 0)
                                    {
                                        classNameStart = classPos + 7;
                                    }
                                }
                                if (classNameStart >= 0)
                                {
                                    int classNameEnd = classNameStart;
                                    while (line.Length > classNameEnd && TestProject.IsIdentifier(line[classNameEnd]))
                                    {
                                        classNameEnd++;
                                    }
                                    testClassName = line.Substring(classNameStart, classNameEnd - classNameStart);
                                    testClassLine = mainLine;

                                    int basePos = classNameEnd;
                                    while (basePos < line.Length && char.IsWhiteSpace(line[basePos]))
                                    {
                                        basePos++;
                                    }
                                    if (basePos < line.Length && line[basePos] == ':')
                                    {
                                        basePos++;
                                        while (basePos < line.Length && line[basePos] != '{')
                                        {
                                            if (char.IsWhiteSpace(line[basePos]) || line[basePos] == ',')
                                            {
                                                basePos++;
                                                continue;
                                            }
                                            int baseIdentBegin = basePos;
                                            while (basePos < line.Length && TestProject.IsIdentifier(line[basePos]))
                                            {
                                                basePos++;
                                            }
                                            if (basePos < line.Length && line[basePos] == '<')
                                            {
                                                int genericNesting = 1;
                                                basePos++;
                                                while (basePos < line.Length)
                                                {
                                                    char c = line[basePos++];
                                                    if (c == '<')
                                                    {
                                                        genericNesting++;
                                                    }
                                                    else if (c == '>')
                                                    {
                                                        if (--genericNesting == 0)
                                                        {
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                            if (basePos > baseIdentBegin)
                                            {
                                                testClassBases.Add(line.Substring(baseIdentBegin, basePos - baseIdentBegin));
                                            }
                                        }
                                    }

                                    while (--mainLine >= 0)
                                    {
                                        line = lines[mainLine];
                                        int namespacePos = line.IndexOf("namespace ");
                                        if (namespacePos >= 0)
                                        {
                                            int namespaceNameStart = namespacePos + 10;
                                            int namespaceNameEnd = namespaceNameStart;
                                            while (namespaceNameEnd < line.Length && TestProject.IsIdentifier(line[namespaceNameEnd]))
                                            {
                                                namespaceNameEnd++;
                                            }
                                            string namespaceName = line.Substring(namespaceNameStart, namespaceNameEnd - namespaceNameStart);
                                            testClassName = namespaceName + "." + testClassName;
                                        }
                                    }
                                }
                            }
                            while (--mainLine >= 0);
                        }
                    }
                    break;
                }
            }

            if (isMainFile)
            {
                lastUsingLine = 0;
                for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    string line = lines[lineIndex];
                    if (line.StartsWith("using"))
                    {
                        lastUsingLine = lineIndex;
                    }
                }

                for (int lineIndex = lastUsingLine + 1; lineIndex < lines.Count; lineIndex++)
                {
                    string line = lines[lineIndex].Trim();
                    if (line == "")
                    {
                        continue;
                    }
                    namespaceLine = lineIndex;
                    if (line.StartsWith("namespace "))
                    {
                        int namespaceNameStart = 10;
                        int namespaceNameEnd = namespaceNameStart;
                        while (namespaceNameEnd < line.Length && TestProject.IsIdentifier(line[namespaceNameEnd]))
                        {
                            namespaceNameEnd++;
                        }
                        testClassNamespace = line.Substring(namespaceNameStart, namespaceNameEnd - namespaceNameStart);
                    }
                    break;
                }
            }
        }

        private static void AnalyzeILSource(
            string path,
            ref string testClassName,
            ref List<string> testClassBases,
            ref string testClassNamespace,
            ref string testClassSourceFile,
            ref int testClassLine,
            ref int mainMethodLine,
            ref int lastUsingLine,
            ref int namespaceLine,
            ref bool hasFactAttribute)
        {
            if (Path.GetFileName(path) == "han3.il")
            {
                Console.WriteLine("AnalyzeILSource: {0}", path);
            }

            List<string> lines = new List<string>(File.ReadAllLines(path));

            int lineIndex = lines.Count;
            while (--lineIndex >= 0)
            {
                string line = lines[lineIndex];
                if (line.Contains(".entrypoint"))
                {
                    break;
                }
            }

            while (--lineIndex >= 0 && !lines[lineIndex].Contains(".method"))
            {
            }

            if (lineIndex >= 0 && lines[lineIndex].Contains("static"))
            {
                for (int endIndex = lineIndex + 2; lineIndex < endIndex; lineIndex++)
                {
                    const string MainTag = " Main(";
                    const string mainTag = " main(";
                    string line = lines[lineIndex];
                    int mainPos = line.IndexOf(MainTag);
                    if (mainPos < 0)
                    {
                        mainPos = line.IndexOf(mainTag);
                    }
                    if (mainPos >= 0)
                    {
                        mainMethodLine = lineIndex;
                        for (int factIndex = lineIndex; factIndex < lineIndex + 10 && factIndex < lines.Count; factIndex++)
                        {
                            if (lines[factIndex].Contains("FactAttribute"))
                            {
                                hasFactAttribute = true;
                                break;
                            }
                        }
                    }
                    const string TestEntrypointTag = " TestEntrypoint(";
                    bool hasEntrypoint = line.Contains(TestEntrypointTag);
                    if (mainPos >= 0 || hasEntrypoint)
                    {
                        testClassSourceFile = path;

                        while (--lineIndex >= 0 && testClassName == "")
                        {
                            if (TestProject.GetILClassName(lines[lineIndex], out string? className))
                            {
                                testClassName = className!;
                                testClassLine = lineIndex;
                                while (--lineIndex >= 0)
                                {
                                    string[] components = lines[lineIndex].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    if (components.Length >= 2 && components[0] == ".namespace")
                                    {
                                        string namespaceName = components[1];
                                        if (namespaceName.StartsWith("\'"))
                                        {
                                            namespaceName = namespaceName.Substring(1, namespaceName.Length - 2);
                                        }
                                        testClassNamespace = namespaceName;
                                        namespaceLine = lineIndex;
                                        testClassName = namespaceName + "." + testClassName;
                                    }
                                }
                                break;
                            }
                        }

                        break;
                    }
                }
            }

            if (namespaceLine < 0)
            {
                for (int index = 0; index < lines.Count; index++)
                {
                    string line = lines[index];
                    if (line.Contains(".class") || line.Contains(".struct"))
                    {
                        namespaceLine = index;
                        break;
                    }
                }
            }
        }

        private void PopulateClassNameMap()
        {
            HashSet<string> ilNamespaceClasses = new HashSet<string>();
            Dictionary<string, HashSet<string>> compileFileToFolderNameMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (TestProject project in _projects.Where(p => p.TestClassName != "" && p.MainMethodLine > 0))
            {
                if (project.RelativePath.Contains("hfa_params"))
                {
                    Console.WriteLine("Project: {0}", project.AbsolutePath);
                }

                if (!_classNameMap.TryGetValue(project.TestClassName, out List<TestProject>? projectList))
                {
                    projectList = new List<TestProject>();
                    _classNameMap.Add(project.TestClassName, projectList);
                }
                projectList!.Add(project);

                if (project.CompileFiles.Any(f => Path.GetFileNameWithoutExtension(f) == "accum"))
                {
                    Console.WriteLine("Project: {0}", project.AbsolutePath);
                }

                string namespaceClass = project.TestClassNamespace + "#" + project.TestClassName;
                if (!_namespaceNameMap.TryGetValue(namespaceClass, out Dictionary<DebugOptimize, List<TestProject>>? debugOptProjectMap))
                {
                    debugOptProjectMap = new Dictionary<DebugOptimize, List<TestProject>>();
                    _namespaceNameMap.Add(namespaceClass, debugOptProjectMap);
                }
                if (!debugOptProjectMap!.TryGetValue(project.DebugOptimize, out projectList))
                {
                    projectList = new List<TestProject>();
                    debugOptProjectMap!.Add(project.DebugOptimize, projectList);
                }
                projectList!.Add(project);

                foreach (string file in project.CompileFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string folderName = Path.GetFileName(Path.GetDirectoryName(file)!);
                    if (!compileFileToFolderNameMap.TryGetValue(fileName, out HashSet<string>? folders))
                    {
                        folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        compileFileToFolderNameMap.Add(fileName, folders);
                    }
                    folders.Add(folderName);
                }
            }

            foreach (List<TestProject> projectList in _classNameMap.Values.Where(v => v.Count > 1))
            {
                Dictionary<DebugOptimize, int> counts = new Dictionary<DebugOptimize, int>();
                foreach (TestProject project in projectList)
                {
                    counts.TryGetValue(project.DebugOptimize, out int count);
                    counts[project.DebugOptimize] = count + 1;
                }

                if (counts.Values.Any(c => c > 1))
                {
                    foreach (TestProject project in projectList)
                    {
                        project.DeduplicatedClassName = TestProject.SanitizeIdentifier(project.TestClassName + "_" + Path.GetFileNameWithoutExtension(project.TestClassSourceFile));
                    }
                }
            }

            HashSet<TestProject> ilProjects = new HashSet<TestProject>(_projects.Where(prj => prj.IsILProject));

            foreach (Dictionary<DebugOptimize, List<TestProject>> debugOptProjectMap in _namespaceNameMap.Values.Where(dict => dict.Values.Any(l => l.Count > 1)))
            {
                if (debugOptProjectMap.Values.Any(prjList => prjList.Any(prj => prj.CompileFiles.Any(f => Path.GetFileNameWithoutExtension(f) == "accum"))))
                {
                    Console.WriteLine("accum!");
                }

                bool haveCsAndIlVersion =
                    debugOptProjectMap.Values.Any(l => l.Any(prj => prj.IsILProject)) &&
                    debugOptProjectMap.Values.Any(l => l.Any(prj => !prj.IsILProject));

                bool existsInMultipleFolders = debugOptProjectMap.Values.Any(pl => pl.Any(
                    p => p.CompileFiles.Any(
                        cf => compileFileToFolderNameMap.TryGetValue(Path.GetFileName(cf), out HashSet<string>? fl) && fl.Count > 1)));

                foreach (TestProject project in debugOptProjectMap.Values.SelectMany(v => v))
                {
                    if (project.RelativePath.Contains("hfa_params"))
                    {
                        Console.WriteLine("Project: {0}", project.AbsolutePath);
                    }

                    if (Path.GetFileName(project.TestClassSourceFile) == "hfa_params.cs")
                    {
                        Console.WriteLine("Project: {0}", project.AbsolutePath);
                    }

                    string deduplicatedName = project.TestClassNamespace;
                    if (deduplicatedName == "")
                    {
                        deduplicatedName = "Test";
                    }
                    deduplicatedName += "_" + Path.GetFileNameWithoutExtension(project.TestClassSourceFile);
                    if (existsInMultipleFolders)
                    {
                        deduplicatedName += "_" + Path.GetFileName(Path.GetDirectoryName(project.TestClassSourceFile)!);
                    }
                    if (haveCsAndIlVersion)
                    {
                        deduplicatedName += (project.IsILProject ? "_il" : "_cs");
                    }
                    project.DeduplicatedNamespaceName = TestProject.SanitizeIdentifier(deduplicatedName);
                }
            }
        }
    }
}
