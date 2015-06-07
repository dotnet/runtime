Creating a Custom DNX
=====================

These instructions will lead you through creating a custom [DNX](https://github.com/aspnet/dnx) using an official DNX and the results of CoreCLR build. The same approach can be be used with the CoreFX build; however, that part isn't explored with these instructions. CoreFX is a bit harder, too, since you need to worry more about dependencies.

These instructions are specific to Windows and assume that you've just completed the [Windows installation instructions](windows-instructions.md). They use the same demo `C:\coreclr-demo` directory and assume the same artifacts. The same general idea will work on OS X and Linux once .NET Core is supported more broadly on those OSes.

Introduction to DNX
===================

DNX is the ".NET Execution Environment". It's a concept that came out of the ASP.NET 5 project, but is more general than Web. DNX can be looked at a few different ways:

- It is a distribution of .NET Core.
- It delivers a source-first and NuGet-first .NET programming experience.
- It is console-everything.
- It is the base mechanism and set of concepts on which ASP.NET 5 is built.

You can have more than one DNX installed (under `.dnx`) on a machine and a single one will be selected for use at any time. You use [DNVM](https://github.com/aspnet/dnvm), the .NET SDK  Manager, to acquire and manage DNX instances.

DNX instances differ/disambiguate on the following axis:

- Version
- CPU architecture
- Runtime type (CLR, CoreCLR, Mono)

Installing DNVM
===============

You need DNVM as a starting point. DNVM enables you to acquire a (or multiple) DNX. The easiest installation approach is to follow the [DNVM instructions](https://github.com/aspnet/home#install-the-net-version-manager-dnvm) on the ASP.NET home repo. DNVM is simply a script, which doesn't depend on or include a DNX or CoreCLR.

You can request to see which DNXs are available, with `dnvm list`, which will display an empty set of installed runtimes.

	C:\coreclr-demo>dnvm list

Installing a DNX
================

You install a DNX via DNVM. It's easy to install the latest default DNX, using the upgrade command.

	C:\coreclr-demo>dnvm upgrade

Alternatively, you can use the install command:

	C:\coreclr-demo>dnvm install latest

Both commands will install a default DNX, which on Windows, is x86 and hosted on the .NET Framework CLR. You can see that with `dnvm list`.

	C:\coreclr-demo>dnvm list

	Active Version           Runtime Architecture Location 
	------ -------           ------- ------------ --------
	  *    1.0.0-beta5-11427 clr     x86          C:\Users\rlander\.dnx\runtimes

Trying out DNX
==============

As stated above, these instructions assume that you've just completed the [Windows installation instructions](windows-instructions.md), which included a "Hello World" demo.

You need one extra file for the demo to work, which is project.json. Add it beside HelloWorld.cs.

```json
{
    "version": "1.0.0-*",
    "dependencies": {
    },
    "frameworks" : {
        "dnx451" : { },
        "dnxcore50" : {
            "dependencies": {
                "System.Console": "4.0.0-beta-*"
            }
        }
    }
}
```

You need to restore dependent nuget packages with the dnu tool (comes with DNX). DNU will pull down NuGet packages from nuget.org or any other configured NuGet feed, such as MyGet.

	C:\coreclr-demo>dnu restore

You can now run the app, with dnx.

	C:\coreclr-demo>dnx . run

Switching to CoreCLR
====================

For the purpose of this demo, you need an x64 DNX hosted on (actually including) CoreCLR. The CoreCLR build only generates X64 artifacts currently. You can install the latest version of an X64 CoreCLR DNX with DNVM. You'll notice that the install process will crossgen the managed assemblies for you. Performance!

	C:\coreclr-demo>dnvm install latest -arch x64 -runtime coreclr

You should now have two runtimes installed. The CoreCLR one will be the default. The install process calls `dnvm use` for you as a convenience, since that's probably what you wanted. `dnvm list` will demonstrate this change in state.

	C:\Users\rlander>dnvm list

	Active Version           Runtime Architecture Location
	------ -------           ------- ------------ --------
	       1.0.0-beta5-11447 clr     x86          C:\Users\rlander\.dnx\runtimes
	  *    1.0.0-beta5-11447 coreclr x64          C:\Users\rlander\.dnx\runtimes

You will need to repeat the steps above to run your app on CoreCLR (CoreCLR requires more NuGet packages).

	C:\coreclr-demo>dnu restore
	C:\coreclr-demo>dnx . run

Creating a Custom DNX
=====================

By this point, the demo is working with an official DNX. You can now create a custom DNX with a custom build of CoreCLR. For the purpose of safe experimentation, you are recommended to copy and rename an existing DNX. Note that the version numbers in the example below might not match your current environment. You'll need to adjust them accordingly.

	C:\coreclr-demo>dir %USERPROFILE%\.dnx\runtimes
	 Volume in drive C has no label.
	 Volume Serial Number is F074-BCDF

	 Directory of C:\Users\rlander\.dnx\runtimes

	04/01/2015  04:45 PM    <DIR>          .
	04/01/2015  04:45 PM    <DIR>          ..
	04/01/2015  04:33 PM    <DIR>          dnx-clr-win-x86.1.0.0-beta5-11447
	04/01/2015  04:45 PM    <DIR>          dnx-coreclr-win-x64.1.0.0-beta5-11447
	               0 File(s)              0 bytes
	               4 Dir(s)  94,895,509,504 bytes free


Copy and rename the X64 CoreCLR DNX. That's the one you'll target. You'll be able to see it with `dnvm list`. This time, you will need to explicitly `use` this DNX in order to use it.

	C:\coreclr-demo>xcopy /E /S %USERPROFILE%\.dnx\runtimes\dnx-coreclr-win-x64.1.0.0-beta5-11469 %USERPROFILE%\.dnx\runtimes\dnx-coreclr-win-x64.1.0.0-beta5-custom

You now need to select the custom DNX as the use to 'use'. You can see the new DNX state with `dnvm list`.

	C:\coreclr-demo>dnvm use 1.0.0-beta5-custom -r coreclr -arch x64
	C:\coreclr-demo>dnvm list

	Active Version            Runtime Architecture Location
	------ -------            ------- ------------ --------
	       1.0.0-beta5-11447  clr     x86          C:\Users\rlander\.dnx\runtimes
	       1.0.0-beta5-11469  coreclr x64          C:\Users\rlander\.dnx\runtimes
	  *    1.0.0-beta5-custom coreclr x64          C:\Users\rlander\.dnx\runtimes

Try running the app again to validate that you have a working installation with the custom DNX.

	C:\coreclr-demo>dnx . run

Injecting a Custom CoreCLR into your Custom DNX
===============================================

Next, you need to copy your CoreCLR build into your custom DNX. The default build for CoreCLR is X64 debug, which will be slow. Unless you intend to debug CoreCLR itself, a release build is recommended.

	C:\git\coreclr>build Release
	C:\git\coreclr>copy /Y bin\Product\Windows_NT.x64.Release\*.dll %USERPROFILE%\.dnx\runtimes\dnx-coreclr-win-x64.1.0.0-beta5-custom\bin	

The custom CLR will invalidate all of the existing crossgen images. You need to delete them.

	C:\git\coreclr>del %USERPROFILE%\.dnx\runtimes\dnx-coreclr-win-x64.1.0.0-beta5-custom\bin\*.ni.dll

You should re-generate crossgen images for your DNX to provide the best performance with the following command. Sadly, crossgen won't work until the [Issue 227](https://github.com/dotnet/coreclr/issues/227) is resolved (so skip this command for now).

	C:\Users\rlander\.dnx\runtimes\dnx-coreclr-win-x64.1.0.0-beta5-custom\bin>k-crossgen.cmd

You should now have a working custom CoreCLR x64 DNX. Give it a try.

	C:\coreclr-demo>dnx . run
