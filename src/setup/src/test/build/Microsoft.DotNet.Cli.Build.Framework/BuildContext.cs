using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class BuildContext
    {
        private IDictionary<string, BuildTargetResult> _completedTargets = new Dictionary<string, BuildTargetResult>(StringComparer.OrdinalIgnoreCase);

        public static readonly string DefaultTarget = "Default";

        private int _maxTargetLen;
        private Stack<string> _targetStack = new Stack<string>();

        public IDictionary<string, BuildTarget> Targets { get; }

        public IDictionary<string, object> Properties = new Dictionary<string, object>();

        public string BuildDirectory { get; }

        public object this[string name]
        {
            get
            {
                if (Properties.ContainsKey(name))
                {
                    return Properties[name];
                }
                else
                {
                    throw new KeyNotFoundException("No property with key " + name + " was found.");
                }
            }
            set { Properties[name] = value; }
        }

        public BuildContext(IDictionary<string, BuildTarget> targets, string buildDirectory)
        {
            Targets = targets;
            BuildDirectory = buildDirectory;
            _maxTargetLen = targets.Values.Select(t => t.Name.Length).Max();
        }

        public T Get<T>(string name) => (T)this[name];

        public BuildTargetResult RunTarget(string name) => RunTarget(name, force: false);

        public BuildTargetResult RunTarget(string name, bool force)
        {
            BuildTarget target;
            if (!Targets.TryGetValue(name, out target))
            {
                throw new UndefinedTargetException($"Undefined target: {name}");
            }

            if (!EvaluateTargetConditions(target))
            {
                Reporter.Verbose.WriteLine($"Skipping, Target Conditions not met: {target.Name}");
                return new BuildTargetResult(target, success: true);
            }

            // Check if it's been completed
            BuildTargetResult result;
            if (!force && _completedTargets.TryGetValue(name, out result))
            {
                Reporter.Verbose.WriteLine($"Skipping completed target: {target.Name}");
                return result;
            }

            // It hasn't, or we're forcing, so run it
            result = ExecTarget(target);
            _completedTargets[target.Name] = result;
            return result;
        }

        public void Verbose(string message)
        {
            Reporter.Output.WriteLine("trace".White() + $": {message}");
        }

        public void Info(string message)
        {
            Reporter.Output.WriteLine("info ".Green() + $": {message}");
        }

        public void Warn(string message)
        {
            Reporter.Output.WriteLine("warn ".Yellow() + $": {message}");
        }

        public void Error(string message)
        {
            Reporter.Error.WriteLine("error".Red().Bold() + $": {message}");
        }

        private bool EvaluateTargetConditions(BuildTarget target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (target.Conditions == null)
            {
                return true;
            }

            foreach (var condition in target.Conditions)
            {
                if (!condition())
                {
                    return false;
                }
            }

            return true;
        }

        private BuildTargetResult ExecTarget(BuildTarget target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var sectionName = $"{target.Name.PadRight(_maxTargetLen + 2).Yellow()} ({target.Source.White()})";
            BuildReporter.BeginSection("TARGET", sectionName);

            BuildTargetResult result;

            // Run the dependencies
            var dependencyResults = new Dictionary<string, BuildTargetResult>();
            var failedDependencyResult = RunDependencies(target, dependencyResults);
            if (failedDependencyResult != null)
            {
                result = failedDependencyResult;
            }
            else if (target.Body != null)
            {
                try
                {
                    result = target.Body(new BuildTargetContext(this, target, dependencyResults));
                }
                catch (Exception ex)
                {
                    result = new BuildTargetResult(target, success: false, exception: ex);
                }
            }
            else
            {
                result = new BuildTargetResult(target, success: true);
            }
            BuildReporter.EndSection("TARGET", sectionName, result.Success);

            return result;
        }

        private BuildTargetResult RunDependencies(BuildTarget target, Dictionary<string, BuildTargetResult> dependencyResults)
        {
            BuildTargetResult result = null;
            foreach (var dependency in target.Dependencies)
            {
                result = RunTarget(dependency);
                dependencyResults[dependency] = result;

                if (!result.Success)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
