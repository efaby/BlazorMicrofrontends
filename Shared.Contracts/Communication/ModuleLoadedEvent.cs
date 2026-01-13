using System;

namespace Shared.Contracts.Communication;

public class ModuleLoadedEvent
{
    public string ModuleName { get; }
    public DateTime LoadedAt { get; }

    public ModuleLoadedEvent(string moduleName, DateTime loadedAt)
    {
        ModuleName = moduleName;
        LoadedAt = loadedAt;
    }
}


public class ModuleErrorEvent
{
    public string ModuleName { get; }
    public string Error { get; }
    public DateTime OccurredAt { get; }

    public ModuleErrorEvent(string moduleName, string error, DateTime occurredAt)
    {
        ModuleName = moduleName;
        Error = error;
        OccurredAt = occurredAt;
    }
}


public class ModuleConnectedEvent
{
    public string ModuleName { get; }
    public DateTime ConnectedAt { get; }

    public ModuleConnectedEvent(string moduleName)
    {
        ModuleName = moduleName;
        ConnectedAt = DateTime.UtcNow;
    }
}


public class CrossModuleDataEvent
{
    public string SourceModule { get; }
    public string TargetModule { get; }
    public string EventType { get; }
    public object? Data { get; }
    public DateTime Timestamp { get; }

    public CrossModuleDataEvent(
        string sourceModule,
        string targetModule,
        string eventType,
        object? data = null)
    {
        SourceModule = sourceModule;
        TargetModule = targetModule;
        EventType = eventType;
        Data = data;
        Timestamp = DateTime.UtcNow;
    }
}
