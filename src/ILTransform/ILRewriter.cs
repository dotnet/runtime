// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Security.Authentication.ExtendedProtection;
using System.Text;

namespace ILTransform
{
    public class ILRewriter
    {
        private static string[] s_xUnitLines =
        {
            ".assembly extern xunit.core {}",
        };

        private static string[] s_factLines =
        {
            ".custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (",
            "    01 00 00 00",
            ")",
        };

        private readonly TestProject _testProject;
        private readonly HashSet<string> _classNameDuplicates;
        private readonly bool _deduplicateClassNames;
        private readonly HashSet<string> _rewrittenFiles;
        private readonly bool _addILFactAttributes;
        private readonly bool _cleanupILModuleAssembly;

        public ILRewriter(
            TestProject testProject,
            HashSet<string> classNameDuplicates,
            bool deduplicateClassNames,
            HashSet<string> rewrittenFiles,
            bool addILFactAttributes,
            bool cleanupILModuleAssembly)
        {
            _testProject = testProject;
            _classNameDuplicates = classNameDuplicates;
            _deduplicateClassNames = deduplicateClassNames;
            _rewrittenFiles = rewrittenFiles;
            _addILFactAttributes = addILFactAttributes;
            _cleanupILModuleAssembly = cleanupILModuleAssembly;
        }

        public void Rewrite()
        {
            if (!string.IsNullOrEmpty(_testProject.TestClassSourceFile) && _rewrittenFiles.Add(_testProject.TestClassSourceFile))
            {
                RewriteFile(_testProject.TestClassSourceFile);
            }
            if (!_deduplicateClassNames && !_cleanupILModuleAssembly)
            {
                RewriteProject(_testProject.AbsolutePath);
            }
        }

