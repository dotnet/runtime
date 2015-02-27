// This is auto generated file. Please don’t modify manually.
// The file is generated as part of the build through the ResourceGenerator tool 
// which takes the project resx resource file and generated this source code file.
// By default the tool will use Resources\Strings.resx but projects can customize
// that by overriding the StringResourcesPath property group.
namespace System
{
    internal static partial class SR
    {
#pragma warning disable 0414
        private const string s_resourcesName = "System.IO.FileSystem.DriveInfo.resources"; // assembly Name + .resources
#pragma warning restore 0414

#if !DEBUGRESOURCES
        internal static string Arg_MustBeDriveLetterOrRootDir {
              get { return SR.GetResourceString("Arg_MustBeDriveLetterOrRootDir", null); }
        }
        internal static string Arg_MustBeNonEmptyDriveName {
              get { return SR.GetResourceString("Arg_MustBeNonEmptyDriveName", null); }
        }
        internal static string Arg_InvalidDriveChars {
              get { return SR.GetResourceString("Arg_InvalidDriveChars", null); }
        }
        internal static string ArgumentOutOfRange_FileLengthTooBig {
              get { return SR.GetResourceString("ArgumentOutOfRange_FileLengthTooBig", null); }
        }
        internal static string InvalidOperation_SetVolumeLabelFailed {
              get { return SR.GetResourceString("InvalidOperation_SetVolumeLabelFailed", null); }
        }
        internal static string IO_AlreadyExists_Name {
              get { return SR.GetResourceString("IO_AlreadyExists_Name", null); }
        }
        internal static string IO_DriveNotFound {
              get { return SR.GetResourceString("IO_DriveNotFound", null); }
        }
        internal static string IO_DriveNotFound_Drive {
              get { return SR.GetResourceString("IO_DriveNotFound_Drive", null); }
        }
        internal static string IO_FileExists_Name {
              get { return SR.GetResourceString("IO_FileExists_Name", null); }
        }
        internal static string IO_FileNotFound {
              get { return SR.GetResourceString("IO_FileNotFound", null); }
        }
        internal static string IO_FileNotFound_FileName {
              get { return SR.GetResourceString("IO_FileNotFound_FileName", null); }
        }
        internal static string IO_PathNotFound_NoPathName {
              get { return SR.GetResourceString("IO_PathNotFound_NoPathName", null); }
        }
        internal static string IO_PathNotFound_Path {
              get { return SR.GetResourceString("IO_PathNotFound_Path", null); }
        }
        internal static string IO_PathTooLong {
              get { return SR.GetResourceString("IO_PathTooLong", null); }
        }
        internal static string IO_SharingViolation_File {
              get { return SR.GetResourceString("IO_SharingViolation_File", null); }
        }
        internal static string IO_SharingViolation_NoFileName {
              get { return SR.GetResourceString("IO_SharingViolation_NoFileName", null); }
        }
        internal static string UnauthorizedAccess_IODenied_NoPathName {
              get { return SR.GetResourceString("UnauthorizedAccess_IODenied_NoPathName", null); }
        }
        internal static string UnauthorizedAccess_IODenied_Path {
              get { return SR.GetResourceString("UnauthorizedAccess_IODenied_Path", null); }
        }
        internal static string UnknownError_Num {
              get { return SR.GetResourceString("UnknownError_Num", null); }
        }
#else
        internal static string Arg_MustBeDriveLetterOrRootDir {
              get { return SR.GetResourceString("Arg_MustBeDriveLetterOrRootDir", @"Drive name must be a root directory ('C:\\') or a drive letter ('C')."); }
        }
        internal static string Arg_MustBeNonEmptyDriveName {
              get { return SR.GetResourceString("Arg_MustBeNonEmptyDriveName", @"Drive name must not be empty."); }
        }
        internal static string Arg_InvalidDriveChars {
              get { return SR.GetResourceString("Arg_InvalidDriveChars", @"Illegal characters in drive name '{0}'."); }
        }
        internal static string ArgumentOutOfRange_FileLengthTooBig {
              get { return SR.GetResourceString("ArgumentOutOfRange_FileLengthTooBig", @"Specified file length was too large for the file system."); }
        }
        internal static string InvalidOperation_SetVolumeLabelFailed {
              get { return SR.GetResourceString("InvalidOperation_SetVolumeLabelFailed", @"Volume labels can only be set for writable local volumes."); }
        }
        internal static string IO_AlreadyExists_Name {
              get { return SR.GetResourceString("IO_AlreadyExists_Name", @"Cannot create '{0}' because a file or directory with the same name already exists."); }
        }
        internal static string IO_DriveNotFound {
              get { return SR.GetResourceString("IO_DriveNotFound", @"Could not find the drive. The drive might not be ready or might not be mapped."); }
        }
        internal static string IO_DriveNotFound_Drive {
              get { return SR.GetResourceString("IO_DriveNotFound_Drive", @"Could not find the drive '{0}'. The drive might not be ready or might not be mapped."); }
        }
        internal static string IO_FileExists_Name {
              get { return SR.GetResourceString("IO_FileExists_Name", @"The file '{0}' already exists."); }
        }
        internal static string IO_FileNotFound {
              get { return SR.GetResourceString("IO_FileNotFound", @"Unable to find the specified file."); }
        }
        internal static string IO_FileNotFound_FileName {
              get { return SR.GetResourceString("IO_FileNotFound_FileName", @"Could not find file '{0}'."); }
        }
        internal static string IO_PathNotFound_NoPathName {
              get { return SR.GetResourceString("IO_PathNotFound_NoPathName", @"Could not find a part of the path."); }
        }
        internal static string IO_PathNotFound_Path {
              get { return SR.GetResourceString("IO_PathNotFound_Path", @"Could not find a part of the path '{0}'."); }
        }
        internal static string IO_PathTooLong {
              get { return SR.GetResourceString("IO_PathTooLong", @"The specified path, file name, or both are too long. The fully qualified file name must be less than 260 characters, and the directory name must be less than 248 characters."); }
        }
        internal static string IO_SharingViolation_File {
              get { return SR.GetResourceString("IO_SharingViolation_File", @"The process cannot access the file '{0}' because it is being used by another process."); }
        }
        internal static string IO_SharingViolation_NoFileName {
              get { return SR.GetResourceString("IO_SharingViolation_NoFileName", @"The process cannot access the file because it is being used by another process."); }
        }
        internal static string UnauthorizedAccess_IODenied_NoPathName {
              get { return SR.GetResourceString("UnauthorizedAccess_IODenied_NoPathName", @"Access to the path is denied."); }
        }
        internal static string UnauthorizedAccess_IODenied_Path {
              get { return SR.GetResourceString("UnauthorizedAccess_IODenied_Path", @"Access to the path '{0}' is denied."); }
        }
        internal static string UnknownError_Num {
              get { return SR.GetResourceString("UnknownError_Num", @"Unknown error '{0}'."); }
        }

#endif
    }
}
