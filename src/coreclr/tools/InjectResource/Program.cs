// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.IO;

CliOption<FileInfo> binOption = new("--bin") { Description = "Binary data to attach to the image" };
CliOption<FileInfo> imageOption = new("--image") { Description = "PE image to add the binary resource into" };
CliOption<string> nameOption = new("--name") { Description = "Resource name" };
CliRootCommand rootCommand = new("Inject native resources into a Portable Executable image");
rootCommand.Options.Add(binOption);
rootCommand.Options.Add(imageOption);
rootCommand.Options.Add(nameOption);

rootCommand.SetAction(result =>
{
    using ResourceUpdater updater = new(result.GetValue(imageOption)!);
    updater.AddBinaryResource(result.GetValue(nameOption)!, File.ReadAllBytes(result.GetValue(binOption)!.FullName));
});

return new CliConfiguration(rootCommand).Invoke(args);
