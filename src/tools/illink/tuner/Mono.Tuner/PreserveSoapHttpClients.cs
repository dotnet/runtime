using System;

using Mono.Linker;

using Mono.Cecil;

namespace Mono.Tuner {

	public class PreserveSoapHttpClients : BaseSubStep {

		public override SubStepTargets Targets {
			get { return SubStepTargets.Type; }
		}

		public override bool IsActiveFor (AssemblyDefinition assembly)
		{
			return Annotations.GetAction (assembly) == AssemblyAction.Link && !Profile.IsSdkAssembly (assembly);
		}

		public override void ProcessType (TypeDefinition type)
		{
			if (IsWebServiceClient (type))
				PreserveClient (type);
		}

		void PreserveClient (TypeDefinition type)
		{
			if (!type.HasMethods)
				return;

			foreach (MethodDefinition method in type.Methods) {
				string sync_method;
				if (!TryExtractSyncMethod (method, out sync_method))
					continue;

				AddPreservedMethod (method, sync_method);
			}
		}

		void AddPreservedMethod (MethodDefinition target, string methodName)
		{
			foreach (MethodDefinition method in target.DeclaringType.Methods)
				if (method.Name == methodName)
					Annotations.AddPreservedMethod (target, method);
		}

		static bool TryExtractSyncMethod (MethodDefinition method, out string sync_method)
		{
			if (TryExtractPrefixedMethodName ("Begin", method.Name, out sync_method))
				return true;

			if (TryExtractPrefixedMethodName ("End", method.Name, out sync_method))
				return true;

			if (TryExtractSuffixedMethodName ("Async", method.Name, out sync_method))
				return true;

			return false;
		}

		static bool TryExtractPrefixedMethodName (string prefix, string fullName, out string methodName)
		{
			methodName = null;

			int pos = fullName.IndexOf (prefix);
			if (pos == -1)
				return false;

			methodName = fullName.Substring (prefix.Length);
			return true;
		}

		static bool TryExtractSuffixedMethodName (string suffix, string fullName, out string methodName)
		{
			methodName = null;

			int pos = fullName.LastIndexOf (suffix);
			if (pos == -1)
				return false;

			methodName = fullName.Substring (0, pos);
			return true;
		}

		static bool IsWebServiceClient (TypeDefinition type)
		{
			return type.Inherits ("System.Web.Services.Protocols.SoapHttpClientProtocol");
		}
	}
}