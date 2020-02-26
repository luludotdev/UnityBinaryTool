using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using UnityBinaryTool.Exceptions;
using UnityBinaryTool.Unity;

namespace UnityBinaryTool
{
  class Options
  {
    [Option('p', "prefer-32bit", Required = false, HelpText = "Prefer 32-bit Unity DLLs")]
    public bool Prefer32Bit { get; set; }

    [Option('l', "list-available", Required = false, HelpText = "List available Unity versions")]
    public bool ListAvailable { get; set; }

    [Option('o', "output", Required = false, HelpText = "Zip output directory (defaults to $PWD)")]
    public string OutputDirectory { get; set; }

    [Option('z', "7zip-path", Required = false, HelpText = "7-Zip CLI path override")]
    public string SevenZipPath { get; set; }

    [Option('c', "no-cache", Required = false, HelpText = "Do not persist cache directory after run")]
    public bool NoCache { get; set; }

    [Value(0, MetaName = "version", HelpText = "Unity Editor Version")]
    public string Version { get; set; }
  }

  class Program
  {
    public static Options Options = null;

    public static readonly string CacheDir = Path.Combine(Directory.GetCurrentDirectory(), ".cache");

    static void Main(string[] args)
    {
      Parser.Default.ParseArguments<Options>(args)
        .WithParsed(o =>
        {
          Options = o;
          Options.OutputDirectory ??= Directory.GetCurrentDirectory();
          Options.SevenZipPath ??= "7z";
          if (Options.ListAvailable == true) Options.Version = "-";

          RunAsync().GetAwaiter().GetResult();
        });
    }

    static async Task RunAsync()
    {
      string version = Options.Version;
      bool prefer32bit = Options.Prefer32Bit;

      if (string.IsNullOrEmpty(version))
      {
        Console.WriteLine("Please specify a Unity version");
        Environment.Exit(1);

        return;
      }

      try
      {
        var asset = await DownloadManager.GetEditorAsset(version).ConfigureAwait(false);
        var dirPath = await ArchiveManager.ExtractEditor(asset).ConfigureAwait(false);
        var zipPath = await ArchiveManager.CompressDLLs(asset, dirPath).ConfigureAwait(false);
      }
      catch (EditorVersionNotFoundException)
      {
        if (version != "-")
        {
          Console.WriteLine($"Could not find assets for Unity Version {version}\n");
          Console.WriteLine("Available Versions:");
        }

        foreach (var asset in DownloadManager.Assets.Values)
        {
          Console.Write(asset.Version);
          if (asset.Win32Url != null) Console.Write("\t (64/32)");

          Console.Write("\n");
        }

        Environment.Exit(1);
      }
      catch (System.ComponentModel.Win32Exception)
      {
        if (Options.SevenZipPath == null)
        {
          Console.WriteLine("Could not find 7-Zip CLI in your PATH");
          Console.WriteLine("You can use the -z flag to override the path search");
        }
        else
        {
          Console.WriteLine("Could not find the 7-Zip CLI");
        }

        Environment.Exit(1);
      }
      finally
      {
        Cleanup();
      }
    }

    static void Cleanup()
    {
      try
      {
        Directory.Delete(DownloadManager.InstallerStagingPath, true);
      }
      catch (DirectoryNotFoundException) { }

      if (Options.NoCache)
      {
        try
        {
          Directory.Delete(CacheDir, true);
        }
        catch (DirectoryNotFoundException) { }
      }
    }
  }
}
