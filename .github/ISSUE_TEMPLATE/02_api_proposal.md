---
name: API proposal
about: Propose a change to the public API surface
title: ''
labels: api-suggestion
assignees: ''

---

## Background and Motivation

<!--
We welcome API proposals! We have a process to evaluate the value and shape of new API. There is an overview of our process [here](https://github.com/dotnet/runtime/blob/master/docs/project/api-review-process.md). This template will help us gather the information we need to start the review process.
First, please describe the purpose and value of the new API here.
-->

## Proposed API

<!--
Please provide the specific public API signature diff that you are proposing. For example:
```diff
namespace System.Collections.Generic
{
-    public class HashSet<T> : ICollection<T>, ISet<T> {
+    public class HashSet<T> : ICollection<T>, ISet<T>, IReadOnlySet<T> {
     }
```
You may find the [Framework Design Guidelines](https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/framework-design-guidelines-digest.md) helpful.
-->

## Usage Examples

<!--
Please provide code examples that highlight how the proposed API additions are meant to be consumed.
This will help suggest whether the API has the right shape to be functional, performant and useable.
You can use code blocks like this:
``` C#
// some lines of code here
```
-->

## Alternative Designs

<!--
Were there other options you considered, such as alternative API shapes?
-->

## Risks

<!--
Please mention any risks that to your knowledge the API proposal might entail, such as breaking changes, performance regressions, etc.
-->
