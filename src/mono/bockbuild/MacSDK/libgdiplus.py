GitHubTarballPackage(
    'mono',
    'libgdiplus',
    '2.11',
    '4e7ab0f555a13a6b2f954c714c4ee5213954ff79',
    configure='CFLAGS="%{gcc_flags} %{local_gcc_flags} -I/opt/X11/include" ./autogen.sh --prefix="%{package_prefix}"',
    override_properties={
        'make': 'C_INCLUDE_PATH="" make'})
