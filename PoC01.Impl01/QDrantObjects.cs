using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PoC01.Impl01
{
    // Qdrant: Struttura per inserimento (Upsert)
    public class QdrantUpsertRequest { public List<Point> Points { get; set; } }
    public class Point { public Guid Id { get; set; } public float[] Vector { get; set; } public Dictionary<string, object> Payload { get; set; } }

    // Qdrant: Risposta per la ricerca
    public class QdrantSearchRequest { public float[] Vector { get; set; } public int Limit { get; set; } = 3; public bool with_payload { get; set; } = true; }
    public class QdrantSearchResponse { public List<ScoredPoint> Result { get; set; } }
    public class ScoredPoint { public float Score { get; set; } public Dictionary<string, JsonElement> Payload { get; set; } }
}
