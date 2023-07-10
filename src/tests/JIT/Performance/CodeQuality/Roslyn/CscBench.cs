// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

public static class CscBench
{

#if DEBUG
    public const int CompileIterations = 1;
    public const int DataflowIterations = 1;
#else
    public const int CompileIterations = 1500;
    public const int DataflowIterations = 10000;
#endif

    public static string MscorlibPath;

    static bool FindMscorlib()
    {
        string CoreRoot = System.Environment.GetEnvironmentVariable("CORE_ROOT");
        if (CoreRoot == null) { return false; }
        // Some CoreCLR packages have System.Private.CoreLib.ni.dll only
        string nicorlib = Path.Combine(CoreRoot, "System.Private.CoreLib.ni.dll");
        if(File.Exists(nicorlib))
        {
            MscorlibPath = nicorlib;
            return true;
        }
        MscorlibPath = Path.Combine(CoreRoot, "System.Private.CoreLib.dll");
        return File.Exists(MscorlibPath);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool CompileBench()
    {
        var expression = "6 * 7";
        var text = @"public class Calculator { public static object Evaluate() { return $; } }".Replace("$", expression);
        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var compilation = CSharpCompilation.Create(
            "calc.dll",
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            syntaxTrees: new[] { tree },
            references: new[] { MetadataReference.CreateFromFile(MscorlibPath) });

        bool result = true;
        for (int i = 0; i < CompileIterations; i++)
        {
            using (var stream = new MemoryStream())
            {
                var emitResult = compilation.Emit(stream);
                result &= emitResult.Success;
            }
        }

        return result;
    }

    public static TextSpan GetSpanBetweenMarkers(SyntaxTree tree)
    {
        SyntaxTrivia startComment = tree
            .GetRoot()
            .DescendantTrivia()
            .First(syntaxTrivia => syntaxTrivia.ToString().Contains("start"));
        SyntaxTrivia endComment = tree
            .GetRoot()
            .DescendantTrivia()
            .First(syntaxTrivia => syntaxTrivia.ToString().Contains("end"));
        TextSpan textSpan = TextSpan.FromBounds(
            startComment.FullSpan.End,
            endComment.FullSpan.Start);
        return textSpan;
    }

    public static void GetStatementsBetweenMarkers(SyntaxTree tree, out StatementSyntax firstStatement, out StatementSyntax lastStatement)
    {
        TextSpan span = GetSpanBetweenMarkers(tree);
        var statementsInside = tree
            .GetRoot()
            .DescendantNodes(span)
            .OfType<StatementSyntax>()
            .Where(s => span.Contains(s.Span));
        firstStatement = statementsInside.First();
        var first = firstStatement;
        lastStatement = statementsInside
            .Where(s => s.Parent == first.Parent)
            .Last();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool DataflowBench()
    {
        var text = @"
public class C {
    public void F(int x)
    {
        int a;
/*start*/
        int b;
        int x, y = 1;
        { var z = ""a""; }
/*end*/
        int c;
    }
}";
        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var compilation = CSharpCompilation.Create(
            "calc.dll",
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            syntaxTrees: new[] { tree },
            references: new[] { MetadataReference.CreateFromFile(MscorlibPath) });
        var semanticModel = compilation.GetSemanticModel(tree);

        bool result = true;
        for (int i = 0; i < DataflowIterations; i++)
        {
            StatementSyntax firstStatement, lastStatement;
            GetStatementsBetweenMarkers(tree, out firstStatement, out lastStatement);
            DataFlowAnalysis regionDataFlowAnalysis = semanticModel.AnalyzeDataFlow(firstStatement, lastStatement);
            string declaredVars = string.Join(",", regionDataFlowAnalysis
                .VariablesDeclared
                .Select(symbol => symbol.Name));

            result &= "b,x,y,z".Equals(declaredVars);
        }

        return result;
    }

    static bool Bench()
    {
        bool result = true;
        result &= CompileBench();
        result &= DataflowBench();
        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool result = true;
        if (!FindMscorlib())
        {
            Console.WriteLine("This test requires CORE_ROOT to be set");
            result = false;
        }
        else {
            result = Bench();
        }
        return result ? 100 : -1;
    }
}
