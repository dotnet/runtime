// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;

public class ILRewriter
{
    static string[] s_xUnitLines =
    {
        ".assembly extern xunit.core {}",
    };

    static string[] s_factLines =
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

    public ILRewriter(TestProject testProject, HashSet<string> classNameDuplicates, bool deduplicateClassNames, HashSet<string> rewrittenFiles, bool addILFactAttributes)
    {
        _testProject = testProject;
        _classNameDuplicates = classNameDuplicates;
        _deduplicateClassNames = deduplicateClassNames;
        _rewrittenFiles = rewrittenFiles;
        _addILFactAttributes = addILFactAttributes;
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
                int lineInBody = lineIndex;
                while (!lines[lineInBody].Contains('{'))
                {
                    if (++lineInBody >= lines.Count)
                    {
                        Console.Error.WriteLine("Opening brace for main method not found in file: {0}", ilSource);
                        break;
                    }
                }

                if (_addILFactAttributes && !_testProject.HasFactAttribute && Path.GetExtension(_testProject.TestClassSourceFile).ToLower() == ".il")
                {
                    string firstMainBodyLine = lines[lineInBody + 1];
                    int indent = 0;
                    while (indent < firstMainBodyLine.Length && firstMainBodyLine[indent] <= ' ')
                    {
                        indent++;
                    }
                    string indentString = firstMainBodyLine.Substring(0, indent);
                    string[] indentedFactLines = new string[s_factLines.Length];
                    for (int i = 0; i < s_factLines.Length; i++)
                    {
                        indentedFactLines[i] = indentString + s_factLines[i];
                    }
                    lines.InsertRange(lineInBody + 1, indentedFactLines);
                    rewritten = true;
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

        /*
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
        */

        if (!_deduplicateClassNames)
        {
            bool hasXunitReference = false;
            string testName = _testProject.TestProjectAlias!;
            bool isILTest = Path.GetExtension(_testProject.TestClassSourceFile).ToLower() == ".il";
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
        }

        /*
        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            {
                if (lines[lineIndex].IndexOf(".module") >= 0)
                {
                    lines.RemoveAt(lineIndex);
                    break;
                }
            }
        }
        */

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
        /*
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
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
        */
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
