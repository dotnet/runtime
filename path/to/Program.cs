# Complete code for Program.cs
using System;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var installerTasks = new InstallerTasks();
            installerTasks.Install();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}