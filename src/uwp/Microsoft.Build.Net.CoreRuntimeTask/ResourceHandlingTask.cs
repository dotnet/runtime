// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using System.Collections;
using System.Resources;
using Microsoft.Cci;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft.Build.Net.CoreRuntimeTask
{

    public sealed class ResourceHandlingTask : Task
    {
        [Serializable()]
        public sealed class ResWInfo
        {
            public DateTime ResWTimeUtc;
            public string   ResWPath;
            public string   ResourceIndexName;
            public string   NeutralResourceLanguage;
        }
        [Serializable()]
        public sealed class PortableLibraryResourceStateInfo
        {
            public DateTime PLibTimeUtc;
            public bool ContainsFrameworkResources;
            public List<ResWInfo> ResWInfoList;
        }

        [Serializable()]
        public sealed class ResourceHandlingState
        {
            [NonSerialized]
            private TaskLoggingHelper _logger;
            public Dictionary<string, PortableLibraryResourceStateInfo> PortableLibraryStatesLookup = new Dictionary<string, PortableLibraryResourceStateInfo>();

            public void SetLogger(TaskLoggingHelper logger) { _logger = logger; } 

            public bool IsUpToDate(string assemblyPath, out bool containsFrameworkResources, out List<ResWInfo> reswInfoList)
            {
                reswInfoList = null;
                containsFrameworkResources = false;

                if (PortableLibraryStatesLookup == null)
                {
                    PortableLibraryStatesLookup = new Dictionary<string, PortableLibraryResourceStateInfo>();
                    return false;
                }
                if (PortableLibraryStatesLookup.Count == 0)
                {
                    return false;
                }

                try
                {
                    if (assemblyPath == null || !File.Exists(assemblyPath))
                    {
                        return false;
                    }
                    PortableLibraryResourceStateInfo info; 
                    if (!PortableLibraryStatesLookup.TryGetValue(assemblyPath, out info))
                    {
                        return false;
                    }
                    FileInfo fiPlib = new FileInfo(assemblyPath);
                    if (!fiPlib.LastWriteTimeUtc.Equals(info.PLibTimeUtc))
                    {
                        _logger.LogMessage(MessageImportance.Low, Resources.Message_CachedReswNotUpToDateAssemblyNewer, assemblyPath); 
                        return false;
                    }
                    if (info.ResWInfoList == null)
                    {
                        return false;
                    }
                    else
                    {
                        foreach (ResWInfo reswInfo in info.ResWInfoList)
                        {
                            if (reswInfo.ResWPath == null || !File.Exists(reswInfo.ResWPath))
                            {
                                _logger.LogMessage(MessageImportance.Low, Resources.Message_CachedReswNotExists, assemblyPath, reswInfo.ResWPath);
                                return false;
                            }

                            FileInfo fiResW = new FileInfo(reswInfo.ResWPath);
                            if (!fiResW.LastWriteTimeUtc.Equals(reswInfo.ResWTimeUtc))
                            {
                                _logger.LogMessage(MessageImportance.Low, Resources.Message_CachedReswNotUpToDate, reswInfo.ResWPath);
                                return false;
                            }
                        }

                    }

                    foreach (ResWInfo reswInfo in info.ResWInfoList)
                    {
                        _logger.LogMessage(MessageImportance.Low, Resources.Message_UsingCachedResw, reswInfo.ResWPath, assemblyPath);
                    }

                    reswInfoList = info.ResWInfoList;
                    containsFrameworkResources = info.ContainsFrameworkResources;
                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogMessage(MessageImportance.Low, Resources.Error_UnspecifiedCheckUpToDate, assemblyPath, e.Message);
                    return false;
                }
            }

            public void Save(string assemblyPath, DateTime plibTimeUtc, bool containsFrameworkResources, List<ResWInfo> reswInfoList)
            {
                try
                {
                    PortableLibraryStatesLookup[assemblyPath] = new PortableLibraryResourceStateInfo() { PLibTimeUtc = plibTimeUtc, ContainsFrameworkResources = containsFrameworkResources, ResWInfoList = reswInfoList};
                }
                catch (Exception e) 
                {
                    _logger.LogMessage(MessageImportance.Low, Resources.Error_UnspecifiedSaveState, assemblyPath, e.Message);
                }
            }

        }

        [Required]
        public ITaskItem[] AssemblyList { get; set; }

        [Required]
        public string OutResWPath { get; set; }

        [Required]
        public string StateFile { get; set; }

        public bool SkipFrameworkResources { get; set; }

        [Output]
        public ITaskItem[] ReswFileList { get; set; }

        [Output]
        public ITaskItem[] UnprocessedAssemblyList { get; set; }

        private MetadataReaderHost _host; 

        private ResourceHandlingState _state = null;
        private List<ITaskItem> _mainAssemblies;
        private List<ITaskItem> _satelliteAssemblies;
        private HashSet<String> _processedAssemblies;

        public override bool Execute()
        {
            ReswFileList            = null;
            UnprocessedAssemblyList = null;

            List<ITaskItem> unprocessedAssemblyList = new List<ITaskItem>();
            List<ITaskItem> reswList = new List<ITaskItem>();

            _state = ReadStateFile(StateFile);
            if (_state == null)
            {
                _state = new ResourceHandlingState();
            }
            _state.SetLogger(Log);

            using (_host = new PeReader.DefaultHost())
            {
                try
                {
                    // Separate main assemblies and satellite assemblies so main assemblies get processed first
                    _mainAssemblies = new List<ITaskItem>();
                    _satelliteAssemblies = new List<ITaskItem>();
                    _processedAssemblies = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

                    foreach (ITaskItem item in AssemblyList)
                    {
                        if (_processedAssemblies.Contains(item.ItemSpec))
                        {
                            continue;
                        }
                        _processedAssemblies.Add(item.ItemSpec);

                        if (item.ItemSpec.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        {
                            if (item.ItemSpec.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
                            {
                                _satelliteAssemblies.Add(item);
                            }
                            else
                            {
                                _mainAssemblies.Add(item);
                            }
                        }
                    }

                    foreach (ITaskItem assemblyFilePath in _mainAssemblies.Concat(_satelliteAssemblies))
                    {
                        List<ResWInfo> resWInfoList = null;
                        bool containsFrameworkResources = false;
                        if (!_state.IsUpToDate(assemblyFilePath.ItemSpec, out containsFrameworkResources, out resWInfoList))
                        {
                            resWInfoList = ExtractAssemblyResWList(assemblyFilePath.ItemSpec, out containsFrameworkResources);

                            if (resWInfoList != null)
                            {
                                FileInfo fiAssembly = new FileInfo(assemblyFilePath.ItemSpec);
                                _state.Save(assemblyFilePath.ItemSpec, fiAssembly.LastWriteTimeUtc, containsFrameworkResources, resWInfoList);
                            }
                        }

                        if (resWInfoList != null)
                        {
                            foreach (ResWInfo reswInfo in resWInfoList)
                            {
                                TaskItem newTaskItem = new TaskItem(reswInfo.ResWPath);
                                newTaskItem.SetMetadata("ResourceIndexName", reswInfo.ResourceIndexName);
                                if (!String.IsNullOrEmpty(reswInfo.NeutralResourceLanguage))
                                {
                                    newTaskItem.SetMetadata("NeutralResourceLanguage", reswInfo.NeutralResourceLanguage);
                                }

                                if (!containsFrameworkResources)
                                {
                                    newTaskItem.SetMetadata("OriginalItemSpec", reswInfo.ResWPath); // Original GenerateResource behavior creates this additional metadata item on processed non-framework assemblies
                                    reswList.Add(newTaskItem);
                                }
                                else if (!SkipFrameworkResources)
                                {
                                    reswList.Add(newTaskItem);
                                }
                            }
                        }

                    }

                    UnprocessedAssemblyList = unprocessedAssemblyList.ToArray(); // For now this list will always be empty
                    ReswFileList = reswList.ToArray();
                    WriteStateFile(StateFile, _state);
                } 
                catch (Exception e)
                {
                    Log.LogError(Resources.Error_ResourceExtractionFailed, e.Message);
                    return false;
                }
            }

            return true;
        }

        private ResourceHandlingState ReadStateFile(string stateFile)
        {
            try 
            {
                if (!String.IsNullOrEmpty(stateFile) && File.Exists(stateFile))
                {
                    using (FileStream fs = new FileStream(stateFile, FileMode.Open))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        object deserializedObject = formatter.Deserialize(fs);
                        ResourceHandlingState state = deserializedObject as ResourceHandlingState;
                        if (state == null && deserializedObject != null)
                        {
                            Log.LogMessage(MessageImportance.Normal, Resources.Message_UnspecifiedStateFileCorrupted, stateFile);
                        }
                        return state;
                    }
                }
                else
                    return null;
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.Low, Resources.Message_UnspecifiedReadStateFile, e.Message);
                return null;
            }
        }

        private bool IsAtOutputFolder(string path)
        {
            try
            {
                return (Path.GetDirectoryName(path).Equals(OutResWPath.TrimEnd(new char[] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}), StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        private void WriteStateFile(string stateFile, ResourceHandlingState state)
        {
            try
            {
                if (stateFile != null && stateFile.Length > 0 )
                {
                    using (FileStream fs = new FileStream(stateFile, FileMode.Create))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(fs, state);
                    }
                }

            }
            catch (Exception e) 
            {
                Log.LogMessage(MessageImportance.Low, Resources.Message_UnspecifiedSaveStateFile, e.Message);
            }
        }

        private ResWInfo ExtractResourcesFromStream(Stream stream, IAssembly assembly, string resourceFileName, bool containsFrameworkResources)
        {
            string reswFilePath;
            string resourceIndexName;
            string neutralResourceLanguage = "";

            if (containsFrameworkResources)
            {
                reswFilePath = OutResWPath + Path.AltDirectorySeparatorChar + resourceFileName + ".resw";
                resourceIndexName = resourceFileName;
                neutralResourceLanguage = "en-US";
            }
            else
            {
                string culturePath = "";
                string culture = assembly.Culture;
                string assemblyName = assembly.Name.Value;

                if (!String.IsNullOrEmpty(culture))
                {
                    culturePath = culture + Path.DirectorySeparatorChar;
                }
                else if (TryGetNeutralResourcesLanguageAttribute(assembly, out neutralResourceLanguage))
                {
                    culturePath = neutralResourceLanguage + Path.DirectorySeparatorChar;
                }
                // Do not handle the case where culture is Invariant and no NeutralResourcesLanguageAttribute is declared
                // This should already be taken care of in method ExtractAssemblyResWList
                else
                {
                    Debug.Assert(false, "Assembly with the Invariant culture and no NeutralResourcesLanguageAttribute is being extracted for embedded resources. This should have been caught by earlier checks.");
                }

                if (resourceFileName.EndsWith("." + culture, StringComparison.OrdinalIgnoreCase))
                {
                    resourceFileName = resourceFileName.Remove(resourceFileName.Length - (culture.Length + 1));
                }

                resourceIndexName = assemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) ? assemblyName.Remove(assemblyName.Length - 10) : assemblyName;
                reswFilePath = OutResWPath + resourceIndexName + Path.DirectorySeparatorChar + culturePath + resourceFileName + ".resw";
                if (!Directory.Exists(Directory.GetParent(reswFilePath).ToString()))
                {
                    Directory.CreateDirectory(Directory.GetParent(reswFilePath).ToString());
                }
            }

            WriteResW(stream, reswFilePath);

            FileInfo fiResW = new FileInfo(reswFilePath);
            return new ResWInfo() { ResWPath = reswFilePath, ResWTimeUtc = fiResW.LastWriteTimeUtc, ResourceIndexName = resourceIndexName, NeutralResourceLanguage = neutralResourceLanguage };
        }

        private bool TryGetNeutralResourcesLanguageAttribute(IAssembly assembly, out String neutralResourceLanguage)
        {
            neutralResourceLanguage = "";
            foreach (ICustomAttribute attribute in assembly.AssemblyAttributes)
            {
                if (TypeHelper.GetTypeName(attribute.Type, NameFormattingOptions.None).Equals("System.Resources.NeutralResourcesLanguageAttribute"))
                {
                    if (attribute.Arguments.Count() > 0)
                    {
                        IMetadataConstant metadataConstant = attribute.Arguments.ElementAt(0) as IMetadataConstant;
                        if (metadataConstant == null)
                        {
                            return false; // Unable to parse
                        }

                        Object value = metadataConstant.Value;
                        if (!(value is String))
                        {
                            return false; // Expected to be a string
                        }
                        neutralResourceLanguage = (String)value;
                        return true;
                    }
                }
            }
            return false;
        }

        private void WriteResW(Stream stream, string reswFilePath)
        {
            using (ResourceReader rr = new ResourceReader(stream))
            {
                using (ResXResourceWriter rw = new ResXResourceWriter(reswFilePath))
                {
                    foreach (DictionaryEntry dict in rr)
                    {
                        rw.AddResource((string)dict.Key, (string)dict.Value);
                    }
                }
            }
        }

        private List<ResWInfo> ExtractAssemblyResWList(string assemblyFilePath, out bool containsFrameworkResources)
        {
            containsFrameworkResources = false;

            IAssembly assembly = _host.LoadUnitFrom(assemblyFilePath) as IAssembly;
            if (assembly == null || assembly == Dummy.Assembly)
            {
                return null;
            }

            if (assembly.Resources == null)
            {
                return null;
            }

            string neutralResourceLanguage;
            if (String.IsNullOrEmpty(assembly.Culture) && !TryGetNeutralResourcesLanguageAttribute(assembly, out neutralResourceLanguage))
            {
                // Must have NeutralResourcesLanguageAttribute
                // warning MSB3817: The assembly "<FullPath>\ClassLibrary1.dll" does not have a NeutralResourcesLanguageAttribute on it. To be used in an app package, portable libraries must define a NeutralResourcesLanguageAttribute on their main assembly (ie, the one containing code, not a satellite assembly).
                return null;
            }

            List<ResWInfo> reswInfoList = new List<ResWInfo>();
            string frameworkResourcesName = "FxResources." + assembly.Name.Value + ".SR.resources";

            foreach (IResourceReference resourceReference in assembly.Resources)
            {
                if (!resourceReference.Resource.IsInExternalFile && resourceReference.Name.Value.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                {
                    const int BUFFERSIZE = 4096;
                    byte[] buffer = new byte[BUFFERSIZE];
                    int index = 0;

                    using (MemoryStream ms = new MemoryStream(BUFFERSIZE))
                    {
                        foreach (byte b in resourceReference.Resource.Data)
                        {
                            if (index == BUFFERSIZE)
                            {
                                ms.Write(buffer, 0, BUFFERSIZE);
                                index = 0;
                            }
                            buffer[index++] = b;
                        }
                        ms.Write(buffer, 0, index);
                        ms.Seek(0, SeekOrigin.Begin);
                        
                        string resourceFileName = resourceReference.Name.Value.Remove(resourceReference.Name.Value.Length - 10);
                        if (resourceReference.Name.Value.Equals(frameworkResourcesName, StringComparison.OrdinalIgnoreCase))
                        {
                            containsFrameworkResources = true;
                            reswInfoList.Add(ExtractResourcesFromStream(ms, assembly, resourceFileName, true));
                            return reswInfoList;
                        }
                        else
                        {
                            reswInfoList.Add(ExtractResourcesFromStream(ms, assembly, resourceFileName, false));
                        }
                    }
                }
            }

            return reswInfoList;
        }
    }
}
