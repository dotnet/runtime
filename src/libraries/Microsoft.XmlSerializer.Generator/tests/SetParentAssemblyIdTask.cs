using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.XmlSerializer.Generator.Tests.Tasks
{
    public class SetParentAssemblyId : Task
    {
        [Required]
        public string CodeFile { get; set; }

        public override bool Execute()
        {
            //
            // Roughly based on System.Xml.Serialization.TempAssembly.GenerateAssemblyId()
            //

            var list = new ArrayList();
            Type type = typeof(SerializationTypes.SimpleType);
            foreach (var module in type.Assembly.GetModules())
            {
                list.Add(module.ModuleVersionId.ToString());
            }
            list.Sort();

            var sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(list[i]!.ToString());
                sb.Append(',');
            }
            string parentAssemblyId = sb.ToString();

            string content = File.ReadAllText(CodeFile);
            content = content.Replace("%%ParentAssemblyId%%", parentAssemblyId);
            File.WriteAllText(CodeFile, content);

            return true;
        }
    }
}