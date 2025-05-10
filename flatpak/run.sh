#!/bin/sh
if [ ! -f /var/cache/first ]; then
  echo "First run, resetting font cache..."
  fc-cache -f
  touch /var/cache/first
fi
exec mono --debug /app/bin/Calctus.exe "$@"
