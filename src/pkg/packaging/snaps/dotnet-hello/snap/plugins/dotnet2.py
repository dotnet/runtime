# -*- Mode:Python; indent-tabs-mode:nil; tab-width:4 -*-
#
# Copyright (C) 2017 Canonical Ltd
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License version 3 as
# published by the Free Software Foundation.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.

"""The dotnet plugin is used to build dotnet core runtime parts.'

The plugin uses the dotnet SDK to install dependencies from nuget
and follows standard semantics from a dotnet core project.

This plugin uses the common plugin keywords as well as those for "sources".
For more information check the 'plugins' topic for the former and the
'sources' topic for the latter.

The plugin will take into account the following build-attributes:

    - debug: builds using a Debug configuration.

"""

import os
import shutil
import fnmatch
import urllib.request
import json

import snapcraft
from snapcraft import sources


_DOTNET_RELEASE_METADATA_URL="https://raw.githubusercontent.com/dotnet/core/master/release-notes/releases.json"
_RUNTIME_DEFAULT = '2.0.5'
_SDK_DEFAULT = '2.1.4'

# TODO extend for more than xenial
_SDKS_AMD64 = {
    '2.0.0': dict(url_path='http://dotnetcli.blob.core.windows.net/dotnet/'
                           'Sdk/2.0.0/dotnet-sdk-2.0.0-linux-x64.tar.gz',
                  checksum='sha256/6059a6f72fb7aa6205ef4b52583e9c041f'
                           'd128e768870a0fc4a33ed84c98ca6b')
              }
# TODO extend for other architectures
_SDK_DICT_FOR_ARCH = {
    'amd64': _SDKS_AMD64,
}


class DotNet2Plugin(snapcraft.BasePlugin):

    @classmethod
    def schema(cls):
        schema = super().schema()

        schema['properties']['dotnet-runtime-version'] = {
            'type': 'string',
            'default': _RUNTIME_DEFAULT,
        }

        if 'required' in schema:
            del schema['required']

        return schema

    @classmethod
    def get_pull_properties(cls):
        # Inform Snapcraft of the properties associated with pulling. If these
        # change in the YAML Snapcraft will consider the build step dirty.
        return ['dotnet-runtime-version']

    @classmethod
    def get_build_properties(cls):
        # Inform Snapcraft of the properties associated with building. If these
        # change in the YAML Snapcraft will consider the build step dirty.
        return ['dotnet-runtime-version']

    def __init__(self, name, options, project):
        super().__init__(name, options, project)

        self._dotnet_dir = os.path.join(self.partdir, 'dotnet')
        self._dotnet_sdk_dir = os.path.join(self._dotnet_dir, 'sdk')

        self.stage_packages.extend([
            'libcurl3',
            'libcurl3-gnutls',
            'libicu55',
            'liblttng-ust0',
            'libunwind8',
            'lldb',
            'libssl1.0.0',
            'libgssapi-krb5-2',
            'libc6',
            'zlib1g',
            'libgcc1'
        ])

        self._sdk = self._get_sdk()
        self._dotnet_cmd = os.path.join(self._dotnet_sdk_dir, 'dotnet')

    def _get_sdk(self):
        try:
            sdk_arch = _SDK_DICT_FOR_ARCH[self.project.deb_arch]
        except KeyError as missing_arch:
            raise NotImplementedError(
                'This plugin does not support architecture '
                '{}'.format(missing_arch))
        # TODO support more SDK releases
        sdk_info = _get_sdk_info(self.option.dotnet-runtime-version)

        sdk_url = sdk_info['package_url']
        return sources.Tar(sdk_url, self._dotnet_sdk_dir,
                           source_checksum=sdk_info['checksum'])

    def pull(self):
        super().pull()

        os.makedirs(self._dotnet_sdk_dir, exist_ok=True)

        self._sdk.pull()

    def clean_pull(self):
        super().clean_pull()

        # Remove the dotnet directory (if any)
        if os.path.exists(self._dotnet_dir):
            shutil.rmtree(self._dotnet_dir)

    def build(self):
        super().build()

        if 'debug' in self.options.build_attributes:
            configuration = 'Debug'
        else:
            configuration = 'Release'

        self.run([self._dotnet_cmd, 'build', '-c', configuration])

        publish_cmd = [self._dotnet_cmd, 'publish', '-c', configuration,
                       '-o', self.installdir]
        # Build command for self-contained application
        publish_cmd += ['--self-contained', '-r', 'linux-x64']
        self.run(publish_cmd)

        # Workaround to set the right permission for the executable.
        appname = os.path.join(self.installdir, self._get_appname())
        if os.path.exists(appname):
            os.chmod(appname, 0o755)

    def _get_appname(self):
        for file in os.listdir(self.builddir):
            if fnmatch.fnmatch(file, '*.??proj'):
                return os.path.splitext(file)[0]
                break

    def _get_version_metadata(version):
        jsonData = _get_dotnet_release_metadata()
        package_data = list(filter(lambda x: x['version-runtime'] == version,jsonData))

        if not package_data or len(package_data) !=1:
            print('error occured while fetching  the version details or the version specified is incorrect')

        return package_data

    def _get_dotnet_release_metadata():
        package_metadata=[]

        req = urllib.request.Request(_DOTNET_RELEASE_METADATA_URL)
        r = urllib.request.urlopen(req).read()
        package_metadata = json.loads(r.decode('utf-8'))

        return package_metadata

    def _get_sdk_info(version):
        metadata= _get_version_metadata(version)

        sdk_package_url='{}{}'.format(metadata[0]['blob-sdk'],metadata[0]['sdk-linux-x64'])
        sdk_checksum = _get_package_checksum(metadata[0]['checksums-sdk'],metadata[0]['sdk-linux-x64'])

        return {'package_url':sdk_package_url,'checksum':sdk_checksum}

    def _get_package_checksum(checksum_url,filename):
        req = urllib.request.Request(checksum_url)
        data = urllib.request.urlopen(req)

        checksum=[]
        for line in data:
           text = str(line,'utf-8').split()
           if len(text) == 2 and filename in text[1]:
               checksum=text[0]
               break

        return checksum

    def env(self, root):
        env = os.environ.copy()
        env['PATH'] = '{}:{}'.format(
            os.path.dirname(self._dotnet_cmd), env['PATH'])
        return env
