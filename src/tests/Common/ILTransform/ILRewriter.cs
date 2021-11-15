// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;

public class ILRewriter
{
    private readonly TestProject _testProject;
    private readonly HashSet<string> _classNameDuplicates;
    private readonly bool _deduplicateClassNames;
    private readonly HashSet<string> _rewrittenFiles;

    public ILRewriter(TestProject testProject, HashSet<string> classNameDuplicates, bool deduplicateClassNames, HashSet<string> rewrittenFiles)
    {
        _testProject = testProject;
        _classNameDuplicates = classNameDuplicates;
        _deduplicateClassNames = deduplicateClassNames;
        _rewrittenFiles = rewrittenFiles;
    }

    public void Rewrite()
    {
        if (!string.IsNullOrEmpty(_testProject.TestClassSourceFile) && _rewrittenFiles.Add(_testProject.TestClassSourceFile))
        {
            RewriteFile(_testProject.TestClassSourceFile);
        }
        if (!_deduplicateClassNames)
        {
            RewriteProject(_testProject.AbsolutePath);
        }
    }

    private void RewriteFile(string ilSource)
    {
        List<string> lines = new List<string>(File.ReadAllLines(ilSource));
        bool rewritten = false;

        if (_testProject.MainMethodLine >= 0)
        {
            int lineIndex = _testProject.MainMethodLine;
            string line = lines[lineIndex];
            const string MainTag = " Main(";
            int mainPos = line.IndexOf(MainTag);
            if (mainPos >= 0)
            {
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
            }
        }

        if (_testProject.TestClassLine >= 0)
        {
            string line = lines[_testProject.TestClassLine];
            if (line.IndexOf("public") < 0)
            {
                if (!_deduplicateClassNames)
                {
                    lines[_testProject.TestClassLine] = line.Replace(" private ", " public ");
                    rewritten = true;
                }
            }
        }

        if (!_deduplicateClassNames)
        {
            string testName = _testProject.TestProjectAlias!;
            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                string line = lines[lineIndex];
                const string AssemblyTag = ".assembly";
                int assemblyIndex = line.IndexOf(AssemblyTag);
                if (assemblyIndex >= 0 && line.IndexOf("extern") < 0)
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
            }

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                if (lines[lineIndex].IndexOf(".module") >= 0)
                {
                    lines.RemoveAt(lineIndex);
                    break;
                }
            }
        }

        if (_testProject.DeduplicatedClassName != null)
        {
            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                lines[lineIndex] = ReplaceIdent(lines[lineIndex], _testProject.TestClassName, _testProject.DeduplicatedClassName);
            }
            rewritten = true;
        }

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
            int outputTypeIndex = line.IndexOf(outputTypeTag);
            if (outputTypeIndex >= 0)
            {
                lines.RemoveAt(lineIndex--);
                rewritten = true;
                continue;
            }
            const string testKindTag = "<CLRTestKind>BuildAndRun</CLRTestKind>";
            int testKindIndex = line.IndexOf(testKindTag);
            if (testKindIndex >= 0)
            {
                lines[lineIndex] = line.Substring(0, testKindIndex) + "<CLRTestKind>BuildOnly</CLRTestKind>";
                rewritten = true;
                continue;
            }
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
