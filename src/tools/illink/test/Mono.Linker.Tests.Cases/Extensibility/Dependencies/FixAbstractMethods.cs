using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Steps;

public class FixAbstractMethods : IMarkHandler
{
	LinkContext _context;

	public void Initialize (LinkContext context, MarkContext markContext)
	{
		_context = context;
		markContext.RegisterMarkTypeAction (type => ProcessType (type));
	}

	void ProcessType (TypeDefinition type)
	{
		if (!type.Name.Contains ("InterfaceImplementation"))
			return;

		Assert (!type.IsAbstract && type.HasInterfaces);
		var iface = type.Interfaces[0];
		Assert (iface.InterfaceType.Name == "InterfaceType");
		var interfaceType = iface.InterfaceType.Resolve ();
		var method = interfaceType.Methods[0];
		Assert (method.Name == "AbstractMethod");

		var newMethod = new MethodDefinition (method.Name, (method.Attributes | MethodAttributes.Final) & ~MethodAttributes.Abstract, method.ReturnType);
		Assert (!method.HasParameters);
		var ilProcessor = newMethod.Body.GetILProcessor ();
		ilProcessor.Append (ilProcessor.Create (Mono.Cecil.Cil.OpCodes.Ret));

		type.Methods.Add (newMethod);
	}

	static void Assert (bool b)
	{
		if (!b)
			throw new Exception ();
	}
}
