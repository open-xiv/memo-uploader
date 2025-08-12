using System;


namespace MemoUploader.Models;

/// <summary>
///     Base interface for all events.
/// </summary>
public interface IEvent
{
    string Category => GetType().Name;
    string Message  => ToString() ?? string.Empty;
}

public record EventLog(DateTime Time, string Category, string Message);
