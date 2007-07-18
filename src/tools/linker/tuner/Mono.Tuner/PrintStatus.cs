using System;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {
	
	public class PrintStatus : BaseStep {

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			Console.WriteLine ("Assembly `{0}' ({1}) tuned", assembly.Name, assembly.MainModule.Image.FileInformation);
		}
	}
}
