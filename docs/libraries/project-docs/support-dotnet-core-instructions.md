# Making your libraries compatible with .NET Core and other .NET Platforms

Want to make your libraries multi-platform? Want to see how much work is required to make your application compatible with other .NET platforms? The [**.NET Portability Analyzer**](http://github.com/microsoft/dotnet-apiport) is a tool that provides you with a detailed report ([example report](http://dotnet.github.io/port-to-core/Moq4_ApiPortabilityAnalysis.htm)) on how portable your code is across .NET platforms by analyzing assemblies. The Portability Analyzer is offered as a Visual Studio Extension and as a console app.

## How to Use Portability Analyzer

To begin using the .NET Portability Analyzer, download the extension from the Visual Studio Gallery. You can configure it in Visual Studio via  *Tools* >> *Options* >> *.NET Portability Analyzer* and select your Target Platforms.

![](../images/portability_screenshot.png)

To analyze your entire project, right-click on your project in the Solution Explorer and select *Analyze* >> *Analyze Assembly Portability*. Otherwise, go to the Analyze menu and select *Analyze Assembly Portability*. From there, select your project's executable or .dll.

![](../images/portability_solution_explorer.png)

After running the analysis, you will see your .NET Portability Report. Only types that are unsupported by a target platform will appear in the list and you can review recommendations in the **Messages** tab in the **Error List**. You can also jump to problem areas directly from the **Messages** tab.

![](../images/portability_report.png)

Don't want to use Visual Studio? You can also use the Portability Analyzer from the Command Prompt. Download the command-line analyzer [here](http://github.com/microsoft/dotnet-apiport/releases).

- Type the following command to analyze the current directory: `ApiPort.exe analyze -f . `
- To analyze a specific list of .dlls type the following command: `ApiPort.exe analyze -f first.dll -f second.dll -f third.dll`

Your .NET Portability Report will be saved as an Excel .xlsx file in your current directory. The **Details** tab in the Excel Workbook will contain more info.

For more info on the .NET Portability Analyzer, read the [documentation](https://github.com/Microsoft/dotnet-apiport/blob/master/docs/HowTo/Introduction.md).
