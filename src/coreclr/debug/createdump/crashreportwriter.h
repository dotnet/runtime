// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

class CrashReportWriter
{
private:
    CrashInfo& m_crashInfo;

public:
    CrashReportWriter(CrashInfo& crashInfo);
    virtual ~CrashReportWriter();
    void WriteCrashReport(const std::string& dumpFileName);

private:
    void WriteCrashReport(JsonWriter& writer);
};
