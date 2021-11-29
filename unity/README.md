# .NET Runtime - Unity Details

This is Unity's fork of the .NET Runtime repository.

The difference between this fork and the upstream repository should be as small as possible, with all of the differences specific to Unity. Our goal is to upstream as many changes made here as possible.

## Pulling changes from upstream

There is a job in Unity's internal CI which runs weekly to pull the latest code from the upstream [dotnet/runtime](https://github.com/dotnet/runtime) repository `main` branch and create a pull request to merge these changes to the [`unity-main`](https://github.com/Unity-Technologies/runtime/tree/unity-main) branch.

## Pushing changes to upstream

When a pull request is open against this fork, we should determine if the changes in that pull request should be pushed upstream (most should). Ideally, pull request should be organized so that all changes in a given pull request can be directly applied upstream. Any changes specific to the Unity fork should be done in a separate pull request.

Assuming the branch with changes to upstream is named `great-new-feature` then a new branch of upstream [`main`](https://github.com/dotnet/runtime/tree/main) named `upstream-great-new-feature` should be created. Each commit from `great-new-feature` should be cherry-picked `upstream-great-new-feature`, and then a pull request should be opened from `upstream-great-new-feature` to [`main`](https://github.com/dotnet/runtime/tree/main) in the upstream repository.

It is acceptable to _merge_ changes to this fork from `great-new-feature` before `upstream-great-new-feature` is merged, but we should at least _open_ an upstream pull request first.
