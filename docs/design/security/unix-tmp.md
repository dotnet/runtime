
# Unix temporary files

The Unix support for temporary files is different from the Windows model and developers who
are used to Windows may inadvertently create security risk if they use the same practices on Unix.

Most notably, the Windows model for temporary files is that the operating system provides each user with a *unique*, *user-owned* temporary directory.
Moreover, all Windows users, including the service and system users, have designated user folders, including temporary folders.

The Unix model is very different. The temp directory, assuming there is one, is often a global folder (except on MacOS).
If possible, prefer a library function like `GetTempPath()` to find the folder. Otherwise,
the `TMPDIR` environment variable is used to store the location of this folder. This variable is
widely used and supported, but it is not mandatory for all Unix implementations. It should be the preferred
mechanism for finding the Unix temporary folder if a library method is not available. It will commonly
point to either the `/tmp` or `/var/tmp` folder. These folders are not used for MacOS, so it is not recommended
to use them directly.

Because the temporary directory is often global, any use of the temp directory should be carefully
considered. In general, the best use of the temp directory is for programs which,

1. Will create the temporary file during their process execution
1. Do not depend on predictable temporary file/folder names
1. Will not access the file after the process exits

In these cases, the process can create a file or files with
  1. A pseudorandom name, unlikely to cause collisions
  1. Permissions which restrict all access to owner-only, i.e. 700 for directories, 600 for files

Any other use needs to be carefully audited, particularly if the temporary file is intended for use across
multiple processes. Some considerations:

- **Never** write files with global access permissions
- **Always** verify that the owner of the file is the current user and that the permissions
  only allow write access by the owner when reading existing files
- **Never** rely on having ownership of a particular file name. Any process can write a file with that name,
  creating a denial of service.
  - When creating files, consider likelihood of file name collision and performance impact of attempting
    to create new names, if supported.

 If any of the above conflict with the feature requirements, consider instead writing temporary files to a
 location in the user home folder. Some considerations for this model:

 - There is no automatic cleanup in user folders. Files will remain permanently or require cleanup by the app
 - Some environments do not have user home folders (e.g., systemd). Consider providing an environment variable
   to override the location of the temporary folder, and provide user documentation for this variable.
