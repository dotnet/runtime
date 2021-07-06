// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

// Include the .NET Core version string instead of link because it is "static".
#include "version.c"

CrashReportWriter::CrashReportWriter(CrashInfo& crashInfo) :
    m_crashInfo(crashInfo)
{
    m_crashInfo.AddRef();
}

CrashReportWriter::~CrashReportWriter()
{
    m_crashInfo.Release();
}

//
// Write the crash report info to the json file
//
void
CrashReportWriter::WriteCrashReport(const std::string& dumpFileName)
{
    std::string crashReportFile(dumpFileName);
    crashReportFile.append(".crashreport.json");
    printf("Writing crash report to file %s\n", crashReportFile.c_str());
    try
    {
        JsonWriter writer;
        if (!writer.OpenWriter(crashReportFile.c_str())) {
            return;
        }
        WriteCrashReport(writer);
        writer.CloseWriter();
    }
    catch (const std::exception& e)
    {
        fprintf(stderr, "Writing the crash report file FAILED\n");

        // Delete the partial json file on error
        remove(crashReportFile.c_str());
    }
}

#ifdef __APPLE__

static void
WriteSysctl(const char* sysctlname, JsonWriter& writer, const char* valueName)
{
    size_t size = 0;
    if (sysctlbyname(sysctlname, nullptr, &size, NULL, 0) >= 0)
    {
        ArrayHolder<char> buffer = new char[size];
        if (sysctlbyname(sysctlname, buffer, &size, NULL, 0) >= 0)
        {
            writer.WriteValue(valueName, buffer);
        }
        else
        {
            TRACE("sysctlbyname(%s) 1 FAILED %s\n", sysctlname, strerror(errno));
        }
    }
    else
    {
        TRACE("sysctlbyname(%s) 2 FAILED %s\n", sysctlname, strerror(errno));
    }
}

void
CrashReportWriter::WriteCrashReport(JsonWriter& writer)
{
    const char* exceptionType = nullptr;
    writer.OpenSection("payload");
    writer.WriteValue("protocol_version", "0.0.7");

    writer.OpenSection("configuration");
#if defined(__x86_64__)
    writer.WriteValue("architecture", "amd64");
#elif defined(__aarch64__)
    writer.WriteValue("architecture", "arm64");
#endif
    std::string version;
    assert(strncmp(sccsid, "@(#)Version ", 12) == 0);
    version.append(sccsid + 12);    // skip "@(#)Version "
    version.append(" ");            // the analyzer requires a space after the version
    writer.WriteValue("version", version.c_str());
    writer.CloseSection();          // configuration

    writer.OpenArray("threads");
    for (const ThreadInfo* thread : m_crashInfo.Threads())
    {
        writer.OpenArrayEntry();
        bool crashed = false;
        if (thread->ManagedExceptionObject() != 0)
        {
            crashed = true;
            exceptionType = "0x05000000";   // ManagedException
        }
        else
        {
            if (thread->Tid() == m_crashInfo.CrashThread())
            {
                crashed = true;
                switch (m_crashInfo.Signal())
                {
                case SIGILL:
                    exceptionType = "0x50000000";
                    break;

                case SIGFPE:
                    exceptionType = "0x70000000";
                    break;

                case SIGBUS:
                    exceptionType = "0x60000000";
                    break;

                case SIGTRAP:
                    exceptionType = "0x03000000";
                    break;

                case SIGSEGV:
                    exceptionType = "0x20000000";
                    break;

                case SIGTERM:
                    exceptionType = "0x02000000";
                    break;

                case SIGABRT:
                default:
                    exceptionType = "0x30000000";
                    break;
                }
            }
        }
        writer.WriteValueBool("is_managed", thread->IsManaged());
        writer.WriteValueBool("crashed", crashed);
        if (thread->ManagedExceptionObject() != 0)
        {
            writer.WriteValue64("managed_exception_object", thread->ManagedExceptionObject());
        }
        if (!thread->ManagedExceptionType().empty())
        {
            writer.WriteValue("managed_exception_type", thread->ManagedExceptionType().c_str());
        }
        writer.WriteValue64("native_thread_id", thread->Tid());
        writer.OpenSection("ctx");
        writer.WriteValue64("IP", thread->GetInstructionPointer());
        writer.WriteValue64("SP", thread->GetStackPointer());
        writer.WriteValue64("BP", thread->GetFramePointer());
        writer.CloseSection();      // ctx

        writer.OpenArray("unmanaged_frames");
        for (const StackFrame& frame : thread->StackFrames())
        {
            WriteStackFrame(writer, frame);
        }
        writer.CloseArray();        // unmanaged_frames
        writer.CloseArrayEntry();
    }
    writer.CloseArray();            // threads
    writer.CloseSection();          // payload

    writer.OpenSection("parameters");
    if (exceptionType != nullptr)
    {
        writer.WriteValue("ExceptionType", exceptionType);
    }
    WriteSysctl("kern.osproductversion", writer, "OSVersion");
    WriteSysctl("hw.model", writer, "SystemModel");
    writer.WriteValue("SystemManufacturer", "apple");
    writer.CloseSection();          // parameters
}

void
CrashReportWriter::WriteStackFrame(JsonWriter& writer, const StackFrame& frame)
{ 
    writer.OpenArrayEntry();
    writer.WriteValueBool("is_managed", frame.IsManaged());
    writer.WriteValue64("module_address", frame.ModuleAddress());
    writer.WriteValue64("stack_pointer", frame.StackPointer());
    writer.WriteValue64("native_address", frame.ReturnAddress());
    writer.WriteValue64("native_offset", frame.NativeOffset());
    if (frame.IsManaged())
    {
        writer.WriteValue32("token", frame.Token());
        writer.WriteValue32("il_offset", frame.ILOffset());
    }
    if (frame.ModuleAddress() != 0)
    {
        const ModuleInfo* moduleInfo = m_crashInfo.GetModuleInfoFromBaseAddress(frame.ModuleAddress());
        if (moduleInfo != nullptr)
        {
            std::string moduleName = GetFileName(moduleInfo->ModuleName());
            if (frame.IsManaged())
            {
                writer.WriteValue32("timestamp", moduleInfo->TimeStamp());
                writer.WriteValue32("sizeofimage", moduleInfo->ImageSize());
                writer.WriteValue("filename", moduleName.c_str());
                writer.WriteValue("guid", FormatGuid(moduleInfo->Mvid()).c_str());
            }
            else
            {
                writer.WriteValue("native_module", moduleName.c_str());
            }
        }
    }
    writer.CloseArrayEntry();
}

#else // __APPLE__

void
CrashReportWriter::WriteCrashReport(JsonWriter& writer)
{
}

#endif // __APPLE__
