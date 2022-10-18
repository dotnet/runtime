using Claunia.PropertyList;

namespace Melanzana.CodeSign
{
    public class Bundle
    {
        public string BundlePath { get; private set; }

        public string ContentsPath { get; private set; }

        private readonly bool hasContents;
        private readonly bool hasResources;
        private readonly string? mainExecutable;
        private readonly NSDictionary? infoPList;
        private readonly string? bundleIdentifier;

        public Bundle(string path)
        {
            BundlePath = path;

            // Detect bundle type:
            // - Shallow (eg. iOS)
            // - `Contents/` (eg. macOS)
            // - `Support Files/`
            // - `Versions/...`
            // - installer package
            // - widget
            // - etc.

            hasContents = Directory.Exists(Path.Combine(path, "Contents"));
            ContentsPath = hasContents ? Path.Combine(path, "Contents") : path;

            // Look for Info.plist, then check CFBundleExecutable
            var infoPlistPath = Path.Combine(ContentsPath, "Info.plist");
            if (File.Exists(infoPlistPath))
            {
                infoPList = (NSDictionary)PropertyListParser.Parse(infoPlistPath);

                if (infoPList.TryGetValue("CFBundleExecutable", out var temp) && temp is NSString bundleExecutable)
                {
                    if (hasContents) // macOS
                    {
                        mainExecutable = Path.Combine("MacOS", (string)bundleExecutable);
                    }
                    else
                    {
                        mainExecutable = (string)bundleExecutable;
                    }
                    
                    if (!File.Exists(Path.Combine(ContentsPath, mainExecutable)))
                    {
                        mainExecutable = null;
                    }
                }

                if (infoPList.TryGetValue("CFBundleIdentifier", out temp) && temp is NSString bundleIdentifier)
                {
                    this.bundleIdentifier = (string)bundleIdentifier;
                }
            }
            else if (!hasContents && Directory.Exists(Path.Combine(BundlePath, "Versions")))
            {
                ContentsPath = Path.Combine(BundlePath, "Versions", "Current");

                // FIXME: Quick hack to get framework executables
                var guessedName = Path.GetFileNameWithoutExtension(path);
                if (File.Exists(Path.Combine(BundlePath, guessedName)))
                {
                    mainExecutable = guessedName;
                }
            }

            hasResources = Directory.Exists(Path.Combine(ContentsPath, "Resources"));
        }

        public string? MainExecutable => mainExecutable != null ? Path.Combine(ContentsPath, mainExecutable) : null;

        public string? BundleIdentifier => bundleIdentifier;

        public NSDictionary InfoPList => infoPList ?? new NSDictionary();

        public void AddResourceRules(ResourceBuilder builder, bool useV2Rules = true)
        {
            string resourcePrefix = hasResources ? "Resources/" : "";

            if (useV2Rules)
            {
                builder.AddRule(new ResourceRule("^.*"));

                // On macOS include nested signatures
                if (hasResources)
                {
                    builder.AddRule(new ResourceRule("^[^/]+") { IsNested = true, Weight = 10 });
                    builder.AddRule(new ResourceRule("^(Frameworks|SharedFrameworks|PlugIns|Plug-ins|XPCServices|Helpers|MacOS|Library/(Automator|Spotlight|LoginItems))/") { IsNested = true, Weight = 10 });
                    builder.AddRule(new ResourceRule($"^{resourcePrefix}") { Weight = 20 });
                }

                builder.AddRule(new ResourceRule(".*\\.dSYM($|/)") { Weight = 11 });

                // Exclude specific files:
                builder.AddRule(new ResourceRule("^Info\\.plist$") { IsOmitted = true, Weight = 20 });
                builder.AddRule(new ResourceRule("^PkgInfo$") { IsOmitted = true, Weight = 20 });

                // Include specific files:
                builder.AddRule(new ResourceRule("^embedded\\.provisionprofile$") { Weight = 20 });
                builder.AddRule(new ResourceRule("^version.plist$") { Weight = 20 });

                builder.AddRule(new ResourceRule("^(.*/)?\\.DS_Store$") { IsOmitted = true, Weight = 2000 });
            }
            else
            {
                builder.AddRule(new ResourceRule("^version.plist$"));
                builder.AddRule(new ResourceRule(hasResources ? $"^{resourcePrefix}" : "^.*"));
            }

            builder.AddRule(new ResourceRule($"^{resourcePrefix}.*\\.lproj/") { IsOptional = true, Weight = 1000 });
            builder.AddRule(new ResourceRule($"^{resourcePrefix}Base\\.lproj/") { Weight = 1010 });
            builder.AddRule(new ResourceRule($"^{resourcePrefix}.*\\.lproj/locversion.plist$") { IsOmitted = true, Weight = 1100 });

            // Add implicit exclusions
            builder.AddExclusion("_CodeSignature");
            builder.AddExclusion("CodeResources");
            builder.AddExclusion("_MASReceipt");
            if (mainExecutable != null)
            {
                builder.AddExclusion(mainExecutable);
            }
        }
    }
}
