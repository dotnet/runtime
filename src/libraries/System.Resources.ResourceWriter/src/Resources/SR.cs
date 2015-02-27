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
        private const string s_resourcesName = "System.Resources.ResourceWriter.resources"; // assembly Name + .resources
#pragma warning restore 0414

#if !DEBUGRESOURCES
        internal static string InvalidOperation_ResourceWriterSaved {
              get { return SR.GetResourceString("InvalidOperation_ResourceWriterSaved", null); }
        }
        internal static string Argument_StreamNotWritable {
              get { return SR.GetResourceString("Argument_StreamNotWritable", null); }
        }
#else
        internal static string InvalidOperation_ResourceWriterSaved {
              get { return SR.GetResourceString("InvalidOperation_ResourceWriterSaved", @"The resource writer has already been closed and cannot be edited."); }
        }
        internal static string Argument_StreamNotWritable {
              get { return SR.GetResourceString("Argument_StreamNotWritable", @"Stream was not writable."); }
        }

#endif
    }
}
