These are the tasks that are pending in the MSVC scripts to fully roll it out:

[ ] Validate that all generated assemblies are identical
[ ] Add support for listing CLEAN_FILES in the `csproj` file
[ ] Adding an "install" target
[ ] On Windows- have a solution that builds both runtime and libraries all in one
[ ] Add the other profiles (mobile, iOS, etc)
[ ] Generate the dependency files
[ ] Eliminate the need for "build-libs.sh/build-libs.bat" at the toplevel with proper MSBuild idioms
[ ] Integrate the "update-solution-files" with each build, so we auto-update the files on commits
[ ] Make it work with MSBuild instead of xbuild
