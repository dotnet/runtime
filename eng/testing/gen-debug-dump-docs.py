import os
import sys
import platform

build_id = ''
job_id = ''
workitem = ''
dump_dir = ''
template_dir = os.getcwd()
out_dir = template_dir
idx = 0
args_len = len(sys.argv)
while idx < args_len:
    arg = sys.argv[idx]
    idx += 1
    if arg == '-buildid':
        if idx >= args_len or sys.argv[idx].startswith('-'):
            print("Must specify a value for -buildid")
            exit(1)
        
        build_id = sys.argv[idx]
        idx += 1
    
    if arg == '-jobid':
        if idx >= args_len or sys.argv[idx].startswith('-'):
            print("Must specify a value for -jobid")
            exit(1)
        
        job_id = sys.argv[idx]
        idx += 1
    
    if arg == '-workitem':
        if idx >= args_len or sys.argv[idx].startswith('-'):
            print("Must specify a value for -workitem")
            exit(1)
        
        workitem = sys.argv[idx]
        idx += 1

    if arg == '-templatedir':
        if idx >= args_len or sys.argv[idx].startswith('-'):
            print("Must specify a value for -templatedir")
            exit(1)
        
        template_dir = sys.argv[idx]
        idx += 1

    if arg == '-outdir':
        if idx >= args_len or sys.argv[idx].startswith('-'):
            print("Must specify a value for -outdir")
            exit(1)
        
        out_dir = sys.argv[idx]
        idx += 1

    if arg == '-dumpdir':
        if idx >= args_len or sys.argv[idx].startswith('-'):
            print("Must specify a value for -dumpdir")
            exit(1)
        
        dump_dir = sys.argv[idx]
        idx += 1

found_dumps = False
if dump_dir != '':
    for filename in os.listdir(dump_dir):
        if filename.endswith('.dmp') or 'core.' in filename:
            found_dumps = True
            break

if not found_dumps:
    print("Did not find dumps, skipping dump docs generation.")
    exit(0)

if build_id == '':
    print("ERROR: unespecified required argument -buildid")
    exit(1)

if workitem == '':
    print("ERROR: unespecified required argument -workitem")
    exit(1)

if job_id == '':
    print("ERROR: unespecified required argument -jobid")
    exit(1)

replace_string = ''
dir_separator = '/' if platform.system() != 'Windows' else '\\'
unix_user_folder = '~/helix_payload/'
windows_user_folder = 'c:\\helix_payload\\'
source_file = template_dir + dir_separator + 'debug-dump-template.md'
with open(source_file, 'r') as f:
    file_text = f.read()

    print('read file: ' + source_file)

    replace_string = file_text.replace('%JOBID%', job_id)
    replace_string = replace_string.replace('%WORKITEM%', workitem)
    replace_string = replace_string.replace('%BUILDID%', build_id)
    replace_string = replace_string.replace('%LOUTDIR%', unix_user_folder + workitem)
    replace_string = replace_string.replace('%WOUTDIR%', windows_user_folder + workitem)

output_file = out_dir + dir_separator + 'how-to-debug-dump.md'
with open(output_file, 'w+') as output:
    print('writing output file: ' + output_file)
    write_file = output.write(replace_string)

print('done writing debug dump information')
