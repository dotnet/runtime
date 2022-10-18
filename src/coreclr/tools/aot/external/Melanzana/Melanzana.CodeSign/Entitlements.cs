using System.Buffers.Binary;
using System.Text;
using Claunia.PropertyList;

namespace Melanzana.CodeSign
{
    public class Entitlements
    {
        public Entitlements(NSDictionary plist)
        {
            PList = plist;
        }

        public NSDictionary PList { get; set; }

        public bool GetTaskAllow => GetBoolEntitlement("get-task-allow");
        public bool RunUnsignedCode => GetBoolEntitlement("run-unsigned-code");
        public bool Debugger => GetBoolEntitlement("com.apple.private.cs.debugger");
        public bool DynamicCodeSigning => GetBoolEntitlement("dynamic-codesigning");
        public bool SkipLibraryValidation => GetBoolEntitlement("com.apple.private.skip-library-validation");
        public bool CanLoadCdHash => GetBoolEntitlement("com.apple.private.amfi.can-load-cdhash");
        public bool CanExecuteCdHash => GetBoolEntitlement("com.apple.private.amfi.can-execute-cdhash");

        private bool GetBoolEntitlement(string name)
        {
            if (PList.TryGetValue(name, out var temp) && temp is NSNumber number && number.isBoolean())
                return number.ToBool();
            return false;
        }
    }
}