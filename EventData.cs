using System;

namespace RiftVibeSolver;

public readonly struct EventData : IComparable<EventData> {
    public readonly EventType EventType;
    public readonly float Time;
    public readonly float Beat;
    public readonly int TotalScore;
    public readonly int BaseScore;
    public readonly int ComboMultiplier;
    public readonly int VibeMultiplier;
    public readonly int PerfectBonus;

    public EventData(EventType eventType, float time, float beat, int totalScore, int baseScore, int comboMultiplier, int vibeMultiplier, int perfectBonus) {
        EventType = eventType;
        TotalScore = totalScore;
        Time = time;
        Beat = beat;
        BaseScore = baseScore;
        ComboMultiplier = comboMultiplier;
        VibeMultiplier = vibeMultiplier;
        PerfectBonus = perfectBonus;
    }

    public int CompareTo(EventData other) => Time.CompareTo(other.Time);
}