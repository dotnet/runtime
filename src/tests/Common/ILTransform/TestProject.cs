// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

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
}

public struct TestCount
{
    public int Total;
    public int Pri0;

    public int Pri1 => Total - Pri0;

    public TestCount(int total, int pri0)
    {
        Total = total;
        Pri0 = pri0;
    }
}

public class TestProject
{
    public readonly string AbsolutePath;
    public readonly string RelativePath;
    public readonly string OutputType;
    public readonly string CLRTestKind;
    public readonly DebugOptimize DebugOptimize;
    public readonly string Priority;
    public readonly string[] CompileFiles;
    public readonly string[] ProjectReferences;
    public readonly string TestClassName;
    public readonly string TestClassSourceFile;
    public readonly int TestClassLine;
    public readonly int MainMethodLine;

    public string? TestProjectAlias;
    public string? DeduplicatedClassName;

    public TestProject(
        string absolutePath,
        string relativePath,
        string outputType,
        string clrTestKind,
        DebugOptimize debugOptimize,
        string priority,
        string[] compileFiles,
        string[] projectReferences,
        string testClassName,
        string testClassSourceFile,
        int testClassLine,
        int mainMethodLine)
    {
        AbsolutePath = absolutePath;
        RelativePath = relativePath;
        OutputType = outputType;
        CLRTestKind = clrTestKind;
        DebugOptimize = debugOptimize;
        Priority = priority;
        CompileFiles = compileFiles;
        ProjectReferences = projectReferences;
        TestClassName = testClassName;
        TestClassSourceFile = testClassSourceFile;
        TestClassLine = testClassLine;
        MainMethodLine = mainMethodLine;
    }

    public static bool IsIdentifier(char c)
    {
        return Char.IsDigit(c) || Char.IsLetter(c) || c == '_' || c == '@';
    }

    public static string SanitizeIdentifier(string source)
    {
        StringBuilder output = new StringBuilder();
        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];
            if (IsIdentifier(c))
            {
                if (Char.IsDigit(c) && output.Length == 0)
                {
                    output.Append('_');
                }
                output.Append(c);
            }
            else if (c == '-')
            {
                output.Append("_");
            }
            else
            {
                output.Append("__");
            }
        }
        return output.ToString();
    }
}

class TestProjectStore
{
    private readonly List<TestProject> _projects;
    private readonly Dictionary<string, List<TestProject>> _classNameMap;
    private readonly HashSet<string> _rewrittenFiles;

