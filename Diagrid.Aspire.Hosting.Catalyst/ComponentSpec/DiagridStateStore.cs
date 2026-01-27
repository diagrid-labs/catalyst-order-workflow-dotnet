using Newtonsoft.Json;

namespace Diagrid.Aspire.Hosting.Catalyst.ComponentSpec;

// see: https://docs.diagrid.io/references/components-reference/state/diagrid/

public class DiagridStateStore : CatalystComponent<DiagridStateSpecMetadata>
{
    public string Type => "state.diagrid";

    public IList<string> Scopes { get; init; } = [];

    public required DiagridStateSpecMetadata Metadata { get; init; }
}

public record DiagridStateSpecMetadata
{
    [JsonProperty("state")]
    public required string State { get; init; }

    [JsonProperty("keyPrefix")]
    public string? KeyPrefix { get; init; }

    [JsonProperty("outboxDiscardWhenMissingState")]
    public string? OutboxDiscardWhenMissingState { get; init; }

    [JsonProperty("outboxPublishPubsub")]
    public string? OutboxPublishPubsub { get; init; }

    [JsonProperty("outboxPublishTopic")]
    public string? OutboxPublishTopic { get; init; }

    [JsonProperty("outboxPubsub")]
    public string? OutboxPubsub { get; init; }
}
