// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Diagnostics
{
    /// <summary>Represents a.dll or .exe file that is loaded into a particular process.</summary>
    /// <remarks>A module is an executable file or a dynamic link library (DLL). Each process consists of one or more modules. You can use this class to get information about the module.
    /// <format type="text/markdown"><![CDATA[
    /// > [!IMPORTANT]
    /// >  This type implements the <xref:System.IDisposable> interface. When you have finished using the type, you should dispose of it either directly or indirectly. To dispose of the type directly, call its <xref:System.IDisposable.Dispose%2A> method in a `try`/`catch` block. To dispose of it indirectly, use a language construct such as `using` (in C#) or `Using` (in Visual Basic). For more information, see the "Using an Object that Implements IDisposable" section in the <xref:System.IDisposable> interface topic.
    /// ]]></format></remarks>
    /// <example>The following code sample demonstrates how to use the <see cref="System.Diagnostics.ProcessModule" /> class to get and display information about all the modules that are used by the Notepad.exe application.
    /// <format type="text/markdown"><![CDATA[
    /// [!code-cpp[ProcessModule#1](~/samples/snippets/cpp/VS_Snippets_CLR/ProcessModule/CPP/processmodule.cpp#1)]
    /// [!code-csharp[ProcessModule#1](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessModule/CS/processmodule.cs#1)]
    /// [!code-vb[ProcessModule#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessModule/VB/processmodule.vb#1)]
    /// ]]></format></example>
    [Designer("System.Diagnostics.Design.ProcessModuleDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProcessModule : Component
    {
        private FileVersionInfo? _fileVersionInfo;

        internal ProcessModule() { }

        /// <summary>Gets the name of the process module.</summary>
        /// <value>The name of the module.</value>
        /// <remarks>If the name is longer than the maximum number of characters allowed, it is truncated.</remarks>
        /// <example>The following code example creates a new process for the Notepad.exe application. The code iterates through the <see cref="System.Diagnostics.ProcessModuleCollection" /> class to obtain a <see cref="System.Diagnostics.ProcessModule" /> object for each module in the collection. The <see cref="System.Diagnostics.ProcessModule.ModuleName" /> property is used to display the name of each module.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-cpp[ProcessModule_ModuleName#1](~/samples/snippets/cpp/VS_Snippets_CLR/ProcessModule_ModuleName/CPP/processmodule_modulename.cpp#1)]
        /// [!code-csharp[ProcessModule_ModuleName#1](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessModule_ModuleName/CS/processmodule_modulename.cs#1)]
        /// [!code-vb[ProcessModule_ModuleName#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessModule_ModuleName/VB/processmodule_modulename.vb#1)]
        /// ]]></format></example>
        public string? ModuleName { get; internal set; }

        /// <summary>Gets the full path to the module.</summary>
        /// <value>The fully qualified path that defines the location of the module.</value>
        /// <remarks>If the file name is longer than the maximum number of characters allowed, the file name is truncated.</remarks>
        /// <example>The following code example creates a new process for the Notepad.exe application. The code iterates through the <see cref="System.Diagnostics.ProcessModuleCollection" /> class to obtain a <see cref="System.Diagnostics.ProcessModule" /> object for each module in the collection. The <see cref="System.Diagnostics.ProcessModule.ModuleName" /> and <see cref="System.Diagnostics.ProcessModule.FileName" /> properties are used to display the module name and the full path information for each module.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-cpp[ProcessModule_FileName#1](~/samples/snippets/cpp/VS_Snippets_CLR/ProcessModule_FileName/CPP/processmodule_filename.cpp#1)]
        /// [!code-csharp[ProcessModule_FileName#1](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessModule_FileName/CS/processmodule_filename.cs#1)]
        /// [!code-vb[ProcessModule_FileName#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessModule_FileName/VB/processmodule_filename.vb#1)]
        /// ]]></format></example>
        public string? FileName { get; internal set; }

        /// <summary>Gets the memory address where the module was loaded.</summary>
        /// <value>The load address of the module.</value>
        /// <remarks></remarks>
        /// <example>The following code example creates a new process for the Notepad.exe application. The code iterates through the <see cref="System.Diagnostics.ProcessModuleCollection" /> class to obtain a <see cref="System.Diagnostics.ProcessModule" /> object for each module in the collection. The <see cref="System.Diagnostics.ProcessModule.ModuleName" /> and  <see cref="System.Diagnostics.ProcessModule.BaseAddress" /> properties are used to display the module name and the memory address where each module was loaded.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-cpp[ProcessModule_BaseAddress#1](~/samples/snippets/cpp/VS_Snippets_CLR/ProcessModule_BaseAddress/CPP/processmodule_baseaddress.cpp#1)]
        /// [!code-csharp[ProcessModule_BaseAddress#1](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessModule_BaseAddress/CS/processmodule_baseaddress.cs#1)]
        /// [!code-vb[ProcessModule_BaseAddress#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessModule_BaseAddress/VB/processmodule_baseaddress.vb#1)]
        /// ]]></format></example>
        public IntPtr BaseAddress { get; internal set; }

        /// <summary>Gets the amount of memory that is required to load the module.</summary>
        /// <value>The size, in bytes, of the memory that the module occupies.</value>
        /// <remarks><see cref="System.Diagnostics.ProcessModule.ModuleMemorySize" /> does not include any additional memory allocations that the module makes once it is running; it includes only the size of the static code and data in the module file.</remarks>
        /// <example>The following code example creates a new process for the Notepad.exe application. The code iterates through the <see cref="System.Diagnostics.ProcessModuleCollection" /> class to obtain a <see cref="System.Diagnostics.ProcessModule" /> object for each module in the collection. The <see cref="System.Diagnostics.ProcessModule.ModuleName" /> and <see cref="System.Diagnostics.ProcessModule.ModuleMemorySize" /> properties are used to display the module name and the amount of memory needed for each module.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-cpp[ProcessModule_ModuleMemorySize#1](~/samples/snippets/cpp/VS_Snippets_CLR/ProcessModule_ModuleMemorySize/CPP/processmodule_modulememorysize.cpp#1)]
        /// [!code-csharp[ProcessModule_ModuleMemorySize#1](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessModule_ModuleMemorySize/CS/processmodule_modulememorysize.cs#1)]
        /// [!code-vb[ProcessModule_ModuleMemorySize#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessModule_ModuleMemorySize/VB/processmodule_modulememorysize.vb#1)]
        /// ]]></format></example>
        public int ModuleMemorySize { get; internal set; }

        /// <summary>Gets the memory address for the function that runs when the system loads and runs the module.</summary>
        /// <value>The entry point of the module.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The module's entry point is the location of the function that is called during process startup, thread startup, process shutdown, and thread shutdown. While the entry point is not the address of the DllMain function, it should be close enough for most purposes.
        /// > [!NOTE]
        /// >  Due to changes in the way that Windows loads assemblies, <xref:System.Diagnostics.ProcessModule.EntryPointAddress%2A> will always return 0 on [!INCLUDE[win8](~/includes/win8-md.md)] or [!INCLUDE[win81](~/includes/win81-md.md)] and should not be relied on for those platforms.
        /// ## Examples
        /// The following code example creates a new process for the Notepad.exe application. The code iterates through the <xref:System.Diagnostics.ProcessModuleCollection> class to obtain a <xref:System.Diagnostics.ProcessModule> object for each module in the collection. The <xref:System.Diagnostics.ProcessModule.ModuleName%2A> and <xref:System.Diagnostics.ProcessModule.EntryPointAddress%2A> properties are used to display the name and the entry point address for each module.
        /// [!code-cpp[ProcessModule_EntryPoint#1](~/samples/snippets/cpp/VS_Snippets_CLR/ProcessModule_EntryPoint/CPP/processmodule_entrypoint.cpp#1)]
        /// [!code-csharp[ProcessModule_EntryPoint#1](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessModule_EntryPoint/CS/processmodule_entrypoint.cs#1)]
        /// [!code-vb[ProcessModule_EntryPoint#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessModule_EntryPoint/VB/processmodule_entrypoint.vb#1)]
        /// ]]></format></remarks>
        public IntPtr EntryPointAddress { get; internal set; }

        /// <summary>Gets version information about the module.</summary>
        /// <value>A <see cref="System.Diagnostics.FileVersionInfo" /> that contains the module's version information.</value>
        /// <remarks></remarks>
        /// <example>The following code example creates a new process for the Notepad.exe application. The code iterates through the <see cref="System.Diagnostics.ProcessModuleCollection" /> class to obtain a <see cref="System.Diagnostics.ProcessModule" /> object for each module in the collection. The <see cref="System.Diagnostics.ProcessModule.ModuleName" /> and <see cref="System.Diagnostics.ProcessModule.FileVersionInfo" /> properties are used to display the module name and the file version information for each module.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-cpp[ProcessModule_FileVersionInfo#1](~/samples/snippets/cpp/VS_Snippets_CLR/ProcessModule_FileVersionInfo/CPP/processmodule_fileversioninfo.cpp#1)]
        /// [!code-csharp[ProcessModule_FileVersionInfo#1](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessModule_FileVersionInfo/CS/processmodule_fileversioninfo.cs#1)]
        /// [!code-vb[ProcessModule_FileVersionInfo#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessModule_FileVersionInfo/VB/processmodule_fileversioninfo.vb#1)]
        /// ]]></format></example>
        public FileVersionInfo FileVersionInfo => _fileVersionInfo ?? (_fileVersionInfo = FileVersionInfo.GetVersionInfo(FileName!));

        /// <summary>Converts the name of the module to a string.</summary>
        /// <returns>The value of the <see cref="System.Diagnostics.ProcessModule.ModuleName" /> property.</returns>
        /// <remarks></remarks>
        /// <example>The following code example creates a new process for the Notepad.exe application. The code iterates through the <see cref="System.Diagnostics.ProcessModuleCollection" /> class to obtain a <see cref="System.Diagnostics.ProcessModule" /> object for each module in the collection. The <see cref="System.Diagnostics.ProcessModule.ToString" /> method is used to display the name for each module.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-cpp[ProcessModule_ToString#1](~/samples/snippets/cpp/VS_Snippets_CLR/ProcessModule_ToString/CPP/processmodule_tostring.cpp#1)]
        /// [!code-csharp[ProcessModule_ToString#1](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessModule_ToString/CS/processmodule_tostring.cs#1)]
        /// [!code-vb[ProcessModule_ToString#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessModule_ToString/VB/processmodule_tostring.vb#1)]
        /// ]]></format></example>
        public override string ToString() => $"{base.ToString()} ({ModuleName})";
    }
}
