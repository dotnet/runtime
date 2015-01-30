//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// ngenparser.inl -- A parser for NGen commands
//
//*****************************************************************************

//  statics
//

namespace
{
    wchar_t const * FIXUPS_STAT_OPTION = W("fixups");
    wchar_t const * CALLS_STAT_OPTION = W("calls");
    wchar_t const * ATTRIB_STAT_OPTION = W("attributed");
    wchar_t const * ALL_STAT_OPTION = W("all");
}

BOOL IsExe(const WCHAR *path)
{
    StackSString s(path);
    SString::Iterator i;

    if (s.GetCount() > 4)
    {
        i = s.End() - 4;
        return s.MatchCaseInsensitive(i, SL(W(".exe")));
    }
    return false;
}

BOOL IsDll(const WCHAR *path)
{
    StackSString s(path);
    SString::Iterator i;

    if (s.GetCount() > 4)
    {
        i = s.End() - 4;
        return s.MatchCaseInsensitive(i, SL(W(".dll")));
    }
    return false;
}

BOOL IsWinMD(const WCHAR *path)
{
    StackSString s(path);
    SString::Iterator i;

    if (s.GetCount() > 6)
    {
        i = s.End() - 6;
        return s.MatchCaseInsensitive(i, SL(W(".winmd")));
    }
    return false;
}

BOOL IsExeOrDllOrWinMD(const WCHAR *path)
{
    return (IsExe(path) || IsDll(path) || IsWinMD(path));
}


void CanonicalizePathAux(const WCHAR *path, SString &s, BOOL bForceFileFound)
{
    const int FULLPATH_BUFFER_SIZE = MAX_PATH+1;
    WCHAR *   pwszFileName = NULL;
    WCHAR     wszFullPath[FULLPATH_BUFFER_SIZE];

    DWORD nRet = WszGetFullPathName(path,
                                    FULLPATH_BUFFER_SIZE,
                                    wszFullPath,
                                    &pwszFileName);

    if (nRet != 0 && nRet <= FULLPATH_BUFFER_SIZE) {
        // GetFullPathName merges the name of the current drive and directory with the specified
        // file name to determine the full path and file name of the specified file.
        // This function does not verify that the resulting path and file name are valid or that
        // they see an existing file on the associated volume.

        // Now let's verify that we do have a file pointed by the fullpath we just constructed.

        DWORD attributes = WszGetFileAttributes(wszFullPath);
        if (attributes != INVALID_FILE_ATTRIBUTES) {
            // Found the file, return the path.
            s.Set(wszFullPath);
            return;
        }
    }

    if (bForceFileFound) {
        StackSString ssMsg(W("Error: The specified file or directory \""));
        ssMsg.Append(path);
        ssMsg.Append(W("\" is invalid."));

        ThrowHR(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND), ssMsg);
    }
    else {
        s.Set(path);  // return the input path.
    }
}

