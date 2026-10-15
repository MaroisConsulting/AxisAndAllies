using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public static class DesktopDownloads
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(ref Guid id, uint flags, IntPtr token, out IntPtr path);
#endif

    public static string GetPath()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        Guid id = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        IntPtr pointer;
        int result = SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out pointer);
        if (result != 0) Marshal.ThrowExceptionForHR(result);
        try { return Marshal.PtrToStringUni(pointer); }
        finally { Marshal.FreeCoTaskMem(pointer); }
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home)) home = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home)) throw new IOException("Cannot locate your home folder.");
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        string config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        string file = Path.Combine(string.IsNullOrEmpty(config) ? Path.Combine(home, ".config") : config, "user-dirs.dirs");
        if (File.Exists(file))
            foreach (string line in File.ReadAllLines(file))
            {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith("XDG_DOWNLOAD_DIR=")) continue;
                string value = trimmed.Substring("XDG_DOWNLOAD_DIR=".Length).Trim().Trim('"')
                    .Replace("${HOME}", home).Replace("$HOME", home);
                if (Path.IsPathRooted(value)) return value;
            }
#endif
        return Path.Combine(home, "Downloads");
#else
        throw new PlatformNotSupportedException("Downloads export supports Windows, macOS and Linux desktop builds.");
#endif
    }
}
