// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The maps of a compile result: what the JIT reported; each is a packet of the .mc format.

namespace SuperPmi;

public partial class CompileResult
{
    // The maps are serialized in this order.
    internal static readonly MapInfo[] s_maps =
    [
        new MapInfo(0, "AllocGCInfo", PacketId.CR_AllocGCInfo, static () => new LightWeightMap<uint, Agnostic_AllocGCInfo>()),
        new MapInfo(1, "AllocMem", PacketId.CR_AllocMem, static () => new LightWeightMap<uint, Agnostic_AllocMemDetails>()),
        new MapInfo(2, "AllocUnwindInfo", PacketId.CR_AllocUnwindInfo, static () => new DenseLightWeightMap<Agnostic_AllocUnwindInfo>()),
        new MapInfo(3, "AssertLog", PacketId.CR_AssertLog, static () => new DenseLightWeightMap<uint>()),
        new MapInfo(4, "CallLog", PacketId.CR_CallLog, static () => new DenseLightWeightMap<uint>()),
        new MapInfo(5, "CompileMethod", PacketId.CR_CompileMethod, static () => new LightWeightMap<uint, Agnostic_CompileMethodResults>()),
        new MapInfo(6, "MessageLog", PacketId.CR_MessageLog, static () => new DenseLightWeightMap<uint>()),
        new MapInfo(7, "ProcessName", PacketId.CR_ProcessName, static () => new DenseLightWeightMap<uint>()),
        new MapInfo(8, "RecordCallSiteWithSignature", PacketId.CR_RecordCallSiteWithSignature, static () => new LightWeightMap<uint, Agnostic_RecordCallSite>()),
        new MapInfo(9, "RecordCallSiteWithoutSignature", PacketId.CR_RecordCallSiteWithoutSignature, static () => new LightWeightMap<uint, ulong>()),
        new MapInfo(10, "RecordRelocation", PacketId.CR_RecordRelocation, static () => new DenseLightWeightMap<Agnostic_RecordRelocation>()),
        new MapInfo(11, "ReportFatalError", PacketId.CR_ReportFatalError, static () => new DenseLightWeightMap<uint>()),
        new MapInfo(12, "ReportInliningDecision", PacketId.CR_ReportInliningDecision, static () => new DenseLightWeightMap<Agnostic_ReportInliningDecision>()),
        new MapInfo(13, "ReportTailCallDecision", PacketId.CR_ReportTailCallDecision, static () => new DenseLightWeightMap<Agnostic_ReportTailCallDecision>()),
        new MapInfo(14, "ReserveUnwindInfo", PacketId.CR_ReserveUnwindInfo, static () => new DenseLightWeightMap<Agnostic_ReserveUnwindInfo>()),
        new MapInfo(15, "SetBoundaries", PacketId.CR_SetBoundaries, static () => new LightWeightMap<uint, Agnostic_SetBoundaries>()),
        new MapInfo(16, "SetEHcount", PacketId.CR_SetEHcount, static () => new LightWeightMap<uint, uint>()),
        new MapInfo(17, "SetEHinfo", PacketId.CR_SetEHinfo, static () => new LightWeightMap<uint, Agnostic_CORINFO_EH_CLAUSE>()),
        new MapInfo(18, "SetMethodAttribs", PacketId.CR_SetMethodAttribs, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(19, "SetVars", PacketId.CR_SetVars, static () => new LightWeightMap<uint, Agnostic_SetVars>()),
        new MapInfo(20, "SetPatchpointInfo", PacketId.CR_SetPatchpointInfo, static () => new LightWeightMap<uint, Agnostic_SetPatchpointInfo>()),
        new MapInfo(21, "CrSigInstHandleMap", PacketId.CR_CrSigInstHandleMap, static () => new DenseLightWeightMap<ulong>()),
    ];

    private readonly LightWeightMapBuffer[] _maps = new LightWeightMapBuffer[s_maps.Length];

    public LightWeightMap<uint, Agnostic_AllocGCInfo> AllocGCInfo { get => (LightWeightMap<uint, Agnostic_AllocGCInfo>)_maps[0]; set => _maps[0] = value; }
    public LightWeightMap<uint, Agnostic_AllocMemDetails> AllocMem { get => (LightWeightMap<uint, Agnostic_AllocMemDetails>)_maps[1]; set => _maps[1] = value; }
    public DenseLightWeightMap<Agnostic_AllocUnwindInfo> AllocUnwindInfo { get => (DenseLightWeightMap<Agnostic_AllocUnwindInfo>)_maps[2]; set => _maps[2] = value; }
    public DenseLightWeightMap<uint> AssertLog { get => (DenseLightWeightMap<uint>)_maps[3]; set => _maps[3] = value; }
    public DenseLightWeightMap<uint> CallLog { get => (DenseLightWeightMap<uint>)_maps[4]; set => _maps[4] = value; }
    public LightWeightMap<uint, Agnostic_CompileMethodResults> CompileMethod { get => (LightWeightMap<uint, Agnostic_CompileMethodResults>)_maps[5]; set => _maps[5] = value; }
    public DenseLightWeightMap<uint> MessageLog { get => (DenseLightWeightMap<uint>)_maps[6]; set => _maps[6] = value; }
    public DenseLightWeightMap<uint> ProcessName { get => (DenseLightWeightMap<uint>)_maps[7]; set => _maps[7] = value; }
    public LightWeightMap<uint, Agnostic_RecordCallSite> RecordCallSiteWithSignature { get => (LightWeightMap<uint, Agnostic_RecordCallSite>)_maps[8]; set => _maps[8] = value; }
    public LightWeightMap<uint, ulong> RecordCallSiteWithoutSignature { get => (LightWeightMap<uint, ulong>)_maps[9]; set => _maps[9] = value; }
    public DenseLightWeightMap<Agnostic_RecordRelocation> RecordRelocation { get => (DenseLightWeightMap<Agnostic_RecordRelocation>)_maps[10]; set => _maps[10] = value; }
    public DenseLightWeightMap<uint> ReportFatalError { get => (DenseLightWeightMap<uint>)_maps[11]; set => _maps[11] = value; }
    public DenseLightWeightMap<Agnostic_ReportInliningDecision> ReportInliningDecision { get => (DenseLightWeightMap<Agnostic_ReportInliningDecision>)_maps[12]; set => _maps[12] = value; }
    public DenseLightWeightMap<Agnostic_ReportTailCallDecision> ReportTailCallDecision { get => (DenseLightWeightMap<Agnostic_ReportTailCallDecision>)_maps[13]; set => _maps[13] = value; }
    public DenseLightWeightMap<Agnostic_ReserveUnwindInfo> ReserveUnwindInfo { get => (DenseLightWeightMap<Agnostic_ReserveUnwindInfo>)_maps[14]; set => _maps[14] = value; }
    public LightWeightMap<uint, Agnostic_SetBoundaries> SetBoundaries { get => (LightWeightMap<uint, Agnostic_SetBoundaries>)_maps[15]; set => _maps[15] = value; }
    public LightWeightMap<uint, uint> SetEHcount { get => (LightWeightMap<uint, uint>)_maps[16]; set => _maps[16] = value; }
    public LightWeightMap<uint, Agnostic_CORINFO_EH_CLAUSE> SetEHinfo { get => (LightWeightMap<uint, Agnostic_CORINFO_EH_CLAUSE>)_maps[17]; set => _maps[17] = value; }
    public LightWeightMap<ulong, uint> SetMethodAttribs { get => (LightWeightMap<ulong, uint>)_maps[18]; set => _maps[18] = value; }
    public LightWeightMap<uint, Agnostic_SetVars> SetVars { get => (LightWeightMap<uint, Agnostic_SetVars>)_maps[19]; set => _maps[19] = value; }
    public LightWeightMap<uint, Agnostic_SetPatchpointInfo> SetPatchpointInfo { get => (LightWeightMap<uint, Agnostic_SetPatchpointInfo>)_maps[20]; set => _maps[20] = value; }
    public DenseLightWeightMap<ulong> CrSigInstHandleMap { get => (DenseLightWeightMap<ulong>)_maps[21]; set => _maps[21] = value; }

    internal void dumpMaps()
    {
        MapDumper.Dump(nameof(AllocGCInfo), AllocGCInfo, (k, v) => dmpAllocGCInfo(k, v));
        MapDumper.Dump(nameof(AllocMem), AllocMem, (k, v) => dmpAllocMem(k, v));
        MapDumper.Dump(nameof(AllocUnwindInfo), AllocUnwindInfo, (k, v) => dmpAllocUnwindInfo(k, v));
        MapDumper.Dump(nameof(AssertLog), AssertLog, (k, v) => dmpAssertLog(k, v));
        MapDumper.Dump(nameof(CallLog), CallLog, (k, v) => dmpCallLog(k, v));
        MapDumper.Dump(nameof(CompileMethod), CompileMethod, (k, v) => dmpCompileMethod(k, v));
        MapDumper.Dump(nameof(MessageLog), MessageLog, (k, v) => dmpMessageLog(k, v));
        MapDumper.Dump(nameof(ProcessName), ProcessName, (k, v) => dmpProcessName(k, v));
        MapDumper.Dump(nameof(RecordCallSiteWithSignature), RecordCallSiteWithSignature, (k, v) => dmpRecordCallSiteWithSignature(k, v));
        MapDumper.Dump(nameof(RecordCallSiteWithoutSignature), RecordCallSiteWithoutSignature, (k, v) => dmpRecordCallSiteWithoutSignature(k, v));
        MapDumper.Dump(nameof(RecordRelocation), RecordRelocation, (k, v) => dmpRecordRelocation(k, v));
        MapDumper.Dump(nameof(ReportFatalError), ReportFatalError, (k, v) => dmpReportFatalError(k, v));
        MapDumper.Dump(nameof(ReportInliningDecision), ReportInliningDecision, (k, v) => dmpReportInliningDecision(k, v));
        MapDumper.Dump(nameof(ReportTailCallDecision), ReportTailCallDecision, (k, v) => dmpReportTailCallDecision(k, v));
        MapDumper.Dump(nameof(ReserveUnwindInfo), ReserveUnwindInfo, (k, v) => dmpReserveUnwindInfo(k, v));
        MapDumper.Dump(nameof(SetBoundaries), SetBoundaries, (k, v) => dmpSetBoundaries(k, v));
        MapDumper.Dump(nameof(SetEHcount), SetEHcount, (k, v) => dmpSetEHcount(k, v));
        MapDumper.Dump(nameof(SetEHinfo), SetEHinfo, (k, v) => dmpSetEHinfo(k, v));
        MapDumper.Dump(nameof(SetMethodAttribs), SetMethodAttribs, (k, v) => dmpSetMethodAttribs(k, v));
        MapDumper.Dump(nameof(SetVars), SetVars, (k, v) => dmpSetVars(k, v));
        MapDumper.Dump(nameof(SetPatchpointInfo), SetPatchpointInfo, (k, v) => dmpSetPatchpointInfo(k, v));
        MapDumper.Dump(nameof(CrSigInstHandleMap), CrSigInstHandleMap, (k, v) => dmpCrSigInstHandleMap(k, v));
    }
}
