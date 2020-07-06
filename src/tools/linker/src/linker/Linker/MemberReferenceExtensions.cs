using Mono.Cecil;

namespace Mono.Linker
{
	public static class MemberReferenceExtensions
	{
		public static string GetDisplayName (this MemberReference member)
		{
			switch (member) {
			case TypeReference type:
				return type.GetDisplayName ();

			case MethodReference method:
				return method.GetDisplayName ();

			default:
				return member.FullName;
			}
		}

		public static string GetNamespaceDisplayName (this MemberReference member)
		{
			var type = member is TypeReference typeReference ? typeReference : member.DeclaringType;
			while (type.DeclaringType != null)
				type = type.DeclaringType;

			return type.Namespace;
		}
	}
}
