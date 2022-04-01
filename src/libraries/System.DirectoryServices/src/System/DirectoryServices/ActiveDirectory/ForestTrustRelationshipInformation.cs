// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Specialized;

using Microsoft.Win32.SafeHandles;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ForestTrustRelationshipInformation : TrustRelationshipInformation
    {
        private TopLevelNameCollection _topLevelNames = new TopLevelNameCollection();
        private StringCollection _excludedNames = new StringCollection();
        private ForestTrustDomainInfoCollection _domainInfo = new ForestTrustDomainInfoCollection();
        private ArrayList _binaryData = new ArrayList();
        private ArrayList _binaryRecordType = new ArrayList();
        private Hashtable _excludedNameTime = new Hashtable();
        private ArrayList _binaryDataTime = new ArrayList();
        internal bool retrieved;

        internal ForestTrustRelationshipInformation(DirectoryContext context, string source, DS_DOMAIN_TRUSTS unmanagedTrust, TrustType type)
        {
            string? tmpDNSName = null;
            string? tmpNetBIOSName = null;

            // security context
            this.context = context;
            // source
            this.source = source;
            // target
            if (unmanagedTrust.DnsDomainName != (IntPtr)0)
                tmpDNSName = Marshal.PtrToStringUni(unmanagedTrust.DnsDomainName);
            if (unmanagedTrust.NetbiosDomainName != (IntPtr)0)
                tmpNetBIOSName = Marshal.PtrToStringUni(unmanagedTrust.NetbiosDomainName);

            this.target = (tmpDNSName == null ? tmpNetBIOSName : tmpDNSName);
            // direction
            if ((unmanagedTrust.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_OUTBOUND) != 0 &&
                (unmanagedTrust.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_INBOUND) != 0)
                direction = TrustDirection.Bidirectional;
            else if ((unmanagedTrust.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_OUTBOUND) != 0)
                direction = TrustDirection.Outbound;
            else if ((unmanagedTrust.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_INBOUND) != 0)
                direction = TrustDirection.Inbound;
            // type
            this.type = type;
        }

        public TopLevelNameCollection TopLevelNames
        {
            get
            {
                if (!retrieved)
                    GetForestTrustInfoHelper();
                return _topLevelNames;
            }
        }

        public StringCollection ExcludedTopLevelNames
        {
            get
            {
                if (!retrieved)
                    GetForestTrustInfoHelper();
                return _excludedNames;
            }
        }

        public ForestTrustDomainInfoCollection TrustedDomainInformation
        {
            get
            {
                if (!retrieved)
                    GetForestTrustInfoHelper();
                return _domainInfo;
            }
        }

        public void Save()
        {
            int count = 0;
            IntPtr records = (IntPtr)0;
            int currentCount = 0;
            IntPtr tmpPtr = (IntPtr)0;
            IntPtr forestInfo = (IntPtr)0;
            SafeLsaPolicyHandle? handle = null;
            IntPtr collisionInfo = (IntPtr)0;
            ArrayList ptrList = new ArrayList();
            ArrayList sidList = new ArrayList();
            bool impersonated = false;
            IntPtr target = (IntPtr)0;
            string? serverName = null;
            IntPtr fileTime = (IntPtr)0;

            // first get the count of all the records
            int toplevelNamesCount = TopLevelNames.Count;
            int excludedNamesCount = ExcludedTopLevelNames.Count;
            int trustedDomainCount = TrustedDomainInformation.Count;
            int binaryDataCount = _binaryData.Count;

            checked
            {
                count += toplevelNamesCount;
                count += excludedNamesCount;
                count += trustedDomainCount;
                count += binaryDataCount;

                // allocate the memory for all the records
                records = Marshal.AllocHGlobal(count * IntPtr.Size);
            }

            try
            {
                try
                {
                    IntPtr ptr = (IntPtr)0;
                    fileTime = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FileTime)));
                    UnsafeNativeMethods.GetSystemTimeAsFileTime(fileTime);

                    // set the time
                    FileTime currentTime = new FileTime();
                    Marshal.PtrToStructure(fileTime, currentTime);

                    for (int i = 0; i < toplevelNamesCount; i++)
                    {
                        // now begin to construct top leve name record
                        LSA_FOREST_TRUST_RECORD record = new LSA_FOREST_TRUST_RECORD();
                        record.Flags = (int)_topLevelNames[i].Status;
                        record.ForestTrustType = LSA_FOREST_TRUST_RECORD_TYPE.ForestTrustTopLevelName;
                        TopLevelName TLN = _topLevelNames[i];
                        record.Time = TLN.time;
                        ptr = Marshal.StringToHGlobalUni(TLN.Name);
                        ptrList.Add(ptr);
                        UnsafeNativeMethods.RtlInitUnicodeString(out record.TopLevelName, ptr);

                        tmpPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_FOREST_TRUST_RECORD)));
                        ptrList.Add(tmpPtr);
                        Marshal.StructureToPtr(record, tmpPtr, false);

                        Marshal.WriteIntPtr(records, IntPtr.Size * currentCount, tmpPtr);

                        currentCount++;
                    }

                    for (int i = 0; i < excludedNamesCount; i++)
                    {
                        // now begin to construct excluded top leve name record
                        LSA_FOREST_TRUST_RECORD record = new LSA_FOREST_TRUST_RECORD();
                        record.Flags = 0;
                        record.ForestTrustType = LSA_FOREST_TRUST_RECORD_TYPE.ForestTrustTopLevelNameEx;
                        if (_excludedNameTime.Contains(_excludedNames[i]!))
                        {
                            record.Time = (LARGE_INTEGER)_excludedNameTime[i]!;
                        }
                        else
                        {
                            record.Time = new LARGE_INTEGER();
                            record.Time.lowPart = currentTime.lower;
                            record.Time.highPart = currentTime.higher;
                        }

                        ptr = Marshal.StringToHGlobalUni(_excludedNames[i]);
                        ptrList.Add(ptr);
                        UnsafeNativeMethods.RtlInitUnicodeString(out record.TopLevelName, ptr);
                        tmpPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_FOREST_TRUST_RECORD)));
                        ptrList.Add(tmpPtr);
                        Marshal.StructureToPtr(record, tmpPtr, false);

                        Marshal.WriteIntPtr(records, IntPtr.Size * currentCount, tmpPtr);

                        currentCount++;
                    }

                    for (int i = 0; i < trustedDomainCount; i++)
                    {
                        // now begin to construct domain info record
                        LSA_FOREST_TRUST_RECORD record = new LSA_FOREST_TRUST_RECORD();
                        record.Flags = (int)_domainInfo[i].Status;
                        record.ForestTrustType = LSA_FOREST_TRUST_RECORD_TYPE.ForestTrustDomainInfo;
                        ForestTrustDomainInformation tmp = _domainInfo[i];
                        record.Time = tmp.time;
                        IntPtr pSid = (IntPtr)0;
                        global::Interop.BOOL result = global::Interop.Advapi32.ConvertStringSidToSid(tmp.DomainSid, out pSid);
                        if (result == global::Interop.BOOL.FALSE)
                        {
                            throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastWin32Error());
                        }
                        record.DomainInfo = new LSA_FOREST_TRUST_DOMAIN_INFO();
                        record.DomainInfo.sid = pSid;
                        sidList.Add(pSid);
                        record.DomainInfo.DNSNameBuffer = Marshal.StringToHGlobalUni(tmp.DnsName);
                        ptrList.Add(record.DomainInfo.DNSNameBuffer);
                        record.DomainInfo.DNSNameLength = (short)(tmp.DnsName == null ? 0 : tmp.DnsName.Length * 2);             // sizeof(WCHAR)
                        record.DomainInfo.DNSNameMaximumLength = (short)(tmp.DnsName == null ? 0 : tmp.DnsName.Length * 2);
                        record.DomainInfo.NetBIOSNameBuffer = Marshal.StringToHGlobalUni(tmp.NetBiosName);
                        ptrList.Add(record.DomainInfo.NetBIOSNameBuffer);
                        record.DomainInfo.NetBIOSNameLength = (short)(tmp.NetBiosName == null ? 0 : tmp.NetBiosName.Length * 2);
                        record.DomainInfo.NetBIOSNameMaximumLength = (short)(tmp.NetBiosName == null ? 0 : tmp.NetBiosName.Length * 2);
                        tmpPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_FOREST_TRUST_RECORD)));
                        ptrList.Add(tmpPtr);
                        Marshal.StructureToPtr(record, tmpPtr, false);

                        Marshal.WriteIntPtr(records, IntPtr.Size * currentCount, tmpPtr);

                        currentCount++;
                    }

                    for (int i = 0; i < binaryDataCount; i++)
                    {
                        LSA_FOREST_TRUST_RECORD record = new LSA_FOREST_TRUST_RECORD();
                        record.Flags = 0;
                        record.Time = (LARGE_INTEGER)_binaryDataTime[i]!;
                        record.Data.Length = ((byte[])_binaryData[i]!).Length;
                        record.ForestTrustType = (LSA_FOREST_TRUST_RECORD_TYPE)_binaryRecordType[i]!;
                        record.Data = new LSA_FOREST_TRUST_BINARY_DATA();
                        if (record.Data.Length == 0)
                        {
                            record.Data.Buffer = (IntPtr)0;
                        }
                        else
                        {
                            record.Data.Buffer = Marshal.AllocHGlobal(record.Data.Length);
                            ptrList.Add(record.Data.Buffer);
                            Marshal.Copy((byte[])_binaryData[i]!, 0, record.Data.Buffer, record.Data.Length);
                        }
                        tmpPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_FOREST_TRUST_RECORD)));
                        ptrList.Add(tmpPtr);
                        Marshal.StructureToPtr(record, tmpPtr, false);

                        Marshal.WriteIntPtr(records, IntPtr.Size * currentCount, tmpPtr);

                        currentCount++;
                    }

                    // finally construct the LSA_FOREST_TRUST_INFORMATION
                    LSA_FOREST_TRUST_INFORMATION trustInformation = new LSA_FOREST_TRUST_INFORMATION();
                    trustInformation.RecordCount = count;
                    trustInformation.Entries = records;
                    forestInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_FOREST_TRUST_INFORMATION)));
                    Marshal.StructureToPtr(trustInformation, forestInfo, false);

                    // get policy server name
                    serverName = Utils.GetPolicyServerName(context, true, true, SourceName);

                    // do impersonation first
                    impersonated = Utils.Impersonate(context);

                    // get the policy handle
                    handle = Utils.GetPolicyHandle(serverName);

                    // get the target name
                    global::Interop.UNICODE_STRING trustedDomainName;
                    target = Marshal.StringToHGlobalUni(TargetName);
                    UnsafeNativeMethods.RtlInitUnicodeString(out trustedDomainName, target);

                    // call the unmanaged function
                    uint error = UnsafeNativeMethods.LsaSetForestTrustInformation(handle, trustedDomainName, forestInfo, 1, out collisionInfo);
                    if (error != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode((int)global::Interop.Advapi32.LsaNtStatusToWinError(error), serverName);
                    }

                    // there is collision, throw proper exception so user can deal with it
                    if (collisionInfo != (IntPtr)0)
                    {
                        throw ExceptionHelper.CreateForestTrustCollisionException(collisionInfo);
                    }

                    // commit the changes
                    error = UnsafeNativeMethods.LsaSetForestTrustInformation(handle, trustedDomainName, forestInfo, 0, out collisionInfo);
                    if (error != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode((int)error, serverName);
                    }

                    // now next time property is invoked, we need to go to the server
                    retrieved = false;
                }
                finally
                {
                    if (impersonated)
                        Utils.Revert();

                    // release the memory
                    for (int i = 0; i < ptrList.Count; i++)
                    {
                        Marshal.FreeHGlobal((IntPtr)ptrList[i]!);
                    }

                    for (int i = 0; i < sidList.Count; i++)
                    {
                        global::Interop.Kernel32.LocalFree((IntPtr)sidList[i]!);
                    }

                    if (records != (IntPtr)0)
                    {
                        Marshal.FreeHGlobal(records);
                    }

                    if (forestInfo != (IntPtr)0)
                    {
                        Marshal.FreeHGlobal(forestInfo);
                    }

                    if (collisionInfo != (IntPtr)0)
                        global::Interop.Advapi32.LsaFreeMemory(collisionInfo);

                    if (target != (IntPtr)0)
                        Marshal.FreeHGlobal(target);

                    if (fileTime != (IntPtr)0)
                        Marshal.FreeHGlobal(fileTime);
                }
            }
            catch { throw; }
        }

        private void GetForestTrustInfoHelper()
        {
            IntPtr forestTrustInfo = (IntPtr)0;
            SafeLsaPolicyHandle? handle = null;
            bool impersonated = false;
            IntPtr targetPtr = (IntPtr)0;
            string? serverName = null;

            TopLevelNameCollection tmpTLNs = new TopLevelNameCollection();
            StringCollection tmpExcludedTLNs = new StringCollection();
            ForestTrustDomainInfoCollection tmpDomainInformation = new ForestTrustDomainInfoCollection();

            // internal members
            ArrayList tmpBinaryData = new ArrayList();
            Hashtable tmpExcludedNameTime = new Hashtable();
            ArrayList tmpBinaryDataTime = new ArrayList();
            ArrayList tmpBinaryRecordType = new ArrayList();

            try
            {
                try
                {
                    // get the target name
                    global::Interop.UNICODE_STRING tmpName;
                    targetPtr = Marshal.StringToHGlobalUni(TargetName);
                    UnsafeNativeMethods.RtlInitUnicodeString(out tmpName, targetPtr);

                    serverName = Utils.GetPolicyServerName(context, true, false, source);

                    // do impersonation
                    impersonated = Utils.Impersonate(context);

                    // get the policy handle
                    handle = Utils.GetPolicyHandle(serverName);

                    uint result = UnsafeNativeMethods.LsaQueryForestTrustInformation(handle, tmpName, ref forestTrustInfo);
                    // check the result
                    if (result != 0)
                    {
                        uint win32Error = global::Interop.Advapi32.LsaNtStatusToWinError(result);
                        if (win32Error != 0)
                        {
                            throw ExceptionHelper.GetExceptionFromErrorCode((int)win32Error, serverName);
                        }
                    }

                    try
                    {
                        if (forestTrustInfo != (IntPtr)0)
                        {
                            LSA_FOREST_TRUST_INFORMATION trustInfo = new LSA_FOREST_TRUST_INFORMATION();
                            Marshal.PtrToStructure(forestTrustInfo, trustInfo);

                            int count = trustInfo.RecordCount;
                            IntPtr addr = (IntPtr)0;
                            for (int i = 0; i < count; i++)
                            {
                                addr = Marshal.ReadIntPtr(trustInfo.Entries, i * IntPtr.Size);
                                LSA_FOREST_TRUST_RECORD record = new LSA_FOREST_TRUST_RECORD();
                                Marshal.PtrToStructure(addr, record);

                                if (record.ForestTrustType == LSA_FOREST_TRUST_RECORD_TYPE.ForestTrustTopLevelName)
                                {
                                    IntPtr myPtr = IntPtr.Add(addr, 16);
                                    Marshal.PtrToStructure(myPtr, record.TopLevelName);
                                    TopLevelName TLN = new TopLevelName(record.Flags, record.TopLevelName, record.Time);
                                    tmpTLNs.Add(TLN);
                                }
                                else if (record.ForestTrustType == LSA_FOREST_TRUST_RECORD_TYPE.ForestTrustTopLevelNameEx)
                                {
                                    // get the excluded TLN and put it in our collection
                                    IntPtr myPtr = IntPtr.Add(addr, 16);
                                    Marshal.PtrToStructure(myPtr, record.TopLevelName);
                                    string excludedName = Marshal.PtrToStringUni(record.TopLevelName.Buffer, record.TopLevelName.Length / 2);
                                    tmpExcludedTLNs.Add(excludedName);
                                    tmpExcludedNameTime.Add(excludedName, record.Time);
                                }
                                else if (record.ForestTrustType == LSA_FOREST_TRUST_RECORD_TYPE.ForestTrustDomainInfo)
                                {
                                    IntPtr myPtr = IntPtr.Add(addr, 16);
                                    Marshal.PtrToStructure(myPtr, record.DomainInfo!);
                                    ForestTrustDomainInformation dom = new ForestTrustDomainInformation(record.Flags, record.DomainInfo!, record.Time);
                                    tmpDomainInformation.Add(dom);
                                }
                                else
                                {
                                    IntPtr myPtr = IntPtr.Add(addr, 16);
                                    Marshal.PtrToStructure(myPtr, record.Data);
                                    int length = record.Data.Length;
                                    byte[] byteArray = new byte[length];
                                    if ((record.Data.Buffer != (IntPtr)0) && (length != 0))
                                    {
                                        Marshal.Copy(record.Data.Buffer, byteArray, 0, length);
                                    }
                                    tmpBinaryData.Add(byteArray);
                                    tmpBinaryDataTime.Add(record.Time);
                                    tmpBinaryRecordType.Add((int)record.ForestTrustType);
                                }
                            }
                        }
                    }
                    finally
                    {
                        global::Interop.Advapi32.LsaFreeMemory(forestTrustInfo);
                    }

                    _topLevelNames = tmpTLNs;
                    _excludedNames = tmpExcludedTLNs;
                    _domainInfo = tmpDomainInformation;

                    _binaryData = tmpBinaryData;
                    _excludedNameTime = tmpExcludedNameTime;
                    _binaryDataTime = tmpBinaryDataTime;
                    _binaryRecordType = tmpBinaryRecordType;

                    // mark it as retrieved
                    retrieved = true;
                }
                finally
                {
                    if (impersonated)
                        Utils.Revert();

                    if (targetPtr != (IntPtr)0)
                    {
                        Marshal.FreeHGlobal(targetPtr);
                    }
                }
            }
            catch { throw; }
        }
    }
}
