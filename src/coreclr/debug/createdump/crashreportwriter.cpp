// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

// Include the .NET Core version string instead of link because it is "static".
#include "version.c"

CrashReportWriter::CrashReportWriter(CrashInfo& crashInfo) :
    m_crashInfo(crashInfo)
{
    m_fd = -1;
    m_indent = JSON_INDENT_VALUE;
    m_comma = false;
    m_crashInfo.AddRef();
}

CrashReportWriter::~CrashReportWriter()
{
    m_crashInfo.Release();
    if (m_fd != -1)
    {
        close(m_fd);
        m_fd = -1;
    }
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
        if (!OpenWriter(crashReportFile.c_str())) {
            return;
        }
        WriteCrashReport();
        CloseWriter();
    }
    catch (const std::exception& e)
    {
        fprintf(stderr, "Writing the crash report file FAILED\n");

        // Delete the partial json file on error
        remove(crashReportFile.c_str());
    }
}

void
CrashReportWriter::WriteCrashReport()
{
    OpenObject("payload");
    WriteValue("protocol_version", "1.0.0");

    OpenObject("configuration");
#if defined(__x86_64__)
    WriteValue("architecture", "amd64");
#elif defined(__aarch64__)
    WriteValue("architecture", "arm64");
#elif defined(__arm__)
    WriteValue("architecture", "arm");
#endif
    std::string version;
    assert(strncmp(sccsid, "@(#)Version ", 12) == 0);
    version.append(sccsid + 12);    // skip "@(#)Version "
    version.append(" ");            // the analyzer requires a space after the version
    WriteValue("version", version.c_str());
    CloseObject();                  // configuration

    // The main module (if one) was saved away in the crash info
    const ModuleInfo* mainModule = m_crashInfo.MainModule();
    if (mainModule != nullptr && mainModule->BaseAddress() != 0)
    {
        WriteValue("process_name", GetFileName(mainModule->ModuleName()).c_str());
    }
    const char* exceptionType = nullptr;
    OpenArray("threads");
    for (const ThreadInfo* thread : m_crashInfo.Threads())
    {
        OpenObject();
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
        WriteValueBool("is_managed", thread->IsManaged());
        WriteValueBool("crashed", crashed);
        if (thread->ManagedExceptionObject() != 0)
        {
            WriteValue64("managed_exception_object", thread->ManagedExceptionObject());
        }
        if (!thread->ManagedExceptionType().empty())
        {
            WriteValue("managed_exception_type", thread->ManagedExceptionType().c_str());
        }
        if (thread->ManagedExceptionHResult() != 0)
        {
            WriteValue32("managed_exception_hresult", thread->ManagedExceptionHResult());
        }
        WriteValue64("native_thread_id", thread->Tid());
        OpenObject("ctx");
        WriteValue64("IP", thread->GetInstructionPointer());
        WriteValue64("SP", thread->GetStackPointer());
        WriteValue64("BP", thread->GetFramePointer());
        CloseObject();          // ctx

        OpenArray("unmanaged_frames");
        for (const StackFrame& frame : thread->StackFrames())
        {
            WriteStackFrame(frame);
        }
        CloseArray();           // unmanaged_frames
        CloseObject();
    }
    CloseArray();               // threads
    CloseObject();              // payload
#ifdef __APPLE__
    OpenObject("parameters");
    if (exceptionType != nullptr)
    {
        WriteValue("ExceptionType", exceptionType);
    }
    WriteSysctl("kern.osproductversion", "OSVersion");
    WriteSysctl("hw.model", "SystemModel");
    WriteValue("SystemManufacturer", "apple");
    CloseObject();              // parameters
#endif // __APPLE__
}

#ifdef __APPLE__

