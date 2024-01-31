handle SIGXCPU SIG33 SIG35 SIG36 SIG37 SIG38 SIGPWR nostop print

define mono_backtrace
 select-frame 0
 set $i = 0
 while ($i < $arg0)
   set $foo = (char*) mono_pmip ($pc)
   if ($foo)
     printf "#%d %p in %s\n", $i, $pc, $foo
   else
     frame
   end
   up-silently
   set $i = $i + 1
 end
end

define mono_stack
 set $mono_thread = mono_thread_current ()
 if ($mono_thread == 0x00)
   printf "No mono thread associated with this thread\n"
 else
   set $ucp = (MonoContext*)malloc (sizeof (MonoContext))
   set $ucp->gregs[0] = $pc
   set $ucp->gregs[2] = $sp
   set $ucp->gregs[8] = $sp
   call (void) mono_print_thread_dump ($ucp)
   call (void) free ($ucp)
 end
end
