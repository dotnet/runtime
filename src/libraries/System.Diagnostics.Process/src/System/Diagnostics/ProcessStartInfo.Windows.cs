// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    /// <summary>Specifies a set of values that are used when you start a process.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// <xref:System.Diagnostics.ProcessStartInfo> is used together with the <xref:System.Diagnostics.Process> component. When you start a process using the <xref:System.Diagnostics.Process> class, you have access to process information in addition to that available when attaching to a running process.
    /// You can use the <xref:System.Diagnostics.ProcessStartInfo> class for better control over the process you start. You must at least set the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property, either manually or using the constructor. The file name is any application or document. Here a document is defined to be any file type that has an open or default action associated with it. You can view registered file types and their associated applications for your computer by using the **Folder Options** dialog box, which is available through the operating system. The **Advanced** button leads to a dialog box that shows whether there is an open action associated with a specific registered file type.
    /// In addition, you can set other properties that define actions to take with that file. You can specify a value specific to the type of the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property for the <xref:System.Diagnostics.ProcessStartInfo.Verb%2A> property. For example, you can specify "print" for a document type. Additionally, you can specify <xref:System.Diagnostics.ProcessStartInfo.Arguments%2A> property values to be command-line arguments to pass to the file's open procedure. For example, if you specify a text editor application in the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property, you can use the <xref:System.Diagnostics.ProcessStartInfo.Arguments%2A> property to specify a text file to be opened by the editor.
    /// Standard input is usually the keyboard, and standard output and standard error are usually the monitor screen. However, you can use the <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardInput%2A>, <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardOutput%2A>, and <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardError%2A> properties to cause the process to get input from or return output to a file or other device. If you use the <xref:System.Diagnostics.Process.StandardInput%2A>, <xref:System.Diagnostics.Process.StandardOutput%2A>, or <xref:System.Diagnostics.Process.StandardError%2A> properties on the <xref:System.Diagnostics.Process> component, you must first set the corresponding value on the <xref:System.Diagnostics.ProcessStartInfo> property. Otherwise, the system throws an exception when you read or write to the stream.
    /// Set the <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> property to specify whether to start the process by using the operating system shell. If <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> is set to `false`, the new process inherits the standard input, standard output, and standard error streams of the calling process, unless the <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardInput%2A>, <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardOutput%2A>, or <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardError%2A> properties, respectively, are set to `true`.
    /// You can change the value of any <xref:System.Diagnostics.ProcessStartInfo> property up to the time that the process starts. After you start the process, changing these values has no effect.
    /// > [!NOTE]
    /// >  This class contains a link demand at the class level that applies to all members. A <xref:System.Security.SecurityException> is thrown when the immediate caller does not have full-trust permission. For details about security demands, see [Link Demands](/dotnet/framework/misc/link-demands).
    /// ## Examples
    /// The following code example demonstrates how to use the <xref:System.Diagnostics.ProcessStartInfo> class to start Internet Explorer, providing the destination URLs as <xref:System.Diagnostics.ProcessStartInfo> arguments.
    /// [!code-cpp[Process.Start_static#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_static/CPP/processstartstatic.cpp)]
    /// [!code-csharp[Process.Start_static#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_static/CS/processstartstatic.cs)]
    /// [!code-vb[Process.Start_static#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_static/VB/processstartstatic.vb)]
    /// ]]></format></remarks>
    /// <altmember cref="System.Diagnostics.Process"/>
    public sealed partial class ProcessStartInfo
    {
        private string? _domain;

        /// <summary>Gets or sets the user password in clear text to use when starting the process.</summary>
        /// <value>The user password in clear text.</value>
        [SupportedOSPlatform("windows")]
        public string? PasswordInClearText { get; set; }

        /// <summary>Gets or sets a value that identifies the domain to use when starting the process. If this value is <see langword="null" />, the <see cref="System.Diagnostics.ProcessStartInfo.UserName" /> property must be specified in UPN format.</summary>
        /// <value>The Active Directory domain to use when starting the process. If this value is <see langword="null" />, the <see cref="System.Diagnostics.ProcessStartInfo.UserName" /> property must be specified in UPN format.</value>
        /// <remarks>This property is primarily of interest to users within enterprise environments that use Active Directory.</remarks>
        [SupportedOSPlatform("windows")]
        public string Domain
        {
            get => _domain ?? string.Empty;
            set => _domain = value;
        }

        /// <summary>Gets or sets a value that indicates whether the Windows user profile is to be loaded from the registry.</summary>
        /// <value><see langword="true" /> if the Windows user profile should be loaded; otherwise, <see langword="false" />. The default is <see langword="false" />.</value>
        /// <remarks>This property is referenced if the process is being started by using the user name, password, and domain.
        /// If the value is <see langword="true" />, the user's profile in the `HKEY_USERS` registry key is loaded. Loading the profile can be time-consuming. Therefore, it is best to use this value only if you must access the information in the `HKEY_CURRENT_USER` registry key.
        /// In Windows Server 2003 and Windows 2000, the profile is unloaded after the new process has been terminated, regardless of whether the process has created child processes.
        /// In Windows XP, the profile is unloaded after the new process and all child processes it has created have been terminated.</remarks>
        [SupportedOSPlatform("windows")]
        public bool LoadUserProfile { get; set; }

        /// <summary>Gets or sets a secure string that contains the user password to use when starting the process.</summary>
        /// <value>The user password to use when starting the process.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!IMPORTANT]
        /// >  The <xref:System.Diagnostics.ProcessStartInfo.WorkingDirectory%2A> property must be set if <xref:System.Diagnostics.ProcessStartInfo.UserName%2A> and <xref:System.Diagnostics.ProcessStartInfo.Password%2A> are provided. If the property is not set, the default working directory is %SYSTEMROOT%\system32.
        /// > [!NOTE]
        /// >  Setting the <xref:System.Diagnostics.ProcessStartInfo.Domain%2A>, <xref:System.Diagnostics.ProcessStartInfo.UserName%2A>, and the <xref:System.Diagnostics.ProcessStartInfo.Password%2A> properties in a <xref:System.Diagnostics.ProcessStartInfo> object is the recommended practice for starting a process with user credentials.
        /// A <xref:System.Security.SecureString> object is like a <xref:string> object in that it has a text value. However, the value of a <xref:System.Security.SecureString> object is automatically encrypted, it can be modified until your application marks it as read-only, and it can be deleted from computer memory by either your application or the .NET Framework garbage collector.
        /// For more information about secure strings and an example of how to obtain a password to set this property, see the <xref:System.Security.SecureString> class.
        /// > [!NOTE]
        /// >  If you provide a value for the <xref:System.Diagnostics.ProcessStartInfo.Password%2A> property, the <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> property must be `false`, or an <xref:System.InvalidOperationException> will be thrown when the <xref:System.Diagnostics.Process.Start%28System.Diagnostics.ProcessStartInfo%29?displayProperty=nameWithType> method is called.
        /// ]]></format></remarks>
        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public SecureString? Password { get; set; }
    }
}
