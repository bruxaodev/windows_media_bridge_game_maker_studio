using System;
using System.Runtime.InteropServices;
using Windows.Media;
using Windows.Media.Control;
using Windows.Storage.Streams;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class Media
{
    [UnmanagedCallersOnly(EntryPoint = "media_get_timeline_json")]
    public static IntPtr GetTimeline()
    {
        try
        {
            var session = GetCurrentSession();
            if (session == null)
                return StringToPtr("{\"ok\":false,\"error\":\"no_session\"}");

            var timeline = session.GetTimelineProperties();
            var result = new MediaTimelineResponse
            {
                Ok = true,
                Timeline = new MediaTimelineResult
                {
                    StartTimeMs = timeline.StartTime.TotalMilliseconds,
                    EndTimeMs = timeline.EndTime.TotalMilliseconds,
                    MinSeekTimeMs = timeline.MinSeekTime.TotalMilliseconds,
                    MaxSeekTimeMs = timeline.MaxSeekTime.TotalMilliseconds,
                    PositionMs = timeline.Position.TotalMilliseconds,
                    LastUpdatedTime = timeline.LastUpdatedTime.ToString("O")
                }
            };

            string json = JsonSerializer.Serialize(result, MediaJsonContext.Default.MediaTimelineResponse);
            return StringToPtr(json);
        }
        catch (Exception e)
        {
            string err = Escape(e.Message);
            return StringToPtr("{\"ok\":false,\"error\":\"" + err + "\"}");
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "media_get_current_json")]
    public static IntPtr GetCurrent()
    {
        try
        {
            var session = GetCurrentSession();

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
        return TryControl(session => session.TryPauseAsync().AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_play")]
    public static int Play()
    {
        return TryControl(session => session.TryPlayAsync().AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_toggle_play_pause")]
    public static int TogglePlayPause()
    {
        return TogglePlayPauseCore();
    }

    [UnmanagedCallersOnly(EntryPoint = "media_play_pause")]
    public static int PlayPause()
    {
        return TogglePlayPauseCore();
    }

    [UnmanagedCallersOnly(EntryPoint = "media_next")]
    public static int Next()
    {
        return TryControl(session => session.TrySkipNextAsync().AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_prev")]
    public static int Prev()
    {
        return TryControl(session => session.TrySkipPreviousAsync().AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_stop")]
    public static int Stop()
    {
        return TryControl(session => session.TryStopAsync().AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_fast_forward")]
    public static int FastForward()
    {
        return TryControl(session => session.TryFastForwardAsync().AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_rewind")]
    public static int Rewind()
    {
        return TryControl(session => session.TryRewindAsync().AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_record")]
    public static int Record()
    {
        return TryControl(session => session.TryRecordAsync().AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_channel_up")]
    public static int ChannelUp()
    {
        return TryControl(session => session.TryChangeChannelUpAsync().AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_channel_down")]
    public static int ChannelDown()
    {
        return TryControl(session => session.TryChangeChannelDownAsync().AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_change_shuffle_active")]
    public static int ChangeShuffleActive(double active)
    {
        return TryControl(session => session.TryChangeShuffleActiveAsync(active != 0).AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_change_playback_rate")]
    public static int ChangePlaybackRate(double rate)
    {
        return TryControl(session => session.TryChangePlaybackRateAsync(rate).AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_change_playback_position")]
    public static int ChangePlaybackPosition(double positionMs)
    {
        long ticks = TimeSpan.FromMilliseconds(positionMs).Ticks;
        return TryControl(session => session.TryChangePlaybackPositionAsync(ticks).AsTask().Result);
    }

    [UnmanagedCallersOnly(EntryPoint = "media_change_auto_repeat_mode")]
    public static int ChangeAutoRepeatMode(double mode)
    {
        int modeValue = (int)mode;
        if (!Enum.IsDefined(typeof(MediaPlaybackAutoRepeatMode), modeValue))
            return 0;

        var repeatMode = (MediaPlaybackAutoRepeatMode)modeValue;
        return TryControl(session => session.TryChangeAutoRepeatModeAsync(repeatMode).AsTask().Result);
    }

    private static GlobalSystemMediaTransportControlsSession? GetCurrentSession()
    {
        var manager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().Result;
        return manager.GetCurrentSession();
    }

    private static int TogglePlayPauseCore()
    {
        return TryControl(session => session.TryTogglePlayPauseAsync().AsTask().Result);
    }

    private static int TryControl(Func<GlobalSystemMediaTransportControlsSession, bool> action)
    {
        try
        {
            var session = GetCurrentSession();
            if (session == null)
                return 0;

            return action(session) ? 1 : 0;
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

internal sealed class MediaTimelineResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("timeline")]
    public MediaTimelineResult Timeline { get; set; } = new();
}

internal sealed class MediaTimelineResult
{
    [JsonPropertyName("startTimeMs")]
    public double StartTimeMs { get; set; }

    [JsonPropertyName("endTimeMs")]
    public double EndTimeMs { get; set; }

    [JsonPropertyName("minSeekTimeMs")]
    public double MinSeekTimeMs { get; set; }

    [JsonPropertyName("maxSeekTimeMs")]
    public double MaxSeekTimeMs { get; set; }

    [JsonPropertyName("positionMs")]
    public double PositionMs { get; set; }

    [JsonPropertyName("lastUpdatedTime")]
    public string LastUpdatedTime { get; set; } = "";
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
[JsonSerializable(typeof(MediaTimelineResponse))]
internal partial class MediaJsonContext : JsonSerializerContext
{
}