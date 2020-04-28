// GENERATED FILE, DO NOT MODIFY
typedef struct {
const char *name;
void *func;
} PinvokeImport;

int SystemNative_ConvertErrorPlatformToPal (int);
int SystemNative_ConvertErrorPalToPlatform (int);
int SystemNative_StrErrorR (int,int,int);
void SystemNative_GetNonCryptographicallySecureRandomBytes (int,int);
int SystemNative_OpenDir (int);
int SystemNative_GetReadDirRBufferSize ();
int SystemNative_ReadDirR (int,int,int,int);
int SystemNative_CloseDir (int);
int SystemNative_ReadLink (int,int,int);
int SystemNative_FStat2 (int,int);
int SystemNative_Stat2 (int,int);
int SystemNative_LStat2 (int,int);
int SystemNative_Symlink (int,int);
int SystemNative_ChMod (int,int);
int SystemNative_CopyFile (int,int);
int SystemNative_GetEGid ();
int SystemNative_GetEUid ();
int SystemNative_LChflags (int,int);
int SystemNative_LChflagsCanSetHiddenFlag ();
int SystemNative_Link (int,int);
int SystemNative_MkDir (int,int);
int SystemNative_Rename (int,int);
int SystemNative_RmDir (int);
int SystemNative_Stat2 (int,int);
int SystemNative_LStat2 (int,int);
int SystemNative_UTime (int,int);
int SystemNative_UTimes (int,int);
int SystemNative_Unlink (int);
static PinvokeImport System_Native_imports [] = {
{"SystemNative_ConvertErrorPlatformToPal", SystemNative_ConvertErrorPlatformToPal},
{"SystemNative_ConvertErrorPalToPlatform", SystemNative_ConvertErrorPalToPlatform},
{"SystemNative_StrErrorR", SystemNative_StrErrorR},
{"SystemNative_GetNonCryptographicallySecureRandomBytes", SystemNative_GetNonCryptographicallySecureRandomBytes},
{"SystemNative_OpenDir", SystemNative_OpenDir},
{"SystemNative_GetReadDirRBufferSize", SystemNative_GetReadDirRBufferSize},
{"SystemNative_ReadDirR", SystemNative_ReadDirR},
{"SystemNative_CloseDir", SystemNative_CloseDir},
{"SystemNative_ReadLink", SystemNative_ReadLink},
{"SystemNative_FStat2", SystemNative_FStat2},
{"SystemNative_Stat2", SystemNative_Stat2},
{"SystemNative_LStat2", SystemNative_LStat2},
{"SystemNative_Symlink", SystemNative_Symlink},
{"SystemNative_ChMod", SystemNative_ChMod},
{"SystemNative_CopyFile", SystemNative_CopyFile},
{"SystemNative_GetEGid", SystemNative_GetEGid},
{"SystemNative_GetEUid", SystemNative_GetEUid},
{"SystemNative_LChflags", SystemNative_LChflags},
{"SystemNative_LChflagsCanSetHiddenFlag", SystemNative_LChflagsCanSetHiddenFlag},
{"SystemNative_Link", SystemNative_Link},
{"SystemNative_MkDir", SystemNative_MkDir},
{"SystemNative_Rename", SystemNative_Rename},
{"SystemNative_RmDir", SystemNative_RmDir},
{"SystemNative_Stat2", SystemNative_Stat2},
{"SystemNative_LStat2", SystemNative_LStat2},
{"SystemNative_UTime", SystemNative_UTime},
{"SystemNative_UTimes", SystemNative_UTimes},
{"SystemNative_Unlink", SystemNative_Unlink},
{NULL, NULL}
};
static void *pinvoke_tables[] = { System_Native_imports,};
static char *pinvoke_names[] = { "System.Native",};
