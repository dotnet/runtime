using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Steps;

public class SharedAnnotation
{
	public bool Mark { get; set; }

	public static void Set (LinkContext context, MethodDefinition method, SharedAnnotation value)
	{
		context.Annotations.SetCustomAnnotation (nameof (SharedAnnotation), method, value);
	}

	public static SharedAnnotation Get (LinkContext context, MethodDefinition method) {
		return context.Annotations.GetCustomAnnotation (nameof (SharedAnnotation), method) as SharedAnnotation;
	}
}

public class SharedStateHandler1 : IMarkHandler
{
	LinkContext context;

	public void Initialize (LinkContext context, MarkContext markContext)
	{
		this.context = context;
		markContext.RegisterMarkTypeAction (ProcessType);
	}

	public void ProcessType (TypeDefinition type)
	{
		if (!type.HasMethods)
			return;

		foreach (var method in type.Methods) {
			if (method.Name == "MarkedMethod")
				SharedAnnotation.Set (context, method, new SharedAnnotation () { Mark = true });
		}
	}
}

public class SharedStateHandler2 : IMarkHandler
{
	LinkContext context;

	public void Initialize (LinkContext context, MarkContext markContext)
	{
		this.context = context;
		markContext.RegisterMarkTypeAction (ProcessType);
	}

	public void ProcessType (TypeDefinition type)
	{
		if (!type.HasMethods)
			return;

		foreach (var method in type.Methods) {
			if (SharedAnnotation.Get (context, method) is SharedAnnotation annotation && annotation.Mark)
				context.Annotations.Mark (method);
		}
	}
}