void
CrashReportWriter::WriteSysctl(const char* sysctlname, const char* valueName)
{
    size_t size = 0;
    if (sysctlbyname(sysctlname, nullptr, &size, NULL, 0) >= 0)
    {
        ArrayHolder<char> buffer = new char[size];
        if (sysctlbyname(sysctlname, buffer, &size, NULL, 0) >= 0)
        {
            WriteValue(valueName, buffer);
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

#endif // __APPLE__

void
CrashReportWriter::WriteStackFrame(const StackFrame& frame)
{
    OpenObject();
    WriteValueBool("is_managed", frame.IsManaged());
    WriteValue64("module_address", frame.ModuleAddress());
    WriteValue64("stack_pointer", frame.StackPointer());
    WriteValue64("native_address", frame.InstructionPointer());
    WriteValue64("native_offset", frame.NativeOffset());
    if (frame.IsManaged())
    {
        WriteValue32("token", frame.Token());
        WriteValue32("il_offset", frame.ILOffset());
    }
    IXCLRDataMethodInstance* pMethod = frame.GetMethod();
    if (pMethod != nullptr)
    {
        ArrayHolder<WCHAR> wszUnicodeName = new WCHAR[MAX_LONGPATH + 1];
        if (SUCCEEDED(pMethod->GetName(0, MAX_LONGPATH, nullptr, wszUnicodeName)))
        {
            std::string methodName = FormatString("%S", wszUnicodeName.GetPtr());
            WriteValue("method_name", methodName.c_str());
        }
    }
    if (frame.ModuleAddress() != 0)
    {
        ModuleInfo* moduleInfo = m_crashInfo.GetModuleInfoFromBaseAddress(frame.ModuleAddress());
        if (moduleInfo != nullptr)
        {
            std::string moduleName = GetFileName(moduleInfo->ModuleName());
            if (frame.IsManaged())
            {
                WriteValue32("timestamp", moduleInfo->TimeStamp());
                WriteValue32("sizeofimage", moduleInfo->ImageSize());
                WriteValue("filename", moduleName.c_str());
                WriteValue("guid", FormatGuid(moduleInfo->Mvid()).c_str());
            }
            else
            {
                const char* symbol = moduleInfo->GetSymbolName(frame.InstructionPointer());
                if (symbol != nullptr)
                {
                    WriteValue("unmanaged_name", symbol);
                    free((void*)symbol);
                }
                WriteValue("native_module", moduleName.c_str());
            }
        }
    }
    CloseObject();
}

bool
CrashReportWriter::OpenWriter(const char* fileName)
{
    m_fd = open(fileName, O_WRONLY|O_CREAT|O_TRUNC, S_IWUSR | S_IRUSR);
    if (m_fd == -1)
    {
        fprintf(stderr, "Could not create json file %s: %d %s\n", fileName, errno, strerror(errno));
        return false;
    }
    Write("{\n");
    return true;
}

void
CrashReportWriter::CloseWriter()
{
    assert(m_indent == JSON_INDENT_VALUE);
    Write("\n}\n");
}

void
CrashReportWriter::Write(const std::string& text)
{
    if (!DumpWriter::WriteData(m_fd, (void*)text.c_str(), text.length()))
    {
        throw std::exception();
    }
}

void
CrashReportWriter::Write(const char* buffer)
{
    std::string text(buffer);
    Write(text);
}

void
CrashReportWriter::Indent(std::string& text)
{
    assert(m_indent >= 0);
    text.append(m_indent, ' ');
}

void
CrashReportWriter::WriteSeperator(std::string& text)
{
    if (m_comma)
    {
        text.append(1, ',');
        text.append(1, '\n');
    }
    Indent(text);
}

void
CrashReportWriter::OpenValue(const char* key, char marker)
{
    std::string text;
    WriteSeperator(text);
    if (key != nullptr)
    {
        text.append("\"");
        text.append(key);
        text.append("\" : ");
    }
    text.append(1, marker);
    text.append(1, '\n');
    m_comma = false;
    m_indent += JSON_INDENT_VALUE;
    Write(text);
}

void
CrashReportWriter::CloseValue(char marker)
{
    std::string text;
    text.append(1, '\n');
    assert(m_indent >= JSON_INDENT_VALUE);
    m_indent -= JSON_INDENT_VALUE;
    Indent(text);
    text.append(1, marker);
    m_comma = true;
    Write(text);
}

void
CrashReportWriter::WriteValue(const char* key, const char* value)
{
    std::string text;
    WriteSeperator(text);
    text.append("\"");
    text.append(key);
    text.append("\" : \"");
    text.append(value);
    text.append("\"");
    m_comma = true;
    Write(text);
}

void
CrashReportWriter::WriteValueBool(const char* key, bool value)
{
    WriteValue(key, value ? "true" : "false");
}

void
CrashReportWriter::WriteValue32(const char* key, uint32_t value)
{
    char buffer[16];
    snprintf(buffer, sizeof(buffer), "0x%x", value);
    WriteValue(key, buffer);
}

void
CrashReportWriter::WriteValue64(const char* key, uint64_t value)
{
    char buffer[32];
    snprintf(buffer, sizeof(buffer), "0x%" PRIx64, value);
    WriteValue(key, buffer);
}

void
CrashReportWriter::OpenObject(const char* key)
{
    OpenValue(key, '{');
}

void
CrashReportWriter::CloseObject()
{
    CloseValue('}');
}

void
CrashReportWriter::OpenArray(const char* key)
{
    OpenValue(key, '[');
}

void
CrashReportWriter::CloseArray()
{
    CloseValue(']');
}
