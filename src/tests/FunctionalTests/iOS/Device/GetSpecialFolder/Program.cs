// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public static class Program
{
    public static void ManagedGetSpecialFolder()
    {
        Console.WriteLine("---------------------------");
        Console.WriteLine("Managed:");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.AdminTools)}            : {Environment.GetFolderPath(System.Environment.SpecialFolder.AdminTools, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.ApplicationData)}       : {Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CDBurning)}             : {Environment.GetFolderPath(System.Environment.SpecialFolder.CDBurning, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonAdminTools)}      : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonAdminTools, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonApplicationData)} : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonDesktopDirectory)}: {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonDesktopDirectory, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonDocuments)}       : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonDocuments, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonMusic)}           : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonMusic, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonOemLinks)}        : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonOemLinks, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonPictures)}        : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonPictures, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonProgramFiles)}    : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonProgramFiles, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonProgramFilesX86)} : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonProgramFilesX86, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonPrograms)}        : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonPrograms, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonStartMenu)}       : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonStartMenu, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonStartup)}         : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonStartup, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonTemplates)}       : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonTemplates, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.CommonVideos)}          : {Environment.GetFolderPath(System.Environment.SpecialFolder.CommonVideos, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.Cookies)}               : {Environment.GetFolderPath(System.Environment.SpecialFolder.Cookies, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.Desktop)}               : {Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.DesktopDirectory)}      : {Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.Favorites)}             : {Environment.GetFolderPath(System.Environment.SpecialFolder.Favorites, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.Fonts)}                 : {Environment.GetFolderPath(System.Environment.SpecialFolder.Fonts, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.History)}               : {Environment.GetFolderPath(System.Environment.SpecialFolder.History, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.InternetCache)}         : {Environment.GetFolderPath(System.Environment.SpecialFolder.InternetCache, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.LocalApplicationData)}  : {Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.LocalizedResources)}    : {Environment.GetFolderPath(System.Environment.SpecialFolder.LocalizedResources, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.MyComputer)}            : {Environment.GetFolderPath(System.Environment.SpecialFolder.MyComputer, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.MyDocuments)}           : {Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.MyMusic)}               : {Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.MyPictures)}            : {Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.MyVideos)}              : {Environment.GetFolderPath(System.Environment.SpecialFolder.MyVideos, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.NetworkShortcuts)}      : {Environment.GetFolderPath(System.Environment.SpecialFolder.NetworkShortcuts, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.Personal)}              : {Environment.GetFolderPath(System.Environment.SpecialFolder.Personal, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.PrinterShortcuts)}      : {Environment.GetFolderPath(System.Environment.SpecialFolder.PrinterShortcuts, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.ProgramFiles)}          : {Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.ProgramFilesX86)}       : {Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.Programs)}              : {Environment.GetFolderPath(System.Environment.SpecialFolder.Programs, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.Recent)}                : {Environment.GetFolderPath(System.Environment.SpecialFolder.Recent, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.Resources)}             : {Environment.GetFolderPath(System.Environment.SpecialFolder.Resources, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.SendTo)}                : {Environment.GetFolderPath(System.Environment.SpecialFolder.SendTo, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.StartMenu)}             : {Environment.GetFolderPath(System.Environment.SpecialFolder.StartMenu, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.Startup)}               : {Environment.GetFolderPath(System.Environment.SpecialFolder.Startup, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.System)}                : {Environment.GetFolderPath(System.Environment.SpecialFolder.System, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.SystemX86)}             : {Environment.GetFolderPath(System.Environment.SpecialFolder.SystemX86, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.Templates)}             : {Environment.GetFolderPath(System.Environment.SpecialFolder.Templates, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.UserProfile)}           : {Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine($"{nameof(System.Environment.SpecialFolder.Windows)}               : {Environment.GetFolderPath(System.Environment.SpecialFolder.Windows, System.Environment.SpecialFolderOption.DoNotVerify)}");
        Console.WriteLine("---------------------------");
    }

    public static async Task<int> Main(string[] args)
    {
        ManagedGetSpecialFolder();
        Console.WriteLine("Done!");
        await Task.Delay(5000);

        return 42;
    }
}
