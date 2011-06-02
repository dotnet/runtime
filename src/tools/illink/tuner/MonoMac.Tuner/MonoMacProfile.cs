using System;
using System.Collections.Generic;

using Mono.Tuner;

using Mono.Cecil;

namespace MonoMac.Tuner {

	class MonoMacProfile : Profile {

		static readonly HashSet<string> Sdk = new HashSet<string> {
			"mscorlib",
			"I18N.CJK",
			"I18N",
			"I18N.MidEast",
			"I18N.Other",
			"I18N.Rare",
			"I18N.West",
			"Microsoft.Build.Engine",
			"Microsoft.Build.Framework",
			"Microsoft.Build.Tasks.v4.0",
			"Microsoft.Build.Utilities.v4.0",
			"Microsoft.CSharp",
			"Microsoft.Web.Infrastructure",
			"Mono.C5",
			"Mono.Cairo",
			"Mono.CodeContracts",
			"Mono.CompilerServices.SymbolWriter",
			"Mono.Configuration.Crypto",
			"Mono.CSharp",
			"Mono.Data.Sqlite",
			"Mono.Data.Tds",
			"Mono.Debugger.Soft",
			"Mono.Http",
			"Mono.Management",
			"Mono.Messaging",
			"Mono.Messaging.RabbitMQ",
			"Mono.Options",
			"Mono.Parallel",
			"Mono.Posix",
			"Mono.Security",
			"Mono.Security.Win32",
			"Mono.Simd",
			"Mono.Tasklets",
			"Mono.Tuner",
			"Mono.WebBrowser",
			"Mono.Web",
			"Novell.Directory.Ldap",
			"Npgsql",
			"OpenSystem.C",
			"PEAPI",
			"System.ComponentModel.Composition",
			"System.ComponentModel.DataAnnotations",
			"System.Configuration",
			"System.Configuration.Install",
			"System.Core",
			"System.Data.DataSetExtensions",
			"System.Data",
			"System.Data.Linq",
			"System.Data.OracleClient",
			"System.Data.Services.Client",
			"System.Data.Services",
			"System.Design",
			"System.DirectoryServices",
			"System",
			"System.Drawing.Design",
			"System.Drawing",
			"System.Dynamic",
			"System.EnterpriseServices",
			"System.IdentityModel",
			"System.IdentityModel.Selectors",
			"System.Management",
			"System.Messaging",
			"System.Net",
			"System.Numerics",
			"System.Runtime.Caching",
			"System.Runtime.DurableInstancing",
			"System.Runtime.Remoting",
			"System.Runtime.Serialization",
			"System.Runtime.Serialization.Formatters.Soap",
			"System.Security",
			"System.ServiceModel.Discovery",
			"System.ServiceModel",
			"System.ServiceModel.Routing",
			"System.ServiceModel.Web",
			"System.ServiceProcess",
			"System.Transactions",
			"System.Web.Abstractions",
			"System.Web.ApplicationServices",
			"System.Web",
			"System.Web.DynamicData",
			"System.Web.Extensions.Design",
			"System.Web.Extensions",
			"System.Web.Routing",
			"System.Web.Services",
			"System.Windows.Forms.DataVisualization",
			"System.Windows.Forms",
			"System.Xaml",
			"System.Xml",
			"System.Xml.Linq",
			"WebMatrix.Data",
			"WindowsBase",
			"Microsoft.VisualBasic",
		};

		protected override bool IsSdk (AssemblyDefinition assembly)
		{
			return Sdk.Contains (assembly.Name.Name);
		}

		protected override bool IsProduct (AssemblyDefinition assembly)
		{
			return assembly.Name.Name == "MonoMac";
		}
	}
}
