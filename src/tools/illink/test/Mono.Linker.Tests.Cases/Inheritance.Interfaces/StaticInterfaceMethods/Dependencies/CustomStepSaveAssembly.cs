using System;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Steps;

public class CustomStepSaveAssembly : BaseStep
{
    protected override void ProcessAssembly (AssemblyDefinition assembly)
    {
        if (assembly.Name.Name == "test")
            Context.Annotations.SetAction (assembly, AssemblyAction.Save);
    }
}
