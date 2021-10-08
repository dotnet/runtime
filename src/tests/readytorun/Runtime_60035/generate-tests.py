# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

from pathlib import Path

class Isa:
    def __init__(self, class_name, namespace, base_class, subclasses_names, is_supported_property_name, config_env_var):
        self.class_name = class_name
        self.namespace = namespace
        self.base_class = base_class
        self.subclasses_names = subclasses_names
        self.is_supported_property_name = is_supported_property_name
        self.config_env_var = config_env_var

    def __repr__(self):
        return self.class_fully_qualified_name()

    def class_fully_qualified_name(self):
        return f'{self.namespace}.{self.class_name}'

class X86Isa(Isa):
    def __init__(self, class_name, base_class, config_env_var):
        super().__init__(class_name, 'System.Runtime.Intrinsics.X86', base_class, ['X64'], 'IsSupported', config_env_var)

class ArmIsa(Isa):
    def __init__(self, class_name, base_class, config_env_var):
        super().__init__(class_name, 'System.Runtime.Intrinsics.Arm', base_class, ['Arm64'], 'IsSupported', config_env_var)

X86Base   = X86Isa('X86Base', None, 'EnableHWIntrinsic')
Sse       = X86Isa('Sse', X86Base, 'EnableSSE')
Sse2      = X86Isa('Sse2', Sse, 'EnableSSE2')
Sse3      = X86Isa('Sse3', Sse2, 'EnableSSE3')
Ssse3     = X86Isa('Ssse3', Sse3, 'EnableSSSE3')
Sse41     = X86Isa('Sse41', Ssse3, 'EnableSSE41')
Sse42     = X86Isa('Sse42', Sse41, 'EnableSSE42')
Avx       = X86Isa('Avx', Sse42, 'EnableAVX')
Avx2      = X86Isa('Avx2', Avx, 'EnableAVX2')
AvxVnni   = X86Isa('AvxVnni', Avx, 'EnableAVXVNNI')
Bmi1      = X86Isa('Bmi1', X86Base, 'EnableBMI1')
Bmi2      = X86Isa('Bmi2', X86Base, 'EnableBMI2')
Fma       = X86Isa('Fma', Avx, 'EnableFMA')
Lzcnt     = X86Isa('Lzcnt', X86Base, 'EnableLZCNT')
Pclmulqdq = X86Isa('Pclmulqdq', Sse2, 'EnablePCLMULQDQ')
Popcnt    = X86Isa('Popcnt', Sse42, 'EnablePOPCNT')

X86Isas = [X86Base, Sse, Sse2, Sse3, Ssse3, Sse41, Sse42, Avx, Avx2, AvxVnni, Bmi1, Bmi2, Fma, Lzcnt, Pclmulqdq, Popcnt]

ArmBase = ArmIsa('ArmBase', None, 'EnableHWIntrinsic')
AdvSimd = ArmIsa('AdvSimd', ArmBase, 'EnableArm64AdvSimd')
Aes     = ArmIsa('Aes', ArmBase, 'EnableArm64Aes')
Crc32   = ArmIsa('Crc32', ArmBase, '(EnableArm64Crc32')
Dp      = ArmIsa('Dp', AdvSimd, 'EnableArm64Dp')
Rdm     = ArmIsa('Rdm', AdvSimd, 'EnableArm64AdvSimd_v81')
Sha1    = ArmIsa('Sha1', ArmBase, 'EnableArm64Sha1')
Sha256  = ArmIsa('Sha256', ArmBase, 'EnableArm64Sha256')

ArmIsas = [ArmBase, AdvSimd, Aes, Crc32, Dp, Rdm, Sha1, Sha256]

AllIsas = X86Isas + ArmIsas

def build_isas_by_env_var_dict(isas):
    isas_by_env_var = dict()

    for isa in isas:
        isas_by_env_var[isa.config_env_var] = [isa]

    for isa in isas:
        base_class = isa.base_class
        while not base_class is None:
            isas_by_env_var[base_class.config_env_var].append(isa)
            base_class = base_class.base_class

    return isas_by_env_var

def build_subclasses_fully_qualified_names(isa):
    class_fqn = isa.class_fully_qualified_name()
    subclasses_fq_names = [class_fqn]
    subclasses_fq_names.extend(f'{class_fqn}.{subclass_name}' for subclass_name in isa.subclasses_names)
    return subclasses_fq_names

def build_helper_class_name(fully_qualified_class_name):
    return 'Helper_' + fully_qualified_class_name.replace('.', '_')

def build_is_supported_method_name(isa):
    return f'{isa.namespace}.{isa.class_name}.IsSupported'

