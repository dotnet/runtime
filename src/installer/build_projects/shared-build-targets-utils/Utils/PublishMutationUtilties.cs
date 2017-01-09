using Microsoft.DotNet.Cli.Build.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build
{
    public class PublishMutationUtilties
    {
        public static void CleanPublishOutput(
            string path, 
            string name, 
            bool deleteRuntimeConfigJson=false, 
            bool deleteDepsJson=false,
            bool deleteAppHost=false)
        {
            File.Delete(Path.Combine(path, $"{name}{Constants.ExeSuffix}"));
            File.Delete(Path.Combine(path, $"{name}.dll"));
            File.Delete(Path.Combine(path, $"{name}.pdb"));

            if (deleteRuntimeConfigJson)
            {
                File.Delete(Path.Combine(path, $"{name}.runtimeconfig.json"));
            }

            if (deleteDepsJson)
            {
                File.Delete(Path.Combine(path, $"{name}.deps.json"));
            }

            if (deleteAppHost)
            {
                File.Delete(Path.Combine(path, $"apphost{Constants.ExeSuffix}"));
            }
        }

        public static void ChangeEntryPointLibraryName(string depsFile, string newName)
        {
            JToken deps;
            using (var file = File.OpenText(depsFile))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                deps = JObject.ReadFrom(reader);
            }

            string version = null;
            foreach (JProperty target in deps["targets"])
            {
                var targetLibrary = target.Value.Children<JProperty>().FirstOrDefault();
                if (targetLibrary == null)
                {
                    continue;
                }
                version = targetLibrary.Name.Substring(targetLibrary.Name.IndexOf('/') + 1);
                if (newName == null)
                {
                    targetLibrary.Remove();
                }
                else
                {
                    targetLibrary.Replace(new JProperty(newName + '/' + version, targetLibrary.Value));
                }
            }
            if (version != null)
            {
                var library = deps["libraries"].Children<JProperty>().First();
                if (newName == null)
                {
                    library.Remove();
                }
                else
                {
                    library.Replace(new JProperty(newName + '/' + version, library.Value));
                }
                using (var file = File.CreateText(depsFile))
                using (var writer = new JsonTextWriter(file) { Formatting = Formatting.Indented })
                {
                    deps.WriteTo(writer);
                }
            }
        }
    }
}
