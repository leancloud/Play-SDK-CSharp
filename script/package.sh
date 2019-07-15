#!/bin/sh
mkdir Plugins
rsync -av --exclude='UnityEngine.dll' ./SDK/SDK/bin/Release/ ./Plugins/
zip -r SDK.zip Plugins
rm -r Plugins