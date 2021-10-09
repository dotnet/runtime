// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Specialized;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ForestTrustRelationshipInformation : TrustRelationshipInformation
    {
        private TopLevelNameCollection _topLevelNames = new TopLevelNameCollection();
        private StringCollection _excludedNames = new StringCollection();
        private ForestTrustDomainInfoCollection _domainInfo = new ForestTrustDomainInfoCollection();
        private ArrayList _binaryData = new ArrayList();
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
            PolicySafeHandle? handle = null;
            LSA_UNICODE_STRING trustedDomainName;
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
            int binaryDataCount = 0;

            checked
            {
                count += toplevelNamesCount;
                count += excludedNamesCount;
                count += trustedDomainCount;
                if (_binaryData.Count != 0)
                {
                    binaryDataCount = _binaryData.Count;
                    // for the ForestTrustRecordTypeLast record
                    count++;
                    count += binaryDataCount;
                }

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
                        record.TopLevelName = new LSA_UNICODE_STRING();
                        ptr = Marshal.StringToHGlobalUni(TLN.Name);
                        ptrList.Add(ptr);
                        UnsafeNativeMethods.RtlInitUnicodeString(record.TopLevelName, ptr);

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
                        record.TopLevelName = new LSA_UNICODE_STRING();
                        ptr = Marshal.StringToHGlobalUni(_excludedNames[i]);
                        ptrList.Add(ptr);
                        UnsafeNativeMethods.RtlInitUnicodeString(record.TopLevelName, ptr);
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
                        IntPtr stringSid = (IntPtr)0;
                        stringSid = Marshal.StringToHGlobalUni(tmp.DomainSid);
                        ptrList.Add(stringSid);
                        int result = UnsafeNativeMethods.ConvertStringSidToSidW(stringSid, ref pSid);
                        if (result == 0)
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

                    if (binaryDataCount > 0)
                    {
                        // now begin to construct ForestTrustRecordTypeLast
                        LSA_FOREST_TRUST_RECORD lastRecord = new LSA_FOREST_TRUST_RECORD();
                        lastRecord.Flags = 0;
                        lastRecord.ForestTrustType = LSA_FOREST_TRUST_RECORD_TYPE.ForestTrustRecordTypeLast;
                        tmpPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_FOREST_TRUST_RECORD)));
                        ptrList.Add(tmpPtr);
                        Marshal.StructureToPtr(lastRecord, tmpPtr, false);
                        Marshal.WriteIntPtr(records, IntPtr.Size * currentCount, tmpPtr);
                        currentCount++;

                        for (int i = 0; i < binaryDataCount; i++)
                        {
                            // now begin to construct excluded top leve name record
                            LSA_FOREST_TRUST_RECORD record = new LSA_FOREST_TRUST_RECORD();
                            record.Flags = 0;
                            record.Time = (LARGE_INTEGER)_binaryDataTime[i]!;
                            record.Data.Length = ((byte[])_binaryData[i]!).Length;
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
                    handle = new PolicySafeHandle(Utils.GetPolicyHandle(serverName));

                    // get the target name
                    trustedDomainName = new LSA_UNICODE_STRING();
                    target = Marshal.StringToHGlobalUni(TargetName);
                    UnsafeNativeMethods.RtlInitUnicodeString(trustedDomainName, target);

                    // call the unmanaged function
                    int error = UnsafeNativeMethods.LsaSetForestTrustInformation(handle, trustedDomainName, forestInfo, 1, out collisionInfo);
                    if (error != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(UnsafeNativeMethods.LsaNtStatusToWinError(error), serverName);
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
                        throw ExceptionHelper.GetExceptionFromErrorCode(error, serverName);
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
                        UnsafeNativeMethods.LocalFree((IntPtr)sidList[i]!);
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
                        UnsafeNativeMethods.LsaFreeMemory(collisionInfo);

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
            PolicySafeHandle? handle = null;
            LSA_UNICODE_STRING? tmpName = null;
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

            try
            {
                try
                {
                    // get the target name
                    tmpName = new LSA_UNICODE_STRING();
                    targetPtr = Marshal.StringToHGlobalUni(TargetName);
                    UnsafeNativeMethods.RtlInitUnicodeString(tmpName, targetPtr);

                    serverName = Utils.GetPolicyServerName(context, true, false, source);

                    // do impersonation
                    impersonated = Utils.Impersonate(context);

                    // get the policy handle
                    handle = new PolicySafeHandle(Utils.GetPolicyHandle(serverName));

                    int result = UnsafeNativeMethods.LsaQueryForestTrustInformation(handle, tmpName, ref forestTrustInfo);
                    // check the result
                    if (result != 0)
                    {
                        int win32Error = UnsafeNativeMethods.LsaNtStatusToWinError(result);
                        if (win32Error != 0)
                        {
                            throw ExceptionHelper.GetExceptionFromErrorCode(win32Error, serverName);
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
                                    ForestTrustDomainInformation dom = new ForestTrustDomainInformation(record.Flags, record.DomainInfo!, record.Time);
                                    tmpDomainInformation.Add(dom);
                                }
                                else if (record.ForestTrustType == LSA_FOREST_TRUST_RECORD_TYPE.ForestTrustRecordTypeLast)
                                {
                                    // enumeration is done, but we might still have some unrecognized entries after that
                                    continue;
                                }
                                else
                                {
                                    int length = record.Data.Length;
                                    byte[] byteArray = new byte[length];
                                    if ((record.Data.Buffer != (IntPtr)0) && (length != 0))
                                    {
                                        Marshal.Copy(record.Data.Buffer, byteArray, 0, length);
                                    }
                                    tmpBinaryData.Add(byteArray);
                                    tmpBinaryDataTime.Add(record.Time);
                                }
                            }
                        }
                    }
                    finally
                    {
                        UnsafeNativeMethods.LsaFreeMemory(forestTrustInfo);
                    }

                    _topLevelNames = tmpTLNs;
                    _excludedNames = tmpExcludedTLNs;
                    _domainInfo = tmpDomainInformation;

                    _binaryData = tmpBinaryData;
                    _excludedNameTime = tmpExcludedNameTime;
                    _binaryDataTime = tmpBinaryDataTime;

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
