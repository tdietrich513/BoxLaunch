BoxLaunch
=========

Handles synchronizing the contents of two folders and running an executable. Used for in-house deployments of applications.

BoxLaunch uses multiple threads for copying files from source to target.
The number of threads to be used will be automatically determined based on your processor.

By default, BoxLaunch will compare files between the two directories based on Last Write Date as reported by windows.
However, if the source directory contains a .blhash file (generated by running the hash command), the .blhash file will 
be examined and used to determine which files need to be updated. 

You can add a .blignore file to the source directory to tell boxlaunch to ignore files or folders. This functions similarly
to the .gitignore file in a Git repository. 


Box Launch Commands: 
  - sync-and-run
  - sync
  - hash
  - copy-and-run
  
sync-and-run 
============
usage: BoxLaunch sync-and-run -s={SOURCE DIRECTORY} -t={TARGET DIRECTORY} -p={PROGRAM}

Downloads updates to a directory then launches an executable.

Available Options:

  -s, --source=SOURCE DIRECTORY
                             The SOURCE DIRECTORY that contains the files to
                               be copied.
                               
  -t, --target=TARGET DIRECTORY
                             The TARGET DIRECTORY that the files should be
                               copied to.
                               
  -p, --program=PROGRAM      The PROGRAM to run once the directories are in
                               sync.
                               
  -a, --arg=ARGUMENT         An ARGUMENT that should be passed to the
                               executable.                              
  
  
sync
====
usage: BoxLaunch sync -s={SOURCE DIRECTORY} -t={TARGET DIRECTORY}

Downloads updates to a directory.

Available Options:

  -s, --source=SOURCE DIRECTORY
                             The SOURCE DIRECTORY that contains the files to
                               be copied.
                               
  -t, --target=TARGET DIRECTORY
                             The TARGET DIRECTORY that the files should be
                               copied to.
                               
                               
hash
====
usage: BoxLaunch hash -d={DIRECTORY}

Creates a hash cache for a directory.

Available Options:

  -d, --directory=DIRECTORY  The DIRECTORY that needs to be hashed.
  
  -f, --file=FILE            A FILE to hash.
  
copy-and-run
============
usage: BoxLaunch copy-and-run -t={TARGET DIRECTORY} -p={PROGRAM}

Downloads a single file program to a target directory then runs it.

Available Options:

  -p, --program=PROGRAM      The PROGRAM to run once the directories are in
                               sync.
                               
  -t, --target=TARGET DIRECTORY
                             The TARGET DIRECTORY that the files should be
                               copied to.
                               
