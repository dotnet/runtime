ABI Compatibility test
----------------------

abicheck.sh uses libabigail to check ABI stability.
It will abort if the current source
tree has a change that breaks binary compatibility.

This protects against the common scenario where:
- an app is compiled against the current zlib-ng
- the system package manager updates the zlib-ng shared library
- the app now crashes because some symbol is
  missing or some public structure or parameter
  has changed type or size

If run with --zlib-compat, it verifies that the
current source tree generates a library that
is ABI-compatible with the reference release
of classic zlib.  This ensures that building
zlib-ng with --zlib-compat does what it says on the tin.

abicheck.sh is not perfect, but it can catch
many common compatibility issues.

Cached files test/abi/*.abi
---------------------------

Comparing to the old version of zlib (or zlib-ng)
means someone has to check out and build
the previous source tree and extract its .abi
using abidw.  This can be slow.

If you don't mind the slowness, run abicheck.sh --refresh-if,
and it will download and build the reference version
and extract the .abi on the spot if needed.
(FIXME: should this be the default?)

On the next run, the reference .abi file will already be
present, and that step will be skipped.
It's stored in the tests/abi directory,
in a file with the architecture and git hash in the name.

If you're running continuous integration
which clear out the source tree on each run,
and you don't want your build machines
constantly downloading and building the old
version, you can check the .abi file into git.

To make this easier, a helper script could be written to automatically build
all the configurations tested by .github/workflows/abicheck.yml
Then they could be checked into git en masse by a maintainer
when a new platform is added or a new major version (which
intentionally breaks backwards compatibility) is being prepared.

Further reading
---------------

- https://sourceware.org/libabigail/manual/
- https://developers.redhat.com/blog/2014/10/23/comparing-abis-for-compatibility-with-libabigail-part-1/
- https://developers.redhat.com/blog/2020/04/02/how-to-write-an-abi-compliance-checker-using-libabigail/
