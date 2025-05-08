#!/bin/sh
if [ ! -f /var/cache/first ]; then
  echo "First run, resetting font cache..."
  fc-cache -f
fi
touch /var/cache/first
exec mono --debug /app/bin/Calctus.exe "$@"
