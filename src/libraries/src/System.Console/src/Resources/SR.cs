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
        private const string s_resourcesName = "System.Console.resources"; // assembly Name + .resources
#pragma warning restore 0414

#if !DEBUGRESOURCES
        internal static string ArgumentOutOfRange_NeedNonNegNum {
              get { return SR.GetResourceString("ArgumentOutOfRange_NeedNonNegNum", null); }
        }
        internal static string ArgumentNull_Buffer {
              get { return SR.GetResourceString("ArgumentNull_Buffer", null); }
        }
        internal static string Argument_InvalidOffLen {
              get { return SR.GetResourceString("Argument_InvalidOffLen", null); }
        }
        internal static string ArgumentOutOfRange_FileLengthTooBig {
              get { return SR.GetResourceString("ArgumentOutOfRange_FileLengthTooBig", null); }
        }
        internal static string NotSupported_UnseekableStream {
              get { return SR.GetResourceString("NotSupported_UnseekableStream", null); }
        }
        internal static string ObjectDisposed_FileClosed {
              get { return SR.GetResourceString("ObjectDisposed_FileClosed", null); }
        }
        internal static string NotSupported_UnwritableStream {
              get { return SR.GetResourceString("NotSupported_UnwritableStream", null); }
        }
        internal static string NotSupported_UnreadableStream {
              get { return SR.GetResourceString("NotSupported_UnreadableStream", null); }
        }
        internal static string IO_AlreadyExists_Name {
              get { return SR.GetResourceString("IO_AlreadyExists_Name", null); }
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
        internal static string UnauthorizedAccess_IODenied_NoPathName {
              get { return SR.GetResourceString("UnauthorizedAccess_IODenied_NoPathName", null); }
        }
        internal static string UnauthorizedAccess_IODenied_Path {
              get { return SR.GetResourceString("UnauthorizedAccess_IODenied_Path", null); }
        }
        internal static string IO_SharingViolation_File {
              get { return SR.GetResourceString("IO_SharingViolation_File", null); }
        }
        internal static string IO_SharingViolation_NoFileName {
              get { return SR.GetResourceString("IO_SharingViolation_NoFileName", null); }
        }
        internal static string IndexOutOfRange_IORaceCondition {
              get { return SR.GetResourceString("IndexOutOfRange_IORaceCondition", null); }
        }
        internal static string UnknownError_Num {
              get { return SR.GetResourceString("UnknownError_Num", null); }
        }
        internal static string Arg_InvalidConsoleColor {
              get { return SR.GetResourceString("Arg_InvalidConsoleColor", null); }
        }
        internal static string IO_NoConsole {
              get { return SR.GetResourceString("IO_NoConsole", null); }
        }
        internal static string InvalidOperation_EmptyStack {
              get { return SR.GetResourceString("InvalidOperation_EmptyStack", null); }
        }
        internal static string IO_TermInfoInvalid {
              get { return SR.GetResourceString("IO_TermInfoInvalid", null); }
        }
        internal static string InvalidOperation_PrintF {
              get { return SR.GetResourceString("InvalidOperation_PrintF", null); }
        }
        internal static string PlatformNotSupported_GettingColor {
              get { return SR.GetResourceString("PlatformNotSupported_GettingColor", null); }
        }
#else
        internal static string ArgumentOutOfRange_NeedNonNegNum {
              get { return SR.GetResourceString("ArgumentOutOfRange_NeedNonNegNum", @"Non-negative number required."); }
        }
        internal static string ArgumentNull_Buffer {
              get { return SR.GetResourceString("ArgumentNull_Buffer", @"Buffer cannot be null."); }
        }
        internal static string Argument_InvalidOffLen {
              get { return SR.GetResourceString("Argument_InvalidOffLen", @"Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection."); }
        }
        internal static string ArgumentOutOfRange_FileLengthTooBig {
              get { return SR.GetResourceString("ArgumentOutOfRange_FileLengthTooBig", @"Specified file length was too large for the file system."); }
        }
        internal static string NotSupported_UnseekableStream {
              get { return SR.GetResourceString("NotSupported_UnseekableStream", @"Stream does not support seeking."); }
        }
        internal static string ObjectDisposed_FileClosed {
              get { return SR.GetResourceString("ObjectDisposed_FileClosed", @"Cannot access a closed file."); }
        }
        internal static string NotSupported_UnwritableStream {
              get { return SR.GetResourceString("NotSupported_UnwritableStream", @"Stream does not support writing."); }
        }
        internal static string NotSupported_UnreadableStream {
              get { return SR.GetResourceString("NotSupported_UnreadableStream", @"Stream does not support reading."); }
        }
        internal static string IO_AlreadyExists_Name {
              get { return SR.GetResourceString("IO_AlreadyExists_Name", @"Cannot create '{0}'because a file or directory with the same name already exists."); }
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
        internal static string UnauthorizedAccess_IODenied_NoPathName {
              get { return SR.GetResourceString("UnauthorizedAccess_IODenied_NoPathName", @"Access to the path is denied."); }
        }
        internal static string UnauthorizedAccess_IODenied_Path {
              get { return SR.GetResourceString("UnauthorizedAccess_IODenied_Path", @"Access to the path '{0}' is denied."); }
        }
        internal static string IO_SharingViolation_File {
              get { return SR.GetResourceString("IO_SharingViolation_File", @"The process cannot access the file '{0}' because it is being used by another process."); }
        }
        internal static string IO_SharingViolation_NoFileName {
              get { return SR.GetResourceString("IO_SharingViolation_NoFileName", @"The process cannot access the file because it is being used by another process."); }
        }
        internal static string IndexOutOfRange_IORaceCondition {
              get { return SR.GetResourceString("IndexOutOfRange_IORaceCondition", @"Probable I/O race condition detected while copying memory. The I/O package is not thread safe by default. In multithreaded applications, a stream must be accessed in a thread-safe way, such as a thread-safe wrapper returned by TextReader's or TextWriter's Synchronized methods. This also applies to classes like StreamWriter and StreamReader."); }
        }
        internal static string UnknownError_Num {
              get { return SR.GetResourceString("UnknownError_Num", @"Unknown error '{0}'."); }
        }
        internal static string Arg_InvalidConsoleColor {
              get { return SR.GetResourceString("Arg_InvalidConsoleColor", @"The ConsoleColor enum value was not defined on that enum. Please use a defined color from the enum."); }
        }
        internal static string IO_NoConsole {
              get { return SR.GetResourceString("IO_NoConsole", @"There is no console."); }
        }
        internal static string InvalidOperation_EmptyStack {
              get { return SR.GetResourceString("InvalidOperation_EmptyStack", @"Stack empty"); }
        }
        internal static string IO_TermInfoInvalid {
              get { return SR.GetResourceString("IO_TermInfoInvalid", @"The terminfo database is invalid."); }
        }
        internal static string InvalidOperation_PrintF {
              get { return SR.GetResourceString("InvalidOperation_PrintF", @"The printf operation failed."); }
        }
        internal static string PlatformNotSupported_GettingColor {
              get { return SR.GetResourceString("PlatformNotSupported_GettingColor", @"This platform does not support getting the current color."); }
        }

#endif
    }
}
