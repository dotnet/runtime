// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO.Compression;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.IL;

using ILCompiler.Win32Resources;
using System.Linq;
using System.IO;

namespace ILCompiler.IBC
{
    static class MIbcProfileParser
    {
        /// <summary>
        /// Parse an MIBC file for the methods that are interesting.
        /// The version bubble must be specified and will describe the restrict the set of methods parsed to those relevant to the compilation
        /// The onlyDefinedInAssembly parameter is used to restrict the set of types parsed to include only those which are defined in a specific module. Specify null to allow definitions from all modules.
        /// This limited parsing is not necessarily an exact set of prevention, so detailed algorithms that work at the individual method level are still necessary, but this allows avoiding excessive parsing.
        ///
        /// The format of the Mibc file is that of a .NET dll, with a global method named "AssemblyDictionary". Inside of that file are a series of references that are broken up by which assemblies define the individual methods.
        /// These references are encoded as IL code that represents the details.
        /// The format of these IL instruction is as follows.
        ///
        /// ldstr mibcGroupName
        /// ldtoken mibcGroupMethod
        /// pop
        /// {Repeat the above pattern N times, once per Mibc group}
        ///
        /// See comment above ReadMIbcGroup for details of the group format
        ///
        /// The mibcGroupName is in the following format "Assembly_{definingAssemblyName};{OtherAssemblyName};{OtherAssemblyName};...; (OtherAssemblyName is ; delimited)
        /// 
        /// </summary>
        /// <returns></returns>
        public static ProfileData ParseMIbcFile(CompilerTypeSystemContext tsc, string filename, HashSet<string> assemblyNamesInVersionBubble, string onlyDefinedInAssembly)
        {
            byte[] peData;

            using (var zipFile = ZipFile.OpenRead(filename))
            {
                var mibcDataEntry = zipFile.GetEntry(Path.GetFileName(filename) + ".dll");
                using (var mibcDataStream = mibcDataEntry.Open())
                {
                    peData = new byte[mibcDataEntry.Length];
                    using (BinaryReader br = new BinaryReader(mibcDataStream))
                    {
                        peData = br.ReadBytes(checked((int)mibcDataEntry.Length));
                    }
                }
            }

            EcmaModule mibcModule = tsc.GetMetadataOnlyModuleFromMemory(filename, peData);
            var assemblyDictionary = (EcmaMethod)mibcModule.GetGlobalModuleType().GetMethod("AssemblyDictionary", null);
            IEnumerable<MethodProfileData> loadedMethodProfileData = Enumerable.Empty<MethodProfileData>();

            EcmaMethodIL ilBody = EcmaMethodIL.Create(assemblyDictionary);
            byte[] ilBytes = ilBody.GetILBytes();
            int currentOffset = 0;

            string mibcGroupName = "";
            while (currentOffset < ilBytes.Length)
            {
                ILOpcode opcode = (ILOpcode)ilBytes[currentOffset];
                if (opcode == ILOpcode.prefix1)
                    opcode = 0x100 + (ILOpcode)ilBytes[currentOffset + 1];
                switch (opcode)
                {
                    case ILOpcode.ldstr:
                        if (mibcGroupName == "")
                        {
                            UInt32 userStringToken = (UInt32)(ilBytes[currentOffset + 1] + (ilBytes[currentOffset + 2] << 8) + (ilBytes[currentOffset + 3] << 16) + (ilBytes[currentOffset + 4] << 24));
                            mibcGroupName = (string)ilBody.GetObject((int)userStringToken);
                        }
                        break;

                    case ILOpcode.ldtoken:
                        if (String.IsNullOrEmpty(mibcGroupName))
                            break;

                        string[] assembliesByName = mibcGroupName.Split(';');
                        
                        bool hasMatchingDefinition = (onlyDefinedInAssembly == null) || assembliesByName[0].Equals(onlyDefinedInAssembly);

                        if (!hasMatchingDefinition)
                            break;

                        bool areAllEntriesInVersionBubble = true;
                        foreach (string s in assembliesByName)
                        {
                            if (string.IsNullOrEmpty(s))
                                continue;

                            if (!assemblyNamesInVersionBubble.Contains(s))
                            {
                                areAllEntriesInVersionBubble = false;
                                break;
                            }
                        }

                        if (!areAllEntriesInVersionBubble)
                            break;

                        uint token = (uint)(ilBytes[currentOffset + 1] + (ilBytes[currentOffset + 2] << 8) + (ilBytes[currentOffset + 3] << 16) + (ilBytes[currentOffset + 4] << 24));
                        loadedMethodProfileData = loadedMethodProfileData.Concat(ReadMIbcGroup(tsc, (EcmaMethod)ilBody.GetObject((int)token)));
                        break;
                    case ILOpcode.pop:
                        mibcGroupName = "";
                        break;
                }

                // This isn't correct if there is a switch opcode, but since we won't do that, its ok
                currentOffset += opcode.GetSize();
            }

            return new IBCProfileData(false, loadedMethodProfileData);
        }

        /// <summary>
        /// Parse MIbcGroup method and return enumerable of MethodProfileData
        ///
        /// Like the AssemblyDictionary method, data is encoded via IL instructions. The format is
        ///
        /// ldtoken methodInProfileData
        /// Any series of instructions that does not include pop
        /// pop
        /// {Repeat N times for N methods described}
        ///
        /// This format is designed to be extensible to hold more data as we add new per method profile data without breaking existing parsers.
        /// </summary>
        static IEnumerable<MethodProfileData> ReadMIbcGroup(TypeSystemContext tsc, EcmaMethod method)
        {
            EcmaMethodIL ilBody = EcmaMethodIL.Create(method);
            byte[] ilBytes = ilBody.GetILBytes();
            int currentOffset = 0;
            object metadataObject = null;
            while (currentOffset < ilBytes.Length)
            {
                ILOpcode opcode = (ILOpcode)ilBytes[currentOffset];
                if (opcode == ILOpcode.prefix1)
                    opcode = 0x100 + (ILOpcode)ilBytes[currentOffset + 1];
                switch (opcode)
                {
                    case ILOpcode.ldtoken:
                        if (metadataObject == null)
                        {
                            uint token = (uint)(ilBytes[currentOffset + 1] + (ilBytes[currentOffset + 2] << 8) + (ilBytes[currentOffset + 3] << 16) + (ilBytes[currentOffset + 4] << 24));
                            metadataObject = ilBody.GetObject((int)token);
                        }
                        break;
                    case ILOpcode.pop:
                        MethodProfileData mibcData = new MethodProfileData((MethodDesc)metadataObject, MethodProfilingDataFlags.ReadMethodCode, 0xFFFFFFFF);
                        yield return mibcData;
                        metadataObject = null;
                        break;
                }

                // This isn't correct if there is a switch opcode, but since we won't do that, its ok
                currentOffset += opcode.GetSize();
            }
        }
    }
}