    public TestProjectStore()
    {
        _projects = new List<TestProject>();
        _classNameMap = new Dictionary<string, List<TestProject>>();
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

    public void DumpFolderStatistics(TextWriter writer)
    {
        for (int level = 1; level <= 2; level++)
        {
            string title = string.Format("COUNT |  PRI0 |  PRI1 | PARTITIONING: {0}", level);
            writer.WriteLine(title);
            writer.WriteLine(new string('-', title.Length));
            Dictionary<string, TestCount> folderCounts = new Dictionary<string, TestCount>();

            foreach (TestProject project in _projects)
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
                folderCounts.TryGetValue(folderName, out TestCount count);
                count.Total++;
                if (project.Priority != "1")
                {
                    count.Pri0++;
                }
                folderCounts[folderName] = count;
            }
            foreach (KeyValuePair<string, TestCount> kvp in folderCounts.OrderBy(kvp => kvp.Key))
            {
                writer.WriteLine("{0,5} | {1,5} | {2,5} | {3}", kvp.Value.Total, kvp.Value.Pri0, kvp.Value.Pri1, kvp.Key);
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

        foreach (KeyValuePair<string, List<TestProject>> kvp in duplicateClassNames.OrderByDescending(kvp => kvp.Value.Count))
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

    public void RewriteAllTests(bool deduplicateClassNames, string classToDeduplicate)
    {
        HashSet<string> classNameDuplicates = new HashSet<string>(_classNameMap.Where(kvp => kvp.Value.Count > 1).Select(kvp => kvp.Key));

        int index = 0;
        foreach (TestProject project in _projects)
        {
            if (!string.IsNullOrEmpty(classToDeduplicate) && project.TestClassName != classToDeduplicate)
            {
                continue;
            }
            new ILRewriter(project, classNameDuplicates, deduplicateClassNames, _rewrittenFiles).Rewrite();
            index++;
            if (index % 500 == 0)
            {
                Console.WriteLine("Rewritten {0} / {1} projects", index, _projects.Count);
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

            TestProject[] projectGroup = projects[firstProject .. Math.Min(projects.Length, firstProject + maxProjectsPerWrapper)];

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

        string outputType = "";
        string testKind = "";
        string priority = "";
        string debugType = "";
        string optimize = "";
        List<string> compileFiles = new List<string>();
        List<string> projectReferences = new List<string>();

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
                        switch (property.Name)
                        {
                            case "OutputType":
                                outputType = property.InnerText;
                                break;

                            case "CLRTestPriority":
                                priority = property.InnerText;
                                break;

                            case "CLRTestKind":
                                testKind = property.InnerText;
                                break;

                            case "DebugType":
                                debugType = property.InnerText;
                                break;

                            case "Optimize":
                                optimize = property.InnerText;
                                break;
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
        string testClassSourceFile = "";
        int testClassLine = -1;
        int mainMethodLine = -1;
        foreach (string compileFile in compileFiles)
        {
            AnalyzeSource(
                compileFile,
                testClassName: ref testClassName,
                testClassSourceFile: ref testClassSourceFile,
                testClassLine: ref testClassLine,
                mainMethodLine: ref mainMethodLine);
        }

        _projects.Add(new TestProject(
            absolutePath,
            relativePath,
            outputType,
            testKind,
            new DebugOptimize(InitCaps(debugType), InitCaps(optimize)),
            priority,
            compileFiles.ToArray(),
            projectReferences.ToArray(),
            testClassName,
            testClassSourceFile,
            testClassLine,
            mainMethodLine));
    }

    private static string InitCaps(string s)
    {
        if (s.Length > 0)
        {
            s = s.Substring(0, 1).ToUpper() + s.Substring(1);
        }
        if (s.Equals("pdbonly", StringComparison.OrdinalIgnoreCase))
        {
            s = "PdbOnly";
        }
        return s;
    }

    private static void AnalyzeSource(string path, ref string testClassName, ref string testClassSourceFile, ref int testClassLine, ref int mainMethodLine)
    {
        if (path.IndexOf('*') < 0 && path.IndexOf('?') < 0)
        {
            // Exact path
            AnalyzeFileSource(path, ref testClassName, ref testClassSourceFile, ref testClassLine, ref mainMethodLine);
            return;
        }

        string directory = Path.GetDirectoryName(path)!;
        string pattern = Path.GetFileName(path);
        SearchOption searchOption = SearchOption.TopDirectoryOnly;
        bool subtreePattern = false;
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
            AnalyzeFileSource(file, ref testClassName, ref testClassSourceFile, ref testClassLine, ref mainMethodLine);
        }
    }

    private static void AnalyzeFileSource(string path, ref string testClassName, ref string testClassSourceFile, ref int testClassLine, ref int mainMethodLine)
    {
        switch (Path.GetExtension(path).ToLower())
        {
            case ".il":
                AnalyzeILSource(path, ref testClassName, ref testClassSourceFile, ref testClassLine, ref mainMethodLine);
                break;

            case ".cs":
                AnalyzeCSSource(path, ref testClassName, ref testClassSourceFile, ref testClassLine, ref mainMethodLine);
                break;

            default:
                Console.Error.WriteLine("Cannot analyze source file '{0}'", path);
                break;
        }
    }

    private static int GetIndent(string line)
    {
        int indent = 0;
        while (indent < line.Length && Char.IsWhiteSpace(line[indent]))
        {
            indent++;
        }
        return indent;
    }

    private static bool IsIdentifier(char c)
    {
        return Char.IsDigit(c) || Char.IsLetter(c) || c == '_' || c == '@';
    }

    private static void AnalyzeCSSource(string path, ref string testClassName, ref string testClassSourceFile, ref int testClassLine, ref int mainMethodLine)
    {
        List<string> lines = new List<string>(File.ReadAllLines(path));

        string fileName = Path.GetFileNameWithoutExtension(path);

        for (int mainLine = lines.Count; --mainLine >= 0;)
        {
            string line = lines[mainLine];
            int mainPos = line.IndexOf("int Main()");
            if (mainPos >= 0)
            {
                int mainLineIndent = GetIndent(line);
                mainMethodLine = mainLine;
                testClassSourceFile = path;
                while (--mainLine >= 0)
                {
                    line = lines[mainLine];
                    int lineIndent = GetIndent(line);
                    if (lineIndent < mainLineIndent && line.IndexOf('{') >= 0)
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
                                while (line.Length > classNameEnd && IsIdentifier(line[classNameEnd]))
                                {
                                    classNameEnd++;
                                }
                                testClassName = line.Substring(classNameStart, classNameEnd - classNameStart);
                                testClassLine = mainLine;

                                while (--mainLine >= 0)
                                {
                                    line = lines[mainLine];
                                    int namespacePos = line.IndexOf("namespace ");
                                    if (namespacePos >= 0)
                                    {
                                        int namespaceNameStart = namespacePos + 10;
                                        int namespaceNameEnd = namespaceNameStart;
                                        while (namespaceNameEnd < line.Length && IsIdentifier(line[namespaceNameEnd]))
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
            }
        }
    }

    private static void AnalyzeILSource(string path, ref string testClassName, ref string testClassSourceFile, ref int testClassLine, ref int mainMethodLine)
    {
        List<string> lines = new List<string>(File.ReadAllLines(path));

        for (int lineIndex = lines.Count; --lineIndex >= 0;)
        {
            string line = lines[lineIndex];
            const string MainTag = " Main(";
            int mainPos = line.IndexOf(MainTag);
            if (mainPos >= 0)
            {
                mainMethodLine = lineIndex;
            }
            const string TestEntrypointTag = " TestEntrypoint(";
            int entrypointPos = line.IndexOf(TestEntrypointTag);
            if (mainPos >= 0 || entrypointPos >= 0)
            {
                while (--lineIndex >= 0 && testClassName == "")
                {
                    string[] components = lines[lineIndex].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (components.Length >= 2 && components[0] == ".class")
                    {
                        for (int componentIndex = 1; componentIndex < components.Length; componentIndex++)
                        {
                            string component = components[componentIndex];
                            if (!component.StartsWith("/*") &&
                                component != "auto" &&
                                component != "ansi" &&
                                component != "interface" &&
                                component != "public" &&
                                component != "private" &&
                                component != "sealed" &&
                                component != "nested" &&
                                component != "value" &&
                                component != "beforefieldinit" &&
                                component != "sequential" &&
                                component != "explicit" &&
                                component != "abstract")
                            {
                                int identEnd = 0;
                                while (identEnd < component.Length && TestProject.IsIdentifier(component[identEnd]))
                                {
                                    identEnd++;
                                }
                                testClassName = component.Substring(0, identEnd);
                                testClassSourceFile = path;
                                testClassLine = lineIndex;
                                while (--lineIndex >= 0)
                                {
                                    components = lines[lineIndex].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    if (components.Length >= 2 && components[0] == ".namespace")
                                    {
                                        string namespaceName = components[1];
                                        if (namespaceName.StartsWith("\'"))
                                        {
                                            namespaceName = namespaceName.Substring(1, namespaceName.Length - 2);
                                        }
                                        testClassName = namespaceName + "." + testClassName;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }

                break;
            }
        }
    }

    private void PopulateClassNameMap()
    {
        foreach (TestProject project in _projects.Where(p => p.TestClassName != "" && p.MainMethodLine > 0))
        {
            if (!_classNameMap.TryGetValue(project.TestClassName!, out List<TestProject>? projectList))
            {
                projectList = new List<TestProject>();
                _classNameMap.Add(project.TestClassName!, projectList);
            }
            projectList!.Add(project);
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
    }
}
