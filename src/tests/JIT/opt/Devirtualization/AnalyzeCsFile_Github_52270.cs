using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

public static class Program
{
    public static async Task<int> Main()
    {
        // Make sure it doesn't assert, see https://github.com/dotnet/runtime/issues/52270
        for (int i = 0; i < 100; i++)
        {
            await Analyze();
        }
        return 100;
    }
    
    private static async Task Analyze()
    {
        var embeddedData = typeof(Program).Assembly.GetManifestResourceStream(
            "AnalyzeCsFile_Github_52270.AnalyzeCsFile_Github_52270_cs.test");
        var streamReader = new StreamReader(embeddedData);
        var text = streamReader.ReadToEnd();

        using var workspace = new AdhocWorkspace();
        Project proj = workspace.AddProject("project", LanguageNames.CSharp)
            .WithMetadataReferences(new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        Document doc = proj.AddDocument("doc", SourceText.From(text));
        Compilation compilation = await doc.Project.GetCompilationAsync();
        SyntaxNode root = await doc.GetSyntaxRootAsync();
        var rewriter = new CmpAgainstNullLiteral { Model = compilation.GetSemanticModel(root.SyntaxTree) };
        SyntaxNode node = rewriter.Visit(root);
    }
}

internal sealed class CmpAgainstNullLiteral : CSharpSyntaxRewriter
{
    internal SemanticModel Model { get; set; }

    public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        try
        {
            if (node.Kind() == SyntaxKind.EqualsExpression || node.Kind() == SyntaxKind.NotEqualsExpression)
            {
                if (node.Right.Kind() == SyntaxKind.NullLiteralExpression)
                {
                    Model.GetSymbolInfo(node.Left).Symbol.ToDisplayString();
                }
            }
            return base.VisitBinaryExpression(node);
        }
        catch
        {
            return base.VisitBinaryExpression(node);
        }
    }
}