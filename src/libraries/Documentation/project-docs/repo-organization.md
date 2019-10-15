Repo Organization
=================

Tests for a project are kept under the `tests` folder, which is a peer of the `src` folder.  If you need to have multiple test projects for a component, structure them in sub folders.

For example, lay things out like this:

```
tests\
    test_project_1\
        test_1.cs
        test_2.cs
        test_project_1.csproj
    test_project_2\
        test_1.cs
        test_2.cs
        test_project_2.csproj
```

Not like this:

```
tests\
    test_project_1.csproj
    test_project_2.csproj
    test_folder_1\
        test_1.cs
        test_2.cs
    test_folder_2\
        test_1.cs
        test_2.cs
```
