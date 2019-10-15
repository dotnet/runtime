@echo off
csc /target:library NamespaceForwardedCS.cs
csc /r:NamespaceForwardedCS.dll /target:library /out:NamespaceTests.dll NamespaceTests.cs