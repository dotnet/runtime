using Microsoft.CodeAnalysis;

namespace UnityEmbedHost.Generator;

[Generator]
public class CallbacksGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {

    }

    public void Execute(GeneratorExecutionContext context)
    {
        IMethodSymbol[] callbackMethods = MethodCollection.FindUnmanagedCallerMethods(context)
            .OrderBy(m => m.Name)
            .ToArray();

        ManagedGeneration.Run(context, callbackMethods);
        NativeGeneration.Run(context, callbackMethods);
    }
}
