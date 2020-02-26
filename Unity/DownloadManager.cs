using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityBinaryTool.Exceptions;
using static UnityBinaryTool.Http;

namespace UnityBinaryTool.Unity
{
  static class DownloadManager
  {
    public static string InstallerCachePath = Path.Combine(Program.CacheDir, ".installers");
    public static string InstallerStagingPath = Path.Combine(InstallerCachePath, ".staging");

    private static readonly Regex _urlRX = new Regex("<li><a href=\"(https:\\/\\/.+\\.exe)\">.+<\\/li>", RegexOptions.Compiled);
    private static readonly Regex _fileRX = new Regex(@"UnitySetup(?:64|32)-(.+)\.exe", RegexOptions.Compiled);

    private static Dictionary<string, Asset> _assets = new Dictionary<string, Asset>();
    public static ReadOnlyDictionary<string, Asset> Assets
    {
      get => new ReadOnlyDictionary<string, Asset>(_assets);
    }

    private static async Task FetchData()
    {
      var resp = await HttpClient.GetAsync("https://unity3d.com/get-unity/download/archive").ConfigureAwait(false);
      var html = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

      lock (_assets)
      {
        var matches = _urlRX.Matches(html);

        foreach (Match match in matches)
        {
          string url = match.Groups[1].Value;
          if (url.Contains("UnitySetup") == false) continue;

          Match fileMatch = _fileRX.Match(url);
          if (fileMatch.Success == false) continue;

          string version = fileMatch.Groups[1].Value;
          bool is32Bit = url.Contains("UnitySetup32");

          Asset asset;
          if (_assets.ContainsKey(version))
          {
            asset = _assets[version];
          }
          else
          {
            asset = new Asset();
            asset.Version = version;
          }

          if (is32Bit)
          {
            // 32 bit URL
            if (asset.Win32Url != url) asset.Win32Url = url;

            string path = Path.Combine(InstallerCachePath, Path.GetFileName(url));
            if (File.Exists(path)) asset.Win32Path = path;

            string dir = Path.Combine(ArchiveManager.VersionCachePath, asset.VersionName(true));
            if (Directory.Exists(dir)) asset.Extracted = true;
          }
          else
          {
            // 64 bit URL
            if (asset.Win64Url != url) asset.Win64Url = url;

            string path = Path.Combine(InstallerCachePath, Path.GetFileName(url));
            if (File.Exists(path)) asset.Win64Path = path;

            string dir = Path.Combine(ArchiveManager.VersionCachePath, asset.VersionName(false));
            if (Directory.Exists(dir)) asset.Extracted = true;
          }

          _assets[version] = asset;
        }
      }
    }

    private static async Task<Asset?> GetDownloadAssets(string version, bool retried = false)
    {
      if (Assets.ContainsKey(version) == false)
      {
        if (retried == true) return null;

        await FetchData().ConfigureAwait(false);
        return await GetDownloadAssets(version, true).ConfigureAwait(false);
      }

      return Assets[version];
    }

    private static async Task DownloadEditor(string version, IProgress<double> progress)
    {
      Asset? assets = await GetDownloadAssets(version).ConfigureAwait(false);

      string url = assets?.Url(Program.Options.Prefer32Bit);
      if (url is null) throw new EditorVersionNotFoundException();

      var resp = await HttpClient.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
      resp.EnsureSuccessStatusCode();

      Directory.CreateDirectory(InstallerCachePath);
      Directory.CreateDirectory(InstallerStagingPath);

      string stagingPath = Path.Combine(InstallerStagingPath, Path.GetFileName(url));
      if (File.Exists(stagingPath)) File.Delete(stagingPath);

      using (var content = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
      using (var fs = File.OpenWrite(stagingPath))
      {
        byte[] buffer = new byte[1 << 13];
        int bytesRead;

        long? contentLength = resp.Content.Headers.ContentLength;
        long totalRead = 0;
        progress?.Report(0);

        while ((bytesRead = await content.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
        {
          if (contentLength != null)
          {
            double prog = (double)totalRead / (double)contentLength;
            progress?.Report(prog);
          }

          await fs.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
          totalRead += bytesRead;
        }
      }

      string finalPath = Path.Combine(InstallerCachePath, Path.GetFileName(url));
      File.Move(stagingPath, finalPath);
      progress?.Report(1);

      var entry = _assets[version];
      if (Program.Options.Prefer32Bit) entry.Win32Path = finalPath;
      else entry.Win64Path = finalPath;
      _assets[version] = entry;
    }

    public static async Task<Asset> GetEditorAsset(string version, IProgress<double> progress = null)
    {
      Asset? asset = await GetDownloadAssets(version).ConfigureAwait(false);
      string path = asset?.Path(Program.Options.Prefer32Bit);
      await DownloadEditor(version, progress).ConfigureAwait(false);

      progress?.Report(1);
      return _assets[version];
    }
  }
}
