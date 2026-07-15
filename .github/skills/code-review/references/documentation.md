# Documentation & Comments

_Rules for comments, XML docs, deferred-work tracking, and breaking-change documentation. Part of the [code-review skill](../SKILL.md)._

- **Comments should explain why, not restate code.** Delete comments like `// Get the types` that just duplicate the code in English. Don't include historical context about why code changed.
- **Delete or update obsolete comments when code changes.** Stale comments describing old behavior are worse than no comments.
- **Track deferred work with GitHub issues and searchable TODOs.** Reference a tracking issue in TODO comments with a consistent prefix (e.g., `TODO-Async:`). Remove ancient TODOs that will never be addressed.
- **Don't duplicate comments on interface implementations.** Documentation comments belong on the interface definition. Duplicating leads to divergence.
- **Add XML doc comments on all new public APIs.** These seed the official API documentation on learn.microsoft.com. Properties should start with "Gets the ..." or "Gets or sets the ...". Do not add XML docs to test code.
- **Use SHA-specific or commit-based links in documentation.** Don't use branch-relative links that break when files move.
- **Reference ECMA-335 and spec sources in metadata code.** When parsing signatures and metadata, cite the relevant ECMA-335 section. Cite CAVP/ACVP sources in crypto test vectors.
- **File breaking change documentation for behavioral changes.** Open an issue in dotnet/docs using the template, send notification to the .NET Breaking Change Notification DL. Applies even to prerelease-to-prerelease changes.
- **Use established terminology in user-facing text.** Do not expose internal type names, private field names, or codenames like "Roslyn" in public docs or error messages.
- **Retain copyright headers and license information.** All C# and C++ source files must include the standard license header, including test files. When porting from other projects, retain original copyright and update THIRD-PARTY-NOTICES.TXT.
