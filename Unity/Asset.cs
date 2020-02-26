namespace UnityBinaryTool.Unity
{
  public struct Asset
  {
    public string Version { get; internal set; }

    public string Win64Url { get; internal set; }
    public string Win64Path { get; internal set; }

    public string Win32Url { get; internal set; }
    public string Win32Path { get; internal set; }

    public bool Extracted { get; internal set; }

    public string Url(bool prefer32bit = false) => prefer32bit ? Win32Url : Win64Url;
    public string Path(bool prefer32bit = false) => prefer32bit ? Win32Path : Win64Path;
    public string VersionName(bool prefer32bit = false) => prefer32bit ? $"{Version}-32bit" : Version;
  }
}
