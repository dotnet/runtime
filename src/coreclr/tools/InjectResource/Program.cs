using System.CommandLine;
using System.IO;

var binOption = new Option<FileInfo?>(
    name: "--bin",
    description: "Binary data to attach to the image");
var imageOption = new Option<FileInfo?>(
    name: "--image",
    description: "PE image to add the binary resource into");
var nameOption = new Option<string>(
    name: "--name",
    description: "Resource name");
var rootCommand = new RootCommand("Inject native resources into a Portable Executable image");
rootCommand.AddOption(binOption);
rootCommand.AddOption(imageOption);
rootCommand.AddOption(nameOption);

rootCommand.SetHandler(
    async (FileInfo binaryData, FileInfo peImage, string name) => 
        {
            using ResourceUpdater updater = new(peImage);
            updater.AddBinaryResource(name, await File.ReadAllBytesAsync(binaryData.FullName));
        },
    binOption,
    imageOption,
    nameOption);

return await rootCommand.InvokeAsync(args);