void CanonicalizePath(const WCHAR *path, SString &s, BOOL bForceFileFound)
{
    //Instead of checking to see if it's a dll or exe, actually check to see if the file exists.  Otherwise
    //you can have a file that doesn't end in dll or exe stay as a relative path and then assert in fusion.
    DWORD attributes = WszGetFileAttributes(path);

    //Never treat a name ending in ".exe" or ".dll" as a strong name.  This handles typos in filenames.
    //Otherwise if you do install /queue on a filename and mistype it you only get the error on eqi/idle.
    if ((attributes == INVALID_FILE_ATTRIBUTES || ((attributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY)) && !IsExeOrDllOrWinMD(path))
    {
        s.Set(path);  // return the input path.
    }
    else
    {
        CanonicalizePathAux(path, s, bForceFileFound);
    }
}

struct NewCommandLineOptions
{
    enum NGenAction
    {
        Action_Install,
        Action_Uninstall,
        Action_Update,
        Action_Display,
        Action_Finish,
        Action_Queue,
        Action_CreatePdb,
        Action_RemoveTaskBootTrigger,
        Action_RemoveTaskDelayStartTrigger,
    };

    NGenAction           ngenAction;
    BSTRHolder           BSTRassemblyName;
    OptimizationScenario optScenario;
    UpdateFlags          updateFlags;
    GeneralFlags         generalFlags;
    BSTRHolder           BSTRconfig;
    BSTRHolder           BSTRRepositoryDir;
    RepositoryFlags      repositoryFlags;
    BSTRHolder           BSTRRuntimeVersion;
    BSTRHolder           BSTRPackageMoniker;
    BSTRHolder           BSTRLocalAppData;

    NGenPrivateAttributesClass ngenPrivateAttributes;

    BOOL                 defer;
    BOOL                 delay;
    BOOL                 deferAlwaysOK;
    BOOL                 noLogo;
    BOOL                 legacyServiceBehavior;
    
    CorSvcLogLevel       logLevel;
    PriorityLevel        priority;
    ControlServiceAction serviceAction;

    BSTRHolder           nativeImagePath;
    BSTRHolder           pdbPath;
    BOOL                 pdbLines;
    BSTRHolder           managedPdbSearchPath;

    NewCommandLineOptions() :
        optScenario(ScenarioDefault),
        updateFlags(UpdateDefault),
        defer(FALSE),
        delay(FALSE),
        deferAlwaysOK(FALSE),
        priority(Priority_Default),
        noLogo(FALSE),
        generalFlags(AllowPartialNames),
        repositoryFlags(RepositoryDefault),
        logLevel(LogLevel_Success),
        serviceAction(Service_NoAction),
        legacyServiceBehavior(FALSE),
        pdbLines(FALSE)
    {
    }
};



//****************************************************************************


// The INGenParserCallback is an abstract class that tell the NGen parser how to
// handle output-related issues.
class INGenParserCallback
{
public:
    virtual void Output(const SString& str) = 0;
    virtual void PrintLogo()                = 0;
    virtual void PrintUsage()               = 0;
    virtual void SetLoggingToFile(BOOL)     = 0;
};

// The NGen parser is a class that parses and executes NGen commands.  The two primary methods are
// ParseNewCommandLine which parses NGen commands, and ProcessNewCommandLineOptions which executes
// the NGen command.  The user of the parser class is free to modify the parse output (NewCommandLineOptions)
// before passing it to ProcessNewCommandLineOptions.
class NGenParser
{
public:

    NGenParser(INGenParserCallback *parserCallback)
    {
        this->parserCallback = parserCallback;
    }

    bool ParseNewCommandLine(int argc, LPCWSTR argv[], NewCommandLineOptions &opt)
    {
        return ParseNewCommandLineHelper(argc, argv, opt);
    }

    void ProcessNewCommandLineOptions(NewCommandLineOptions &opt,
                                      ICorSvc *pCorSvc,
                                      ICompileProgressNotification *pCompileProgressNotification,
                                      ICorSvcLogger                *pCorSvcLogger)
    {
        ProcessNewCommandLineOptionsHelper(opt, pCorSvc, pCompileProgressNotification, pCorSvcLogger);
    }

private:

    void SetLoggingToFile(BOOL fLoggingToFile)
    {
        parserCallback->SetLoggingToFile(fLoggingToFile);
    }

    void PrintLogoHelper()
    {
        parserCallback->PrintLogo();
    }

    void PrintUsageHelper()
    {
        parserCallback->PrintUsage();
    }

    void Output(const SString& str)
    {
        parserCallback->Output(str);
    }

    void Output(LPCSTR str)
    {
        Output(SString(SString::Ascii, str));
    }

    void Output(LPCWSTR str)
    {
        Output(SString(str));
    }

    void Outputf(__in_z LPWSTR szFormat, ...)
    {
        StackSString formatted;

        va_list args;
        va_start(args, szFormat);
        formatted.VPrintf(szFormat, args);
        va_end(args);

        Output(formatted);
    }

private:

    LPCSTR ServiceStatusString(DWORD dwState)
    {
        switch (dwState)
        {
        case SERVICE_CONTINUE_PENDING: return "The .NET Runtime Optimization Service continue is pending.\n";
        case SERVICE_PAUSE_PENDING:    return "The .NET Runtime Optimization Service pause is pending.\n";
        case SERVICE_PAUSED:           return "The .NET Runtime Optimization Service is started and paused.\n";
        case SERVICE_RUNNING:          return "The .NET Runtime Optimization Service is running.\n";
        case SERVICE_START_PENDING:    return "The .NET Runtime Optimization Service is starting.\n";
        case SERVICE_STOP_PENDING:     return "The .NET Runtime Optimization Service is stopping.\n";
        case SERVICE_STOPPED:          return "The .NET Runtime Optimization Service is stopped.\n";
        }

        return "Unknown state";
    }

    bool IsScenario(LPCWSTR arg, OptimizationScenario *optScenario)
    {
        DWORD *pScenario = (DWORD *) optScenario;

        if ((arg[0] == W('-')) || (arg[0] == W('/')))
        {
            arg++;

            if (SString::_wcsicmp(arg, W("Debug")) == 0)
            {
                *pScenario |= ScenarioDebug;
            }
            else if (SString::_wcsicmp(arg, W("Profile")) == 0)
            {
                *pScenario |= ScenarioProfile;
            }
            else if (SString::_wcsicmp(arg, W("Tuning")) == 0)
            {
                *pScenario |= ScenarioTuningDataCollection;
            }
            else if (SString::_wcsicmp(arg, W("NoDependencies")) == 0)
            {
                *pScenario |= ScenarioLegacy;
            }
            else
            {
                return false;
            }

            return true;
        }

        return false;
    }

    bool IsCommandLineOption(LPCWSTR arg, NewCommandLineOptions &options)
    {
        if (options.ngenAction == NewCommandLineOptions::Action_Finish)
        {
            if (arg[0] == W('0')) { options.priority = Priority_0; return true;}
            if (arg[0] == W('1')) { options.priority = Priority_1; return true;}
            if (arg[0] == W('2')) { options.priority = Priority_2; return true;}
            if (arg[0] == W('3')) { options.priority = Priority_3; return true;}
        }

        if (options.ngenAction == NewCommandLineOptions::Action_CreatePdb) 
        {
            if (options.nativeImagePath == NULL) {

                options.nativeImagePath.Assign(::SysAllocString(arg));
                return true;

            } else if (options.pdbPath == NULL) {

                options.pdbPath.Assign(::SysAllocString(arg));
                return true;

            } else if (options.pdbLines && (options.managedPdbSearchPath == NULL)) {

                options.managedPdbSearchPath.Assign(::SysAllocString(arg));
                return true;
            }
        }

        if (options.ngenAction == NewCommandLineOptions::Action_Queue)
        {
            if (SString::_wcsicmp(arg, W("scmstart")) == 0)
            {
                options.serviceAction = Service_Start;
                return true;
            }
            else if (SString::_wcsicmp(arg, W("scmpause")) == 0)
            {
                options.serviceAction = Service_Pause;
                return true;
            }
            else if (SString::_wcsicmp(arg, W("scmstop")) == 0)
            {
                options.serviceAction = Service_Stop;
                return true;
            }
            else if (SString::_wcsicmp(arg, W("scmcontinue")) == 0)
            {
                options.serviceAction = Service_Continue;
                return true;
            }
            else if (SString::_wcsicmp(arg, W("scmstatus")) == 0)
            {
                options.serviceAction = Service_Interrogate;
                return true;
            }
            else if (SString::_wcsicmp(arg, W("pause")) == 0)
            {
                options.serviceAction = Service_StartPaused;
                return true;
            }
            else if (SString::_wcsicmp(arg, W("continue")) == 0)
            {
                options.serviceAction = Service_Continue;
                return true;
            }
            else if (SString::_wcsicmp(arg, W("status")) == 0)
            {
                options.serviceAction = Service_Interrogate;
                return true;
            }
        }

        if ((arg[0] == W('-')) || (arg[0] == W('/')))
        {
            arg++;

            if (SString::_wcsicmp(arg, W("Silent")) == 0)
            {
                options.logLevel = LogLevel_Warning;
            } else if (SString::_wcsicmp(arg, W("Verbose")) == 0)
            {
                options.logLevel = LogLevel_Info;
            } else if ((options.ngenAction == NewCommandLineOptions::Action_Update) && (SString::_wcsicmp(arg, W("Force")) == 0)) {
                options.updateFlags = (UpdateFlags)(options.updateFlags | Force);
            }
            else if ((options.ngenAction == NewCommandLineOptions::Action_Update) && (_wcsicmp(arg, W("Postreboot")) == 0))
            {
                options.priority = Priority_Default;
                options.updateFlags = (UpdateFlags)(options.updateFlags | PostReboot);
            }
            else if ( (options.ngenAction == NewCommandLineOptions::Action_Install ||
                       options.ngenAction == NewCommandLineOptions::Action_Update) &&
                      SString::_wcsicmp(arg, W("Queue")) == 0)
            {
                options.defer = TRUE;
                options.priority = Priority_Default;
            } else if (options.ngenAction == NewCommandLineOptions::Action_Install &&
                       SString::_wcsicmp(arg, W("Queue:1")) == 0)
            {
                options.defer = TRUE;
                options.priority = Priority_1;
            } else if (options.ngenAction == NewCommandLineOptions::Action_Install &&
                       SString::_wcsicmp(arg, W("Queue:2")) == 0)
            {
                options.defer = TRUE;
                options.priority = Priority_2;
            } else if (options.ngenAction == NewCommandLineOptions::Action_Install &&
                       SString::_wcsicmp(arg, W("Queue:3")) == 0)
            {
                options.defer = TRUE;
                options.priority = Priority_3;
            } else if (options.ngenAction == NewCommandLineOptions::Action_Update &&
                       SString::_wcsicmp(arg, W("Delay")) == 0)
            {
                options.delay = TRUE;
            } else if (options.ngenAction == NewCommandLineOptions::Action_Install && 
                       SString::_wcsicmp(arg, W("NetfxPri1")) == 0)
            {
                options.generalFlags = (GeneralFlags)(options.generalFlags | KeepPriority);
            } else if ((options.ngenAction == NewCommandLineOptions::Action_CreatePdb) &&
                       SString::_wcsicmp(arg, W("lines")) == 0)
            {
                options.pdbLines = TRUE;
            } else if (SString::_wcsicmp(arg, W("NoLogo")) == 0)
            {
                options.noLogo = TRUE;
            } else if (SString::_wcsicmp(arg, W("NoRoot")) == 0)
            {
                options.generalFlags = (GeneralFlags)(options.generalFlags | NoRoot);
            } else if (SString::_wcsicmp(arg, W("LegacyServiceBehavior")) == 0)
            {
                options.legacyServiceBehavior = TRUE;
            } else
            {
                // See if the input is one of:
                //  /ExeConfig:<exe>
                //  /AppBase:<dir>
                //  /MoveFromRepository:<dir>
                //  /CopyFromRepository:<dir>
                //  /CopyToRepository:<dir>
                //  /Stats:<stats>
                //  /Version:<runtime version>
                //  /Package:<package moniker>
                //  /LocalAppData:<local app data directory>
                StackSString ssArg(arg);
                StackSString ssExeConfig(W("ExeConfig:"));
                StackSString ssAppBase(W("AppBase:"));
                StackSString ssMoveFromRepository(W("MoveFromRepository:"));
                StackSString ssCopyFromRepository(W("CopyFromRepository:"));
                StackSString ssCopyToRepository(W("CopyToRepository:"));
                StackSString ssVersion(W("Version:"));
                StackSString ssPackage(W("Package:"));
                StackSString ssLocalAppData(W("LocalAppData:"));
                StackSString ssStats(W("Stats"));
                StackSString ssStatsWithOption(ssStats); ssStatsWithOption += W(':');

                if (ssArg.MatchCaseInsensitive(ssArg.Begin(), ssExeConfig))
                {
                    if (IsNgenOffline())
                    {
                        ThrowHR(E_INVALIDARG, SL(W("Error: Cannot use /ExeConfig with the Ngen Offline feature.")));
                    }
                    
                    if (!options.BSTRconfig.IsNull()) {
                        PrintLogoHelper();
                        ThrowHR(E_INVALIDARG, SL(W("Error: Cannot specify both /ExeConfig and /AppBase")));
                    }

                    ssArg.Delete(ssArg.Begin(), ssExeConfig.GetCount());

                    if (!IsExe(ssArg.GetUnicode())) {
                        PrintLogoHelper();
                        ThrowHR(E_INVALIDARG, SL(W("Error: /ExeConfig specified without an executable")));
                    }

                    StackSString ss;
                    EX_TRY
                    {
                        CanonicalizePath(ssArg.GetUnicode(), ss, TRUE);
                    }
                    EX_CATCH
                    {
                        PrintLogoHelper();
                        EX_RETHROW;
                    }
                    EX_END_CATCH(SwallowAllExceptions)

                    options.BSTRconfig.Assign(::SysAllocString(ss.GetUnicode()));
                    return true;
                }
                else if (ssArg.MatchCaseInsensitive(ssArg.Begin(), ssAppBase))
                {
                    if (IsNgenOffline())
                    {
                        ThrowHR(E_INVALIDARG, SL(W("Error: Cannot use /AppBase with the Ngen Offline feature.")));
                    }
                    
                    if (!options.BSTRconfig.IsNull()) {
                        PrintLogoHelper();
                        ThrowHR(E_INVALIDARG, SL(W("Error: Cannot specify both /ExeConfig and /AppBase")));
                    }

                    ssArg.Delete(ssArg.Begin(), ssAppBase.GetCount());

                    StackSString ss;

                    EX_TRY
                    {
                        CanonicalizePathAux(ssArg.GetUnicode(), ss, TRUE);
                    }
                    EX_CATCH
                    {
                        PrintLogoHelper();
                        EX_RETHROW;
                    }
                    EX_END_CATCH(SwallowAllExceptions)

                    DWORD attributes = WszGetFileAttributes(ss.GetUnicode());
                    if ((attributes == INVALID_FILE_ATTRIBUTES) ||
                        ((attributes & FILE_ATTRIBUTE_DIRECTORY) != FILE_ATTRIBUTE_DIRECTORY)) {
                        PrintLogoHelper();
                        ThrowHR(E_INVALIDARG, SL(W("Error: /AppBase specified without a valid directory")));
                    }

                    options.BSTRconfig.Assign(::SysAllocString(ss.GetUnicode()));
                    return true;
                }
                else if (ssArg.MatchCaseInsensitive(ssArg.Begin(), ssMoveFromRepository))
                {
                    if (options.BSTRRepositoryDir != NULL)
                        ThrowHR(E_INVALIDARG, SL(W("Error: Cannot specify multiple repository options")));

                    StackSString ssFullPath;
                    CanonicalizePathAux(ssArg.GetUnicode() + ssMoveFromRepository.GetCount(), ssFullPath, FALSE);
                    options.BSTRRepositoryDir = ::SysAllocString(ssFullPath.GetUnicode());
                    options.repositoryFlags = (RepositoryFlags)(options.repositoryFlags | MoveFromRepository);
                    return true;
                }
                else if (ssArg.MatchCaseInsensitive(ssArg.Begin(), ssCopyFromRepository))
                {
                    if (options.BSTRRepositoryDir != NULL)
                        ThrowHR(E_INVALIDARG, SL(W("Error: Cannot specify multiple repository options")));

                    StackSString ssFullPath;
                    CanonicalizePathAux(ssArg.GetUnicode() + ssCopyFromRepository.GetCount(), ssFullPath, FALSE);
                    options.BSTRRepositoryDir = ::SysAllocString(ssFullPath.GetUnicode());
                    return true;
                }
                else if (ssArg.MatchCaseInsensitive(ssArg.Begin(), ssCopyToRepository))
                {
                    if (options.BSTRRepositoryDir != NULL)
                        ThrowHR(E_INVALIDARG, SL(W("Error: Cannot specify multiple repository options")));

                    StackSString ssFullPath;
                    CanonicalizePathAux(ssArg.GetUnicode() + ssCopyToRepository.GetCount(), ssFullPath, TRUE);
                    options.BSTRRepositoryDir = ::SysAllocString(ssFullPath.GetUnicode());
                    options.repositoryFlags = (RepositoryFlags)(options.repositoryFlags | CopyToRepository);

                    if (options.ngenAction == NewCommandLineOptions::Action_Update)
                        options.updateFlags = (UpdateFlags)(options.updateFlags | Force);

                    return true;
                }
                else if (ssArg.MatchCaseInsensitive(ssArg.Begin(), ssVersion))
                {
                    if (options.BSTRRuntimeVersion != NULL)
                        ThrowHR(E_INVALIDARG, SL(W("Error: Cannot specify multiple runtime versions")));

                    ssArg.Delete(ssArg.Begin(), ssVersion.GetCount());
                    options.BSTRRuntimeVersion = ::SysAllocString(ssArg.GetUnicode());

                    return true;
                }
                else if (ssArg.MatchCaseInsensitive(ssArg.Begin(), ssPackage))
                {
                    if (options.BSTRPackageMoniker != NULL)
                        ThrowHR(E_INVALIDARG, SL(W("Error: Cannot specify multiple package monikers")));

                    ssArg.Delete(ssArg.Begin(), ssPackage.GetCount());
                    options.BSTRPackageMoniker = ::SysAllocString(ssArg.GetUnicode());

                    return true;
                }
                else if (ssArg.MatchCaseInsensitive(ssArg.Begin(), ssLocalAppData))
                {
                    if (options.BSTRLocalAppData != NULL)
                        ThrowHR(E_INVALIDARG, SL(W("Error: Cannot specify multiple localappdata directories")));

                    ssArg.Delete(ssArg.Begin(), ssLocalAppData.GetCount());
                    options.BSTRLocalAppData = ::SysAllocString(ssArg.GetUnicode());

                    return true;
                }
                else if (ssArg.EqualsCaseInsensitive(ssStats))
                {
                    options.ngenPrivateAttributes.ZapStats = ZapperOptions::DEFAULT_STATS;
                    return true;
                }
                else if (ssArg.MatchCaseInsensitive(ssArg.Begin(), ssStatsWithOption))
                {
                    ssArg.Delete(ssArg.Begin(), ssStatsWithOption.GetCount());

                    if (ssArg.EqualsCaseInsensitive(FIXUPS_STAT_OPTION))
                    {
                        options.ngenPrivateAttributes.ZapStats = ZapperOptions::FIXUP_STATS;
                    }
                    else if (ssArg.EqualsCaseInsensitive(CALLS_STAT_OPTION))
                    {
                        options.ngenPrivateAttributes.ZapStats = ZapperOptions::CALL_STATS;
                    }
                    else if (ssArg.EqualsCaseInsensitive(ATTRIB_STAT_OPTION))
                    {
                        options.ngenPrivateAttributes.ZapStats = ZapperOptions::ATTRIB_STATS;
                    }
                    else if (ssArg.EqualsCaseInsensitive(ALL_STAT_OPTION))
                    {
                        options.ngenPrivateAttributes.ZapStats = ZapperOptions::ALL_STATS;
                    }
                    else
                    {
                        // We have an option of the form "/Stats:<int>"
                        int statsVal = _wtoi(ssArg.GetUnicode());
                        if (statsVal == 0) {
                            PrintLogoHelper();
                            ThrowHR(E_INVALIDARG, SL(W("Error: Unrecognized option used for /Stats:<option>")));
                        }
                        options.ngenPrivateAttributes.ZapStats = statsVal;
                    }

                    return true;
                }

                return false;
            }

            return true;
        }

        return false;
    }

    bool ParseNewCommandLineHelper(int argc, LPCWSTR argv[], NewCommandLineOptions &opt)
    {
        if (argc < 1) return false;

        // Determine the action
        LPCWSTR action = argv[0];
        argc--;
        argv++;

        if (SString::_wcsicmp(action, W("install")) == 0)
            opt.ngenAction = NewCommandLineOptions::Action_Install;
        else if (SString::_wcsicmp(action, W("uninstall")) == 0)
        {
            opt.ngenAction = NewCommandLineOptions::Action_Uninstall;
            opt.optScenario = ScenarioAll;
        }
        else if (SString::_wcsicmp(action, W("update")) == 0)
            opt.ngenAction = NewCommandLineOptions::Action_Update;
        else  if (SString::_wcsicmp(action, W("display")) == 0)
        {
            opt.ngenAction = NewCommandLineOptions::Action_Display;
        }
        else  if ((SString::_wcsicmp(action, W("executeQueuedItems")) == 0) ||
                  (SString::_wcsicmp(action, W("eqi")) == 0))
        {
            opt.ngenAction = NewCommandLineOptions::Action_Finish;
            opt.priority = Priority_Lowest;
        }
        else  if (SString::_wcsicmp(action, W("queue")) == 0)
        {
            opt.ngenAction = NewCommandLineOptions::Action_Queue;
        }
        else if (SString::_wcsicmp(action, W("createpdb")) == 0)
        {
            opt.ngenAction = NewCommandLineOptions::Action_CreatePdb;
        }
        else if (SString::_wcsicmp(action, W("removetaskboottrigger")) == 0)
        {
            opt.ngenAction = NewCommandLineOptions::Action_RemoveTaskBootTrigger;
        }
        else if (SString::_wcsicmp(action, W("removetaskdelaystarttrigger")) == 0)
        {
            opt.ngenAction = NewCommandLineOptions::Action_RemoveTaskDelayStartTrigger;
        }
        else return false;

        // Look for optional args before the assembly path

        while (argc > 0)
        {
            LPCWSTR arg = argv[0];

            if (!IsScenario(arg, &opt.optScenario) &&
                !IsCommandLineOption(arg, opt))
            {
                break;
            }

            argc--;
            argv++;
        }

        // Determine the assembly path.  If the argument starts with / or -, then it must be
        // an ngen option, otherwise assume its an assembly name
        if ((opt.ngenAction == NewCommandLineOptions::Action_Install)   ||
            (opt.ngenAction == NewCommandLineOptions::Action_Uninstall) ||
            (opt.ngenAction == NewCommandLineOptions::Action_Display))
        {
            if (argc < 1) return true;
            StackSString assemblyName;
            if ((argv[0][0] != W('-')) && (argv[0][0] != W('/')))
            {
                BOOL bForceFileFound = (opt.ngenAction == NewCommandLineOptions::Action_Install ||
                                        opt.ngenAction == NewCommandLineOptions::Action_Display);
                CanonicalizePath(argv[0], assemblyName, bForceFileFound);
                if (IsNgenOffline() && IsExeOrDllOrWinMD(assemblyName.GetUnicode()))
                {
                    ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS),
                        SL(W("Error: Only strong-named assemblies are allowed with offline ngen")));
                }
                opt.BSTRassemblyName.Assign(::SysAllocString(assemblyName.GetUnicode()));
                argc--;
                argv++;
            }
        }

        // Parse the optional arguments
        while (argc > 0)
        {
            LPCWSTR arg = argv[0];
            argc--;
            argv++;

            if (!IsScenario(arg, &opt.optScenario) &&
                !IsCommandLineOption(arg, opt))
            {
                PrintLogoHelper();
                StackSString s;
                s.Printf(W("Error: Unrecognized option %s\n"), arg);
                PrintUsageHelper();
                ThrowHR(E_INVALIDARG, s);
            }
        }

        // If uninstall command line specifies a scenario, remove the ScenarioAll flag
        if (opt.ngenAction == NewCommandLineOptions::Action_Uninstall)
        {
            if (opt.optScenario != ScenarioAll)
                *((DWORD *) &opt.optScenario) &= ~ScenarioAll;
        }

        return true;
    }

    void ProcessNewCommandLineOptionsHelper(NewCommandLineOptions &opt,
                                            ICorSvc *pCorSvc,
                                            ICompileProgressNotification *pCompileProgressNotification,
                                            ICorSvcLogger                *pCorSvcLogger)
    {
        HRESULT hr;

        if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_NGENUseService) == 0)
        {
            opt.defer = FALSE;
        }

        ReleaseHolder<IUnknown> pIUnknownServiceManager;
        IfFailThrow(pCorSvc->GetServiceManagerInterface(&pIUnknownServiceManager));
        ReleaseHolder<ICorSvcInstaller> pCorSvcInstaller;
        IfFailThrow(pIUnknownServiceManager->QueryInterface(IID_ICorSvcInstaller, &pCorSvcInstaller));
        pCorSvcInstaller->SetLogger(pCorSvcLogger);

        ReleaseHolder<ICorSvcAdvancedInstaller> pCorSvcAdvancedInstaller;
        ReleaseHolder<ICorSvcOptimizer>         pCorSvcOptimizer;
        ReleaseHolder<ICorSvcOptimizer3>        pCorSvcOptimizer3;
        ReleaseHolder<ICorSvcManager>           pCorSvcManager;
        ReleaseHolder<ICorSvcManager2>           pCorSvcManager2;

        if (!opt.noLogo && opt.logLevel >= LogLevel_Success)
            PrintLogoHelper();

        if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ZapDisable) != 0 &&
            opt.ngenAction != NewCommandLineOptions::Action_Display)
        {
            Output(W("Warning: ZapDisable is turned on.\n"));
        }

        if ((opt.generalFlags & NoRoot) != 0)
        {
            if (opt.defer)
            {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS), SL(W("Error: Cannot use /NoRoot with /Queue.")));
            }
            if ((opt.ngenAction != NewCommandLineOptions::Action_Install) && 
                (opt.ngenAction != NewCommandLineOptions::Action_Uninstall) &&
                (opt.ngenAction != NewCommandLineOptions::Action_CreatePdb))
            {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS), SL(W("Error: /NoRoot can only be used with Install or Uninstall or CreatePDB commands.")));
            }
            if (opt.ngenAction == NewCommandLineOptions::Action_Uninstall && (opt.optScenario != ScenarioAll || opt.BSTRconfig != NULL))
            {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS), SL(W("Error: Scenario, /ExeConfig or /AppBase not allowed with Uninstall /NoRoot command.")));
            }
        }

        if ((opt.BSTRPackageMoniker == NULL) != (opt.BSTRLocalAppData == NULL))
        {
            ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS), SL(W("Error: If either /Package or /LocalAppData is specified, then BOTH must be specified.")));
        }

        if (opt.BSTRPackageMoniker != NULL)
        {
            if ((opt.generalFlags & NoRoot) == 0)
            {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS), SL(W("Error: Cannot use /Package without /NoRoot.")));
            }
            if (opt.ngenAction == NewCommandLineOptions::Action_Install && (opt.optScenario & ScenarioLegacy) == 0)
            {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS), SL(W("Error: Install /Package requires /NoDependencies.")));
            }
        }

        if (opt.BSTRRuntimeVersion != NULL || opt.BSTRPackageMoniker != NULL || opt.BSTRLocalAppData != NULL)
        {
            // We could have made /Version and /Package working with all commands, but for simplicity we currently only
            // support install, uninstall, and createpdb.
            if ((opt.ngenAction != NewCommandLineOptions::Action_Install) && 
                (opt.ngenAction != NewCommandLineOptions::Action_Uninstall) &&
                (opt.ngenAction != NewCommandLineOptions::Action_CreatePdb))
            {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS), SL(W("Error: /Version, /LocalAppData and /Package can only be used with Install or Uninstall or CreatePDB commands.")));
            }
        }
        
        ReleaseHolder<ICorSvcRepository> pCorSvcRepository;
        IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcRepository, &pCorSvcRepository));
        IfFailThrow(pCorSvcRepository->SetRepository(opt.BSTRRepositoryDir, opt.repositoryFlags));

        ReleaseHolder<ICorSvcSetPrivateAttributes> pCorSvcSetPrivateAttributes;
        IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcSetPrivateAttributes, &pCorSvcSetPrivateAttributes));
        IfFailThrow(pCorSvcSetPrivateAttributes->SetNGenPrivateAttributes(opt.ngenPrivateAttributes));

        if (opt.BSTRRuntimeVersion != NULL)
        {
            if (pCorSvcManager2 == NULL)
            {
                IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcManager2, &pCorSvcManager2));
            }
            IfFailThrow(pCorSvcManager2->SetRuntimeVersion(opt.BSTRRuntimeVersion));
        }

        if (opt.BSTRPackageMoniker != NULL)
        {
            if (pCorSvcManager2 == NULL)
            {
                IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcManager2, &pCorSvcManager2));
            }
            IfFailThrow(pCorSvcManager2->SetPackageMoniker(opt.BSTRPackageMoniker));
        }

        if (opt.BSTRLocalAppData != NULL)
        {
            if (pCorSvcManager2 == NULL)
            {
                IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcManager2, &pCorSvcManager2));
            }
            IfFailThrow(pCorSvcManager2->SetLocalAppData(opt.BSTRLocalAppData));
        }

        if (opt.legacyServiceBehavior)
        {
            ReleaseHolder<ICorSvcSetLegacyServiceBehavior> pCorSvcSetLegacyServiceBehavior;
            IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcSetLegacyServiceBehavior, &pCorSvcSetLegacyServiceBehavior));
            IfFailThrow(pCorSvcSetLegacyServiceBehavior->SetLegacyServiceBehavior());
        }
        
        StackSString assemblyName(opt.BSTRassemblyName);

        switch(opt.ngenAction)
        {
        case NewCommandLineOptions::Action_Install:
            if (opt.BSTRassemblyName.IsNull() || assemblyName.IsEmpty()) {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND),
                        SL(W("Error: You must specify an assembly to install.")));
            }
            if ((opt.generalFlags & KeepPriority) != 0 && (!opt.defer || opt.priority != Priority_1))
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS), 
                        SL(W("Error: Cannot use /NetfxPri1 without /Queue:1.")));
            IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcAdvancedInstaller, &pCorSvcAdvancedInstaller));
            IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcManager, &pCorSvcManager));
            IfFailThrow(pCorSvcAdvancedInstaller->Install(opt.BSTRassemblyName, opt.optScenario,
                                                          opt.BSTRconfig, opt.generalFlags, opt.priority));
            if (opt.defer)
            {
                IfFailThrow(pCorSvcManager->NotifyService(NewWorkAvailable));
            }
            else
            {
                IfFailThrow(pCorSvcInstaller->Optimize(pCompileProgressNotification, DefaultOptimizeFlags));
            }
            break;

        case NewCommandLineOptions::Action_Uninstall:
            if (opt.BSTRassemblyName.IsNull() || assemblyName.IsEmpty()) {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND),
                        SL(W("Error: You must specify an assembly to uninstall.")));
            }

            if (opt.defer && !opt.deferAlwaysOK)
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS), 
                        SL(W("Error: Cannot queue an uninstall action")));

            IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcManager, &pCorSvcManager));
            IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcAdvancedInstaller, &pCorSvcAdvancedInstaller));
            hr = pCorSvcAdvancedInstaller->Uninstall(opt.BSTRassemblyName, opt.optScenario, opt.BSTRconfig, opt.generalFlags);
            if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
                ThrowHR(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND), SL(W("Error: The specified assembly is not installed.")));
            IfFailThrow(hr);
            if (opt.defer)
            {
                IfFailThrow(pCorSvcManager->NotifyService(NewWorkAvailable));
            }
            else
            {
                IfFailThrow(pCorSvcInstaller->Optimize(pCompileProgressNotification, DefaultOptimizeFlags));
            }
            break;

        case NewCommandLineOptions::Action_Update:
            IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcOptimizer, &pCorSvcOptimizer));
            IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcManager, &pCorSvcManager));

            _ASSERTE(opt.priority == Priority_Default);

            if ((opt.updateFlags & PostReboot) && !opt.defer)
            {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS), 
                        SL(W("Error: /postreboot requires the use of /queue option.")));
            }

            if (opt.delay && !opt.defer)
            {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS),
                        SL(W("Error: /delay requires the use of /queue option.")));
            }

            if (opt.BSTRassemblyName != NULL)
            {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS),
                        SL(W("Error: Cannot specify an assembly name to update")));
            }

            hr = pCorSvcOptimizer->Update(opt.BSTRassemblyName, opt.updateFlags, opt.generalFlags);
            if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
            {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND), SL(W("Error: The specified assembly is not installed.")));
            }
            else
            {
                IfFailThrow(hr);
            }

            if (opt.defer)
            {
                IfFailThrow(pCorSvcManager->NotifyService(opt.delay ? NewWorkAvailableWithDelay : NewWorkAvailable));
            }
            else
            {
                IfFailThrow(pCorSvcInstaller->Optimize(pCompileProgressNotification, TolerateCompilationFailures));
            }
            break;

        case NewCommandLineOptions::Action_Display:
            IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcOptimizer, &pCorSvcOptimizer));
            SetLoggingToFile(FALSE);
            hr = pCorSvcOptimizer->Display(opt.BSTRassemblyName, opt.generalFlags);
            SetLoggingToFile(TRUE);
            if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
                ThrowHR(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND), SL(W("Error: The specified assembly is not installed.")));
            IfFailThrow(hr);
            break;

        case NewCommandLineOptions::Action_Finish:

            if (opt.defer)
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS), 
                        SL(W("Error: Cannot queue an ExecuteQueuedItems action")));
            
            IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcOptimizer, &pCorSvcOptimizer));

            // NGen Queue work needs to be completed before ScheduleWork is called for the other priorities
            IfFailThrow(pCorSvcOptimizer->ScheduleWork(Priority_0));
            IfFailThrow(pCorSvcInstaller->Optimize(pCompileProgressNotification, (OptimizeFlags) (TolerateCompilationFailures | OptimizeNGenQueueOnly)));

            // Now do the rest of it.
            IfFailThrow(pCorSvcOptimizer->ScheduleWork(opt.priority));
            IfFailThrow(pCorSvcInstaller->Optimize(pCompileProgressNotification, TolerateCompilationFailures));
            
            
            break;
        case NewCommandLineOptions::Action_Queue:
        {
            Output("\n");
            
            if (IsNgenOffline())
            {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS), 
                        SL(W("Error: Cannot interact with the service when using the offline ngen feature")));
            }
            
            if (opt.serviceAction == Service_NoAction)
            {
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS),
                        SL(W("Error: queue action must be pause|continue|status")));
            }

            COR_SERVICE_STATUS status;
            IfFailThrow(pCorSvc->ControlService(opt.serviceAction, &status));

            if (opt.serviceAction == Service_Interrogate)
            {
                Output("Service name is: "); Output(status.sServiceName); Output("\n");
            }

            Output(ServiceStatusString(status.dwCurrentState));

            break;
        }

        case NewCommandLineOptions::Action_CreatePdb: {
            if (opt.nativeImagePath == NULL || opt.pdbPath == NULL)
                ThrowHR(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS),
                        SL(W("Error: insufficient number of arguments to createpdb")));

            IfFailThrow(pIUnknownServiceManager->QueryInterface(IID_ICorSvcOptimizer3, &pCorSvcOptimizer3));
            IfFailThrow(pCorSvcOptimizer3->CreatePdb2(opt.nativeImagePath, opt.pdbPath, opt.pdbLines, opt.managedPdbSearchPath));
            break;
        }
        case NewCommandLineOptions::Action_RemoveTaskBootTrigger: {
            
            ReleaseHolder<ICorSvcSetTaskBootTriggerState> pCorSvcSetTaskBootTriggerState;
            IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcSetTaskBootTriggerState, &pCorSvcSetTaskBootTriggerState));
            IfFailThrow(pCorSvcSetTaskBootTriggerState->SetTaskBootTriggerState(FALSE));
            Output("Task boot trigger removed\n");
            break;
        }
        case NewCommandLineOptions::Action_RemoveTaskDelayStartTrigger:
        {
            ReleaseHolder<ICorSvcSetTaskDelayStartTriggerState> pCorSvcSetTaskDelayStartTriggerState;
            IfFailThrow(pCorSvcInstaller->QueryInterface(IID_ICorSvcSetTaskDelayStartTriggerState, &pCorSvcSetTaskDelayStartTriggerState));
            IfFailThrow(pCorSvcSetTaskDelayStartTriggerState->SetTaskDelayStartTriggerState(FALSE));
            Output("Task delay start trigger removed\n");
            break;
        }
        default:
            ThrowHR(E_UNEXPECTED);
        }
    }

private:
    INGenParserCallback *parserCallback;
};

