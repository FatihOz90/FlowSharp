using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace FlowSharp.Web.Logging;

/// <summary>
/// <see cref="ScrubbingSink"/>'i Serilog yapilandirmasina baglayan extension.
/// Saridigi tum sink'ler (Console, File, ...) temizlenmis olaylari alir.
/// </summary>
public static class ScrubbingSinkExtensions
{
    /// <summary>
    /// Icine yazilan sink'leri merkezi temizleme (log forging + PII maskeleme) ile sarar.
    /// Kullanim: <c>WriteTo.Scrubbed(write =&gt; write.Console().WriteTo.File(...))</c>.
    /// </summary>
    public static LoggerConfiguration Scrubbed(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        Action<LoggerSinkConfiguration> configureWrappedSinks,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null)
    {
        var wrapper = LoggerSinkConfiguration.Wrap(
            inner => new ScrubbingSink(inner),
            configureWrappedSinks);

        return loggerSinkConfiguration.Sink(wrapper, restrictedToMinimumLevel, levelSwitch);
    }
}
