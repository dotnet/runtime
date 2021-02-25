// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;

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
    public sealed partial class ProcessStartInfo
    {
        /// <summary>Gets the set of verbs associated with the type of file specified by the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property.</summary>
        /// <value>The actions that the system can apply to the file indicated by the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property.</value>
        /// <remarks>The <see cref="System.Diagnostics.ProcessStartInfo.Verbs" /> property enables you to determine the verbs that can be used with the file specified by the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property. You can set the <see cref="System.Diagnostics.ProcessStartInfo.Verb" /> property to the value of any verb in the set. Examples of verbs are "Edit", "Open", "OpenAsReadOnly", "Print", and "Printto".
        /// When you use the <see cref="System.Diagnostics.ProcessStartInfo.Verbs" /> property, you must include the file name extension when you set the value of the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property. The file name extension determines the set of possible verbs.</remarks>
        /// <example>The following code example displays the defined verbs for the chosen file name. If the user selects one of the defined verbs, the example starts a new process using the selected verb and the input file name.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[ProcessVerbs_Diagnostics#3](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessVerbs_Diagnostics/CS/source.cs#3)]
        /// [!code-vb[ProcessVerbs_Diagnostics#3](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessVerbs_Diagnostics/VB/source.vb#3)]
        /// ]]></format></example>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.Verb"/>
        public string[] Verbs
        {
            get
            {
                string extension = Path.GetExtension(FileName);
                if (string.IsNullOrEmpty(extension))
                    return Array.Empty<string>();

                using (RegistryKey? key = Registry.ClassesRoot.OpenSubKey(extension))
                {
                    if (key == null)
                        return Array.Empty<string>();

                    string? value = key.GetValue(string.Empty) as string;
                    if (string.IsNullOrEmpty(value))
                        return Array.Empty<string>();

                    using (RegistryKey? subKey = Registry.ClassesRoot.OpenSubKey(value + "\\shell"))
                    {
                        if (subKey == null)
                            return Array.Empty<string>();

                        string[] names = subKey.GetSubKeyNames();
                        List<string> verbs = new List<string>();
                        foreach (string name in names)
                        {
                            if (!string.Equals(name, "new", StringComparison.OrdinalIgnoreCase))
                            {
                                verbs.Add(name);
                            }
                        }
                        return verbs.ToArray();
                    }
                }
            }
        }

        /// <summary>Gets or sets a value indicating whether to use the operating system shell to start the process.</summary>
        /// <value><see langword="true" /> if the shell should be used when starting the process; <see langword="false" /> if the process should be created directly from the executable file. The default is <see langword="true" /> on .NET Framework apps and <see langword="false" /> on .NET Core apps.</value>
        /// <remarks>Setting this property to <see langword="false" /> enables you to redirect input, output, and error streams.
        /// The word "shell" in this context (`UseShellExecute`) refers to a graphical shell (similar to the Windows shell) rather than command shells (for example, `bash` or `sh`) and lets users launch graphical applications or open documents.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// > <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute> must be `false` if the <xref:System.Diagnostics.ProcessStartInfo.UserName> property is not `null` or an empty string, or an <xref:System.InvalidOperationException> will be thrown when the <xref:System.Diagnostics.Process.Start(System.Diagnostics.ProcessStartInfo)?displayProperty=nameWithType> method is called.
        /// ]]></format>
        /// When you use the operating system shell to start processes, you can start any document (which is any registered file type associated with an executable that has a default open action) and perform operations on the file, such as printing, by using the <see cref="System.Diagnostics.Process" /> object. When <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="false" />, you can start only executables by using the <see cref="System.Diagnostics.Process" /> object.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// > <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute> must be `true` if you set the <xref:System.Diagnostics.ProcessStartInfo.ErrorDialog> property to `true`.
        /// ]]></format>
        /// If you set the <see cref="System.Diagnostics.ProcessStartInfo.WindowStyle" /> to <see cref="System.Diagnostics.ProcessWindowStyle.Hidden" />, <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> must be set to <see langword="true" />.
        /// ### WorkingDirectory
        /// The <see cref="System.Diagnostics.ProcessStartInfo.WorkingDirectory" /> property behaves differently depending on the value of the <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property. When <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="true" />, the <see cref="System.Diagnostics.ProcessStartInfo.WorkingDirectory" /> property specifies the location of the executable. If <see cref="System.Diagnostics.ProcessStartInfo.WorkingDirectory" /> is an empty string, it is assumed that the current directory contains the executable.
        /// When <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="false" />, the <see cref="System.Diagnostics.ProcessStartInfo.WorkingDirectory" /> property is not used to find the executable. Instead, it is used only by the process that is started and has meaning only within the context of the new process. When <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="false" />, the <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property can be either a fully qualified path to the executable, or a simple executable name that the system will attempt to find within folders specified by the PATH environment variable.</remarks>
        /// <example><format type="text/markdown"><![CDATA[
        /// [!code-cpp[ProcessOneStream#1](~/samples/snippets/cpp/VS_Snippets_CLR/ProcessOneStream/CPP/stdstr.cpp#1)]
        /// [!code-csharp[ProcessOneStream#1](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessOneStream/CS/stdstr.cs#1)]
        /// [!code-vb[ProcessOneStream#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessOneStream/VB/stdstr.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.PlatformNotSupportedException">An attempt to set the value to <see langword="true" /> on Universal Windows Platform (UWP) apps occurs.</exception>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.RedirectStandardInput"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError"/>
        public bool UseShellExecute { get; set; }
    }
}
