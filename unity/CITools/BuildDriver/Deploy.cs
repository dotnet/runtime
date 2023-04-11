// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NiceIO;

namespace BuildDriver;

static class Deploy
{
    public static void ToPlayer(GlobalConfig gConfig, NPath playerPath)
    {
        Console.WriteLine("******************************");
        Console.WriteLine($"Deploying Runtime to Player : {playerPath}");
        Console.WriteLine("******************************");


        var sourceDir = Utils.RuntimeArtifactDirectory(gConfig);
        var destinationDir = playerPath.Combine("CoreCLR");

        var destinationFiles = destinationDir
            .Files(recurse: true)
            .Select(f => f.RelativeTo(destinationDir).ToString())
            .ToHashSet();

        foreach (var sourceFile in sourceDir.Files(recurse: true))
        {
            // Only update files that exist in the player.  This seems like the safest thing to do in case the player build process decides to drop any files
            if (!destinationFiles.Contains(sourceFile.RelativeTo(sourceDir)))
                continue;

            var fileDestination = destinationDir.Combine(sourceFile.RelativeTo(sourceDir));
            sourceFile.Copy(fileDestination);

            // Display some info to give feedback that files were copied
            // Use a relative path and don't include the source path to keep the output smaller.
            // When I included full source path and full destination path it was this wall of text that was hard to read
            Console.WriteLine($"Updated : {fileDestination.RelativeTo(playerPath)}");
        }
    }
}
