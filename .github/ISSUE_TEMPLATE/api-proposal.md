---
name: API Proposal
about: Propose a change to the public API surface.
title: "[API Proposal] Your title goes here"
labels: api-suggestion
assignees: ''

---

# Background and Motivation

Please provide a thorough explanation of what might necessitate a change to the current APIs.

# Proposed API

Please provide the precise public API signature diff that you are proposing. If modifying existing API signatures this should be expressed using diff blocks, for example
```diff
namespace System.Collections.Generic
{
-    public class HashSet<T> : ICollection<T>, ISet<T> {
+    public class HashSet<T> : ICollection<T>, ISet<T>, IReadOnlySet<T> {
     }
```

# Usage Examples

Please provide code examples that highlight how the proposed API changes are meant to be consumed.

# Risks

Please mention any risks that to your knowledge the API proposal might entail, such as breaking changes, performance regressions, etc.