class ProgramClassBuilder:
    def __init__(self, isas):
        self._isas = isas

    def _begin_test_class(self):
        self._str += '''// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Runtime_60035
{
    static class Program
    {
        public static int Main()
        {
            bool success = true;

            if (!TestEnvironmentVariablesAreWorking())
            {
                success = false;
            }

            if (!TestReadyToRunAssumptionsAreCorrect())
            {
                success = false;
            }

            return success ? 100 : 0;
        }
'''

    def _add_test_environment_variables_are_working(self):
        self._str += '''
        static bool TestEnvironmentVariablesAreWorking()
        {
            bool success = true;
'''

        for env_var, isas in build_isas_by_env_var_dict(self._isas).items():
            dotnet_config_env_var = f'DOTNET_{env_var}'
            complus_config_env_var = f'COMPlus_{env_var}'

            self._str += f'''
            if (String.Equals(Environment.GetEnvironmentVariable("{dotnet_config_env_var}"), "0") || String.Equals(Environment.GetEnvironmentVariable("{complus_config_env_var}"), "0"))
            {{'''
            for isa in isas:
                for class_name in build_subclasses_fully_qualified_names(isa):
                    is_supported_property_name = f'{class_name}.{isa.is_supported_property_name}'
                    self._str += f'''
                if ({is_supported_property_name})
                {{
                    success = false;
                    Console.WriteLine("ERROR: Either {dotnet_config_env_var} or {complus_config_env_var} is '0' but {is_supported_property_name} returns 'True'");
                }}
'''
            self._str += '''            }
'''
        self._str += '''
            return success;
        }
'''

    def _add_test_ready_to_run_assumptions_are_correct(self):
        self._str += '''
        static bool TestReadyToRunAssumptionsAreCorrect()
        {
            bool success = true;
'''

        for isa in self._isas:
            for class_name in build_subclasses_fully_qualified_names(isa):
                helper_class_name = f'{build_helper_class_name(class_name)}'
                is_supported_property_name = f'{class_name}.{isa.is_supported_property_name}'
                helper_is_supported_property_name = f'{helper_class_name}.{isa.is_supported_property_name}'
                self._str += f'''
            if ({is_supported_property_name} != {helper_is_supported_property_name})
            {{
                success = false;
                Console.WriteLine("ERROR: {is_supported_property_name} returns '{{0}}' while a loaded into the process ReadyToRun image assumes the value being '{{1}}'", {is_supported_property_name}, {helper_is_supported_property_name});
            }}
'''
        self._str += '''
            return success;
        }
'''

    def _end_test_class(self):
        self._str += '''    }
}
'''

    def build(self):
        self._str = ''
        self._begin_test_class()
        self._add_test_environment_variables_are_working()
        self._add_test_ready_to_run_assumptions_are_correct()
        self._end_test_class()
        return self._str

class HelperClassBuilder:
    def __init__(self, isa):
        self._isa = isa

    def _begin_test(self):
        self._str += '''// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Runtime_60035
{'''

    def _add_test_classes(self):
        for class_fully_qualified_name in build_subclasses_fully_qualified_names(self._isa):
            self._str += f'''
    public static class {build_helper_class_name(class_fully_qualified_name)}
    {{
        public static bool IsSupported
        {{
            [MethodImpl(MethodImplOptions.NoInlining)]
            get {{ return {class_fully_qualified_name}.{isa.is_supported_property_name}; }}
        }}
    }}
'''

    def _end_test(self):
        self._str += '''}
'''

    def build(self):
        self._str = ''
        self._begin_test()
        self._add_test_classes()
        self._end_test()
        return self._str

HelperMSBuildProject = f'''<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Library</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="$(MSBuildProjectName).cs" />
  </ItemGroup>

</Project>
'''

class ProgramMSBuildProjectBuilder:
    def __init__(self, isas):
        self._isas = isas

    def _begin_msbuild_project(self):
        self._str += '''<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
'''

    def _add_project_references(self):
        self._str += '''
  <ItemGroup>'''
        for isa in self._isas:
            helper_class_name = build_helper_class_name(isa.class_fully_qualified_name())
            self._str += f'''
    <ProjectReference Include="{helper_class_name}.csproj" />'''
        self._str += '''
  </ItemGroup>
'''

    def _add_clrtest_batch_pre_commands(self):
        self._str += '''
  <PropertyGroup>
    <CLRTestBatchPreCommands><![CDATA[
$(CLRTestBatchPreCommands)
setlocal
'''
        for isa in self._isas:
            helper_class_name = build_helper_class_name(isa.class_fully_qualified_name())
            crossgen2_command_line = f'%CORE_ROOT%\corerun.exe %CORE_ROOT%\crossgen2\crossgen2.dll -r:%CORE_ROOT%\*.dll -o:{helper_class_name}.dll ..\{helper_class_name}\{helper_class_name}.dll'
            self._str += f'''
{crossgen2_command_line}

set CrossGenStatus=!ERRORLEVEL!
if not !CrossGenStatus! == 0 (
    echo ERROR: "{crossgen2_command_line}" returned %CrossGenStatus%
    exit /b 1
)
'''
        self._str += '''
endlocal
]]></CLRTestBatchPreCommands>
  </PropertyGroup>
'''
    def _end_msbuild_project(self):
        self._str += '''
  <ItemGroup>
    <Compile Include="$(MSBuildProjectName).cs" />
  </ItemGroup>
</Project>
'''

    def build(self):
        self._str = ''
        self._begin_msbuild_project()
        self._add_project_references()
        self._add_clrtest_batch_pre_commands()
        self._end_msbuild_project()
        return self._str

if __name__ == "__main__":
    tests_dir = Path(__file__).parent

    builder = ProgramClassBuilder(AllIsas)
    with open(tests_dir.joinpath('Runtime_60035.cs'), 'w') as out:
        out.write(builder.build())

    builder = ProgramMSBuildProjectBuilder(AllIsas)
    with open(tests_dir.joinpath('Runtime_60035.csproj'), 'w') as out:
        out.write(builder.build())

    for isa in AllIsas:
        helper_class_name = build_helper_class_name(isa.class_fully_qualified_name())

        builder = HelperClassBuilder(isa)
        with open(tests_dir.joinpath(f'{helper_class_name}.cs'), 'w') as out:
            out.write(builder.build())

        with open(tests_dir.joinpath(f'{helper_class_name}.csproj'), 'w') as out:
            out.write(HelperMSBuildProject)