        private void RewriteFile(string ilSource)
        {
            List<string> lines = new List<string>(File.ReadAllLines(ilSource));
            bool isILTest = Path.GetExtension(ilSource).ToLower() == ".il";
            bool rewritten = false;

            if (Path.GetFileName(ilSource).Equals("instance.il", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("RewriteFile: {0}", ilSource);
            }

            if (_testProject.MainMethodLine >= 0 /*&& !_cleanupILModuleAssembly*/)
            {
                int lineIndex = _testProject.MainMethodLine;
                string line = lines[lineIndex];
                const string MainTag = " Main(";
                const string mainTag = " main(";
                int mainPos = line.IndexOf(MainTag);
                if (mainPos < 0)
                {
                    mainPos = line.IndexOf(mainTag);
                }
                if (mainPos >= 0)
                {
                    int lineInBody = lineIndex;
                    while (!lines[lineInBody].Contains('{'))
                    {
                        if (++lineInBody >= lines.Count)
                        {
                            Console.Error.WriteLine("Opening brace for main method not found in file: {0}", ilSource);
                            break;
                        }
                    }

                    if (_addILFactAttributes && !_testProject.HasFactAttribute)
                    {
                        int indentLine = (isILTest ? lineInBody + 1 : lineIndex);
                        string firstMainBodyLine = lines[indentLine];
                        int indent = TestProject.GetIndent(firstMainBodyLine);
                        string indentString = firstMainBodyLine.Substring(0, indent);
                        if (isILTest)
                        {
                            string[] indentedFactLines = new string[s_factLines.Length];
                            for (int i = 0; i < s_factLines.Length; i++)
                            {
                                indentedFactLines[i] = indentString + s_factLines[i];
                            }
                            lines.InsertRange(lineInBody + 1, indentedFactLines);
                        }
                        else
                        {
                            lines[lineIndex] = ReplaceIdent(line, "Main", "TestEntryPoint");
                            lines.Insert(lineIndex++, indentString + "[Fact]");
                            rewritten = true;
                        }
                    }

                    if (!_cleanupILModuleAssembly)
                    {
                        if (isILTest)
                        {
                            while (lineIndex >= 0)
                            {
                                line = lines[lineIndex];
                                bool isMethodLine = line.Contains(".method ");
                                if (TestProject.MakePublic(isILTest: isILTest, ref line, force: isMethodLine))
                                {
                                    lines[lineIndex] = line;
                                    rewritten = true;
                                    break;
                                }
                                if (isMethodLine)
                                {
                                    break;
                                }
                                lineIndex--;
                            }
                        }
                        else
                        {
                            line = lines[lineIndex];
                            TestProject.MakePublic(isILTest: isILTest, ref line, force: true);
                            lines[lineIndex] = line;
                            rewritten = true;
                        }

                        foreach (string baseClassName in _testProject.TestClassBases)
                        {
                            for (int index = 0; index < lines.Count; index++)
                            {
                                line = lines[index];
                                if (index != _testProject.TestClassLine &&
                                    (line.Contains("class") || line.Contains("struct")) &&
                                    line.Contains(baseClassName))
                                {
                                    if (TestProject.MakePublic(isILTest: isILTest, ref line, force: true))
                                    {
                                        lines[index] = line;
                                        rewritten = true;
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    /*
                    int closingParen = line.IndexOf(')', mainPos + MainTag.Length);
                    if (!_deduplicateClassNames)
                    {
                        string replacement = " Test(";
                        lines[lineIndex] = line.Substring(0, mainPos) + replacement + line.Substring(closingParen);
                        rewritten = true;
                    }
                    lines[lineIndex] = line.Substring(0, mainPos) + replacement + line.Substring(mainPos + MainTag.Length);
                    rewritten = true;
                    */

                    /*
                    for (int privateIndex = lineIndex; privateIndex >= lineIndex - 1 && privateIndex >= 0; privateIndex--)
                    {
                        line = lines[privateIndex];
                        int privatePos = line.IndexOf("private ");
                        if (privatePos >= 0)
                        {
                            if (!_deduplicateClassNames)
                            {
                                line = line.Substring(0, privatePos) + "public" + line.Substring(privatePos + 7);
                                lines[privateIndex] = line;
                                rewritten = true;
                            }
                            break;
                        }
                        int publicPos = line.IndexOf("public ");
                        if (publicPos >= 0)
                        {
                            break;
                        }
                    }
                    */
                }
            }

            if (_testProject.TestClassLine < 0)
            {
                if (isILTest)
                {
                    string classLine = $".class public auto ansi Test_{Path.GetFileNameWithoutExtension(ilSource)} extends [mscorlib] System.Object {{";
                    lines.Insert(_testProject.MainMethodLine, classLine);
                    lines.Add("}");
                }
            }
            else if (!_cleanupILModuleAssembly)
            {
                string line = lines[_testProject.TestClassLine];
                TestProject.MakePublic(isILTest: isILTest, ref line, force: true);
                lines[_testProject.TestClassLine] = line;
                rewritten = true;
            }

            if (!_deduplicateClassNames && !_cleanupILModuleAssembly)
            {
                bool hasXunitReference = false;
                string testName = _testProject.TestProjectAlias!;
                bool addFactAttribute = _addILFactAttributes && !_testProject.HasFactAttribute && isILTest;
                if (isILTest)
                {
                    for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                    {
                        string line = lines[lineIndex];
                        if (line.StartsWith(".assembly extern xunit.core"))
                        {
                            hasXunitReference = true;
                            if (!line.Contains('}'))
                            {
                                int endLine = lineIndex;
                                do
                                {
                                    endLine++;
                                }
                                while (!lines[endLine].Contains('}'));
                                lines.RemoveRange(lineIndex + 1, endLine - lineIndex);
                                lines[lineIndex] = s_xUnitLines[0];
                                rewritten = true;
                            }
                            break;
                        }
                    }

                    for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                    {
                        string line = lines[lineIndex];
                        if (line.StartsWith(".assembly"))
                        {
                            while (!lines[lineIndex].Contains('}'))
                            {
                                lineIndex++;
                            }

                            line = lines[++lineIndex];
                            if (addFactAttribute && !hasXunitReference)
                            {
                                lines.InsertRange(lineIndex, s_xUnitLines);
                                rewritten = true;
                            }
                            break;
                        }

                        /*
                        int start = assemblyIndex + AssemblyTag.Length;
                        for (; ;)
                        {
                            int start = assemblyIndex + AssemblyTag.Length;
                            for (; ; )
                            {
                                while (start < line.Length && Char.IsWhiteSpace(line[start]))
                                {
                                    start++;
                                }
                                const string LibraryTag = "library";
                                if (start + LibraryTag.Length <= line.Length && line.Substring(start, LibraryTag.Length) == LibraryTag)
                                {
                                    start += LibraryTag.Length;
                                    continue;
                                }
                                const string LegacyTag = "legacy";
                                if (start + LegacyTag.Length <= line.Length && line.Substring(start, LegacyTag.Length) == LegacyTag)
                                {
                                    start += LegacyTag.Length;
                                    continue;
                                }

                                if (start + 2 <= line.Length && line[start] == '/' && line[start + 1] == '*')
                                {
                                    start += 2;
                                    while (start + 2 <= line.Length && !(line[start] == '*' && line[start + 1] == '/'))
                                    {
                                        start++;
                                    }
                                    continue;
                                }
                                break;
                            }
                            bool quoted = (start < line.Length && line[start] == '\'');
                            if (quoted)
                            {
                                start++;
                            }
                            int end = start;
                            while (end < line.Length && line[end] != '\'' && (quoted || TestProject.IsIdentifier(line[end])))
                            {
                                end++;
                            }
                            string ident = line.Substring(start, end - start);
                            if (ident != testName)
                            {
                                line = line.Substring(0, start) + (quoted ? "" : "'") + testName + (quoted ? "" : "'") + line.Substring(end);
                                lines[lineIndex] = line;
                                rewritten = true;
                                break;
                            }
                        }
                        */
                    }
                }

                if (_testProject.TestClassNamespace == "" && _testProject.DeduplicatedNamespaceName != null)
                {
                    int lineIndex = _testProject.NamespaceLine;
                    lines.Insert(lineIndex, (isILTest ? "." : "") + "namespace " + _testProject.DeduplicatedNamespaceName);
                    lines.Insert(lineIndex + 1, "{");
                    lines.Add("}");
                    for (int i = lineIndex; i < lines.Count; i++)
                    {
                        if (TestProject.GetILClassName(lines[i], out string? className))
                        {
                            string qualifiedClassName = _testProject.DeduplicatedNamespaceName + "." + className!;
                            for (int s = lineIndex; s < lines.Count; s++)
                            {
                                if (s != i)
                                {
                                    lines[s] = TestProject.ReplaceIdentifier(lines[s], className!, qualifiedClassName);
                                }
                            }
                        }
                    }

                    rewritten = true;
                }
                else if (_testProject.DeduplicatedNamespaceName != null)
                {
                    if (isILTest)
                    {
                        for (int lineIndex = _testProject.NamespaceLine; lineIndex < lines.Count; lineIndex++)
                        {
                            lines[lineIndex] = TestProject.ReplaceIdentifier(lines[lineIndex], _testProject.TestClassNamespace, _testProject.DeduplicatedNamespaceName);
                        }
                    }
                    else
                    {
                        lines[_testProject.NamespaceLine] = lines[_testProject.NamespaceLine].Replace(_testProject.TestClassNamespace, _testProject.DeduplicatedNamespaceName);
                    }
                    rewritten = true;
                }

                if (!isILTest)
                {
                    bool usingXUnit = (_testProject.LastUsingLine >= 0 && lines[_testProject.LastUsingLine].Contains("Xunit"));
                    int rewriteLine = _testProject.LastUsingLine;
                    rewriteLine++;
                    if (!usingXUnit)
                    {
                        lines.Insert(rewriteLine++, "using Xunit;");
                        rewritten = true;
                    }
                }
            }

            if (_cleanupILModuleAssembly && isILTest)
            {
                for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    if (lines[lineIndex].Contains(".module"))
                    {
                        lines.RemoveAt(lineIndex);
                        rewritten = true;
                        break;
                    }
                }
                for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    string line = lines[lineIndex];
                    int assemblyIndex = line.IndexOf(".assembly");
                    if (assemblyIndex >= 0)
                    {
                        for (int charIndex = assemblyIndex + 9; charIndex < line.Length; charIndex++)
                        {
                            if (char.IsWhiteSpace(line[charIndex]))
                            {
                                continue;
                            }
                            if (line[charIndex] == '/' && charIndex + 1 < line.Length && line[charIndex + 1] == '*')
                            {
                                charIndex += 2;
                                while (charIndex + 1 < line.Length && !(line[charIndex] == '*' && line[charIndex + 1] == '/'))
                                {
                                    charIndex++;
                                }
                                charIndex++;
                                continue;
                            }
                            int identStart = charIndex;
                            string assemblyName;
                            if (line[charIndex] == '\'')
                            {
                                charIndex++;
                                while (charIndex < line.Length && line[charIndex++] != '\'')
                                {
                                }
                                assemblyName = line.Substring(identStart + 1, charIndex - identStart - 2);
                            }
                            else
                            {
                                while (charIndex < line.Length && TestProject.IsIdentifier(line[charIndex]))
                                {
                                    charIndex++;
                                }
                                assemblyName = line.Substring(identStart, charIndex - identStart);

                                if (assemblyName == "extern")
                                {
                                    break;
                                }
                                if (assemblyName == "legacy" || assemblyName == "library")
                                {
                                    continue;
                                }
                            }
                            int identEnd = charIndex;
                            string sourceName = Path.GetFileNameWithoutExtension(ilSource);
                            if (sourceName != assemblyName)
                            {
                                line = line.Substring(0, identStart) + '\'' + sourceName + '\'' + line.Substring(identEnd);
                                lines[lineIndex] = line;
                                rewritten = true;
                            }
                            break;
                        }
                    }
                }
            }

            /*
            if (_testProject.DeduplicatedClassName != null)
            {
                for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    lines[lineIndex] = ReplaceIdent(lines[lineIndex], _testProject.TestClassName, _testProject.DeduplicatedClassName);
                }
                rewritten = true;
            }
            */

            if (rewritten)
            {
                File.WriteAllLines(ilSource, lines);
            }
        }

        private void RewriteProject(string path)
        {
            List<string> lines = new List<string>(File.ReadAllLines(path));
            bool rewritten = false;
            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                string line = lines[lineIndex];
                const string outputTypeTag = "<OutputType>Exe</OutputType>";
                bool containsOutputType = line.Contains(outputTypeTag);
                if (_addILFactAttributes && containsOutputType)
                {
                    lines.RemoveAt(lineIndex--);
                    rewritten = true;
                    continue;
                }
                /*
                const string testKindTag = "<CLRTestKind>BuildAndRun</CLRTestKind>";
                int testKindIndex = line.IndexOf(testKindTag);
                if (testKindIndex >= 0)
                {
                    lines[lineIndex] = line.Substring(0, testKindIndex) + "<CLRTestKind>BuildOnly</CLRTestKind>";
                    rewritten = true;
                    continue;
                }
                */
            }
            if (rewritten)
            {
                File.WriteAllLines(path, lines);
            }
        }

        private static string ReplaceIdent(string source, string searchIdent, string replaceIdent)
        {
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < source.Length;)
            {
                char c = source[index];
                if (c == '\"')
                {
                    builder.Append(c);
                    while (++index < source.Length && source[index] != '\"')
                    {
                        builder.Append(source[index]);
                    }
                    if (index < source.Length)
                    {
                        builder.Append(source[index++]);
                    }
                }
                else if (c == '/' && index + 1 < source.Length && source[index + 1] == '/')
                {
                    // Comment - copy over rest of line
                    builder.Append(source, index, source.Length - index);
                    break;
                }
                else if (!TestProject.IsIdentifier(c))
                {
                    builder.Append(c);
                    index++;
                }
                else
                {
                    int start = index;
                    while (index < source.Length && TestProject.IsIdentifier(source[index]))
                    {
                        index++;
                    }
                    string ident = source.Substring(start, index - start);
                    builder.Append(ident == searchIdent ? replaceIdent : ident);
                }
            }
            return builder.ToString();
        }
    }
}
