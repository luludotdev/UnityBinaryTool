using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityBinaryTool.Exceptions;

namespace UnityBinaryTool.Unity
{
  static class ArchiveManager
  {
    public static readonly string VersionCachePath = Path.Combine(Program.CacheDir, ".versions");

    private static Task<int> RunProcessAsync(string fileName, string arguments)
    {
      var tcs = new TaskCompletionSource<int>();
      var process = new Process
      {
        StartInfo = {
          FileName = fileName,
          Arguments = arguments,
          UseShellExecute = false,
          CreateNoWindow = true,
        },
        EnableRaisingEvents = true,
      };

      process.Exited += (sender, args) =>
      {
        tcs.SetResult(process.ExitCode);
        process.Dispose();
      };

      process.OutputDataReceived += (sender, args) =>
      {
        Console.WriteLine(args.Data);
      };

      process.Start();
      return tcs.Task;
    }

    public static async Task<string> ExtractEditor(Asset asset)
    {
      string exePath = asset.Path(Program.Options.Prefer32Bit);
      string dirName = asset.VersionName(Program.Options.Prefer32Bit);

      string dirPath = Path.Combine(VersionCachePath, dirName);
      if (Directory.Exists(dirPath)) return dirPath;

      string managedDir = Path.Combine(dirPath, "Managed");
      string monoDir = Path.Combine(dirPath, "Mono");

      int exit;
      exit = await RunProcessAsync(Program.Options.SevenZipPath, $@"e ""{exePath}"" ""-o{managedDir}"" ""Editor/Data/Managed/UnityEngine/*.dll""").ConfigureAwait(false);
      if (exit != 0) throw new ExtractionException();

      exit = await RunProcessAsync(Program.Options.SevenZipPath, $@"e ""{exePath}"" ""-o{monoDir}"" ""Editor/Data/PlaybackEngines/windowsstandalonesupport/Variations/win32_nondevelopment_mono/UnityPlayer.dll""").ConfigureAwait(false);
      if (exit != 0) throw new ExtractionException();
      File.Move(Path.Combine(monoDir, "UnityPlayer.dll"), Path.Combine(monoDir, "MonoUnityPlayer_x86.dll"));

      exit = await RunProcessAsync(Program.Options.SevenZipPath, $@"e ""{exePath}"" ""-o{monoDir}"" ""Editor/Data/PlaybackEngines/windowsstandalonesupport/Variations/win64_nondevelopment_mono/UnityPlayer.dll""").ConfigureAwait(false);
      if (exit != 0) throw new ExtractionException();
      File.Move(Path.Combine(monoDir, "UnityPlayer.dll"), Path.Combine(monoDir, "MonoUnityPlayer_x64.dll"));

      return dirPath;
    }

    public static async Task<string> CompressDLLs(Asset asset, string dirPath)
    {
      string zipName = asset.VersionName(Program.Options.Prefer32Bit);
      string path = Path.Combine(Program.Options.OutputDirectory, $"{zipName}.zip");

      var exit = await RunProcessAsync(Program.Options.SevenZipPath, $@"a ""{path}"" ""{dirPath}""").ConfigureAwait(false);
      if (exit != 0) throw new CompressionException();

      return path;
    }
  }
}
