// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SuperFileCheck
{
    internal readonly record struct MethodDeclarationInfo(MethodDeclarationSyntax Syntax, string FullyQualifiedName);

    internal readonly record struct FileCheckResult(int ExitCode, string StandardOutput, string StandardError);

    internal class SuperFileCheckException : Exception
    {
        public SuperFileCheckException(string message): base(message) { }
    }

    internal class Program
    {
        const string CommandLineArgumentCSharp = "--csharp";
        const string CommandLineArgumentCSharpListMethodNames = "--csharp-list-method-names";
        const string CommandLineCheckPrefixes = "--check-prefixes";
        const string CommandLineCheckPrefixesEqual = "--check-prefixes=";
        const string CommandLineInputFile = "--input-file";
        const string SyntaxDirectiveFullLine = "-FULL-LINE:";
        const string SyntaxDirectiveFullLineNext = "-FULL-LINE-NEXT:";

        static string FileCheckPath;

        static Program()
        {
            // Determine the location of LLVM FileCheck.
            // We first look through the "runtimes" directory relative to
            // the location of SuperFileCheck to find FileCheck.
            // If it cannot find it, then we assume FileCheck
            // is in the same directory as SuperFileCheck.
            var superFileCheckPath = typeof(Program).Assembly.Location;
            if (String.IsNullOrEmpty(superFileCheckPath))
            {
                throw new SuperFileCheckException("Invalid SuperFileCheck path.");
            }
            var superFileCheckDir = Path.GetDirectoryName(superFileCheckPath);
            if (superFileCheckDir != null)
            {
                var fileCheckPath =
                    Directory.EnumerateFiles(Path.Combine(superFileCheckDir, "runtimes/"), "FileCheck*", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (fileCheckPath != null)
                {
                    FileCheckPath = fileCheckPath;
                }
                else
                {
                    FileCheckPath = Path.Combine(superFileCheckDir, "FileCheck");
                }
            }
            else
            {
                FileCheckPath = "FileCheck";
            }
        }

        /// <summary>
        /// Checks if the given string contains LLVM "<prefix>" directives, such as "<prefix>:", "<prefix>-LABEL:", etc..
        /// </summary>
        static bool ContainsCheckPrefixes(string str, string[] checkPrefixes)
        {
            // LABEL, NOT, SAME, etc. are from LLVM FileCheck https://llvm.org/docs/CommandGuide/FileCheck.html

            // FULL-LINE and FULL-LINE-NEXT are not part of LLVM FileCheck - they are new syntax directives for SuperFileCheck to be able to
            // match a single full-line, similar to that of LLVM FileCheck's --match-full-lines option.

            var pattern = $"({String.Join('|', checkPrefixes)})+?({{LITERAL}})?(:|-LABEL:|-NEXT:|-NOT:|-SAME:|-EMPTY:|-COUNT:|-DAG:|{SyntaxDirectiveFullLine}|{SyntaxDirectiveFullLineNext})";
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            return regex.Count(str) > 0;
        }

        /// <summary>
        /// Runs LLVM's FileCheck executable.
        /// Will always redirect standard error and output.
        /// </summary>
        static async Task<FileCheckResult> RunLLVMFileCheckAsync(string[] args)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = FileCheckPath;
            startInfo.Arguments = String.Join(' ', args);
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            try
            {
                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                using (var proc = Process.Start(startInfo))
                {
                    if (proc == null)
                    {
                        return new FileCheckResult(1, String.Empty, String.Empty);
                    }

                    var stdOut = new StringBuilder();
                    var stdErr = new StringBuilder();

                    proc.OutputDataReceived += (_, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            stdOut.AppendLine(e.Data);
                        }
                    };

                    proc.ErrorDataReceived += (_, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            stdErr.AppendLine(e.Data);
                        }
                    };

                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    await proc.WaitForExitAsync();
                    outputWaitHandle.WaitOne();
                    errorWaitHandle.WaitOne();

                    var exitCode = proc.ExitCode;
                    return new FileCheckResult(exitCode, stdOut.ToString(), stdErr.ToString());
                }
            }
            catch (Exception ex)
            {
                return new FileCheckResult(1, String.Empty, ex.Message);
            }
        }

        /// <summary>
        /// Get the method name from the method declaration.
        /// </summary>
        static string GetMethodName(MethodDeclarationSyntax methodDecl)
        {
            var methodName = methodDecl.Identifier.ValueText;

            var typeArity = methodDecl.TypeParameterList?.ChildNodes().Count();
            if (typeArity > 0)
            {
                methodName = $"{methodName}[*]";
            }

            return $"{methodName}(*)";
        }

        /// <summary>
        /// Get the enclosing type declaration syntax of the given node.
        /// Errors if it cannot find one.
        /// </summary>
        static TypeDeclarationSyntax GetEnclosingTypeDeclaration(SyntaxNode node)
        {
            var typeDecl = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (typeDecl == null)
            {
                throw new SuperFileCheckException($"Unable to find enclosing type declaration on: {node.Span}");
            }
            return typeDecl;
        }

        /// <summary>
        /// Get all the acestoral enclosing type declaration syntaxes of the given node.
        /// </summary>
        static TypeDeclarationSyntax[] GetEnclosingTypeDeclarations(SyntaxNode node)
        {
            return node.Ancestors().OfType<TypeDeclarationSyntax>().ToArray();
        }

        /// <summary>
        /// Try to get an enclosing type name from the given syntax node.
        /// </summary>
        static string GetTypeName(TypeDeclarationSyntax typeDecl)
        {
            var typeName = typeDecl.Identifier.ValueText;

            var typeArity = typeDecl.TypeParameterList?.ChildNodes().Count();
            if (typeArity > 0)
            {
                typeName = $"{typeName}`{typeArity}[*]";
            }

            return typeName;
        }

        /// <summary>
        /// Get the method's fully qualified enclosing namespace and type name.
        /// </summary>
        static string GetFullyQualifiedEnclosingTypeName(MethodDeclarationSyntax methodDecl)
        {
            var qualifiedTypeName = String.Empty;

            var typeDecl = GetEnclosingTypeDeclaration(methodDecl);
            qualifiedTypeName = GetTypeName(typeDecl);

            var typeDecls = GetEnclosingTypeDeclarations(typeDecl);
            for (var i = 0; i < typeDecls.Length; i++)
            {
                typeDecl = typeDecls[i];
                qualifiedTypeName = $"{GetTypeName(typeDecl)}+{qualifiedTypeName}";
            }

            var namespaceDecl = typeDecl.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            if (namespaceDecl != null)
            {
                var identifiers =
                    namespaceDecl.Name.DescendantTokens().Where(x => x.IsKind(SyntaxKind.IdentifierToken)).Select(x => x.ValueText);
                return $"{String.Join(".", identifiers)}.{qualifiedTypeName}";
            }

            return qualifiedTypeName;
        }

        /// <summary>
        /// Get all the descendant single line comment trivia items.
        /// </summary>
        static IEnumerable<SyntaxTrivia> GetDescendantSingleLineCommentTrivia(SyntaxNode node)
        {
            return
                node
                .DescendantTrivia()
                .Where(x => x.IsKind(SyntaxKind.SingleLineCommentTrivia));
        }

        /// <summary>
        /// Gather all syntactical method declarations whose body contains
        /// FileCheck syntax.
        /// </summary>
        static MethodDeclarationInfo[] FindMethodsByFile(string filePath, string[] checkPrefixes)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(File.ReadAllText(filePath)));
            var root = syntaxTree.GetRoot();

            var trivia =
                GetDescendantSingleLineCommentTrivia(root)
                .Where(x =>
                {
                    if (x.Token.Parent == null)
                    {
                        return true;
                    }

                    // A comment before the method declaration is considered a child of the method
                    // declaration.  In this example:
                    //
                    // // trivia1
                    // public void M()
                    // {
                    //     // trivia2
                    // }
                    //
                    // Both // trivia1 and // trivia2 are descendants of MethodDeclarationSyntax.
                    //
                    // We are only allowing checks to occur in 'trivia2'.  The 'Contains' check is
                    // used to find 'trivia1'.
                    return !x.Token.Parent.Ancestors().Any(p => p.IsKind(SyntaxKind.MethodDeclaration) && p.Span.Contains(x.Span));
                })
                .Where(x => ContainsCheckPrefixes(x.ToString(), checkPrefixes))
                .ToArray();

            if (trivia.Length > 0)
            {
                throw new SuperFileCheckException("FileCheck syntax not allowed outside of a method.");
            }

            return
                root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(x => ContainsCheckPrefixes(x.ToString(), checkPrefixes))
                .Select(x => new MethodDeclarationInfo(x, $"{GetFullyQualifiedEnclosingTypeName(x)}:{GetMethodName(x)}"))
                .ToArray();
        }

        /// <summary>
        /// Helper to expand FileCheck syntax.
        /// </summary>
        static string? TryTransformDirective(string lineStr, string[] checkPrefixes, string syntaxDirective, string transformSuffix)
        {
            var index = lineStr.IndexOf(syntaxDirective);
            if (index == -1)
            {
                return null;
            }

            var prefix = lineStr.Substring(0, index);

            // Do not transform if the prefix is not part of --check-prefixes.
            if (!checkPrefixes.Any(x => prefix.EndsWith(x)))
            {
                return null;
            }

            return lineStr.Substring(0, index) + $"{transformSuffix}: {{{{^ *}}}}" + lineStr.Substring(index + syntaxDirective.Length) + "{{$}}";
        }

        /// <summary>
        /// Will try to transform a line containing custom SuperFileCheck syntax, e.g. "CHECK-FULL-LINE:"
        /// to the appropriate FileCheck syntax.
        /// </summary>
        static string TransformLine(TextLine line, string[] checkPrefixes)
        {
            var text = line.Text;
            if (text == null)
            {
                throw new InvalidOperationException("SourceText is null.");
            }

            var lineStr = text.ToString(line.Span);

            var result = TryTransformDirective(lineStr, checkPrefixes, SyntaxDirectiveFullLine, String.Empty);
            if (result != null)
            {
                return result;
            }

            result = TryTransformDirective(lineStr, checkPrefixes, SyntaxDirectiveFullLineNext, "-NEXT");

            return result ?? lineStr;
        }

        /// <summary>
        /// Will try to transform a method containing custom SuperFileCheck syntax, e.g. "CHECK-FULL-LINE:"
        /// to the appropriate FileCheck syntax.
        /// </summary>
        static string TransformMethod(MethodDeclarationSyntax methodDecl, string[] checkPrefixes)
        {
            return String.Join(Environment.NewLine, methodDecl.GetText().Lines.Select(x => TransformLine(x, checkPrefixes)));
        }

        /// <summary>
        /// Gets the starting line number of the method declaration.
        /// </summary>
        static int GetMethodStartingLineNumber(MethodDeclarationSyntax methodDecl)
        {
            var leadingTrivia = methodDecl.GetLeadingTrivia();
            if (leadingTrivia.Count == 0)
            {
                return methodDecl.GetLocation().GetLineSpan().StartLinePosition.Line;
            }
            else
            {
                return leadingTrivia[0].GetLocation().GetLineSpan().StartLinePosition.Line;
            }
        }

        /// <summary>
        /// Returns only the method declaration text along with any SuperFileCheck transformations.
        /// </summary>
        static string PreProcessMethod(MethodDeclarationInfo methodDeclInfo, string[] checkPrefixes)
        {
            var methodDecl = methodDeclInfo.Syntax;
            var methodName = methodDeclInfo.FullyQualifiedName.Replace("*", "{{.*}}"); // Change wild-card to FileCheck wild-card syntax.

            // Create anchors from the first prefix.
            var startAnchorText = $"// {checkPrefixes[0]}-LABEL: for method {methodName}";
            var endAnchorText = $"// {checkPrefixes[0]}: for method {methodName}";

            // Create temp source file based on the source text of the method.
            // Newlines are added to pad the text so FileCheck's error messages will correspond
            // to the correct line and column of the original source file.
            // This is not perfect but will work for most cases.
            var lineNumber = GetMethodStartingLineNumber(methodDecl);
            var tmpSrc = new StringBuilder();
            for (var i = 1; i < lineNumber; i++)
            {
                tmpSrc.AppendLine(String.Empty);
            }
            tmpSrc.AppendLine(startAnchorText);
            tmpSrc.AppendLine(TransformMethod(methodDecl, checkPrefixes));
            tmpSrc.AppendLine(endAnchorText);

            return tmpSrc.ToString();
        }

        /// <summary>
        /// Runs SuperFileCheck logic.
        /// </summary>

        static async Task<FileCheckResult> RunSuperFileCheckAsync(MethodDeclarationInfo methodDeclInfo, string[] args, string[] checkPrefixes, string tmpFilePath)
        {
            File.WriteAllText(tmpFilePath, PreProcessMethod(methodDeclInfo, checkPrefixes));

            try
            {
                args[0] = tmpFilePath;
                return await RunLLVMFileCheckAsync(args);
            }
            finally
            {
                try { File.Delete(tmpFilePath); } catch { }
            }
        }

        /// <summary>
        /// Checks if the argument is --csharp.
        /// </summary>
        static bool IsArgumentCSharp(string arg)
        {
            return arg.Equals(CommandLineArgumentCSharp);
        }

        /// <summary>
        /// Checks if the argument is --csharp-list-method-names.
        /// </summary>
        static bool IsArgumentCSharpListMethodNames(string arg)
        {
            return arg.Equals(CommandLineArgumentCSharpListMethodNames);
        }

        /// <summary>
        /// Checks if the argument contains -h.
        /// </summary>
        static bool ArgumentsContainHelp(string[] args)
        {
            return args.Any(x => x.Contains("-h"));
        }

        /// <summary>
        /// From the given arguments, find the first --check-prefixes argument and parse its value
        /// in the form of an array.
        /// </summary>
        static string[] ParseCheckPrefixes(string[] args)
        {
            var checkPrefixesArg = args.FirstOrDefault(x => x.StartsWith(CommandLineCheckPrefixesEqual));
            if (checkPrefixesArg == null)
            {
                return new string[] { };
            }

            return
                checkPrefixesArg
                .Replace(CommandLineCheckPrefixesEqual, "")
                .Split(",")
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .ToArray();
        }

        /// <summary>
        /// Will always return one or more prefixes.
        /// </summary>
        static string[] DetermineCheckPrefixes(string[] args)
        {
            var checkPrefixes = ParseCheckPrefixes(args);
            if (checkPrefixes.Length == 0)
            {
                // FileCheck's default.
                return new string[] { "CHECK" };
            }

            return checkPrefixes;
        }

        /// <summary>
        /// Prints error expecting a CSharp file.
        /// </summary>
        static void PrintErrorExpectedCSharpFile()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Expected C# file.");
            Console.ResetColor();
        }

        /// <summary>
        /// Prints error indicating a duplicate method name was found.
        /// </summary>
        static void PrintErrorDuplicateMethodName(string methodName)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Duplicate method name found: {methodName}");
            Console.ResetColor();
        }

        /// <summary>
        /// Prints error indicating the method was not marked with attribute 'MethodImpl(MethodImplOptions.NoInlining)'.
        /// </summary>
        static void PrintErrorMethodNoInlining(string methodName)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"'{methodName}' is not marked with attribute 'MethodImpl(MethodImplOptions.NoInlining)'.");
            Console.ResetColor();
        }

        /// <summary>
        /// Prints error indicating that no methods were found to have any FileCheck syntax
        /// of the given --check-prefixes.
        /// </summary>
        static void PrintErrorNoMethodsFound(string[] checkPrefixes)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("No methods were found. Check if any method bodies are using one or more of the following FileCheck prefixes:");
            foreach (var prefix in checkPrefixes)
            {
                Console.Error.WriteLine($"    {prefix}");
            }
            Console.ResetColor();
        }

        /// <summary>
        /// Prints error indicating that a --input-file was not found.
        /// </summary>
        static void PrintErrorNoInputFileFound()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"{CommandLineInputFile} is required.");
            Console.ResetColor();
        }

        /// <summary>
        /// Prints command line help.
        /// </summary>
        static void PrintHelp()
        {
            Console.Write(Environment.NewLine);
            Console.WriteLine("USAGE: SuperFileCheck [options] <check-file>");
            Console.WriteLine("USAGE: SuperFileCheck <super-option> <check-file> [options]");
            Console.Write(Environment.NewLine);
            Console.WriteLine("SUPER OPTIONS:");
            Console.Write(Environment.NewLine);
            Console.WriteLine($"  --csharp                       - A {CommandLineInputFile} is required.");
            Console.WriteLine($"                                   <check-file> must be a C# source file.");
            Console.WriteLine($"                                   Methods must not have duplicate names.");
            Console.WriteLine($"                                   Methods must be marked as not inlining.");
            Console.WriteLine($"                                   One or more methods are required.");
            Console.WriteLine($"                                   Prefixes are determined by {CommandLineCheckPrefixes}.");
            Console.WriteLine($"  --csharp-list-method-names     - Print a space-delimited list of method names to be");
            Console.WriteLine($"                                   supplied to environment variable DOTNET_JitDisasm.");
            Console.WriteLine($"                                   <check-file> must be a C# source file.");
            Console.WriteLine($"                                   Methods must not have duplicate names.");
            Console.WriteLine($"                                   Methods must be marked as not inlining.");
            Console.WriteLine($"                                   Prints nothing if no methods are found.");
            Console.WriteLine($"                                   Prefixes are determined by {CommandLineCheckPrefixes}.");
        }

        /// <summary>
        /// Try to find the first duplicate method name of the given method declarations.
        /// </summary>
        static string? TryFindDuplicateMethodName(MethodDeclarationInfo[] methodDeclInfos)
        {
            var set = new HashSet<string>();

            var duplicateMethodDeclInfo =
                methodDeclInfos.FirstOrDefault(x => !set.Add(x.FullyQualifiedName));

            return duplicateMethodDeclInfo.FullyQualifiedName;
        }

        /// <summary>
        /// Is the method marked with MethodImpl(MethodImplOptions.NoInlining)?
        /// </summary>
        static bool MethodHasNoInlining(MethodDeclarationSyntax methodDecl)
        {
            return methodDecl.AttributeLists.ToString().Contains("MethodImplOptions.NoInlining");
        }

        /// <summary>
        /// Will print an error if any duplicate method names are found.
        /// </summary>
        static bool CheckDuplicateMethodNames(MethodDeclarationInfo[] methodDeclInfos)
        {
            var duplicateMethodName = TryFindDuplicateMethodName(methodDeclInfos);
            if (duplicateMethodName != null)
            {
                PrintErrorDuplicateMethodName(duplicateMethodName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Will print an error if the methods containing FileCheck syntax
        /// is not marked with the attribute 'MethodImpl(MethodImplOptions.NoInlining)'.
        /// </summary>
        static bool CheckMethodsHaveNoInlining(MethodDeclarationInfo[] methodDeclInfos)
        {
            return
                methodDeclInfos
                .All(methodDeclInfo =>
                {
                    if (!MethodHasNoInlining(methodDeclInfo.Syntax))
                    {
                        PrintErrorMethodNoInlining(methodDeclInfo.FullyQualifiedName);
                        return false;
                    }

                    return true;
                });
        }

        /// <summary>
        /// The goal of SuperFileCheck is to make writing LLVM FileCheck tests against the
        /// NET Core Runtime easier in C#.
        /// </summary>
        static async Task<int> Main(string[] args)
        {
            if (args.Length >= 1)
            {
                if (IsArgumentCSharpListMethodNames(args[0]))
                {
                    if (args.Length == 1)
                    {
                        PrintErrorExpectedCSharpFile();
                        return 1;
                    }

                    var checkPrefixes = DetermineCheckPrefixes(args);
                    try
                    {
                        var methodDeclInfos = FindMethodsByFile(args[1], checkPrefixes);

                        if (methodDeclInfos.Length == 0)
                        {
                            return 0;
                        }

                        if (!CheckDuplicateMethodNames(methodDeclInfos))
                        {
                            return 1;
                        }

                        Console.Write(String.Join(' ', methodDeclInfos.Select(x => x.FullyQualifiedName)));
                        return 0;
                    }
                    catch(Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex.Message);
                        Console.ResetColor();
                        return 1;
                    }
                }

                if (IsArgumentCSharp(args[0]))
                {
                    if (args.Length == 1)
                    {
                        PrintErrorExpectedCSharpFile();
                        return 1;
                    }

                    var checkFilePath = args[1];
                    var checkFileNameNoExt = Path.GetFileNameWithoutExtension(checkFilePath);

                    var hasInputFile = args.Any(x => x.Equals(CommandLineInputFile));
                    if (!hasInputFile)
                    {
                        PrintErrorNoInputFileFound();
                        return 1;
                    }

                    var checkPrefixes = DetermineCheckPrefixes(args);
                    try
                    {
                        var methodDeclInfos = FindMethodsByFile(checkFilePath, checkPrefixes);

                        if (!CheckDuplicateMethodNames(methodDeclInfos))
                        {
                            return 1;
                        }

                        if (!CheckMethodsHaveNoInlining(methodDeclInfos))
                        {
                            return 1;
                        }

                        if (methodDeclInfos.Length > 0)
                        {
                            var didSucceed = true;

                            var tasks = new Task<FileCheckResult>[methodDeclInfos.Length];

                            // Remove the first 'csharp' argument so we can pass the rest of the args
                            // to LLVM FileCheck.
                            var argsToCopy = args.AsSpan(1).ToArray();

                            for (int i = 0; i < methodDeclInfos.Length; i++)
                            {
                                var index = i;
                                var tmpFileName = $"__tmp{index}_{checkFileNameNoExt}.cs";
                                var tmpDirName = Path.GetDirectoryName(checkFilePath);
                                string tmpFilePath;
                                if (String.IsNullOrWhiteSpace(tmpDirName))
                                {
                                    tmpFilePath = tmpFileName;
                                }
                                else
                                {
                                    tmpFilePath = Path.Combine(tmpDirName, tmpFileName);
                                }
                                tasks[i] = Task.Run(() => RunSuperFileCheckAsync(methodDeclInfos[index], argsToCopy.ToArray(), checkPrefixes, tmpFilePath));
                            }

                            await Task.WhenAll(tasks);

                            foreach (var x in tasks)
                            {
                                if (x.Result.ExitCode != 0)
                                {
                                    didSucceed = false;
                                }
                                Console.Write(x.Result.StandardOutput);
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Error.Write(x.Result.StandardError);
                                Console.ResetColor();
                            }

                            return didSucceed ? 0 : 1;
                        }
                        else
                        {
                            PrintErrorNoMethodsFound(checkPrefixes);
                            return 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex.Message);
                        Console.ResetColor();
                        return 1;
                    }
                }
            }

            var result = await RunLLVMFileCheckAsync(args);
            Console.Write(result.StandardOutput);

            if (ArgumentsContainHelp(args))
            {
                PrintHelp();
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.Write(result.StandardError);
            Console.ResetColor();
            return result.ExitCode;
        }
    }
}
