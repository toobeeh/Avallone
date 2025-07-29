using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace tobeh.Avallone.Server.Util;

/// <summary>
/// Converter to safely read JSON strings, handling potential encoding issues.
/// Especially needed since invalid user names broke lobby updating
/// </summary>
public class SafeJsonStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            return reader.GetString();
        }
        catch
        {
            // Attempt to recover the raw bytes and decode safely
            if (reader.HasValueSequence)
            {
                var bytes = reader.ValueSequence.ToArray();
                return Encoding.UTF8.GetString(bytes, 0, bytes.Length)
                    .Replace("\uFFFD", ""); // remove replacement chars
            }
            else
            {
                var bytes = reader.ValueSpan.ToArray();
                return Encoding.UTF8.GetString(bytes, 0, bytes.Length)
                    .Replace("\uFFFD", ""); // remove replacement chars
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}