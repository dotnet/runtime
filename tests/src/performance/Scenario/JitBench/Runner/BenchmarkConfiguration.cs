using System;
using System.Collections.Generic;
using System.Text;

namespace JitBench
{
    public class BenchmarkConfiguration
    {
        public BenchmarkConfiguration()
        {
            Name = "Default";
            EnvironmentVariables = new Dictionary<string, string>();
        }
        public bool IsDefault {  get { return Name == "Default"; } }
        public string Name { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; private set; }

        public BenchmarkConfiguration WithTiering()
        {
            return WithModifier("Tiering", "COMPLUS_EXPERIMENTAL_TieredCompilation", "1");
        }

        public BenchmarkConfiguration WithMinOpts()
        {
            return WithModifier("Minopts", "COMPLUS_JitMinOpts", "1");
        }

        public BenchmarkConfiguration WithNoR2R()
        {
            return WithModifier("NoR2R", "COMPlus_ReadyToRun", "0");
        }

        public BenchmarkConfiguration WithNoNgen()
        {
            return WithModifier("NoNgen", "COMPLUS_ZapDisable", "1");
        }

        private BenchmarkConfiguration WithModifier(string modifier, string variableName, string variableValue)
        {
            if (IsDefault)
            {
                Name = modifier;
            }
            else
            {
                Name += " " + modifier;
            }
            EnvironmentVariables.Add(variableName, variableValue);
            return this;
        }
    }
}
