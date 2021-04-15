using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class ComputeDeltaFileOutputNames : Microsoft.Build.Utilities.Task {
    [Required]
    public string BaseAssemblyName { get; set; }
    [Required]
    public string DeltaScript {get; set; }

    [Output]
    public ITaskItem[] DeltaOutputs { get; set; }

    public override bool Execute()
    {
        if (!System.IO.File.Exists (DeltaScript)) {
            Log.LogError("Hot reload delta script {0} does not exist", DeltaScript);
            return false;
        }
        string baseAssemblyName = BaseAssemblyName;
        int count;
        try {
            var json = DeltaScriptParser.Parse (DeltaScript).Result;
            count = json.Changes.Length;
        } catch (System.Text.Json.JsonException exn) {
            Log.LogErrorFromException (exn, showStackTrace: true);
            return false;
        }
        ITaskItem[] result = new TaskItem[3*count];
        for (int i = 0; i < count; ++i) {
            int rev = 1+i;
            string dmeta = baseAssemblyName + $".{rev}.dmeta";
            string dil = baseAssemblyName + $".{rev}.dil";
            string dpdb = baseAssemblyName + $".{rev}.dpdb";
            result[3*i] = new TaskItem(dmeta);
            result[3*i+1] = new TaskItem(dil);
            result[3*i+2] = new TaskItem(dpdb);
        }
        DeltaOutputs = result;
        return true;
    }

}
