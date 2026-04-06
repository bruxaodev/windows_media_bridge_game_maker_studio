using System;
using System.Runtime.InteropServices;
using Windows.Media.Control;
using Windows.Storage.Streams;

public static class Media
{
    [UnmanagedCallersOnly(EntryPoint = "media_get_current_json")]
    public static IntPtr GetCurrent()
    {
        try
        {
            var manager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().Result;
            var session = manager.GetCurrentSession();

            if (session == null)
                return StringToPtr("{\"ok\":false,\"error\":\"no_session\"}");

            var media = session.TryGetMediaPropertiesAsync().AsTask().Result;

            var playback = session.GetPlaybackInfo();

            string thumb = Escape(ReadThumbnailDataUrl(media.Thumbnail));

            string title = Escape(media.Title ?? "");
            string artist = Escape(media.Artist ?? "");
            string album = Escape(media.AlbumTitle ?? "");
            string source = Escape(session.SourceAppUserModelId ?? "");
            string status = Escape(playback.PlaybackStatus.ToString());

            string json =
                "{"
                + "\"ok\":true,"
                + "\"title\":\"" + title + "\","
                + "\"artist\":\"" + artist + "\","
                + "\"album\":\"" + album + "\","
                + "\"source\":\"" + source + "\","
                + "\"status\":\"" + status + "\","
                + "\"thumbnail\":\"" + thumb + "\""
                + "}";

            return StringToPtr(json);
        }
        catch (Exception e)
        {
            string err = Escape(e.Message);
            return StringToPtr("{\"ok\":false,\"error\":\"" + err + "\"}");
        }
    }


    [UnmanagedCallersOnly(EntryPoint = "media_pause")]
    public static int Pause()
    {
        try
        {
            var manager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().Result;
            var session = manager.GetCurrentSession();
            if (session == null) return 0;

            var playback = session.GetPlaybackInfo();

            bool success = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing
                ? session.TryPauseAsync().AsTask().Result
                : session.TryPlayAsync().AsTask().Result;

            return success ? 1 : 0;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "media_next")]
    public static int Next()
    {
        try
        {
            var manager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().Result;
            var session = manager.GetCurrentSession();
            if (session == null) return 0;

            return session.TrySkipNextAsync().AsTask().Result ? 1 : 0;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "media_prev")]
    public static int Prev()
    {
        try
        {
            var manager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().Result;
            var session = manager.GetCurrentSession();
            if (session == null) return 0;

            return session.TrySkipPreviousAsync().AsTask().Result ? 1 : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string Escape(string s)
    {
        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    private static string ReadThumbnailDataUrl(IRandomAccessStreamReference? thumbnail)
    {
        if (thumbnail == null)
            return "";

        using var stream = thumbnail.OpenReadAsync().AsTask().Result;
        if (stream == null || stream.Size == 0)
            return "";

        using var reader = new DataReader(stream);
        reader.LoadAsync((uint)stream.Size).AsTask().Wait();

        var bytes = new byte[(int)stream.Size];
        reader.ReadBytes(bytes);

        string contentType = string.IsNullOrWhiteSpace(stream.ContentType)
            ? "application/octet-stream"
            : stream.ContentType;

        return "data:" + contentType + ";base64," + Convert.ToBase64String(bytes);
    }

    private static IntPtr StringToPtr(string str)
    {
        return Marshal.StringToHGlobalAnsi(str);
    }
}