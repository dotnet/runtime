// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace System.Diagnostics
{
    /// <summary>Specifies a set of values that are used when you start a process.</summary>
    /// <remarks><see cref="System.Diagnostics.ProcessStartInfo" /> is used together with the <see cref="System.Diagnostics.Process" /> component. When you start a process using the <see cref="System.Diagnostics.Process" /> class, you have access to process information in addition to that available when attaching to a running process.
    /// You can use the <see cref="System.Diagnostics.ProcessStartInfo" /> class for better control over the process you start. You must at least set the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property, either manually or using the constructor. The file name is any application or document. Here a document is defined to be any file type that has an open or default action associated with it. You can view registered file types and their associated applications for your computer by using the **Folder Options** dialog box, which is available through the operating system. The **Advanced** button leads to a dialog box that shows whether there is an open action associated with a specific registered file type.
    /// In addition, you can set other properties that define actions to take with that file. You can specify a value specific to the type of the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property for the <see cref="System.Diagnostics.ProcessStartInfo.Verb" /> property. For example, you can specify "print" for a document type. Additionally, you can specify <see cref="System.Diagnostics.ProcessStartInfo.Arguments" /> property values to be command-line arguments to pass to the file's open procedure. For example, if you specify a text editor application in the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property, you can use the <see cref="System.Diagnostics.ProcessStartInfo.Arguments" /> property to specify a text file to be opened by the editor.
    /// Standard input is usually the keyboard, and standard output and standard error are usually the monitor screen. However, you can use the <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardInput" />, <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput" />, and <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError" /> properties to cause the process to get input from or return output to a file or other device. If you use the <see cref="System.Diagnostics.Process.StandardInput" />, <see cref="System.Diagnostics.Process.StandardOutput" />, or <see cref="System.Diagnostics.Process.StandardError" /> properties on the <see cref="System.Diagnostics.Process" /> component, you must first set the corresponding value on the <see cref="System.Diagnostics.ProcessStartInfo" /> property. Otherwise, the system throws an exception when you read or write to the stream.
    /// Set the <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property to specify whether to start the process by using the operating system shell. If <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is set to <see langword="false" />, the new process inherits the standard input, standard output, and standard error streams of the calling process, unless the <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardInput" />, <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput" />, or <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError" /> properties, respectively, are set to <see langword="true" />.
    /// You can change the value of any <see cref="System.Diagnostics.ProcessStartInfo" /> property up to the time that the process starts. After you start the process, changing these values has no effect.
    /// <format type="text/markdown"><![CDATA[
    /// > [!NOTE]
    /// >  This class contains a link demand at the class level that applies to all members. A <xref:System.Security.SecurityException> is thrown when the immediate caller does not have full-trust permission. For details about security demands, see [Link Demands](/dotnet/framework/misc/link-demands).
    /// ]]></format></remarks>
    /// <example>The following code example demonstrates how to use the <see cref="System.Diagnostics.ProcessStartInfo" /> class to start Internet Explorer, providing the destination URLs as <see cref="System.Diagnostics.ProcessStartInfo" /> arguments.
    /// <format type="text/markdown"><![CDATA[
    /// [!code-cpp[Process.Start_static#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_static/CPP/processstartstatic.cpp)]
    /// [!code-csharp[Process.Start_static#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_static/CS/processstartstatic.cs)]
    /// [!code-vb[Process.Start_static#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_static/VB/processstartstatic.vb)]
    /// ]]></format></example>
    /// <altmember cref="System.Diagnostics.Process"/>
    [DebuggerDisplay("FileName={FileName}, Arguments={BuildArguments()}, WorkingDirectory={WorkingDirectory}")]
    public sealed partial class ProcessStartInfo
    {
        private string? _fileName;
        private string? _arguments;
        private string? _directory;
        private string? _userName;
        private string? _verb;
        private Collection<string>? _argumentList;
        private ProcessWindowStyle _windowStyle;

        internal DictionaryWrapper? _environmentVariables;

        /// <summary>Initializes a new instance of the <see cref="System.Diagnostics.ProcessStartInfo" /> class without specifying a file name with which to start the process.</summary>
        /// <remarks>You must set at least the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property before you start the process. The file name is any application or document. In this case, a document is defined to be any file type that has an open or default action associated with it. You can view registered file types and their associated applications for your computer by using the **Folder Options** dialog box, which is available through the operating system. The **Advanced** button leads to a dialog box that shows whether there is an open action associated with a specific registered file type.
        /// Optionally, you can also set other properties before you start the process. The <see cref="System.Diagnostics.ProcessStartInfo.Verb" /> property supplies actions to take, such as "print", with the file indicated in the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property. The <see cref="System.Diagnostics.ProcessStartInfo.Arguments" /> property supplies a way to pass command-line arguments to the file when the system opens it.</remarks>
        public ProcessStartInfo()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="System.Diagnostics.ProcessStartInfo" /> class and specifies a file name such as an application or document with which to start the process.</summary>
        /// <param name="fileName">An application or document with which to start a process.</param>
        /// <remarks>The file name is any application or document. In this case, a document is defined to be any file type that has an open or default action associated with it. You can view registered file types and their associated applications for your computer by using the **Folder Options** dialog box, which is available through the operating system. The **Advanced** button leads to a dialog box that shows whether there is an open action associated with a specific registered file type.
        /// You can change the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property after you call this constructor, up to the time that the process starts. After you start the process, changing these values has no effect.</remarks>
        public ProcessStartInfo(string fileName)
        {
            _fileName = fileName;
        }

        /// <summary>Initializes a new instance of the <see cref="System.Diagnostics.ProcessStartInfo" /> class, specifies an application file name with which to start the process, and specifies a set of command-line arguments to pass to the application.</summary>
        /// <param name="fileName">An application with which to start a process.</param>
        /// <param name="arguments">Command-line arguments to pass to the application when the process starts.</param>
        /// <remarks>The file name is any application or document. In this case, a document is defined to be any file type that has an open or default action associated with it. You can view registered file types and their associated applications for your computer by using the **Folder Options** dialog box, which is available through the operating system. The **Advanced** button leads to a dialog box that shows whether there is an open action associated with a specific registered file type.
        /// You can change the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> or <see cref="System.Diagnostics.ProcessStartInfo.Arguments" /> properties after you call this constructor, up to the time that the process starts. After you start the process, changing these values has no effect.</remarks>
        public ProcessStartInfo(string fileName, string arguments)
        {
            _fileName = fileName;
            _arguments = arguments;
        }

        /// <summary>Gets or sets the set of command-line arguments to use when starting the application.</summary>
        /// <value>A single string containing the arguments to pass to the target application specified in the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property. The default is an empty string ("").</value>
        /// <remarks>The length of the string assigned to the `Arguments` property must be less than 32,699.
        /// Arguments are parsed and interpreted by the target application, so must align with the expectations of that application. For .NET applications as demonstrated in the Examples below, spaces are interpreted as a separator between multiple arguments. A single argument that includes spaces must be surrounded by quotation marks, but those quotation marks are not carried through to the target application. To include quotation marks in the final parsed argument, triple-escape each mark.
        /// If you use this property to set command-line arguments, <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList" /> must not contain any elements.
        /// `Arguments` and <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList" />, which is supported starting with .NET Core 2.1 and .NET Standard 2.1, are independent of one another. That is, the string assigned to the `Arguments` property does not populate the <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList" /> collection, and the members of the <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList" /> collection are not assigned to the `Arguments` property.</remarks>
        /// <example>The first example creates a small application (argsecho.exe) that echos its arguments to the console. The second example creates an application that invokes argsecho.exe to demonstrate different variations for the `Arguments` property.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-cpp[Process.Start_static#3](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_static/CPP/processstartstatic3.cpp)]
        /// [!code-csharp[Process.Start_static#3](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_static/CS/processstartstatic3.cs)]
        /// [!code-vb[Process.Start_static#3](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_static/VB/processstartstatic3.vb)]
        /// ]]></format>
        /// <format type="text/markdown"><![CDATA[
        /// [!code-cpp[Process.Start_static#2](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_static/CPP/processstartstatic2.cpp)]
        /// [!code-csharp[Process.Start_static#2](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_static/CS/processstartstatic2.cs)]
        /// [!code-vb[Process.Start_static#2](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_static/VB/processstartstatic2.vb)]
        /// ]]></format></example>
        public string Arguments
        {
            get => _arguments ?? string.Empty;
            set => _arguments = value;
        }

        /// <summary>Gets a collection of command-line arguments to use when starting the application. Strings added to the list don't need to be previously escaped.</summary>
        /// <value>A collection of command-line arguments.</value>
        /// <remarks>The <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList" /> and <see cref="System.Diagnostics.ProcessStartInfo.Arguments" /> properties are independent of one another and <b>only one of them can be used at the same time</b>. The main difference between both APIs is that <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList" /> takes care of escaping the provided arguments and <b>internally</b> builds a single string that is passed to operating system when calling <see cref="System.Diagnostics.Process.Start(ProcessStartInfo)" />. So if you are not sure how to properly escape your arguments, you should choose <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList" /> over <see cref="System.Diagnostics.ProcessStartInfo.Arguments" />.
        /// </remarks>
        /// <example>This example adds three arguments to the process start info.
        /// <code class="lang-csharp">
        /// var info = new System.Diagnostics.ProcessStartInfo("cmd.exe");
        /// info.ArgumentList.Add("/c");
        /// info.ArgumentList.Add("dir");
        /// info.ArgumentList.Add(@"C:\Program Files\dotnet"); // there is no need to escape the space, the API takes care of it
        /// // or if you prefer collection property initializer syntax:
        /// var info = new System.Diagnostics.ProcessStartInfo("cmd.exe")
        /// {
        /// ArgumentList = {
        /// "/c",
        /// "dir",
        /// @"C:\Program Files\dotnet"
        /// }
        /// };
        /// // The corresponding assignment to the Arguments property is:
        /// var info = new System.Diagnostics.ProcessStartInfo("cmd.exe")
        /// {
        /// Arguments = "/c dir \"C:\\Program Files\\dotnet\""
        /// };
        /// </code>
        /// <code class="lang-vb">
        /// Dim info As New System.Diagnostics.ProcessStartInfo("cmd.exe")
        /// info.ArgumentList.Add("/c")
        /// info.ArgumentList.Add("dir")
        /// info.ArgumentList.Add("C:\Program Files\dotnet")
        /// ' The corresponding assignment to the Arguments property is:
        /// info.Arguments = "/c dir ""C:\Program Files\dotnet"""
        /// </code>
        /// </example>
        public Collection<string> ArgumentList => _argumentList ??= new Collection<string>();

        internal bool HasArgumentList => _argumentList is not null && _argumentList.Count != 0;

        /// <summary>Gets or sets a value indicating whether to start the process in a new window.</summary>
        /// <value><see langword="true" /> if the process should be started without creating a new window to contain it; otherwise, <see langword="false" />. The default is <see langword="false" />.</value>
        /// <remarks>If the <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property is <see langword="true" /> or the <see cref="System.Diagnostics.ProcessStartInfo.UserName" /> and <see cref="System.Diagnostics.ProcessStartInfo.Password" /> properties are not <see langword="null" />, the <see cref="System.Diagnostics.ProcessStartInfo.CreateNoWindow" /> property value is ignored and a new window is created.
        /// .NET Core does not support creating windows directly on Unix-like platforms, including macOS and Linux. This property is ignored on such platforms.</remarks>
        /// <example><format type="text/markdown"><![CDATA[
        /// [!code-cpp[Process.Start_instance#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_instance/CPP/processstart.cpp#1)]
        /// [!code-csharp[Process.Start_instance#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_instance/CS/processstart.cs#1)]
        /// [!code-vb[Process.Start_instance#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_instance/VB/processstart.vb#1)]
        /// ]]></format></example>
        public bool CreateNoWindow { get; set; }

        /// <summary>Gets search paths for files, directories for temporary files, application-specific options, and other similar information.</summary>
        /// <value>A string dictionary that provides environment variables that apply to this process and child processes. The default is <see langword="null" />.</value>
        /// <remarks>Although you cannot set the <see cref="System.Diagnostics.ProcessStartInfo.EnvironmentVariables" /> property, you can modify the <see cref="System.Collections.Specialized.StringDictionary" /> returned by the property. For example, the following code adds a TempPath environment variable: `myProcess.StartInfo.EnvironmentVariables.Add("TempPath", "C:\\Temp")`.  You must set the <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property to <see langword="false" /> to start the process after changing the <see cref="System.Diagnostics.ProcessStartInfo.EnvironmentVariables" /> property. If <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="true" />, an <see cref="System.InvalidOperationException" /> is thrown when the <see cref="O:System.Diagnostics.Process.Start" /> method is called.</remarks>
        [Editor("System.Diagnostics.Design.StringDictionaryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public StringDictionary EnvironmentVariables => new StringDictionaryWrapper((Environment as DictionaryWrapper)!);

        /// <summary>Gets the environment variables that apply to this process and its child processes.</summary>
        /// <value>A generic dictionary containing the environment variables that apply to this process and its child processes. The default is <see langword="null" />.</value>
        /// <remarks>The environment variables contain search paths for files, directories for temporary files, application-specific options, and other similar information. Although you cannot directly set the <see cref="System.Diagnostics.ProcessStartInfo.Environment" /> property, you can modify the generic dictionary returned by the property. For example, the following code adds a TempPath environment variable: `myProcess.StartInfo.Environment.Add("TempPath", "C:\\Temp")`.  You must set the <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property to <see langword="false" /> to start the process after changing the <see cref="System.Diagnostics.ProcessStartInfo.Environment" /> property. If <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="true" />, an <see cref="System.InvalidOperationException" /> is thrown when the <see cref="O:System.Diagnostics.Process.Start" /> method is called.
        /// On .NET Framework applications, using the <see cref="System.Diagnostics.ProcessStartInfo.Environment" /> property is the same as using the <see cref="System.Diagnostics.ProcessStartInfo.EnvironmentVariables" /> property.</remarks>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.EnvironmentVariables"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.UseShellExecute"/>
        /// <altmember cref="O:System.Diagnostics.Process.Start"/>
        public IDictionary<string, string?> Environment
        {
            get
            {
                if (_environmentVariables == null)
                {
                    IDictionary envVars = System.Environment.GetEnvironmentVariables();

                    _environmentVariables = new DictionaryWrapper(new Dictionary<string, string?>(
                        envVars.Count,
                        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal));

                    // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                    IDictionaryEnumerator e = envVars.GetEnumerator();
                    Debug.Assert(!(e is IDisposable), "Environment.GetEnvironmentVariables should not be IDisposable.");
                    while (e.MoveNext())
                    {
                        DictionaryEntry entry = e.Entry;
                        _environmentVariables.Add((string)entry.Key, (string?)entry.Value);
                    }
                }
                return _environmentVariables;
            }
        }

        /// <summary>Gets or sets a value indicating whether the input for an application is read from the <see cref="System.Diagnostics.Process.StandardInput" /> stream.</summary>
        /// <value><see langword="true" /> if input should be read from <see cref="System.Diagnostics.Process.StandardInput" />; otherwise, <see langword="false" />. The default is <see langword="false" />.</value>
        /// <remarks>A <see cref="System.Diagnostics.Process" /> can read input text from its standard input stream, typically the keyboard. By redirecting the <see cref="System.Diagnostics.Process.StandardInput" /> stream, you can programmatically specify the input of a process. For example, instead of using keyboard input, you can provide text from the contents of a designated file or output from another application.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  You must set <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> to `false` if you want to set <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardInput%2A> to `true`. Otherwise, writing to the <xref:System.Diagnostics.Process.StandardInput%2A> stream throws an exception.
        /// ]]></format></remarks>
        /// <example>The following example illustrates how to redirect the <see cref="System.Diagnostics.Process.StandardInput" /> stream of a process. The `sort` command is a console application that reads and sorts text input.
        /// The example starts the `sort` command with redirected input. It then prompts the user for text, and passes the text to the `sort` process through the redirected <see cref="System.Diagnostics.Process.StandardInput" /> stream. The `sort` results are displayed to the user on the console.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-cpp[Process_StandardInput#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process_StandardInput/CPP/process_standardinput.cpp#1)]
        /// [!code-csharp[Process_StandardInput#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process_StandardInput/CS/process_standardinput.cs#1)]
        /// [!code-vb[Process_StandardInput#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process_StandardInput/VB/process_standardinput.vb#1)]
        /// ]]></format></example>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.UseShellExecute"/>
        /// <altmember cref="System.Diagnostics.Process.StandardInput"/>
        public bool RedirectStandardInput { get; set; }
        /// <summary>Gets or sets a value that indicates whether the textual output of an application is written to the <see cref="System.Diagnostics.Process.StandardOutput" /> stream.</summary>
        /// <value><see langword="true" /> if output should be written to <see cref="System.Diagnostics.Process.StandardOutput" />; otherwise, <see langword="false" />. The default is <see langword="false" />.</value>
        /// <remarks>When a <see cref="System.Diagnostics.Process" /> writes text to its standard stream, that text is typically displayed on the console. By setting <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput" /> to <see langword="true" /> to redirect the <see cref="System.Diagnostics.Process.StandardOutput" /> stream, you can manipulate or suppress the output of a process. For example, you can filter the text, format it differently, or write the output to both the console and a designated log file.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  You must set <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> to `false` if you want to set <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardOutput%2A> to `true`. Otherwise, reading from the <xref:System.Diagnostics.Process.StandardOutput%2A> stream throws an exception.
        /// ]]></format>
        /// The redirected <see cref="System.Diagnostics.Process.StandardOutput" /> stream can be read synchronously or asynchronously. Methods such as <see cref="O:System.IO.StreamReader.Read" />, <see cref="System.IO.StreamReader.ReadLine" />, and <see cref="System.IO.StreamReader.ReadToEnd" /> perform synchronous read operations on the output stream of the process. These synchronous read operations do not complete until the associated <see cref="System.Diagnostics.Process" /> writes to its <see cref="System.Diagnostics.Process.StandardOutput" /> stream, or closes the stream.
        /// In contrast, <see cref="System.Diagnostics.Process.BeginOutputReadLine" /> starts asynchronous read operations on the <see cref="System.Diagnostics.Process.StandardOutput" /> stream. This method enables a designated event handler (see <see cref="System.Diagnostics.Process.OutputDataReceived" />) for the stream output and immediately returns to the caller, which can perform other work while the stream output is directed to the event handler.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  The application that is processing the asynchronous output should call the <xref:System.Diagnostics.Process.WaitForExit%2A> method to ensure that the output buffer has been flushed.
        /// ]]></format>
        /// Synchronous read operations introduce a dependency between the caller reading from the <see cref="System.Diagnostics.Process.StandardOutput" /> stream and the child process writing to that stream. These dependencies can cause deadlock conditions. When the caller reads from the redirected stream of a child process, it is dependent on the child. The caller waits for the read operation until the child writes to the stream or closes the stream. When the child process writes enough data to fill its redirected stream, it is dependent on the parent. The child process waits for the next write operation until the parent reads from the full stream or closes the stream. The deadlock condition results when the caller and child process wait for each other to complete an operation, and neither can continue. You can avoid deadlocks by evaluating dependencies between the caller and child process.
        /// The last two examples in this section use the <see cref="O:System.Diagnostics.Process.Start" /> method to launch an executable named *Write500Lines.exe*. The following example contains its source code.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[Executable launched by Process.Start](~/samples/snippets/csharp/api/system.diagnostics/process/standardoutput/write500lines.cs)]
        /// [!code-vb[Executable launched by Process.Start](~/samples/snippets/visualbasic/api/system.diagnostics/process/standardoutput/write500lines.vb)]
        /// ]]></format>
        /// The following example shows how to read from a redirected stream and wait for the child process to exit. The example avoids a deadlock condition by calling `p.StandardOutput.ReadToEnd` before `p.WaitForExit`. A deadlock condition can result if the parent process calls `p.WaitForExit` before `p.StandardOutput.ReadToEnd` and the child process writes enough text to fill the redirected stream. The parent process would wait indefinitely for the child process to exit. The child process would wait indefinitely for the parent to read from the full <see cref="System.Diagnostics.Process.StandardOutput" /> stream.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[Reading synchronously from a redirected output stream](~/samples/snippets/csharp/api/system.diagnostics/process/standardoutput/stdoutput-sync.cs)]
        /// [!code-vb[Reading synchronously from a redirected output stream](~/samples/snippets/visualbasic/api/system.diagnostics/process/standardoutput/stdoutput-sync.vb)]
        /// ]]></format>
        /// There is a similar issue when you read all text from both the standard output and standard error streams. The following example performs a read operation on both streams. It avoids the deadlock condition by performing asynchronous read operations on the <see cref="System.Diagnostics.Process.StandardError" /> stream. A deadlock condition results if the parent process calls `p.StandardOutput.ReadToEnd` followed by `p.StandardError.ReadToEnd` and the child process writes enough text to fill its error stream. The parent process would wait indefinitely for the child process to close its <see cref="System.Diagnostics.Process.StandardOutput" /> stream. The child process would wait indefinitely for the parent to read from the full <see cref="System.Diagnostics.Process.StandardError" /> stream.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[Reading from a redirected output and error stream](~/samples/snippets/csharp/api/system.diagnostics/process/standardoutput/stdoutput-async.cs)]
        /// [!code-vb[Reading from a redirected output and error stream](~/samples/snippets/visualbasic/api/system.diagnostics/process/standardoutput/stdoutput-async.vb)]
        /// ]]></format>
        /// You can use asynchronous read operations to avoid these dependencies and their deadlock potential. Alternately, you can avoid the deadlock condition by creating two threads and reading the output of each stream on a separate thread.</remarks>
        /// <example><format type="text/markdown"><![CDATA[
        /// [!code-cpp[ProcessOneStream#1](~/samples/snippets/cpp/VS_Snippets_CLR/ProcessOneStream/CPP/stdstr.cpp#1)]
        /// [!code-csharp[ProcessOneStream#1](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessOneStream/CS/stdstr.cs#1)]
        /// [!code-vb[ProcessOneStream#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessOneStream/VB/stdstr.vb#1)]
        /// ]]></format></example>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.UseShellExecute"/>
        /// <altmember cref="System.Diagnostics.Process.StandardOutput"/>
        public bool RedirectStandardOutput { get; set; }
        /// <summary>Gets or sets a value that indicates whether the error output of an application is written to the <see cref="System.Diagnostics.Process.StandardError" /> stream.</summary>
        /// <value><see langword="true" /> if error output should be written to <see cref="System.Diagnostics.Process.StandardError" />; otherwise, <see langword="false" />. The default is <see langword="false" />.</value>
        /// <remarks>When a <see cref="System.Diagnostics.Process" /> writes text to its standard error stream, that text is typically displayed on the console. By redirecting the <see cref="System.Diagnostics.Process.StandardError" /> stream, you can manipulate or suppress the error output of a process. For example, you can filter the text, format it differently, or write the output to both the console and a designated log file.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  You must set <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> to `false` if you want to set <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardError%2A> to `true`. Otherwise, reading from the <xref:System.Diagnostics.Process.StandardError%2A> stream throws an exception.
        /// ]]></format>
        /// The redirected <see cref="System.Diagnostics.Process.StandardError" /> stream can be read synchronously or asynchronously. Methods such as <see cref="O:System.IO.StreamReader.Read" />, <see cref="System.IO.StreamReader.ReadLine" /> and <see cref="System.IO.StreamReader.ReadToEnd" /> perform synchronous read operations on the error output stream of the process. These synchronous read operations do not complete until the associated <see cref="System.Diagnostics.Process" /> writes to its <see cref="System.Diagnostics.Process.StandardError" /> stream, or closes the stream.
        /// In contrast, <see cref="System.Diagnostics.Process.BeginErrorReadLine" /> starts asynchronous read operations on the <see cref="System.Diagnostics.Process.StandardError" /> stream. This method enables a designated event handler for the stream output and immediately returns to the caller, which can perform other work while the stream output is directed to the event handler.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  The application that is processing the asynchronous output should call the <xref:System.Diagnostics.Process.WaitForExit%2A?displayProperty=nameWithType> method to ensure that the output buffer has been flushed.
        /// ]]></format>
        /// Synchronous read operations introduce a dependency between the caller reading from the <see cref="System.Diagnostics.Process.StandardError" /> stream and the child process writing to that stream. These dependencies can cause deadlock conditions. When the caller reads from the redirected stream of a child process, it is dependent on the child. The caller waits for the read operation until the child writes to the stream or closes the stream. When the child process writes enough data to fill its redirected stream, it is dependent on the parent. The child process waits for the next write operation until the parent reads from the full stream or closes the stream. The deadlock condition results when the caller and child process wait for each other to complete an operation, and neither can continue. You can avoid deadlocks by evaluating dependencies between the caller and child process.
        /// The last two examples in this section use the <see cref="O:System.Diagnostics.Process.Start" /> method to launch an executable named *Write500Lines.exe*. The following example contains its source code.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[Executable launched by Process.Start](~/samples/snippets/csharp/api/system.diagnostics/process/standardoutput/write500lines.cs)]
        /// [!code-vb[Executable launched by Process.Start](~/samples/snippets/visualbasic/api/system.diagnostics/process/standardoutput/write500lines.vb)]
        /// ]]></format>
        /// The following example shows how to read from a redirected error stream and wait for the child process to exit. It avoids a deadlock condition by calling `p.StandardError.ReadToEnd` before `p.WaitForExit`. A deadlock condition can result if the parent process calls `p.WaitForExit` before `p.StandardError.ReadToEnd` and the child process writes enough text to fill the redirected stream. The parent process would wait indefinitely for the child process to exit. The child process would wait indefinitely for the parent to read from the full <see cref="System.Diagnostics.Process.StandardError" /> stream.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[Reading from the error stream](~/samples/snippets/csharp/api/system.diagnostics/process/standarderror/stderror-sync.cs)]
        /// [!code-vb[Reading from the error stream](~/samples/snippets/visualbasic/api/system.diagnostics/process/standarderror/stderror-sync.vb)]
        /// ]]></format>
        /// There is a similar issue when you read all text from both the standard output and standard error streams. The following C# code, for example, performs a read operation on both streams. It avoids the deadlock condition by performing asynchronous read operations on the <see cref="System.Diagnostics.Process.StandardError" /> stream. A deadlock condition results if the parent process calls `p.StandardOutput.ReadToEnd` followed by `p.StandardError.ReadToEnd` and the child process writes enough text to fill its error stream. The parent process would wait indefinitely for the child process to close its <see cref="System.Diagnostics.Process.StandardOutput" /> stream. The child process would wait indefinitely for the parent to read from the full <see cref="System.Diagnostics.Process.StandardError" /> stream.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[Reading from both streams](~/samples/snippets/csharp/api/system.diagnostics/process/standardoutput/stdoutput-async.cs)]
        /// [!code-vb[Reading from both streams](~/samples/snippets/visualbasic/api/system.diagnostics/process/standardoutput/stdoutput-async.vb)]
        /// ]]></format>
        /// You can use asynchronous read operations to avoid these dependencies and their deadlock potential. Alternately, you can avoid the deadlock condition by creating two threads and reading the output of each stream on a separate thread.</remarks>
        /// <example>The following example uses the `net use` command together with a user-supplied argument to map a network resource. It then reads the standard error stream of the net command and writes it to console.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-cpp[Process_StandardError#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process_StandardError/CPP/source.cpp#1)]
        /// [!code-csharp[Process_StandardError#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process_StandardError/CS/source.cs#1)]
        /// [!code-vb[Process_StandardError#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process_StandardError/VB/source.vb#1)]
        /// ]]></format></example>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.UseShellExecute"/>
        /// <altmember cref="System.Diagnostics.Process.StandardError"/>
        public bool RedirectStandardError { get; set; }

        /// <summary>Gets or sets the preferred encoding for standard input.</summary>
        /// <value>An object that represents the preferred encoding for standard input. The default is <see langword="null" />.</value>
        /// <remarks>If the value of the `StandardInputEncoding` property is <see langword="null" />, the process uses the default standard input encoding for the standard input. The `StandardInputEncoding` property must be set before the process is started. Setting this property does not guarantee that the process will use the specified encoding. The application should be tested to determine which encodings the process supports.</remarks>
        public Encoding? StandardInputEncoding { get; set; }

        /// <summary>Gets or sets the preferred encoding for error output.</summary>
        /// <value>An object that represents the preferred encoding for error output. The default is <see langword="null" />.</value>
        /// <remarks>If the value of the <see cref="System.Diagnostics.ProcessStartInfo.StandardErrorEncoding" /> property is <see langword="null" />, the process uses the default standard error encoding for error output. The <see cref="System.Diagnostics.ProcessStartInfo.StandardErrorEncoding" /> property must be set before the process is started. Setting this property does not guarantee that the process will use the specified encoding; the process will use only those encodings that it supports. The application should be tested to determine which encodings are supported.</remarks>
        public Encoding? StandardErrorEncoding { get; set; }

        /// <summary>Gets or sets the preferred encoding for standard output.</summary>
        /// <value>An object that represents the preferred encoding for standard output. The default is <see langword="null" />.</value>
        /// <remarks>If the value of the <see cref="System.Diagnostics.ProcessStartInfo.StandardOutputEncoding" /> property is <see langword="null" />, the process uses the default standard output encoding for the standard output. The <see cref="System.Diagnostics.ProcessStartInfo.StandardOutputEncoding" /> property must be set before the process is started. Setting this property does not guarantee that the process will use the specified encoding. The application should be tested to determine which encodings the process supports.</remarks>
        public Encoding? StandardOutputEncoding { get; set; }

        /// <summary>Gets or sets the application or document to start.</summary>
        /// <value>The name of the application to start, or the name of a document of a file type that is associated with an application and that has a default open action available to it. The default is an empty string ("").</value>
        /// <remarks>You must set at least the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property before you start the process. The file name is any application or document. A document is defined to be any file type that has an open or default action associated with it. You can view registered file types and their associated applications for your computer by using the **Folder Options** dialog box, which is available through the operating system. The **Advanced** button leads to a dialog box that shows whether there is an open action associated with a specific registered file type.
        /// The set of file types available to you depends in part on the value of the <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property. If <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="true" />, you can start any document and perform operations on the file, such as printing, with the <see cref="System.Diagnostics.Process" /> component. When <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="false" />, you can start only executables with the <see cref="System.Diagnostics.Process" /> component.
        /// You can start a ClickOnce application by setting the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property to the location (for example, a Web address) from which you originally installed the application. Do not start a ClickOnce application by specifying its installed location on your hard disk.</remarks>
        /// <example><format type="text/markdown"><![CDATA[
        /// [!code-cpp[Process.Start_instance#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_instance/CPP/processstart.cpp#1)]
        /// [!code-csharp[Process.Start_instance#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_instance/CS/processstart.cs#1)]
        /// [!code-vb[Process.Start_instance#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_instance/VB/processstart.vb#1)]
        /// ]]></format></example>
        [Editor("System.Diagnostics.Design.StartFileNameEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string FileName
        {
            get => _fileName ?? string.Empty;
            set => _fileName = value;
        }

        /// <summary>When the <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property is <see langword="false" />, gets or sets the working directory for the process to be started. When <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="true" />, gets or sets the directory that contains the process to be started.</summary>
        /// <value>When <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="true" />, the fully qualified name of the directory that contains the process to be started. When the <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property is <see langword="false" />, the working directory for the process to be started. The default is an empty string ("").</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!IMPORTANT]
        /// >  The <xref:System.Diagnostics.ProcessStartInfo.WorkingDirectory%2A> property must be set if <xref:System.Diagnostics.ProcessStartInfo.UserName%2A> and <xref:System.Diagnostics.ProcessStartInfo.Password%2A> are provided. If the property is not set, the default working directory is %SYSTEMROOT%\system32.
        /// ]]></format>
        /// If the directory is already part of the system path variable, you do not have to repeat the directory's location in this property.
        /// The <see cref="System.Diagnostics.ProcessStartInfo.WorkingDirectory" /> property behaves differently when <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="true" /> than when <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="false" />. When <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="true" />, the <see cref="System.Diagnostics.ProcessStartInfo.WorkingDirectory" /> property specifies the location of the executable. If <see cref="System.Diagnostics.ProcessStartInfo.WorkingDirectory" /> is an empty string, the current directory is understood to contain the executable.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  When <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> is `true`, the working directory of the application that starts the executable is also the working directory of the executable.
        /// ]]></format>
        /// When <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="false" />, the <see cref="System.Diagnostics.ProcessStartInfo.WorkingDirectory" /> property is not used to find the executable. Instead, its value applies to the process that is started and only has meaning within the context of the new process.</remarks>
        [Editor("System.Diagnostics.Design.WorkingDirectoryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string WorkingDirectory
        {
            get => _directory ?? string.Empty;
            set => _directory = value;
        }

        /// <summary>Gets or sets a value indicating whether an error dialog box is displayed to the user if the process cannot be started.</summary>
        /// <value><see langword="true" /> if an error dialog box should be displayed on the screen if the process cannot be started; otherwise, <see langword="false" />. The default is <see langword="false" />.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> must be `true` if you want to set <xref:System.Diagnostics.ProcessStartInfo.ErrorDialog%2A> to `true`.
        /// ]]></format></remarks>
        public bool ErrorDialog { get; set; }
        /// <summary>Gets or sets the window handle to use when an error dialog box is shown for a process that cannot be started.</summary>
        /// <value>A pointer to the handle of the error dialog box that results from a process start failure.</value>
        /// <remarks>If <see cref="System.Diagnostics.ProcessStartInfo.ErrorDialog" /> is <see langword="true" />, the <see cref="System.Diagnostics.ProcessStartInfo.ErrorDialogParentHandle" /> property specifies the parent window for the dialog box that is shown. It is useful to specify a parent to keep the dialog box in front of the application.</remarks>
        public IntPtr ErrorDialogParentHandle { get; set; }

        /// <summary>Gets or sets the user name to use when starting the process. If you use the UPN format, <c>user</c>@<c>DNS_domain_name</c>, the <see cref="System.Diagnostics.ProcessStartInfo.Domain" /> property must be <see langword="null" />.</summary>
        /// <value>The user name to use when starting the process. If you use the UPN format, <c>user</c>@<c>DNS_domain_name</c>, the <see cref="System.Diagnostics.ProcessStartInfo.Domain" /> property must be <see langword="null" />.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!IMPORTANT]
        /// >  The <xref:System.Diagnostics.ProcessStartInfo.WorkingDirectory%2A> property must be set if <xref:System.Diagnostics.ProcessStartInfo.UserName%2A> and <xref:System.Diagnostics.ProcessStartInfo.Password%2A> are provided. If the property is not set, the default working directory is %SYSTEMROOT%\system32.
        /// ]]></format>
        /// If the <see cref="System.Diagnostics.ProcessStartInfo.UserName" /> property is not <see langword="null" /> or an empty string, the <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property must be <see langword="false" />, or an <see cref="System.InvalidOperationException" /> will be thrown when the <see cref="System.Diagnostics.Process.Start(System.Diagnostics.ProcessStartInfo)" /> method is called.</remarks>
        public string UserName
        {
            get => _userName ?? string.Empty;
            set => _userName = value;
        }

        /// <summary>Gets or sets the verb to use when opening the application or document specified by the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property.</summary>
        /// <value>The action to take with the file that the process opens. The default is an empty string (""), which signifies no action.</value>
        /// <remarks>Each file name extension has its own set of verbs, which can be obtained by using the <see cref="System.Diagnostics.ProcessStartInfo.Verbs" /> property. For example, the "`print`" verb will print a document specified by using <see cref="System.Diagnostics.ProcessStartInfo.FileName" />. The default verb can be specified by using an empty string (""). Examples of verbs are "Edit", "Open", "OpenAsReadOnly", "Print", and "Printto". You should use only verbs that appear in the set of verbs returned by the <see cref="System.Diagnostics.ProcessStartInfo.Verbs" /> property.
        /// When you use the <see cref="System.Diagnostics.ProcessStartInfo.Verb" /> property, you must include the file name extension when you set the value of the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property. The file name does not need to have an extension if you manually enter a value for the <see cref="System.Diagnostics.ProcessStartInfo.Verb" /> property.</remarks>
        /// <example>The following code example starts a new process by using the specified verb and file name. This code example is part of a larger example provided for the <see cref="System.Diagnostics.ProcessStartInfo.Verbs" /> property.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[ProcessVerbs_Diagnostics#4](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessVerbs_Diagnostics/CS/source.cs#4)]
        /// [!code-vb[ProcessVerbs_Diagnostics#4](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessVerbs_Diagnostics/VB/source.vb#4)]
        /// ]]></format></example>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.Verbs"/>
        [DefaultValue("")]
        public string Verb
        {
            get => _verb ?? string.Empty;
            set => _verb = value;
        }

        /// <summary>Gets or sets the window state to use when the process is started.</summary>
        /// <value>One of the enumeration values that indicates whether the process is started in a window that is maximized, minimized, normal (neither maximized nor minimized), or not visible. The default is <see langword="Normal" />.</value>
        /// <remarks></remarks>
        /// <example><format type="text/markdown"><![CDATA[
        /// [!code-cpp[Process.Start_static#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_static/CPP/processstartstatic.cpp)]
        /// [!code-csharp[Process.Start_static#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_static/CS/processstartstatic.cs)]
        /// [!code-vb[Process.Start_static#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_static/VB/processstartstatic.vb)]
        /// ]]></format></example>
        /// <exception cref="System.ComponentModel.InvalidEnumArgumentException">The window style is not one of the <see cref="System.Diagnostics.ProcessWindowStyle" /> enumeration members.</exception>
        [DefaultValueAttribute(System.Diagnostics.ProcessWindowStyle.Normal)]
        public ProcessWindowStyle WindowStyle
        {
            get => _windowStyle;
            set
            {
                if (!Enum.IsDefined(typeof(ProcessWindowStyle), value))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ProcessWindowStyle));
                }

                _windowStyle = value;
            }
        }

        internal string BuildArguments()
        {
            if (HasArgumentList)
            {
                var arguments = new ValueStringBuilder(stackalloc char[256]);
                AppendArgumentsTo(ref arguments);
                return arguments.ToString();
            }

            return Arguments;
        }

        internal void AppendArgumentsTo(ref ValueStringBuilder stringBuilder)
        {
            if (_argumentList != null && _argumentList.Count > 0)
            {
                foreach (string argument in _argumentList)
                {
                    PasteArguments.AppendArgument(ref stringBuilder, argument);
                }
            }
            else if (!string.IsNullOrEmpty(Arguments))
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append(' ');
                }

                stringBuilder.Append(Arguments);
            }
        }
    }
}
