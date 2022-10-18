using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Claunia.PropertyList;

namespace Melanzana.CodeSign
{
    public class ProvisioningProfile
    {
        private readonly NSDictionary plist;

        public ProvisioningProfile(string fileName)
        {
            var signedCms = new SignedCms();
            signedCms.Decode(File.ReadAllBytes(fileName));
            var contentInfo = signedCms.ContentInfo;
            var content = contentInfo.Content;

            plist = (NSDictionary)XmlPropertyListParser.Parse(content);

        }

        public IEnumerable<string> TeamIdentifiers => ((NSArray)plist["TeamIdentifier"]).Select(i => i.ToString()!).ToArray();

        public IEnumerable<X509Certificate2> DeveloperCertificates =>
            ((NSArray)plist["DeveloperCertificates"]).OfType<NSData>().Select(d => new X509Certificate2((byte[])d));

        public NSDictionary Entitlements => (NSDictionary)plist["Entitlements"];
    }
}