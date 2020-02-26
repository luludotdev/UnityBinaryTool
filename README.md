# 🗑️ Unity Binary Tool

## 🚀 Usage
This is a CLI tool, so you will need to use the Terminal to run it. Simply pass a Unity version to the tool as an argument and it will do the rest.
```sh
# Example
$ UnityBinaryTool.exe 2018.4.10f1
```

You can also use the `-l` flag to list all available versions. You will also be prompted with this list if you enter an invalid / unsupported Unity version.

To view other flags, use the `--help` flag.

### ❗ 7-zip Requirement
This tool relies on the 7-zip CLI (`7z.exe`) being in the system PATH. If you do not have the 7-zip CLI in your path, you can specify the location using the `-z` flag.

You must have a full install of 7-zip, the standalone CLI (`7za.exe`) is not sufficient. You should be able to use this tool on POSIX systems using p7zip, although this is untested and you will need to compile the tool for your distro yourself.

You can download 7-zip from [their website](https://www.7-zip.org/).

## 💾 Installation
This tool is packaged as a single exe using .NET Warp. This means it contains all the .NET Core runtime required to execute inside of the executable. On first run, it will extract the required runtime to a temporary directory and continue from there.

You can grab a release binary from the [Releases](https://github.com/lolPants/UnityBinaryTool/releases) page, or an indev binary from the [CI Workflow artifacts](https://github.com/lolPants/UnityBinaryTool/actions).

## ⚠️ Known Limitations
* This tool only packages core Unity DLLs, meaning any DLLs used in games that come from external sources (ie: Unity Package Manager) will not be included.
