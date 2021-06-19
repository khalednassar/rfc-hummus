#!/bin/sh

JSON_OUTPUT_FILENAME="gen-index.json"
/built/index-builder "$JSON_OUTPUT_FILENAME" $@

echo "::set-output name=index-file::$JSON_OUTPUT_FILENAME"