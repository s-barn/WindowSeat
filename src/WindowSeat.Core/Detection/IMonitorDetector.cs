namespace WindowSeat.Core.Detection;

/// <summary>
/// Defines a strategy for determining which monitor a new window should be moved to.
/// </summary>
public interface IMonitorDetector
{
    /// <summary>
    /// Returns the bounds of the target monitor in screen coordinates.
    /// Returns null if no suitable monitor can be determined.
    /// </summary>
    System.Drawing.Rectangle? GetTargetMonitorBounds();
}
