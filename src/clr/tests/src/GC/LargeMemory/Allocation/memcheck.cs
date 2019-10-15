using System;
using System.IO;
using System.Text.RegularExpressions;

public sealed class MemCheck {

    public static int GetPhysicalMem() {
        if(File.Exists("/proc/meminfo")){
            string[] lines = System.IO.File.ReadAllLines("/proc/meminfo");
            foreach(string line in lines){
                if(line.StartsWith("MemAvailable")){
                    int availableMem = Int32.Parse(Regex.Match(line, @"\d+").Value);
                    return availableMem / 1024;
                }
            }
        }
        return -1;
    }

}

