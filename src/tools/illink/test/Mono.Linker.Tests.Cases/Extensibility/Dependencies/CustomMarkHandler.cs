using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Steps;

public class CustomMarkHandler : IMarkHandler
{
	LinkContext _context;

	public void Initialize (LinkContext context, MarkContext markContext)
	{
		_context = context;
		markContext.RegisterMarkAssemblyAction (assembly => DiscoverTypesInAssembly (assembly));
		markContext.RegisterMarkTypeAction (type => DiscoverMethodsInType (type));
		markContext.RegisterMarkMethodAction (method => DiscoverMethodsOnDeclaringType (method));
	}

	void MarkTypeFoo (TypeDefinition type)
	{
		if (type.Name == "DiscoveredTypeForAssembly")
			_context.Annotations.Mark (type);

		if (!type.HasNestedTypes)
			return;

		foreach (var nested in type.NestedTypes)
			MarkTypeFoo (nested);
	}

	void DiscoverTypesInAssembly (AssemblyDefinition assembly)
	{
		foreach (var type in assembly.MainModule.Types)
			MarkTypeFoo (type);
	}

	void DiscoverMethodsInType (TypeDefinition type)
	{
		foreach (var method in type.Methods) {
			if (method.Name == $"DiscoveredMethodForType_{type.Name}")
				_context.Annotations.Mark (method);
		}
	}

	void DiscoverMethodsOnDeclaringType (MethodDefinition method)
	{
		foreach (var otherMethod in method.DeclaringType.Methods)
			if (otherMethod.Name == $"DiscoveredMethodForMethod_{method.Name}")
				_context.Annotations.Mark (otherMethod);
	}
}