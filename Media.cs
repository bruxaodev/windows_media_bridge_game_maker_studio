using System;
using System.Runtime.InteropServices;
using Windows.Media.Control;
using Windows.Storage.Streams;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            string subTitle = Escape(media.Subtitle ?? "");
            string artist = Escape(media.Artist ?? "");
            string album = Escape(media.AlbumTitle ?? "");
            string source = Escape(session.SourceAppUserModelId ?? "");
            int albumCount = media.AlbumTrackCount;
            int trackNumber = media.TrackNumber;

            var mediaResult = new MediaDataResult
            {
                Title = title,
                SubTitle = subTitle,
                Artist = artist,
                Album = album,
                Source = source,
                Thumbnail = thumb,
                AlbumCount = albumCount,
                TrackNumber = trackNumber
            };

            var controls = new MediaControlResult
            {
                IsChannelDownEnabled = playback.Controls.IsChannelDownEnabled,
                IsChannelUpEnabled = playback.Controls.IsChannelUpEnabled,
                IsFastForwardEnabled = playback.Controls.IsFastForwardEnabled,
                IsNextEnabled = playback.Controls.IsNextEnabled,
                IsPauseEnabled = playback.Controls.IsPauseEnabled,
                IsPlayEnabled = playback.Controls.IsPlayEnabled,
                IsPlaybackPositionEnabled = playback.Controls.IsPlaybackPositionEnabled,
                IsPlaybackRateEnabled = playback.Controls.IsPlaybackRateEnabled,
                IsPreviousEnabled = playback.Controls.IsPreviousEnabled,
                IsRecordEnabled = playback.Controls.IsRecordEnabled,
                IsRepeatEnabled = playback.Controls.IsRepeatEnabled,
                IsRewindEnabled = playback.Controls.IsRewindEnabled,
                IsShuffleEnabled = playback.Controls.IsShuffleEnabled,
                IsStopEnabled = playback.Controls.IsStopEnabled
            };

            var playbackResult = new MediaPlaybackResult
            {
                Status = Escape(playback.PlaybackStatus.ToString()),
                // Type = Escape(playback.PlaybackType.GetValueOrDefault().ToString()),
                // AutoRepeatMode = Escape(playback.AutoRepeatMode.GetValueOrDefault().ToString()),
                Controls = controls
            };

            var result = new MediaCurrentResult
            {
                Ok = true,
                Media = mediaResult,
                Playback = playbackResult
            };

            string json = JsonSerializer.Serialize(result, MediaJsonContext.Default.MediaCurrentResult);

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

internal sealed class MediaCurrentResult
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("media")]
    public MediaDataResult Media { get; set; } = new();

    [JsonPropertyName("playback")]
    public MediaPlaybackResult Playback { get; set; } = new();
}

internal sealed class MediaDataResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("subTitle")]
    public string SubTitle { get; set; } = "";

    [JsonPropertyName("artist")]
    public string Artist { get; set; } = "";

    [JsonPropertyName("album")]
    public string Album { get; set; } = "";

    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; } = "";

    [JsonPropertyName("albumCount")]
    public int AlbumCount { get; set; }

    [JsonPropertyName("trackNumber")]
    public int TrackNumber { get; set; }
}

internal sealed class MediaPlaybackResult
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    // [JsonPropertyName("type")]
    // public string Type { get; set; } = "";

    // [JsonPropertyName("autoRepeatMode")]
    // public string AutoRepeatMode { get; set; } = "";

    [JsonPropertyName("controls")]
    public MediaControlResult Controls { get; set; } = new();
}

internal sealed class MediaControlResult
{
    [JsonPropertyName("isChannelDownEnabled")]
    public bool IsChannelDownEnabled { get; set; }

    [JsonPropertyName("isChannelUpEnabled")]
    public bool IsChannelUpEnabled { get; set; }

    [JsonPropertyName("isFastForwardEnabled")]
    public bool IsFastForwardEnabled { get; set; }

    [JsonPropertyName("isNextEnabled")]
    public bool IsNextEnabled { get; set; }

    [JsonPropertyName("isPauseEnabled")]
    public bool IsPauseEnabled { get; set; }

    [JsonPropertyName("isPlayEnabled")]
    public bool IsPlayEnabled { get; set; }

    [JsonPropertyName("isPlaybackPositionEnabled")]
    public bool IsPlaybackPositionEnabled { get; set; }

    [JsonPropertyName("isPlaybackRateEnabled")]
    public bool IsPlaybackRateEnabled { get; set; }

    [JsonPropertyName("isPreviousEnabled")]
    public bool IsPreviousEnabled { get; set; }

    [JsonPropertyName("isRecordEnabled")]
    public bool IsRecordEnabled { get; set; }

    [JsonPropertyName("isRepeatEnabled")]
    public bool IsRepeatEnabled { get; set; }

    [JsonPropertyName("isRewindEnabled")]
    public bool IsRewindEnabled { get; set; }

    [JsonPropertyName("isShuffleEnabled")]
    public bool IsShuffleEnabled { get; set; }

    [JsonPropertyName("isStopEnabled")]
    public bool IsStopEnabled { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MediaCurrentResult))]
internal partial class MediaJsonContext : JsonSerializerContext
{
}