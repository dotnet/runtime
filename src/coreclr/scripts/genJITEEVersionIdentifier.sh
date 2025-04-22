#!/bin/sh

# Generate a UUIDv4 using the updated command
uuid=$(printf '%s\n' "$(od -An -N16 -tx1 /dev/urandom | tr -d ' \n' | sed 's/\([a-f0-9]\{8\}\)\([a-f0-9]\{4\}\)\([a-f0-9]\{4\}\)\([a-f0-9]\{4\}\)\([a-f0-9]\{12\}\)/\1-\2-4\3-\4-\5/')")

# Extract parts of the UUID
data1=$(echo "$uuid" | cut -d'-' -f1)
data2=$(echo "$uuid" | cut -d'-' -f2)
data3=$(echo "$uuid" | cut -d'-' -f3)
data4=$(echo "$uuid" | cut -d'-' -f4)
data5=$(echo "$uuid" | cut -d'-' -f5)

# Convert parts to desired format
data1=$(printf "0x%s" "$data1")
data2=$(printf "0x%s" "$data2")
data3=$(printf "0x%s" "$data3")
data4=$(echo "$data4" | sed 's/\(..\)/0x\1, /g' | sed 's/, $//')
data5=$(echo "$data5" | sed 's/\(..\)/0x\1, /g' | sed 's/, $//')

# Print the result in the desired format
echo "constexpr GUID JITEEVersionIdentifier = { /* $uuid */
    $data1,
    $data2,
    $data3,
    {$data4, $data5}
  };"
