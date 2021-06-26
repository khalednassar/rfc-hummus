#!/bin/sh

if [ $# -lt 2 ]; then
    echo "Missing arguments" 1>&2
    exit 1
fi

JSON_OUTPUT_FILENAME="$1"
DOWNLOAD_LOC="$2"
/built/index-builder "$JSON_OUTPUT_FILENAME" "$DOWNLOAD_LOC"

echo "::set-output name=index-file-path::$JSON_OUTPUT_FILENAME"