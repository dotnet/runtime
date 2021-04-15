using System;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class ComputeDeltaFileOutputNames : Task {
    [Required]
    public string BaseAssemblyName { get; set; }
    [Required]
    public int DeltaCount { get; set; }

    [Output]
    public ITaskItem[] DeltaOutputs { get; set; }

    public override bool Execute ()
    {
        int count = DeltaCount;
        if (count == 0) {
            Log.LogError("Did not expect 0 deltas");
            return false;
        }
        string baseAssemblyName = BaseAssemblyName;
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
