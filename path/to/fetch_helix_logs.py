# Fetch Helix logs to verify the fix
import requests

def fetch_helix_logs(build_id: str) -> str:
    try:
        response = requests.get(f"https://dev.azure.com/dnceng/public/_apis/build/builds/{build_id}/logs")
        return response.text
    except requests.exceptions.RequestException as e:
        return f"Error fetching Helix logs: {e}"

build_id = "1161146"
helix_logs = fetch_helix_logs(build_id)
print(helix_logs)