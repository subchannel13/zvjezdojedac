rem Copies data from {svnRoot}/source/Zvjezdojedac/bin/Debug/
rem to {svnRoot}/build

echo off
robocopy ../source/Zvjezdojedac/bin/Debug/podaci/ ../build/podaci/ /xd .svn /e
robocopy ../source/Zvjezdojedac/bin/Debug/slike/ ../build/slike/ /xd .svn /e
robocopy ../source/Zvjezdojedac/bin/Debug/jezici/ ../build/jezici/ /xd .svn /e
pause