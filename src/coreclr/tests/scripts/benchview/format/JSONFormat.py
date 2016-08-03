from benchview.format.helpers import is_valid_datetime
from benchview.format.helpers import is_valid_description
from benchview.format.helpers import is_valid_name

def get_valid_submission_types() -> list:
    return [t.casefold() for t in ['rolling', 'private', 'local']]

def is_valid_submission_types(runtype: str) -> bool:
    return [runtype.casefold() in get_valid_submission_types()]

class Build(object):
    def __init__(self, repository: str, branch: str, number: str, timestamp: str, buildType: str):
        if not is_valid_name(repository):
            raise ValueError('Build.repository must be a string and it cannot be empty or white space.')
        self.repository = repository

        if not is_valid_name(branch):
            raise ValueError('Build.branch must be a string and it cannot be empty or white space.')
        self.branch = branch

        if not is_valid_name(number):
            raise ValueError('Build.number must be a string and it cannot be empty or white space.')
        self.number = number

        if not is_valid_datetime(timestamp):
            raise ValueError('Invalid Build.sourceTimestamp (It must be a date-time from RFC 3339, Section 5.6. "%%Y-%%m-%%dT%%H:%%M:%%SZ").')
        self.sourceTimestamp = timestamp

        if not is_valid_submission_types(buildType):
            raise ValueError('Build.type is not any of the valid options: {0}.'.format(get_valid_submission_types()))
        self.type = buildType

class Configuration(object):
    def __init__(self, displayName: str, properties: dict):
        if not is_valid_name(displayName):
            raise ValueError('Configuration.displayName must be a string and it cannot be empty or white space.')
        self.displayName = displayName

        # Currently this is encoded in the Config.Name column of the Config table.
        if not type(properties) is dict:
            raise TypeError('Configuration.properties must be of type dict.')
        #TODO: Verify that there is at least one config?
        for key, value in properties.items():
            if not is_valid_name(key) or not is_valid_name(value):
                raise ValueError('Configuration.properties keys or values cannot be empty or white space.')
        self.properties = properties

class Machine(object):
    def __init__(self, name: str, architecture: str, manufacturer: str, cores: int, threads: int, physicalMemory: float):
        if not is_valid_name(name):
            raise ValueError('Machine.name must be a string and it cannot be empty or white space.')
        self.name = name

        if not is_valid_name(architecture):
            raise ValueError('Machine.architecture must be a string and it cannot be empty or white space.')
        self.architecture = architecture

        if not is_valid_name(manufacturer):
            raise ValueError('Machine.manufacturer must be a string and it cannot be empty or white space.')
        self.manufacturer = manufacturer

        if not type(cores) is int:
            raise TypeError('Machine.cores must be of "int" type.')
        if not cores > 0:
            raise ValueError('Machine.cores must be greater than zero.')
        self.cores = cores

        if not type(threads) is int:
            raise TypeError('Machine.threads must be of "int" type.')
        if not threads > 0:
            raise ValueError('Machine.threads must be greater than zero.')
        self.threads = threads

        if not type(physicalMemory) is float:
            raise TypeError('Machine.physicalMemory must be of "float" type.')
        if not physicalMemory > 1:
            raise ValueError('Machine.physicalMemory must be greater than one.')
        self.physicalMemory = physicalMemory

class OperatingSystem(object):
    def __init__(self, name: str, version: str, edition: str, architecture: str):
        if not is_valid_name(name):
            raise ValueError('OperatingSystem.name must be a string and it cannot be empty or white space.')
        self.name = name

        if not is_valid_name(version):
            raise ValueError('OperatingSystem.version must be a string and it cannot be empty or white space.')
        self.version = version

        if not is_valid_name(edition):
            raise ValueError('OperatingSystem.edition must be a string and it cannot be empty or white space.')
        self.edition = edition

        if not is_valid_name(architecture):
            raise ValueError('OperatingSystem.architecture must be a string and it cannot be empty or white space.')
        self.architecture = architecture

class Metric(object):
    def __init__(self, name: str, unit: str, greaterTheBetter: bool, ismachinedependent: bool):

        if not is_valid_name(name):
            raise ValueError('Metric.name cannot be empty or white space.')
        self.name = name

        if not is_valid_name(unit):
            raise ValueError('Metric.unit cannot be empty or white space.')
        self.unit = unit

        if not type(greaterTheBetter) is bool:
            raise TypeError('Metric.greaterTheBetter must be a boolean.')
        self.greaterTheBetter = greaterTheBetter

        if not type(ismachinedependent) is bool:
            raise TypeError('Metric.ismachinedependent must be a boolean.')
        self.isMachineDependent = ismachinedependent

    def __eq__(self, rhs):
        same_type               = type(self) == type(rhs)
        same_name               = self.name.casefold()      == rhs.name.casefold()
        same_unit               = self.unit.casefold()      == rhs.unit.casefold()
        same_greaterTheBetter   = self.greaterTheBetter     == rhs.greaterTheBetter
        same_ismachinedependent = self.isMachineDependent   == rhs.isMachineDependent
        return same_type and same_name and same_unit and same_greaterTheBetter and same_ismachinedependent

