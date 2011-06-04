using System;
using System.IO;
using System.Xml.XPath;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class PreserveHttps : BaseStep {

		static string [] types = new [] {
			"System.Net.WebRequest",
			"System.Net.WebClient",
			"System.Net.Security.RemoteCertificateValidationCallback",
			"System.Web.Services.Protocols.WebClientProtocol",
			"System.Security.Cryptography.X509Certificates.X509Certificate",
			"System.Web.Services.WebServiceBindingAttribute",
			"System.Web.Services.Protocols.SoapHttpClientProtocol",
		};

		bool need_https;

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (need_https)
				return;

			if (Profile.IsSdkAssembly (assembly))
				return;

			if (HasNeededReference (assembly.MainModule))
				need_https = true;
		}

		static bool HasNeededReference (ModuleDefinition module)
		{
			foreach (var type in types)
				if (module.HasTypeReference (type))
					return true;

			return false;
		}

		protected override void EndProcess ()
		{
			if (!need_https)
				return;

			var mono_security = Context.Resolve ("Mono.Security");
			if (mono_security == null)
				return;

			if (Annotations.GetAction (mono_security) != AssemblyAction.Link)
				return;

			var xml_preserve = CreatePreserveStep ();
			Context.Pipeline.AddStepAfter (typeof (PreserveHttps), xml_preserve);
//			Context.Pipeline.AddStepAfter (xml_preserve, new PreserveCrypto ());
		}

		static IStep CreatePreserveStep ()
		{
			return new ResolveFromXmlStep (
				new XPathDocument (
					new StringReader (descriptor)));
		}

		const string descriptor = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<linker>
	<assembly fullname=""mscorlib"">
		<namespace fullname=""System.Security.Cryptography"" />
	</assembly>
	<assembly fullname=""System"">
		<namespace fullname=""System.Security.Cryptography"" />
	</assembly>
	<assembly fullname=""Mono.Security"">
		<type fullname=""Mono.Security.Protocol.Tls.HttpsClientStream"" />
		<type fullname=""Mono.Security.Protocol.Tls.SslClientStream"">
			<method name=""get_SelectedClientCertificate"" />
		</type>
		<type fullname=""Mono.Security.Protocol.Tls.SslStreamBase"">
			<method name=""get_ServerCertificate"" />
		</type>
	</assembly>
</linker>
";
	}
}
