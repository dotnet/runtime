// GENERATED FILE, DO NOT MODIFY
typedef struct {
const char *name;
void *func;
} PinvokeImport;

int SystemNative_ConvertErrorPlatformToPal (int);
int SystemNative_ConvertErrorPalToPlatform (int);
int SystemNative_StrErrorR (int,int,int);
int SystemNative_Access (int,int);
int SystemNative_ChDir (int);
int SystemNative_Close (int);
int SystemNative_FLock (int,int);
int SystemNative_FLock (int,int);
int SystemNative_FSync (int);
int SystemNative_FTruncate (int,int64_t);
int SystemNative_GetCpuUtilization (int);
int SystemNative_GetCwd (int,int);
int SystemNative_GetEUid ();
int SystemNative_GetHostName (int,int);
int SystemNative_GetPid ();
int SystemNative_GetPwUidR (int,int,int,int);
void SystemNative_GetNonCryptographicallySecureRandomBytes (int,int);
int64_t SystemNative_GetSystemTimeAsTicks ();
uint64_t SystemNative_GetTimestampResolution ();
uint64_t SystemNative_GetTimestamp ();
int SystemNative_GetUnixRelease ();
int SystemNative_LockFileRegion (int,int64_t,int64_t,int);
int64_t SystemNative_LSeek (int,int64_t,int);
int SystemNative_MksTemps (int,int);
int SystemNative_GetAllMountPoints (int);
int SystemNative_Open (int,int,int);
int SystemNative_PosixFAdvise (int,int64_t,int64_t,int);
int SystemNative_Read (int,int,int);
int SystemNative_OpenDir (int);
int SystemNative_GetReadDirRBufferSize ();
int SystemNative_ReadDirR (int,int,int,int);
int SystemNative_CloseDir (int);
int SystemNative_ReadLink (int,int,int);
int SystemNative_FStat (int,int);
int SystemNative_Stat (int,int);
int SystemNative_LStat (int,int);
int64_t SystemNative_SysConf (int);
void SystemNative_SysLog (int,int,int);
int SystemNative_Unlink (int);
int SystemNative_Write (int,int,int);
int SystemNative_Write (int,int,int);
int SystemNative_ConvertErrorPlatformToPal (int);
int SystemNative_ConvertErrorPalToPlatform (int);
int SystemNative_StrErrorR (int,int,int);
int SystemNative_Dup (int);
void SystemNative_GetControlCharacters (int,int,int,int);
int SystemNative_IsATty (int);
int64_t SystemNative_LSeek (int,int64_t,int);
int SystemNative_Open (int,int,int);
int SystemNative_Poll (int,int,int,int);
int SystemNative_GetEUid ();
int SystemNative_GetPwUidR (int,int,int,int);
void SystemNative_RegisterForCtrl (int);
void SystemNative_UnregisterForCtrl ();
void SystemNative_RestoreAndHandleCtrl (int);
void SystemNative_SetTerminalInvalidationHandler (int);
int SystemNative_GetSignalForBreak ();
int SystemNative_SetSignalForBreak (int);
int SystemNative_SNPrintF (int,int,int,int);
int SystemNative_SNPrintF (int,int,int,int);
int SystemNative_Read (int,int,int);
int SystemNative_Write (int,int,int);
int SystemNative_GetWindowSize (int);
int SystemNative_InitializeTerminalAndSignalHandling ();
void SystemNative_SetKeypadXmit (int);
int SystemNative_ReadStdin (int,int);
void SystemNative_InitializeConsoleBeforeRead (int,int);
void SystemNative_UninitializeConsoleAfterRead ();
int SystemNative_StdinReady ();
int SystemNative_ConvertErrorPlatformToPal (int);
int SystemNative_ConvertErrorPalToPlatform (int);
int SystemNative_StrErrorR (int,int,int);
int SystemNative_GetEUid ();
int SystemNative_GetAllMountPoints (int);
int SystemNative_OpenDir (int);
int SystemNative_GetReadDirRBufferSize ();
int SystemNative_ReadDirR (int,int,int,int);
int SystemNative_CloseDir (int);
int SystemNative_Stat (int,int);
int SystemNative_LStat (int,int);
int SystemNative_Unlink (int);
int SystemNative_ChMod (int,int);
int SystemNative_CopyFile (int,int,int,int);
int SystemNative_GetEGid ();
int SystemNative_LChflags (int,int);
int SystemNative_LChflagsCanSetHiddenFlag ();
int SystemNative_Link (int,int);
int SystemNative_MkDir (int,int);
int SystemNative_Rename (int,int);
int SystemNative_RmDir (int);
int SystemNative_Stat (int,int);
int SystemNative_LStat (int,int);
int SystemNative_UTimensat (int,int);
int SystemNative_GetUnixName ();
int SystemNative_GetUnixVersion (int,int);
int SystemNative_GetOSArchitecture ();
int SystemNative_GetProcessArchitecture ();
int SystemNative_ConvertErrorPlatformToPal (int);
int SystemNative_ConvertErrorPalToPlatform (int);
int SystemNative_StrErrorR (int,int,int);
int SystemNative_MMap (int,uint64_t,int,int,int,int64_t);
int SystemNative_MUnmap (int,uint64_t);
int SystemNative_MSync (int,uint64_t,int);
int64_t SystemNative_SysConf (int);
int SystemNative_FTruncate (int,int64_t);
int SystemNative_MAdvise (int,uint64_t,int);
int SystemNative_ShmOpen (int,int,int);
int SystemNative_ShmUnlink (int);
int SystemNative_Unlink (int);
static PinvokeImport libSystem_Native_imports [] = {
{"SystemNative_ConvertErrorPlatformToPal", SystemNative_ConvertErrorPlatformToPal},
{"SystemNative_ConvertErrorPalToPlatform", SystemNative_ConvertErrorPalToPlatform},
{"SystemNative_StrErrorR", SystemNative_StrErrorR},
{"SystemNative_Access", SystemNative_Access},
{"SystemNative_ChDir", SystemNative_ChDir},
{"SystemNative_Close", SystemNative_Close},
{"SystemNative_FLock", SystemNative_FLock},
{"SystemNative_FLock", SystemNative_FLock},
{"SystemNative_FSync", SystemNative_FSync},
{"SystemNative_FTruncate", SystemNative_FTruncate},
{"SystemNative_GetCpuUtilization", SystemNative_GetCpuUtilization},
{"SystemNative_GetCwd", SystemNative_GetCwd},
{"SystemNative_GetEUid", SystemNative_GetEUid},
{"SystemNative_GetHostName", SystemNative_GetHostName},
{"SystemNative_GetPid", SystemNative_GetPid},
{"SystemNative_GetPwUidR", SystemNative_GetPwUidR},
{"SystemNative_GetNonCryptographicallySecureRandomBytes", SystemNative_GetNonCryptographicallySecureRandomBytes},
{"SystemNative_GetSystemTimeAsTicks", SystemNative_GetSystemTimeAsTicks},
{"SystemNative_GetTimestampResolution", SystemNative_GetTimestampResolution},
{"SystemNative_GetTimestamp", SystemNative_GetTimestamp},
{"SystemNative_GetUnixRelease", SystemNative_GetUnixRelease},
{"SystemNative_LockFileRegion", SystemNative_LockFileRegion},
{"SystemNative_LSeek", SystemNative_LSeek},
{"SystemNative_MksTemps", SystemNative_MksTemps},
{"SystemNative_GetAllMountPoints", SystemNative_GetAllMountPoints},
{"SystemNative_Open", SystemNative_Open},
{"SystemNative_PosixFAdvise", SystemNative_PosixFAdvise},
{"SystemNative_Read", SystemNative_Read},
{"SystemNative_OpenDir", SystemNative_OpenDir},
{"SystemNative_GetReadDirRBufferSize", SystemNative_GetReadDirRBufferSize},
{"SystemNative_ReadDirR", SystemNative_ReadDirR},
{"SystemNative_CloseDir", SystemNative_CloseDir},
{"SystemNative_ReadLink", SystemNative_ReadLink},
{"SystemNative_FStat", SystemNative_FStat},
{"SystemNative_Stat", SystemNative_Stat},
{"SystemNative_LStat", SystemNative_LStat},
{"SystemNative_SysConf", SystemNative_SysConf},
{"SystemNative_SysLog", SystemNative_SysLog},
{"SystemNative_Unlink", SystemNative_Unlink},
{"SystemNative_Write", SystemNative_Write},
{"SystemNative_Write", SystemNative_Write},
{"SystemNative_ConvertErrorPlatformToPal", SystemNative_ConvertErrorPlatformToPal},
{"SystemNative_ConvertErrorPalToPlatform", SystemNative_ConvertErrorPalToPlatform},
{"SystemNative_StrErrorR", SystemNative_StrErrorR},
{"SystemNative_Dup", SystemNative_Dup},
{"SystemNative_GetControlCharacters", SystemNative_GetControlCharacters},
{"SystemNative_IsATty", SystemNative_IsATty},
{"SystemNative_LSeek", SystemNative_LSeek},
{"SystemNative_Open", SystemNative_Open},
{"SystemNative_Poll", SystemNative_Poll},
{"SystemNative_GetEUid", SystemNative_GetEUid},
{"SystemNative_GetPwUidR", SystemNative_GetPwUidR},
{"SystemNative_RegisterForCtrl", SystemNative_RegisterForCtrl},
{"SystemNative_UnregisterForCtrl", SystemNative_UnregisterForCtrl},
{"SystemNative_RestoreAndHandleCtrl", SystemNative_RestoreAndHandleCtrl},
{"SystemNative_SetTerminalInvalidationHandler", SystemNative_SetTerminalInvalidationHandler},
{"SystemNative_GetSignalForBreak", SystemNative_GetSignalForBreak},
{"SystemNative_SetSignalForBreak", SystemNative_SetSignalForBreak},
{"SystemNative_SNPrintF", SystemNative_SNPrintF},
{"SystemNative_SNPrintF", SystemNative_SNPrintF},
{"SystemNative_Read", SystemNative_Read},
{"SystemNative_Write", SystemNative_Write},
{"SystemNative_GetWindowSize", SystemNative_GetWindowSize},
{"SystemNative_InitializeTerminalAndSignalHandling", SystemNative_InitializeTerminalAndSignalHandling},
{"SystemNative_SetKeypadXmit", SystemNative_SetKeypadXmit},
{"SystemNative_ReadStdin", SystemNative_ReadStdin},
{"SystemNative_InitializeConsoleBeforeRead", SystemNative_InitializeConsoleBeforeRead},
{"SystemNative_UninitializeConsoleAfterRead", SystemNative_UninitializeConsoleAfterRead},
{"SystemNative_StdinReady", SystemNative_StdinReady},
{"SystemNative_ConvertErrorPlatformToPal", SystemNative_ConvertErrorPlatformToPal},
{"SystemNative_ConvertErrorPalToPlatform", SystemNative_ConvertErrorPalToPlatform},
{"SystemNative_StrErrorR", SystemNative_StrErrorR},
{"SystemNative_GetEUid", SystemNative_GetEUid},
{"SystemNative_GetAllMountPoints", SystemNative_GetAllMountPoints},
{"SystemNative_OpenDir", SystemNative_OpenDir},
{"SystemNative_GetReadDirRBufferSize", SystemNative_GetReadDirRBufferSize},
{"SystemNative_ReadDirR", SystemNative_ReadDirR},
{"SystemNative_CloseDir", SystemNative_CloseDir},
{"SystemNative_Stat", SystemNative_Stat},
{"SystemNative_LStat", SystemNative_LStat},
{"SystemNative_Unlink", SystemNative_Unlink},
{"SystemNative_ChMod", SystemNative_ChMod},
{"SystemNative_CopyFile", SystemNative_CopyFile},
{"SystemNative_GetEGid", SystemNative_GetEGid},
{"SystemNative_LChflags", SystemNative_LChflags},
{"SystemNative_LChflagsCanSetHiddenFlag", SystemNative_LChflagsCanSetHiddenFlag},
{"SystemNative_Link", SystemNative_Link},
{"SystemNative_MkDir", SystemNative_MkDir},
{"SystemNative_Rename", SystemNative_Rename},
{"SystemNative_RmDir", SystemNative_RmDir},
{"SystemNative_Stat", SystemNative_Stat},
{"SystemNative_LStat", SystemNative_LStat},
{"SystemNative_UTimensat", SystemNative_UTimensat},
{"SystemNative_GetUnixName", SystemNative_GetUnixName},
{"SystemNative_GetUnixVersion", SystemNative_GetUnixVersion},
{"SystemNative_GetOSArchitecture", SystemNative_GetOSArchitecture},
{"SystemNative_GetProcessArchitecture", SystemNative_GetProcessArchitecture},
{"SystemNative_ConvertErrorPlatformToPal", SystemNative_ConvertErrorPlatformToPal},
{"SystemNative_ConvertErrorPalToPlatform", SystemNative_ConvertErrorPalToPlatform},
{"SystemNative_StrErrorR", SystemNative_StrErrorR},
{"SystemNative_MMap", SystemNative_MMap},
{"SystemNative_MUnmap", SystemNative_MUnmap},
{"SystemNative_MSync", SystemNative_MSync},
{"SystemNative_SysConf", SystemNative_SysConf},
{"SystemNative_FTruncate", SystemNative_FTruncate},
{"SystemNative_MAdvise", SystemNative_MAdvise},
{"SystemNative_ShmOpen", SystemNative_ShmOpen},
{"SystemNative_ShmUnlink", SystemNative_ShmUnlink},
{"SystemNative_Unlink", SystemNative_Unlink},
{NULL, NULL}
};
static void *pinvoke_tables[] = { libSystem_Native_imports,};
static char *pinvoke_names[] = { "libSystem.Native",};