class Result(object):
    def __init__(self, metric: 'Metric'):
        if not type(metric) is Metric:
            raise TypeError('Metric.metric must be of type benchview.format.JSONFormat.Metric.')
        self.metric = metric
        self.values = []

class Test(object):
    def __init__(self, name: str, results: list = None, tests: list = None):
        results = results or []
        tests = tests or []

        if not is_valid_name(name):
            raise ValueError('Test.name must be a string and it cannot be empty or white space.')
        self.name = name

        if not type(results) is list:
            raise TypeError('Test.results must be a list.')
        for result in results:
            if not type(result) is Result:
                raise TypeError('Test.results must be a list of benchview.format.JSONFormat.Result type objects')
        self.results = results

        if not type(tests) is list:
            raise TypeError('Test.tests must be a list.')
        for test in tests:
            if not type(test) is Test:
                raise TypeError('Test.tests must be a list of benchview.format.JSONFormat.Test type objects.')
        self.tests = tests

class Run(object):
    def __init__(self, architecture: str, build: 'Build', configuration: 'Configuration', group: str, machine: 'Machine', machinePool: str, os: 'OperatingSystem', tests: list, runType: str):
        if not is_valid_name(architecture):
            raise ValueError('Run.architecture must be a string and it cannot be empty or white space.')
        self.architecture = architecture

        if not type(build) is Build:
            raise TypeError('Run.build must be of type benchview.format.JSONFormat.Build.')
        self.build = build

        if not type(configuration) is Configuration:
            raise TypeError('Run.configuration must be of type benchview.format.JSONFormat.Configuration.')
        self.configuration = configuration

        # Currently:  test_type + submission_type + branch (e.g. ViBench_Rolling_WinCComp)
        # Recommended value format "{Team} {Test Group}": VC++ ViBench|VC++ ViBench Baseline|VC++ TP|VC++ PNBench|CoreFX
        if not is_valid_name(group):
            raise ValueError('Run.group must be a string and it cannot be empty or white space.')
        self.group = group

        if not type(machine) is Machine:
            raise TypeError('Run.machine must be of type benchview.format.JSONFormat.Machine.')
        self.machine = machine

        if not is_valid_description(machinePool):
            raise TypeError('Run.machinePool must be a string and it cannot be empty or white space.')
        self.machinePool = machinePool

        if not type(os) is OperatingSystem:
            raise TypeError('Run.os must be of type benchview.format.JSONFormat.OperatingSystem.')
        self.os = os

        if not type(tests) is list:
            raise TypeError('Run.tests must be of list type.')
        for test in tests:
            if not type(test) is Test:
                raise TypeError('Run.tests must be a list of benchview.format.JSONFormat.Test type objects.')
        self.tests = tests

        if not is_valid_submission_types(runType):
            raise ValueError('Run.type is not any of the valid options: {0}.'.format(get_valid_submission_types()))
        self.type = runType

class Submission(object):
    def __init__(self, name: str, description: str, email: str, cuid: str, created: str):
        if not is_valid_name(name):
            raise ValueError('Submission.name must be a string and it cannot be empty or white space.')
        self.name = name

        if not is_valid_description(description):
            raise ValueError('Submission.description must be a string and it cannot be empty or white space.')
        self.description = description

        if not email or not '@' in email: # TODO: Perform stronger email validation here. Can we use plone.rfc822?
            raise TypeError('Submission.email field should be a valid email address.')
        self.email = email

        if not is_valid_datetime(created):
            raise ValueError('Invalid Submission.created. It must be a date-time from RFC 3339, Section 5.6. "%%Y-%%m-%%dT%%H:%%M:%%SZ"')
        self.created = created

        # TODO: Improve error checking for cuid.
        if not isinstance(cuid, str) or not len(cuid) == 25:
            raise ValueError('Invalid cuid object. Submission.cuid must be a string and its length must be 25.')
        self.cuid = cuid

        self.runs = []

class MachineData(object):
    """This is an intermediate type used to serialize/deserialize data among scripts."""
    def __init__(self, machine: 'Machine', os: 'OperatingSystem'):
        if not type(machine) is Machine:
            raise TypeError('MachineData.machine must of type benchview.format.JSONFormat.Machine')
        self.machine = machine

        if not type(os) is OperatingSystem:
            raise TypeError('MachineData.machine must of type benchview.format.JSONFormat.OperatingSystem')
        self.os = os
