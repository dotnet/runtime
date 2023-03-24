using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Steps;

public class CustomWarning : IMarkHandler
{
	LinkContext _context;

	public void Initialize (LinkContext context, MarkContext markContext)
	{
		_context = context;
		markContext.RegisterMarkTypeAction (type => WarnOnKnownType (type));
	}

	void WarnOnKnownType (TypeDefinition type )
	{
		if (type.Name == "KnownTypeThatShouldWarn")
			_context.LogMessage (MessageContainer.CreateCustomWarningMessage (_context, "custom warning on type", 6200, new MessageOrigin (type), WarnVersion.ILLink5));
	}
}