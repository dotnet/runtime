// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Build.Net.CoreRuntimeTask
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System.Runtime.InteropServices;

    /// <summary> Task for wiring up CoreRuntime to appx payload </summary>
    public class WireUpCoreRuntime : Task
    {
        private static int timeoutDuration = 360000; // 6 minutes

        // HRESULT exit codes from Crossgen that indicate the input file is not a managed assembly
        private const int ErrorNotAnAssembly = -2146230517; // 0x80131f0b
        
        private const string PackageDependencyElementName = "PackageDependency";

        private const string AppEntryPointDir = "entrypoint";

        private const string UWPShimEXE = "UWPShim.exe";

        /// <summary> Gets or sets the path to the input AppxManifest file </summary>
        [Required]
        public string AppxManifest { get; set; }

        /// <summary> Gets or sets the appx package payload that will be processed </summary>
        [Required]
        public ITaskItem[] AppxPackagePayload { get; set; }

        /// <summary> Gets or sets the output directory that will contain the output appx manifest and any output executables </summary>
        [Required]
        public string OutputPath { get; set; }

        /// <summary> Gets ot sets the target runtime of the application </summary>
        [Required]
        public string TargetRuntime { get; set; }

        /// <summary> Gets or sets the target architecture </summary>
        [Required]
        public string TargetArch { get; set; } 

        /// <summary> Gets or sets the framework packages to be added as PackageDependencies </summary>
        [Required]
        public ITaskItem[] FrameworkPackages { get; set; }

        /// <summary> Gets or sets the CoreRuntime Extension SDK location. This is used to locate the UWPShim.exe </summary>
        [Required]
        public string CoreRuntimeSDKLocation { get; set; }

        /// <summary> Location of CopyWin32Resources.exe </summary>
        [Required]
        public string CopyWin32ResourcesLocation { get; set; }

        /// <summary> Gets ot sets the timeout duration in which the call to CopyWin32Resources is expected to return </summary>
        public int TimeoutDuration 
        { 
            get 
            { 
                return timeoutDuration; 
            }  
            set
            {
                timeoutDuration = value;
            }
        }

        /// <summary> Output parameter indicating the error code of executing this task </summary>
        [Output]
        public int ErrorCode { get; private set; }

        /// <summary> 
        /// Output parameter indicating whether the CoreRuntime framework package needs to be deployed before
        /// deploying the application
        /// </summary>
        [Output]
        public bool FrameworkPackagesNeedsToBeDeployed { get; private set; }

        /// <summary> Gets or sets the appx package payload that has been processed </summary>
        [Output]
        public ITaskItem[] TransformedAppxPackagePayload { get; set; }


        /// <summary> Execute the task. </summary>
        /// <returns> True if succeeeds </returns>
        public override bool Execute()
        {
            ErrorCode = InternalExecute();
            return (ErrorCode == (int)ErrorCodes.Success);
        }

        /// <summary> The internal implementation of the execute task </summary>
        /// <returns> ErrorCodes.Success if succeeds, one of the other ErrorCodes otherwise </returns>
        internal int InternalExecute()
        {
            bool isTargetRuntimeManaged = TargetRuntime.Equals("Managed", StringComparison.OrdinalIgnoreCase);
            List<ITaskItem> currentAppxPackagePayload = AppxPackagePayload.ToList();
            string transformedAppxManifest = Path.Combine(OutputPath, "AppxManifest.xml");

            // Perform setup:
            //   - Create output directory if it does not already exist
            //   - Delete existing AppxManifest.xml if it exists
            try
            {
                Directory.CreateDirectory(OutputPath);
                File.Delete(transformedAppxManifest);
            }
            catch (PathTooLongException)
            {
                Log.LogError(Resources.Error_PathTooLongExceptionOutputFolder);
                return (int)ErrorCodes.PathTooLongException;
            }
            catch (IOException ioException)
            {
                Log.LogError(Resources.Error_IOExceptionOutputFolder, ioException.Message);
                return (int)ErrorCodes.InputFileReadError;
            }
            catch (Exception e)
            {
                Log.LogError(Resources.Error_InternalWireUpCoreRuntimeError, e.Message);
                return (int)ErrorCodes.InternalWireUpCoreRuntimeError;
            }

            // Apply transformations required to hook up the Core Runtime
            // 1. For Managed apps, move <app.exe> to [AppEntryPointDir]\<app.exe> and copy UWPShim.exe to package root as <app.exe>
            // 2. Replace all ClrHost.dll with either UWPShim.exe (for hybrid apps) or <app.exe> (for managed apps)
            // 3. If #2 above is performed, inject UWPShim.exe into the output directory for unmanaged apps
            //    which contain managed winmd and for managed background tasks (which do not contain entrypoint exe)
            // 4. If #1 or #2 above is performed, add a package dependency to CoreRuntime framework package.

            using (StreamReader sr = new StreamReader(AppxManifest))
            {
                XDocument doc = XDocument.Load(sr, LoadOptions.None);
                XNamespace ns = @"http://schemas.microsoft.com/appx/manifest/foundation/windows10";
                string inprocServer = UWPShimEXE;
                var uwpShimLocation = Path.Combine(new [] { 
                            CoreRuntimeSDKLocation,
                            "AppLocal",
                            UWPShimEXE} );

                IEnumerable<XAttribute> entryPointExecutables = Enumerable.Empty<XAttribute>();
                if (isTargetRuntimeManaged)
                {
                    // 1. For Managed apps, move <app.exe> to [AppEntryPointDir]\<app.exe> and copy UWPShim.exe to package root as <app.exe>
                    entryPointExecutables = doc.Descendants(ns + "Applications").Descendants(ns + "Application").Where(x => x.Attribute("Executable") != null).Select(x => x.Attribute("Executable"));

                    if (entryPointExecutables.Any())
                    {
                        // Set the inprocServer to the <app.exe>. From this point on that's our UWPShim.exe
                        // and since we will be copying uwpShim possibly several times with different names, yet they are all 
                        // uwpshim.exe, it's OK to grab the first one and use it as inprocserver entry
                        inprocServer = entryPointExecutables.First().Value;
                        if (entryPointExecutables.Any(x => x.Value.Contains(Path.DirectorySeparatorChar)))
                        {
                            Log.LogError(Resources.Error_CustomEntryPointNotSupported);
                            return (int)ErrorCodes.NotSupported;
                        }

                        foreach (var entryPointExecutable in entryPointExecutables)
                        {
                            // Do not copy <app.exe> from original location, just modify TargetPath to [AppEntryPointDir]\<app.exe>
                            ITaskItem currentManagedEntryPointExecutableTaskItem = AppxPackagePayload.Where(x => x.GetMetadata("TargetPath") == entryPointExecutable.Value).Single();
                            currentManagedEntryPointExecutableTaskItem.SetMetadata("TargetPath", AppEntryPointDir + "\\" + entryPointExecutable.Value);

                            // Copy UWPShim
                            var entryPointExecutableShim = Path.Combine(OutputPath, entryPointExecutable.Value);
                            File.Copy(uwpShimLocation, entryPointExecutableShim, true);
                            var copyResourcesReturncode = CopyWin32Resources(currentManagedEntryPointExecutableTaskItem.ItemSpec, entryPointExecutableShim);
                            if (copyResourcesReturncode != 0)
                            {
                                Log.LogError(Resources.Error_CopyWin32ResourcesFailed, copyResourcesReturncode);
                                return (int)ErrorCodes.CoreRuntimeLinkageError;
                            }

                            // Add UWPShim to appx package payload
                            ITaskItem entryPointExecutableShimTaskItem = new TaskItem(entryPointExecutableShim);
                            entryPointExecutableShimTaskItem.SetMetadata("TargetPath", entryPointExecutable.Value);
                            currentManagedEntryPointExecutableTaskItem.CopyMetadataTo(entryPointExecutableShimTaskItem);
                            currentAppxPackagePayload.Add(entryPointExecutableShimTaskItem);
                        }
                    }
                }
                
                // 
                // 2. Replace all ClrHost.dll with either UWPShim.exe (for hybrid apps) or <app.exe> (for managed apps)
                var inprocServerNodes = doc.DescendantNodes().OfType<XText>()
                                           .Where(x => x.Value.Equals("ClrHost.dll", StringComparison.OrdinalIgnoreCase))
                                           .Select(x => x);

                bool bHasManagedWinMD = false;
                foreach (var node in inprocServerNodes)
                {
                    node.Value = inprocServer;
                    bHasManagedWinMD = true;
                }

                //
                // 3. If #2 above is performed, inject UWPShim.exe into the output directory for unmanaged apps
                //    which contain managed winmd and for managed background tasks (which do not contain entrypoint exe)
                if (bHasManagedWinMD && inprocServer.Equals(UWPShimEXE))
                {
                    try 
                    {
                        // Copy UWPShim
                        string uwpDestination = Path.Combine(OutputPath, inprocServer);
                        File.Copy(uwpShimLocation, uwpDestination, true);

                        // Add UWPShim to appx package payload
                        TaskItem uwpShimTaskItem = new TaskItem(uwpDestination);
                        uwpShimTaskItem.SetMetadata("TargetPath", inprocServer);
                        currentAppxPackagePayload.Add(uwpShimTaskItem);
                    }
                    catch (Exception exception)
                    {
                        Log.LogError(Resources.Error_UnspecifiedCreatingUWPShimForHybrid, exception.Message);
                        return (int)ErrorCodes.CoreRuntimeLinkageError;
                    }

                }

                //
                // 4. If #1 or #2 above is performed, add a package dependency to CoreRuntime framework package.
                if (isTargetRuntimeManaged || bHasManagedWinMD)
                {
                    foreach (var FrameworkPackage in FrameworkPackages) 
                    {
                        string FrameworkPackageName = FrameworkPackage.GetMetadata("Name");
                        string FrameworkPackageMinVersion = FrameworkPackage.GetMetadata("Version");
                        string FrameworkPackagePublisher = FrameworkPackage.GetMetadata("Publisher");
                        if (!doc.Descendants(ns + "PackageDependency").Any(x => x.Attributes("Name").SingleOrDefault().Value == FrameworkPackageName))
                        {
                            // There aren't any such PackageDependency. Add it now.
                            XElement packageDependency = new XElement(
                                                            ns + "PackageDependency",
                                                            new XAttribute("Name",          FrameworkPackageName),
                                                            new XAttribute("MinVersion",    FrameworkPackageMinVersion),
                                                            new XAttribute("Publisher",     FrameworkPackagePublisher)
                                                        );
                            doc.Descendants(ns + "Dependencies").SingleOrDefault().Add(packageDependency);
                            FrameworkPackagesNeedsToBeDeployed = true;

                        }
                    }
                }

                doc.Save(transformedAppxManifest);
            }

            TransformedAppxPackagePayload = currentAppxPackagePayload.ToArray();
            return (int)ErrorCodes.Success;

        }

        int CopyWin32Resources(string lpPEFileToReadResourcesFrom, string lpPEFileToInsertResourcesInto)
        {

            Process process = new Process();
            process.StartInfo.FileName = CopyWin32ResourcesLocation ;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            string args = "/input:\"" + lpPEFileToReadResourcesFrom + "\" ";
            args += "/output:\"" + lpPEFileToInsertResourcesInto + "\"";

            process.StartInfo.Arguments = args;
            string output = String.Empty;

            process.Start();

            bool timeOut = !process.WaitForExit(TimeoutDuration);

            if (timeOut)
            {
                try 
                {
                    process.Kill();
                }
                catch {}
                
                return -1;
            }

            return process.ExitCode;
        }
        
    }
}

