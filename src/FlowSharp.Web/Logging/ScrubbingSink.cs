using Serilog.Core;
using Serilog.Events;

namespace FlowSharp.Web.Logging;

/// <summary>
/// Sarmaladigi sink'lere iletmeden once her log olayinin property degerlerini merkezi olarak temizler.
/// Serilog render'i property degerlerinden uretildigi icin, degerleri burada temizlemek hem mesaj
/// metnini hem de yapisal ciktiyi kapsar:
/// <list type="bullet">
/// <item>CR/LF ve diger kontrol karakterleri temizlenir → log forging (CWE-117) onlenir.</item>
/// <item>E-posta adresleri maskelenir (<c>jo***@example.com</c>) → PII sizintisi (CWE-359) azaltilir.</item>
/// </list>
/// Tek noktada tanimlanir; ayri log satirlarinda manuel temizleme gerektirmez.
/// </summary>
internal sealed class ScrubbingSink(ILogEventSink inner) : ILogEventSink, IDisposable
{
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        var scrubbed = new LogEvent(
            logEvent.Timestamp,
            logEvent.Level,
            logEvent.Exception,
            logEvent.MessageTemplate,
            logEvent.Properties.Select(p => new LogEventProperty(p.Key, Scrub(p.Value))));

        inner.Emit(scrubbed);
    }

    private static LogEventPropertyValue Scrub(LogEventPropertyValue value) => value switch
    {
        ScalarValue { Value: string text } => new ScalarValue(LogScrubber.Scrub(text)),
        SequenceValue sequence => new SequenceValue(sequence.Elements.Select(Scrub)),
        StructureValue structure => new StructureValue(
            structure.Properties.Select(p => new LogEventProperty(p.Name, Scrub(p.Value))),
            structure.TypeTag),
        DictionaryValue dictionary => new DictionaryValue(
            dictionary.Elements.Select(kvp =>
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(kvp.Key, Scrub(kvp.Value)))),
        _ => value
    };

    public void Dispose() => (inner as IDisposable)?.Dispose();
}
