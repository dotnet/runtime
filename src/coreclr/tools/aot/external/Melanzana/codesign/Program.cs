using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Claunia.PropertyList;
using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using RSAKeyVaultProvider;

namespace Melanzana.CodeSign
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var identity = new Argument<string>("identity", "Name of signing identity or \"-\" for ad-hoc signing");
            var path = new Argument<string>("path", "Path to bundle or executable on disk");
            var entitlements = new Option<string?>("--entitlements", "Path to entitlements to embed into the signature");
            var azureKekVaultUrl = new Option<string?>("--azure-key-vault-url", "URL to an Azure Key Vault");

            var signCommand = new Command("sign", "Sign code at path using given identity")
            {
                identity,
                path,
                entitlements,
                azureKekVaultUrl,
            };

            signCommand.SetHandler<string, string, string?, string?>(HandleSign, identity, path, entitlements, azureKekVaultUrl);

            return new RootCommand { signCommand }.Invoke(args);
        }

        private static X509Certificate2? FindCertificate(string identity)
        {
            if (identity == "-")
            {
                // Ad-hoc signing
                return null;
            }

            var store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection collection;

            if (identity.Length == 40 && identity.All(Uri.IsHexDigit))
            {
                collection = store.Certificates.Find(
                    X509FindType.FindByThumbprint, 
                    identity,
                    true);
            }
            else
            {
                collection = store.Certificates.Find(
                    X509FindType.FindBySubjectName,
                    identity,
                    true);
            }

            var certificate = collection.Where(c => c.IsAppleDeveloperCertificate()).FirstOrDefault();
            if (certificate == null)
            {
                throw new Exception($"Certificate for identity \"{identity}\" not found");
            }

            return certificate;
        }

        public static void HandleSign(
            string identity,
            string path,
            string? entitlements,
            string? azureKeyVaultUrl)
        {
            var codeSignOptions = new CodeSignOptions();

            if (String.IsNullOrEmpty(azureKeyVaultUrl))
            {
                codeSignOptions.DeveloperCertificate = FindCertificate(identity);
            }
            else
            {
                // Azure key vault
                var credential = new AzureCliCredential();
                var certClient = new CertificateClient(new Uri(azureKeyVaultUrl), credential);
                var azureCertificate = certClient.GetCertificate(identity).Value;
                codeSignOptions.DeveloperCertificate = new X509Certificate2(azureCertificate.Cer);
                codeSignOptions.PrivateKey = RSAFactory.Create(credential, azureCertificate.KeyId, codeSignOptions.DeveloperCertificate);
            }

            if (!String.IsNullOrEmpty(entitlements))
            {
                codeSignOptions.Entitlements = new Entitlements(
                    (NSDictionary)PropertyListParser.Parse(
                        new FileInfo(entitlements)
                    )
                );
            }

            var signer = new Signer(codeSignOptions);
            signer.Sign(path);
        }
    }
}
