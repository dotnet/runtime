// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;

class Program
{
    static int Main(string[] args)
    {
        // Test that we can access resources
        ResourceManager rm = new ResourceManager("SatelliteAssemblies.Strings", Assembly.GetExecutingAssembly());

        // Test English (default)
        string englishGreeting = rm.GetString("Greeting", CultureInfo.InvariantCulture);
        Console.WriteLine($"English: {englishGreeting}");
        if (englishGreeting != "Hello, World!")
        {
            Console.WriteLine("ERROR: Expected 'Hello, World!' for English");
            return 1;
        }

        // Test Spanish
        string spanishGreeting = rm.GetString("Greeting", new CultureInfo("es"));
        Console.WriteLine($"Spanish: {spanishGreeting}");
        if (spanishGreeting != "¡Hola, Mundo!")
        {
            Console.WriteLine("ERROR: Expected '¡Hola, Mundo!' for Spanish");
            return 1;
        }

        // Verify that satellite assemblies are NOT in the publish directory
        // when this is published with PublishAot
        string exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
        string publishDir = Path.GetDirectoryName(exePath);
        
        if (string.IsNullOrEmpty(publishDir))
        {
            Console.WriteLine("ERROR: Could not determine publish directory");
            return 1;
        }

        // Check that the 'es' subdirectory does not exist in publish output
        string esDir = Path.Combine(publishDir, "es");
        if (Directory.Exists(esDir))
        {
            Console.WriteLine($"ERROR: Satellite assembly directory exists: {esDir}");
            Console.WriteLine("Satellite assemblies should be embedded in the native binary, not copied to publish folder");
            
            // List what's in there for diagnostic purposes
            var files = Directory.GetFiles(esDir);
            foreach (var file in files)
            {
                Console.WriteLine($"  Found: {Path.GetFileName(file)}");
            }
            
            return 1;
        }

        Console.WriteLine("SUCCESS: Resources are accessible and no satellite assembly folders in publish directory");
        return 100;
    }
}
