using Newtonsoft.Json;

[Serializable]
public class RateJSON {
    [JsonProperty("rate")]
    public double rate {get; set;}

    [JsonProperty("count")]
    public int count{get; set;}
}