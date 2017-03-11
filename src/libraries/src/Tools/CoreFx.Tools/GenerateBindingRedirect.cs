using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace Microsoft.DotNet.Build.Tasks
{
    public class GenerateBindingRedirect : Task
    {
        [Required]
        public ITaskItem[] Assemblies { get; set; }

        [Required]
        public ITaskItem[] Executables { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public bool OutputCodeBase { get; set; }

        private static XNamespace ns { get; set; }

        public override bool Execute()
        {
            ns = "urn:schemas-microsoft-com:asm.v1";
            XElement bindingRedirectAssemblies = new XElement(ns + "assemblyBinding");
            foreach (ITaskItem assembly in Assemblies)
            {
                AssemblyName result = null;
                try
                {
                    using (FileStream assemblyStream = new FileStream(assembly.ItemSpec, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read))
                    using (PEReader peReader = new PEReader(assemblyStream, PEStreamOptions.LeaveOpen))
                    {
                        if (peReader.HasMetadata)
                        {
                            MetadataReader reader = peReader.GetMetadataReader();
                            if (reader.IsAssembly)
                            {
                                AssemblyDefinition assemblyDef = reader.GetAssemblyDefinition();

                                result = new AssemblyName();
                                result.Name = reader.GetString(assemblyDef.Name);
                                result.CultureName = reader.GetString(assemblyDef.Culture);
                                result.Version = assemblyDef.Version;

                                if (!assemblyDef.PublicKey.IsNil)
                                {
                                    result.SetPublicKey(reader.GetBlobBytes(assemblyDef.PublicKey));
                                }
                            }
                        }
                        else
                        {
                            //Native
                            continue;
                        }
                    }
                }
                catch (BadImageFormatException)
                {
                    // not a PE
                    continue;
                }

                string publicKeyToken = string.Empty;
                if (result.GetPublicKeyToken() != null)
                { 
                    publicKeyToken = BitConverter.ToString(result.GetPublicKeyToken()).Replace("-", "");
                }
                if (string.IsNullOrEmpty(publicKeyToken))
                {
                    Log.LogMessage(MessageImportance.Low, $"Empty publicKeyToken for {result.FullName} ");
                }
                string culture = string.IsNullOrEmpty(result.CultureName) ? "neutral" : result.CultureName;
                XElement assemblyIdentity = new XElement(ns + "assemblyIdentity",
                    new XAttribute("name", result.Name),
                    new XAttribute("publicKeyToken", publicKeyToken),
                    new XAttribute("culture", culture));
                XElement bindingRedirect = new XElement(ns + "bindingRedirect",
                    new XAttribute("oldVersion", $"0.0.0.0-{result.Version}"),
                    new XAttribute("newVersion", result.Version));
                XElement dependentAssembly = new XElement(ns + "dependentAssembly",
                    assemblyIdentity,
                    bindingRedirect);

                if (OutputCodeBase)
                {
                    XElement codeBase = new XElement(ns + "codeBase",
                                                new XAttribute("version", result.Version),
                                                new XAttribute("href", Path.GetFullPath(assembly.ItemSpec)));
                    dependentAssembly.Add(codeBase);
                }

                bindingRedirectAssemblies.Add(dependentAssembly);

            }
            XDocument doc = new XDocument(new XElement("configuration", new XElement("runtime", bindingRedirectAssemblies)));
            foreach (ITaskItem executable in Executables)
            {
                string executableName = Path.GetFileName(executable.ItemSpec);
                using (FileStream fs = new FileStream(Path.Combine(OutputPath, executableName + ".config"), FileMode.Create))
                {
                    doc.Save(fs);
                }
            }
            
            return !Log.HasLoggedErrors;
        }
    }
}
