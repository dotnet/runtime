// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
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
        public sealed class PortableLibraryResourceStateInfo
        {
            public DateTime PLibTimeUtc;
            public DateTime ResWTimeUtc;
            public string   ResWPath;
        }

        [Serializable()]
        public sealed class ResourceHandlingState
        {
            [NonSerialized]
            private TaskLoggingHelper _logger;
            public Dictionary<string, PortableLibraryResourceStateInfo> PortableLibraryStatesLookup = new Dictionary<string, PortableLibraryResourceStateInfo>();

            public void SetLogger(TaskLoggingHelper logger) { _logger = logger; } 

            public bool IsUpToDate(string assemblyPath, out string reswFilePath)
            {
                reswFilePath = null;
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
                    if (info.ResWPath == null || !File.Exists(info.ResWPath))
                    {
                        _logger.LogMessage(MessageImportance.Low, Resources.Message_CachedReswNotExists, assemblyPath, info.ResWPath); 
                        return false;
                    }
    
                    FileInfo fiResW = new FileInfo(info.ResWPath);
                    if (!fiResW.LastWriteTimeUtc.Equals(info.ResWTimeUtc))
                    {
                        _logger.LogMessage(MessageImportance.Low, Resources.Message_CachedReswNotUpToDate, info.ResWPath); 
                        return false;
                    }

                    _logger.LogMessage(MessageImportance.Low, Resources.Message_UsingCachedResw, info.ResWPath, assemblyPath); 
                    reswFilePath = info.ResWPath;
                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogMessage(MessageImportance.Low, Resources.Error_UnspecifiedCheckUpToDate, assemblyPath, e.Message);
                    return false;
                }
            }

            public void Save(string assemblyPath, string reswPath, DateTime plibTimeUtc, DateTime reswTimeUtc)
            {
                try
                {
                    PortableLibraryStatesLookup[assemblyPath] = new PortableLibraryResourceStateInfo() { PLibTimeUtc = plibTimeUtc, ResWTimeUtc = reswTimeUtc, ResWPath = reswPath};
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
                    ITaskItem firstNonFrameworkAssembly = null;
                    foreach (ITaskItem assemblyFilePath in AssemblyList)
                    {
                        string reswPath = null;
                        bool containsResources = false;
                        if (!_state.IsUpToDate(assemblyFilePath.ItemSpec, out reswPath) || 
                            !IsAtOutputFolder(reswPath) )
                        {
                            reswPath = ExtractFrameworkAssemblyResW(assemblyFilePath.ItemSpec, out containsResources);
                            if (reswPath != null)
                            {
                                FileInfo fiAssembly = new FileInfo(assemblyFilePath.ItemSpec);
                                FileInfo fiResW = new FileInfo(reswPath);
                                _state.Save(assemblyFilePath.ItemSpec, reswPath, fiAssembly.LastWriteTimeUtc, fiResW.LastWriteTimeUtc);
                            }
                        }

                        if (reswPath == null)
                        {
                            if (containsResources)
                                unprocessedAssemblyList.Add(assemblyFilePath);
                            
                            if (unprocessedAssemblyList.Count == 0)
                                firstNonFrameworkAssembly = assemblyFilePath;
                        }
                        else
                        {
                            TaskItem newTaskItem = new TaskItem(reswPath);
                            newTaskItem.SetMetadata("NeutralResourceLanguage","en-US");
                            newTaskItem.SetMetadata("ResourceIndexName",Path.GetFileNameWithoutExtension(reswPath));
                            reswList.Add(newTaskItem);
                        }

                    }

                    UnprocessedAssemblyList = unprocessedAssemblyList.ToArray();
                    
                    if (!SkipFrameworkResources)
                    {
                        ReswFileList = reswList.ToArray();
                    }

                    // we make sure unprocessedAssemblyList has at least one item if ReswFileList is empty to avoid having _GeneratePrisForPortableLibraries
                    // repopulate the assembly list and reprocess them
                    if ((ReswFileList == null || ReswFileList.Length == 0) && 
                        UnprocessedAssemblyList.Length == 0 && 
                        firstNonFrameworkAssembly != null)
                    {
                        UnprocessedAssemblyList = new ITaskItem[1] { firstNonFrameworkAssembly };
                    }

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

        private string ExtractFrameworkAssemblyResW(string assemblyFilePath, out bool containsResources)
        {
            string assemblyName;
            using (Stream stream = ExtractFromAssembly(assemblyFilePath, out assemblyName, out containsResources))
            {
                if (stream == null)
                    return null;

                string reswFilePath = OutResWPath + Path.AltDirectorySeparatorChar + "FxResources." + assemblyName + ".SR.resw";
                WriteResW(stream, reswFilePath);
                return reswFilePath;
            }
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

        private Stream ExtractFromAssembly(string assemblyFilePath, out string assemblyName, out bool containsResources)
        {
            assemblyName = null;
            containsResources = true;

            IAssembly assembly = _host.LoadUnitFrom(assemblyFilePath) as IAssembly;
            if (assembly == null || assembly == Dummy.Assembly)
            {
                containsResources = false;
                return null;
            }

            if (assembly.Resources == null)
            {
                containsResources = false;
                return null;
            }

            assemblyName = assembly.Name.Value;
            string resourcesName = "FxResources." + assemblyName + ".SR.resources";
            int resourceCount = 0;

            foreach (IResourceReference resourceReference in assembly.Resources)
            {
                resourceCount++;
                if (!resourceReference.Resource.IsInExternalFile && resourceReference.Name.Value.Equals(resourcesName, StringComparison.OrdinalIgnoreCase))
                {
                    const int BUFFERSIZE = 4096;
                    byte[] buffer = new byte[BUFFERSIZE];
                    int index = 0;

                    MemoryStream ms = new MemoryStream(BUFFERSIZE);

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
                    return ms;
                }
            }

            if (resourceCount == 0) // no resources
            {
                containsResources = false;
            }

            return null;
        }

    }
}
