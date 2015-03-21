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
        private const string s_resourcesName = "System.Runtime.Serialization.Primitives.resources"; // assembly Name + .resources
#pragma warning restore 0414

#if !DEBUGRESOURCES
        internal static string OrderCannotBeNegative {
              get { return SR.GetResourceString("OrderCannotBeNegative", null); }
        }
        internal static string SerializationException {
              get { return SR.GetResourceString("SerializationException", null); }
        }
#else
        internal static string OrderCannotBeNegative {
              get { return SR.GetResourceString("OrderCannotBeNegative", @"Property 'Order' in DataMemberAttribute attribute cannot be a negative number."); }
        }
        internal static string SerializationException {
              get { return SR.GetResourceString("SerializationException", @"Serialization error."); }
        }

#endif
    }
}
