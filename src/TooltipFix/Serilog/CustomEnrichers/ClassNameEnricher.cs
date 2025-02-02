#nullable enable
namespace Dawn.Apps.TooltipFix.Serilog.CustomEnrichers;

using System.Diagnostics;
using global::Serilog.Core;
using global::Serilog.Events;

public class ClassNameEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var st = new StackTrace();
        var frame = st.GetFrames().FirstOrDefault(x =>
        {
            var type = x.GetMethod()?.ReflectedType;
            if (type == null || type == typeof(ClassNameEnricher))
                return false;

            return !type.FullName!.StartsWith("Serilog.");
        });
        var type = frame?.GetMethod()?.ReflectedType;
        if (type != null)
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Source", GetClassName(type)));
    }

    private string? GetClassName(Type type)
    {
        var last = type.FullName!.Split('.').LastOrDefault();

        return last?.Split('+').FirstOrDefault()?.Replace("`1", string.Empty);
    }
    
}