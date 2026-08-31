# Check the code is in sync
$changed = (select-string "nothing to commit" artifacts\status.txt).count -eq 0
return $changed