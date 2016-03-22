// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: 
**
**
===========================================================*/
namespace System {
    using System.Runtime.Serialization;
    using System.Globalization;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;


    [ComVisible(true)]
    [Serializable]
    public sealed class OperatingSystem : ICloneable , ISerializable
    {
        private Version _version;
        private PlatformID _platform;
        private string _servicePack;
        private string _versionString;

        private OperatingSystem()
        {
        }

        public OperatingSystem(PlatformID platform, Version version) : this(platform, version, null) {
        }
    
        internal OperatingSystem(PlatformID platform, Version version, string servicePack) {
            if( platform < PlatformID.Win32S || platform > PlatformID.MacOSX) {
                throw new ArgumentException(
                    Environment.GetResourceString("Arg_EnumIllegalVal", (int)platform),
                    "platform");
            }

            if ((Object) version == null)
                throw new ArgumentNullException("version");
            Contract.EndContractBlock();

            _platform = platform;
            _version = (Version) version.Clone();
            _servicePack = servicePack;
        }
        
        private OperatingSystem(SerializationInfo info, StreamingContext context) {            
            SerializationInfoEnumerator enumerator = info.GetEnumerator();                        
            while( enumerator.MoveNext()) {
                switch( enumerator.Name) {
                    case "_version":
                        _version = (Version) info.GetValue("_version", typeof(Version));
                        break;
                    case "_platform":
                        _platform = (PlatformID) info.GetValue("_platform", typeof(PlatformID));
                        break;
                    case "_servicePack":
                        _servicePack = info.GetString("_servicePack");
                        break;
                }
            }

            if (_version == null ) {
                throw new SerializationException(Environment.GetResourceString("Serialization_MissField", "_version"));
            }
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            if( info == null ) {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();

            info.AddValue("_version", _version);
            info.AddValue("_platform", _platform);
            info.AddValue("_servicePack", _servicePack);
        }        

        public PlatformID Platform {
            get { return _platform; }
        }
        
        public string ServicePack { 
            get { 
                if( _servicePack == null) {
                    return string.Empty;
                }

                return _servicePack;
            }
        }    

        public Version Version {
            get { return _version; }
        }
    
        public Object Clone() {
            return new OperatingSystem(_platform,
                                       _version, _servicePack );
        }
    
        public override String ToString() {
            return VersionString;
        }

        public String VersionString {
            get {
                if(_versionString != null) {
                    return _versionString;
                }

                String os;
                switch(_platform)
                {
                    case PlatformID.Win32NT:
                        os = "Microsoft Windows NT ";
                        break;
                    case PlatformID.Win32Windows:
                        if ((_version.Major > 4) ||
                            ((_version.Major == 4) && (_version.Minor > 0)))
                            os = "Microsoft Windows 98 ";
                        else
                            os = "Microsoft Windows 95 ";
                        break;
                    case PlatformID.Win32S:
                        os = "Microsoft Win32S ";
                        break;
                    case PlatformID.WinCE:
                        os = "Microsoft Windows CE ";
                        break;
                    case PlatformID.MacOSX:
                        os = "Mac OS X ";
                        break;
                    default:
                        os = "<unknown> ";
                        break;
                }

                if( String.IsNullOrEmpty(_servicePack)) {
                    _versionString = os + _version.ToString();
                }
                else {
                    _versionString = os + _version.ToString(3) + " " + _servicePack;
                }

                return _versionString;            
            }
        }
    }
}
