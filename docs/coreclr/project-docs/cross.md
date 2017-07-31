# Automatic Security Assessment with CROSS

CROSS is a security code review tool we are now using for the .NET Core GitHub repos and stands for “Code Review for Open Source Software.”

## The Genesis of CROSS

As the .NET Core team threat modeled our OSS development process we discovered that many threats relating to malicious code and accidentally introduced security bugs are mitigated by code reviews. Since the volume of code changes to .NET Core and related projects is quite high, counting on busy developers to always catch subtle security issues can be problematic. We decided that some automation was in order to help developers quickly spot potentially risky changes. We wanted something that would mesh well with the current workflow for reviewing GitHub PRs. CROSS is the result!

## How CROSS Works

CROSS is invoked from the Jenkins continuous integration server whenever a PR gets submitted to GitHub. It uses GitHub APIs to access the PR data and then it scans the changed files. CROSS flags potentially risky changes for inspection based on a set of configurable security rules. Specific findings along with explanations and suggested fixes are posted as inline PR diff comments on github.com. This enables developers to inspect potentially risky changes as part of their normal GitHub PR code review workflow.

## Interpreting the Results

The findings are designed to help developers by flagging potential security issues for closer inspection but it is still up to the reviewer to determine if a real security issue is present. CROSS reduces the likelihood that security problems will be introduced and go unnoticed, especially for large change sets. Where possible, CROSS suggests more secure alternatives (e.g. safe vs. banned API methods or approved vs. deprecated crypto algorithms).
