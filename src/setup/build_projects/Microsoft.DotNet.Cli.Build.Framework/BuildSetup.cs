using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class BuildSetup
    {
        private Dictionary<string, BuildTarget> _targets = new Dictionary<string, BuildTarget>();

        public IList<TargetOverride> _overrides = new List<TargetOverride>();

        public string ProductName { get; }

        public BuildSetup(string productName)
        {
            ProductName = productName;
        }

        public static BuildSetup Create(string productName)
        {
            return new BuildSetup(productName);
        }

        public BuildSetup UseTargets(IEnumerable<BuildTarget> targets)
        {
            foreach (var target in targets)
            {
                BuildTarget previousTarget;
                if (_targets.TryGetValue(target.Name, out previousTarget))
                {
                    _overrides.Add(new TargetOverride(target.Name, previousTarget.Source, target.Source));
                }
                _targets[target.Name] = target;
            }
            return this;
        }

        public BuildSetup UseAllTargetsFromAssembly<T>()
        {
            var asm = typeof(T).GetTypeInfo().Assembly;
            return UseTargets(asm.GetExportedTypes().SelectMany(t => CollectTargets(t)));
        }

        public BuildSetup UseTargetsFrom<T>()
        {
            return UseTargets(CollectTargets(typeof(T)));
        }

        public int Run(string[] args)
        {
            string[] targets;
            string[] environmentVariables;
            ParseArgs(args, out targets, out environmentVariables);

            foreach (string environmentVariable in environmentVariables)
            {
                int delimiterIndex = environmentVariable.IndexOf('=');
                string name = environmentVariable.Substring(0, delimiterIndex);
                string value = environmentVariable.Substring(delimiterIndex + 1);

                Environment.SetEnvironmentVariable(name, value);
            }

            Reporter.Output.WriteBanner($"Building {ProductName}");

            if (_overrides.Any())
            {
                foreach (var targetOverride in _overrides)
                {
                    Reporter.Verbose.WriteLine($"Target {targetOverride.Name} from {targetOverride.OriginalSource} was overridden in {targetOverride.OverrideSource}".Black());
                }
            }

            var context = new BuildContext(_targets, Directory.GetCurrentDirectory());
            BuildTargetResult result = null;
            try
            {
                foreach (var target in targets)
                {
                    result = context.RunTarget(target);
                    if (!result.Success)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Reporter.Error.WriteLine(ex.ToString().Red());
                return 1;
            }

            if (result != null && !result.Success)
            {
                Reporter.Error.WriteLine($"Build failed: {result.ErrorMessage}".Red());
                return 1;
            }
            else
            {
                Reporter.Output.WriteLine("Build succeeded".Green());
                return 0;
            }
        }

        private static void ParseArgs(string[] args, out string[] targets, out string[] environmentVariables)
        {
            List<string> targetList = new List<string>();
            List<string> environmentVariableList = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-t")
                {
                    i++;
                    while (i < args.Length && !args[i].StartsWith("-", StringComparison.Ordinal))
                    {
                        targetList.Add(args[i]);
                        i++;
                    }
                }

                if (args[i] == "-e")
                {
                    i++;
                    while (i < args.Length && !args[i].StartsWith("-", StringComparison.Ordinal))
                    {
                        environmentVariableList.Add(args[i]);
                        i++;
                    }
                }
            }

            targets = targetList.Any() ? targetList.ToArray() : new[] { BuildContext.DefaultTarget };
            environmentVariables = environmentVariableList.ToArray();
        }

        private static IEnumerable<BuildTarget> CollectTargets(Type typ)
        {
            return from m in typ.GetMethods()
                   let targetAttribute = m.GetCustomAttribute<TargetAttribute>()
                   let conditionalAttributes = m.GetCustomAttributes<TargetConditionAttribute>(false)
                   where targetAttribute != null
                   select CreateTarget(m, targetAttribute, conditionalAttributes);
        }

        private static BuildTarget CreateTarget(
            MethodInfo methodInfo,
            TargetAttribute targetAttribute,
            IEnumerable<TargetConditionAttribute> targetConditionAttributes)
        {
            var name = targetAttribute.Name ?? methodInfo.Name;

            var conditions = ExtractTargetConditionsFromAttributes(targetConditionAttributes);

            return new BuildTarget(
                name,
                $"{methodInfo.DeclaringType.FullName}.{methodInfo.Name}",
                targetAttribute.Dependencies,
                conditions,
                (Func<BuildTargetContext, BuildTargetResult>)methodInfo.CreateDelegate(typeof(Func<BuildTargetContext, BuildTargetResult>)));
        }

        private static IEnumerable<Func<bool>> ExtractTargetConditionsFromAttributes(
            IEnumerable<TargetConditionAttribute> targetConditionAttributes)
        {
            if (targetConditionAttributes == null || targetConditionAttributes.Count() == 0)
            {
                return Enumerable.Empty<Func<bool>>();
            }

            return targetConditionAttributes
                    .Select<TargetConditionAttribute, Func<bool>>(c => c.EvaluateCondition)
                    .ToArray();
        }

        private string GenerateSourceString(string file, int? line, string member)
        {
            if (!string.IsNullOrEmpty(file) && line != null)
            {
                return $"{file}:{line}";
            }
            else if (!string.IsNullOrEmpty(member))
            {
                return member;
            }
            return string.Empty;
        }

        public class TargetOverride
        {
            public string Name { get; }
            public string OriginalSource { get; }
            public string OverrideSource { get; }

            public TargetOverride(string name, string originalSource, string overrideSource)
            {
                Name = name;
                OriginalSource = originalSource;
                OverrideSource = overrideSource;
            }
        }
    }
}
