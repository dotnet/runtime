project (mono)

#set (subdirs eglib arch utils cil sgen metadata mini dis profiler)
set (subdirs mini)

foreach (dir ${subdirs})
  add_subdirectory (${dir})
endforeach ()
