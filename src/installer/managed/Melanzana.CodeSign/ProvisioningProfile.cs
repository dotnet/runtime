using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Claunia.PropertyList;

namespace Melanzana.CodeSign
{
    public class ProvisioningProfile
    {
        private readonly NSDictionary plist;

        public ProvisioningProfile(byte[] bytes)
        {
            var signedCms = new SignedCms();
            signedCms.Decode(bytes);
            var contentInfo = signedCms.ContentInfo;
            var content = contentInfo.Content;

            plist = (NSDictionary)XmlPropertyListParser.Parse(content);
        }

        public ProvisioningProfile(string fileName)
            : this(File.ReadAllBytes(fileName))
        {
        }

        public IList<string> TeamIdentifiers => GetStringArray("TeamIdentifier");

        public IEnumerable<X509Certificate2> DeveloperCertificates =>
            ((NSArray)plist["DeveloperCertificates"]).OfType<NSData>().Select(d => new X509Certificate2((byte[])d));

        public NSDictionary Entitlements => (NSDictionary)plist["Entitlements"];

        public string AppIDName => plist["AppIDName"].ToString()!;

        public IList<string> ApplicationIdentifierPrefix => GetStringArray("ApplicationIdentifierPrefix");

        public DateTimeOffset CreationDate => new DateTimeOffset(((NSDate)plist["CreationDate"]).Date);

        public IList<string> Platform => GetStringArray("Platform");

        public DateTimeOffset ExpirationDate => new DateTimeOffset(((NSDate)plist["ExpirationDate"]).Date);

        public string Name => plist["Name"].ToString()!;

        public IList<string> ProvisionedDevices => GetStringArray("ProvisionedDevices");

        public string TeamName => plist["TeamName"].ToString()!;

        public Guid UUID => new Guid(plist["UUID"].ToString()!);

        public int Version => (int)plist["Version"].ToObject();

        private IList<string> GetStringArray(string name)
            => ((NSArray)plist[name]).Select(v => v.ToString()!).ToArray();

        public override string ToString() => this.Name;

        public NSDictionary PropertyList => this.PropertyList;

        public void Save(string filename)
        {
            File.WriteAllText(filename, this.plist.ToXmlPropertyList());
        }
    }
}