using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UnityEmbedHost.Generator;

[Generator]
public class HostStructGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {

    }

    public void Execute(GeneratorExecutionContext context)
    {
        IMethodSymbol[] callbackMethods = FindUnmanagedCallerMethods(context)
            .OrderBy(m => m.Name)
            .ToArray();

        WriteHostStruct(context, callbackMethods);
        WriteCoreCLRHost(context, callbackMethods);
    }

    static void WriteHostStruct(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        string sourceBegin = @"
// Auto-generated code

namespace Unity.CoreCLRHelpers;

unsafe partial struct HostStruct
{";

        var sb = new StringBuilder();

        sb.Append(sourceBegin);
        sb.AppendLine();


        foreach (var methodSymbol in callbackMethods)
        {
            sb.AppendLine($"       public delegate* unmanaged<{FormatMethodParameters(methodSymbol)}> {methodSymbol.Name};");
        }

        string sourceEnd = @"}";

        sb.Append(sourceEnd);
        context.AddSource($"GeneratedHostStruct.gen.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    static void WriteCoreCLRHost(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        string sourceBegin = @"
// Auto-generated code

namespace Unity.CoreCLRHelpers;

static unsafe partial class CoreCLRHost
{
    static partial void InitHostStruct(HostStruct* functionStruct)
    {";

        var sb = new StringBuilder();

        sb.Append(sourceBegin);
        sb.AppendLine();


        foreach (var methodSymbol in callbackMethods)
        {
            sb.AppendLine($"       functionStruct->{methodSymbol.Name} = &{methodSymbol.Name}_native;");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        foreach (var methodSymbol in callbackMethods)
        {
            sb.AppendLine("    [System.Runtime.InteropServices.UnmanagedCallersOnly]");
            string signature = FormatMethodParametersForMethodSignature(methodSymbol);
            sb.AppendLine($"    static {methodSymbol.ReturnType} {methodSymbol.Name}_native({signature}) => {methodSymbol.Name}({FormatMethodParametersNames(methodSymbol)});");
            sb.AppendLine();
        }

        sb.Append("}");
        context.AddSource($"GeneratedCoreCLRHost.gen.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }
    static string FormatMethodParametersForMethodSignature(IMethodSymbol methodSymbol)
    {
        var sb = new StringBuilder();
        for (int index = 0; index < methodSymbol.Parameters.Length; index++)
        {
            IParameterSymbol? param = methodSymbol.Parameters[index];
            sb.Append(param.Type);
            sb.Append(' ');
            sb.Append(param.Name);

            if (index < methodSymbol.Parameters.Length -1)
                sb.Append(", ");
        }

        return sb.ToString();
    }

    static string FormatMethodParametersNames(IMethodSymbol methodSymbol)
    {
        var sb = new StringBuilder();
        for (int index = 0; index < methodSymbol.Parameters.Length; index++)
        {
            IParameterSymbol? param = methodSymbol.Parameters[index];
            sb.Append(param.Name);

            if (index < methodSymbol.Parameters.Length -1)
                sb.Append(", ");
        }

        return sb.ToString();
    }


    static string FormatMethodParameters(IMethodSymbol methodSymbol)
    {
        var sb = new StringBuilder();
        foreach (var param in methodSymbol.Parameters)
        {
            sb.Append(param.Type);
            sb.Append(", ");
        }

        sb.Append(methodSymbol.ReturnType);

        return sb.ToString();
    }

    private static INamedTypeSymbol FindTypeByName(INamespaceSymbol nsSymbol, string name)
    {
        foreach (var member in nsSymbol.GetMembers())
        {
            if (member == null)
                continue;

            if (member.IsNamespace)
            {
                return FindTypeByName((INamespaceSymbol)member, name);
            }
            else
            {
                var typeSymbol = (INamedTypeSymbol)member;
                if (typeSymbol.Name == name)
                {
                    return typeSymbol;
                }
            }
        }

        throw new ArgumentException($"Could not locate a type named {name}");
    }

    private static IEnumerable<IMethodSymbol> FindUnmanagedCallerMethods(INamespaceSymbol nsSymbol)
    {
        var typeSymbol = FindTypeByName(nsSymbol, "CoreCLRHost");
        foreach (var method in GetCallbackMethods(typeSymbol))
            yield return method;
    }

    static IEnumerable<IMethodSymbol> GetCallbackMethods(INamedTypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IMethodSymbol methodSymbol && methodSymbol.DeclaredAccessibility == Accessibility.Public)
            {
                yield return methodSymbol;
            }
        }
    }

    private static IEnumerable<IMethodSymbol> FindUnmanagedCallerMethods(GeneratorExecutionContext context)
    {
        return FindUnmanagedCallerMethods(context.Compilation.GlobalNamespace);
    }
}
