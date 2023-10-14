using System;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Steps;

public class CustomMarkHandlerSaveAssembly : IMarkHandler
{
    public void Initialize (LinkContext context, MarkContext markContext)
    {
        markContext.RegisterMarkAssemblyAction (assembly => {
            if (assembly.Name.Name == "saved")
                context.Annotations.SetAction (assembly, AssemblyAction.Save);
        });
    }
}
