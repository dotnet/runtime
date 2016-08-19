from sys import version_info, path
import os.path

def add_dependencies_to_path(filePath: str) -> None:
    scriptDir = os.path.dirname(os.path.realpath(filePath))
    path.append(os.path.join(scriptDir, "dependencies")) 

def is_supported_version() -> bool:
    return version_info.major > 2 and version_info.minor > 4

def is_null_or_whitespace(name: str) -> bool:
    return not name or name.isspace()

def is_number(value: str) -> bool:
    try:
        float(value)
    except ValueError:
        try:
            float(int(value, 16))
        except ValueError:
            return False
    return True

def to_number(value: str) -> float:
    try:
        return float(value)
    except ValueError:
        try:
            return float(int(value, 16))
        except ValueError:
            return None
    return None
