#!/usr/bin/env bash

# Boots the Android emulator that is baked into the Helix Android container image and waits
# until it can accept an APK install.
#
# On the historical Ubuntu.2204.Amd64.Android.<api> queues the emulator was started by a systemd
# unit at machine provisioning time and shared by every work item. Container based queues have no
# systemd, so each work item boots its own emulator through this script (wired up as a Helix
# pre-command). A per-work-item emulator also removes the state bleed between work items that the
# shared emulator suffered from.
#
# Emulator failures here are infrastructure failures, not test failures, so they request a Helix
# retry rather than reporting a failed test run.

set -uo pipefail

avd_name="${ANDROID_AVD_NAME:-}"
boot_timeout_seconds="${ANDROID_EMULATOR_BOOT_TIMEOUT:-300}"
boot_attempts="${ANDROID_EMULATOR_BOOT_ATTEMPTS:-2}"
android_sdk_root="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-/usr/local/android-sdk}}"

emulator_binary="$android_sdk_root/emulator/emulator"
adb_binary="$android_sdk_root/platform-tools/adb"

# Helix picks these up to retry the work item on a different machine instead of reporting failures.
report_infrastructure_failure() {
    echo "Android emulator infrastructure failure: $1"

    if [ -n "${HELIX_WORKITEM_ROOT:-}" ]; then
        echo "$1" > "$HELIX_WORKITEM_ROOT/.retry"
        echo "$1" > "$HELIX_WORKITEM_ROOT/.reboot"
    fi
}

die() {
    report_infrastructure_failure "$1"
    exit 1
}

if [ -x "$adb_binary" ]; then
    "$adb_binary" start-server > /dev/null 2>&1
fi

# The legacy Ubuntu.2204.Amd64.Android.<api> queues boot a shared emulator from a systemd unit
# before the Helix client starts, so on those queues there is already a device and nothing to do.
# Detecting that here lets the very same work item payload run on both the legacy queues and the
# container queues, so the two can be migrated independently.
if [ -x "$adb_binary" ] && \
   [ "$("$adb_binary" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r\n')" = "1" ]; then
    echo "A booted Android device is already attached; leaving emulator lifecycle to the host."
    "$adb_binary" devices -l
    exit 0
fi

if [ -z "$avd_name" ]; then
    # The image bakes in exactly one AVD, so fall back to whatever it is called.
    avd_name="$("$emulator_binary" -list-avds 2>/dev/null | head -n 1)"
fi

[ -n "$avd_name" ] || die "No AVD found. Set ANDROID_AVD_NAME or bake an AVD into the image."
[ -x "$emulator_binary" ] || die "Emulator not found at '$emulator_binary'."
[ -x "$adb_binary" ] || die "adb not found at '$adb_binary'."

# The emulator needs hardware acceleration; without KVM it silently falls back to full instruction
# emulation, which is slow enough to blow through every work item timeout.
#
# Providing the device, and access to it, is the infrastructure's job: the image declares that it
# needs /dev/kvm and the Helix container host passes it through together with the gid that owns it.
# This script therefore only verifies that it is usable and never modifies host or container state.
[ -e /dev/kvm ] || die "/dev/kvm is not present. The container must be started with the KVM device passed through."

if [ ! -r /dev/kvm ] || [ ! -w /dev/kvm ]; then
    die "No read/write access to /dev/kvm as '$(id -un)' (groups: $(id -Gn)). The container needs to be started with the gid owning /dev/kvm."
fi

emulator_log="${HELIX_WORKITEM_UPLOAD_ROOT:-/tmp}/emulator-$avd_name.log"
logcat_log="${HELIX_WORKITEM_UPLOAD_ROOT:-/tmp}/emulator-$avd_name-logcat.log"

wait_for_boot() {
    local deadline=$((SECONDS + boot_timeout_seconds))

    "$adb_binary" start-server > /dev/null 2>&1

    while [ $SECONDS -lt $deadline ]; do
        if [ -n "$emulator_pid" ] && ! kill -0 "$emulator_pid" 2>/dev/null; then
            echo "Emulator process exited while booting."
            return 1
        fi

        if [ "$("$adb_binary" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r\n')" = "1" ]; then
            return 0
        fi

        sleep 2
    done

    echo "Emulator did not finish booting within $boot_timeout_seconds seconds."
    return 1
}

emulator_pid=""

for attempt in $(seq 1 "$boot_attempts"); do
    echo "Starting AVD '$avd_name' (attempt $attempt of $boot_attempts)."

    # Each container starts from a pristine image, so the emulator does not need '-wipe-data' the
    # way the shared per-machine emulator did, and skipping it saves boot time.
    "$emulator_binary" -avd "$avd_name" \
        -no-window \
        -no-audio \
        -no-boot-anim \
        -no-snapshot \
        -gpu swiftshader_indirect \
        -memory "${ANDROID_EMULATOR_MEMORY:-3072}" \
        -partition-size "${ANDROID_EMULATOR_PARTITION_SIZE:-1550}" \
        -skip-adb-auth \
        -detect-image-hang \
        -restart-when-stalled \
        -logcat-output "$logcat_log" \
        -prop net.ipv6.conf.all.disable_ipv6=0 \
        -prop net.ipv6.conf.lo.disable_ipv6=0 \
        > "$emulator_log" 2>&1 &
    emulator_pid=$!

    if wait_for_boot; then
        echo "AVD '$avd_name' booted (pid $emulator_pid)."

        if [ -n "${HELIX_WORKITEM_ROOT:-}" ]; then
            echo "$emulator_pid" > "$HELIX_WORKITEM_ROOT/.emulator-pid"
        fi

        # XHarness installs unsigned/debug APKs, which the package verifier rejects.
        "$adb_binary" shell settings put global verifier_verify_adb_installs 0 > /dev/null 2>&1
        "$adb_binary" shell settings put global package_verifier_enable 0 > /dev/null 2>&1

        "$adb_binary" devices -l
        exit 0
    fi

    echo "Emulator failed to boot; tail of $emulator_log:"
    tail -n 30 "$emulator_log" 2>/dev/null

    kill "$emulator_pid" 2>/dev/null
    wait "$emulator_pid" 2>/dev/null
    "$adb_binary" kill-server > /dev/null 2>&1
    emulator_pid=""
done

die "AVD '$avd_name' failed to boot after $boot_attempts attempts."
