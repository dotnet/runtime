using Claunia.PropertyList;

namespace Melanzana.CodeSign
{
    public class Entitlements
    {
        public Entitlements()
            : this(new NSDictionary())
        {
        }

        public Entitlements(NSDictionary plist)
        {
            PList = plist;
        }

        public NSDictionary PList { get; set; }

        public string? ApplicationIdentifier
        {
            get { return GetStringEntitlement("application-identifier"); }
            set { SetStringEntitlement("application-identifier", value); }
        }

        public string? TeamIdentifier
        {
            get { return GetStringEntitlement("com.apple.developer.team-identifier"); }
            set { SetStringEntitlement("com.apple.developer.team-identifier", value); }
        }

        public IList<string>? KeychainAccessGroups
        {
            get { return GetStringListEntitlement("keychain-access-groups"); }
            set { SetStringListEntitlement("keychain-access-groups", value);}
        }

        public bool GetTaskAllow
        {
            get { return GetBoolEntitlement("get-task-allow"); }
            set { SetBoolEntitlement("get-task-allow", value); }
        }

        public bool RunUnsignedCode
        {
            get { return GetBoolEntitlement("run-unsigned-code"); }
            set { SetBoolEntitlement("run-unsigned-code", value); }
        }

        public bool Debugger
        {
            get { return GetBoolEntitlement("com.apple.private.cs.debugger"); }
            set { SetBoolEntitlement("com.apple.private.cs.debugger", value); }
        }

        public bool DynamicCodeSigning
        {
            get { return GetBoolEntitlement("dynamic-codesigning"); }
            set { SetBoolEntitlement("dynamic-codesigning", value); }
        }

        public bool SkipLibraryValidation
        {
            get { return GetBoolEntitlement("com.apple.private.skip-library-validation"); }
            set { SetBoolEntitlement("com.apple.private.skip-library-validation", value); }
        }

        public bool CanLoadCdHash
        {
            get { return GetBoolEntitlement("com.apple.private.amfi.can-load-cdhash"); }
            set { SetBoolEntitlement("com.apple.private.amfi.can-load-cdhash", value); }
        }

        public bool CanExecuteCdHash
        {
            get { return GetBoolEntitlement("com.apple.private.amfi.can-execute-cdhash"); }
            set { SetBoolEntitlement("com.apple.private.amfi.can-execute-cdhash", value); }
        }

        private bool GetBoolEntitlement(string name)
        {
            if (PList.TryGetValue(name, out var temp) && temp is NSNumber number && number.isBoolean())
                return number.ToBool();
            return false;
        }

        private string? GetStringEntitlement(string name)
        {
            if (PList.TryGetValue(name, out var temp) && temp is NSString value)
                return value.ToString();
            return null;
        }

        private IList<string>? GetStringListEntitlement(string name)
        {
            if (PList.TryGetValue(name, out var temp) && temp is NSArray array)
            {
                return array.Select(v => ((NSString)v).ToString()).ToList();
            }
            return null;
        }

        public void SetBoolEntitlement(string name, bool value)
        {
            PList[name] = new NSNumber(value);
        }

        private void SetStringEntitlement(string name, string? value)
        {
            if (value == null)
            {
                PList.Remove(name);
            }
            else
            {
                PList[name] = new NSString(value);
            }
        }

        private void SetStringListEntitlement(string name, IList<string>? value)
        {
            if (value == null)
            {
                PList.Remove(name);
            }
            else
            {
                var array = new NSArray();
                foreach(var v in value)
                {
                    array.Add(new NSString(v));
                }

                PList[name] = array;
            }
        }
    }
}