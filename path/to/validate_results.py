# Validate the results of the fix
import re

def validate_results(helix_logs: str) -> bool:
    try:
        # Check for Mutex release failures in the Helix logs
        if re.search(r"ReleaseMutex failed", helix_logs):
            return False
        else:
            return True
    except Exception as e:
        print(f"Error validating results: {e}")
        return False

helix_logs = fetch_helix_logs("1161146")
is_valid = validate_results(helix_logs)
print(is_valid)